using Newtonsoft.Json;
using System.IO;

namespace KML2SQL
{
    internal static class SettingsPersister
    {
        private static object _sync = new object();
        private static string FileName = Path.Combine(Utility.GetApplicationFolder(), "KML2SQL.settings");

        public static void Persist(Settings settings)
        {
            lock (_sync)
            {
                string settingsText = JsonConvert.SerializeObject(settings);
                File.WriteAllText(FileName, settingsText);
            }
        }

        public static void DeleteSettings()
        {
            lock (_sync)
            {
                if (File.Exists(FileName))
                    File.Delete(FileName);
            }
        }

        public static Settings Retrieve()
        {
            lock (_sync)
            {
                if (File.Exists(FileName))
                {
                    try
                    {
                        string settingsText = File.ReadAllText(FileName);
                        Settings settings = JsonConvert.DeserializeObject<Settings>(settingsText);
                        return settings;
                    }
                    catch
                    {
                        File.Delete(FileName);
                    }
                }
                return null;
            }
        }
    }
}