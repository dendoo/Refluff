// Same platform gate as Steamworks.NET itself: outside it the Steamworks namespace does not exist and this is a no-op.
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || UNITY_ANDROID || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace CarpetCleaning
{
    /// <summary>
    /// Steamworks lifetime (Docs/STEAM.md 2): relaunch through Steam, SteamAPI init, callbacks every frame, shutdown.
    /// A failed init is logged and the game runs on without Steam: no hard DRM, and the Editor works without the client.
    /// While the Steam overlay is up the simulation is paused and the tools stop listening (Docs/STEAM.md 3).
    /// Order -1000: Steam is up before any other Awake, and RestartAppIfNecessary can quit before anything heavy starts.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class SteamService : MonoBehaviour
    {
        public CarpetSimulation sim;
        public ToolController tools;
        [Tooltip("Initialise Steam in Play mode inside the Editor (needs the Steam client running and steam_appid.txt in the project root). Players always initialise.")]
        public bool initInEditor = true;

        public static SteamService Instance { get; private set; }
        public static bool IsReady { get; private set; }
        public static bool OverlayActive { get; private set; }
        public static uint RunningAppId { get; private set; }
        public static string PersonaName { get; private set; } = "";
        /// <summary>The language the player runs the game in on Steam (Steam API name: "english", "schinese", "latam" ...);
        /// empty without Steam. Loc takes it as the first-launch language (Docs/LOCALIZATION.md 2).</summary>
        public static string GameLanguage { get; private set; } = "";
        public static string LastError { get; private set; } = "";

        bool _pausedByOverlay;
#if !DISABLESTEAMWORKS
        Callback<GameOverlayActivated_t> _overlayCallback;
        static SteamAPIWarningMessageHook_t s_warningHook;   // held in a static so the GC never frees what native code calls
#endif

        // domain reload is disabled: a static left over from the previous play session must not claim Steam is still up
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            IsReady = false;
            OverlayActive = false;
            RunningAppId = 0;
            PersonaName = "";
            GameLanguage = "";
            LastError = "";
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[Steam] a second SteamService was found and removed");
                Destroy(this);
                return;
            }
            Instance = this;
            if (sim == null) sim = FindAnyObjectByType<CarpetSimulation>();
            if (tools == null) tools = FindAnyObjectByType<ToolController>();
            Init();
        }

        void Update()
        {
#if !DISABLESTEAMWORKS
            if (IsReady) SteamAPI.RunCallbacks();
#endif
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Shutdown();
            Instance = null;
        }

        void Init()
        {
#if DISABLESTEAMWORKS
            LastError = "Steamworks is not supported on this platform";
#else
#if UNITY_EDITOR
            if (!initInEditor)
            {
                LastError = "disabled in the Editor (SteamService.initInEditor)";
                Debug.Log("[Steam] " + LastError);
                return;
            }
#endif
            if (!Packsize.Test()) { Fail("Packsize.Test failed: the Steamworks.NET build does not match this platform"); return; }
            if (!DllCheck.Test()) { Fail("DllCheck.Test failed: steam_api64.dll does not match Steamworks.NET"); return; }
            try
            {
#if !UNITY_EDITOR
                // Started from the exe directly with no steam_appid.txt beside it: Steam starts the game again through
                // the client (ownership check, overlay injection) and this copy quits. Local test builds carry the file.
                if (SteamAPI.RestartAppIfNecessary(new AppId_t(SteamConfig.AppId)))
                {
                    Debug.Log("[Steam] not launched by Steam: relaunching through the client");
                    Application.Quit();
                    return;
                }
#endif
                var result = SteamAPI.InitEx(out string msg);
                if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK) { Fail(result + ": " + msg); return; }
            }
            catch (DllNotFoundException e) { Fail("steam_api64.dll could not be loaded: " + e.Message); return; }

            IsReady = true;
            LastError = "";
            RunningAppId = SteamUtils.GetAppID().m_AppId;
            PersonaName = SteamFriends.GetPersonaName() ?? "";
            GameLanguage = SteamApps.GetCurrentGameLanguage() ?? "";
            s_warningHook = OnSteamWarning;
            SteamClient.SetWarningMessageHook(s_warningHook);
            _overlayCallback = Callback<GameOverlayActivated_t>.Create(OnOverlayActivated);
            Debug.Log("[Steam] ready: app " + RunningAppId + ", user " + PersonaName + ", game language " + GameLanguage);
            if (RunningAppId != SteamConfig.AppId)
                Debug.LogWarning("[Steam] running as app " + RunningAppId + " but SteamConfig.AppId is " + SteamConfig.AppId + " (check steam_appid.txt)");
#endif
        }

        void Fail(string reason)
        {
            LastError = reason;
            Debug.LogWarning("[Steam] init failed, running without Steam: " + reason);
        }

        void Shutdown()
        {
            SetOverlay(false);   // never leave the sim paused behind
#if !DISABLESTEAMWORKS
            if (!IsReady) return;
            IsReady = false;
            SteamFriends.ClearRichPresence();
            if (_overlayCallback != null) { _overlayCallback.Dispose(); _overlayCallback = null; }
            SteamAPI.Shutdown();
            Debug.Log("[Steam] shut down");
#endif
        }

#if !DISABLESTEAMWORKS
        void OnOverlayActivated(GameOverlayActivated_t e) { SetOverlay(e.m_bActive != 0); }

        [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
        static void OnSteamWarning(int severity, System.Text.StringBuilder text)
        {
            Debug.LogWarning("[Steam] " + text);
        }
#endif

        /// <summary>
        /// Overlay up: the sim pauses and the tools stop reading the mouse and the hotkeys (the Input System still sees
        /// input under the overlay). Down: only a pause this class set is lifted. Public so CarpetDebugApi can fake it.
        /// </summary>
        public void SetOverlay(bool active)
        {
            if (active == OverlayActive) return;
            OverlayActive = active;
            if (tools != null) tools.Suspended = active;
            if (sim == null) return;
            if (active)
            {
                if (!sim.Paused) { sim.Paused = true; _pausedByOverlay = true; }
            }
            else if (_pausedByOverlay)
            {
                sim.Paused = false;
                _pausedByOverlay = false;
            }
        }
    }
}
