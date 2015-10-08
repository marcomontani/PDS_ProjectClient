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
        Socket s;
        
        int rowElements;
        string currentDirectory;
        bool flag=true;
        

        public MainWindow()
        {
            InitializeComponent();
            currentDirectory = "C:";
            //syncFolder();
            watchFolder();
            rowElements = 9;           
            ((StackPanel)FindName("fs_grid")).SizeChanged+= (s, e) =>
            {
                ((StackPanel)FindName("fs_grid")).Children.Clear();
                double d = ((StackPanel)FindName("fs_grid")).ActualWidth;
              
                rowElements =(int)(d /100)+1;
                addCurrentFoderInfo(currentDirectory);
            };


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
            checkFileExists(currentDirectory, items);

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
            FileSystemWatcher fs = new FileSystemWatcher(currentDirectory);
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
            TextBlock lblDirectory = (TextBlock)p.Children[1];            
            string newDir = (string)(lblDirectory).Text;
            currentDirectory += ("\\" + newDir);
            addCurrentFoderInfo(currentDirectory);
        }


        private void MouseFileButtonDownHandler(object sender, RoutedEventArgs e) {
            //  Grid.SetColumnSpan((UIElement)this.FindName("fs_grid"), 1);
            if (flag == false) return;
            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Visible;
            Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("key_details_animation");
            
           
                //rowElements = 7;
                ((StackPanel)this.FindName("fs_grid")).Children.Clear();
                addCurrentFoderInfo(currentDirectory);
            flag = false;
            sb.Completed += openSidebar;
            sb.Begin();
            
            e.Handled = true;
            
        }

        void openSidebar(object sender, EventArgs e)
        {
            flag = true;


        }

        private void addCurrentFoderInfo(string path)
        {
            StackPanel g = (StackPanel)this.FindName("fs_grid");
            StackPanel hpanel=null;
            int i = rowElements;
            
            foreach (string dir in Directory.GetDirectories(path))
            {
                if ((i % rowElements) == 0) {
                    hpanel = new StackPanel();
                    //hpanel.Name = "row_panel_" + i;
                    hpanel.VerticalAlignment = VerticalAlignment.Center;
                    hpanel.Orientation = Orientation.Horizontal;
                    hpanel.Margin = new Thickness(5, 5, 0, 0); 
                        };
              
                i++;
                StackPanel panel = new StackPanel();
                panel.Width = 100;
                panel.Height = 85;
                panel.Name = "folder_panel";
                panel.VerticalAlignment = VerticalAlignment.Center;
                panel.HorizontalAlignment = HorizontalAlignment.Center;
                panel.Orientation = Orientation.Vertical;
                panel.MouseLeftButtonDown += MouseFolderButtonDownHandler;
               

                Image img_folder = new Image();
                img_folder.Source = new BitmapImage(new Uri(@"\images\folderIcon.png", UriKind.RelativeOrAbsolute));

                img_folder.Width = 50;
                img_folder.Height = 50;
                panel.Children.Add(img_folder);


                TextBlock lbl_dir_name = new TextBlock();

                lbl_dir_name.MaxWidth = 85;
                lbl_dir_name.MinWidth = 40;
                lbl_dir_name.TextWrapping = TextWrapping.Wrap;
                lbl_dir_name.TextAlignment = TextAlignment.Center;
       
                lbl_dir_name.Name = "lbl_folder_name";
                lbl_dir_name.Text = dir.Split('\\')[dir.Split('\\').Length-1];
                panel.Children.Add(lbl_dir_name);


                hpanel.Children.Add(panel);
                if (((i-1) % rowElements) == 0)g.Children.Add(hpanel);
            }



            foreach (string file in Directory.GetFiles(path))
            {
               
                StackPanel panel = new StackPanel();
                panel.Name = "file_panel";
                panel.VerticalAlignment = VerticalAlignment.Center;
                panel.Orientation = Orientation.Horizontal;
                panel.MouseLeftButtonDown += MouseFileButtonDownHandler;
                panel.Width = 150;
                Image img_file = new Image();
                img_file.Source = new BitmapImage(new Uri(@"\images\fileIcon.png", UriKind.RelativeOrAbsolute));
                img_file.Width = 50;
                img_file.Height = 50;

                panel.Children.Add(img_file);


                Label lbl_file_name = new Label();
                lbl_file_name.Content = file.Split('\\')[file.Split('\\').Length - 1]; ;
                panel.Children.Add(lbl_file_name);
           
       
                g.Children.Add(panel);
            }

        }

   
        public void updateFolders()
        {
            addCurrentFoderInfo(currentDirectory);
        }

        
        public void setCurrentDirectory(string currDir)
        {
            currentDirectory = currDir;
        }

   
        private void closeVersions(object sender, MouseButtonEventArgs e)
        {
            if (flag == false) return;
                int span = Grid.GetColumnSpan((UIElement)this.FindName("fs_grid"));
                if (span == 7 && flag) return;
                Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("key_details_animation_close");
                sb.Completed += closeSidebar;
                flag = false;
                sb.Begin();
        }

        void closeSidebar(object sender, EventArgs e)
        {
            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Collapsed;
            //Grid.SetColumnSpan((UIElement)this.FindName("fs_grid"), 7);
           // rowElements = 10;
            ((StackPanel)this.FindName("fs_grid")).Children.Clear();
            addCurrentFoderInfo(currentDirectory);
            flag = true;
        }
     
    }
}
