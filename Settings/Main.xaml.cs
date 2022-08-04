using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using SharedProject;
using SharedProject.Models;

namespace Settings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Main : Window
    {
        public Main()
        {

            //MessageBox.Show(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            InitializeComponent();
            AppConfig.Open();

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            // set defaults
            DatabaseHostnameTextBox.Text = AppConfig.Get("DatabaseHostname");
            DatabaseUsernameTextBox.Text = AppConfig.Get("DatabaseUsername");
            DatabasePasswordTextBox.Password = StringCipher.Decrypt(AppConfig.Get("DatabasePassword"));
            MachineDomainTextBox.Text = AppConfig.Get("MachineDomain");
            MachineUsernameTextBox.Text = AppConfig.Get("MachineUsername");
            MachinePasswordPasswordBox.Password = StringCipher.Decrypt(AppConfig.Get("MachinePassword"));
            MachineVNCPassPasswordBox.Password = StringCipher.Decrypt(AppConfig.Get("MachineVNCPass"));
            RDPWidthTextBox.Text = AppConfig.Get("RDPWidth");
            RDPHeightTextBox.Text = AppConfig.Get("RDPHeight");
            RDPFullscreenCheckBox.IsChecked = AppConfig.Get("RDPFullscreen").Equals("true") ? true : false;
            SourceControlProductComboBox.SelectedValue = AppConfig.Get("SourceControlProduct");
            SourceControlUsernameTextBox.Text = AppConfig.Get(AppConfig.Get("SourceControlProduct") + "Username");

            if (AppConfig.Get(AppConfig.Get("SourceControlProduct") + "Password").Length > 0)

                SourceControlPasswordPasswordBox.Password = StringCipher.Decrypt(AppConfig.Get(AppConfig.Get("SourceControlProduct") + "Password"));

            SourceControlDeploymentBranchTextBox.Text = AppConfig.Get("SourceControlDeploymentBranch");
            SourceControlURLTextBox.Text = AppConfig.Get(AppConfig.Get("SourceControlProduct") + "URL");
            ProwlAPIKeyTextBox.Text = AppConfig.Get("ProwlAPIKey");
            RFTAppPathTextBox.Text = AppConfig.Get("RFTAppPath");
            RFTJREPathTextBox.Text = AppConfig.Get("RFTJREPath");
            RFTCleanCheckBox.IsChecked = AppConfig.Get("RFTClean").Equals("true") ? true : false;
            SeAppPathTextBox.Text = AppConfig.Get("SeleniumAppPath");
            SeJREPathTextBox.Text = AppConfig.Get("SeleniumJREPath");
            SeCleanCheckBox.IsChecked = AppConfig.Get("SeleniumClean").Equals("true") ? true : false;
            DebugModeCheckBox.IsChecked = AppConfig.Get("DebugMode").Equals("true") ? true : false;

        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)

                Close();
        }

        void Main_Closing(object sender, CancelEventArgs e)
        {

            // write config

            AppConfig.Put("DatabaseHostname", DatabaseHostnameTextBox.Text);
            AppConfig.Put("DatabaseUsername", DatabaseUsernameTextBox.Text);
            AppConfig.Put("DatabasePassword", StringCipher.Encrypt(DatabasePasswordTextBox.Password));
            AppConfig.Put("MachineDomain", MachineDomainTextBox.Text);
            AppConfig.Put("MachineUsername", MachineUsernameTextBox.Text);
            AppConfig.Put("MachinePassword", StringCipher.Encrypt(MachinePasswordPasswordBox.Password));
            AppConfig.Put("MachineVNCPass", StringCipher.Encrypt(MachineVNCPassPasswordBox.Password));
            AppConfig.Put("RDPWidth", RDPWidthTextBox.Text);
            AppConfig.Put("RDPHeight", RDPHeightTextBox.Text);
            AppConfig.Put("RDPFullscreen", RDPFullscreenCheckBox.IsChecked == true ? "true" : "false");
            AppConfig.Put(SourceControlProductComboBox.SelectedValue.ToString() + "Username", SourceControlUsernameTextBox.Text);

            if (SourceControlPasswordPasswordBox.Password.Length > 0)

                AppConfig.Put(SourceControlProductComboBox.SelectedValue.ToString() + "Password", StringCipher.Encrypt(SourceControlPasswordPasswordBox.Password));

            AppConfig.Put(SourceControlProductComboBox.SelectedValue.ToString() + "URL", SourceControlURLTextBox.Text);
            AppConfig.Put("SourceControlProduct", SourceControlProductComboBox.SelectedValue.ToString());
            AppConfig.Put("SourceControlDeploymentBranch", SourceControlDeploymentBranchTextBox.Text);
            AppConfig.Put("ProwlAPIKey", ProwlAPIKeyTextBox.Text);
            AppConfig.Put("RFTAppPath", RFTAppPathTextBox.Text);
            AppConfig.Put("RFTJREPath", RFTJREPathTextBox.Text);
            AppConfig.Put("RFTClean", RFTCleanCheckBox.IsChecked == true ? "true" : "false");
            AppConfig.Put("SeleniumAppPath", SeAppPathTextBox.Text);
            AppConfig.Put("SeleniumJREPath", SeJREPathTextBox.Text);
            AppConfig.Put("SeleniumClean", SeCleanCheckBox.IsChecked == true ? "true" : "false");
            AppConfig.Put("DebugMode", DebugModeCheckBox.IsChecked == true ? "true" : "false");
            AppConfig.Save();

            if (AppConfig.Get("SourceControlProduct").Equals("Git"))
            {
                // Run as current user (do not run as admin because R and L drives won't create - psubst behaviour)
                Command2.ExecuteInProgramDirectory("psubstgit.bat", null, ProcessWindowStyle.Hidden, false);
            }
            else
            {
                // Run as current user (do not run as admin because R and L drives won't create - psubst behaviour)
                Command2.ExecuteInProgramDirectory("psubsttfs.bat", null, ProcessWindowStyle.Hidden, false);
            }

        }


        private void DatabaseBackupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DatabaseLogin();
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                var path = DBBackupRestore.Backup(dialog.username, dialog.password);
                System.Windows.MessageBox.Show("Backup saved to " + path, "CoPilot", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }

        private void DatabaseSizeButton_Click(object sender, RoutedEventArgs e)
        {
            var dbConnect = new DBConnect("control_automation_machine");
            var database_table_sizes = dbConnect.Select<InformationSchema>("SELECT table_schema as `Database`,table_name AS `Table`,round(((data_length + index_length) / 1024 / 1024), 2) `SizeInMb` FROM information_schema.TABLES ORDER BY(data_length + index_length) DESC;");
            var csv = "Database,Table,Size in MB";

            foreach (var database_table_size in database_table_sizes)
            {
                csv = csv + "\n" + database_table_size.Database + "," + database_table_size.Table + "," + database_table_size.SizeInMb;
            }

            SharedProject.File.overwrite("R:\\databasesize.csv", csv);
            System.Windows.MessageBox.Show("Database size report saved to R:\\databasesize.csv", "CoPilot", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void DatabasePurgeOldLogsButton_Click(object sender, RoutedEventArgs e)
        {
            var dbConnect = new DBConnect("control_automation_machine");
            var execution_group_script_log_tables = dbConnect.Select<InformationSchema>("SELECT table_schema as `Database`,table_name AS `Table` FROM information_schema.TABLES WHERE table_name='execution_group_script_log';");

            foreach (var execution_group_script_log_table in execution_group_script_log_tables)
            {
                dbConnect = new DBConnect(execution_group_script_log_table.Database);
                dbConnect.Delete("DELETE FROM execution_group_script_log WHERE date_time < CURRENT_DATE() - INTERVAL 12 MONTH;");
                dbConnect.Optimize("OPTIMIZE table execution_group_script_log;");
            }


        }


        private void SourceControlProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var product = (sender as ComboBox).SelectedValue.ToString();

            if (AppConfig.Get(product + "Username") != null)

                SourceControlUsernameTextBox.Text = AppConfig.Get(product + "Username");

            if (AppConfig.Get(product + "Password") != null)

                SourceControlPasswordPasswordBox.Password = StringCipher.Decrypt(AppConfig.Get(product + "Password"));

            if (AppConfig.Get(product + "URL") != null)

                SourceControlURLTextBox.Text = AppConfig.Get(product + "URL");

            //(sender as ComboBox).SelectedValue = "Git";

            //// Write this to the config as the last used project
            //AppConfig.Put("ManageExecutionGroupsProjectPath", project.path);

            //// apply the filters

            //string filter_clause = "";

            ////  if GUICtrlRead($machines_archived_checkbox) = $GUI_CHECKED Then

            ////$filter_clause = $filter_clause & " AND eg.archived = 1"

            ////  Else

            //filter_clause = filter_clause + " AND eg.archived = 0";

            ////  EndIf

            //// refresh the Execution Groups listview
            //TextBlock.Update(StatusBarText, "Please Wait. Getting execution groups ...");
            //ExecutionGroup2.QueryToListView(ExecutionGroupsListView, "SELECT eg.id, eg.name, i.name AS iteration, e.name AS test_environment, eg.start_date_time, eg.end_date_time, eg.result FROM execution_group eg, iteration i, environment e WHERE eg.iteration_id = i.id AND eg.environment_id = e.id" + filter_clause + " ORDER BY eg.name ASC LIMIT " + num_records_per_page + ";", project.schema_name);
            //TextBlock.ClearNoError(StatusBarText);


        }



    }
}
