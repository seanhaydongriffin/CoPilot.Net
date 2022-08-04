using SharedProject;
using SharedProject.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Controls;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;

namespace ExecutionGroupCreateUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Main : Window
    {
        private int num_records_per_page = 100;
        public List<Project> project { get; set; }
        public List<ExecutionGroup> execution_group { get; set; }
        public List<Machine> machines { get; set; }

        public static ObservableCollection<ExecutionGroupScript> ScriptsListViewCollection { get; set; }

        private static ObservableCollection<ExecutionGroupScript> orig_ScriptsListViewCollection;
        private static ObservableCollection<ExecutionGroupScript> RemovedScriptsListViewCollection = new ObservableCollection<ExecutionGroupScript>();



        public Main()
        {
            InitializeComponent();
            Log.Reset();

            // catch unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            App.CurrentStatusBarText = StatusBarText;
            AppConfig.Open();

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            ProjectIDTextBox.Text = App.project_id;

            TextBlock.Update(StatusBarText, "Please Wait. Getting project ...");
            project = Project.Query("SELECT path, external_id, external_name FROM project WHERE id = " + App.project_id + ";", "control_automation_machine");
            TextBlock.ClearNoError(StatusBarText);

            ProjectPathTextBox.Text = project[0].path;
            ExternalProjectIDTextBox.Text = project[0].external_id;
            ExternalProjectPathTextBox.Text = project[0].external_name;

            ScriptsListViewCollection = new ObservableCollection<ExecutionGroupScript>();

            TextBlock.Update(StatusBarText, "Please Wait. Getting machines ...");
            var dbConnect = new DBConnect("control_automation_machine");
            machines = dbConnect.Select<Machine>("SELECT host_name FROM machine WHERE archived = 0 ORDER BY host_name;");
            TextBlock.ClearNoError(StatusBarText);

            if (App.execution_group_id.ToInt() == 0)
            {
                this.Title = "Execution Group - Create";
                //ExeGrpNameTextBox.IsReadOnly = false;
                ExternalPlanIDTextBox.Text = "0";
                ExternalExeRecRunIDTextBox.Text = "0";
            } else
            {
                this.Title = "Execution Group - Update";

                ExeGrpIDTextBox.Text = App.execution_group_id;

                TextBlock.Update(StatusBarText, "Please Wait. Getting execution group ...");
                execution_group = ExecutionGroup.Query("SELECT eg.name, eg.iteration_id, i.name AS iteration_name, eg.environment_id, e.name AS environment_name, eg.auto_stop_tests, eg.auto_send_results, eg.device_notifications, eg.external_plan_id, eg.external_plan_name, eg.external_exe_rec_run_id, eg.external_exe_rec_run_name FROM execution_group eg, iteration i, environment e WHERE eg.iteration_id = i.id AND eg.environment_id = e.id AND eg.id = " + App.execution_group_id + ";", App.schema_name);
                TextBlock.ClearNoError(StatusBarText);

                ExeGrpNameTextBox.Text = execution_group[0].name;
                IterationIDTextBox.Text = execution_group[0].iteration_id.ToString();
                IterationNameTextBox.Text = execution_group[0].iteration_name;
                TestEnvIDTextBox.Text = execution_group[0].environment_id.ToString();
                TestEnvNameTextBox.Text = execution_group[0].environment_name;
                ExternalPlanIDTextBox.Text = execution_group[0].external_plan_id.ToString();
                ExternalPlanNameTextBox.Text = execution_group[0].external_plan_name;
                ExternalExeRecRunIDTextBox.Text = execution_group[0].external_exe_rec_run_id.ToString();
                ExternalExeRecRunNameTextBox.Text = execution_group[0].external_exe_rec_run_name;
                AutoStopTestsCheckBox.IsChecked = execution_group[0].auto_stop_tests == 1 ? true : false;
                AutoSendResultsCheckBox.IsChecked = execution_group[0].auto_send_results == 1 ? true : false;
                DeviceNotificationsCheckBox.IsChecked = execution_group[0].device_notifications == 1 ? true : false;

                ScriptsListView_Reload(App.execution_group_id, App.schema_name);
            }

            // below is important to activate all the bindings from the XAML to the data set above
            this.DataContext = this;
        }


        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)

                Close();
        }


        private void ScriptsListView_Reload(string execution_group_id, string db)
        {
            TextBlock.Update(StatusBarText, "Please Wait. Getting execution group scripts ...");
            var dbConnect = new DBConnect(db);
//            Main.ScriptsListViewCollection = dbConnect.SelectToObservableCollection<ExecutionGroupScript>("SELECT egs.id, m.host_name, egs.script_id, s.name AS script_name, egs.selector, egs.post_run_delay, egs.state, egs.excluded, egs.end_date_time, egs.em_comment, egs.shared_folder_1, egs.order_id FROM execution_group_script egs, script s, control_automation_machine.machine m WHERE egs.script_id = s.id AND egs.machine_id = m.id AND egs.execution_group_id = " + execution_group_id + " ORDER BY egs.order_id ASC;");
            Main.ScriptsListViewCollection = dbConnect.SelectToObservableCollection<ExecutionGroupScript>("SELECT egs.id, m.host_name, egs.script_id, s.name AS script_name, egs.selector, egs.post_run_delay, egs.state, egs.excluded, egs.end_date_time, egs.em_comment, egs.shared_folder_1, egs.order_id FROM execution_group_script egs, script s, control_automation_machine.machine m WHERE egs.script_id = s.id AND egs.machine_id = m.id AND egs.execution_group_id = ?execution_group_id ORDER BY egs.order_id ASC;", new Hashtable {
                { "?execution_group_id",                 execution_group_id }
            });
            TextBlock.ClearNoError(StatusBarText);
            //lv.ItemsSource = null;
            //lv.ItemsSource = execution_schedule_line;

            foreach (var eachItem in ScriptsListViewCollection)
            {
                if (eachItem.excluded == false)

                    eachItem.excluded = null;
            }

            // deep copy of ScriptsListViewCollection once populated from the database
            orig_ScriptsListViewCollection = new ObservableCollection<ExecutionGroupScript>(Main.ScriptsListViewCollection);

            ScriptsListView.SelectedIndex = 0;
        }



        private void IterationButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SelectIteration(this);
            dialog.Owner = this;
            bool? result = dialog.ShowDialog();
        }

        private void TestEnvButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SelectEnvironment(this);
            dialog.Owner = this;
            bool? result = dialog.ShowDialog();
        }

        private void ExternalPlanButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SelectExternalPlan(this);
            dialog.Owner = this;
            bool? result = dialog.ShowDialog();
        }

        private void ExternalExeRecRunButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SelectExternalExeRecRun(this);
            dialog.Owner = this;
            bool? result = dialog.ShowDialog();
        }


        private void SaveAndCloseButton_Click(object sender, RoutedEventArgs e)
        {
            var mandatory_fields = "";

            if (ExeGrpNameTextBox.Text.Length == 0)

                mandatory_fields = mandatory_fields + "\nExe Grp Name";

            if (IterationIDTextBox.Text.Length == 0)

                mandatory_fields = mandatory_fields + "\nIteration";

            if (TestEnvIDTextBox.Text.Length == 0)

                mandatory_fields = mandatory_fields + "\nEnvironment";

            if (mandatory_fields.Length > 0)
            {
                System.Windows.MessageBox.Show("Cannot Save.  The following mandatory fields are not set:" + mandatory_fields, "CoPilot", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ScriptsListViewCollection == null || ScriptsListViewCollection.Count < 1)
            {
                System.Windows.MessageBox.Show("Cannot Save.  There are no scripts in the group.", "CoPilot", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // scan the host names and warn if a host name is blank

            foreach (ExecutionGroupScript eachItem in ScriptsListViewCollection)
            {
                if (eachItem.host_name.Length == 0)
                {
                    System.Windows.MessageBox.Show("Cannot Save.  One or more scripts do not have a Host Name.\nAll scripts must have a host name assigned.", "CoPilot", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }



            var dbConnect = new DBConnect(App.schema_name);

            if (this.Title.Equals("Execution Group - Create"))
            {
                // Create the new execution group

                TextBlock.Update(StatusBarText, "Please Wait. Creating execution group ...");
//                var execution_group_id = dbConnect.Insert("INSERT INTO execution_group (name, iteration_id, environment_id, scheduled, auto_stop_tests, auto_send_results, device_notifications, number_of_repeats, external_plan_id, external_plan_name, external_exe_rec_run_id, external_exe_rec_run_name, archived) VALUES ('" + ExeGrpNameTextBox.Text + "'," + IterationIDTextBox.Text + "," + TestEnvIDTextBox.Text + ",''," + (AutoStopTestsCheckBox.IsChecked == true ? 1 : 0) + "," + (AutoSendResultsCheckBox.IsChecked == true ? 1 : 0) + "," + (DeviceNotificationsCheckBox.IsChecked == true ? 1 : 0) + ",1," + ExternalPlanIDTextBox.Text + ",'" + ExternalPlanNameTextBox.Text + "'," + ExternalExeRecRunIDTextBox.Text + ",'" + ExternalExeRecRunNameTextBox.Text + "',0);");
                var execution_group_id = dbConnect.Insert("INSERT INTO execution_group (name, iteration_id, environment_id, scheduled, auto_stop_tests, auto_send_results, device_notifications, number_of_repeats, external_plan_id, external_plan_name, external_exe_rec_run_id, external_exe_rec_run_name, archived) VALUES (?name, ?iteration_id, ?environment_id, ?scheduled, ?auto_stop_tests, ?auto_send_results, ?device_notifications, ?number_of_repeats, ?external_plan_id, ?external_plan_name, ?external_exe_rec_run_id, ?external_exe_rec_run_name, ?archived);", new Hashtable {
                    { "?name",                          ExeGrpNameTextBox.Text },
                    { "?iteration_id",                  IterationIDTextBox.Text },
                    { "?environment_id",                TestEnvIDTextBox.Text },
                    { "?scheduled",                     "" },
                    { "?auto_stop_tests",               (AutoStopTestsCheckBox.IsChecked == true ? 1 : 0) },
                    { "?auto_send_results",             (AutoSendResultsCheckBox.IsChecked == true ? 1 : 0) },
                    { "?device_notifications",          (DeviceNotificationsCheckBox.IsChecked == true ? 1 : 0) },
                    { "?number_of_repeats",             1 },
                    { "?external_plan_id",              ExternalPlanIDTextBox.Text },
                    { "?external_plan_name",            ExternalPlanNameTextBox.Text },
                    { "?external_exe_rec_run_id",       ExternalExeRecRunIDTextBox.Text },
                    { "?external_exe_rec_run_name",     ExternalExeRecRunNameTextBox.Text },
                    { "?archived",                      0 },
                });
                TextBlock.ClearNoError(StatusBarText);

                // Create the schedule

                // Create the new execution group scripts

                var script_index = 0;

                foreach (ExecutionGroupScript eachItem in ScriptsListViewCollection)
                {
                    script_index++;

                    var dbConnect2 = new DBConnect("control_automation_machine");
                    var machines = dbConnect2.Select<Machine>("SELECT id FROM machine WHERE host_name = '" + eachItem.host_name + "';");

                    TextBlock.Update(StatusBarText, "Please Wait. Creating execution group ...");
//                    dbConnect.Insert("INSERT INTO execution_group_script (execution_group_id, order_id, script_id, machine_id, selector, post_run_delay, excluded, em_comment, shared_folder_1) VALUES (" + execution_group_id + ", " + script_index + ", " + eachItem.script_id + ", " + machines[0].id + ", '" + eachItem.selector + "', '" + eachItem.post_run_delay + "', " + (eachItem.excluded == true ? 1 : 0) + ", '', '" + eachItem.shared_folder_1 + "');");
                    dbConnect.Insert("INSERT INTO execution_group_script (execution_group_id, order_id, script_id, machine_id, selector, post_run_delay, excluded, em_comment, shared_folder_1) VALUES (?execution_group_id, ?order_id, ?script_id, ?machine_id, ?selector, ?post_run_delay, ?excluded, ?em_comment, ?shared_folder_1);", new Hashtable {
                        { "?execution_group_id",    execution_group_id },
                        { "?order_id",              script_index },
                        { "?script_id",             eachItem.script_id },
                        { "?machine_id",            machines[0].id },
                        { "?selector",              eachItem.selector },
                        { "?post_run_delay",        eachItem.post_run_delay },
                        { "?excluded",              (eachItem.excluded == true ? 1 : 0) },
                        { "?em_comment",            "" },
                        { "?shared_folder_1",       eachItem.shared_folder_1 }
                    });
                    TextBlock.ClearNoError(StatusBarText);

                }



            }


            if (this.Title.Equals("Execution Group - Update"))
            {

                // update the execution group in the database

                TextBlock.Update(StatusBarText, "Please Wait. Updating execution group script id " + App.execution_group_id + " ...");
//                dbConnect.Update("UPDATE execution_group SET name = '" + ExeGrpNameTextBox.Text + "', iteration_id = " + IterationIDTextBox.Text + ", environment_id = " + TestEnvIDTextBox.Text + ", scheduled = '', auto_stop_tests = " + (AutoStopTestsCheckBox.IsChecked == true ? 1 : 0) + ", auto_send_results = " + (AutoSendResultsCheckBox.IsChecked == true ? 1 : 0) + ", device_notifications = " + (DeviceNotificationsCheckBox.IsChecked == true ? 1 : 0) + ", number_of_repeats = 1, external_plan_id = " + ExternalPlanIDTextBox.Text + ", external_plan_name = '" + ExternalPlanNameTextBox.Text + "', external_exe_rec_run_id = " + ExternalExeRecRunIDTextBox.Text + ", external_exe_rec_run_name = '" + ExternalExeRecRunNameTextBox.Text + "' WHERE id = " + App.execution_group_id + ";");
                dbConnect.Update("UPDATE execution_group SET name = ?name, iteration_id = ?iteration_id, environment_id = ?environment_id, scheduled = ?scheduled, auto_stop_tests = ?auto_stop_tests, auto_send_results = ?auto_send_results, device_notifications = ?device_notifications, number_of_repeats = ?number_of_repeats, external_plan_id = ?external_plan_id, external_plan_name = ?external_plan_name, external_exe_rec_run_id = ?external_exe_rec_run_id, external_exe_rec_run_name = ?external_exe_rec_run_name WHERE id = ?id;", new Hashtable {
                    { "?name",                          ExeGrpNameTextBox.Text },
                    { "?iteration_id",                  IterationIDTextBox.Text },
                    { "?environment_id",                TestEnvIDTextBox.Text },
                    { "?scheduled",                     "" },
                    { "?auto_stop_tests",               (AutoStopTestsCheckBox.IsChecked == true ? 1 : 0) },
                    { "?auto_send_results",             (AutoSendResultsCheckBox.IsChecked == true ? 1 : 0) },
                    { "?device_notifications",          (DeviceNotificationsCheckBox.IsChecked == true ? 1 : 0) },
                    { "?number_of_repeats",             1 },
                    { "?external_plan_id",              ExternalPlanIDTextBox.Text },
                    { "?external_plan_name",            ExternalPlanNameTextBox.Text },
                    { "?external_exe_rec_run_id",       ExternalExeRecRunIDTextBox.Text },
                    { "?external_exe_rec_run_name",     ExternalExeRecRunNameTextBox.Text },
                    { "?id",                            App.execution_group_id }      
                });
                TextBlock.ClearNoError(StatusBarText);

                // For each item that was removed from the execution group perform the relevant database delete

                foreach (ExecutionGroupScript eachItem in RemovedScriptsListViewCollection)
                {
                    TextBlock.Update(StatusBarText, "Please Wait. Deleting execution group script id " + eachItem.id + " ...");
//                    dbConnect.Delete("DELETE FROM execution_group_script WHERE id = " + eachItem.id + " AND order_id = " + eachItem.order_id + ";");
                    dbConnect.Delete("DELETE FROM execution_group_script WHERE id = ?id AND order_id = ?order_id;", new Hashtable {
                        { "?id",                            eachItem.id },
                        { "?order_id",                      eachItem.order_id }
                    });
                    TextBlock.ClearNoError(StatusBarText);
                }


                // loop through the current ScriptsListViewCollection and compare the position of each member
                //  to the member of the same position originally.
                //  If this is different then the member at this position (Order ID) has changed and a MySQL Update for this
                //  of the "Order ID" for this member is required.


                for (int i = 0; i < Main.ScriptsListViewCollection.Count; i++)
                {
                    // if a ScriptsListView item is not matching the original item from the database, then a change has occurred and the database must be updated

                    // if a script has been updated or it's new / added (it's order id has become -1)
                    // or it's out of sequence (it's order id does not match the expected order id), then update it in the database

                    if (i > (orig_ScriptsListViewCollection.Count - 1) || Main.ScriptsListViewCollection[i].id != orig_ScriptsListViewCollection[i].id || Main.ScriptsListViewCollection[i].order_id == -1)
                    {

                        if (Main.ScriptsListViewCollection[i].host_name == null || Main.ScriptsListViewCollection[i].host_name.Equals(""))
                        {
                            System.Windows.MessageBox.Show("Save Aborted. Script " + ScriptsListViewCollection[i].script_name + " requires a host name.");
                            return;
                        }

                        TextBlock.Update(StatusBarText, "Please Wait. Getting machine id ...");
                        dbConnect = new DBConnect("control_automation_machine");
//                        var machines = dbConnect.Select<Machine>("SELECT id FROM machine WHERE host_name = '" + Main.ScriptsListViewCollection[i].host_name + "'");
                        var machines = dbConnect.Select<Machine>("SELECT id FROM machine WHERE host_name = ?host_name", new Hashtable {
                            { "?host_name",                 Main.ScriptsListViewCollection[i].host_name }
                        });

                        TextBlock.ClearNoError(StatusBarText);

                        dbConnect = new DBConnect(App.schema_name);

                        // if a new script added (execution group script ID not yet allocated)
                        if (Main.ScriptsListViewCollection[i].id == null)
                        {
                            // insert a new script (to the database)
                            TextBlock.Update(StatusBarText, "Please Wait. inserting script with id " + Main.ScriptsListViewCollection[i].script_id + " ...");
//                            dbConnect.Insert("INSERT INTO execution_group_script (execution_group_id, order_id, script_id, machine_id, selector, post_run_delay, state, excluded, em_comment, shared_folder_1) VALUES (" + App.execution_group_id + ", " + (i + 1) + ", " + Main.ScriptsListViewCollection[i].script_id + ", " + machines[0].id + ", '" + Main.ScriptsListViewCollection[i].selector + "', '" + Main.ScriptsListViewCollection[i].post_run_delay + "', '', '" + (Main.ScriptsListViewCollection[i].excluded == true ? 1 : 0) + "', '" + Main.ScriptsListViewCollection[i].em_comment + "', '" + Main.ScriptsListViewCollection[i].shared_folder_1 + "');");
                            dbConnect.Insert("INSERT INTO execution_group_script (execution_group_id, order_id, script_id, machine_id, selector, post_run_delay, state, excluded, em_comment, shared_folder_1) VALUES (" + App.execution_group_id + ", " + (i + 1) + ", " + Main.ScriptsListViewCollection[i].script_id + ", " + machines[0].id + ", '" + Main.ScriptsListViewCollection[i].selector + "', '" + Main.ScriptsListViewCollection[i].post_run_delay + "', '', '" + (Main.ScriptsListViewCollection[i].excluded == true ? 1 : 0) + "', '" + Main.ScriptsListViewCollection[i].em_comment + "', '" + Main.ScriptsListViewCollection[i].shared_folder_1 + "');", new Hashtable {
                                { "?execution_group_id",    App.execution_group_id },
                                { "?order_id",              (i + 1) },
                                { "?script_id",             Main.ScriptsListViewCollection[i].script_id },
                                { "?machine_id",            machines[0].id },
                                { "?selector",              Main.ScriptsListViewCollection[i].selector },
                                { "?post_run_delay",        Main.ScriptsListViewCollection[i].post_run_delay },
                                { "?state",                 "" },
                                { "?excluded",              (Main.ScriptsListViewCollection[i].excluded == true ? 1 : 0) },
                                { "?em_comment",            Main.ScriptsListViewCollection[i].em_comment },
                                { "?shared_folder_1",       Main.ScriptsListViewCollection[i].shared_folder_1 }
                            });

                            TextBlock.ClearNoError(StatusBarText);
                        }
                        else
                        {
                            // update to an existing script (from the database)
                            TextBlock.Update(StatusBarText, "Please Wait. updating execution group script id " + Main.ScriptsListViewCollection[i].id + " ...");
//                            dbConnect.Update("UPDATE execution_group_script SET order_id = " + (i + 1) + ", machine_id = " + machines[0].id + ", selector = '" + Main.ScriptsListViewCollection[i].selector + "', post_run_delay = '" + Main.ScriptsListViewCollection[i].post_run_delay + "', state = '" + Main.ScriptsListViewCollection[i].state + "', excluded = " + (Main.ScriptsListViewCollection[i].excluded == true ? 1 : 0) + ", em_comment = '" + Main.ScriptsListViewCollection[i].em_comment + "', shared_folder_1 = '" + Main.ScriptsListViewCollection[i].shared_folder_1 + "' WHERE id = " + Main.ScriptsListViewCollection[i].id + ";");
                            dbConnect.Update("UPDATE execution_group_script SET order_id = ?order_id, machine_id = ?machine_id, selector = ?selector, post_run_delay = ?post_run_delay, state = ?state, excluded = ?excluded, em_comment = ?em_comment, shared_folder_1 = ?shared_folder_1 WHERE id = ?id;", new Hashtable {
                                { "?order_id",              (i + 1) },
                                { "?machine_id",            machines[0].id },
                                { "?selector",              Main.ScriptsListViewCollection[i].selector },
                                { "?post_run_delay",        Main.ScriptsListViewCollection[i].post_run_delay },
                                { "?state",                 Main.ScriptsListViewCollection[i].state },
                                { "?excluded",              (Main.ScriptsListViewCollection[i].excluded == true ? 1 : 0) },
                                { "?em_comment",            Main.ScriptsListViewCollection[i].em_comment },
                                { "?shared_folder_1",       Main.ScriptsListViewCollection[i].shared_folder_1 },
                                { "?id",                    Main.ScriptsListViewCollection[i].id }
                            });

                            TextBlock.ClearNoError(StatusBarText);
                        }
                    }
                }
            }

            Close();
            


        }


        private void PreRunDelayDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var items = ScriptsListView.SelectedItems;

            if (items.Count > 0)
            {
                foreach (var item in items)
                {
                    ((ExecutionGroupScript)item).post_run_delay = "";
                    ((ExecutionGroupScript)item).order_id = -1;
                }

                ScriptsListView.Items.Refresh();
                ScriptsListView.Focus();
            }
        }

        private void ExcludeButton_Click(object sender, RoutedEventArgs e)
        {
            var items = ScriptsListView.SelectedItems;

            if (items.Count > 0)
            {
                foreach (var item in items)
                {
                    if (((ExecutionGroupScript)item).excluded == null || ((ExecutionGroupScript)item).excluded == false)

                        ((ExecutionGroupScript)item).excluded = true;
                    else

                        ((ExecutionGroupScript)item).excluded = null;

                    ((ExecutionGroupScript)item).order_id = -1;
                }

                ScriptsListView.Items.Refresh();
                ScriptsListView.Focus();
            }
        }

        private void EMCommentButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ScriptsAddButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ((ExecutionGroupScript)ScriptsListView.SelectedItem);
            string script_name = "";

            if (item != null)

                script_name = item.script_name.Split('.').Last();

            var dialog = new AddScript(script_name);
            bool? result = dialog.ShowDialog();

            //if (result == true)

            //    refresh_machines();

        }

        private void ScriptsRemoveButton_Click(object sender, RoutedEventArgs e)
        {

            // We can't remove items directly from the ScriptsListView as it's bound to data (ScriptsListViewCollection).
            // We also can't remove items from the bound data (ScriptsListViewCollection) while we enumerate it.
            // The solution is to Clone (deep copy) the selected items to another list and enumerate that list, removing each item
            //  from the bound data (ScriptsListViewCollection).
            var items = ScriptsListView.SelectedItems.Clone();

            foreach (ExecutionGroupScript eachItem in items)
            {
                // if the item is one that exists in the database (has an execution group script ID)
                if (eachItem.id != null)

                    // Keep a copy of items removed, so later we can DELETE them in the database when saving
                    RemovedScriptsListViewCollection.Add(eachItem);

                ScriptsListViewCollection.Remove(eachItem);
            }

        }

        private void ScriptsUpButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ScriptsDownButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ScriptsScriptLogButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ScriptsVideoLogButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ScriptsVideoLogDownloadButton_Click(object sender, RoutedEventArgs e)
        {

            var items = ScriptsListView.SelectedItems;

            if (items.Count > 0)
            {
                foreach (var item in items)
                {
                    if (((ExecutionGroupScript)item).host_name.Length > 0)
                    {
                        var ProjectPathWithoutDrive = project[0].path.Replace("R:\\", "");
                        var ScriptShortName = ((ExecutionGroupScript)item).script_name.Substring(((ExecutionGroupScript)item).script_name.LastIndexOf('.') + 1);
                        var DateNow = DateTime.Now.ToString("MMM dd");

                        Log.Debug("Deleting file log.mp4 from R:");
                        File.delete("R:", "log.mp4");

                        Log.Debug("Deleting file " + ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4 from R:");
                        File.delete("R:", ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");

                        Log.Debug("Robocopy file log.mp4 from \\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name + " to R:");
                        File.RoboCopy2("\\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name, "R:", "log.mp4");

                        Log.Debug("Deleting file log 1.mp4 from R:");
                        File.delete("R:", "log 1.mp4");

                        Log.Debug("Deleting file " + ScriptShortName + " log 1 " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4 from R:");
                        File.delete("R:", ScriptShortName + " log 1 " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");

                        Log.Debug("Robocopy file log 1.mp4 from \\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name + " to R:");
                        File.RoboCopy2("\\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name, "R:", "log 1.mp4");

                        Log.Debug("Deleting file perf chart.png from R:");
                        File.delete("R:", "perf chart.png");

                        Log.Debug("Deleting file " + ScriptShortName + " perf chart " + DateNow + " " + TestEnvNameTextBox.Text + ".png from R:");
                        File.delete("R:", ScriptShortName + " perf chart " + DateNow + " " + TestEnvNameTextBox.Text + ".png");

                        Log.Debug("Robocopy file perf chart.png from \\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name + " to R:");
                        File.RoboCopy2("\\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name, "R:", "perf chart.png");

                        if (File.Exists("R:\\log.mp4"))
                        {
                            Log.Debug("Moving R:\\log.mp4 to R:\\" + ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");
                            File.Move("R:\\log.mp4", "R:\\" + ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");
                        }

                        if (File.Exists("R:\\log 1.mp4"))
                        {
                            Log.Debug("Moving R:\\log 1.mp4 to R:\\" + ScriptShortName + " log 1 " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");
                            File.Move("R:\\log 1.mp4", "R:\\" + ScriptShortName + " log 1 " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");
                        }

                        if (File.Exists("R:\\perf chart.png"))
                        {
                            Log.Debug("Moving R:\\perf chart.png to R:\\" + ScriptShortName + " perf chart " + DateNow + " " + TestEnvNameTextBox.Text + ".png");
                            File.Move("R:\\perf chart.png", "R:\\" + ScriptShortName + " perf chart " + DateNow + " " + TestEnvNameTextBox.Text + ".png");
                        }
                    }
                }

            }
        }

        private void ScriptsRDPVideoLogUploadButton_Click(object sender, RoutedEventArgs e)
        {


            var items = ScriptsListView.SelectedItems;

            if (items.Count > 0)
            {
                foreach (var item in items)
                {
                    if (((ExecutionGroupScript)item).host_name.Length > 0)
                    {
                        var ProjectPathWithoutDrive = project[0].path.Replace("R:\\", "");
                        var ScriptShortName = ((ExecutionGroupScript)item).script_name.Substring(((ExecutionGroupScript)item).script_name.LastIndexOf('.') + 1);
                        var DateNow = DateTime.Now.ToString("MMM dd");

                        Log.Debug("Deleting file log.mp4 from \\\\tsclient\\R");
                        File.delete("\\\\tsclient\\R", "log.mp4");

                        Log.Debug("Deleting file " + ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4 from \\\\tsclient\\R");
                        File.delete("\\\\tsclient\\R", ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");

                        Log.Debug("Robocopy file log.mp4 from \\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name + " to \\\\tsclient\\R");
                        File.RoboCopy2("\\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name, "\\\\tsclient\\R", "log.mp4");

                        Log.Debug("Deleting file log 1.mp4 from \\\\tsclient\\R");
                        File.delete("\\\\tsclient\\R", "log 1.mp4");

                        Log.Debug("Deleting file " + ScriptShortName + " log 1 " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4 from \\\\tsclient\\R");
                        File.delete("\\\\tsclient\\R", ScriptShortName + " log 1 " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");

                        Log.Debug("Robocopy file log 1.mp4 from \\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name + " to \\\\tsclient\\R");
                        File.RoboCopy2("\\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name, "\\\\tsclient\\R", "log 1.mp4");

                        //File.delete("R:", "perf chart.png");
                        //File.delete("R:", ScriptShortName + " perf chart " + DateNow + " " + TestEnvNameTextBox.Text + ".png");
                        //File.RoboCopy2("\\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name, "R:", "perf chart.png");

                        Log.Debug("Deleting file log.xml from \\\\tsclient\\R");
                        File.delete("\\\\tsclient\\R", "log.xml");

                        Log.Debug("Deleting file " + ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".xml from \\\\tsclient\\R");
                        File.delete("\\\\tsclient\\R", ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".xml");

                        Log.Debug("Robocopy file log.xml from \\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name + " to \\\\tsclient\\R");
                        File.RoboCopy2("\\\\" + ((ExecutionGroupScript)item).host_name + "\\Automation\\" + ProjectPathWithoutDrive + "_local_logs\\" + ((ExecutionGroupScript)item).script_name, "\\\\tsclient\\R", "log.xml");

                        if (File.Exists("\\\\tsclient\\R\\log.mp4"))
                        {
                            Log.Debug("Moving \\\\tsclient\\R\\log.mp4 to \\\\tsclient\\R\\" + ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");
                            File.Move("\\\\tsclient\\R\\log.mp4", "\\\\tsclient\\R\\" + ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");
                        }

                        if (File.Exists("\\\\tsclient\\R\\log 1.mp4"))
                        {
                            Log.Debug("Moving \\\\tsclient\\R\\log 1.mp4 to \\\\tsclient\\R\\" + ScriptShortName + " log 1 " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");
                            File.Move("\\\\tsclient\\R\\log 1.mp4", "\\\\tsclient\\R\\" + ScriptShortName + " log 1 " + DateNow + " " + TestEnvNameTextBox.Text + ".mp4");
                        }

                        //if (File.Exists("R:\\perf chart.png"))

                        //  File.Move("R:\\perf chart.png", "R:\\" + ScriptShortName + " perf chart " + DateNow + " " + TestEnvNameTextBox.Text + ".png");

                        if (File.Exists("\\\\tsclient\\R\\log.xml"))
                        {
                            Log.Debug("Moving \\\\tsclient\\R\\log.xml to \\\\tsclient\\R\\" + ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".xml");
                            File.Move("\\\\tsclient\\R\\log.xml", "\\\\tsclient\\R\\" + ScriptShortName + " log " + DateNow + " " + TestEnvNameTextBox.Text + ".xml");
                        }
                    }
                }

            }


        }

        private void ScriptsArchivedLogsButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ScriptsSendResultsToExternalAppButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private bool HostNameComboBox_Handled = false;

        private void HostNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HostNameComboBox_Handled = true;
            HostNameComboBox_Handler(sender as ComboBox);
        }

        private void HostNameComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (!HostNameComboBox_Handled) HostNameComboBox_Handler(sender as ComboBox);
            HostNameComboBox_Handled = false;
        }

        private void HostNameComboBox_Handler(ComboBox sender)
        {
            var machine = (Machine)sender.SelectedItem;
            var items = ScriptsListView.SelectedItems;

            foreach (var item in items)
            {
                ((ExecutionGroupScript)item).host_name = machine.host_name;
                ((ExecutionGroupScript)item).order_id = -1;
            }

            sender.IsDropDownOpen = false;
            ScriptsListView.Items.Refresh();
            ScriptsListView.Focus();
        }

        private bool SelectorsComboBox_Handled = false;

        private void SelectorsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectorsComboBox_Handled = true;
            SelectorsComboBox_Handler(sender as ComboBox);
        }

        private void SelectorsComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (!SelectorsComboBox_Handled) SelectorsComboBox_Handler(sender as ComboBox);
            SelectorsComboBox_Handled = false;
        }

        private void SelectorsComboBox_Handler(ComboBox sender)
        {
            var selector = sender.SelectedValue;
            var items = ScriptsListView.SelectedItems;

            foreach (var item in items)
            {
                if (selector.Equals("Parallel"))
                {
                    if (items[0] == item)

                        ((ExecutionGroupScript)item).selector = "PS";
                    else

                        if (items[items.Count-1] == item)

                            ((ExecutionGroupScript)item).selector = "PE";
                        else

                            ((ExecutionGroupScript)item).selector = "PC";
                }

                if (selector.Equals("Sequential"))

                    ((ExecutionGroupScript)item).selector = "S";

                ((ExecutionGroupScript)item).order_id = -1;
            }

            sender.IsDropDownOpen = false;
            ScriptsListView.Items.Refresh();
            ScriptsListView.Focus();
        }

        private void ScriptsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var script = (ExecutionGroupScript)(sender as ListView).SelectedItem;

            if (script != null)
            {
                HostNameComboBox.SelectionChanged -= new SelectionChangedEventHandler(HostNameComboBox_SelectionChanged);
                HostNameComboBox.Text = script.host_name;
                HostNameComboBox.SelectionChanged += new SelectionChangedEventHandler(HostNameComboBox_SelectionChanged);

                SelectorsComboBox.SelectionChanged -= new SelectionChangedEventHandler(SelectorsComboBox_SelectionChanged);

                switch (script.selector)
                {
                    case "S":

                        SelectorsComboBox.Text = "Sequential";
                        break;

                    case "PS":
                    case "PC":
                    case "PE":

                        SelectorsComboBox.Text = "Parallel";
                        break;
                }

                SelectorsComboBox.SelectionChanged += new SelectionChangedEventHandler(SelectorsComboBox_SelectionChanged);

                if (script.shared_folder_1 != null && script.shared_folder_1.Length > 0)
                {
                    SharedFolder1PathTextBox.TextChanged -= new TextChangedEventHandler(SharedFolder1PathTextBox_TextChanged);
                    SharedFolder1PathTextBox.Text = script.shared_folder_1;
                    SharedFolder1PathTextBox.TextChanged += new TextChangedEventHandler(SharedFolder1PathTextBox_TextChanged);
                }
            }
        }

        private void PreRunDelayTimePicker_ValueChanged(object sender, RoutedEventArgs e)
        {
            var time = ((TimePicker)sender).Value;
            var items = ScriptsListView.SelectedItems;

            if (items.Count > 0)
            {

                foreach (var item in items)
                {
                    ((ExecutionGroupScript)item).post_run_delay = ((DateTime)time).ToString("mm:ss");
                    ((ExecutionGroupScript)item).order_id = -1;
                }

                ScriptsListView.Items.Refresh();
                ScriptsListView.Focus();
            }
        }

        private void SharedFolder1PathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var items = ScriptsListView.SelectedItems;

            if (items.Count > 0)
            {

                foreach (var item in items)
                {
                    ((ExecutionGroupScript)item).shared_folder_1 = SharedFolder1PathTextBox.Text;
                    ((ExecutionGroupScript)item).order_id = -1;
                }

                ScriptsListView.Items.Refresh();
                //ScriptsListView.Focus();
            }

        }

        private void SharedFolder1PathTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SharedFolder1PathTextBox_TextChanged(sender, null);
            }
        }


        private void SharedFolder1DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var items = ScriptsListView.SelectedItems;

            if (items.Count > 0)
            {
                foreach (var item in items)
                {
                    ((ExecutionGroupScript)item).shared_folder_1 = "";
                    ((ExecutionGroupScript)item).order_id = -1;
                }

                ScriptsListView.Items.Refresh();
                ScriptsListView.Focus();
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Debug(e.ExceptionObject.ToString());
        }


    }
}
