using System;
using System.Collections.Generic;
using System.Windows;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Settings
{
    class DBBackupRestore
    {


        //Backup
        public static string Backup(string root_uid, string root_password)
        {
            string path = "";

            try
            {
                DateTime Time = DateTime.Now;
                int year = Time.Year;
                int month = Time.Month;
                int day = Time.Day;
                int hour = Time.Hour;
                int minute = Time.Minute;
                int second = Time.Second;

                //Save file to C:\ with the current date as a filename
                path = "R:\\CoPilotBackup-" + year + month + day + "T" + hour + minute + second + ".sql";
                StreamWriter file = new StreamWriter(path);

                using (Process process = new Process())
                {

                    //                process.StartInfo.WorkingDirectory = "C:\\Program Files\\MySQL\\MySQL Server 5.7\\bin";
                    process.StartInfo.FileName = "C:\\Program Files\\MySQL\\MySQL Server 5.7\\bin\\mysqldump.exe";
                    process.StartInfo.RedirectStandardInput = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    //                process.StartInfo.Arguments = "--all-databases --single-transaction --quick --lock-tables=false " + string.Format(@"-u{0} -p{1} -h{2} {3}", uid, password, server, database);
                    process.StartInfo.Arguments = "--all-databases --single-transaction --quick --lock-tables=false " + string.Format(@"-u{0} -p{1}", root_uid, root_password);
                    process.StartInfo.UseShellExecute = false;

                    using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            {
                                file.WriteLine(e.Data);
                            }
                        };

                        process.Start();

                        process.BeginOutputReadLine();

                        if (process.WaitForExit(60000) && outputWaitHandle.WaitOne(60000))
                        {
                            // Process completed. Check process.ExitCode here.
                            file.Close();
                        }
                        else
                        {
                            // Timed out.
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show("Error , unable to backup!");
            }

            return path;
        }

        //Restore
        public static void Restore(string root_uid, string root_password)
        {
            try
            {
                //Read file from C:\
                string path;
                path = "C:\\MySqlBackup.sql";
                StreamReader file = new StreamReader(path);
                string input = file.ReadToEnd();
                file.Close();

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "mysql";
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = false;
//                psi.Arguments = string.Format(@"-u{0} -p{1} -h{2} {3}", root_uid, root_password, server, database);
                psi.Arguments = string.Format(@"-u{0} -p{1}", root_uid, root_password);
                psi.UseShellExecute = false;


                Process process = Process.Start(psi);
                process.StandardInput.WriteLine(input);
                process.StandardInput.Close();
                process.WaitForExit();
                process.Close();
            }
            catch (IOException ex)
            {
                MessageBox.Show("Error , unable to Restore!");
            }
        }
    }
}
