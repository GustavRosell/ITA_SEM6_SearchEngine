namespace ConsoleSearch
{
    public static class Config
    {
        public static bool CaseSensitive { get; set; } = true;
        public static bool ViewTimeStamps { get; set; } = true;
        public static int? ResultLimit { get; set; } = 20;
        public static bool PatternSearch { get; set; } = false;
        public static bool CompactView { get; set; } = false;
    }
}
