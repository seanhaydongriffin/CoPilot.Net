using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AutomationAdapter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static public System.Windows.Controls.TextBlock CurrentStatusBarText = null;

        public static string current_host_name = "";
        public static string run_ids = "";
        public static string run_auto_archive_script_logs = "";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                current_host_name = e.Args[0];
                run_ids = e.Args[1];
                run_auto_archive_script_logs = e.Args[2];
            }

            Main wnd = new Main();
            wnd.Show();
        }
    }
}
