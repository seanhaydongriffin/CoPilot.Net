using SharedProject;
using System.Windows;
using System.Windows.Input;

namespace CoPilot
{
    /// <summary>
    /// Interaction logic for CreateMachine.xaml
    /// </summary>
    public partial class CreateMachine : Window
    {
        public CreateMachine()
        {
            InitializeComponent();

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            MySQL.Insert("INSERT INTO machine (name, host_type, host_name, comment, archived) VALUES ('" + FirstNameTextBox.Text + "', '" + HostTypeTextBox.Text + "', '" + HostNameTextBox.Text + "', '" + CommentTextBox.Text + "', 0);", "control_automation_machine");
            this.DialogResult = true;
            Close();
        }
    }
}
