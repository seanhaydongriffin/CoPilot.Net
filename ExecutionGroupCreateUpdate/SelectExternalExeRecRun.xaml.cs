using SharedProject.TestRail;
using Newtonsoft.Json.Linq;
using SharedProject.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ExecutionGroupCreateUpdate
{
    /// <summary>
    /// Interaction logic for CreateMachine.xaml
    /// </summary>
    public partial class SelectExternalExeRecRun : Window
    {
        private int num_records_per_page = 100;
        public static ObservableCollection<ExternalRun> ExternalRunListViewCollection { get; set; }

        public SelectExternalExeRecRun(Main parent)
        {
            InitializeComponent();

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            ExternalRunListViewCollection = new ObservableCollection<ExternalRun>();

            TextBlock.Update(parent.StatusBarText, "Please Wait. Getting External Runs ...");

            APIClient client = new APIClient("https://janison.testrail.com");
            client.User = "sgriffin@janison.com";
            client.Password = "Gri01ffo";
            JObject c = (JObject)client.SendGet("get_plan/" + parent.ExternalPlanIDTextBox.Text);
            ExternalRunListViewCollection = new ObservableCollection<ExternalRun>();

            foreach (dynamic entry in c["entries"])
            {
                foreach (dynamic run in entry["runs"])
                {
                    var t = new ExternalRun();
                    t.id = run["id"].Value;
                    t.name = run["name"].Value;
                    ExternalRunListViewCollection.Add(t);
                }
            }

            TextBlock.ClearNoError(parent.StatusBarText);

            // below is important to activate all the bindings from the XAML to the data set above
            this.DataContext = this;
            App.CurrentStatusBarText = StatusBarText;

        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            (this.Owner as Main).ExternalExeRecRunIDTextBox.Text = ((ExternalRun)ExternalExeRecRunListView.SelectedItem).id.ToString();
            (this.Owner as Main).ExternalExeRecRunNameTextBox.Text = ((ExternalRun)ExternalExeRecRunListView.SelectedItem).name;
            this.DialogResult = true;
            Close();
        }
    }
}
