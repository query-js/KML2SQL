using Newtonsoft.Json.Linq;
using Semver;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace KML2SQL.Updates
{
    public static class UpdateChecker
    {
        private static readonly string apiUrl = "https://api.github.com/repos/query-js/KML2SQL/releases/latest";
        private static readonly string downloadUrl = "https://github.com/query-js/KML2SQL/releases";

        public static async Task CheckForNewVersion()
        {
            Settings settings = SettingsPersister.Retrieve();
            if (CheckForUpdates(settings))
            {
                settings.UpdateInfo.LastCheckedForUpdates = DateTime.Now;
                HttpClient client = new HttpClient();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    SemVersion latestVersion = await GetLatestVersion(response);
                    SemVersion thisVersion = SemVersion.Parse(GetCurrentVersion());
                    if (latestVersion > thisVersion && ShouldNag(settings, latestVersion))
                    {
                        settings.UpdateInfo.LastTimeNagged = DateTime.Now;
                        MessageBoxResult mbResult = MessageBox.Show(
                            "A new version is availbe. Press 'Yes' to go to the download page, Cancel to skip, or 'No' to not be reminded unless an even newer version comes out.",
                            "New Version Available!",
                            MessageBoxButton.YesNoCancel);
                        if (mbResult == MessageBoxResult.Yes)
                        {
                            Process.Start(downloadUrl);
                        }
                        if (mbResult == MessageBoxResult.No)
                        {
                            settings.UpdateInfo.DontNag = true;
                        }
                    }
                }
                SettingsPersister.Persist(settings);
            }
        }

        private static async Task<SemVersion> GetLatestVersion(HttpResponseMessage response)
        {
            string json = await response.Content.ReadAsStringAsync();
            JObject obj = JObject.Parse(json);
            string latestVersionString = (string)obj["tag_name"];
            SemVersion latestVersion = SemVersion.Parse(latestVersionString);
            return latestVersion;
        }

        private static bool ShouldNag(Settings settings, SemVersion latestVersion)
        {
            bool shouldNag = false;
            SemVersion lastVersionSeen = SemVersion.Parse(settings.UpdateInfo.LastVersionSeen ?? "0.1");
            if (latestVersion > lastVersionSeen)
            {
                shouldNag = true;
                settings.UpdateInfo.LastVersionSeen = latestVersion.ToString();
            }
            return shouldNag;
        }

        private static bool CheckForUpdates(Settings settings)
        {
            if (settings == null)
            {
                return false;
            }
            if (settings.UpdateInfo == null)
            {
                settings.UpdateInfo = new UpdateInfo();
            }
            bool check = settings.UpdateInfo.LastCheckedForUpdates < DateTime.Now.AddDays(-1)
                         && settings.UpdateInfo.LastTimeNagged < DateTime.Now.AddDays(-7);
            return check;
        }

        internal static string GetCurrentVersion()
        {
            AssemblyInformationalVersionAttribute[] attr = Assembly
                .GetEntryAssembly()
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                as AssemblyInformationalVersionAttribute[];
            return attr.First().InformationalVersion;
        }
    }
}