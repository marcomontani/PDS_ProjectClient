using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace PDS_Client
{
    /// <summary>
    /// Logica di interazione per Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        DialogResult result = System.Windows.Forms.DialogResult.Cancel;
        public Window2()
        {
            InitializeComponent();
            this.ResizeMode = ResizeMode.NoResize;
        }

        public  DialogResult Show_D(string filename)
        {
            ((TextBlock)this.FindName("filename_msg")).Text = filename;
            this.ShowDialog();
            return result;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            // it means restore the file
            result = System.Windows.Forms.DialogResult.Yes;
            this.Close();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            // it means delete permanent the file
            result = System.Windows.Forms.DialogResult.Abort;
            this.Close();

        }
    }
}
