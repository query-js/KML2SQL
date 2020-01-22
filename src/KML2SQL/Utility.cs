using System;
using System.Configuration;
using System.IO;

namespace KML2SQL
{
    public static class Utility
    {
        public static string GetApplicationFolder()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ConfigurationManager.AppSettings["AppFolder"]
            );
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public static string GetDefaultScriptSaveLoc()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "script.sql"
            );
        }
    }
}