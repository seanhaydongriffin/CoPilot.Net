using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ExecutionGroupCreateUpdate
{
    class TextBlock
    {
        static public void Update(System.Windows.Controls.TextBlock tb, string text)
        {
            tb.Text = text;

            // Yield to allow UI to update
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
        }

        static public void ClearNoError(System.Windows.Controls.TextBlock tb)
        {
            if (!tb.Text.StartsWith("Error"))
            {
                tb.Text = "";

                // Yield to allow UI to update
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
            }
        }


    }
}
