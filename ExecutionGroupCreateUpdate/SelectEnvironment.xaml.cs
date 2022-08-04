using SharedProject;
using SharedProject.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExecutionGroupCreateUpdate
{
    /// <summary>
    /// Interaction logic for CreateMachine.xaml
    /// </summary>
    public partial class SelectEnvironment : Window
    {
        private int num_records_per_page = 100;
        public static ObservableCollection<Environment> EnvironmentListViewCollection { get; set; }

        public SelectEnvironment(Main parent)
        {
            InitializeComponent();

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            EnvironmentListViewCollection = new ObservableCollection<Environment>();

            TextBlock.Update(parent.StatusBarText, "Please Wait. Getting Environments ...");
            var dbConnect = new DBConnect(App.schema_name);
            EnvironmentListViewCollection = dbConnect.SelectToObservableCollection<Environment>("SELECT id, name FROM environment WHERE archived = 0");
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
            (this.Owner as Main).TestEnvIDTextBox.Text = ((Environment)EnvironmentListView.SelectedItem).id.ToString();
            (this.Owner as Main).TestEnvNameTextBox.Text = ((Environment)EnvironmentListView.SelectedItem).name;
            this.DialogResult = true;
            Close();
        }
    }
}
