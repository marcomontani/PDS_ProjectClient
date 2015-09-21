using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System;
using System.Diagnostics;

namespace PDS_Client
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        BindingList<FileSystemElement> currentWorkDirectory = new BindingList<FileSystemElement>();

        public MainWindow()
        {
            InitializeComponent();
            Debug.WriteLine("Fil2e: ");
            watchFolder();
            Debug.WriteLine("File: ");
            addCurrentFoderInfo();

            this.DataContext = currentWorkDirectory;
        }


        private void watchFolder()
        {
            FileSystemWatcher fs = new FileSystemWatcher("C:\\Users\\Gaetano\\Documents\\malnati");
            fs.Changed += new FileSystemEventHandler(OnChanged);
            fs.NotifyFilter = NotifyFilters.LastWrite;
            fs.EnableRaisingEvents = true;

        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            MessageBox.Show("File: " + e.FullPath + " " + e.ChangeType);
        }


        private void addCurrentFoderInfo()
        {
            StackPanel g = (StackPanel)this.FindName("fs_grid");
            StackPanel Malnati = new StackPanel();
            Label labmal = new Label();
            labmal.Content = "MALNATI";
            Malnati.Orientation = Orientation.Horizontal;
            Malnati.Children.Add(labmal);

            Label labmal2 = new Label();
            labmal2.Content = "FILE";            
            Malnati.Children.Add(labmal2);


            g.Children.Add(Malnati);


            StackPanel Cabodi = new StackPanel();
            Label labcab = new Label();
            labcab.Content = "CABODI";
            Cabodi.Orientation = Orientation.Horizontal;
            Cabodi.Children.Add(labcab);
            Label labcab2 = new Label();
            labcab2.Content = "FILE";
            Cabodi.Children.Add(labcab2);


            g.Children.Add(Cabodi);



        }

    }
}
