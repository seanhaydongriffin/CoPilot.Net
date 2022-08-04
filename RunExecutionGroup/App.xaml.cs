using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RunExecutionGroup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static public System.Windows.Controls.TextBlock CurrentStatusBarText = null;

        public static string mysql_host = ""; //"localhost";
        public static string mysql_user = ""; //"auto";
        public static string mysql_pass = ""; //"janison";
        public static string project_id = ""; //"13";
        public static string execution_group_id = ""; //"320";
        public static string iteration_name = ""; //"Sprint 1";
        public static string test_environment_name = ""; //"SIT";
        public static string browserstack_runtime_host_name = ""; //"";
        public static string deploy_and_build = ""; //"0";
        public static string source_control_username = ""; //"sgriffin";
        public static string source_control_password = ""; //"";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                mysql_host = e.Args[0];
                mysql_user = e.Args[1];
                mysql_pass = e.Args[2];
                project_id = e.Args[3];
                execution_group_id = e.Args[4];
                iteration_name = e.Args[5];
                test_environment_name = e.Args[6];
                browserstack_runtime_host_name = e.Args[7];
                deploy_and_build = e.Args[8];
                source_control_username = e.Args[9];
                source_control_password = e.Args[10];
            }

            Main wnd = new Main();
            wnd.Show();
        }

    }
}
