using Kml2Sql.Mapping;
using KML2SQL.Updates;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace KML2SQL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///
    public partial class MainWindow : Window
    {
        private Kml2SqlConfig config;

        public MainWindow()
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            InitializeComponent();
            if (!Directory.Exists(Utility.GetApplicationFolder()))
                Directory.CreateDirectory(Utility.GetApplicationFolder());
            saveScriptTo.Text = Utility.GetDefaultScriptSaveLoc();
            RestoreSettings();
            Task.Run(UpdateChecker.CheckForNewVersion);
        }

        private void ResetUploadButton()
        {
            CreateDatabaseButton.Content = "Start";
            if (CreateDatabaseButton.Visibility != Visibility.Visible)
                CreateDatabaseButton.Visibility = Visibility.Visible;
        }

        private void serverNameBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (serverNameBox.Text == "foo.myserver.com")
                serverNameBox.Clear();
        }

        private void KMLFileLocationBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (KMLFileLocationBox.Text == "C:\\...")
                KMLFileLocationBox.Clear();
        }

        private void userNameBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (userNameBox.Text == "username")
                userNameBox.Clear();
        }

        private void passwordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            passwordBox.Clear();
        }

        private void tableBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (tableBox.Text == "myTable")
                tableBox.Clear();
        }

        private void columnNameBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (columnNameBox.Text == "polygon")
                columnNameBox.Clear();
        }

        private void sridCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            sridBox.IsEnabled = true;
        }

        private void sridCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            sridBox.Text = "4326";
            sridBox.IsEnabled = false;
        }

        private void CreateDatabaseButton_Click(object sender, RoutedEventArgs routedArgs)
        {
            SaveSettings();
            UploadFile();
        }

        private async void UploadFile()
        {
            Logger logger = new Logger();
            try
            {
                CreateDatabaseButton.Visibility = Visibility.Hidden;
                Kml2SqlConfig config = GetConfig();
                string kmlFile = KMLFileLocationBox.Text;
                if (!config.PolygonToClipboard)
                {
                    string connectionString = BuildConnectionString();
                    Progress<ProgressReoprt> progresss = new Progress<ProgressReoprt>(p =>
                    {
                        UpdateProgressBar(p.PercentDone);
                        logger.AddToLog(p.Message);
                        if (p.Exception != null)
                        {
                            AlertFailure(p.Exception);
                            logger.AddToLog(p.Exception);
                            logger.WriteOut();
                            CreateDatabaseButton.Visibility = Visibility.Visible;
                        }
                        if (p.PercentDone == 100)
                        {
                            logger.WriteOut();
                        }
                    });
                    bool dropTable = Convert.ToBoolean(dropExisting.IsChecked);
                    if (tabControl.SelectedIndex == 0)
                    {
                        await Task.Run(() =>
                        {
                            Uploader uploader = new Uploader(kmlFile, config, progresss);
                            uploader.Upload(connectionString, dropTable);
                        });
                    }
                    else
                    {
                        string fileLoc = saveScriptTo.Text;
                        await Task.Run(() =>
                        {
                            Uploader uploader = new Uploader(kmlFile, config, progresss);
                            string script = uploader.GetScript();
                            File.WriteAllText(fileLoc, script);
                        });
                    }
                }
                else
                {
                    Progress<ProgressReoprt> progresss = new Progress<ProgressReoprt>(p =>
                    {
                        UpdateProgressBar(p.PercentDone);
                    });

                    Uploader uploader = new Uploader(kmlFile, config, progresss);
                    string script = uploader.GetScript();
                    Clipboard.Clear();
                    Clipboard.SetText(script);
                }

                await Task.Delay(2000);
                ResetUploadButton();
            }
            catch (Exception ex)
            {
                CreateDatabaseButton.Visibility = Visibility.Visible;
                logger.AddToLog(ex);
                AlertFailure(ex);
            }
        }

        private void AlertFailure(Exception ex)
        {
            MessageBox.Show("The process failed with the following error. See the log for details: \r\n\r\n "
                    + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            WriteErrorLog(ex);
        }

        private void WriteErrorLog(Exception ex)
        {
            string logFileName = "log_" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-ffff") + ".txt";
            string fullPath = System.IO.Path.Combine(Utility.GetApplicationFolder(), logFileName);
            File.WriteAllText(fullPath, ex.ToString());
        }

        public void UpdateProgressBar(int percentage)
        {
            progressBar.Value = percentage;
            if (percentage == 100)
            {
                CreateDatabaseButton.Visibility = Visibility.Visible;
                CreateDatabaseButton.Content = "Finished!";
            }
        }

        private Kml2SqlConfig GetConfig()
        {
            config = new Kml2SqlConfig
            {
                GeoType = geographyMode.IsChecked != null && geographyMode.IsChecked.Value ? PolygonType.Geography : PolygonType.Geometry,
                GeographyOnly = geographyOnly.IsChecked != null ? geographyOnly.IsChecked.Value : false,
                PolygonToClipboard = polyToClipboard.IsChecked != null ? polyToClipboard.IsChecked.Value : false
            };

            config.Srid = ParseSRID(config.GeoType);
            config.TableName = tableBox.Text;
            config.PlacemarkColumnName = columnNameBox.Text;
            config.FixPolygons = Convert.ToBoolean(fixBrokenPolygons.IsChecked);
            return config;
        }

        private void SaveSettings()
        {
            Settings settings = new Settings
            {
                DatabaseName = databaseNameBox.Text,
                ServerName = serverNameBox.Text,
                KMLFileName = KMLFileLocationBox.Text,
                TableName = tableBox.Text,
                ShapeColumnName = columnNameBox.Text,
                Login = userNameBox.Text,
                SRID = sridBox.Text,
                SRIDEnabled = sridCheckBox.IsChecked.Value,
                Geography = geographyMode.IsChecked.Value,
                UseIntegratedSecurity = integratedSecurityCheckbox.IsChecked.Value,
                FixBrokenPolygons = fixBrokenPolygons.IsChecked.Value
            };
            SettingsPersister.Persist(settings);
        }

        private void RestoreSettings()
        {
            var settings = SettingsPersister.Retrieve();
            if (settings != null)
            {
                geographyMode.IsChecked = settings.Geography;
                sridCheckBox.IsChecked = settings.SRIDEnabled;
                sridBox.Text = settings.SRID;
                userNameBox.Text = settings.Login;
                columnNameBox.Text = settings.ShapeColumnName;
                tableBox.Text = settings.TableName;
                KMLFileLocationBox.Text = settings.KMLFileName;
                serverNameBox.Text = settings.ServerName;
                databaseNameBox.Text = settings.DatabaseName;
                integratedSecurityCheckbox.IsChecked = settings.UseIntegratedSecurity;
                fixBrokenPolygons.IsChecked = settings.FixBrokenPolygons;
            }
        }

        private string BuildConnectionString()
        {
            string connString = "Data Source=" + serverNameBox.Text + ";Initial Catalog=" + databaseNameBox.Text + ";Persist Security Info=True;";
            if (integratedSecurityCheckbox.IsChecked ?? false)
                connString += "Integrated Security = SSPI;";
            else
                connString += "User ID=" + userNameBox.Text + ";Password=" + passwordBox.Password;
            return connString;
        }

        private int ParseSRID(PolygonType geoType)
        {
            if (geoType == PolygonType.Geometry)
            {
                return 4326;
            }
            else
            {
                if (int.TryParse(sridBox.Text, out int srid))
                    return srid;
                else
                    MessageBox.Show("SRID must be a valid four digit number");
                return srid;
            }
        }

        private void databaseNameBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (databaseNameBox.Text == "myDatabase")
                databaseNameBox.Clear();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myOpenFileDialog = new OpenFileDialog();
            myOpenFileDialog.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
            myOpenFileDialog.Filter = "KML Files (*.kml|*.kml|All Files (*.*)|*.*";
            myOpenFileDialog.FileName = "myFile.kml";
            bool? result = myOpenFileDialog.ShowDialog();
            if (result == true)
            {
                try
                {
                    KMLFileLocationBox.Text = myOpenFileDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occured while opening the file" + myOpenFileDialog.FileName + "\n" + ex.Message, "Unable to open KML file.");
                }
            }
        }

        private void geometryMode_Checked(object sender, RoutedEventArgs e)
        {
            if (sridCheckBox != null)
                sridCheckBox.IsEnabled = false;
            if (sridBox != null)
                sridBox.Text = "NA";
        }

        private void geographyMode_Checked(object sender, RoutedEventArgs e)
        {
            if (sridCheckBox != null)
                sridCheckBox.IsEnabled = true;
            if (sridBox != null)
                sridBox.Text = "4326";
        }

        private void About_MouseEnter(object sender, MouseEventArgs e)
        {
            About.Opacity = 1;
        }

        private void About_MouseLeave(object sender, MouseEventArgs e)
        {
            About.Opacity = .25;
        }

        private void About_MouseDown(object sender, MouseButtonEventArgs e)
        {
            About about = new About();
            about.Show();
        }

        private void Log_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Utility.GetApplicationFolder());
        }

        private void IntegratedSecurityCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (integratedSecurityCheckbox.IsChecked.Value)
            {
                userNameBox.IsEnabled = false;
                passwordBox.IsEnabled = false;
            }
            else
            {
                userNameBox.IsEnabled = true;
                passwordBox.IsEnabled = true;
            }
        }

        private void geographyOnly_Checked(object sender, RoutedEventArgs e)
        {
            if (geographyOnly.IsChecked.HasValue)
            {
                databaseNameBox.IsEnabled = tableBox.IsEnabled = columnNameBox.IsEnabled = dropExisting.IsEnabled = !geographyOnly.IsChecked.Value;
            }
        }

        private void polyToClipboard_Checked(object sender, RoutedEventArgs e)
        {
            geographyOnly.IsChecked = true;
            geographyOnly_Checked(sender, e);
        }
    }
}