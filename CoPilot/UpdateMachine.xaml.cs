using SharedProject;
using System.Windows;
using System.Windows.Input;

namespace CoPilot
{
    /// <summary>
    /// Interaction logic for CreateMachine.xaml
    /// </summary>
    public partial class UpdateMachine : Window
    {
        public UpdateMachine(int ID, string FirstName, string HostType, string HostName, string Comment)
        {
            InitializeComponent();

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            IDTextBox.Text = ID.ToString();
            FirstNameTextBox.Text = FirstName;
            HostTypeTextBox.Text = HostType;
            HostNameTextBox.Text = HostName;
            CommentTextBox.Text = Comment;
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)

                Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            MySQL.Update("UPDATE machine SET name = '" + FirstNameTextBox.Text + "', host_type = '" + HostTypeTextBox.Text + "', host_name = '" + HostNameTextBox.Text + "', comment = '" + CommentTextBox.Text + "' WHERE id = " + IDTextBox.Text + ";", "control_automation_machine");
            this.DialogResult = true;
            Close();
        }
    }
}
