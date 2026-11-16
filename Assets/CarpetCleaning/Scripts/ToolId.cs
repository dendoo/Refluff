namespace CarpetCleaning
{
    public enum ToolId
    {
        Scraper = 0,
        DryVacuum = 1,
        Hose = 2,
        FoamSprayer = 3,
        Brush = 4,
        Squeegee = 5,
        WetVacuum = 6,
        Dryer = 7,
        // Floor-only tools (Docs/FLOOR_TOOLS.md): no branch in CarpetSim.compute, so they never touch the carpet.
        FloorCleaner = 8,
        Mop = 9
    }
}
