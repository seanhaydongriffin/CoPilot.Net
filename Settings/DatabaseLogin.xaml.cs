using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Settings
{
    /// <summary>
    /// Interaction logic for DatabaseLogin.xaml
    /// </summary>
    public partial class DatabaseLogin : Window
    {
        public string username { get; set; }
        public string password { get; set; }

        public DatabaseLogin()
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
            this.username = UsernameTextBox.Text;
            this.password = PasswordTextBox.Text;
            this.DialogResult = true;
            Close();
        }
    }
}
