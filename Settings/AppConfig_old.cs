using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Settings
{
    class AppConfig_old
    {
        static private Configuration configuration;

        static public void Open()
        {
            var CoPilotConfigPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\CoPilot.exe.config";

            if (!File.Exists(CoPilotConfigPath))

                CoPilotConfigPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\..\\..\\..\\CoPilot\\App.config";

            if (!File.Exists(CoPilotConfigPath))
            {
                MessageBox.Show("Can't find CoPilot config.  Exiting.");
                System.Windows.Application.Current.Shutdown();
            }

            System.Configuration.ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
            fileMap.ExeConfigFilename = CoPilotConfigPath;
            configuration = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
        }

        static public string Get(string key)
        {
            return configuration.AppSettings.Settings[key].Value;
        }

        static public void Put(string key, string value)
        {
            configuration.AppSettings.Settings[key].Value = value;
        }

        static public void Save()
        {
            configuration.Save();
        }
    }
}
