using SharedProject;
using SharedProject.Models;
using RoboSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using SharedProject.TestRail;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using BetterConsoleTables;

namespace runeg
{
    class Program
    {
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
        static DBConnect dbConnect;

        public static List<ExecutionGroupScript> ScriptsListViewCollection = new List<ExecutionGroupScript>();
        public static List<ExecutionGroupScript> PreviousScriptsListViewCollection = null;
        public static List<Project> project = null;
        public static List<ExecutionGroup> execution_group = null;
        private static List<string> host_first_script_deploy_list = new List<string>();
        private List<string> host_with_in_progress_script = new List<string>();

        private static string execution_group_script_ids_in_progress = "";
        private static int msbuild_exit_code = 0;
        private static string msbuild_path = "C:\\Program Files (x86)\\MSBuild\\14.0\\Bin";
        private static TableConfiguration TableConfigurationUnicodeHeader = TableConfiguration.Unicode();
        private static TableConfiguration TableConfigurationUnicodeNoHeader = TableConfiguration.Unicode();
        private static Table eg_table = null;
        private static Table egs_table = null;
        private static Table progress_table = null;
        private static int num_scripts_complete = 0;
        private static int num_included_scripts = 0;

        // Handlers for events that will update GUI controls in realtime (in background threads) ...
        static string solution_name = "";
        static string project_name = "";
        static DateTime start_date_time = DateTime.Now;


        static void Main(string[] args)
        {

            if (args.Length > 0)
            {
                mysql_host = args[0];
                mysql_user = args[1];
                mysql_pass = args[2];
                project_id = args[3];
                execution_group_id = args[4];
                iteration_name = args[5];
                test_environment_name = args[6];
                browserstack_runtime_host_name = args[7];
                deploy_and_build = args[8];
                source_control_username = args[9];
                source_control_password = args[10];
            }

            Log.ResetRunLog(project_id, execution_group_id);

            // catch unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            AppConfig.Open();

            if (!System.IO.Directory.Exists(msbuild_path))
            {
                msbuild_path = "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319";

                if (!System.IO.Directory.Exists(msbuild_path))

                    msbuild_path = "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319";
            }

            var dbConnect = new DBConnect("control_automation_machine");
            project = dbConnect.Select<Project>("SELECT schema_name, path, external_id, external_name, webhook_url, run_reports_url FROM project WHERE id = " + project_id + ";");
            solution_name = project[0].path.Substring(0, project[0].path.LastIndexOf("\\")).Substring(project[0].path.IndexOf("\\") + 1);
            project_name = project[0].path.Substring(project[0].path.LastIndexOf("\\") + 1);

            dbConnect = new DBConnect(project[0].schema_name);
            execution_group = dbConnect.Select<ExecutionGroup>("SELECT eg.name, eg.iteration_id, i.name AS iteration_name, eg.environment_id, e.name AS environment_name, eg.auto_stop_tests, eg.auto_send_results, eg.device_notifications, eg.external_plan_id, eg.external_plan_name, eg.external_exe_rec_run_id, eg.external_exe_rec_run_name FROM execution_group eg, iteration i, environment e WHERE eg.iteration_id = i.id AND eg.environment_id = e.id AND eg.id = " + execution_group_id + ";");

            TableConfigurationUnicodeHeader.hasHeaderRow = true;
            TableConfigurationUnicodeNoHeader.hasHeaderRow = false;

            eg_table = new Table(TableConfigurationUnicodeNoHeader, new[] {
                new ColumnHeader("Project [ID] Name", Alignment.Left),
                new ColumnHeader("[" + project_id + "] " + project_name, Alignment.Left)
            });

            eg_table.AddRow("Exe Group [ID] Name", "[" + execution_group_id + "] " + project_name);
            eg_table.AddRow("Iteration Name", iteration_name);
            eg_table.AddRow("Environment Name", test_environment_name);
            eg_table.AddRow("External Project [ID] Name", "[" + project[0].external_id + "] " + project[0].external_name);
            eg_table.AddRow("External Plan [ID] Name", "[" + execution_group[0].external_plan_id + "] " + execution_group[0].external_plan_name);
            eg_table.AddRow("External Exe Rec / Run [ID] Name", "[" + execution_group[0].external_exe_rec_run_id + "] " + execution_group[0].external_exe_rec_run_name);
            eg_table.AddRow("Auto Send Results", execution_group[0].auto_send_results == 1 ? "Y" : "");
            eg_table.AddRow("Auto Stop Tests", execution_group[0].auto_stop_tests == 1 ? "Y" : "");
            eg_table.AddRow("Device Notifications", execution_group[0].device_notifications == 1 ? "Y" : "");
            Console.Write(eg_table.ToString());
            Console.WriteLine();
            Debug.Write(eg_table.ToString());
            Debug.WriteLine("");

            egs_table = new Table(TableConfigurationUnicodeHeader, new[] {
                new ColumnHeader("Script [ID] Long Name", Alignment.Left),
                new ColumnHeader("Host Name", Alignment.Left),
                new ColumnHeader("Sel", Alignment.Left),
                new ColumnHeader("Excl", Alignment.Center),
                new ColumnHeader("State      ", Alignment.Left)
            });

            progress_table = new Table(TableConfiguration.Simple(), new[] {
                new ColumnHeader("Script [ID] Short Name                  ", Alignment.Left),
                new ColumnHeader("State      ", Alignment.Left),
                new ColumnHeader("Start Date     ", Alignment.Left),
                new ColumnHeader("End Date       ", Alignment.Left),
                new ColumnHeader("Progress", Alignment.Left)
            });


            dbConnect = new DBConnect(project[0].schema_name);
            ScriptsListViewCollection = dbConnect.Select<ExecutionGroupScript>("SELECT egs.id, m.host_name, egs.script_id, s.name AS script_name, egs.selector, egs.post_run_delay, egs.state, egs.excluded, egs.start_date_time, egs.end_date_time, egs.em_comment, egs.shared_folder_1, egs.order_id FROM execution_group_script egs, script s, control_automation_machine.machine m WHERE egs.script_id = s.id AND egs.machine_id = m.id AND egs.execution_group_id = " + execution_group_id + " ORDER BY egs.order_id ASC;");

            // make the initial state of all scripts "Not Run"

            if (PreviousScriptsListViewCollection == null)
            {
                foreach (var eachItem in ScriptsListViewCollection)
                {
                    eachItem.state = "Not Run";
                    eachItem.start_date_time = null;
                    eachItem.end_date_time = null;
                }

                PreviousScriptsListViewCollection = ScriptsListViewCollection.CloneList();
            }

            // Display table

            foreach (var eachItem in ScriptsListViewCollection)
            {
                var excluded = "";

                if (eachItem.excluded == true)

                    excluded = "Y";

                egs_table.AddRow("[" + eachItem.id + "] " + eachItem.script_name, eachItem.host_name, eachItem.selector, excluded, "Not Run");
            }

            Console.Write(egs_table.ToString());
            Console.WriteLine();
            Console.Write(progress_table.ToString());
            Debug.Write(egs_table.ToString());
            Debug.WriteLine("");
            Debug.Write(progress_table.ToString());

            dbConnect = new DBConnect(project[0].schema_name);
            dbConnect.Update("UPDATE execution_group_script SET state = 'Not Run', start_date_time = NULL, end_date_time = NULL WHERE execution_group_id = '" + execution_group_id + "' AND excluded = 0;");

            // if deploy and build is required

            if (deploy_and_build.ToInt() == 1 || deploy_and_build.ToInt() == 2)
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
                        tmp_sql_case_clause = tmp_sql_case_clause + " when host_name = '" + eachItem.host_name + "' then '" + source_control_username + "," + source_control_password + ",https://tfscls.janison.com.au:8081/tfs/clscollection," + solution_name + "," + project_name + "," + test_environment_name + ",1,1," + iteration_name + "," + test_environment_name + "," + browser_name + "," + eachItem.script_name + "," + eachItem.id + ",1," + project[0].schema_name + "'";
                    }
                }

                // update the database with all host names

                if (tmp_host_names.Length > 0)
                {
                    tmp_sql_case_clause = tmp_sql_case_clause.Replace("\\", "\\\\");

                    // Instruct the adapter on that machine to deploy and build the project

                    dbConnect = new DBConnect("control_automation_machine");
                    dbConnect.Update("UPDATE machine SET source_control_args = (case" + tmp_sql_case_clause + " end) WHERE host_name IN (" + tmp_host_names + ");");
                }
            }

            // Perform the build and deploy process before running any scripts

            if (deploy_and_build.ToInt() == 3)
            {
                foreach (var eachItem in ScriptsListViewCollection)
                {
                    if (!host_first_script_deploy_list.Any(str => str.Equals(eachItem.host_name)) && (eachItem.excluded == null || eachItem.excluded == false))
                    {
                        host_first_script_deploy_list.Add(eachItem.host_name);
                        eachItem.state = "Deploying";
                    }
                }

                var exit_code = C_Sharp_Executable_Build(project[0].path, "Executable\\Run.cs");

                if (exit_code != 0)
                {
                    System.Threading.Thread.Sleep(3000);
                    ClosingHandler();
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
                    }
                }
            }


            // MainLoop

            while (true)
            {
                // if a deploy and build is occurring before the run

                if (deploy_and_build.ToInt() == 1 || deploy_and_build.ToInt() == 2)
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
                                ClosingHandler();
                            }

                            host_first_script_deploy_list.Remove(eachItem.host_name);
                        }
                    }

                    if (host_first_script_deploy_list.Count < 1)
                    {
                        deploy_and_build = "0";
                    }
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
                        dbConnect = new DBConnect(project[0].schema_name);
                        var scripts = dbConnect.Select<ExecutionGroupScript>("SELECT id, state, start_date_time, end_date_time FROM execution_group_script WHERE id IN (" + execution_group_script_ids_in_progress + ");");

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
                                            var now = DateTime.Now;
                                            dbConnect = new DBConnect(project[0].schema_name);
                                            dbConnect.Update("UPDATE execution_group_script SET state = 'In Progress' WHERE id = " + ScriptsListViewCollection[i].id + ";");
                                            ScriptsListViewCollection.Single(x => x.id == ScriptsListViewCollection[i].id).state = "In Progress";
                                            var script = ScriptsListViewCollection.Single(x => x.id == ScriptsListViewCollection[i].id);

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

                                            machine.run_ids.Add(new Run { p = project_id, eg = execution_group_id, egFst = execution_group_first_run, egs = ScriptsListViewCollection[i].id, egsDel = ScriptsListViewCollection[i].post_run_delay });
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

                                    var now = DateTime.Now;
                                    dbConnect = new DBConnect(project[0].schema_name);
                                    dbConnect.Update("UPDATE execution_group_script SET state = 'In Progress', start_date_time = '" + now.ToString("yyyy-MM-dd HH:mm:ss") + "' WHERE id = " + ScriptsListViewCollection[i].id + ";");
                                    ScriptsListViewCollection.Single(x => x.id == ScriptsListViewCollection[i].id).state = "In Progress";
                                    ScriptsListViewCollection.Single(x => x.id == ScriptsListViewCollection[i].id).start_date_time = now;
                                    var script = ScriptsListViewCollection.Single(x => x.id == ScriptsListViewCollection[i].id);

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

                                    machine.run_ids.Add(new Run { p = project_id, eg = execution_group_id, egFst = execution_group_first_run, egs = ScriptsListViewCollection[i].id, egsDel = ScriptsListViewCollection[i].post_run_delay });
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

                    var auto_archive_script_logs = "0";

                    foreach (var machine in machine_list)
                    {
                        // StatusBarSetText("Please Wait. Updating execution group script " + eachItem.id + " to 'In Progress'");
                        dbConnect = new DBConnect("control_automation_machine");
                        dbConnect.Update("UPDATE machine SET run_ids = '" + JsonConvert.SerializeObject(machine.run_ids) + "', run_auto_archive_script_logs = " + auto_archive_script_logs + " WHERE host_name = '" + machine.host_name + "';");
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

                        dbConnect = new DBConnect(project[0].schema_name);
                        dbConnect.Update("UPDATE execution_group SET start_date_time = '" + start_date_time.ToString("yyyy-MM-dd HH:mm:ss") + "', end_date_time = '" + execution_group[0].end_date_time.ToString("yyyy-MM-dd HH:mm:ss") + "', result = '" + tmp_overall_status + "' WHERE id = " + execution_group_id + ";");

                        ClosingHandler();
                    }
                }

                WriteProgress();

                // Set timer to trigger this method in another 10 seconds
                Thread.Sleep(10000);

                PreviousScriptsListViewCollection = ScriptsListViewCollection.CloneList();

            }

        }


        private static int C_Sharp_Executable_Build(string solution_and_project_path, string full_file_name)
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

            var csproj_str = SharedProject.File.read(csproj_path);
            csproj_str = Regex.Replace(csproj_str, "<AssemblyName>.*<\\/AssemblyName>", "", RegexOptions.Singleline);
            csproj_str = Regex.Replace(csproj_str, "<StartupObject>.*<\\/StartupObject>", "", RegexOptions.Singleline);
            csproj_str = csproj_str.Replace("..\\packages\\", "C:\\Selenium\\packages\\");
            csproj_str = csproj_str.Replace("    \r\n", "");
            csproj_str = csproj_str.Replace("</RootNamespace>\r\n", "</RootNamespace>\r\n    <AssemblyName>" + script_name + "</AssemblyName>\r\n");
            csproj_str = csproj_str.Replace("<PropertyGroup>\r\n  </PropertyGroup>", "<PropertyGroup>\r\n    <StartupObject>" + full_file_name + "</StartupObject>\r\n  </PropertyGroup>");

            if (SharedProject.File.Exists(test_csproj_path))
            {
                var file_delete_result = SharedProject.File.delete(project_path, script_name + ".csproj");
            }

            var file_write_result = SharedProject.File.overwrite(test_csproj_path, csproj_str);

            if (file_write_result == false)
            {
                msbuild_exit_code = 1000;
            }
            else
            {
                var _processStartInfo = new ProcessStartInfo();
                _processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _processStartInfo.WorkingDirectory = project_path;
                _processStartInfo.FileName = "CMD.exe";
                _processStartInfo.Arguments = "/c @\"" + msbuild_path + "\\MSBuild.exe\" /m \"" + script_name + ".csproj\" > \"" + script_name + ".log\"";
                var process = Process.Start(_processStartInfo);
                process.WaitForExit();
                msbuild_exit_code = process.ExitCode;

                SharedProject.File.Copy(project_debug_path + "\\" + script_name + ".exe", project_path + "\\" + target_exe);
                SharedProject.File.Copy(project_debug_path + "\\" + script_name + ".pdb", project_path + "\\" + target_pdb);
            }

            return msbuild_exit_code;

        }


        private static void robocopy(string source_path, string target_path, string filename = "")
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
            }
            else
            {
                copy.CopyOptions.Mirror = true;
            }

            var copy_task = copy.Start();
            copy_task.Wait();

        }



        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Debug(e.ExceptionObject.ToString());
        }



        static void WriteProgress()
        {

            // Determine progress (percent complete so far)

            int progress_pcnt = 0;
            num_scripts_complete = 0;
            num_included_scripts = 0;

            foreach (var eachItem in ScriptsListViewCollection)
            {
                if (eachItem.state.Equals("Pass") || eachItem.state.Equals("Fail"))

                    num_scripts_complete++;

                if (eachItem.excluded == null || eachItem.excluded == false)

                    num_included_scripts++;
            }

            progress_pcnt = (int)((double)num_scripts_complete / (double)num_included_scripts * 100);

            // Find changes in the script list compared to last (previous) iteration and output the changes / updates

            var eg_script_changes = ScriptsListViewCollection.Except(PreviousScriptsListViewCollection, new ExecutionGroupScriptEqualityComparer()).ToList();

            foreach (var each in eg_script_changes)
            {
                var start_date_time = "";
                var end_date_time = "";

                if (each.start_date_time != null)

                    start_date_time = ((DateTime)each.start_date_time).ToString("dd MMM yy HH:mm");

                if (each.end_date_time != null)

                    end_date_time = ((DateTime)each.end_date_time).ToString("dd MMM yy HH:mm");

                progress_table.AddWriteRow("[" + each.id + "] " + each.script_name.Substring(each.script_name.LastIndexOf('.') + 1), each.state, start_date_time, end_date_time, progress_pcnt + "%");
            }

        }




        static void ClosingHandler()
        {
            WriteProgress();

            if (execution_group[0].auto_send_results == 1)
            {
                Console.WriteLine();
                Debug.WriteLine("");

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

                Console.WriteLine("TestRail GET get_statuses");
                Debug.WriteLine("TestRail GET get_statuses");
                var statuses = (JArray)client.SendGet("get_statuses");

                Console.WriteLine("TestRail GET get_tests/" + execution_group[0].external_exe_rec_run_id);
                Debug.WriteLine("TestRail GET get_tests/" + execution_group[0].external_exe_rec_run_id);
                var tests = (JObject)client.SendGet("get_tests/" + execution_group[0].external_exe_rec_run_id);

                dbConnect = new DBConnect(project[0].schema_name);
                var test_case_results = dbConnect.Select<TestCaseResult>("SELECT GROUP_CONCAT(egs.id) AS execution_group_script_ids, GROUP_CONCAT(s.name) AS test_case_script_names, LEFT(s.name, LENGTH(s.name) - LOCATE('_', REVERSE(s.name))) AS test_case_name, GROUP_CONCAT(egs.state) AS test_case_states, SUM(timestampdiff(SECOND, egs.start_date_time, egs.end_date_time)) AS elapsed FROM execution_group_script egs, script s WHERE egs.excluded = 0 AND egs.script_id = s.id AND egs.execution_group_id = " + execution_group_id + " GROUP BY test_case_name ORDER BY s.name ASC;");

                foreach (var test_case_result in test_case_results)
                {
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

                    // TestRail Results

                    var test_id = tests["tests"].SelectToken("$.[?(@.custom_auto_script_ref == '" + test_case_result.test_case_name + "')]")["id"].ToString().ToInt();
                    var status_id = statuses.SelectToken("$.[?(@.label == '" + test_case_state + "')]")["id"].ToString().ToInt();

                    results_json.results.Add(new
                    {
                        test_id = test_id,
                        status_id = status_id,
                        comment = comment,
                        elapsed = test_case_result.elapsed + "s"
                    });

                }

                // Pass the CoPilot results into the TestRail run

                Console.WriteLine("TestRail POST add_results/" + execution_group[0].external_exe_rec_run_id);
                Debug.WriteLine("TestRail POST add_results/" + execution_group[0].external_exe_rec_run_id);
                var response_string = client.SeanPost("add_results/" + execution_group[0].external_exe_rec_run_id, results_json.ToJSONString());
            }

            System.Environment.Exit(0);


        }
    }
}
