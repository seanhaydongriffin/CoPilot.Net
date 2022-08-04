using SharedProject;
using SharedProject.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExecutionGroupCreateUpdate
{
    /// <summary>
    /// Interaction logic for CreateMachine.xaml
    /// </summary>
    public partial class AddScript : Window
    {
        private int num_records_per_page = 100;

        public AddScript(string search_text = "")
        {
            InitializeComponent();
            App.CurrentStatusBarText = StatusBarText;

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            SearchTextBox.Text = search_text;
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            //MySQL.Insert("INSERT INTO machine (name, host_type, host_name, comment, archived) VALUES ('" + FirstNameTextBox.Text + "', '" + HostTypeTextBox.Text + "', '" + HostNameTextBox.Text + "', '" + CommentTextBox.Text + "', 0);", "control_automation_machine");
            //this.DialogResult = true;
            //Close();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            TextBlock.Update(StatusBarText, "Please Wait. Getting scripts ...");
            var dbConnect = new DBConnect(App.schema_name);
            var scripts = dbConnect.Select<Script>("SELECT id, name FROM script WHERE name like '%" + SearchTextBox.Text + "%' and archived = 0 LIMIT " + num_records_per_page + ";");
            ScriptsListView.ItemsSource = scripts;
            TextBlock.ClearNoError(StatusBarText);

            // Focus the first item in the ScriptsListView
            ScriptsListView.Focus();
            ListViewItem item = ScriptsListView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
            item.Focus();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var items = ScriptsListView.SelectedItems;

            foreach(Script item in items)
            {
                Main.ScriptsListViewCollection.Add(new ExecutionGroupScript() { id = null, host_name = "", script_id = item.id, script_name = item.name, selector = "S", post_run_delay = "", state = "", excluded = null, end_date_time = null, em_comment = "", order_id = -1 });
            }

            this.DialogResult = true;
            Close();
        }
    }
}
