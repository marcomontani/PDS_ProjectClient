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
using System.Drawing;

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

        public override bool Equals(object o)
        {
            if (o == null) return false;
            if (o.GetType() != this.GetType()) return false;
            queueObject other = (queueObject)o;

            return (other.file.Equals(this.file) && other.type.Equals(this.type));
        }
    }


    public partial class MainWindow : Window
    {
        Queue<queueObject> eventsArray = new Queue<queueObject>();
        Mutex events_semaphore;

        Socket s;
        int rowElements;
        string currentDirectory;
        string root;
        bool first = true;
        bool flag = true;

        string selectedFile;



        public MainWindow()
        {
            InitializeComponent();
            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Collapsed;
            currentDirectory = "C:";
            events_semaphore = new Mutex();
            rowElements = 9;
            ((StackPanel)FindName("fs_grid")).SizeChanged += (s, e) =>
            {
                ((StackPanel)FindName("fs_grid")).Children.Clear();
                double d = ((StackPanel)FindName("fs_grid")).ActualWidth;
                if (first)
                {
                    first = false;
                    root = "" + currentDirectory;
                }
                rowElements = (int)(d / 100) + 1;
                addCurrentFoderInfo(currentDirectory);
                updateAddress();
                NetworkHandler.createInstance(this.s);
            };
            // <Label x:Name="label" Background="#2C4566" Foreground="AliceBlue" Content="C:\\" HorizontalAlignment="Left" VerticalAlignment="Top" Margin="2,2,0,0"/>


        }

        public void updateAddress()
        {
            StackPanel sp = ((StackPanel)FindName("address"));
            sp.Children.Clear();
            var bc = new BrushConverter();
            int num_base = root.Split('\\').Length;
            string [] perc = currentDirectory.Split('\\');
            int counter = 0;
            Label rt = new Label();
            rt.Background = System.Windows.Media.Brushes.DarkGreen;
            rt.Foreground = System.Windows.Media.Brushes.AliceBlue;
            rt.BorderThickness = new Thickness(2, 2, 2, 2);
            rt.BorderBrush = System.Windows.Media.Brushes.LightGray;
            rt.Content = root;
            rt.VerticalContentAlignment = VerticalAlignment.Center;
            rt.HorizontalAlignment = HorizontalAlignment.Left;
            rt.MouseLeftButtonDown += (s, e) => {                
                ((StackPanel)this.FindName("fs_grid")).Children.Clear();
                currentDirectory = root;
                updateAddress();
                addCurrentFoderInfo(root);
            };
            rt.MouseEnter += (s, e) =>
            {
                ((Label)s).Background = System.Windows.Media.Brushes.LightGreen;
                ((Label)s).BorderBrush = System.Windows.Media.Brushes.White;
            };
            rt.MouseLeave += (s, e) =>
            {
                ((Label)s).Background = System.Windows.Media.Brushes.DarkGreen;
                
            };

    
            sp.Children.Add(rt);
            
            foreach (string p in perc)
            {                
                if (counter++ < num_base ) continue;
                Label lb = new Label();
                lb.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#2C4566");
                lb.Foreground = System.Windows.Media.Brushes.AliceBlue;
                lb.Content = p;
                lb.VerticalContentAlignment = VerticalAlignment.Center;
                lb.BorderThickness = new Thickness(2, 2, 2, 2); 
                lb.BorderBrush = System.Windows.Media.Brushes.LightGray;
                lb.HorizontalAlignment = HorizontalAlignment.Left;
                lb.MouseLeftButtonDown += (s, e) => {                    
                    ((StackPanel)this.FindName("fs_grid")).Children.Clear(); 
                    currentDirectory = "";
                    foreach (string a in perc)
                    {
                        currentDirectory += a;
                        if (a == p) break;
                        currentDirectory += "\\";
                    } 
                    updateAddress();
                    addCurrentFoderInfo(currentDirectory);
                };
                lb.MouseEnter += (s, e) =>
                {
                    ((Label)s).Background = System.Windows.Media.Brushes.LightBlue;
                    ((Label)s).BorderBrush = System.Windows.Media.Brushes.White;
                };
                lb.MouseLeave += (s, e) =>
                {
                    ((Label)s).Background = (System.Windows.Media.Brush)bc.ConvertFrom("#2C4566"); 

                };
                sp.Children.Add(lb);
            }
            double addrWidth = 0;
            
            foreach (Label c in sp.Children)
            {
                c.UpdateLayout();
                addrWidth += c.ActualWidth; 
            }
            sp.UpdateLayout();
            var scr = ((ScrollViewer)this.FindName("scrolladd"));
            if (addrWidth > scr.Width)
            {
                scr.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                scr.Height = 50;
            }
            else
            {
                ((ScrollViewer)this.FindName("scrolladd")).HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                scr.Height = 30;
            }

        }

        public void sync()
        {
            Debug.WriteLine("sync called");
            NetworkHandler.getInstance().addFunction(syncFolder);
        }

        private void syncFolder(Socket socket)
        {
            Debug.WriteLine("THREAD STARTED");
            socket.Send(BitConverter.GetBytes(8));  // == ENUM.getUserFiles
            byte[] buffer = new byte[4096];
            
            int received = socket.Receive(buffer, 4096, SocketFlags.None);
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
                    catch (Exception)
                    {
                        MessageBox.Show("Impossibile inviare il file " + file + " al server", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Debug.WriteLine("Impossibile inviare il file " + file + " al server");
                    }
                }
            }
        }


        private void sendFileToServer(string path)
        {
            NetworkHandler.getInstance().addFunction ( (Socket socket) => {
                socket.Send(BitConverter.GetBytes(2)); // UPLOAD FILE
                socket.Send(Encoding.ASCII.GetBytes(path));

                byte[] inBuff = new byte[1024];
                socket.Receive(inBuff);
                if (!Encoding.ASCII.GetString(inBuff).Contains("OK")) throw new Exception("error: filename sent but error was returned");


                long dimension = (new FileInfo(path)).Length;
                if (dimension > Int32.MaxValue) throw new Exception("error: file dimension too big! > 32 bit");
                int dim = (int)dimension;

                socket.Send(BitConverter.GetBytes(dim));

                socket.Send(File.ReadAllBytes(path));

                socket.Receive(inBuff);
                if (!Encoding.ASCII.GetString(inBuff).Contains("OK")) throw new Exception("error: file not uploaded correctly");

                // todo: calculate and send sha1 checksum
                SHA1 shaProvider = SHA1.Create();
                shaProvider.ComputeHash(new FileStream(path, FileMode.Open));
                socket.Send(shaProvider.Hash);
            });
        }

        private void downloadFile(string path, string version)
        {
            // sending path
            s.Send(BitConverter.GetBytes(path.Length));
            s.Send(Encoding.ASCII.GetBytes(path));
            // check if it's ok
            byte[] buffer = new byte[1024];
            int ricevuti = s.Receive(buffer);
            buffer[ricevuti] = (byte)'\0';
            string msg = Encoding.ASCII.GetString(buffer);
            if (ricevuti != 2 || !msg.Contains("OK")) // there was an error
            {
                MessageBox.Show("Errore: Impossibile mandare il nome del file correttamente", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // sending path
            s.Send(BitConverter.GetBytes(version.Length));
            s.Send(Encoding.ASCII.GetBytes(version));
            // check if it's ok
            ricevuti = s.Receive(buffer);
            buffer[ricevuti] = (byte)'\0';
            msg = Encoding.ASCII.GetString(buffer);
            if (ricevuti != 2 || !msg.Contains("OK")) // there was an error
            {
                MessageBox.Show("Errore: Impossibile mandare la versione del file correttamente", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // now i need to read the dimension of the file
            ricevuti = s.Receive(buffer);
            if (ricevuti != 4)
            {
                MessageBox.Show("Errore: La dimensione del file è arrivata corrotta", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int fDim = BitConverter.ToInt32(buffer, 0);
            if(fDim <= 0)
            {
                MessageBox.Show("Errore: La dimensione del file è negativa", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            FileStream stream = new FileStream(path, FileMode.Create);
            // todo: maybe it is better to use a tmp file, download and check the checksum. if all is ok overwrite on path!
            while(fDim > 0)
            {
                ricevuti = s.Receive(buffer);
                stream.Write(buffer, 0, ricevuti);
                fDim -= ricevuti;
            }

            ricevuti = s.Receive(buffer); // this is server hash
            SHA1 shaProvider = SHA1.Create();
            shaProvider.ComputeHash(new FileStream(path, FileMode.Open));
            byte[] chash = shaProvider.Hash;

            bool equal = true;
            for (int i = 0; i < ricevuti && equal; i++)
            {
                if ((byte)buffer[i] != chash[i]) equal = false;
            }

            if (equal) s.Send(Encoding.ASCII.GetBytes("OK"));
            else s.Send(Encoding.ASCII.GetBytes("ERR"));

            ricevuti = s.Receive(buffer);
            buffer[ricevuti] = 0;
            if (Encoding.ASCII.GetString(buffer).Contains("OK"))
                // todo: here i should overwrite the file on path
                return;
            else
            {
                MessageBox.Show("Errore, impossibile tornare alla versione del file del " + version, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
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
            fs.IncludeSubdirectories = true;
            fs.NotifyFilter = NotifyFilters.LastWrite;
            fs.EnableRaisingEvents = true;

        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            //MessageBox.Show("File: " + e.FullPath + " " + e.ChangeType);
            Monitor.Enter(events_semaphore);
            queueObject q = new queueObject();
            q.file = e.FullPath; q.type = e.ChangeType;
            if (!eventsArray.Contains(q)) eventsArray.Enqueue(q);
            Monitor.Exit(events_semaphore);


            Thread t = new Thread(() =>
            {
                Thread.Sleep(5); // to avoid duplicated changes (known bug of the filesystewatcher)
                Monitor.Enter(events_semaphore);
                if(eventsArray.Count > 0)
                {
                    queueObject obj = eventsArray.Dequeue();
                    if(obj.type == WatcherChangeTypes.Changed || obj.type == WatcherChangeTypes.Created) sendFileToServer(obj.file);
                }
                Monitor.Exit(events_semaphore);
            });
            t.Start();
            
        }

        private void mouse_MouseDown(object sender, MouseButtonEventArgs e)
        {
         
            //if (sender.GetType().FullName=="StackPanel") M
            try {
                this.DragMove();
            }catch(InvalidOperationException)
            {
                // this means that someone has already cought the mousedown event. probably i did not want to move the window
            }
        }

        private void mouse_x_click(object sender, RoutedEventArgs e)
        {
            // todo: send to try bar
            this.Close();

            /* CODICE PROVVISORIO*/
            NetworkHandler.getInstance().killWorkers();
            NetworkHandler.deleteInstance();

        }


        private void MouseFolderButtonDownHandler(object sender, MouseButtonEventArgs e)
        {

            ((StackPanel)this.FindName("fs_grid")).Children.Clear(); // remove all childs
            Panel p = (Panel)sender;
            TextBlock lblDirectory = (TextBlock)p.Children[1];            
            string newDir = (string)(lblDirectory).Text;
            currentDirectory += ("\\" + newDir);            
            updateAddress();
            addCurrentFoderInfo(currentDirectory);
            
        }


        private void MouseFileButtonDownHandler(object sender, RoutedEventArgs e) {
            //  Grid.SetColumnSpan((UIElement)this.FindName("fs_grid"), 1);
            if (flag == false) return;

            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Visible;
            // i start here a thread in order to download the versions of this file
            ((StackPanel)this.FindName("panel_details")).Children.Clear();
            string filename = (string)((TextBlock)((StackPanel)sender).Children[1]).Text;
            this.selectedFile = currentDirectory + "\\" + filename;

            NetworkHandler.getInstance().addFunction( (Socket socket) =>
           {
               Debug.WriteLine("Into downloader (versions) thread");
               socket.Send(BitConverter.GetBytes(5)); // GET FILE VERSIONS

               string pathToSend = currentDirectory + "\\" + filename;
               socket.Send(BitConverter.GetBytes(pathToSend.Length));
               socket.Send(Encoding.ASCII.GetBytes(pathToSend));
               Debug.WriteLine("sent " + pathToSend);


               byte[] dim = new byte[4]; // just the space for an int
               if(s.Receive(dim) != 4)
               {
                   Debug.WriteLine("did not receive a valid number");
                   return;
               }
               if(BitConverter.ToInt32(dim, 0) <= 0)
               {
                   // an error server side has occurred!
                   Debug.WriteLine("dim of versions < 0");
                   return;
               }
               else
                   Debug.WriteLine("dim = " + BitConverter.ToInt32(dim, 0));


               byte[] buff = new byte[BitConverter.ToInt32(dim, 0)+1];
               socket.Receive(buff); // receive json

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
            
            
    
            Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("key_details_animation");


            //rowElements = 7;
            ((StackPanel)this.FindName("fs_grid")).Children.Clear();
            addCurrentFoderInfo(currentDirectory);
            flag = false;
            sb.Completed += (object s, EventArgs ev) =>
            {
                flag = true;
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
               

                System.Windows.Controls.Image img_folder = new System.Windows.Controls.Image();
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

            i = rowElements;


            foreach (string file in Directory.GetFiles(path))
            {
                if ((i % rowElements) == 0)
                {
                    hpanel = new StackPanel();
                    hpanel.VerticalAlignment = VerticalAlignment.Center;
                    hpanel.Orientation = Orientation.Horizontal;
                    hpanel.Margin = new Thickness(5, 5, 0, 0);
                };
               
                i++;
                StackPanel panel = new StackPanel();
                panel.Width = 100;
                panel.Height = 85;
                panel.Name = "file_panel";
                panel.VerticalAlignment = VerticalAlignment.Center;
                panel.HorizontalAlignment = HorizontalAlignment.Center;
                panel.Orientation = Orientation.Vertical;
                panel.MouseLeftButtonDown += MouseFileButtonDownHandler;


                System.Windows.Controls.Image img_file = new System.Windows.Controls.Image();
                img_file.Source = new BitmapImage(new Uri(@"\images\fileIcon.png", UriKind.RelativeOrAbsolute));

                img_file.Width = 50;
                img_file.Height = 50;
                panel.Children.Add(img_file);


                TextBlock lbl_file_name = new TextBlock();

                lbl_file_name.MaxWidth = 85;
                lbl_file_name.MinWidth = 40;
                lbl_file_name.TextWrapping = TextWrapping.Wrap;
                lbl_file_name.TextAlignment = TextAlignment.Center;

                lbl_file_name.Name = "lbl_folder_name";
                lbl_file_name.Text = file.Split('\\')[file.Split('\\').Length - 1];
                panel.Children.Add(lbl_file_name);
           
       
                hpanel.Children.Add(panel);
                if (((i - 1) % rowElements) == 0) g.Children.Add(hpanel);
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
            watchFolder();
        }

   
        private void closeVersions(object sender, MouseButtonEventArgs e)
        {
            if (flag == false) return;
            selectedFile = null;
            int span = Grid.GetColumnSpan((UIElement)this.FindName("fs_grid"));
            if (span == 7 && flag) return;
            Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("key_details_animation_close");
            sb.Completed += closeSidebar;
            flag = false;
            sb.Begin();
        }

        private void closeSidebar(object sender, EventArgs e)
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
