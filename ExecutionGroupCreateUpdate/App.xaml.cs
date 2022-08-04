using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ExecutionGroupCreateUpdate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static public System.Windows.Controls.TextBlock CurrentStatusBarText = null;

        public static string schema_name = ""; //"ite_r6_se_project";
        public static string project_id = ""; //"13";
        public static string execution_group_id = ""; //"346";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                schema_name = e.Args[0];
                project_id = e.Args[1];
                execution_group_id = e.Args[2];
            }

            Main wnd = new Main();
            wnd.Show();
        }

    }
}
