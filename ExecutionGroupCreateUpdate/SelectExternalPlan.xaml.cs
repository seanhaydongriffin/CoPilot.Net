using SharedProject.Models;
using SharedProject.TestRail;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExecutionGroupCreateUpdate
{
    /// <summary>
    /// Interaction logic for CreateMachine.xaml
    /// </summary>
    public partial class SelectExternalPlan : Window
    {
        private int num_records_per_page = 100;
        public static ObservableCollection<ExternalPlan> ExternalPlanListViewCollection { get; set; }

        public SelectExternalPlan(Main parent)
        {
            InitializeComponent();

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            TextBlock.Update(parent.StatusBarText, "Please Wait. Getting External Plans ...");

            APIClient client = new APIClient("https://janison.testrail.com");
            client.User = "sgriffin@janison.com";
            client.Password = "Gri01ffo";
            JObject c = (JObject)client.SendGet("get_plans/" + parent.ExternalProjectIDTextBox.Text);
            ExternalPlanListViewCollection = new ObservableCollection<ExternalPlan>();

            foreach (dynamic item in c["plans"])
            {
                var t = new ExternalPlan();
                t.id = item["id"].Value;
                t.name = item["name"].Value;
                ExternalPlanListViewCollection.Add(t);
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
            (this.Owner as Main).ExternalPlanIDTextBox.Text = ((ExternalPlan)ExternalPlanListView.SelectedItem).id.ToString();
            (this.Owner as Main).ExternalPlanNameTextBox.Text = ((ExternalPlan)ExternalPlanListView.SelectedItem).name;
            this.DialogResult = true;
            Close();
        }
    }
}
