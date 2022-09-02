using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Management.Automation;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;
using System.Threading;
using SharedProject;
using SharedProject.Models;
using System.Net;

namespace CoPilot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static System.Windows.Controls.TextBlock statusbar;
        public static System.Threading.Timer StatusBarClearTextTimer;

        public MainWindow()
        {
            InitializeComponent();
            Log.Reset();

            // catch unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            App.CurrentStatusBarText = StatusBarText;
            statusbar = StatusBarText;
            AppConfig.Open();

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            // MySQL

            try
            {
                refresh_machines();
            } catch (Exception e)
            {
                int i = 0;
            }


            // Create a timer that clears the status bar, but don't start it
            StatusBarClearTextTimer = new System.Threading.Timer(StatusBarClearText, null, 0, Timeout.Infinite);


        }

        private void StatusBarClearText(object state)
        {
            StatusBarSetText("");

            // stop the timer that clears the status bar
            StatusBarClearTextTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void refresh_machines()
        {
            // apply the filters

            string filter_clause = "";

            //  if GUICtrlRead($machines_archived_checkbox) = $GUI_CHECKED Then

            //$filter_clause = $filter_clause & " WHERE archived = 1"

            //  Else

            filter_clause = filter_clause + " WHERE archived = 0";

            //  EndIf


            //            ListView_Refresh_with_MySql_query_and_set_item_selected_by_id($main_gui, $machines_listview, "SELECT id, name, host_type, host_name, '', comment FROM machine" & $filter_clause & " ORDER BY name, host_type, host_name", 6, "machineid", $machines_status_input, "control_automation_machine")
            ListView_Refresh_with_MySql_query_and_set_item_selected_by_id(MachinesListView, "SELECT id, name, host_type, host_name, '' as blank, comment FROM machine" + filter_clause + " ORDER BY name, host_type, host_name", "control_automation_machine");

        }

        private void ListView_Refresh_with_MySql_query_and_set_item_selected_by_id(ListView lv, string mysql_query, string db)
        {
            TextBlock.Update(StatusBarText, "Please Wait. Getting machines ...");
            var dbConnect = new DBConnect(db);
            var execution_schedule_line = dbConnect.Select<Machine2>(mysql_query);
            TextBlock.ClearNoError(StatusBarText);
            lv.ItemsSource = null;
            lv.ItemsSource = execution_schedule_line;
            

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
        


        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        public static void StatusBarSetText(string message, int sleep = 0)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate ()
            {
                // stop the timer that clears the status bar
                StatusBarClearTextTimer.Change(Timeout.Infinite, Timeout.Infinite);

                // update UI
                MainWindow.statusbar.Text = message;

                if (sleep > 0)

                    // Set timer that clears the status bar to trigger in another "sleep" seconds
                    MainWindow.StatusBarClearTextTimer.Change(sleep * 1000, Timeout.Infinite);
            });
        }


        private void MachinesCreateButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CreateMachine();
            bool? result = dialog.ShowDialog();

            if (result == true)

                refresh_machines();
        }

        private void MachinesUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Machine2)MachinesListView.SelectedItem);
            var dialog = new UpdateMachine(item.id, item.name, item.host_type, item.host_name, item.comment);
            bool? result = dialog.ShowDialog();

            if (result == true)

                refresh_machines();
        }

        private void MachinesArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Machine2)MachinesListView.SelectedItem);
            MySQL.Update("UPDATE machine SET archived = 1 WHERE id = " + item.id + ";", "control_automation_machine");
            refresh_machines();
        }

        private void MachinesRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            refresh_machines();
        }

        private void MachinesPingButton_Click(object sender, RoutedEventArgs e)
        {
            this.Title = "Clicked";
        }

        private void MachinesLastBootTimeButton_Click(object sender, RoutedEventArgs e)
        {
            var items = MachinesListView.SelectedItems;

            foreach (Machine2 item in items)
            {
                item.blank = "";
                MachinesListView.Items.Refresh();
            }

            var azure_powershell_instance_arr = "";

            foreach (Machine2 item in items)
            {
                if (item.comment.Equals("hyperv"))
                {
                    TextBlock.Update(StatusBarText, "Please Wait. Querying Hyper V VM " + item.host_name + " ...");
                    var output = HyperV.QueryVM(item.host_name);
                    TextBlock.ClearNoError(StatusBarText);
                    item.blank = output[0].BaseObject.ToString();
                    MachinesListView.Items.Refresh();
                }
                else
                {
                    if (azure_powershell_instance_arr.Length > 0)

                        azure_powershell_instance_arr = azure_powershell_instance_arr + ",";

                    azure_powershell_instance_arr = azure_powershell_instance_arr + item.host_name.Replace("azureauto", "");
                }
            }

            // if Azure VMs

            if (azure_powershell_instance_arr.Length > 0)
            {
                TextBlock.Update(StatusBarText, "Please Wait. Querying Azure instances " + azure_powershell_instance_arr + " ...");
                Log.Debug("Please Wait. Querying Azure instances " + azure_powershell_instance_arr + " ...");
                var output = Azure.QueryVMs("Load Testing - Dev/Test", "LNB-Test", "lnbtest", azure_powershell_instance_arr);
                TextBlock.ClearNoError(StatusBarText);

                if (output.Count < 1)

                    TextBlock.Update(StatusBarText, "Failed to Query Azure!");
                else
                {
                    var azure_instance_num = -1;

                    foreach (Machine2 item in items)
                    {
                        azure_instance_num++;
                        item.blank = output[azure_instance_num].BaseObject.ToString();
                    }

                    MachinesListView.Items.Refresh();
                }
            }
        }

        private void MachinesStartButton_Click(object sender, RoutedEventArgs e)
        {
            var items = MachinesListView.SelectedItems;
            var vm_names = "";
            var azure_powershell_instance_arr = "";

            foreach (Machine2 item in items)
            {
                if (item.comment.Equals("hyperv"))
                {
                    if (vm_names.Length > 0)

                        vm_names = vm_names + ",";

                    vm_names = vm_names + item.host_name;
                } else
                {
                    if (azure_powershell_instance_arr.Length > 0)

                        azure_powershell_instance_arr = azure_powershell_instance_arr + ",";

                    azure_powershell_instance_arr = azure_powershell_instance_arr + item.host_name.Replace("azureauto", "");
                }
            }

            // if Hyper-V VMs

            if (vm_names.Length > 0)
            {
                StatusBarSetText("Please Wait. Starting Hyper V VMs " + vm_names + " ...", 2);
                HyperV.StartVMs(vm_names);
            }

            // if Azure VMs

            if (azure_powershell_instance_arr.Length > 0)
            {
                StatusBarSetText("Please Wait. Starting Azure instances " + azure_powershell_instance_arr + " ...", 2);
                Azure.StartVMs("Load Testing - Dev/Test", "LNB-Test", "lnbtest", azure_powershell_instance_arr);
            }

        }

        private void MachinesSaveButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MachinesStopButton_Click(object sender, RoutedEventArgs e)
        {
            var items = MachinesListView.SelectedItems;
            var vm_names = "";
            var azure_powershell_instance_arr = "";

            foreach (Machine2 item in items)
            {
                if (item.comment.Equals("hyperv"))
                {
                    if (vm_names.Length > 0)

                        vm_names = vm_names + ",";

                    vm_names = vm_names + item.host_name;
                }
                else
                {
                    if (azure_powershell_instance_arr.Length > 0)

                        azure_powershell_instance_arr = azure_powershell_instance_arr + ",";

                    azure_powershell_instance_arr = azure_powershell_instance_arr + item.host_name.Replace("azureauto", "");
                }
            }

            // if Hyper-V VMs

            if (vm_names.Length > 0)
            {
                StatusBarSetText("Please Wait. Stopping Hyper V VMs " + vm_names + " ...", 2);
                HyperV.StopVMs(vm_names);
            }

            // if Azure VMs

            if (azure_powershell_instance_arr.Length > 0)
            {
                StatusBarSetText("Please Wait. Stopping Azure instances " + azure_powershell_instance_arr + " ...", 2);
                Azure.StopVMs("Load Testing - Dev/Test", "LNB-Test", "lnbtest", azure_powershell_instance_arr);
            }



        }

        private void MachinesRestartButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MachinesCitrixReceiverButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MachinesRemoteDesktopButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MachinesVNCViewButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MachinesVNCInteractButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void RecreateRDriveButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void StartSeleniumButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ManageTestArtifactsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ManageTestArtifacts();
            window.Show();
        }

        private void ManageExecutionGroupsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ManageExecutionGroups();
            window.Show();
        }

        private void ManageAutomationProjectsButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ExecutionSchedulerButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ExecutionReporterButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ExecutionMonitorButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Settings.exe");
        }

        private void StartRFTButton_Click(object sender, RoutedEventArgs e)
        {
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Debug(e.ExceptionObject.ToString());
        }

    }
}
