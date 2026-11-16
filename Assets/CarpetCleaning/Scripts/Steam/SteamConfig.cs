namespace CarpetCleaning
{
    /// <summary>
    /// Steam ids (Docs/STEAM.md 1). The same numbers live in steam_appid.txt (project root, Editor only) and in the
    /// SteamPipe scripts Tools/Steam/app_build_*.vdf / depot_build_*.vdf; change all of them together.
    /// </summary>
    public static class SteamConfig
    {
        public const uint AppId = 5304930;
        /// <summary>The Windows content depot. Steamworks creates AppId + 1 by default; SteamPipe > Depots is the authority.</summary>
        public const uint DepotId = 5304931;
    }
}
