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
using System.Diagnostics;
using System.Threading;

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
        
        int rowElements = 9;
        string currentDirectory;
        

        public MainWindow()
        {
            InitializeComponent();
            currentDirectory = "C:";
           
            watchFolder();

        }


        public void sync()
        {
            Thread t = new Thread(syncFolder);
            t.Start();
        }

        private void syncFolder()
        {
            Debug.WriteLine("THREAD STARTED");
            s.Send(BitConverter.GetBytes(8));  // == ENUM.getUserFiles
            byte[] buffer = new byte[1024];
            
            int received = s.Receive(buffer, 1024, SocketFlags.None);
            string serverFolderDescription = Encoding.ASCII.GetString(buffer);
            serverFolderDescription = serverFolderDescription.Remove(received);
            // now in the string we have the JSON string description. it is "folder: [{"path":"...", "name":"......"}]"

            Debug.WriteLine("JSON rappresentation of the folder status on the server: \n" + serverFolderDescription + "\n");
            
            List<JSON_Folder_Items> items = JsonConvert.DeserializeObject<List<JSON_Folder_Items>>(serverFolderDescription);
            
            // todo: add date of last update and compare it with date of last modify
            checkFileExists(currentDirectory, items);

        }

        private void checkFileExists(string basePath,  List<JSON_Folder_Items> items)
        {
            foreach (string p in System.IO.Directory.GetDirectories(basePath)) checkFileExists(p, items);

            foreach (string file in System.IO.Directory.GetFiles(basePath)) {
                JSON_Folder_Items item = new JSON_Folder_Items();
                string[] splitPath = file.Split('\\');
                item.name = splitPath[splitPath.Length-1];
                item.path = basePath;

                if (!items.Contains(item))
                {
                    try
                    {
                        sendFileToServer(file);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Impossibile inviare il file " + file + " al server", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Debug.WriteLine("Impossibile inviare il file " + file + " al server");
                    }
                }
            }
        }


        private void sendFileToServer(string path)
        {
            s.Send(BitConverter.GetBytes(2)); // UPLOAD FILE
            s.Send(Encoding.ASCII.GetBytes(path));

            byte[] inBuff = new byte[1024];
            s.Receive(inBuff);
            if (!Encoding.ASCII.GetString(inBuff).Contains("OK")) throw new Exception("error: filename sent but error was returned");

            
            long dimension = (new FileInfo(path)).Length;
            if (dimension > Int32.MaxValue) throw new Exception("error: file dimension too big! > 32 bit");
            int dim = (int)dimension;

            s.Send(BitConverter.GetBytes(dim));

            s.Send(File.ReadAllBytes(path));
            
            s.Receive(inBuff);
            if (!Encoding.ASCII.GetString(inBuff).Contains("OK")) throw new Exception("error: file not uploaded correctly");
            
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
            Panel p = (Panel)sender;
            TextBlock lblDirectory = (TextBlock)p.Children[1];            
            string newDir = (string)(lblDirectory).Text;
            currentDirectory += ("\\" + newDir);
            ((StackPanel)this.FindName("fs_grid")).Children.Clear(); // remove all childs
            addCurrentFoderInfo(currentDirectory);
            
        }


        private void MouseFileButtonDownHandler(object sender, RoutedEventArgs e) {
            // todo: add here the download of the file versions
            Grid.SetColumnSpan((UIElement)this.FindName("fs_grid"), 1);
            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Visible;
            // i start here a thread in order to download the versions of this file
            string filename = (string)((Label)((StackPanel)sender).Children[1]).Content;


            Thread downloader = new Thread( () =>
           {
               Debug.WriteLine("Into downloader (versions) thread");
               s.Send(BitConverter.GetBytes(5)); // GET FILE VERSIONS

               string pathToSend = currentDirectory + "\\" + filename;
               s.Send(BitConverter.GetBytes(pathToSend.Length));
               s.Send(Encoding.ASCII.GetBytes(pathToSend));

               byte[] dim = new byte[4]; // just the space for an int
               if(s.Receive(dim) != 4)
               {
                   Debug.WriteLine("did not receive a valid number");
                   return;
               }
               if(BitConverter.ToInt32(dim, 0) < 0)
               {
                   // an error server side has occurred!
                   Debug.WriteLine("dim of versions < 0");
                   return;
               }
               byte[] buff = new byte[BitConverter.ToInt32(dim, 0)];
               s.Receive(buff); // receive json

               string versions = Encoding.ASCII.GetString(buff);

               Debug.WriteLine(versions);

               List<JSONVersion> items = JsonConvert.DeserializeObject<List<JSONVersion>>(versions);

               foreach (JSONVersion v in items)
               {
                   Debug.WriteLine("v.date = " + v.date);
                   Dispatcher.Invoke(()=>
                   {
                       TextBlock line = new TextBlock();
                       line.Text = v.date;

                       line.Foreground = new SolidColorBrush(Colors.AliceBlue);
                       line.TextWrapping = TextWrapping.Wrap;
                       line.TextAlignment = TextAlignment.Center;
                       line.Name = "lbl_folder_name";
                       ((Panel)FindName("panel_details")).Children.Add(line);
                       Debug.WriteLine("inserted the new line -> " + line.Text);
                   });
               }
               return;
           }
            );
            downloader.Start();
            

            Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("key_details_animation");
            sb.Completed += (object s, EventArgs ev) => {
                rowElements = 7;
                ((StackPanel)this.FindName("fs_grid")).Children.Clear();
                addCurrentFoderInfo(currentDirectory);
            };
            sb.Begin();
            e.Handled = true;
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
                    hpanel.Name = "row_panel_" + i;
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
            Debug.Print("Main Window: setCurrentDirectory(" + currDir + ")");
            currentDirectory = currDir;
            Debug.Print("Main Window: current directory = " + currentDirectory);
        }

   
        private void closeVersions(object sender, MouseButtonEventArgs e)
        {
                Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("key_details_animation_close");
                sb.Completed += closeSidebar;
                sb.Begin();
        }

        private void closeSidebar(object sender, EventArgs e)
        {
            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Collapsed;
            Grid.SetColumnSpan((UIElement)this.FindName("fs_grid"), 2);
            rowElements = 10;
            ((StackPanel)this.FindName("fs_grid")).Children.Clear();
            addCurrentFoderInfo(currentDirectory);
        }
     
    }
}
