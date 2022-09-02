using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Reflection;
using System.Diagnostics;
using SharedProject;
using SharedProject.Models;

namespace CoPilot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ManageExecutionGroups : Window
    {
        private int num_records_per_page = 100;
        public List<Project2> projects { get; set; }
        private Project2 project;

        public ManageExecutionGroups()
        {
            InitializeComponent();
            App.CurrentStatusBarText = StatusBarText;

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            TextBlock.Update(StatusBarText, "Please Wait. Getting projects ...");
            var dbConnect = new DBConnect("control_automation_machine");
            projects = dbConnect.Select<Project2>("SELECT id, path, schema_name, external_app, external_id, external_name FROM project WHERE archived = 0 LIMIT " + num_records_per_page + ";");
            TextBlock.ClearNoError(StatusBarText);



            //AppConfig.Get("ManageExecutionGroupsProjectPath")

            // The following will update the data in all controls bound to "projects" above (ie. ProjectPathComboBox)
            // Note to avoid events unnecessarily firing they are disabled temporarily and re-enabled
            ProjectPathComboBox.SelectionChanged -= new SelectionChangedEventHandler(ProjectPathComboBox_SelectionChanged);
            DataContext = this;
            ProjectPathComboBox.SelectionChanged += new SelectionChangedEventHandler(ProjectPathComboBox_SelectionChanged);



            // get the project last used from config and select it
            var project = projects.GetLastUsed();
            ProjectPathComboBox.SelectedItem = project;
            ProjectIDTextBox.Text = project.id.ToString();

            //ListView_Refresh_with_MySql_query_and_set_item_selected_by_id($manage_test_artifacts_gui, $projects_listview, "SELECT id, path, schema_name, external_app, external_id, external_name FROM project WHERE archived = 0 LIMIT " & $num_records_per_page & ";", 6, "projectid", $execution_groups_status_input, "control_automation_machine")

        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }



        private void ListView_Refresh_with_MySql_query_and_set_item_selected_by_id(ListView lv, string mysql_query, int num_fields, string sqlite_parameter_name, string db)
        {
            //TextBlock.Update(StatusBarText, "Please Wait. Getting machines ...");
            //var execution_schedule_line = MySQL.Query(mysql_query, db);
            //TextBlock.ClearNoError(StatusBarText);
            //lv.ItemsSource = null;
            //lv.ItemsSource = execution_schedule_line;


            //_WinAPI_LockWindowUpdate($gui)

            //_GUICtrlListView_DeleteAllItems($listview)

            //_WinAPI_LockWindowUpdate(0)


            //_GUICtrlListView_BeginUpdate($listview)


            //            for (int execution_schedule_line_num = 0; execution_schedule_line_num < execution_schedule_line.Length; execution_schedule_line_num++)
            //            {
            //                var listview_line = "";

            //                for (int j = 0; j < num_fields; j++)
            //                {
            //                    if (j > 0)

            //                        listview_line = listview_line + "|";

            //                    listview_line = listview_line + execution_schedule_line[execution_schedule_line_num][j];
            //                }

            ////                GUICtrlCreateListViewItem($listview_line, $listview)

            //            }


            //_GUICtrlListView_EndUpdate($listview)


            //_SQLite_GetTable(-1, "SELECT value FROM parameter WHERE name = '" & $sqlite_parameter_name & "';", $aResult, $iRows, $iColumns)


            //if $iRows > 0 Then


            //    for $i = 0 to _GUICtrlListView_GetItemCount($listview)


            //        Local $id = _GUICtrlListView_GetItemText($listview, $i, 0)


            //        if StringCompare($id, $aResult[2]) = 0 Then


            //            _GUICtrlListView_SetItemSelected($listview, $i, true, true)

            //            _GUICtrlListView_EnsureVisible($listview, $i)

            //            ExitLoop
            //        EndIf

            //    Next
            //EndIf

        }


        private void FiltersApplyButton_Click(object sender, RoutedEventArgs e)
        {
        }


        private void PreRunCreateButton_Click(object sender, RoutedEventArgs e)
        {
            var project = projects.Find(x => x.path == ((Project2)ProjectPathComboBox.SelectedItem).path);
            Log.Debug("Starting process \"" + System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\ExecutionGroupCreateUpdate.exe\" " + project.schema_name + " " + project.id + " 0");
            Process.Start(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\ExecutionGroupCreateUpdate.exe", project.schema_name + " " + project.id + " 0");
        }

        private void PreRunUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var execution_group = ((ExecutionGroup2)ExecutionGroupsListView.SelectedItem);
            var project = projects.Find(x => x.path == ((Project2)ProjectPathComboBox.SelectedItem).path);
            Log.Debug("Starting process \"" + System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\ExecutionGroupCreateUpdate.exe\" " + project.schema_name + " " + project.id + " " + execution_group.id);
            Process.Start(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\ExecutionGroupCreateUpdate.exe", project.schema_name + " " + project.id + " " + execution_group.id);
        }

        private void ExecutionGroupsListView_MouseDoubleClick(object sender, EventArgs e)
        {
            PreRunUpdateButton_Click(null, null);
        }


        private void PreRunArchiveButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void PreRunCopyButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void PreRunRefreshButton_Click(object sender, RoutedEventArgs e)
        {

            // apply the filters

            string filter_clause = "";

            //  if GUICtrlRead($machines_archived_checkbox) = $GUI_CHECKED Then

            //$filter_clause = $filter_clause & " AND eg.archived = 1"

            //  Else

            filter_clause = filter_clause + " AND eg.archived = 0";

            //  EndIf

            // refresh the Execution Groups listview
            TextBlock.Update(StatusBarText, "Please Wait. Getting execution groups ...");

            ExecutionGroupsListView.FromQuery<ExecutionGroup2>("SELECT eg.id, eg.name, i.name AS iteration, e.name AS test_environment, eg.start_date_time, eg.end_date_time, eg.result FROM execution_group eg, iteration i, environment e WHERE eg.iteration_id = i.id AND eg.environment_id = e.id" + filter_clause + " ORDER BY eg.name ASC LIMIT " + num_records_per_page + ";", project.schema_name);
//            ExecutionGroup2.QueryToListView(ExecutionGroupsListView, "SELECT eg.id, eg.name, i.name AS iteration, e.name AS test_environment, eg.start_date_time, eg.end_date_time, eg.result FROM execution_group eg, iteration i, environment e WHERE eg.iteration_id = i.id AND eg.environment_id = e.id" + filter_clause + " ORDER BY eg.name ASC LIMIT " + num_records_per_page + ";", project.schema_name);
            TextBlock.ClearNoError(StatusBarText);

        }

        private void PreRunDeployBuildButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void RunBuildDeployRunButton_Click(object sender, RoutedEventArgs e)
        {
            var execution_groups = ExecutionGroupsListView.SelectedItems;

            foreach (ExecutionGroup2 execution_group in execution_groups)
            {
                Log.Debug("Starting process \"" + System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\RunExecutionGroup.exe\" " + AppConfig.Get("DatabaseHostname") + " " + AppConfig.Get("DatabaseUsername") + " " + StringCipher.Decrypt(AppConfig.Get("DatabasePassword")) + " " + project.id + " " + execution_group.id + " \"" + execution_group.iteration + "\" \"" + execution_group.test_environment + "\" \"\" 3 \"\" \"\"");
                Process.Start(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\RunExecutionGroup.exe", AppConfig.Get("DatabaseHostname") + " " + AppConfig.Get("DatabaseUsername") + " " + StringCipher.Decrypt(AppConfig.Get("DatabasePassword")) + " " + project.id + " " + execution_group.id + " \"" + execution_group.iteration + "\" \"" + execution_group.test_environment + "\" \"\" 3 \"\" \"\"");
            }
        }

        private void RunDeployBuildRunButton_Click(object sender, RoutedEventArgs e)
        {
            var execution_groups = ExecutionGroupsListView.SelectedItems;

            foreach (ExecutionGroup2 execution_group in execution_groups)
            {
                Log.Debug("Starting process \"" + System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\RunExecutionGroup.exe\" " + AppConfig.Get("DatabaseHostname") + " " + AppConfig.Get("DatabaseUsername") + " " + StringCipher.Decrypt(AppConfig.Get("DatabasePassword")) + " " + project.id + " " + execution_group.id + " \"" + execution_group.iteration + "\" \"" + execution_group.test_environment + "\" \"\" 1 \"" + AppConfig.Get(AppConfig.Get("SourceControlProduct") + "Username") + "\" \"" + StringCipher.Decrypt(AppConfig.Get(AppConfig.Get("SourceControlProduct") + "Password")) + "\"");
                Process.Start(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\RunExecutionGroup.exe", AppConfig.Get("DatabaseHostname") + " " + AppConfig.Get("DatabaseUsername") + " " + StringCipher.Decrypt(AppConfig.Get("DatabasePassword")) + " " + project.id + " " + execution_group.id + " \"" + execution_group.iteration + "\" \"" + execution_group.test_environment + "\" \"\" 1 \"" + AppConfig.Get(AppConfig.Get("SourceControlProduct") + "Username") + "\" \"" + StringCipher.Decrypt(AppConfig.Get(AppConfig.Get("SourceControlProduct") + "Password")) + "\"");
            }
        }

        private void RunRunButton_Click(object sender, RoutedEventArgs e)
        {
            var execution_groups = ExecutionGroupsListView.SelectedItems;

            foreach (ExecutionGroup2 execution_group in execution_groups)
            {
                Log.Debug("Starting process \"" + System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\RunExecutionGroup.exe\" " + AppConfig.Get("DatabaseHostname") + " " + AppConfig.Get("DatabaseUsername") + " " + StringCipher.Decrypt(AppConfig.Get("DatabasePassword")) + " " + project.id + " " + execution_group.id + " \"" + execution_group.iteration + "\" \"" + execution_group.test_environment + "\" \"\" 0 \"\" \"\"");
                Process.Start(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\RunExecutionGroup.exe", AppConfig.Get("DatabaseHostname") + " " + AppConfig.Get("DatabaseUsername") + " " + StringCipher.Decrypt(AppConfig.Get("DatabasePassword")) + " " + project.id + " " + execution_group.id + " \"" + execution_group.iteration + "\" \"" + execution_group.test_environment + "\" \"\" 0 \"\" \"\"");
            }

            //ExecutionGroupsListView.Items.Refresh();


        }

        private void SharingPathChangeButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SharingPathDeleteButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void PostRunUploadResultsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ProjectPathComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //App.CurrentStatusBarText = StatusBarText;

            project = (Project2)(sender as ComboBox).SelectedItem;
            ProjectIDTextBox.Text = project.id.ToString();

            // Write this to the config as the last used project
            AppConfig.Put("ManageExecutionGroupsProjectPath", project.path);

            // apply the filters

            string filter_clause = "";

            //  if GUICtrlRead($machines_archived_checkbox) = $GUI_CHECKED Then

            //$filter_clause = $filter_clause & " AND eg.archived = 1"

            //  Else

            filter_clause = filter_clause + " AND eg.archived = 0";

            //  EndIf

            // refresh the Execution Groups listview
            TextBlock.Update(StatusBarText, "Please Wait. Getting execution groups ...");
            ExecutionGroupsListView.FromQuery<ExecutionGroup2>("SELECT eg.id, eg.name, i.name AS iteration, e.name AS test_environment, eg.start_date_time, eg.end_date_time, eg.result FROM execution_group eg, iteration i, environment e WHERE eg.iteration_id = i.id AND eg.environment_id = e.id" + filter_clause + " ORDER BY eg.name ASC LIMIT " + num_records_per_page + ";", project.schema_name);
//            ExecutionGroup2.QueryToListView(ExecutionGroupsListView, "SELECT eg.id, eg.name, i.name AS iteration, e.name AS test_environment, eg.start_date_time, eg.end_date_time, eg.result FROM execution_group eg, iteration i, environment e WHERE eg.iteration_id = i.id AND eg.environment_id = e.id" + filter_clause + " ORDER BY eg.name ASC LIMIT " + num_records_per_page + ";", project.schema_name);
            TextBlock.ClearNoError(StatusBarText);


        }





    }
}
