using System;
using System.Collections.Generic;
using System.Linq;
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
using MySql.Data.MySqlClient;
using System.Management.Automation;
using Microsoft.Win32;
using SharedProject.Models;
using SharedProject;

namespace CoPilot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ManageTestArtifacts : Window
    {
        private int num_records_per_page = 100;
        public List<Project2> projects { get; set; }

        public ManageTestArtifacts()
        {
            InitializeComponent();
            App.CurrentStatusBarText = StatusBarText;

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            TextBlock.Update(StatusBarText, "Please Wait. Getting projects ...");
            projects = Project2.Query("SELECT id, path, schema_name, external_app, external_id, external_name FROM project WHERE archived = 0 LIMIT " + num_records_per_page + ";", "control_automation_machine");
            TextBlock.ClearNoError(StatusBarText);

            // The following will update all controls bound to "projects" above (ie. ProjectPathComboBox)
            DataContext = this;

            ProjectIDTextBox.Text = projects[0].id.ToString();

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


        private void ExternalProjectUpdateButton_Click(object sender, RoutedEventArgs e)
        {
        }


        private void TestEnvironmentsCreateButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TestEnvironmentsUpdateButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TestEnvironmentsArchiveButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void IterationsCreateButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void IterationsUpdateButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void IterationsArchiveButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void IterationsFilterApplyButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TestScriptsImportButton_Click(object sender, RoutedEventArgs e)
        {
            var project = (Project2)ProjectPathComboBox.SelectedItem;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = project.path + "\\Executable";
            openFileDialog1.Filter = "Script files (*.java, *.cs, *.feature)|*.java;*.cs;*.feature";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == true)
            {
                var selectedFileNames = openFileDialog1.FileNames;

                foreach (var selectedFileName in selectedFileNames)
                {
                    var script_name = System.IO.Path.GetFileNameWithoutExtension(SharedProject.Directory.GetRightPartOfPath(selectedFileName, "Executable").Replace(System.IO.Path.DirectorySeparatorChar, '.'));
                    var dbConnect = new DBConnect(project.schema_name);
                    var scripts = dbConnect.Select<Script>("SELECT id FROM script WHERE name = '" + script_name + "';");

                    if (scripts.Count == 0)
                    {
                        dbConnect = new DBConnect(project.schema_name);
                        dbConnect.Insert("INSERT INTO script (name, archived) VALUES ('" + script_name + "', 0);");
                    }
                }
            }
        }

        private void TestScriptsArchiveButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TestScriptsEditButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TestScriptsFilterApplyButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ProjectPathComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.CurrentStatusBarText = StatusBarText;

            var project = (Project2)(sender as ComboBox).SelectedItem;
            ProjectIDTextBox.Text = project.id.ToString();
            ExternalProjectIDTextBox.Text = project.external_id.ToString();
            ExternalProjectNameTextBox.Text = project.external_name.ToString();

            // apply the filters

            string filter_clause = "";

            //  if GUICtrlRead($machines_archived_checkbox) = $GUI_CHECKED Then

            //$filter_clause = $filter_clause & " WHERE archived = 1"

            //  Else

            filter_clause = filter_clause + " WHERE archived = 0";

            //  EndIf


            // refresh the Test Environments listview
            TextBlock.Update(StatusBarText, "Please Wait. Getting test environments ...");
            TestEnvironment.QueryToListView(TestEnvironmentsListView, "SELECT id, name, external_id FROM environment" + filter_clause + " LIMIT " + num_records_per_page + ";", project.schema_name);
            TextBlock.ClearNoError(StatusBarText);

            // refresh the Iterations listview
            TextBlock.Update(StatusBarText, "Please Wait. Getting iterations ...");
            TestEnvironment.QueryToListView(IterationsListView, "SELECT id, name, external_id FROM iteration" + filter_clause + " LIMIT " + num_records_per_page + ";", project.schema_name);
            TextBlock.ClearNoError(StatusBarText);

            // refresh the Test Scripts listview
            TextBlock.Update(StatusBarText, "Please Wait. Getting test scripts ...");
            TestEnvironment.QueryToListView(TestScriptsListView, "SELECT s.id, s.name, s.description, s.comment, s.external_id FROM script s" + filter_clause + " LIMIT " + num_records_per_page + ";", project.schema_name);
            TextBlock.ClearNoError(StatusBarText);


        }





    }
}
