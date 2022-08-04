using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AutomationAdapter
{
    public static class TextBoxEx
    {
        private static Action EmptyDelegate = delegate () { };

        public static void LogMessage(this TextBox tb, string message)
        {
            var dated_message = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} : " + message;

            if (tb.Text.Length > 0)

                dated_message = Environment.NewLine + dated_message;

            tb.AppendText(dated_message);
            tb.Refresh();
        }

        public static void Refresh(this TextBox tb)
        {
            tb.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }


    }
}
