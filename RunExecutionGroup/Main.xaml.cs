using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.Threading;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using RoboSharp;
using SharedProject.TestRail;
using SharedProject;
using SharedProject.Models;
using System.Globalization;

namespace RunExecutionGroup
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Window
    {

        DBConnect dbConnect;

        public static ObservableCollection<ExecutionGroupScript> ScriptsListViewCollection { get; set; }
        public List<Project> project { get; set; }
        public List<ExecutionGroup> execution_group { get; set; }
        private List<string> host_with_in_progress_script = new List<string>();
        private string execution_group_script_ids_in_progress = "";
        private System.Threading.Timer timer;
        public static System.Windows.Controls.TextBlock statusbar;
        private List<string> host_first_script_deploy_list = new List<string>();
        private int msbuild_exit_code = 0;
        private string msbuild_path = "C:\\Program Files (x86)\\MSBuild\\14.0\\Bin";

        // Handlers for events that will update GUI controls in realtime (in background threads) ...
        bool run_complete = false;
        bool initialise_gui = true;
        bool close_gui = false;
        string solution_name = "";
        string project_name = "";
        DateTime start_date_time = DateTime.Now;

        public Main()
        {
            InitializeComponent();
            Log.ResetRunLog(App.project_id, App.execution_group_id);

            // catch unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            App.CurrentStatusBarText = StatusBarText;
            AppConfig.Open();

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            if (!System.IO.Directory.Exists(msbuild_path))
            {
                msbuild_path = "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319";

                if (!System.IO.Directory.Exists(msbuild_path))

                    msbuild_path = "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319";
            }

            // Create a timer object, but don't start anything yet
            timer = new System.Threading.Timer(MainLoop, null, 0, Timeout.Infinite);
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)

                Close();
        }

        public static void StatusBarSetText(string message)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate () {
                // update UI
                Main.statusbar.Text = message;
            });
        }

        private void ScriptsListView_Reload(string execution_group_id, string db)
        {
            ///StatusBarSetText("Please Wait. Getting execution group scripts ...");
            dbConnect = new DBConnect(db);
            Main.ScriptsListViewCollection = dbConnect.SelectToObservableCollection<ExecutionGroupScript>("SELECT egs.id, m.host_name, egs.script_id, s.name AS script_name, egs.selector, egs.post_run_delay, egs.state, egs.excluded, egs.start_date_time, egs.end_date_time, egs.em_comment, egs.shared_folder_1, egs.browser, egs.order_id FROM execution_group_script egs, script s, control_automation_machine.machine m WHERE egs.script_id = s.id AND egs.machine_id = m.id AND egs.execution_group_id = " + execution_group_id + " ORDER BY egs.order_id ASC;");
            //StatusBarSetText("");
            //lv.ItemsSource = null;
            //lv.ItemsSource = execution_schedule_line;

            foreach (var eachItem in ScriptsListViewCollection)
            {
                eachItem.state = "";

                if (eachItem.excluded == false)

                    eachItem.excluded = null;
            }
        }

        private void MainLoop(object state)
        {
            // Initialise GUI

            if (initialise_gui)
            {
                initialise_gui = false;

                statusbar = StatusBarText;

                Dispatcher.Invoke((Action)delegate ()
                {
                    ProjectIDTextBox.Text = App.project_id;
                    ExeGroupIDTextBox.Text = App.execution_group_id;
                    IterationNameTextBox.Text = App.iteration_name;
                    EnvironmentNameTextBox.Text = App.test_environment_name;
                });

                // wait for the GUI to render (render thread to complete)
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => { })).Wait();

                StatusBarSetText("Please Wait. Getting project ...");
                var dbConnect = new DBConnect("control_automation_machine");
                project = dbConnect.Select<Project>("SELECT schema_name, path, external_id, external_name, webhook_url, run_reports_url FROM project WHERE id = " + App.project_id + ";");
                StatusBarSetText("");

                solution_name = project[0].path.Substring(0, project[0].path.LastIndexOf("\\")).Substring(project[0].path.IndexOf("\\") + 1);
                project_name = project[0].path.Substring(project[0].path.LastIndexOf("\\") + 1);

                // render the following GUI controls immediately (via Dispatcher)

                Dispatcher.Invoke((Action)delegate ()
                {
                    ProjectSchemaTextBox.Text = project[0].schema_name;
                    ExternalProjectIDTextBox.Text = project[0].external_id;
                    ExternalProjectNameTextBox.Text = project[0].external_name;
                });

                StatusBarSetText("Please Wait. Getting execution group ...");
                dbConnect = new DBConnect(project[0].schema_name);
                execution_group = dbConnect.Select<ExecutionGroup>("SELECT eg.name, eg.iteration_id, i.name AS iteration_name, eg.environment_id, e.name AS environment_name, eg.auto_stop_tests, eg.auto_send_results, eg.device_notifications, eg.external_plan_id, eg.external_plan_name, eg.external_exe_rec_run_id, eg.external_exe_rec_run_name FROM execution_group eg, iteration i, environment e WHERE eg.iteration_id = i.id AND eg.environment_id = e.id AND eg.id = " + App.execution_group_id + ";");
                StatusBarSetText("");

                // render the following GUI controls immediately (via Dispatcher)

                Dispatcher.Invoke((Action)delegate ()
                {
                    ExeGroupNameTextBox.Text = execution_group[0].name;
                    ExternalPlanIDTextBox.Text = execution_group[0].external_plan_id.ToString();
                    ExternalPlanNameTextBox.Text = execution_group[0].external_plan_name;
                    ExternalExeRecRunIDTextBox.Text = execution_group[0].external_exe_rec_run_id.ToString();
                    ExternalExeRecRunNameTextBox.Text = execution_group[0].external_exe_rec_run_name;
                    AutoStopTestsCheckBox.IsChecked = execution_group[0].auto_stop_tests == 1 ? true : false;
                    AutoSendResultsCheckBox.IsChecked = execution_group[0].auto_send_results == 1 ? true : false;
                    DeviceNotificationsCheckBox.IsChecked = execution_group[0].device_notifications == 1 ? true : false;
                });

                StatusBarSetText("Please Wait. Getting execution group scripts ...");
                ScriptsListViewCollection = new ObservableCollection<ExecutionGroupScript>();
                ScriptsListView_Reload(App.execution_group_id, project[0].schema_name);
                StatusBarSetText("");

                // make the initial state of all scripts "Not Run"

                foreach (var eachItem in ScriptsListViewCollection)
                {
                    eachItem.state = "Not Run";
                    eachItem.start_date_time = null;
                    eachItem.end_date_time = null;
                }

                //            StatusBarSetText("Please Wait. Updating all scripts in the execution group to 'Not Run'");
                dbConnect = new DBConnect(project[0].schema_name);
                dbConnect.Update("UPDATE execution_group_script SET state = 'Not Run', start_date_time = NULL, end_date_time = NULL WHERE execution_group_id = '" + App.execution_group_id + "' AND excluded = 0;");
                //          StatusBarSetText("");

                // if deploy and build is required

                if (App.deploy_and_build.ToInt() == 1 || App.deploy_and_build.ToInt() == 2)
                {
                    var browser_name = "";

                    if ((new Regex("( |^)chrome( |$)").IsMatch(execution_group[0].name)))

                        browser_name = "chrome";

                    if ((new Regex("( |^)MicrosoftEdge( |$)").IsMatch(execution_group[0].name)))

                        browser_name = "MicrosoftEdge";

                    if ((new Regex("( |^)internet explorer( |$)").IsMatch(execution_group[0].name)))

                        browser_name = "internet explorer";

                    if ((new Regex("( |^)firefox( |$)").IsMatch(execution_group[0].name)))

                        browser_name = "firefox";

                    if ((new Regex("( |^)safari( |$)").IsMatch(execution_group[0].name)))

                        browser_name = "safari";

                    if ((new Regex("( |^)opera( |$)").IsMatch(execution_group[0].name)))

                        browser_name = "opera";

                    //var host_first_script_deploy_list = new List<string>();
                    var tmp_host_names = "";
                    var tmp_sql_case_clause = "";

                    foreach (var eachItem in ScriptsListViewCollection)
                    {
                        if (!host_first_script_deploy_list.Any(str => str.Equals(eachItem.host_name)) && (eachItem.excluded == null || eachItem.excluded == false))
                        {
                            host_first_script_deploy_list.Add(eachItem.host_name);
                            eachItem.state = "Deploying";

                            if (tmp_host_names.Length > 0)

                                tmp_host_names = tmp_host_names + ",";

                            tmp_host_names = tmp_host_names + "'" + eachItem.host_name + "'";
					        tmp_sql_case_clause = tmp_sql_case_clause + " when host_name = '" + eachItem.host_name + "' then '" + App.source_control_username + "," + App.source_control_password + ",https://tfscls.janison.com.au:8081/tfs/clscollection," + solution_name + "," + project_name + "," + App.test_environment_name + ",1,1," + App.iteration_name + "," + App.test_environment_name + "," + browser_name + "," + eachItem.script_name + "," + eachItem.id + ",1," + project[0].schema_name + "'";

                            //var request_filename = "S:\\Requests\\" + eachItem.host_name + "\request.ini";
                            //var request_data = "\n\n" + App.source_control_username + "," + App.source_control_password + ",https://tfscls.janison.com.au:8081/tfs/clscollection," & $solution_name & "," & $project_name & "," & $test_environment_name & ",1,1," & $iteration_name & "," & $test_environment_name & "," & $browser_name & "," & $tmp_test_case_name & "," & $tmp_execution_group_script_id & "," & $run_number & "," & $mysql_db
                            //File.delete("S:\\Requests\\" + eachItem.host_name, "request.ini");
                            //File.overwrite(request_filename, request_data);
                        }
                    }

                    // update the database with all host names

                    if (tmp_host_names.Length > 0)
                    { 
				        tmp_sql_case_clause = tmp_sql_case_clause.Replace("\\", "\\\\");

                        // Instruct the adapter on that machine to deploy and build the project

                        //StatusBarSetText("Please Wait. Updating execution group script " + eachItem.id + " to 'In Progress'");
                        dbConnect = new DBConnect("control_automation_machine");
                        dbConnect.Update("UPDATE machine SET source_control_args = (case" + tmp_sql_case_clause + " end) WHERE host_name IN (" + tmp_host_names + ");");
                        //StatusBarSetText("");
                    }
                }

                // below will activate all the bindings from the XAML to the data set above, and immediately (via Dispatcher)

                Dispatcher.Invoke((Action)delegate ()
                {
                    this.DataContext = this;
                });

                // Perform the build and deploy process before running any scripts

                if (App.deploy_and_build.ToInt() == 3)
                {
                    foreach (var eachItem in ScriptsListViewCollection)
                    {
                        if (!host_first_script_deploy_list.Any(str => str.Equals(eachItem.host_name)) && (eachItem.excluded == null || eachItem.excluded == false))
                        {
                            host_first_script_deploy_list.Add(eachItem.host_name);
                            eachItem.state = "Deploying";
                        }
                    }

                    StatusBarSetText("Please Wait. Building the project.  Please Wait ...");
                    var exit_code = C_Sharp_Executable_Build(project[0].path, "Executable\\Run.cs");
                    StatusBarSetText("MSBuild exit code = " + exit_code);

                    if (exit_code != 0)
                    {
                        System.Threading.Thread.Sleep(3000);
                        cleanup(false);
                        Close();
                    }

                    foreach (var eachItem in ScriptsListViewCollection)
                    {
                        if (host_first_script_deploy_list.Any(str => str.Equals(eachItem.host_name)))
                        {
                            if (exit_code == 0)
                            {
                                robocopy(project[0].path + "\\Executable", "\\\\" + eachItem.host_name + "\\auto\\" + solution_name + "\\" + project_name + "\\Executable", "Run.*");
                                robocopy(project[0].path + "\\Data", "\\\\" + eachItem.host_name + "\\auto\\" + solution_name + "\\" + project_name + "\\Data", "*.*");
                                robocopy(project[0].path + "\\Data\\Pools", "\\\\" + eachItem.host_name + "\\auto\\" + solution_name + "\\" + project_name + "\\Data\\Pools", "*.*");
                                robocopy(project[0].path + "\\Toolkit\\Windows", "\\\\" + eachItem.host_name + "\\auto\\" + solution_name + "\\" + project_name + "\\Toolkit\\Windows");
                            }

                            eachItem.state = "Not Run";
                            eachItem.end_date_time = null;

                            // refresh the scripts listview
                            Dispatcher.Invoke((Action)delegate () { ScriptsListView.Items.Refresh(); });
                        }
                    }
                }

                // Set timer to trigger this method in another 10 seconds
                timer.Change(10 * 1000, Timeout.Infinite);
            } //else

                // Close GUI

                if (close_gui)
                {
                    //; Initialise cURL
                    //cURL_initialise()

                    //; MS Teams Results
                    //Local $teams_incoming_webhook_json = "{""$schema"": ""https://adaptivecards.io/schemas/adaptive-card.json"", ""type"": ""AdaptiveCard"", ""version"": ""1.0"", ""summary"": ""xxx"", ""text"": ""<h1><b><a href=\""https://janison.testrail.com/index.php?/runs/view/" & GUICtrlRead($external_exe_rec_run_id_input) & "\"">Execution Group " & $execution_group_id & " - " & $execution_group_name & " - Iteration " & $iteration_name & "</a></b></h1>"", ""sections"": ["
                    //Local $teams_incoming_webhook_sections_json = ""

                    //; TestRail Results - there are 3 entries per test in this array:
                    //;	[0] = test case name
                    //;	[1] = test case state (the overall result of the test for all test case scripts combined)
                    //;	[2] = test case log (the combined set of all test case script logs for this test)
                    //Local $copilot_results_arr[0]

                    //        Main.statusbar.Text = "ggg";

                    StatusBarSetText("Please Wait. Pushing the results into TestRail Run ID " + execution_group[0].external_exe_rec_run_id + " ...");

                    APIClient client = new APIClient("https://janison.testrail.com");
                    client.User = "sgriffin@janison.com";
                    client.Password = "Gri01ffo";


                    var results_json = new
                    {
                        results = new[] {
                                new {
                                    test_id = 1,
                                    status_id = 1,
                                    comment = "",
                                    elapsed = ""
                                }
                            }.ToList()
                    };

                    results_json.results.RemoveAt(0);

                    var statuses = (JArray)client.SendGet("get_statuses");
                    var tests = (JObject)client.SendGet("get_tests/" + execution_group[0].external_exe_rec_run_id);

                    dbConnect = new DBConnect(project[0].schema_name);
                    var test_case_results = dbConnect.Select<TestCaseResult>("SELECT GROUP_CONCAT(egs.id) AS execution_group_script_ids, GROUP_CONCAT(s.name) AS test_case_script_names, LEFT(s.name, LENGTH(s.name) - LOCATE('_', REVERSE(s.name))) AS test_case_name, GROUP_CONCAT(egs.state) AS test_case_states, SUM(timestampdiff(SECOND, egs.start_date_time, egs.end_date_time)) AS elapsed FROM execution_group_script egs, script s WHERE egs.excluded = 0 AND egs.script_id = s.id AND egs.execution_group_id = " + App.execution_group_id + " GROUP BY test_case_name ORDER BY s.name ASC;");

                    foreach (var test_case_result in test_case_results)
                    {
                        Log.Debug("Processing result for test_case_name = " + test_case_result.test_case_name);

                        test_case_result.test_case_name = test_case_result.test_case_name.Substring(test_case_result.test_case_name.LastIndexOf('.') + 1);
                        var tmp_test_case_state = test_case_result.test_case_states.Split(',');

                        var test_case_state = "Passed";

                        if (tmp_test_case_state.Contains("Fail"))

                            test_case_state = "Failed";
                        else

                            if (tmp_test_case_state.Contains("") || tmp_test_case_state.Contains("Not Run") || tmp_test_case_state.Contains("In Progress"))

                                test_case_state = "Incomplete Automation";

                        // MS Teams Results


                        // Extract the log of that results for each test case script part of the test case

                        var tmp_test_case_script_names = test_case_result.test_case_script_names.Split(',');
                        var tmp_execution_group_script_id = test_case_result.execution_group_script_ids.Split(',');
                        var comment = "";

                        for (int i = 0; i < tmp_test_case_script_names.Length; i++)
                        {
                            // TestRail Results
                            dbConnect = new DBConnect(project[0].schema_name);
                            var tmp_arr = dbConnect.Select<ExecutionGroupScriptLog>("SELECT '|' AS pipe, date_time, result, text FROM execution_group_script_log WHERE execution_group_script_id = " + tmp_execution_group_script_id[i] + " AND result <> 'DEBUG' ORDER BY id ASC;");

                            if (tmp_arr.Count > 0)
                            {
                                // Double up on any backslashes because when posting into TestRail we lose one set of backslashes

                                var log_str = "";

                                foreach (var each in tmp_arr)
                                {
                                    if (log_str.Length > 0)

                                        log_str = log_str + "\n";

                                    log_str = log_str + each.pipe + "|" + each.date_time + "|" + each.result + "|" + each.text.Replace("\\", "\\\\").Replace("\"\"", "\\\"\"");
                                }

                                comment = comment + "\n\n" + tmp_test_case_script_names[i] + "\n\n" + log_str;
                            }
                        }

                        // TestRail Results

                        if (tests != null && tests["tests"] != null)
                        {
                            var testrail_test = tests["tests"].SelectToken("$.[?(@.custom_auto_script_ref == '" + test_case_result.test_case_name + "')]");

                            if (testrail_test != null)
                            {
                                var test_id = testrail_test["id"].ToString().ToInt();
                                var status_id = statuses.SelectToken("$.[?(@.label == '" + test_case_state + "')]")["id"].ToString().ToInt();

                                results_json.results.Add(new
                                {
                                    test_id = test_id,
                                    status_id = status_id,
                                    comment = comment,
                                    elapsed = test_case_result.elapsed + "s"
                                });
                            }
                        }
                    }

                    // Pass the CoPilot results into the TestRail run

                    if (results_json.results.Count > 0)

                        client.SeanPost("add_results/" + execution_group[0].external_exe_rec_run_id, results_json.ToJSONString());

                    StatusBarSetText("");

                    //GUICtrlSetData($status_input, "Getting the TestRail Test Statuses ... ")
                    //Local $status_label_and_id_dict = _TestRailGetStatusLabelAndID()

                    //GUICtrlSetData($status_input, "Getting the TestRail Test References and IDs ... ")
                    //Local $test_refs_and_id_dict = _TestRailGetTestsReferenceAndIDFromRunID(GUICtrlRead($external_exe_rec_run_id_input))

                    //Local $tmp_arr = MySql_query_sean("SELECT GROUP_CONCAT(egs.id), GROUP_CONCAT(s.name), LEFT(s.name, LENGTH(s.name) - LOCATE('_', REVERSE(s.name))) AS fred, GROUP_CONCAT(egs.state), SUM(timestampdiff(SECOND, egs.start_date_time, egs.end_date_time)) FROM execution_group_script egs, script s WHERE egs.excluded = 0 AND egs.script_id = s.id AND egs.execution_group_id = " & $execution_group_id & " GROUP BY fred ORDER BY s.name ASC;", $mysql_db)

                    //for $i = 1 to (UBound($tmp_arr) - 1)

                    // Local $tmp_execution_group_script_ids = $tmp_arr[$i][0]
                    // Local $tmp_test_case_script_names = $tmp_arr[$i][1]
                    // Local $tmp_test_case_name = $tmp_arr[$i][2]
                    // $tmp_test_case_name = StringMid($tmp_test_case_name, StringInStr($tmp_test_case_name, ".", 0, -1) + 1)
                    // Local $tmp_test_case_states = $tmp_arr[$i][3]
                    // Local $tmp_test_case_state = StringSplit($tmp_test_case_states, ",", 3)
                    // Local $elapsed = $tmp_arr[$i][4]

                    // Local $test_case_state = "Passed"

                    // if _ArraySearch($tmp_test_case_state, "Fail") > -1 Then

                    //  $test_case_state = "Failed"
                    // Else

                    //  if _ArraySearch($tmp_test_case_state, "") > -1 or _ArraySearch($tmp_test_case_state, "Not Run") > -1 or _ArraySearch($tmp_test_case_state, "In Progress") > -1 Then

                    //   $test_case_state = "Incomplete Automation"
                    //  EndIf
                    // EndIf

                    // ; MS Teams Results

                    // Local $result_colour = "red"

                    // if StringCompare($test_case_state, "Passed") = 0 Then

                    //  $result_colour = "green"
                    // EndIf

                    // Local $teams_incoming_webhook_section_json = "{""markdown"": false, ""activityTitle"": ""<a href=\""https://janison.testrail.com/index.php?/tests/view/" & $test_refs_and_id_dict.Item($tmp_test_case_name) & "\"">Test Case " & $tmp_test_case_name & "</a>"", ""activitySubtitle"": ""<font color=\""" & $result_colour & "\"">" & $test_case_state & "</font>"", ""facts"": ["
                    // Local $teams_incoming_webhook_section_facts_json = ""

                    // ; Extract the log of that results for each test case script part of the test case

                    // $tmp_test_case_script_name = StringSplit($tmp_test_case_script_names, ",", 3)
                    // $tmp_execution_group_script_id = StringSplit($tmp_execution_group_script_ids, ",", 3)
                    // Local $fred = ""

                    // for $j = 0 to (UBound($tmp_test_case_script_name) - 1)

                    //  ; MS Teams Results

                    //  if StringLen($teams_incoming_webhook_section_facts_json) > 0 Then

                    //   $teams_incoming_webhook_section_facts_json = $teams_incoming_webhook_section_facts_json & ", "
                    //  EndIf

                    //  Local $tmp_test_case_script_part_number = StringMid($tmp_test_case_script_name[$j], StringInStr($tmp_test_case_script_name[$j], "_", 1, -1) + 1)

                    //  Local $result_colour = "red"

                    //  if StringCompare($tmp_test_case_state[$j], "Pass") = 0 Then

                    //   $result_colour = "green"
                    //  EndIf

                    //  $teams_incoming_webhook_section_facts_json = $teams_incoming_webhook_section_facts_json & "{""name"": ""Part " & Number($tmp_test_case_script_part_number) & """, ""value"": ""<font color=\""" & $result_colour & "\"">" & $tmp_test_case_state[$j] & "</font>""}"

                    //  ; TestRail Results

                    //  Local $tmp_arr2 = MySql_query_sean("SELECT '|', date_time, result, text FROM execution_group_script_log WHERE execution_group_script_id = " & $tmp_execution_group_script_id[$j] & " AND result <> 'DEBUG' ORDER BY id ASC;", $mysql_db)
                    //  _ArrayDelete($tmp_arr2, 0)

                    //  ; Double up on any backslashes because when posting into TestRail we lose one set of backslashes

                    //  for $k = 0 to (UBound($tmp_arr2) - 1)

                    //   $tmp_arr2[$k][3] = StringReplace($tmp_arr2[$k][3], "\", "\\")
                    //   $tmp_arr2[$k][3] = StringReplace($tmp_arr2[$k][3], """", "\""")
                    //  Next

                    //  Local $log_str = _ArrayToString($tmp_arr2, "|", -1, -1, "\n")
                    //  $fred = $fred & "\n\n" & $tmp_test_case_script_name[$j] & "\n\n" & $log_str
                    // Next

                    // ; MS Teams Results

                    // $teams_incoming_webhook_section_json = $teams_incoming_webhook_section_json & $teams_incoming_webhook_section_facts_json & "]}"

                    // if StringLen($teams_incoming_webhook_sections_json) > 0 Then

                    //  $teams_incoming_webhook_sections_json = $teams_incoming_webhook_sections_json & ", "
                    // EndIf

                    // $teams_incoming_webhook_sections_json = $teams_incoming_webhook_sections_json & $teams_incoming_webhook_section_json

                    // ; TestRail Results

                    // _ArrayAdd($copilot_results_arr, $test_refs_and_id_dict.Item($tmp_test_case_name))
                    // _ArrayAdd($copilot_results_arr, $status_label_and_id_dict.Item($test_case_state))
                    // _ArrayAdd($copilot_results_arr, $fred, 0, "", "", 1)
                    // _ArrayAdd($copilot_results_arr, $elapsed & "s", 0, "", "", 1)
                    //Next

                    //; MS Teams Results

                    //GUICtrlSetData($status_input, "Pushing the results into MS Teams ... ")
                    //$teams_incoming_webhook_json = $teams_incoming_webhook_json & $teams_incoming_webhook_sections_json & ",{""markdown"": false, ""activityTitle"": ""<i><a href=\""" & $run_reports_url & "\"">Test Automation Run Reports</a></i>""}]}"
                    //FileDelete(@ScriptDir & "\curl_in_webhook.json")
                    //FileWrite(@ScriptDir & "\curl_in_webhook.json", $teams_incoming_webhook_json)
                    //Local $iPID = Run('curl.exe -s -k -H "Content-Type: application/json" --data @curl_in_webhook.json ' & $webhook_url, @ScriptDir, @SW_HIDE, $STDOUT_CHILD)
                    //ProcessWaitClose($iPID)
                    //Local $webhook_json = StdoutRead($iPID)
                    //GUICtrlSetData($status_input, "")

                    //; sean url...
                    //;https://outlook.office.com/webhook/1d246eea-76e2-48f0-a25c-78b8b031584e@13f5c343-cdd7-416f-af2a-093bb2cd877a/IncomingWebhook/f7fc1e2d515840cf872e7360dd6ec906/d95f08ac-256d-4f50-a70a-beef3a33ff8d

                    //;https://outlook.office.com/webhook/1d246eea-76e2-48f0-a25c-78b8b031584e@13f5c343-cdd7-416f-af2a-093bb2cd877a/IncomingWebhook/367b861f3a6a47aeba663df4d824532c/d95f08ac-256d-4f50-a70a-beef3a33ff8d

                    //; phoenix up test results url ...
                    //;https://outlook.office.com/webhook/60c5cf5e-7699-4483-a0d3-a7e8726a523c@13f5c343-cdd7-416f-af2a-093bb2cd877a/IncomingWebhook/5c3ee9188ec34653bd728394be777e2a/d95f08ac-256d-4f50-a70a-beef3a33ff8d

                    //; phoenix pisa test results url ...
                    //;https://outlook.office.com/webhook/60c5cf5e-7699-4483-a0d3-a7e8726a523c@13f5c343-cdd7-416f-af2a-093bb2cd877a/IncomingWebhook/9c18eae2811742b391f4ada22b1aa4a9/d95f08ac-256d-4f50-a70a-beef3a33ff8d

                    //; rms test results url...
                    //;https://outlook.office.com/webhook/21d9905c-6c75-4610-8c2f-83263c767885@13f5c343-cdd7-416f-af2a-093bb2cd877a/IncomingWebhook/29e85a150cf2411eb0442390f9c550d3/d95f08ac-256d-4f50-a70a-beef3a33ff8d

                    //; Pass the CoPilot results array into the TestRail run

                    //GUICtrlSetData($status_input, "Pushing the results into TestRail Run ID " & GUICtrlRead($external_exe_rec_run_id_input) & " ... ")
                    //_TestRailAddResults(GUICtrlRead($external_exe_rec_run_id_input), $copilot_results_arr)
                    //GUICtrlSetData($status_input, "")

                    //cURL_cleanup()
                    
                    // close the Main Window
                    Dispatcher.Invoke((Action)delegate () { this.Close(); });
                }
                else
                {
                    // Main processing loop

                    // if a deploy and build is occurring before the run

                    if (App.deploy_and_build.ToInt() == 1 || App.deploy_and_build.ToInt() == 2)
                    {
                        var tmp_host_names = "";

                        foreach (var host_name in host_first_script_deploy_list)
                        {
                            if (tmp_host_names.Length > 0)

                                tmp_host_names = tmp_host_names + ",";

                            tmp_host_names = tmp_host_names + "'" + host_name + "'";
                        }

                        dbConnect = new DBConnect("control_automation_machine");
                        var machines = dbConnect.Select<Machine3>("SELECT host_name, source_control_args FROM machine WHERE host_name IN (" + tmp_host_names + ");");

                        foreach (var eachItem in ScriptsListViewCollection)
                        {
                            //     if (!host_first_script_deploy_list.Any(str => str.Equals(eachItem.host_name)))

                            var machine = machines.Find(str => str.host_name.Equals(eachItem.host_name));

                            if (machine != null && machine.source_control_args.IsInt())
                            {
                                if (machine.source_control_args.ToInt() == 0)
                                {
                                    eachItem.state = "Not Run";
                                    eachItem.end_date_time = null;
                                }
                                else
                                {
                                    eachItem.state = "Fail";
                                    cleanup(false);

                                    try
                                    {
                                        Close();
                                    }
                                    catch (Exception)
                                    {

                                    }
                                }

                                host_first_script_deploy_list.Remove(eachItem.host_name);
                            }
                        }

                        if (host_first_script_deploy_list.Count < 1)
                        {
                            App.deploy_and_build = "0";
                        }

                        // refresh the scripts listview

                        Dispatcher.Invoke((Action)delegate () { ScriptsListView.Items.Refresh(); });

                    }
                    else
                    {

                        // get all scripts are already in progress in the current group.
                        //  a group is defined as a contiguous set of scripts with the same Selector

                        var first_selector = '-';
                        execution_group_script_ids_in_progress = "";

                        foreach (var eachItem in ScriptsListViewCollection)
                        {
                            if (eachItem.excluded != true)
                            {
                                if (eachItem.state.Equals("In Progress") && first_selector.Equals('-'))

                                    first_selector = eachItem.selector[0];

                                if (first_selector.Equals(eachItem.selector[0]))
                                {
                                    if (eachItem.state.Equals("In Progress"))
                                    {
                                        if (execution_group_script_ids_in_progress.Length > 0)

                                            execution_group_script_ids_in_progress = execution_group_script_ids_in_progress + ",";

                                        execution_group_script_ids_in_progress = execution_group_script_ids_in_progress + eachItem.id;
                                    }
                                }
                            }
                        }

                        // update the scripts whose state is In Progress (to whatever it is now in the database)

                        if (execution_group_script_ids_in_progress.Length > 0)
                        {
                            StatusBarSetText("Please Wait. Getting execution group scripts ...");
                            dbConnect = new DBConnect(project[0].schema_name);
                            var scripts = dbConnect.Select<ExecutionGroupScript>("SELECT id, state, start_date_time, end_date_time FROM execution_group_script WHERE id IN (" + execution_group_script_ids_in_progress + ");");
                            //ScriptsListView.ItemsSource = scripts;
                            StatusBarSetText("");

                            foreach (var each in scripts)
                            {
                                ScriptsListViewCollection.Single(x => x.id == each.id).state = each.state;
                                ScriptsListViewCollection.Single(x => x.id == each.id).start_date_time = each.start_date_time;
                                ScriptsListViewCollection.Single(x => x.id == each.id).end_date_time = each.end_date_time;
                            }

                        }



                        // determine which scripts should now be in progress in the current group.
                        //  a group is defined as a contiguous set of scripts in the same Selector

                        first_selector = '-';
                        var machine_list = new List<Machine3>();

//                        foreach (var eachItem in ScriptsListViewCollection)
                        for (int i = 0; i < ScriptsListViewCollection.Count; i++)
                        {
//                            if (eachItem.excluded != true)
                            if (ScriptsListViewCollection[i].excluded != true)
                            {
                                if (first_selector.Equals('-'))
                                {
                                    if (ScriptsListViewCollection[i].state.Equals("Not Run") || ScriptsListViewCollection[i].state.Equals("In Progress"))
                                    {
                                        first_selector = ScriptsListViewCollection[i].selector[0];

                                        // handle sequential selectors

                                        if (ScriptsListViewCollection[i].selector[0].Equals('S'))

                                            if (ScriptsListViewCollection[i].state.Equals("In Progress"))

                                                break;
                                            else
                                            {
                                                StatusBarSetText("Please Wait. Updating execution group script " + ScriptsListViewCollection[i].id + " to 'In Progress'");
                                                var now = DateTime.Now;
                                                dbConnect = new DBConnect(project[0].schema_name);
//                                                dbConnect.Update("UPDATE execution_group_script SET state = 'In Progress', start_date_time = '" + now.ToString("yyyy-MM-dd HH:mm:ss") + "', end_date_time = NULL WHERE id = " + ScriptsListViewCollection[i].id + ";");
                                                dbConnect.Update("UPDATE execution_group_script SET state = 'In Progress' WHERE id = " + ScriptsListViewCollection[i].id + ";");
                                                StatusBarSetText("");
                                                ScriptsListViewCollection.Single(x => x.id == ScriptsListViewCollection[i].id).state = "In Progress";
                                                //ScriptsListViewCollection.Single(x => x.id == ScriptsListViewCollection[i].id).start_date_time = now;

                                                // add the run to the machine

                                                var execution_group_first_run = 0;

                                                if (!machine_list.Exists(x => x.host_name == ScriptsListViewCollection[i].host_name))
                                                {
                                                    machine_list.Add(new Machine3() { host_name = ScriptsListViewCollection[i].host_name });
                                                    execution_group_first_run = 1;
                                                }

                                                var machine = machine_list.Single(x => x.host_name == ScriptsListViewCollection[i].host_name);

                                                if (machine.run_ids == null)

                                                    machine.run_ids = new List<Run>();

                                                machine.run_ids.Add(new Run { p = App.project_id, eg = App.execution_group_id, egFst = execution_group_first_run, egs = ScriptsListViewCollection[i].id, egsDel = ScriptsListViewCollection[i].post_run_delay });
                                                break;
                                            }
                                    }
                                }

                                if (first_selector.Equals('P'))
                                {
                                    // handle parallel selectors

                                    if (ScriptsListViewCollection[i].state.Equals("Not Run"))
                                    {
                                        // any script that is not running should be in progress

                                        //if (eachItem.state.Equals("In Progress"))

                                        //    host_with_in_progress_script.Add(eachItem.host_name);

                                        //if (!host_with_in_progress_script.Contains(eachItem.host_name))
                                        //{
                                        //    host_with_in_progress_script.Add(eachItem.host_name);
                                        //    eachItem.state = "In Progress";
                                        //    run_ids = run_ids + "," + eachItem.id + "," + eachItem.post_run_delay + ",100,1";

                                        StatusBarSetText("Please Wait. Updating execution group script " + ScriptsListViewCollection[i].id + " to 'In Progress'");
                                        var now = DateTime.Now;
                                        dbConnect = new DBConnect(project[0].schema_name);
                                        dbConnect.Update("UPDATE execution_group_script SET state = 'In Progress', start_date_time = '" + now.ToString("yyyy-MM-dd HH:mm:ss") + "' WHERE id = " + ScriptsListViewCollection[i].id + ";");
                                        StatusBarSetText("");
                                        ScriptsListViewCollection.Single(x => x.id == ScriptsListViewCollection[i].id).state = "In Progress";
                                        ScriptsListViewCollection.Single(x => x.id == ScriptsListViewCollection[i].id).start_date_time = now;

                                    // add the run to the machine

                                    var execution_group_first_run = 0;

                                        if (!machine_list.Exists(x => x.host_name == ScriptsListViewCollection[i].host_name))
                                        {
                                            machine_list.Add(new Machine3() { host_name = ScriptsListViewCollection[i].host_name });
                                            execution_group_first_run = 1;
                                        }

                                        var machine = machine_list.Single(x => x.host_name == ScriptsListViewCollection[i].host_name);

                                        if (machine.run_ids == null)

                                            machine.run_ids = new List<Run>();

                                        machine.run_ids.Add(new Run { p = App.project_id, eg = App.execution_group_id, egFst = execution_group_first_run, egs = ScriptsListViewCollection[i].id, egsDel = ScriptsListViewCollection[i].post_run_delay });
                                    }

                                    if (ScriptsListViewCollection[i].selector.Equals("PE"))

                                        break;
                                }

                                // if auto stop tests and a script has failed (this means all scripts afterwards belonging to the same test
                                //  must be excluded)

                                if (execution_group[0].auto_stop_tests == 1 && ScriptsListViewCollection[i].state.Equals("Fail"))
                                {
                                    var match = Regex.Match(ScriptsListViewCollection[i].script_name, "_[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]");

                                    if (match.Success)
                                    {
                                        var test_name = ScriptsListViewCollection[i].script_name.Substring(0, match.Index);

                                        for (int j = i + 1; j < ScriptsListViewCollection.Count; j++)
                                        
                                            if (ScriptsListViewCollection[j].script_name.Contains(test_name))

                                                ScriptsListViewCollection[j].excluded = true;
                                        
                                    }

                                }
                            }
                        }

                        // refresh the scripts listview

                        Dispatcher.Invoke((Action)delegate () { ScriptsListView.Items.Refresh(); });

                        var auto_archive_script_logs = "0";

                        foreach (var machine in machine_list)
                        {
                            // StatusBarSetText("Please Wait. Updating execution group script " + eachItem.id + " to 'In Progress'");
                            dbConnect = new DBConnect("control_automation_machine");
                            dbConnect.Update("UPDATE machine SET run_ids = '" + JsonConvert.SerializeObject(machine.run_ids) + "', run_auto_archive_script_logs = " + auto_archive_script_logs + " WHERE host_name = '" + machine.host_name + "';");
                            // StatusBarSetText("");

                        }

                        var tmp_overall_status = "Pass";

                        foreach (var eachItem in ScriptsListViewCollection)
                        {
                            if (eachItem.excluded != true)
                            {
                                if (eachItem.state.Equals("Fail"))

                                    tmp_overall_status = "Fail";
                            }
                        }

                        // if first_selector is still not updated then all available scripts have ended (in either pass or fail) 
                        //  and the whole execution group is done

                        if (first_selector.Equals('-'))
                        {
                            execution_group[0].end_date_time = DateTime.Now;

                            StatusBarSetText("Please Wait. Updating execution group " + App.execution_group_id + " ...");
                            dbConnect = new DBConnect(project[0].schema_name);
                            dbConnect.Update("UPDATE execution_group SET start_date_time = '" + start_date_time.ToString("yyyy-MM-dd HH:mm:ss") + "', end_date_time = '" + execution_group[0].end_date_time.ToString("yyyy-MM-dd HH:mm:ss") + "', result = '" + tmp_overall_status + "' WHERE id = " + App.execution_group_id + ";");
                            StatusBarSetText("");

                            //cleanup();
                            run_complete = true;

                            // close the Main Window
                            Dispatcher.Invoke((Action)delegate () { this.Close(); });
                        }

                    }

                    // Set timer to trigger this method in another 10 seconds
                    timer.Change(10 * 1000, Timeout.Infinite);
                }

        }


        private void robocopy(string source_path, string target_path, string filename = "")
        {
            RoboCommand copy = new RoboCommand();
            copy.SelectionOptions.IncludeSame = true;
            copy.SelectionOptions.IncludeTweaked = true;
            copy.RetryOptions.RetryCount = 15;
            copy.RetryOptions.RetryWaitTime = 1;
            copy.CopyOptions.RemoveAttributes = "R";
            copy.CopyOptions.Source = source_path;
            copy.CopyOptions.Destination = target_path;

            if (filename.Length > 0)
            {
                copy.CopyOptions.Purge = true;
                copy.CopyOptions.FileFilter = new[] { filename };
            } else
            {
                copy.CopyOptions.Mirror = true;
            }

            var copy_task = copy.Start();
            copy_task.Wait();

        }



        private void cleanup(bool run_aborted)
        {



	       // If Jenkins exists

	       // if StringInStr($working_dir, "\.jenkins\") > 0 Then

		      //  ; Build a Jenkins JUnit test result report XML

		      //  local $remote_logs_folder_name = StringReplace($project_path, "R:\", "") & "_remote_logs"
		      //  Local $jenkins_pipeline_path = $working_dir & "\" & $remote_logs_folder_name
		      //  local $jenkins_pipeline_iteration_env_path = $jenkins_pipeline_path & "\" & $iteration_name & "_" & $test_environment_name
		      //  Local $jenkins_pipeline_junit_test_result_xml_path = $jenkins_pipeline_iteration_env_path & "\" & GUICtrlread($execution_group_name_input) & ".xml"

		      //  Local $time = _DateDiff("s", $execution_group_start_date, $execution_group_end_date)

		      //  $jenkins_test_result_xml = 	"<?xml version=""1.0"" encoding=""UTF-8""?>" & @CRLF & _
								//	        "<testsuite name=""" & GUICtrlread($execution_group_name_input) & """ tests=""" & _GUICtrlListView_GetItemCount($script_listview) & """ errors=""0"" failures=""" & $num_failures & """ hostname=""localhost"" id=""0"" time=""" & $time & """>" & @CRLF

		      //  for $script_index = 0 to (_GUICtrlListView_GetItemCount($script_listview) - 1)

			     //   Local $test_case_name = _GUICtrlListView_GetItemText($script_listview, $script_index, 3)
			     //   Local $state = _GUICtrlListView_GetItemText($script_listview, $script_index, 7)
			     //   Local $date = _GUICtrlListView_GetItemText($script_listview, $script_index, 9)
			     //   Local $failure_msg = ""

			     //   $time = _DateDiff("s", $execution_group_start_date, $date)

			     //   if StringCompare($state, "Fail") = 0 Then

				    //    $failure_msg = "<failure message=""test failure"">See the HTML Report for details</failure>"
			     //   EndIf

        //;						local $system_out = "<system-out>Removing old temporary Selenium files" & @CRLF & "Executable.PIV_POC.EB_PIV_POC_S0001_00000010 line 248 called Executable.PIV_POC.EB_PIV_POC_S0001_00000010" & @CRLF & "PRE-CONDITIONS" & @CRLF & "PR1. Get an existing EB WORLD Member from the datapool</system-out>"
			     //   local $system_out = ""

			     //   $jenkins_test_result_xml = $jenkins_test_result_xml & "<testcase name=""" & $test_case_name & """ classname=""" & $test_case_name & """ status=""" & $state & """ time=""" & $time & """>" & $failure_msg & $system_out & "</testcase>" & @CRLF
		      //  Next

		      //  $jenkins_test_result_xml = $jenkins_test_result_xml & 	"</testsuite>" & @CRLF

		      //  if FileExists($jenkins_pipeline_iteration_env_path) = False Then

			     //   DirCreate($jenkins_pipeline_iteration_env_path)
		      //  EndIf

		      //  if FileExists($jenkins_pipeline_junit_test_result_xml_path) = True Then

			     //   FileDelete($jenkins_pipeline_junit_test_result_xml_path)
		      //  EndIf

		      //  FileWrite($jenkins_pipeline_junit_test_result_xml_path, $jenkins_test_result_xml)

		      //  ; Build a Jenkins HTML Report

		      //  if FileExists($jenkins_pipeline_iteration_env_path & "\" & GUICtrlread($execution_group_name_input)) = False Then

			     //   DirCreate($jenkins_pipeline_iteration_env_path & "\" & GUICtrlread($execution_group_name_input))
		      //  EndIf

		      //  FileDelete($jenkins_pipeline_iteration_env_path & "\" & GUICtrlread($execution_group_name_input) & "\index.html")

		      //  $jenkins_test_result_html = "<html>" & @CRLF & _
								//	        "	<head>" & @CRLF & _
								//	        "		<style>" & @CRLF & _
								//	        "			body {" & @CRLF & _
								//	        "   			font-family: Arial, Helvetica, sans-serif;" & @CRLF & _
								//	        "			}" & @CRLF & _
								//	        "			th {" & @CRLF & _
								//	        "   			text-align: left;" & @CRLF & _
								//	        "				background-color:LightGrey;" & @CRLF & _
								//	        "			}" & @CRLF & _
								//	        "			table {" & @CRLF & _
								//	        "				border-collapse: collapse;" & @CRLF & _
								//	        "			}" & @CRLF & _
								//	        "		</style>" & @CRLF & _
								//	        "	</head>" & @CRLF & _
								//	        "	<body>" & @CRLF & _
								//	        "		<h1>Build Test Results Report for Execution Group """ & GUICtrlread($execution_group_name_input) & """ </h1>" & @CRLF & _
								//	        "		<h2>Summary</h2>" & @CRLF & _
								//	        "		<table style=""width:100%"">" & @CRLF & _
								//	        "	  		<tr><th>Test Case</th><th>Duration (sec)</th><th>Result</th></tr>" & @CRLF

		      //  for $script_index = 0 to (_GUICtrlListView_GetItemCount($script_listview) - 1)

			     //   Local $test_case_name = _GUICtrlListView_GetItemText($script_listview, $script_index, 3)
			     //   Local $state = _GUICtrlListView_GetItemText($script_listview, $script_index, 7)
			     //   Local $date = _GUICtrlListView_GetItemText($script_listview, $script_index, 9)

			     //   $time = _DateDiff("s", $execution_group_start_date, $date)

			     //   Local $msg_bgcolor = ""

			     //   Switch $state

				    //    Case "Pass"

					   //     $msg_bgcolor = " bgcolor=""LawnGreen"""

				    //    Case "Fail"

					   //     $msg_bgcolor = " bgcolor=""LightSalmon"""

			     //   EndSwitch

			     //   $jenkins_test_result_html = $jenkins_test_result_html & "	  		<tr" & $msg_bgcolor & "><td>" & $test_case_name & "</td><td>" & $time & "</td><td>" & $state & "</td></tr>" & @CRLF
		      //  Next

		      //  $jenkins_test_result_html = $jenkins_test_result_html & "		</table>" & @CRLF & _
								//								        "		<h2>Detail</h2>" & @CRLF

		      //  for $script_index = 0 to (_GUICtrlListView_GetItemCount($script_listview) - 1)

			     //   Local $host_name = _GUICtrlListView_GetItemText($script_listview, $script_index, 1)
			     //   Local $test_case_name = _GUICtrlListView_GetItemText($script_listview, $script_index, 3)
			     //   Local $state = _GUICtrlListView_GetItemText($script_listview, $script_index, 7)
			     //   Local $date = _GUICtrlListView_GetItemText($script_listview, $script_index, 9)
			     //   Local $failure_msg = ""

			     //   if StringLen($browserstack_runtime_host_name) > 0 Then

				    //    $host_name = $browserstack_runtime_host_name
			     //   EndIf

			     //   $jenkins_test_result_html = $jenkins_test_result_html & "		<h3>" & $test_case_name & "</h3>" & @CRLF & _
								//									        "		<table style=""width:100%"">" & @CRLF & _
								//									        "	  		<tr><th width=""200"">Date Time</th><th width=""100"">Result</th><th>Text</th></tr>" & @CRLF

        //;			local $remote_logs_iteration_env_path = $project_path & "_remote_logs\" & $iteration_name & "_" & $test_environment_name
			     //   local $remote_logs_iteration_env_path = "\\" & $host_name & "\auto\" & StringReplace($project_path, "R:\", "") & "_remote_logs\" & $iteration_name & "_" & $test_environment_name
        //;			msgbox(0,"script_index", $script_index)
			     //   Local $remote_logs_for_script_path = $remote_logs_iteration_env_path & "\" & $test_case_name
        //;			msgbox(0,"remote_logs_for_script_path", $remote_logs_for_script_path)

			     //   if FileExists($jenkins_pipeline_iteration_env_path & "\" & GUICtrlread($execution_group_name_input) & "\" & $test_case_name) = False Then

				    //    DirCreate($jenkins_pipeline_iteration_env_path & "\" & GUICtrlread($execution_group_name_input) & "\" & $test_case_name)
			     //   EndIf

			     //   FileDelete($jenkins_pipeline_iteration_env_path & "\" & GUICtrlread($execution_group_name_input) & "\" & $test_case_name & "\*.png")
			     //   FileCopy($remote_logs_for_script_path & "\*.png", $jenkins_pipeline_iteration_env_path & "\" & GUICtrlread($execution_group_name_input) & "\" & $test_case_name)

			     //   Local $log_xml = FileRead($remote_logs_for_script_path & "\log.xml")
        //;			msgbox(0,"log_xml", $log_xml)
			     //   Local $xml_dom = _XMLLoadXML($log_xml, "")
			     //   Local $num_nodes = _XMLGetNodeCount($xml_dom, "/log/msg")

			     //   for $i = 1 to $num_nodes

				    //    $msg_date_time = _XMLGetValue($xml_dom, "/log/msg[" & $i & "]/date_time")
				    //    $msg_result = _XMLGetValue($xml_dom, "/log/msg[" & $i & "]/result")
				    //    $msg_text = _XMLGetValue($xml_dom, "/log/msg[" & $i & "]/text")

				    //    if StringInStr($msg_text, "Screenshot ") = 1 Then

					   //     Local $screenshot_filename = StringReplace($msg_text, "Screenshot ", "")
					   //     $screenshot_filename = $test_case_name & "\" & $screenshot_filename

					   //     if StringInStr($test_case_name, "_MOB_") > 0 Then

						  //      $msg_text = "<img width=""25%"" alt="""" src=""" & $screenshot_filename & """ />"
					   //     Else

						  //      $msg_text = "<img width=""75%"" alt="""" src=""" & $screenshot_filename & """ />"
					   //     EndIf
				    //    EndIf

				    //    Local $msg_bgcolor = ""

				    //    Switch $msg_result

					   //     Case "Pass"

						  //      $msg_bgcolor = " bgcolor=""LawnGreen"""

					   //     Case "Fail"

						  //      $msg_bgcolor = " bgcolor=""LightSalmon"""

				    //    EndSwitch

				    //    $jenkins_test_result_html = $jenkins_test_result_html & "	  		<tr" & $msg_bgcolor & "><td>" & $msg_date_time & "</td><td>" & $msg_result & "</td><td>" & $msg_text & "</td></tr>" & @CRLF
			     //   Next

			     //   $jenkins_test_result_html = $jenkins_test_result_html & "		</table>" & @CRLF

		      //  Next

		      //  $jenkins_test_result_html = $jenkins_test_result_html & "	</body>" & @CRLF & _
								//								        "</html>" & @CRLF

		      //  FileWrite($jenkins_pipeline_iteration_env_path & "\" & GUICtrlread($execution_group_name_input) & "\index.html", $jenkins_test_result_html)

	       // EndIf

            //if (AutoSendResultsCheckBox.IsChecked == true)
            //{

            //}



        }



        private int C_Sharp_Executable_Build(string solution_and_project_path, string full_file_name)
        {
            var last_folder_pos = solution_and_project_path.LastIndexOf("\\");
            var solution_path = solution_and_project_path.Substring(0, last_folder_pos);
            var project_name = solution_and_project_path.Substring(last_folder_pos + 1);

            var target_exe = full_file_name.Replace(".cs", ".exe");
            var target_pdb = full_file_name.Replace(".cs", ".pdb");
            full_file_name = full_file_name.Replace("\\", ".");
            full_file_name = full_file_name.Replace(".cs", "");

            var last_dot_pos = full_file_name.LastIndexOf(".") + 1;
            var script_name = full_file_name.Substring(last_dot_pos);

            var project_path = solution_path + "\\" + project_name;
            var project_debug_path = project_path + "\\bin\\Debug";
            var project_release_path = project_path + "\\Executable";
            var csproj_name = project_name + ".csproj";
            var csproj_path = project_path + "\\" + csproj_name;
            var test_csproj_path = project_path + "\\" + script_name + ".csproj";

            //LogMessage(csproj_path);
            var csproj_str = File.read(csproj_path);

            csproj_str = Regex.Replace(csproj_str, "<AssemblyName>.*<\\/AssemblyName>", "", RegexOptions.Singleline);
            csproj_str = Regex.Replace(csproj_str, "<StartupObject>.*<\\/StartupObject>", "", RegexOptions.Singleline);
            csproj_str = csproj_str.Replace("..\\packages\\", "C:\\Selenium\\packages\\");
            csproj_str = csproj_str.Replace("    \r\n", "");

            csproj_str = csproj_str.Replace("</RootNamespace>\r\n", "</RootNamespace>\r\n    <AssemblyName>" + script_name + "</AssemblyName>\r\n");
            csproj_str = csproj_str.Replace("<PropertyGroup>\r\n  </PropertyGroup>", "<PropertyGroup>\r\n    <StartupObject>" + full_file_name + "</StartupObject>\r\n  </PropertyGroup>");

            if (File.Exists(test_csproj_path))
            {
                var file_delete_result = File.delete(project_path, script_name + ".csproj");

                if (file_delete_result == false)

                    StatusBarSetText("Deletion of " + test_csproj_path + " failed.");
            }

            var file_write_result = File.overwrite(test_csproj_path, csproj_str);

            if (file_write_result == false)
            {
                StatusBarSetText("Writing to " + test_csproj_path + " failed.");
                msbuild_exit_code = 1000;
            }
            else
            {
                StatusBarSetText(msbuild_path + "\\MSBuild.exe /m \"" + script_name + ".csproj\" > \"" + script_name + ".log\"");
                var _processStartInfo = new ProcessStartInfo();
                _processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _processStartInfo.WorkingDirectory = project_path;
                _processStartInfo.FileName = "CMD.exe";
                _processStartInfo.Arguments = "/c @\"" + msbuild_path + "\\MSBuild.exe\" /m \"" + script_name + ".csproj\" > \"" + script_name + ".log\"";
                var process = Process.Start(_processStartInfo);
                process.WaitForExit();
                msbuild_exit_code = process.ExitCode;
                StatusBarSetText("MSBuild exit code = " + msbuild_exit_code);

                File.Copy(project_debug_path + "\\" + script_name + ".exe", project_path + "\\" + target_exe);
                File.Copy(project_debug_path + "\\" + script_name + ".pdb", project_path + "\\" + target_pdb);
            }

            return msbuild_exit_code;

        }




        private void ScriptsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void NotRunButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void PassButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void FailButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ExcludeButton_Click(object sender, RoutedEventArgs e)
        {
        }



        void Main_Closing(object sender, CancelEventArgs e)
        {
            // stop the main loop
            timer.Change(Timeout.Infinite, Timeout.Infinite);

            if (!close_gui)
            {
                if (AutoSendResultsCheckBox.IsChecked == true)
                {
                    var result = MessageBoxResult.Yes;

                    if (!run_complete)
                    {
                        string msg = "Run aborted. Would you like to send partial results to TestRail?";
                        result = MessageBox.Show(msg, "CoPilot", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    }

                    if (result == MessageBoxResult.Yes)
                    {
                        timer = new System.Threading.Timer(MainLoop, null, 0, Timeout.Infinite);
                        close_gui = true;
                        e.Cancel = true;
                    }
                }
            }
        }


        private void myfunction()
        {
            //App.Current.Dispatcher.Invoke(new Action(() =>
            //{



            //  }));
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Debug(e.ExceptionObject.ToString());
        }
    }
}
