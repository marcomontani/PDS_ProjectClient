using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.IO;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PDS_Client
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    /// 

    struct queueObject
    {
        public string file;
        public WatcherChangeTypes type;
    }


    public partial class MainWindow : Window
    {
        Queue<queueObject> eventsArray = new Queue<queueObject>();
        BindingList<FileSystemElement> currentWorkDirectory = new BindingList<FileSystemElement>();
        Socket s;
        int flag;
        string BASE_DIRECTORY = "C:\\Users\\Gaetano\\Documents\\malnati";
        
        public MainWindow()
        {
            InitializeComponent();
            //syncFolder();
            watchFolder();
            flag = 0;
            addCurrentFoderInfo(BASE_DIRECTORY);

            this.DataContext = currentWorkDirectory;
        }

        private void syncFolder()
        {
            s.Send(BitConverter.GetBytes(8));  // == ENUM.getUserFiles
            byte[] buffer = new byte[1024];
            
            s.Receive(buffer, 1024, SocketFlags.None);
            string serverFolderDescription = Encoding.ASCII.GetString(buffer);
            // now in the string we have the JSON string description. it is "folder: [{"path":"...", "name":"......"}]"

            List<JSON_Folder_Items> items = JsonConvert.DeserializeObject<List<JSON_Folder_Items>>(serverFolderDescription);
            // todo: add date of last update and compare it with date of last modify
            checkFileExists(BASE_DIRECTORY, items);

        }

        private void checkFileExists(string basePath,  List<JSON_Folder_Items> items)
        {
            foreach (string p in System.IO.Directory.GetDirectories(basePath)) checkFileExists(p, items);

            foreach (string file in System.IO.Directory.GetFiles(basePath)) {
                JSON_Folder_Items item = new JSON_Folder_Items();
                item.name = file;
                item.path = basePath;


                if (!items.Contains(item)) sendFileToServer(basePath);
            }



        }


        private void sendFileToServer(string path)
        {
            s.Send(BitConverter.GetBytes(2)); // UPLOAD FILE
            s.Send(Encoding.ASCII.GetBytes(path));

            byte[] inBuff = new byte[1024];
            s.Receive(inBuff);
            if (Encoding.ASCII.GetString(inBuff) != "OK") throw new Exception("error: filename sent but error was returned");

            long dimension = (new FileInfo(path)).Length;
            if (dimension > Int32.MaxValue) throw new Exception("error: file dimension too big! > 32 bit");
            int dim = (int)dimension;

            s.Send(BitConverter.GetBytes(dim));
            s.Send(File.ReadAllBytes(path));

            s.Receive(inBuff);
            if (Encoding.ASCII.GetString(inBuff) != "OK") throw new Exception("error: file not uploaded correctly");

            // todo: calculate and send sha1 checksum
            SHA1 shaProvider = SHA1.Create();
            shaProvider.ComputeHash(new FileStream(path, FileMode.Open));
            s.Send(shaProvider.Hash);
        }

        

        public void setSocket(Socket sock)
        {
           // if (!sock.Connected) throw new SocketException();
            s = sock;
        }

        private void watchFolder()
        {
            FileSystemWatcher fs = new FileSystemWatcher(BASE_DIRECTORY);
            fs.Changed += new FileSystemEventHandler(OnChanged);
            fs.NotifyFilter = NotifyFilters.LastWrite;
            fs.EnableRaisingEvents = true;

        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            MessageBox.Show("File: " + e.FullPath + " " + e.ChangeType);
            queueObject q; q.file = e.FullPath; q.type = e.ChangeType;
            if (!this.eventsArray.Contains(q)) eventsArray.Enqueue(q);
        }

        private void mouse_MouseDown(object sender, MouseButtonEventArgs e)
        {
         
            //if (sender.GetType().FullName=="StackPanel") M
            try {
                this.DragMove();
            }catch(InvalidOperationException ioe)
            {
                // this means that someone has already cought the mousedown event. probably i did not want to move the window
            }
        }

        private void mouse_x_click(object sender, RoutedEventArgs e)
        {
            // todo: send to try bar
            this.Close();
        }


        private void MouseFolderButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            ((StackPanel)this.FindName("fs_grid")).Children.Clear(); // remove all childs

            Panel p = (Panel)sender;
            Label lblDirectory = (Label)p.Children[1];            
            string newDir = (string)(lblDirectory).Content;
            addCurrentFoderInfo(newDir);
        }


        private void MouseFileButtonDownHandler(object sender, RoutedEventArgs e) {
            Grid.SetColumnSpan((UIElement)this.FindName("fs_grid"), 1);
            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Visible;
            Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("key_details_animation");
            sb.Begin();
            e.Handled = true;
            flag = 1;
        }



        private void addCurrentFoderInfo(string path)
        {
            StackPanel g = (StackPanel)this.FindName("fs_grid");
            
            foreach (string dir in Directory.GetDirectories(path))
            {
                StackPanel panel = new StackPanel();
                panel.Name = "folder_panel";
                panel.VerticalAlignment = VerticalAlignment.Center;
                panel.Orientation = Orientation.Horizontal;
                panel.MouseLeftButtonDown += MouseFolderButtonDownHandler;
               

                Image img_folder = new Image();
                img_folder.Source = new BitmapImage(new Uri(@"\images\folderIcon.png", UriKind.RelativeOrAbsolute));

                img_folder.Width = 50;
                img_folder.Height = 50;
                panel.Children.Add(img_folder);


                Label lbl_dir_name = new Label();
                lbl_dir_name.Name = "lbl_folder_name";
                lbl_dir_name.Content = dir;
                panel.Children.Add(lbl_dir_name);


                
                g.Children.Add(panel);
            }



            foreach (string file in Directory.GetFiles(path))
            {
               
                StackPanel panel = new StackPanel();
                panel.Name = "file_panel";
                panel.VerticalAlignment = VerticalAlignment.Center;
                panel.Orientation = Orientation.Horizontal;
                panel.MouseLeftButtonDown += MouseFileButtonDownHandler;

                Image img_file = new Image();
                img_file.Source = new BitmapImage(new Uri(@"\images\fileIcon.png", UriKind.RelativeOrAbsolute));
              //  img_file.Stretch = Stretch.None;
                img_file.Width = 50;
                img_file.Height = 50;

                panel.Children.Add(img_file);


                Label lbl_file_name = new Label();
                lbl_file_name.Content = file;
                panel.Children.Add(lbl_file_name);
           
       
                g.Children.Add(panel);
            }

        }

   
        private void closeVersions(object sender, MouseButtonEventArgs e)
        {
            if (flag==1)
            {
                Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("key_details_animation_close");
                sb.Begin();
                sb.Completed += closeSidebar;
                
            }
        }

        void closeSidebar(object sender, EventArgs e)
        {
            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Collapsed;
            Grid.SetColumnSpan((UIElement)this.FindName("fs_grid"), 2);
            flag = 0;
        }
     
    }
}
