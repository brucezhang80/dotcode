using dotcode.Properties;

namespace dotcode.Models
{
    public static class GlobalSettings
    {
        public static string TempCompilerDir { get; set; }

        static GlobalSettings()
        {
            TempCompilerDir = Settings.Default.TempCompilerDir;
        }
    }
}