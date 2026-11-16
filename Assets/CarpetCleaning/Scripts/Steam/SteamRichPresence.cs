#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || UNITY_ANDROID || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.Globalization;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace CarpetCleaning
{
    /// <summary>
    /// Friends-list status (Docs/STEAM.md 4). steam_display names a token from Tools/Steam/rich_presence_*.vdf; until
    /// those files are uploaded in Steamworks (Community > Rich Presence) friends see only "Playing CarpetCleaning".
    /// Polls the game state every frame and pushes only on change, at most once per minInterval real seconds: the
    /// percentage ticks several times a second while scrubbing. The state is worked out even without Steam, so
    /// CarpetDebugApi.SteamJson can check the mapping in any Editor.
    /// Order 220 = after CarpetHandover (210), so a state change is seen in the frame it happens.
    /// </summary>
    [DefaultExecutionOrder(220)]
    public sealed class SteamRichPresence : MonoBehaviour
    {
        public const string TokenCleaning = "#Status_Cleaning";               // "%clean% clean"
        public const string TokenSpotless = "#Status_Spotless";               // won, waiting to be handed over
        public const string TokenBetweenCarpets = "#Status_BetweenCarpets";   // carpet flying out or away, floor still there

        public CarpetSimulation sim;
        public CarpetHandover handover;
        [Tooltip("Minimum real seconds between two pushes to Steam.")]
        public float minInterval = 2f;

        /// <summary>What the game state asks for right now.</summary>
        public string Token { get; private set; } = "";
        public string Clean { get; private set; } = "";
        /// <summary>What Steam was last told (empty until the first push).</summary>
        public string SentToken { get; private set; } = "";
        public string SentClean { get; private set; } = "";
        public int PushCount { get; private set; }

        float _lastPush = float.NegativeInfinity;

        void Awake()
        {
            if (sim == null) sim = FindAnyObjectByType<CarpetSimulation>();
            if (handover == null) handover = FindAnyObjectByType<CarpetHandover>();
        }

        // SteamService clears the presence when it shuts down; a re-enabled component must send everything again
        void OnDisable()
        {
            SentToken = "";
            SentClean = "";
            _lastPush = float.NegativeInfinity;
        }

        void LateUpdate()
        {
            Evaluate();
            if (Token == SentToken && Clean == SentClean) return;
            if (Time.unscaledTime - _lastPush < minInterval) return;
            Push();
        }

        /// <summary>Game state -> token and value. FlyIn counts as cleaning: the fresh carpet is already on its way down.</summary>
        public void Evaluate()
        {
            var st = handover != null ? handover.State : HandoverState.Idle;
            if (st == HandoverState.FlyOut || st == HandoverState.Away)
            {
                Token = TokenBetweenCarpets;
                Clean = "";
            }
            else if (st == HandoverState.Idle && sim != null && sim.IsWon)
            {
                Token = TokenSpotless;
                Clean = "";
            }
            else
            {
                // floor, not round: 99.6% must not read 100% while the win check still says no
                int pct = sim != null ? Mathf.Clamp(Mathf.FloorToInt(sim.Cleanliness * 100f), 0, 100) : 0;
                Token = TokenCleaning;
                Clean = pct.ToString(CultureInfo.InvariantCulture) + "%";
            }
        }

        /// <summary>Sends the current state now, ignoring minInterval. False when Steam is not up.</summary>
        public bool Push()
        {
#if DISABLESTEAMWORKS
            return false;
#else
            if (!SteamService.IsReady) return false;
            // the value first: steam_display is what makes Steam re-render the line
            SteamFriends.SetRichPresence("clean", Clean);
            SteamFriends.SetRichPresence("steam_display", Token);
            SentToken = Token;
            SentClean = Clean;
            PushCount++;
            _lastPush = Time.unscaledTime;
            return true;
#endif
        }
    }
}
