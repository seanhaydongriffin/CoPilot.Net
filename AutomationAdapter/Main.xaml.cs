using JCS;
using Microsoft.Win32;
using Newtonsoft.Json;
using SharedProject;
using SharedProject.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;

namespace AutomationAdapter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Main : Window
    {
        private string current_host_name = System.Environment.MachineName;
        private int run_auto_archive_script_logs = 0;
        private string win_version = OSVersionInfo.Name;
        private bool deploy_vc = false;
        private int exit_code = 0;
        private List<Project> project_arr = null;
        private string project_path = "";
        private List<ExecutionGroupScript2> execution_group_script_arr = null;
        private List<Run> run = null;
        private string log_xml = "";
        private string script_name = "";
        private string script_name_short = "";
        private string device_notification_api_key = "";
        private System.Threading.Timer timer;
        private int msbuild_exit_code = 0;
        private string msbuild_path = "C:\\Program Files (x86)\\MSBuild\\14.0\\Bin";
        public static TextBlock statusbar;

        // Handlers for events that will update GUI controls in realtime (in background threads) ...
        public static event EventHandler MessagesUpdate;
        public static event EventHandler StatusBarUpdate;

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

            statusbar = StatusBarText;

            // Defining the background event handlers for GUI control updates in realtime ...

            Main.MessagesUpdate += (s, e) => {
                Dispatcher.Invoke((Action)delegate () {
                    // update UI
                    MessagesTextBox.ScrollToEnd();
                    MessagesTextBox.LogMessage(s.ToString());
                });
            };

            Main.StatusBarUpdate += (s, e) => {
                Dispatcher.Invoke((Action)delegate () {
                    // update UI
                    Main.statusbar.Text = s.ToString(); // "Status: Connected";
                });
            };

            if (!System.IO.Directory.Exists(msbuild_path))
            {
                msbuild_path = "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319";

                if (!System.IO.Directory.Exists(msbuild_path))

                    msbuild_path = "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319";
            }

            // Check if running as administrator

            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);

            if (principal.IsInRole(WindowsBuiltInRole.Administrator))

                LogMessage("Running as Administrator");
            else

                LogMessage("Running as User");

            LogMessage("Using DB hostname \"" + AppConfig.Get("DatabaseHostname") + "\" username \"" + AppConfig.Get("DatabaseUsername") + "\" encrypted password \"" + AppConfig.Get("DatabasePassword").Truncate(5, "...") + "\" source control \"" + AppConfig.Get("SourceControlProduct") + "\" this hostname \"" + current_host_name + "\"");

            // below is important to activate all the bindings from the XAML to the data set above
            this.DataContext = this;

            // Create a timer object, but don't start anything yet
            timer = new System.Threading.Timer(dispatcherTimer_Tick, null, 0, Timeout.Infinite);
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)

                Close();
        }

        // A "static" method to call the event handler to update the GUI in realtime ...

        public static void LogMessage(string message)
        {
            Main.MessagesUpdate(message, null);
            Log.Debug(message);
        }

        // A "static" method to call the event handler to update the GUI in realtime ...

        public static void StatusBarSetText(string message)
        {
            Main.StatusBarUpdate(message, null);
        }

        private void dispatcherTimer_Tick(object state)
        {
            exit_code = 0;
            List<Machine> machine_arr = null;
            var dbConnect = new DBConnect("control_automation_machine");

            if (App.run_ids.Length == 0)
            {
                Main.StatusBarSetText("Status: Connected");

                // Main.StatusBarSetText("Please Wait. Getting the run values for this machine");
                machine_arr = dbConnect.Select<Machine>("SELECT run_ids, run_auto_archive_script_logs, source_control_args FROM machine WHERE host_name = '" + current_host_name + "';");

                if (machine_arr == null)
                {
                    Main.StatusBarSetText("Status: Error. See log file for more information.");
                    // Set timer to trigger this method in another 10 seconds
                    timer.Change(10 * 1000, Timeout.Infinite);
                    return;
                }

            }

            if (machine_arr != null && machine_arr.Count > 0 && machine_arr[0].source_control_args != null && machine_arr[0].source_control_args.Length > 0 && machine_arr[0].source_control_args.IsInt() == false && !machine_arr[0].source_control_args.Equals("in progress"))
            {
                Log.Debug("Getting the run values for this machine ...");

                //Main.StatusBarSetText("Please Wait. Getting the run values for this machine");
                Main.StatusBarSetText("Status: Connected");

                dbConnect = new DBConnect("control_automation_machine");
                var result = dbConnect.Update("UPDATE machine SET source_control_args = 'in progress' WHERE host_name = '" + current_host_name + "';");

                if (result == false)

                    Main.StatusBarSetText("Status: Error. See log file for more information.");

                var source_control_arg = machine_arr[0].source_control_args.Split(',');
                var source_control_username = "";
                var source_control_decrypted_password = "";

                if (source_control_arg[0].Length > 0 && source_control_arg[1].Length > 0)
                {
                    LogMessage("Using " + AppConfig.Get("SourceControlProduct") + " credentials from the command line");
                    source_control_username = source_control_arg[0];
                    source_control_decrypted_password = source_control_arg[1];
                }
                else
                {
                    LogMessage("Using " + AppConfig.Get("SourceControlProduct") + " credentials from the local Settings of CoPilot");
                    source_control_username = AppConfig.Get(AppConfig.Get("SourceControlProduct") + "Username");

                    if (AppConfig.Get(AppConfig.Get("SourceControlProduct") + "Password").Length > 0)

                        source_control_decrypted_password = StringCipher.Decrypt(AppConfig.Get(AppConfig.Get("SourceControlProduct") + "Password"));
                }

                var source_control_url = source_control_arg[2];
                var source_control_solution_name = source_control_arg[3];
                var source_control_project_name = source_control_arg[4];
                var source_control_merged_folder_name = source_control_arg[5];
                deploy_vc = source_control_arg[6].ToInt().Equals(1) ? true : false;
                var position_merged_pools = source_control_arg[7];
                var iteration_name = source_control_arg[8];
                var environment_name = source_control_arg[9];
                var browser_name = source_control_arg[10];
                script_name = source_control_arg[11];
                var run_execution_group_script_id = source_control_arg[12];
                var run_execution_group_run_number = source_control_arg[13];
                var mysql_db = source_control_arg[14];
                project_path = "R:\\" + source_control_solution_name + "\\" + source_control_project_name;

                Log.Debug("source_control_url = " + source_control_url + ", source_control_solution_name = " + source_control_solution_name + ", source_control_project_name = " + source_control_project_name + ", source_control_merged_folder_name = " + source_control_merged_folder_name + ", deploy_vc = " + deploy_vc + ", position_merged_pools = " + position_merged_pools + ", iteration_name = " + iteration_name + ", environment_name = " + environment_name + ", browser_name = " + browser_name + ", script_name = " + script_name + ", run_execution_group_script_id = " + run_execution_group_script_id + ", run_execution_group_run_number = " + run_execution_group_run_number + ", mysql_db = " + mysql_db + ", project_path = " + project_path);

                if (deploy_vc)
                {
                    // if Git

                    if (AppConfig.Get("SourceControlProduct").Equals("Git"))
                    {
                        if (!File.Exists("C:\\Program Files\\Git\\bin\\git.exe"))

                            LogMessage("\"C:\\Program Files\\Git\\bin\\git.exe\" is required and does not exist");
                        else
                        {
                            var GitRepoName = AppConfig.Get("GitURL").Split('/').Last().Replace(".git", "");
                            var git_url_scheme = (new Uri(AppConfig.Get("GitURL"))).Scheme;
                            var git_url_right = AppConfig.Get("GitURL").Replace(git_url_scheme + "://", "");
                            var local_git_repo_exists = "";

                            if (System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName))
                            {
                                // add this repository to .gitconfig as safe

                                File.AddToIniFile(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\.gitconfig", "safe", "directory", (System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName).Replace(@"\", @"/"));
                                File.AddToIniFile(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\.gitconfig", "safe", "directory", (System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\Source\\Repos\\" + GitRepoName).Replace(@"\", @"/"));

                                // check if local Git repo already exists

                                LogMessage("\"C:\\Program Files\\Git\\bin\\git.exe\" rev-parse --is-inside-work-tree");
                                var outputStringBuilder = new StringBuilder();
                                var process = new Process();
                                process.StartInfo.WorkingDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName;
                                process.StartInfo.FileName = "C:\\Program Files\\Git\\bin\\git.exe";
                                process.StartInfo.Arguments = "rev-parse --is-inside-work-tree";
                                process.StartInfo.RedirectStandardOutput = true;
                                process.StartInfo.UseShellExecute = false;
                                process.OutputDataReceived += (sender, eventArgs) => outputStringBuilder.AppendLine(eventArgs.Data);
                                process.Start();
                                process.BeginOutputReadLine();
                                process.WaitForExit();
                                local_git_repo_exists = outputStringBuilder.ToString().Replace("\r\n", "");
                            }

                            if (!local_git_repo_exists.Equals("true"))
                            {
                                if (System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName + "\\Automation"))
                                {
                                    LogMessage("Removing dir " + System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName + "\\Automation");
                                    Directory.delete(new System.IO.DirectoryInfo(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName + "\\Automation"));
                                }

                                if (System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName))

                                    LogMessage("Warning - local Git repo folder exists and Git might fail to clone to it if populated.");

                                //LogMessage("Deploying to Git repository folder " + System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName);

                                //LogMessage("Removing dir C:\\Users\\auto\\source\\repos\\test-automation");
                                //Directory.delete(new System.IO.DirectoryInfo("C:\\Users\\auto\\source\\repos\\test-automation"));

                                // note "--sparse" is used below to support LFS which will ensure datapools are downloaded

//                                LogMessage("\"C:\\Program Files\\Git\\bin\\git.exe\" clone --sparse --no-checkout " + AppConfig.Get("GitURL"));
                                LogMessage("\"C:\\Program Files\\Git\\bin\\git.exe\" clone --sparse --no-checkout " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right);
                                var _processStartInfo = new ProcessStartInfo();
                                _processStartInfo.WorkingDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos";
                                _processStartInfo.FileName = "C:\\Program Files\\Git\\bin\\git.exe";
//                                _processStartInfo.Arguments = "clone --sparse --no-checkout " + AppConfig.Get("GitURL");
                                _processStartInfo.Arguments = "clone --sparse --no-checkout " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right;
                                var process2 = Process.Start(_processStartInfo);
                                process2.WaitForExit();
                            }

                            if (!System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName))

                                LogMessage("Local Git repo folder does not exist and sparse-checkout cannot be run.");
                            else
                            {
                                // remove the current git index to ensure full checkouts below
                                File.delete(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName + "\\.git", "index");

                                var automation_dir_existed = Directory.exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName + "\\Automation");

                                // note "sparse-checkout" is used below to support LFS which will ensure datapools are downloaded

                                string app = "cmd";
                                string args_for_logging = "";
                                string args_for_execution = "";

                                if (AppConfig.Get("SourceControlDeploymentBranch").Equals(""))
                                {
                                    //                                    args_for_logging = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " & git checkout & git merge origin/main";
                                    //                                    args_for_execution = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " & git checkout & git merge origin/main";
                                    //args_for_logging = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " & git stash & git stash drop & git merge -X theirs FETCH_HEAD";
                                    //args_for_execution = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " & git stash & git stash drop & git merge -X theirs FETCH_HEAD";
                                    //args_for_logging = "git init & git config core.sparseCheckout true & git remote add -f origin " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " & git fetch " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " & git checkout FETCH_HEAD";
                                    //args_for_execution = "git init & git config core.sparseCheckout true & git remote add -f origin " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " & git fetch " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " & git checkout FETCH_HEAD";

                                    //args_for_logging = "git init & git config core.sparseCheckout true & git remote add -f origin " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " & echo Automation/" + source_control_solution_name + "/" + source_control_project_name + "/* > .git/info/sparse-checkout & git fetch " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " & git checkout FETCH_HEAD";
                                    //args_for_execution = "git init & git config core.sparseCheckout true & git remote add -f origin " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " & echo Automation/" + source_control_solution_name + "/" + source_control_project_name + "/* > .git/info/sparse-checkout & git fetch " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " & git checkout FETCH_HEAD";
                                    args_for_logging = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " & git reset --hard FETCH_HEAD";
                                    args_for_execution = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " & git reset --hard FETCH_HEAD";
                                }
                                else
                                {
                                    //                                    args_for_logging = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " +" + AppConfig.Get("SourceControlDeploymentBranch") + " & git checkout " + AppConfig.Get("SourceControlDeploymentBranch") + " & git merge origin/" + AppConfig.Get("SourceControlDeploymentBranch");
                                    //                                    args_for_execution = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " +" + AppConfig.Get("SourceControlDeploymentBranch") + " & git checkout " + AppConfig.Get("SourceControlDeploymentBranch") + " & git merge origin/" + AppConfig.Get("SourceControlDeploymentBranch");
                                    //args_for_logging = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " +" + AppConfig.Get("SourceControlDeploymentBranch") + " & git stash & git stash drop & git merge -X theirs FETCH_HEAD";
                                    //args_for_execution = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " +" + AppConfig.Get("SourceControlDeploymentBranch") + " & git stash & git stash drop & git merge -X theirs FETCH_HEAD";
                                    //args_for_logging = "git init & git config core.sparseCheckout true & git remote add -f origin " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " & git fetch " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " +" + AppConfig.Get("SourceControlDeploymentBranch") + " & git checkout FETCH_HEAD";
                                    //args_for_execution = "git init & git config core.sparseCheckout true & git remote add -f origin " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " & git fetch " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " +" + AppConfig.Get("SourceControlDeploymentBranch") + " & git checkout FETCH_HEAD";

                                    //args_for_logging = "git init & git config core.sparseCheckout true & git remote add -f origin " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " & echo Automation/" + source_control_solution_name + "/" + source_control_project_name + "/* > .git/info/sparse-checkout & git fetch " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " +" + AppConfig.Get("SourceControlDeploymentBranch") + " & git checkout FETCH_HEAD";
                                    //args_for_execution = "git init & git config core.sparseCheckout true & git remote add -f origin " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " & echo Automation/" + source_control_solution_name + "/" + source_control_project_name + "/* > .git/info/sparse-checkout & git fetch " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " +" + AppConfig.Get("SourceControlDeploymentBranch") + " & git checkout FETCH_HEAD";
                                    args_for_logging = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":<password>@" + git_url_right + " +" + AppConfig.Get("SourceControlDeploymentBranch") + " & git reset --hard FETCH_HEAD";
                                    args_for_execution = "git sparse-checkout set \"Automation/" + source_control_solution_name + "/" + source_control_project_name + "\" & git fetch " + git_url_scheme + "://" + source_control_username + ":" + source_control_decrypted_password + "@" + git_url_right + " +" + AppConfig.Get("SourceControlDeploymentBranch") + " & git reset --hard FETCH_HEAD";
                                }

                                LogMessage(args_for_logging);

                                using (Process process = new Process())
                                {
                                    process.StartInfo = new ProcessStartInfo(app, "/c " + args_for_execution)
                                    {
                                        UseShellExecute = false,
                                        WorkingDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName
                                    };

                                    process.Start();
                                    process.WaitForExit();
                                }

                                if (!automation_dir_existed)
                                {
                                    LogMessage("net share Automation=\"" + System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName + "\\Automation\"");
                                    Command2.DoProcessElevated("net", "share Automation=\"" + System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName + "\\Automation\"");

                                    //Subst.MapDrive('r', System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\source\\repos\\" + GitRepoName + "\\Automation");
                                }
                            }
                        }
                    }

                    // if TFVC

                    if (AppConfig.Get("SourceControlProduct").Equals("TFVC"))
                    {
                        LogMessage("Deploying to TFVC workspace");
                        LogMessage("Removing dir c:\\auto\\$tf");
                        Directory.delete(new System.IO.DirectoryInfo("c:\\auto\\$tf"));
                        LogMessage("Removing dir c:\\auto\\" + source_control_solution_name + "\\$tf");
                        Directory.delete(new System.IO.DirectoryInfo("c:\\auto\\" + source_control_solution_name + "\\$tf"));
                        LogMessage("Removing dir c:\\auto\\" + source_control_solution_name + "\\" + source_control_project_name);
                        //Directory.delete(new System.IO.DirectoryInfo("c:\\auto\\" + source_control_solution_name + "\\" + source_control_project_name));

                        LogMessage("C:\\TEE-CLC-14.123.1\\tf.cmd get \"$/CLS/QA/Automation/Alternate/" + source_control_solution_name + "/" + source_control_project_name + "\" /force /recursive /login:" + source_control_username + ",<password> \"c:\\auto\\" + source_control_solution_name + "\\" + source_control_project_name + "\"");

                        var _processStartInfo = new ProcessStartInfo();
                        _processStartInfo.WorkingDirectory = "C:\\auto";
                        _processStartInfo.FileName = "C:\\TEE-CLC-14.123.1\\tf.cmd";
                        _processStartInfo.Arguments = "get \"$/CLS/QA/Automation/Alternate/" + source_control_solution_name + "/" + source_control_project_name + "\" /force /recursive /login:" + source_control_username + ',' + source_control_decrypted_password + " \"c:\\auto\\" + source_control_solution_name + "\\" + source_control_project_name + "\"";
                        var process = Process.Start(_processStartInfo);
                        process.WaitForExit();
                    }
                }

                if (position_merged_pools.ToInt() == 1)
                {
                    LogMessage("Copying " + project_path + "\\Data\\Pools\\Merged\\" + source_control_merged_folder_name + "\\*.csv to " + project_path + "\\Data\\Pools");

                    if (!System.IO.Directory.Exists(project_path + "\\Data\\Pools\\Merged\\" + source_control_merged_folder_name))

                        LogMessage("Folder \"" + project_path + "\\Data\\Pools\\Merged\\" + source_control_merged_folder_name + "\" does not exist.");
                    else
                    {
                        try
                        {
                            File.CopyFiles(project_path + "\\Data\\Pools\\Merged\\" + source_control_merged_folder_name, project_path + "\\Data\\Pools", true, "*.csv");
                        }
                        catch (Exception e)
                        {
                            LogMessage("Exception = " + e.ToString());
                        }

                        LogMessage("Files copied.");

                        if (browser_name.Length > 0 && File.Exists(project_path + "\\Data\\Pools\\" + environment_name + " - Browser.csv"))
                        {
                            LogMessage("Updating " + project_path + "\\Data\\Pools\\" + environment_name + " - Browser.csv with Browser Name " + browser_name);

                            //               _FileReadToArray($project_path & "\Data\Pools\" & $environment_name & " - Browser.csv", $browser_datapool_arr, 0)

                            //for $i = 1 to (UBound($browser_datapool_arr) - 1)

                            // $browser_datapool_arr[$i] = StringRegExpReplace($browser_datapool_arr[$i], "([^,]+)", $browser_name, 1)
                            //Next

                            //_FileWriteFromArray($project_path & "\Data\Pools\" & $environment_name & " - Browser.csv", $browser_datapool_arr)
                        }
                    }
                }

                if (deploy_vc)
                {
                    LogMessage("C_Sharp_Executable_Build(" + project_path + ", Executable\\Run.cs");
                    exit_code = C_Sharp_Executable_Build(project_path, "Executable\\Run.cs");
                }

                Main.StatusBarSetText("Status: Connected");
                dbConnect = new DBConnect("control_automation_machine");
                result = dbConnect.Update("UPDATE machine SET source_control_args = '" + exit_code + "' WHERE host_name = '" + current_host_name + "';");

                if (result == false)

                    Main.StatusBarSetText("Status: Error. See log file for more information.");

                //Local $response_filename = "S:\Responses\" & $current_host_name & ".ini"
                //FileDelete($response_filename)
                //FileWrite($response_filename, "")

                if (exit_code != 0)
                {
                    var log_xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                                    "<log>\n" +
                                    "<script_name>" + script_name + "</script_name>\n" +
                                    "<start_date_time>07 Sep 2016 10:00:00</start_date_time>\n" +
                                    "<end_date_time>07 Sep 2016 12:00:00</end_date_time>\n" +
                                    "<result>PASS</result>\n" +
                                    "<first_exception_message>bad</first_exception_message>\n" +
                                    "<msg><date_time>" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</date_time><result>FAIL</result><text>Failed deploy and build with exit code " + exit_code + "</text></msg>\n" +
                                    "</log>";

                    if (!File.Exists(project_path + "_local_logs\\" + script_name))
                    {
                        var result2 = Directory.create(project_path + "_local_logs\\" + script_name);

                        if (result2 == false)

                            LogMessage("Failed to create directory " + project_path + "_local_logs\\" + script_name);
                    }

                    File.delete(project_path + "_local_logs\\" + script_name, "*.*");
                    File.overwrite(project_path + "_local_logs\\" + script_name + "\\log.xml", log_xml);
                }
            }

            if (App.run_ids.Length > 0 || (machine_arr.Count > 0 && machine_arr[0].run_ids != null && machine_arr[0].run_ids.Length > 0))
            {
                // remove the run values from the database (so this isn't run again)
                Main.StatusBarSetText("Status: Connected");
                dbConnect = new DBConnect("control_automation_machine");
                var result = dbConnect.Update("UPDATE machine SET run_ids = null, run_auto_archive_script_logs = 0 WHERE host_name = '" + current_host_name + "';");

                if (result == false)

                    Main.StatusBarSetText("Status: Error. See log file for more information.");

                if (App.run_ids.Length > 0)
                {
                    run = new List<Run>();
                    var single_run = JsonConvert.DeserializeObject<SharedProject.Models.Run>(App.run_ids);
                    run.Add(single_run);
                }

                if (machine_arr != null && machine_arr[0].run_ids.Length > 0)

                    run = JsonConvert.DeserializeObject<List<SharedProject.Models.Run>>(machine_arr[0].run_ids);

                LogMessage("Proj ID = " + run[0].p + ", Exe Grp ID = " + run[0].eg + ", First Run = " + run[0].egFst + ", Script ID = " + run[0].egs + ", Pre-Run Delay = " + run[0].egsDel);

                if (run.Count > 1)
                {
                    for (int i = 1; i < run.Count; i++)
                    {
                        var debug = "\"" + current_host_name + "\" \"" + JsonConvert.SerializeObject(run[i]).Replace("\"", "\\\"") + "\" " + run_auto_archive_script_logs.ToString();
                        Process.Start(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\AutomationAdapter.exe", "\"" + current_host_name + "\" \"" + JsonConvert.SerializeObject(run[i]).Replace("\"", "\\\"") + "\" " + run_auto_archive_script_logs.ToString());
                    }
                }

                // if a pre run delay is requested
                if (run[0].egsDel.Length > 0)
                {
                    var pre_run_delay_part = run[0].egsDel.Split(':');
                    var pre_run_delay_end_date_time = DateTime.Now;
                    pre_run_delay_end_date_time = DateTime.Now.AddMinutes(pre_run_delay_part[0].ToDouble());
                    pre_run_delay_end_date_time = DateTime.Now.AddSeconds(pre_run_delay_part[1].ToDouble());

                    LogMessage("Pre Run Delay of " + pre_run_delay_part[0] + " mins and " + pre_run_delay_part[1] + " secs until " + pre_run_delay_end_date_time);
                    System.Threading.Thread.Sleep((pre_run_delay_part[0].ToInt() * 60000) + (pre_run_delay_part[1].ToInt() * 1000));
                }

                // get the project details - path and mysql db
                Main.StatusBarSetText("Status: Connected");
                dbConnect = new DBConnect("control_automation_machine");
                project_arr = dbConnect.Select<Project>("SELECT schema_name, path, external_app FROM project WHERE id = " + run[0].p);

                if (project_arr == null)
                {
                    Main.StatusBarSetText("Status: Error. See log file for more information.");
                    // Set timer to trigger this method in another 10 seconds
                    timer.Change(10 * 1000, Timeout.Infinite);
                    return;
                }

                if (project_path.Length == 0 && project_arr[0].path.Length > 0)

                    project_path = project_arr[0].path;

                // get the script id, script name, project path and post run delay
                Main.StatusBarSetText("Status: Connected");
                dbConnect = new DBConnect(project_arr[0].schema_name);
                execution_group_script_arr = dbConnect.Select<ExecutionGroupScript2>("SELECT s.name AS scriptName, s.external_id AS scriptExternalId, i.name AS iterationName, e.name AS environmentName, egs.post_run_delay AS postRunDelay, eg.name, eg.device_notifications AS deviceNotifications, egs.shared_folder_1 AS sharedFolder1 FROM execution_group_script egs, script s, execution_group eg, iteration i, environment e WHERE egs.id = " + run[0].egs + " AND egs.script_id = s.id AND egs.execution_group_id = eg.id AND eg.iteration_id = i.id AND eg.environment_id = e.id;");

                if (execution_group_script_arr == null)
                {
                    Main.StatusBarSetText("Status: Error. See log file for more information.");
                    // Set timer to trigger this method in another 10 seconds
                    timer.Change(10 * 1000, Timeout.Infinite);
                    return;
                }

                if (script_name.Length == 0 && execution_group_script_arr[0].scriptName.Length > 0)

                    script_name = execution_group_script_arr[0].scriptName;

                // wipe the existing local logs folder
                File.delete(project_path + "_local_logs\\" + script_name, "*.*");

                if (win_version.Equals("Windows 7") || win_version.Equals("Windows 8.1"))
                {
                    // Set the command prompt size to 140 characters wide and 27 characters high
                    SharedProject.Registry.CreateCurrentUserSubKeyAndValue("", "Console", "ScreenBufferSize", 19660940, RegistryValueKind.DWord);
                    SharedProject.Registry.CreateCurrentUserSubKeyAndValue("", "Console", "WindowSize", 1638540, RegistryValueKind.DWord);

                    // Set the command prompt position to 540 pixels y &100 pixels x
                    // 021C0064
                    // 023A0064

                    SharedProject.Registry.CreateCurrentUserSubKeyAndValue("", "Console", "WindowPosition", 35389540, RegistryValueKind.DWord);
                    // RegWrite("HKEY_CURRENT_USER\Console", "WindowPosition", "REG_DWORD", "‭59703530‬")
                }

                // Map the S drive to the shared folder 1 (if set)

                if (execution_group_script_arr[0].sharedFolder1 != null && execution_group_script_arr[0].sharedFolder1.Length > 0)
                {
                    LogMessage("Flushing DNS cache to improve shared folder mapping");

                    Network.FlushCache();
                    var S_drive_exists = false;

                    for (var i = 1; i <= 120 && !S_drive_exists; i++)
                    {
                        LogMessage("Mapping S: to Shared Folder \"" + execution_group_script_arr[0].sharedFolder1 + "\" - attempt " + i);

//                        Network.MapNetworkDrive("S", execution_group_script_arr[0].sharedFolder1);
                        Command2.DoProcess("net", "use /D S:");
                        Command2.DoProcess("net", @"use S: " + execution_group_script_arr[0].sharedFolder1);

                        S_drive_exists = Drive.Exists('S');

                        if (!S_drive_exists)

                            Thread.Sleep(1000);
                    }
                }

                LogMessage("Updating start time to the database");

                // update the test script start time in the execution_group_script
                Main.StatusBarSetText("Status: Connected");
                dbConnect = new DBConnect(project_arr[0].schema_name);
                result = dbConnect.Update("UPDATE execution_group_script SET start_date_time = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' WHERE id = " + run[0].egs + ";");

                if (result == false)

                    Main.StatusBarSetText("Status: Error. See log file for more information.");

                // execute the run and wait until it finishes

                // if an RFT project

                if (project_path.Contains(" FT project"))
                {

                }
                else
                {
                    // Check for a Cucumber Feature file
                    var feature_path = project_path + "\\" + script_name.Replace('.', '\\') + ".feature";

                    if (File.Exists(feature_path))
                    {

                    }
                    else
                    {
                        // Determine which language the script is authored in (i.e.Java, C#, etc)
                        var script_path_and_filename = script_name.Replace('.', '\\');
                        var script_full_path = project_path + "\\" + script_path_and_filename;

                        // if FileExists($script_full_path & ".cs") Then

                        // if this is the very first script this adapter is running for an execution group, then clean the selenium grid and build the Run program

                        if (run[0].egFst == 1)
                        {
                            var machines_to_clean = new string[] { current_host_name, current_host_name + "highsierra" };

                            foreach (var each_machine in machines_to_clean)
                            {
                                LogMessage("Cleaning the Selenium Grid on " + each_machine + " ...");
                                //   								$response = cURL_easy($each_machine & ":5555/wd/hub/sessions", "", 0, 0, "", "Content-Type: application/json", "", 0, 1, 0)

                            }

                            if (!deploy_vc && !File.Exists(project_path + "\\Executable\\Run.exe"))
                            {
                                LogMessage("Building the Run executable \"" + project_path + "\\Executable\\Run.exe\" ...");
                                C_Sharp_Executable_Build(project_path, "Executable\\Run.cs");
                            }
                        }

                        var exe = script_full_path + ".exe";
                        var pdb = script_full_path.Substring(0, script_full_path.LastIndexOf('\\')) + "\\Run.pdb";

                        for (int copy_attempt_num = 1; copy_attempt_num <= (60 * 5); copy_attempt_num++)
                        {
                            LogMessage("Copying \"" + project_path + "\\Executable\\Run.exe\" to \"" + exe + "\" Attempt #" + copy_attempt_num + " ...");
                            File.Copy(project_path + "\\Executable\\Run.exe", exe);
                            File.Copy(project_path + "\\Executable\\Run.pdb", pdb);

                            //                          var res = File.Exists(exe);
                            //                        LogMessage("File.Exists(\"" + exe + "\") returns " + res);

                            //                            if (File.Exists(exe))
                            if (File.Exists(exe) && File.Exists(pdb))

                                break;

                            System.Threading.Thread.Sleep(1000);
                        }

                        LogMessage("Executing \"" + exe + "\"");
                        var script_path_pos = script_path_and_filename.LastIndexOf('\\');
                        var script_path = script_path_and_filename.Substring(0, script_path_pos);

                        var _processStartInfo = new ProcessStartInfo();
                        _processStartInfo.WorkingDirectory = project_path + "\\" + script_path;
                        _processStartInfo.FileName = exe;
                        var process = Process.Start(_processStartInfo);
                        process.WaitForExit();
                        exit_code = process.ExitCode;

                        if (File.Exists(script_full_path + ".java"))
                        {

                        }



                    }

                }

                LogMessage("exit code = " + exit_code);

            }

            //            if (App.run_ids.Length > 0 || exit_code != 0 || (machine_arr != null && machine_arr[0].run_ids != null && machine_arr[0].run_ids.Length > 0))
            if (App.run_ids.Length > 0 || (machine_arr != null && machine_arr.Count > 0 && machine_arr[0].run_ids != null && machine_arr[0].run_ids.Length > 0))
            {
                var end_date_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // An exit code that is not 0 means abnormal termination of the run.
                // On some computers an exit code of - 1073741819 is generated when the automated script runs perfectly fine(ie.C# / .Net executable).
                // Running the same executable on another computer can return a proper exit code of 0.
                // There are discussions on the internet about this error but no good conclusions on the cause.  For now I'm excluding this code below to avoid false negatives.
                // Add this message to the end of the log...

                if (exit_code != 0 && exit_code != -1073741819)
                {
                    Log.Debug("Writing abnormal termination to the test log ...");
                    log_xml = File.read(project_path + "_local_logs\\" + script_name + "\\log.xml");
                    log_xml = log_xml.Replace("</log>", "<msg><date_time>" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</date_time><result>FAIL</result><text>Abnormal termination</text></msg>\n</log>");
                    File.overwrite(project_path + "_local_logs\\" + script_name + "\\log.xml", log_xml);
                }

                if (win_version.Equals("Windows 7") || win_version.Equals("Windows 8.1"))
                {
                    // Restore the command prompt size and position to normal
                    Log.Debug("Restoring the command prompt size and position to normal ...");
                    SharedProject.Registry.DeleteCurrentUserValue("Console", "WindowPosition");
                    SharedProject.Registry.CreateCurrentUserSubKeyAndValue("", "Console", "WindowSize", 1638480, RegistryValueKind.DWord);
                    SharedProject.Registry.CreateCurrentUserSubKeyAndValue("", "Console", "ScreenBufferSize", 19660880, RegistryValueKind.DWord);
                }

                // Recreating the remote logs path (on L drive)

                var remote_logs_path = "";

                //if (Drive.Exists('L'))
                //{
                //    remote_logs_path = project_path.Replace("R:", "L:") + "_remote_logs\\" + execution_group_script_arr[0].iterationName + "_" + execution_group_script_arr[0].environmentName + "\\" + script_name;
                //    Log.Debug("Recreating the remote logs path " + remote_logs_path + " ...");
                //    Directory.delete(new System.IO.DirectoryInfo(remote_logs_path));
                //    Directory.create(remote_logs_path);
                //}

                log_xml = "";

                // if an RFT project
                if (project_path.Contains(" FT project"))
                {
                    Log.Debug("Reading file " + project_path + "_logs\\" + script_name + "\\rational_ft_log.xml ...");
                    log_xml = File.read(project_path + "_logs\\" + script_name + "\\rational_ft_log.xml");
                }
                else
                {
                    Log.Debug("Reading file " + project_path + "_local_logs\\" + script_name + "\\log.xml ...");
                    log_xml = File.read(project_path + "_local_logs\\" + script_name + "\\log.xml");

                    if (remote_logs_path.Length > 0)
                    {
                        LogMessage("Copying \"" + project_path + "_local_logs\\" + script_name + "\" to \"" + remote_logs_path + "\"");

                        // copy the local logs content to the remote logs iteration &environment folder
                        File.CopyFiles(project_path + "_local_logs\\" + script_name, remote_logs_path, true, "*.xml");
                        File.CopyFiles(project_path + "_local_logs\\" + script_name, remote_logs_path, true, "*.png");
                        File.CopyFiles(project_path + "_local_logs\\" + script_name, remote_logs_path, true, "*.mp4");
                    }
                }


                // determine the result(pass or fail)
                var script_result = "Pass";

                // An exit code that is not 0 means abnormal termination of the run.
                //   On some computers an exit code of - 1073741819 is generated when the automated script runs perfectly fine(ie.C# / .Net executable).
                //   Running the same executable on another computer can return a proper exit code of 0.
                //   There are discussions on the internet about this error but no good conclusions on the cause.  For now I'm excluding this code below to avoid false negatives.

                if (log_xml.Length == 0 || log_xml.Contains("<result>FAIL</result>"))

                    script_result = "Fail";

                // get the custom fields for execution group scripts in the project

                //Local $custom_field_arr[0][2]
                //Local $tmp_arr = MySql_query_sean("SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '" & $mysql_db & "' AND TABLE_NAME = 'execution_group_script';", $mysql_db)

                //for $i = 1 to(UBound($tmp_arr) - 1)

                //    if StringInStr($tmp_arr[$i][0], "custom ") = 1 Then

                //        _ArrayAdd($custom_field_arr, StringMid($tmp_arr[$i][0], StringLen("custom ") + 1) & "|")
                //    EndIf
                //Next

                LogMessage("Updating results to the database");

                // update the message results in the test_case_script_result_script
                Main.StatusBarSetText("Status: Connected");
                dbConnect = new DBConnect(project_arr[0].schema_name);
                var result = dbConnect.Delete("DELETE FROM execution_group_script_log WHERE execution_group_script_id = " + run[0].egs + ";");

                if (result == false)

                    Main.StatusBarSetText("Status: Error. See log file for more information.");

                var failure_count = 0;
                var first_failure_message = "";
                var cam_log_xml = "";

                if (log_xml.Length > 0)
                {
                    // if an RFT project

                    if (project_path.Contains(" FT project"))
                    {

                    }
                    else
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(log_xml);
                        var log_msgs = doc.DocumentElement.SelectNodes("/log/msg");
                        var mysql_insert_values = "";

                        foreach (XmlNode log_msg in log_msgs)
                        {
                            var log_msg_date_time = log_msg.SelectSingleNode("date_time").InnerText;
                            var log_msg_result = log_msg.SelectSingleNode("result").InnerText;
                            var log_msg_text = log_msg.SelectSingleNode("text").InnerText;

                            // Single quotes must be replaced with doubles to work in mysql statements
                            log_msg_text = log_msg_text.Replace("'", "''");

                            // Single backslashes must be replaced with doubles to work in mysql statements
                            log_msg_text = log_msg_text.Replace("\\", "\\\\");

                            // truncate the msg text to fit in the database (2000 characters)
                            if (log_msg_text.Length > 2000)

                                log_msg_text = log_msg_text.Substring(0, 2000);

                            if (log_msg_result.Equals("FAIL"))
                            {
                                script_result = "Fail";
                                failure_count = failure_count + 1;

                                if (failure_count == 1)

                                    first_failure_message = log_msg_text;
                            }

                            // detect custom field values (for Reports in CoPilot)

                            //Local $msg_text_equals_pos = StringInStr($msg_text, " = ")

                            //if $msg_text_equals_pos > 0 Then

                            // Local $custom_field_name = StringLeft($msg_text, $msg_text_equals_pos - 1)

                            // Local $custom_field_arr_index = _ArraySearch($custom_field_arr, $custom_field_name, 0, 0, 0, 0, 1, 0)

                            // if $custom_field_arr_index > -1 Then

                            //  Local $custom_field_value = StringMid($msg_text, $msg_text_equals_pos + StringLen(" = "))
                            //  $custom_field_value = StringStripCR($custom_field_value)
                            //  $custom_field_value = StringStripWS($custom_field_value, 3)

                            //  if StringLen($custom_field_arr[$custom_field_arr_index][1]) > 0 Then

                            //   $custom_field_arr[$custom_field_arr_index][1] = $custom_field_arr[$custom_field_arr_index][1] & ","
                            //  EndIf

                            //  $custom_field_arr[$custom_field_arr_index][1] = $custom_field_arr[$custom_field_arr_index][1] & $custom_field_value
                            // EndIf
                            //EndIf

                            // Enhance the extra video log text

                            if (log_msg_text.Contains("EXTRA VIDEO PATH - "))
                            {
                                script_name_short = script_name.Substring(script_name.LastIndexOf('.') + 1);
                                log_msg_text = log_msg_text + "\\Run_1_" + script_name_short + "_" + script_result + ".mp4";
                            }

                            if (mysql_insert_values.Length > 0)

                                mysql_insert_values = mysql_insert_values + ",";

                            mysql_insert_values = mysql_insert_values + "(" + run[0].egs + ", '" + log_msg_date_time + "', '" + log_msg_result + "', '" + log_msg_text.Truncate(2000) + "')";

                            if (run_auto_archive_script_logs == 1)
                            {
                                if (cam_log_xml.Length > 0)

                                    cam_log_xml = cam_log_xml + "\n";

                                cam_log_xml = cam_log_xml + run[0].egs + "|" + log_msg_date_time + "|" + log_msg_result + "|" + log_msg_text.Replace('|', ' ');
                            }
                        }

                        Main.StatusBarSetText("Status: Connected");
                        dbConnect = new DBConnect(project_arr[0].schema_name);
                        var id = dbConnect.Insert("INSERT INTO execution_group_script_log (execution_group_script_id, date_time, result, text) VALUES " + mysql_insert_values + ";");

                        if (id == -1)

                            Main.StatusBarSetText("Status: Error. See log file for more information.");
                    }
                }

                // keep a copy of the video log, latest screenshots and XML log against the run number in the local_logs folder

                script_name_short = script_name.Substring(script_name.LastIndexOf('.') + 1);
                File.delete(project_path + "_local_logs", "Run_1_" + script_name_short + "_*.mp4");
                File.delete(project_path + "_local_logs", "Run_1_" + script_name_short + "_*.xml");
                File.delete(project_path + "_local_logs", "Run_1_" + script_name_short + "_*.txt");
                File.delete(project_path + "_local_logs", "Run_1_" + script_name_short + "_*.png");
                File.Copy(project_path + "_local_logs\\" + script_name + "\\log 1.mp4", project_path + "_local_logs\\Run_1_" + script_name_short + "_" + script_result + ".mp4");
                File.Copy(project_path + "_local_logs\\" + script_name + "\\log.xml", project_path + "_local_logs\\Run_1_" + script_name_short + "_" + script_result + ".txt");

                //Local $screenshot_arr = _FileListToArray($project_path & "_local_logs\" & $script_name, "*.png", 1, false)

                //         if @error = 0 Then

                // if $screenshot_arr[0] >= 2 Then

                //  for $i = 1 to $screenshot_arr[0]

                //   $screenshot_arr[$i] = Number(StringReplace($screenshot_arr[$i], ".png", ""))
                //  Next

                //  _ArrayDelete($screenshot_arr, 0)
                //  _ArraySort($screenshot_arr)

                //  FileCopy($project_path & "_local_logs\" & $script_name & "\" & $screenshot_arr[UBound($screenshot_arr) - 1] & ".png", $project_path & "_local_logs\Run_" & $run_execution_group_run_number & "_" & $script_name_short & "_" & $screenshot_arr[UBound($screenshot_arr) - 1] & ".png", 1)
                //  FileCopy($project_path & "_local_logs\" & $script_name & "\" & $screenshot_arr[UBound($screenshot_arr) - 2] & ".png", $project_path & "_local_logs\Run_" & $run_execution_group_run_number & "_" & $script_name_short & "_" & $screenshot_arr[UBound($screenshot_arr) - 2] & ".png", 1)
                // EndIf
                //EndIf

                // robocopy the video log to the extra path (if it exists in the log)

                if (log_xml.Contains("<result>INFO</result><text>EXTRA VIDEO PATH - "))
                {

                    Regex regex = new Regex(@"(?U)<result>INFO</result><text>EXTRA VIDEO PATH - (.*)</text></msg>");
                    var tmp_path = regex.Match(log_xml).Groups[1].Value;

                    var extra_video_source_path = project_path + "_local_logs";
                    var extra_video_target_path = tmp_path;
                    var extra_video_filename = "Run_1_" + script_name_short + "_" + script_result + ".mp4";

                    LogMessage("Deleting " + extra_video_target_path + "\\Run_1_" + script_name_short + "_*.mp4");
                    File.delete(extra_video_target_path, "Run_1_" + script_name_short + "_*.mp4");
                    File.RoboCopy(MessagesTextBox, extra_video_source_path, extra_video_target_path, extra_video_filename);
                }

                // send iOS push notification

                if (execution_group_script_arr[0].deviceNotifications == 1 && device_notification_api_key.Length > 0)
                {
                    //            Local $video_log_url = "http://149.28.170.80/ite/local%20ITE%20Release%201.4%20Se%20project_local_logs/Run_" & $run_execution_group_run_number & "_" & $script_name_short & "_" & $script_result & ".mp4"
                    //local $cmd = @ScriptDir & "\curl.exe"
                    //Local $cmd_params = "-k https://prowl.weks.net/publicapi/add -F apikey=" & $device_notification_api_key & " -F priority=0 -F application=""Run #" & $run_execution_group_run_number & " - " & $script_name_short & """ -F event=""" & $script_result & """ -F description=""" & $video_log_url & """"
                    //log_message("Executing " & $cmd & " " & $cmd_params)
                    //ShellExecute($cmd, $cmd_params, @ScriptDir, "", @SW_HIDE)

                }

                // if Zephyr for Jira is the external test management application

                //  if StringCompare($project_external_app, "Zephyr") = 0 Then

                //   Local $project_name_start_pos = StringInStr($project_path, "R:\local ") + StringLen("R:\local ")
                //   Local $project_name_end_pos = StringInStr($project_path, " Release ")
                //   Local $zephyr_project_name = StringMid($project_path, $project_name_start_pos, $project_name_end_pos - $project_name_start_pos)

                //   Local $zephyr_version_name = $iteration_name
                //   Local $zephyr_cycle_name = $execution_group_name
                //   Local $zephyr_issue_key = $script_external_id
                //   Local $zephyr_status_id = -1 ; UNEXECUTED

                //   if StringCompare($script_result, "Pass") = 0 Then

                //    $zephyr_status_id = 1 ; PASS
                //   EndIf

                //   if StringCompare($script_result, "Fail") = 0 Then

                //    $zephyr_status_id = 2 ; FAIL
                //   EndIf

                //   Local $zephyr_project_path = $project_path
                //   local $zephyr_environment_name = $environment_name

                //   $zephyr_project_name = StringReplace($zephyr_project_name, " ", "+")
                //   $zephyr_project_path = StringReplace($zephyr_project_path, " ", "+")
                //   $zephyr_project_path = StringReplace($zephyr_project_path, ":", "%3A")
                //   $zephyr_project_path = StringReplace($zephyr_project_path, "\", "%5C")
                //   $zephyr_environment_name = StringReplace($zephyr_environment_name, " ", "+")
                //   $zephyr_version_name = StringReplace($zephyr_version_name, " ", "+")
                //   $zephyr_cycle_name = StringReplace($zephyr_cycle_name, " ", "+")
                //   $zephyr_issue_key = StringReplace($zephyr_issue_key, " ", "+")

                //;					msgbox(0, "", "$zephyr_project_name=" & $zephyr_project_name & " $zephyr_version_name=" & $zephyr_version_name & " $zephyr_cycle_name=" & $zephyr_cycle_name & " $zephyr_issue_key=" & $zephyr_issue_key)
                //   Local $comment = "cam://scriptlog?projschema=" & $mysql_db & "&projid=" & $run_project_id & "&projpath=" & $zephyr_project_path & "&egid=" & $run_execution_group_id & "&egscriptid=" & $run_execution_group_script_id & "&hostname=" & $current_host_name & "&scriptname=" & $script_name & "&itername=" & $zephyr_version_name & "&envname=" & $zephyr_environment_name

                //   ; get the execution for the test

                //   local $cmd = @ScriptDir & "\curl.exe --request GET --header ""Content-Type: application/json"" --user seangriffin:griffo ""http://atlassian:8180/rest/zapi/latest/zql/executeSearch?zqlQuery=project=\""" & $zephyr_project_name & "\""+AND+fixVersion=\""" & $zephyr_version_name & "\""+AND+cycleName=\""" & $zephyr_cycle_name & "\""+AND+issue=" & $zephyr_issue_key
                //   log_message("Executing " & $cmd)
                //   Local $iPID = Run($cmd, @ScriptDir, @SW_HIDE, $STDOUT_CHILD)
                //   ProcessWaitClose($iPID)
                //   Local $sOutput = StdoutRead($iPID)
                //   log_message("Output = " & $sOutput)

                //   Local $Data1 = Json_Decode($sOutput)
                //   Local $execution_id = Json_Get($Data1, '["executions"][0]["id"]')
                //;					log_message("$execution_id = " & $execution_id)

                //   ConsoleWrite('@@ Debug(' & @ScriptLineNumber & ') : $execution_id = ' & $execution_id & @CRLF & '>Error code: ' & @error & @CRLF) ;### Debug Console
                //   ;Exit

                //   ; update the execution for the test

                //   Local $cmd = @ScriptDir & "\curl.exe --request PUT --header ""Content-Type: application/json"" --data-binary ""        {\""status\"":\""" & $zephyr_status_id & "\"",\""comment\"":\""" & $comment & "\""}"" --user seangriffin:griffo ""http://atlassian:8180/rest/zapi/latest/execution/" & $execution_id & "/execute"
                //   log_message("Executing " & $cmd)
                //   Local $iPID = Run($cmd, @ScriptDir, @SW_HIDE, $STDOUT_CHILD)
                //   ProcessWaitClose($iPID)
                //   Local $sOutput = StdoutRead($iPID)
                //   log_message("Output = " & $sOutput)

                //  EndIf

                // if the log is to be archived

                //if Number($run_auto_archive_script_logs) = 1 Then

                // if StringLen($cam_log_xml) > 0 Then

                //  FileWrite($remote_logs_path & "\log.csv", $cam_log_xml)
                // EndIf

                // Local $dest_7z_path = $remote_logs_path & "_20170101T010101_pass.7z"
                // log_message("Archiving """ & $remote_logs_path & """ to """ & $dest_7z_path & """")
                // $retResult = _7ZipAdd(0, $dest_7z_path, $remote_logs_path & "\*.*")
                //EndIf

                // convert the custom field array into a update clause

                var custom_fields_update_clause = "";

                //for $i = 0 to (UBound($custom_field_arr) - 1)

                // ; truncate the custom field value to fit in the database (128 characters)
                // if StringLen($custom_field_arr[$i][1]) > 128 Then

                //  $custom_field_arr[$i][1] = StringLeft($custom_field_arr[$i][1], 128)
                // EndIf

                // ; for the clause quote the populated custom fields and NULL the unpopulated custom fields
                // if StringLen($custom_field_arr[$i][1]) > 0 Then

                //  $custom_field_arr[$i][1] = "'" & $custom_field_arr[$i][1] & "'"
                // Else

                //  $custom_field_arr[$i][1] = "NULL"
                // EndIf

                // $custom_fields_update_clause = $custom_fields_update_clause & ", `custom " & $custom_field_arr[$i][0] & "` = " & $custom_field_arr[$i][1]
                //Next

                // update the test script result in the execution_group_script
                Main.StatusBarSetText("Status: Connected");
                dbConnect = new DBConnect(project_arr[0].schema_name);
                result = dbConnect.Update("UPDATE execution_group_script SET state = '" + script_result + "', state_override = NULL, end_date_time = '" + end_date_time + "', first_exception_message = '" + first_failure_message + "', failure_count = " + failure_count + custom_fields_update_clause + " WHERE id = " + run[0].egs + ";");

                if (result == false)

                    Main.StatusBarSetText("Status: Error. See log file for more information.");

                var response_filename = "S:\\Responses\\" + current_host_name + ".ini";
                File.delete("S:\\Responses", current_host_name + ".ini");
                File.overwrite(response_filename, "");

                // log a message about the post run delay if in effect

                if (execution_group_script_arr[0].postRunDelay.Length > 0 && !execution_group_script_arr[0].postRunDelay.Equals("00:00"))
                {
                    var post_run_delay_part = execution_group_script_arr[0].postRunDelay.Split(':');
                    var now_date = DateTime.Now;
                    var future_date = now_date.AddMinutes(post_run_delay_part[0].ToDouble()).AddSeconds(post_run_delay_part[1].ToDouble()).ToString("yyyy-MM-dd HH:mm:ss");
                    LogMessage("Post run delay until " + future_date);
                }


            }

            project_path = "";
            script_name = "";

            // Set timer to trigger this method in another 10 seconds
            timer.Change(10 * 1000, Timeout.Infinite);

            // If this is a "temporary" Automation Adapter then close it now

            if (App.run_ids.Length > 0)

                Dispatcher.Invoke((Action)delegate () { this.Close(); });

            return;
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

                    LogMessage("Deletion of " + test_csproj_path + " failed.");

            }

            LogMessage("Writing to " + test_csproj_path + " ...");
            var file_write_result = File.overwrite(test_csproj_path, csproj_str);
            LogMessage("Write complete.");

            if (file_write_result == false)
            {
                LogMessage("Writing to " + test_csproj_path + " failed.");
                msbuild_exit_code = 1000;
            }
            else
            {
                LogMessage(msbuild_path + "\\MSBuild.exe /m \"" + script_name + ".csproj\" > \"" + script_name + ".log\"");
                var _processStartInfo = new ProcessStartInfo();
                _processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _processStartInfo.WorkingDirectory = project_path;
                _processStartInfo.FileName = "CMD.exe";
                _processStartInfo.Arguments = "/c @\"" + msbuild_path + "\\MSBuild.exe\" /m \"" + script_name + ".csproj\" > \"" + script_name + ".log\"";
                var process = Process.Start(_processStartInfo);
                process.WaitForExit();
                msbuild_exit_code = process.ExitCode;
                LogMessage("MSBuild exit code = " + msbuild_exit_code);

                File.Copy(project_debug_path + "\\" + script_name + ".exe", project_path + "\\" + target_exe);
                File.Copy(project_debug_path + "\\" + script_name + ".pdb", project_path + "\\" + target_pdb);
            }

            return msbuild_exit_code;

        }


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Debug(e.ExceptionObject.ToString());
        }

    }
}

