using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SharedProject
{
    public static class Command2
    {

        public static void ExecuteInProgramDirectoryAsUserAndAdmin(String FileName, String Arguments = null, ProcessWindowStyle WindowStyle = ProcessWindowStyle.Normal)
        {
            // Run as current user
            ExecuteInProgramDirectory(FileName, Arguments, WindowStyle, false);

            // Run as administrator
            ExecuteInProgramDirectory(FileName, Arguments, WindowStyle, true);
        }

        public static void ExecuteInProgramDirectory(String FileName, String Arguments = null, ProcessWindowStyle WindowStyle = ProcessWindowStyle.Normal, bool UseShellExecute = false)
        {
            ExecuteWait(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\" + FileName, Arguments, System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), WindowStyle, UseShellExecute);
        }

        public static void ExecuteWait(String FileName, String Arguments = null, String WorkingDirectory = null, ProcessWindowStyle WindowStyle = ProcessWindowStyle.Normal, bool UseShellExecute = false)
        {
            var _processStartInfo = new ProcessStartInfo();

            if (WorkingDirectory != null)

                _processStartInfo.WorkingDirectory = WorkingDirectory;

            if (Arguments != null)

                _processStartInfo.Arguments = Arguments;

            _processStartInfo.FileName = FileName;

            if (UseShellExecute)
            {
                _processStartInfo.UseShellExecute = true;
                _processStartInfo.Verb = "runas";
            }

            if (WindowStyle != ProcessWindowStyle.Normal)

                _processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            var process = Process.Start(_processStartInfo);
            process.WaitForExit();
        }

        public static string DoProcess(string cmd, string argv)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = cmd;
            p.StartInfo.Arguments = $" {argv}";
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.WaitForExit();
            string output = p.StandardOutput.ReadToEnd();
            p.Dispose();

            return output;
        }

        public static void DoProcessElevated(string cmd, string argv)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C " + cmd + " " + argv;
            startInfo.Verb = "runas";
            process.StartInfo = startInfo;
            process.Start();

            //return output;
        }

    }
}
