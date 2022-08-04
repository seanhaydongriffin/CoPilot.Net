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
    public partial class SelectIteration : Window
    {
        private int num_records_per_page = 100;
        public static ObservableCollection<Iteration> IterationListViewCollection { get; set; }

        public SelectIteration(Main parent)
        {
            InitializeComponent();

            // handle Esc key to close GUI
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            IterationListViewCollection = new ObservableCollection<Iteration>();

            TextBlock.Update(parent.StatusBarText, "Please Wait. Getting iterations ...");
            var dbConnect = new DBConnect(App.schema_name);
  //          var iterations = dbConnect.Select<Script>("SELECT id, name FROM iteration WHERE archived = 0");
            IterationListViewCollection = dbConnect.SelectToObservableCollection<Iteration>("SELECT id, name FROM iteration WHERE archived = 0");
//            IterationListView.ItemsSource = iterations;
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
            (this.Owner as Main).IterationIDTextBox.Text = ((Iteration)IterationListView.SelectedItem).id.ToString();
            (this.Owner as Main).IterationNameTextBox.Text = ((Iteration)IterationListView.SelectedItem).name;
            this.DialogResult = true;
            Close();
        }
    }
}
