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
    enum messages
    {
        LOGIN = 0,
        SIGNIN = 1,
        UPLOAD_FILE = 2,
        REMOVE_FILE = 3,
        DELETE_FILE = 4,
        GET_FILE_VERSIONS = 5,
        DOWNLOAD_PREVIOUS_VERSION = 6,
        GET_DELETED_FILES = 7,
        GET_USER_FOLDER = 8,
        GET_USER_PATH = 9,
        DOWNLOAD_LAST_VERSION = 10  
    }
    

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

        int rowElements;
        string currentDirectory;
        string root;
        bool first = true;
        bool flag = true;
        string[] months = { "GEN", "FEB","MAR","APR","MAG","GIU","LUG","AGO","SET","OTT","NOV","DIC"};
        Mutex saveFlag;
        string selectedFile;
        FileSystemWatcher fs;



        public MainWindow()
        {
            InitializeComponent();
            fs = null;
            saveFlag = new Mutex();
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
                
            };
            // <Label x:Name="label" Background="#2C4566" Foreground="AliceBlue" Content="C:\\" HorizontalAlignment="Left" VerticalAlignment="Top" Margin="2,2,0,0"/>


        }

        /*
            FUNCTIONS THAT MODIFY GRAPHICAL INTERFACE
        */

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
            // now in the string we have the JSON string description. it is "[{"path":"...", "name":"......"}]"

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

        public void updateAddress()
        {
            StackPanel sp = ((StackPanel)FindName("address"));
            sp.Children.Clear();
            var bc = new BrushConverter();
            int num_base = root.Split('\\').Length;
            string[] perc = currentDirectory.Split('\\');
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
                if (counter++ < num_base) continue;
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

        private void insertFilesFromJSON(List<JSONDeletedFile> items, Boolean trash)
        {
            ((StackPanel)this.FindName("fs_grid")).Children.Clear();


            int i = 0;
            StackPanel hpanel = null;
            foreach (JSONDeletedFile file in items)
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
                if (!trash) panel.MouseLeftButtonDown += MouseFileButtonDownHandler;
                else panel.MouseLeftButtonDown += MouseFileThrashHandler;



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
                lbl_file_name.Text = file.name;
                panel.Children.Add(lbl_file_name);

                TextBlock hddn_path = new TextBlock();
                hddn_path.Visibility = Visibility.Collapsed;
                hddn_path.Name = "hidden_path";
                hddn_path.Text = file.path; // the complete path of the file
                panel.Children.Add(hddn_path);


                hpanel.Children.Add(panel);
                if (((i - 1) % rowElements) == 0) ((StackPanel)this.FindName("fs_grid")).Children.Add(hpanel);
            }
        }

        private Border getCalendar(string date, BrushConverter bc, string completePath)
        {
            string hour_s = date.Split(' ')[1];
            string year_s = date.Split(' ')[0].Split('-')[0];
            string month_s = date.Split(' ')[0].Split('-')[1];
            string day_s = date.Split(' ')[0].Split('-')[2];

            Border brd = new Border();
            brd.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#111221");
            brd.BorderThickness = new Thickness(1);
            brd.Height = 80;

            StackPanel sline = new StackPanel();
            sline.Orientation = Orientation.Horizontal;
            sline.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) =>
            {
                downloadFile(completePath, date);
            };

            StackPanel calendar = new StackPanel();
            calendar.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#D2691E");
            calendar.Width = 50;
            calendar.HorizontalAlignment = HorizontalAlignment.Left;
            calendar.Margin = new Thickness(20, 18, 0, 11);


            TextBlock year = new TextBlock();
            year.FontSize = 12;
            year.Foreground = (System.Windows.Media.Brush)bc.ConvertFrom("#111221");
            year.FontWeight = FontWeights.SemiBold;
            year.VerticalAlignment = VerticalAlignment.Center;
            year.TextAlignment = TextAlignment.Center;
            year.Height = 15;
            year.Text = year_s;


            TextBlock day = new TextBlock();
            //< TextBlock Text = "15" FontSize = "24" Foreground = "#111221" LineStackingStrategy = 
            //"BlockLineHeight" LineHeight = "21" TextOptions.TextFormattingMode = "Display"
            //FontWeight = "Bold" VerticalAlignment = "Top" TextAlignment = "Center" Height = "20" />
            day.FontSize = 24;
            day.Foreground = (System.Windows.Media.Brush)bc.ConvertFrom("#111221");
            day.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            day.LineHeight = 21;
            day.FontWeight = FontWeights.Bold;
            day.VerticalAlignment = VerticalAlignment.Top;
            day.TextAlignment = TextAlignment.Center;
            day.Height = 20;
            day.Text = day_s;
            TextBlock month = new TextBlock();
            //< TextBlock Text = "FEB" FontSize = "16" Foreground = "#111221" 
            // LineStackingStrategy = "BlockLineHeight" LineHeight = "13"  TextOptions.TextFormattingMode
            //  = "Display" Padding = "0,0,0,0" FontWeight = "SemiBold" VerticalAlignment = "Stretch"
            //TextAlignment = "Center" Height = "12" />
            month.FontSize = 16;
            month.Foreground = (System.Windows.Media.Brush)bc.ConvertFrom("#111221");
            month.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            month.LineHeight = 13;
            month.FontWeight = FontWeights.SemiBold;
            month.VerticalAlignment = VerticalAlignment.Stretch;
            month.TextAlignment = TextAlignment.Center;
            month.Height = 12;
            month.Text = months[Int32.Parse(month_s) - 1];

            //< TextBlock Text = "ORE  17:30" FontSize = "20" Foreground = "AliceBlue"  
            //TextOptions.TextFormattingMode = "Display" FontWeight = "SemiBold" 
            //VerticalAlignment = "Top" TextAlignment = "Center" HorizontalAlignment = "Left" 
            //Margin = "35,28,0,0" />

            TextBlock hour = new TextBlock();
            hour.FontSize = 20;
            hour.Foreground = new SolidColorBrush(Colors.AliceBlue);
            //hour.FontWeight = FontWeights.SemiBold;
            hour.VerticalAlignment = VerticalAlignment.Top;
            hour.TextAlignment = TextAlignment.Center;
            hour.HorizontalAlignment = HorizontalAlignment.Left;
            hour.Margin = new Thickness(35, 28, 0, 0);
            hour.Text = "ORE " + hour_s;

            //  <Image Source="images/download.png"  Margin="45,10,0,0" Width="30"/>
            System.Windows.Controls.Image dwn = new System.Windows.Controls.Image();
            dwn.Source = new BitmapImage(new Uri(@"\images\download.png", UriKind.RelativeOrAbsolute));
            dwn.Width = 30;
            dwn.Margin = new Thickness(45, 10, 0, 0);

            calendar.Children.Add(year);
            calendar.Children.Add(day);
            calendar.Children.Add(month);

            sline.Children.Add(calendar);
            sline.Children.Add(hour);
            sline.Children.Add(dwn);
            brd.Child = sline;

            return brd;
        }

        private void addCurrentFoderInfo(string path)
        {
 
            Debug.WriteLine("into addCurrentFoderInfo");
            StackPanel g = (StackPanel)this.FindName("fs_grid");
            g.Children.Clear();
            StackPanel hpanel = null;
            int i = rowElements;

            // code to add the thrash folder

            List<string> lista = new List<string>(Directory.GetDirectories(path));
            if (path.Equals(root)) lista.Add("\\Cestino");
            foreach (string dir in lista)
            {
                if ((i % rowElements) == 0)
                {
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
                if (!dir.Equals("\\Cestino"))
                    panel.MouseLeftButtonDown += MouseFolderButtonDownHandler;
                else
                    panel.MouseLeftButtonDown += MouseTrashHandler;


                System.Windows.Controls.Image img_folder = new System.Windows.Controls.Image();
                if (!dir.Equals("\\Cestino"))
                    img_folder.Source = new BitmapImage(new Uri(@"\images\folderIcon.png", UriKind.RelativeOrAbsolute));
                else
                    img_folder.Source = new BitmapImage(new Uri(@"\images\trash.png", UriKind.RelativeOrAbsolute));

                img_folder.Width = 50;
                img_folder.Height = 50;
                panel.Children.Add(img_folder);

                TextBlock lbl_dir_name = new TextBlock();

                lbl_dir_name.MaxWidth = 85;
                lbl_dir_name.MinWidth = 40;
                lbl_dir_name.TextWrapping = TextWrapping.Wrap;
                lbl_dir_name.TextAlignment = TextAlignment.Center;

                lbl_dir_name.Name = "lbl_folder_name";
                lbl_dir_name.Text = dir.Split('\\')[dir.Split('\\').Length - 1];
                panel.Children.Add(lbl_dir_name);


                hpanel.Children.Add(panel);
                if (((i - 1) % rowElements) == 0) g.Children.Add(hpanel);
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
        
        /*
            UPLOAD AND DOWNLOAD OF FILES
        */

        private void sendFileToServer(string path)
        {
            NetworkHandler.getInstance().addFunction ( (Socket socket) => {
                socket.Send(BitConverter.GetBytes((int)messages.UPLOAD_FILE)); // UPLOAD FILE
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
                socket.Receive(inBuff);
                if (!Encoding.ASCII.GetString(inBuff).Contains("OK")) MessageBox.Show("sha non accettato");
            });
        }

        private void downloadFile(string path, string version)
        {
            NetworkHandler.getInstance().addFunction((Socket s) => {
                // selecting operation
                int ricevuti;
                byte[] buffer = new byte[1024];
                if (version == null)
                {
                    // downloading the last version
                    s.Send(BitConverter.GetBytes((int)messages.DOWNLOAD_LAST_VERSION));
                    s.Send(BitConverter.GetBytes(path.Length));
                    s.Send(Encoding.ASCII.GetBytes(path));
                    // check if it's ok

                    ricevuti = s.Receive(buffer);
                    buffer[ricevuti] = (byte)'\0';
                    string msg = Encoding.ASCII.GetString(buffer);
                    if (ricevuti != 2 || !msg.Contains("OK")) // there was an error
                    {
                        MessageBox.Show("Errore: Impossibile mandare il nome del file correttamente", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    // downloading a specific old version
                    s.Send(BitConverter.GetBytes((int)messages.DOWNLOAD_PREVIOUS_VERSION));
                    Debug.WriteLine("voglio scaricare la versione del {0} di {1}", version, path);
                    // sending path
                    s.Send(BitConverter.GetBytes(path.Length));
                    s.Send(Encoding.ASCII.GetBytes(path));
                    // check if it's ok
                    
                    ricevuti = s.Receive(buffer);
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
                }


                
                string tmp_path = "C:\\Temp\\polihub.tmp"; // what if that file is already there?
                
                

                // now i need to read the dimension of the file
                ricevuti = s.Receive(buffer);
                if (ricevuti != 4)
                {
                    MessageBox.Show("Errore: La dimensione del file è arrivata corrotta", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                {
                    Debug.WriteLine("La vecchia versione pesa {0}", BitConverter.ToInt32(buffer, 0));
                }

                int fDim = BitConverter.ToInt32(buffer, 0);
                if (fDim <= 0)
                {
                    MessageBox.Show("Errore: La dimensione del file è negativa", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                FileStream stream = new FileStream(tmp_path, FileMode.Create);
                while (fDim > 0)
                {
                    ricevuti = s.Receive(buffer);
                    stream.Write(buffer, 0, ricevuti);
                    fDim -= ricevuti;
                }
                stream.Close();


                s.Send(Encoding.ASCII.GetBytes("OK"));

                Debug.WriteLine("aspetto l'hash del server");
                ricevuti = s.Receive(buffer); // this is server hash
                SHA1 shaProvider = SHA1.Create();


                FileStream tmpStr = new FileStream(tmp_path, FileMode.Open);
                byte[] chash = shaProvider.ComputeHash(tmpStr);
                tmpStr.Close();

                Debug.WriteLine("Calcolo se gli hash sono uguali. |chash| = {0}, |shash| = {1}", chash.Length, ricevuti);
                buffer[ricevuti] = 0;
                string str_chash = BitConverter.ToString(chash).Replace("-", "");
                string str_shash = Encoding.ASCII.GetString(buffer, 0, 40).ToUpper();

                Debug.WriteLine("{0}\n{1}", str_shash, str_chash);



                if (!str_chash.Equals(str_shash))
                {

                    MessageBox.Show("Gli hash sono diversi", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    s.Send(Encoding.ASCII.GetBytes("ERR"));
                    File.Delete(tmp_path);
                    return;
                }
                else
                    s.Send(Encoding.ASCII.GetBytes("OK"));


                s.Receive(buffer);
                if (!Encoding.ASCII.GetString(buffer).Contains("OK"))
                {
                    MessageBox.Show("Impossibile aggiungere una nuova versione lato server", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }



                fs.EnableRaisingEvents = false;
                Debug.WriteLine("File.Delete( " + path + " );");
                if(version != null) File.Delete(path);
                Debug.WriteLine("File.Move({0}, {1});", path+".tmp", path);
                File.Move(tmp_path, path);
                fs.EnableRaisingEvents = true;
                
                return; 
            });
        }

        /*
            UTILITY FUNCTIONS, MOSTLY SETTERS
        */

        private void watchFolder()
        {
            fs = new FileSystemWatcher(currentDirectory);
            
            fs.Changed += new FileSystemEventHandler(OnChanged);
            fs.Created += new FileSystemEventHandler(OnChanged);
            fs.Deleted += new FileSystemEventHandler(OnChanged);

            fs.IncludeSubdirectories = true;
            fs.NotifyFilter = NotifyFilters.FileName;
            fs.EnableRaisingEvents = true;

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

        private void setFlag(bool f)
        {

            Monitor.Enter(saveFlag);
            flag = f;
            Monitor.Exit(saveFlag);

        }


        /*
            EVENT HANDLERS
        */
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
                    if (obj.type == WatcherChangeTypes.Deleted) fileDeleted(obj.file);
                    Dispatcher.Invoke(updateFolders);
                }
                Monitor.Exit(events_semaphore);
            });
            t.Start();
            
        }

        public void fileDeleted(string path)
        {
            NetworkHandler.getInstance().addFunction((Socket socket) =>
            {
                byte[] buffer = new byte[5];
                socket.Send(BitConverter.GetBytes(4));
                socket.Send(Encoding.ASCII.GetBytes(path));
                Debug.WriteLine("Voglio cancellare il file: " + path);
                socket.Receive(buffer);
                if (Encoding.ASCII.GetString(buffer).Contains("ERR"))
                    MessageBox.Show("Errore: errore nel comunicare al server che il file " + path + "e' stato cancellato", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }); 
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

            
            Debug.WriteLine("MouseFileButtonDownHandler called");

            //  Grid.SetColumnSpan((UIElement)this.FindName("fs_grid"), 1);
            
            Monitor.Enter(saveFlag);
            if (flag == false)
            {
                Monitor.Exit(saveFlag);
                return;
            }
            Monitor.Exit(saveFlag);
            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Visible;
            // i start here a thread in order to download the versions of this file
           // ((StackPanel)this.FindName("panel_details")).Children.Clear();
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
               if(socket.Receive(dim) != 4)
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
               BrushConverter bc = new BrushConverter();
               foreach (JSONVersion v in items)
               {
                   Debug.WriteLine("v.date = " + v.date);
                   Dispatcher.Invoke(()=>
                   {
                       ((Panel)FindName("panel_details")).Children.Add(getCalendar(v.date, bc, pathToSend));
                       //Debug.WriteLine("inserted the new line -> " + line.Text);
                   });
               }
               return;
           }
            );
            
    
            Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("key_details_animation");


            //rowElements = 7;
            ((StackPanel)this.FindName("fs_grid")).Children.Clear();
            addCurrentFoderInfo(currentDirectory);
           
            setFlag(false);
            sb.Completed += (object s, EventArgs ev) =>
            {
                setFlag(true);
            };
            sb.Begin();

            e.Handled = true;
        }

        private void MouseTrashHandler(object sender, RoutedEventArgs e)
        {
            currentDirectory = "trash";


            StackPanel sp = ((StackPanel)FindName("address"));
            Label rt = new Label();
            rt.Background = System.Windows.Media.Brushes.DarkRed;
            rt.Foreground = System.Windows.Media.Brushes.AliceBlue;
            rt.BorderThickness = new Thickness(2, 2, 2, 2);
            rt.BorderBrush = System.Windows.Media.Brushes.LightGray;
            rt.Content = "Cestino";
            rt.VerticalContentAlignment = VerticalAlignment.Center;
            rt.HorizontalAlignment = HorizontalAlignment.Left;
            rt.MouseEnter += (s, er) =>
            {
                ((Label)s).Background = System.Windows.Media.Brushes.Red;
                ((Label)s).BorderBrush = System.Windows.Media.Brushes.White;
            };
            rt.MouseLeave += (s, er) =>
            {
                ((Label)s).Background = System.Windows.Media.Brushes.DarkRed;

            };


            sp.Children.Add(rt);



            NetworkHandler.getInstance().addFunction((Socket socket) =>
            {
                socket.Send(BitConverter.GetBytes(7));
                byte[] dim = new byte[4];
                int ricevuti = socket.Receive(dim);
                int dimension = BitConverter.ToInt32(dim, 0);
                byte[] buffer = new byte[dimension];
                ricevuti = socket.Receive(buffer);

                string s = Encoding.ASCII.GetString(buffer);
                List<JSONDeletedFile> items = JsonConvert.DeserializeObject<List<JSONDeletedFile>>(s);

                Debug.WriteLine("deletedFiles: \n " + s);

                Dispatcher.Invoke(() => {
                    insertFilesFromJSON(items, true);
                });


            });
        }

        private void MouseFileThrashHandler(object sender, RoutedEventArgs e)
        {
            string path =
            ((TextBlock)((Panel)sender).Children[2]).Text + "\\" + ((TextBlock)((Panel)sender).Children[1]).Text;
            MessageBoxResult res =  MessageBox.Show("Vuoi davvero ripristinare il file " + path + "?", "Ripristino", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.No) return;
            downloadFile(path, null);
        }

        private void closeVersions(object sender, MouseButtonEventArgs e)
        {
            Monitor.Enter(saveFlag);
            if (flag == false)
            {
                Monitor.Exit(saveFlag);
                return;
            }
            Monitor.Exit(saveFlag);
            selectedFile = null;
            int span = Grid.GetColumnSpan((UIElement)this.FindName("fs_grid"));
            if (span == 7 && flag) return;
            Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("key_details_animation_close");
            sb.Completed += closeSidebar;
            setFlag(false);
            sb.Begin();
        }

        private void closeSidebar(object sender, EventArgs e)
        {
            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Collapsed;
            //Grid.SetColumnSpan((UIElement)this.FindName("fs_grid"), 7);
            // rowElements = 10;
            ((StackPanel)this.FindName("fs_grid")).Children.Clear();
            addCurrentFoderInfo(currentDirectory);
            setFlag(true);
        }

        private void TS_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
          
            ScrollViewer scrollviewer = sender as ScrollViewer;
            if (e.Delta > 0)
            {
                scrollviewer.LineLeft();
            }
            else
            {
                scrollviewer.LineRight();
            }
            e.Handled = true;
        }


    }

    
}
