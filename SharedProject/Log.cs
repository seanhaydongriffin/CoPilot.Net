using System;
using System.IO;
using System.Reflection;

namespace SharedProject
{
    class Log
    {
        static private string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\" + Assembly.GetCallingAssembly().GetName().Name + ".log";


        static public void Reset()
        {
            if (System.IO.File.Exists(path))

                System.IO.File.Delete(path);
        }

        static public void ResetRunLog(string project_id, string execution_group_id)
        {
            path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\" + Assembly.GetCallingAssembly().GetName().Name + "-P" + project_id + "-EG" + execution_group_id + ".log";

            if (System.IO.File.Exists(path))

                System.IO.File.Delete(path);
        }

        static public void Debug(string message)
        {
            try
            {
                using (StreamWriter w = System.IO.File.AppendText(path))
                {
                    w.Write("\r\nLog Entry : ");
                    w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                    w.WriteLine("  :");
                    w.WriteLine($"  :{message}");
                    w.WriteLine("-------------------------------");
                }
            } catch (Exception)
            {
            }
        }


    }
}
