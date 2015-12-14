using System;
using System.Collections.Generic;
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
using System.Diagnostics;
using System.Threading;
using System.Drawing.Imaging;
using System.Drawing;
using WpfAnimatedGif;
using System.Reflection;
using System.ComponentModel;

namespace PDS_Client
{
    ///// <summary>
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
        DOWNLOAD_LAST_VERSION = 10,
        SEND_PATH = 11
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
        List<MouseButtonEventHandler> restoreHandlers;
        int rowElements;
        string currentDirectory;
        string root;
        bool first = true;
        bool flag = true;
        
        string[] months = { "GEN", "FEB","MAR","APR","MAG","GIU","LUG","AGO","SET","OTT","NOV","DIC"};
        Mutex saveFlag;
        string selectedFile;
        FileSystemWatcher fs;
        System.Windows.Forms.NotifyIcon noty;
        List<System.Windows.Controls.Image> images = new List<System.Windows.Controls.Image>();
        List<StackPanel> calendars = new List<StackPanel>();
        Mutex singleDownload = new Mutex();
        int counterFly = 0;
        int marginFly = 1;
        double angleFly = 0.0;
        bool rotateFly = false;

        bool canMoveSidebar=true;


        public MainWindow(string user)
        {
            InitializeComponent();
            fs = null;
            restoreHandlers = new List<MouseButtonEventHandler>();
            noty = new System.Windows.Forms.NotifyIcon();
            noty.Icon = new System.Drawing.Icon(@"images\\ico.ico");
            noty.DoubleClick += (s,e) =>
            {
                this.Show();
                noty.Visible = false;
            };
            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();
            menu.MenuItems.Add("Apri");
            menu.MenuItems.Add("Logout");
            menu.MenuItems.Add("Esci");
            
            ((Label)FindName("user_label")).Content =user[0];
            ((Label)FindName("user_label")).ToolTip = user+"@poliHub";


            menu.MenuItems[0].Click += (s,e) =>
            {
                this.Show();
                noty.Visible = false;
            };

            menu.MenuItems[1].Click += (s, e) =>
            {
                NetworkHandler.getInstance().killWorkers();
                NetworkHandler.deleteInstance();
                File.Delete("./polihub.settings");
                Window1 loginPage = new Window1();
                loginPage.Show();
                this.Close();
            };

            menu.MenuItems[2].Click += (s, e) =>
            {                
                noty.Visible = false;
                NetworkHandler.getInstance().killWorkers();
                NetworkHandler.deleteInstance();
                noty = null;
                Environment.Exit(0);
            };

            noty.ContextMenu = menu;
            
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


            System.Windows.Controls.Image fly = (System.Windows.Controls.Image)((Grid)this.FindName("fs_container")).FindName("fly");
            fly.Visibility = Visibility.Visible;
            Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("flymove2");
            sb.Begin();
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(move);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 25);
            timer.Start();

            fly.MouseLeftButtonDown += (s, e) =>
            {
                Storyboard sb2 = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("flyfly");
                Storyboard sb1 = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("flymove2");
                sb1.Stop();
                timer.Stop();
                counterFly = 300;
                fly.RenderTransform = new RotateTransform(0, 0.5, 0.5);
                sb2.Begin();             
                System.Windows.Threading.DispatcherTimer timer2 = new System.Windows.Threading.DispatcherTimer();
                timer2.Tick += new EventHandler((st, et) => {
                    if(counterFly!=0)
                    {
                        ((Label)this.FindName("donwstring")).Content = "" + marginFly + "" + counterFly;
                        counterFly--;
                        fly.Margin = new Thickness(fly.Margin.Left -15 , fly.Margin.Top - 15, fly.Margin.Right, fly.Margin.Bottom);
                    }
                    else
                    {
                        timer2.Stop();
                        sb2.Stop();
                        fly.Margin = new Thickness(449, 448, 0, 0);

                    }

                });
                timer2.Interval = new TimeSpan(0, 0, 0, 0, 1);
                timer2.Start();


            };
                
          
           
        }

        void move(object sender, EventArgs e)
        { //Margin="508,315,170,0"
            System.Windows.Controls.Image fly = (System.Windows.Controls.Image)((Grid)this.FindName("fs_container")).FindName("fly");
            fly.Visibility = Visibility.Collapsed;

            if (counterFly<32)
            {
                ((Label)this.FindName("donwstring")).Content = ""+ marginFly + "" + counterFly;
                counterFly++;
                Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("flymove2");
                sb.Resume();

                if (marginFly >= 5 )
                {
                    counterFly += 2;
                    sb.Pause();
                    fly.Margin = new Thickness(fly.Margin.Left, fly.Margin.Top, fly.Margin.Right, fly.Margin.Bottom);
                }
      
                //fly.RenderTransform = new RotateTransform(0);
                if (marginFly == 1)
                {
                    fly.RenderTransform = new RotateTransform(0,0.5,0.5);
                    fly.Margin = new Thickness(fly.Margin.Left, fly.Margin.Top - 2, fly.Margin.Right, fly.Margin.Bottom);
                }
                if (marginFly == 2)
                {
                    fly.RenderTransform = new RotateTransform(-90,0.5,0.5);
                    fly.Margin = new Thickness(fly.Margin.Left - 2, fly.Margin.Top, fly.Margin.Right, fly.Margin.Bottom);
                }
                if (marginFly == 3)
                {
                    fly.RenderTransform = new RotateTransform(+90,0.5,0.5);
                    fly.Margin = new Thickness(fly.Margin.Left+2, fly.Margin.Top, fly.Margin.Right, fly.Margin.Bottom);
                }
                if (marginFly == 4)
                {
                    fly.RenderTransform = new RotateTransform(180,0.5,0.5);
                    fly.Margin = new Thickness(fly.Margin.Left, fly.Margin.Top+2, fly.Margin.Right, fly.Margin.Bottom);
                }


            }
            else
            {
                counterFly = 0;
                Random rnd = new Random();
                marginFly = rnd.Next(1, 12); 
              

            }

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
            socket.Send(BitConverter.GetBytes((int)messages.GET_USER_FOLDER)); 
            byte[] buffer = new byte[4096];
            
            int received = socket.Receive(buffer, 4096, SocketFlags.None);
            string serverFolderDescription = Encoding.UTF8.GetString(buffer);
            serverFolderDescription = serverFolderDescription.Remove(received);
            // now in the string we have the JSON string description. it is "[{"path":"...", "name":"......"}]"

            Debug.WriteLine("JSON rappresentation of the folder status on the server: \n" + serverFolderDescription + "\n");
            
            List<JSON_Folder_Items> items = JsonConvert.DeserializeObject<List<JSON_Folder_Items>>(serverFolderDescription);
            
            checkFileExists(currentDirectory, items);

            Debug.WriteLine("The server has {0} files more then me. i need to download them", items.Count);
            foreach (JSON_Folder_Items it in items)
            {
                if (it.path.Length > 0) Debug.Write("{0}\\", it.path);
                Debug.WriteLine("{0}", it.name);
                downloadFile(it.path +"\\"+ it.name, null);
            }           

        }

        private void checkFileExists(string basePath,  List<JSON_Folder_Items> items)
        {
            foreach (string p in System.IO.Directory.GetDirectories(basePath)) checkFileExists(p, items);

            foreach (string file in System.IO.Directory.GetFiles(basePath)) {
                JSON_Folder_Items item = new JSON_Folder_Items();
                string[] splitPath = file.Split('\\');
                item.name = splitPath[splitPath.Length-1];
                item.path = basePath;
                item.checksum = BitConverter.ToString(getSha1(file)).Replace("-", "").ToLower();
                if (!items.Contains(item))
                {
                    Debug.WriteLine(file + "is NOT present");
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
                else
                {
                    Debug.WriteLine(file + "is present");
                    for (int i = 0; i < items.Count; i++)
                    { 
                        if (items[i] == item)
                        {
                            if (items[i].checksum.Equals(item.checksum))
                            {
                                items.Remove(items[i]);
                            }
                            else
                            {
                                Debug.WriteLine("checksums are different");

                                // i need to get the date of last modify

                                DateTime lastModified = File.GetLastWriteTime(file);

                                if (lastModified.CompareTo(DateTime.Parse(items[i].date)) > 0) // this means that the server has an older version 
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
                                else // the version of the server is more recent than mine
                                {
                                    try {
                                        downloadFile(file, null);
                                    }
                                    catch (Exception)
                                    {
                                        MessageBox.Show("Impossibile scaricare il file " + file + " dal server", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        Debug.WriteLine("Impossibile scaricare il file " + file + " dal server");
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }

        /*
            This function is used to update the address bar, where we can se the current path
        */
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
                if (((UIElement)this.FindName("details_container")).Visibility != Visibility.Collapsed) return;
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
                    if (((UIElement)this.FindName("details_container")).Visibility != Visibility.Collapsed) return;
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


        private void insertFilesFromJSON(List<JSONDeletedFile> items, bool trash)
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

        /*
            This functions creates the storyline of the selected file in the right bar
        */
        private Border getCalendar(string date, BrushConverter bc, string completePath ,string filename)
        {
            string hour_s = date.Split(' ')[1].Split(':')[0]+":"+date.Split(' ')[1].Split(':')[1];
            string year_s = date.Split(' ')[0].Split('-')[0];
            string month_s = date.Split(' ')[0].Split('-')[1];
            string day_s = date.Split(' ')[0].Split('-')[2];

            Border brd = new Border();
            brd.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#111221");
            brd.BorderThickness = new Thickness(1);
            brd.Height = 80;

            StackPanel sline = new StackPanel();
            sline.Orientation = Orientation.Horizontal;          

            StackPanel calendar = new StackPanel();
            calendar.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#D2691E");
            calendar.Width = 50;
            calendar.HorizontalAlignment = HorizontalAlignment.Left;
            calendar.Margin = new Thickness(20, 18, 0, 11);
            calendars.Add(calendar);

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
            images.Add(dwn);
            dwn.Source = new BitmapImage(new Uri(@"\images\download.png", UriKind.RelativeOrAbsolute));
            dwn.Width = 50;
            dwn.Margin = new Thickness(45, 5, 0, 0);

            dwn.MouseEnter += mousenter;
            dwn.MouseLeave += mouseleave;
            
            MouseButtonEventHandler del = null;
            del = (object sender, MouseButtonEventArgs e) => {
                try {
                    foreach (System.Windows.Controls.Image a in images)
                    {
                        a.Source = new BitmapImage(new Uri(@"\images\disable.png", UriKind.RelativeOrAbsolute));
                        foreach(MouseButtonEventHandler m in restoreHandlers)
                            a.MouseLeftButtonDown -= m;
                        a.MouseLeave -= mouseleave;                                              
                        a.MouseEnter -= mousenter;                   
                    }
                    foreach (StackPanel c in calendars)
                    {
                        c.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#d3d3d3");                    
                    }
                    calendar.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#D2691E");
                    dwn.BeginInit();
                    dwn.Source = new BitmapImage(new Uri(@"\images\down.gif", UriKind.RelativeOrAbsolute));
                    ImageBehavior.SetAnimatedSource((System.Windows.Controls.Image)sender, ((System.Windows.Controls.Image)sender).Source);                
                    var controller = ImageBehavior.GetAnimationController(dwn);
         
                    controller.Play();
                    dwn.EndInit();
                    Monitor.Enter(this);
                    canMoveSidebar = false;
                    Monitor.Exit(this);
         
                }catch
                {
                    Monitor.Exit(singleDownload);
                    return;
                }
                downloadFile(completePath, date, filename);

            };

            restoreHandlers.Add(del);

            dwn.MouseLeftButtonDown +=del;

            calendar.Children.Add(year);
            calendar.Children.Add(day);
            calendar.Children.Add(month);

            sline.Children.Add(calendar);
            sline.Children.Add(hour);
            sline.Children.Add(dwn);
            brd.Child = sline;

            return brd;
        }

        // used when mouse is moving on the download file icon
        private void mousenter(object sender, MouseEventArgs e)
        {                 
                  ((System.Windows.Controls.Image)sender).Source = new BitmapImage(new Uri(@"\images\downloadhigh.png", UriKind.RelativeOrAbsolute));
                  ImageBehavior.SetAnimatedSource(((System.Windows.Controls.Image)sender), ((System.Windows.Controls.Image)sender).Source);
            return;
        }
        // used when mouse is moving out of the download file icon
        private void mouseleave(object sender, MouseEventArgs e)
        {
            ((System.Windows.Controls.Image)sender).Source = new BitmapImage(new Uri(@"\images\download.png", UriKind.RelativeOrAbsolute));
        }


        /*
            This function creates the view of the files in the directory where the user is currently
        */
        private void addCurrentFoderInfo(string path)
        {
            Debug.WriteLine("into addCurrentFoderInfo");
            StackPanel g = (StackPanel)this.FindName("fs_grid");
            // <Image Source="/images/pixelart.png" x:Name="image1" Height="997" Margin="782,0,0,0"/>
            System.Windows.Controls.Image pixel = new System.Windows.Controls.Image();           
            pixel.Source = new BitmapImage(new Uri(@"\images\pixelart.png", UriKind.RelativeOrAbsolute));
            pixel.Height = 977;
            pixel.Margin = new Thickness(782, 0, 0, 0);
            if (!path.Equals("trash")) g.Children.Clear();
            
            StackPanel hpanel = null;
            int i = rowElements;

            // code to add the thrash folder
            if (path.Equals("trash")) return;
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
                BrushConverter bc = new BrushConverter();
                Border brpanel = new Border();
                brpanel.Background = System.Windows.Media.Brushes.Transparent;
                brpanel.BorderThickness = new Thickness(1);
                brpanel.BorderBrush = System.Windows.Media.Brushes.Transparent;
                brpanel.Height = 85;

                StackPanel panel = new StackPanel();
                panel.Width = 100;
                panel.Height = 84;
                panel.Name = "folder_panel";
                panel.VerticalAlignment = VerticalAlignment.Center;
                panel.HorizontalAlignment = HorizontalAlignment.Center;
                panel.Orientation = Orientation.Vertical;
                panel.MouseEnter += (s, e) =>
                {
                    panel.Background = System.Windows.Media.Brushes.LightBlue;
                    brpanel.BorderBrush = System.Windows.Media.Brushes.DeepSkyBlue;
                };
                panel.MouseLeave += (s, e) =>
                {
                    panel.Background = System.Windows.Media.Brushes.Transparent;
                    brpanel.BorderBrush = System.Windows.Media.Brushes.Transparent;
                };

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
                img_folder.Margin = new Thickness(0, 4, 0, 0);
                panel.Children.Add(img_folder);

                TextBlock lbl_dir_name = new TextBlock();

                lbl_dir_name.MaxWidth = 85;
                lbl_dir_name.MinWidth = 40;
                lbl_dir_name.TextWrapping = TextWrapping.Wrap;
                lbl_dir_name.TextAlignment = TextAlignment.Center;

                lbl_dir_name.Name = "lbl_folder_name";
                lbl_dir_name.Text = dir.Split('\\')[dir.Split('\\').Length - 1];
                panel.Children.Add(lbl_dir_name);

                brpanel.Child = panel;
                hpanel.Children.Add(brpanel);
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
                BrushConverter bc = new BrushConverter();
                Border brpanel = new Border();
                brpanel.Background = System.Windows.Media.Brushes.Transparent;
                brpanel.BorderThickness = new Thickness(1);
                brpanel.BorderBrush = System.Windows.Media.Brushes.Transparent;
                brpanel.Height = 85;
                
                StackPanel panel = new StackPanel();
                panel.Width = 100;
                panel.Height = 85;
                panel.Name = "file_panel";
                panel.VerticalAlignment = VerticalAlignment.Center;
                panel.HorizontalAlignment = HorizontalAlignment.Center;
                panel.Orientation = Orientation.Vertical;
                panel.MouseLeftButtonDown += MouseFileButtonDownHandler;

                panel.MouseEnter += (s, e) =>
                {
                    panel.Background = System.Windows.Media.Brushes.LightBlue;
                    brpanel.BorderBrush = System.Windows.Media.Brushes.DeepSkyBlue;
                };
                panel.MouseLeave += (s, e) =>
                {
                    panel.Background = System.Windows.Media.Brushes.Transparent;
                    brpanel.BorderBrush = System.Windows.Media.Brushes.Transparent;
                };

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


                brpanel.Child = panel;
                hpanel.Children.Add(brpanel);

                if (((i - 1) % rowElements) == 0) g.Children.Add(hpanel);
            }

        }


        private void Panel_MouseEnter(object sender, MouseEventArgs e)
        {
            throw new NotImplementedException();
        }
        
        /*
            UPLOAD AND DOWNLOAD OF FILES
        */

        private void sendFileToServer(string path)
        {
            NetworkHandler.getInstance().addFunction ( (Socket socket) => {
                socket.Send(BitConverter.GetBytes((int)messages.UPLOAD_FILE)); // UPLOAD FILE
                socket.Send(Encoding.UTF8.GetBytes(path));

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

                
                
                socket.Send(getSha1(path));
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
                    s.Send(BitConverter.GetBytes(ASCIIEncoding.UTF8.GetByteCount(path)));
                    
                    Debug.WriteLine("path length = " +s.Send(Encoding.UTF8.GetBytes(path)));
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
                s.Send(BitConverter.GetBytes(ASCIIEncoding.UTF8.GetByteCount(path)));
                s.Send(Encoding.UTF8.GetBytes(path));
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
                s.Send(BitConverter.GetBytes(System.Text.ASCIIEncoding.UTF8.GetByteCount(version)));
                s.Send(Encoding.UTF8.GetBytes(version));
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




                string[] path_arr = path.Split('\\');
                string[] fileext = path_arr[path_arr.Length - 1].Split('.');
                string tmp_path = "C:\\Temp\\"+ fileext[0] + ".tmp"; // what if that file is already there? it is deleted by file create
                
                

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
                if (!Directory.Exists("C:\\Temp"))
                    Directory.CreateDirectory("C:\\Temp");
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
                byte[] chash = getSha1(tmp_path);
                                
                buffer[ricevuti] = 0;
                string str_chash = BitConverter.ToString(chash).Replace("-", "");
                string str_shash = Encoding.ASCII.GetString(buffer, 0, 40).ToUpper();             

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
                if(File.Exists(path)) File.Delete(path);
                Debug.WriteLine("File.Move({0}, {1});", path+".tmp", path);
                File.Move(tmp_path, path);
                fs.EnableRaisingEvents = true;

                if(version == null)
                {
                    // here i need to refresh the interface!
                    if (currentDirectory.Equals("trash"))
                    {
                        List<JSONDeletedFile> items = getDeletedFiles(s);
                        Dispatcher.Invoke(() =>
                        {
                            insertFilesFromJSON(items, true);
                        });
                    }
                    else
                        Dispatcher.Invoke(updateFolders);

                }         
                
                return; 
            });
        }


        private void downloadFile(string path, string version,string filename)
        {
            NetworkHandler.getInstance().addFunction((Socket s) => {
                try {
                // selecting operation
                int ricevuti;
                byte[] buffer = new byte[1024];
                if (version == null)
                {
                    // downloading the last version
                    s.Send(BitConverter.GetBytes((int)messages.DOWNLOAD_LAST_VERSION));
                    s.Send(BitConverter.GetBytes(ASCIIEncoding.UTF8.GetByteCount(path)));

                    Debug.WriteLine("path length = " + s.Send(Encoding.UTF8.GetBytes(path)));
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
                    s.Send(BitConverter.GetBytes(ASCIIEncoding.UTF8.GetByteCount(path)));
                    s.Send(Encoding.UTF8.GetBytes(path));
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
                    s.Send(BitConverter.GetBytes(System.Text.ASCIIEncoding.UTF8.GetByteCount(version)));
                    s.Send(Encoding.UTF8.GetBytes(version));
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




                string[] path_arr = path.Split('\\');
                string[] fileext = path_arr[path_arr.Length - 1].Split('.');
                string tmp_path = "C:\\Temp\\" + fileext[0] + ".tmp"; // what if that file is already there? it is deleted by file create



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
                if (!Directory.Exists("C:\\Temp"))
                    Directory.CreateDirectory("C:\\Temp");
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
                byte[] chash = getSha1(tmp_path);

                buffer[ricevuti] = 0;
                string str_chash = BitConverter.ToString(chash).Replace("-", "");
                string str_shash = Encoding.ASCII.GetString(buffer, 0, 40).ToUpper();

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
                if (File.Exists(path)) File.Delete(path);
                Debug.WriteLine("File.Move({0}, {1});", path + ".tmp", path);
                File.Move(tmp_path, path);
                fs.EnableRaisingEvents = true;

                if (version == null)
                {
                    // here i need to refresh the interface!
                    if (currentDirectory.Equals("trash"))
                    {
                        List<JSONDeletedFile> items = getDeletedFiles(s);
                        Dispatcher.Invoke(() =>
                        {
                            insertFilesFromJSON(items, true);
                        });
                    }
                    else
                        Dispatcher.Invoke(updateFolders);

                }
                Dispatcher.Invoke(() =>
                {
                    ((StackPanel)this.FindName("panel_details")).Children.Clear();

                });
                    Monitor.Enter(this);
                    canMoveSidebar = true;
                    Monitor.Exit(this);
                    
                    drawVersionCalendar(filename);
                
             
                }
                finally
                {
                  
                }
            });
        }

        /*
            UTILITY FUNCTIONS, MOSTLY SETTERS
        */


        private List<JSONDeletedFile> getDeletedFiles(Socket s)
        {
            s.Send(BitConverter.GetBytes((int)messages.GET_DELETED_FILES));
            byte[] dim = new byte[4];
            int ricevuti = s.Receive(dim);
            int dimension = BitConverter.ToInt32(dim, 0);
            byte[] buffer = new byte[dimension];
            ricevuti = s.Receive(buffer);

            string delFiles = Encoding.UTF8.GetString(buffer);
            List<JSONDeletedFile> items = JsonConvert.DeserializeObject<List<JSONDeletedFile>>(delFiles);
            return items;
        }

        private byte[] getSha1(string file)
        {
            SHA1 shaProvider = SHA1.Create();
            FileStream hashStr = new FileStream(file, FileMode.Open);
            shaProvider.ComputeHash(hashStr);
            hashStr.Close();
            return shaProvider.Hash;
        }

        private void watchFolder()
        {
            fs = new FileSystemWatcher(currentDirectory);
            
            fs.Changed += new FileSystemEventHandler(OnChanged);
            fs.Created += new FileSystemEventHandler(OnChanged);
            fs.Deleted += new FileSystemEventHandler(OnChanged);
            fs.Renamed += new RenamedEventHandler((object source, RenamedEventArgs e) =>
            {
                fileDeleted(e.OldFullPath);
                sendFileToServer(e.FullPath);
                
            });
            fs.IncludeSubdirectories = true;
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

        /*
            This flag is used to syncronize the threads for some operations. for example i cannot enter in a folder if the right panel is open
        */
        private void setFlag(bool f)
        {

            Monitor.Enter(saveFlag);
            flag = f;
            Monitor.Exit(saveFlag);

        }

        private void removeFilePermanently(string filepath)
        {
            NetworkHandler.getInstance().addFunction((Socket s) =>
            {
                s.Send(BitConverter.GetBytes((int)messages.REMOVE_FILE));
                s.Send(Encoding.UTF8.GetBytes(filepath));
                byte[]buf = new byte[3];
                s.Receive(buf);
                if (Encoding.ASCII.GetString(buf).Contains("ERR"))
                    MessageBox.Show("Impossibile cancellare il file " + filepath, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    List<JSONDeletedFile> items = getDeletedFiles(s);
                    Dispatcher.Invoke(()=>
                    {
                        insertFilesFromJSON(items, true);
                    });
                }
                
            });
        }

        /*
            EVENT HANDLERS
        */

        // Specify what is done when a file is changed or created
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Debug.WriteLine("\n\nInto onchanged  for " + e.FullPath + "\n");
            if (!e.FullPath.Contains(".")) return; // if it is a folder i am not interested
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

        // Specify what is done when a file is deleted.
        public void fileDeleted(string path)
        {
            NetworkHandler.getInstance().addFunction((Socket socket) =>
            {
                byte[] buffer = new byte[5];
                socket.Send(BitConverter.GetBytes(4));
                socket.Send(Encoding.UTF8.GetBytes(path));
                socket.Receive(buffer);
                if (Encoding.ASCII.GetString(buffer).Contains("ERR"))
                    MessageBox.Show("Errore: errore nel comunicare al server che il file " + path + "e' stato cancellato", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }); 
        }

        // this allows the user to move the window using the mouse
        private void mouse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try {
                this.DragMove();
            }catch(InvalidOperationException)
            {
                // this means that someone has already cought the mousedown event. probably i did not want to move the window
            }
        }

        // behaviour of the top right button, the one with an X on it
        private void mouse_x_click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            noty.Visible = true;
        }

        // this function specifies what happens when a folder icon is clicked
        private void MouseFolderButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (((UIElement)this.FindName("details_container")).Visibility != Visibility.Collapsed) return;
            ((StackPanel)this.FindName("fs_grid")).Children.Clear(); // remove all childs
            Panel p = (Panel)sender;
            TextBlock lblDirectory = (TextBlock)p.Children[1];            
            string newDir = (string)(lblDirectory).Text;
            currentDirectory += ("\\" + newDir);            
            updateAddress();
            addCurrentFoderInfo(currentDirectory);
            
        }

        // returns the filename of the file where i clicked since the click is on the entire stackpanel
        private string getFileNamesender(StackPanel sender)
        {
            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Visible;
            // i start here a thread in order to download the versions of this file
            ((StackPanel)this.FindName("panel_details")).Children.Clear();
            string filename = (string)((TextBlock)sender.Children[1]).Text;
            this.selectedFile = currentDirectory + "\\" + filename;
            return filename;

        }

        // this function makes the right panel open and downloads the informations (asks the networkhandler to download) about the versions of the selected file
        private void drawVersionCalendar(string filename)
        {
            restoreHandlers.Clear();

            NetworkHandler.getInstance().addFunction((Socket socket) =>
            {

                Debug.WriteLine("Into downloader (versions) thread");
                socket.Send(BitConverter.GetBytes(5)); // GET FILE VERSIONS

                string pathToSend = currentDirectory + "\\" + filename;
                socket.Send(BitConverter.GetBytes(ASCIIEncoding.UTF8.GetByteCount(pathToSend)));
                socket.Send(Encoding.UTF8.GetBytes(pathToSend));
                Debug.WriteLine("sent " + pathToSend);


                byte[] dim = new byte[4]; // just the space for an int
                if (socket.Receive(dim) != 4)
                {
                    Debug.WriteLine("did not receive a valid number");
                    return;
                }
                if (BitConverter.ToInt32(dim, 0) <= 0)
                {
                    // an error server side has occurred!
                    Debug.WriteLine("dim of versions < 0");
                    return;
                }
                else
                    Debug.WriteLine("dim = " + BitConverter.ToInt32(dim, 0));


                byte[] buff = new byte[BitConverter.ToInt32(dim, 0) + 1];
                socket.Receive(buff); // receive json

                string versions = Encoding.UTF8.GetString(buff);

                Debug.WriteLine(versions);

                List<JSONVersion> items = JsonConvert.DeserializeObject<List<JSONVersion>>(versions);
                BrushConverter bc = new BrushConverter();
                images.Clear();
                calendars.Clear();
                foreach (JSONVersion v in items)
                {
                    Debug.WriteLine("v.date = " + v.date);
                   
                    Dispatcher.Invoke(() =>
                    {
                        ((Panel)FindName("panel_details")).Children.Add(getCalendar(v.date, bc, pathToSend,filename));
                        //Debug.WriteLine("inserted the new line -> " + line.Text);
                    });
                }
                return;
            }
          );

        }

        // behaviour when a file icon is clicked
        private void MouseFileButtonDownHandler(object sender, RoutedEventArgs e) {

            Monitor.Enter(this);
            if (!canMoveSidebar)
            {
                Monitor.Exit(this);
                return;
            }
            else Monitor.Exit(this);

            Monitor.Enter(saveFlag);
            if (flag == false)
            {
                Monitor.Exit(saveFlag);
                return;
            }
            Monitor.Exit(saveFlag);
            
                   

            Debug.WriteLine("MouseFileButtonDownHandler called");

            string filename= getFileNamesender(((StackPanel)sender));
            drawVersionCalendar(filename);
            Storyboard sb = (Storyboard)((Grid)this.FindName("fs_container")).FindResource("key_details_animation");
            //rowElements = 7;
            ((StackPanel)this.FindName("fs_grid")).Children.Clear();
            ((TextBlock)this.FindName("version_text")).Text = filename;
            addCurrentFoderInfo(currentDirectory);
           
            setFlag(false);
            sb.Completed += (object s, EventArgs ev) =>
            {
                setFlag(true);
            };
            sb.Begin();

            e.Handled = true;
        }

        // behaviour when the trash icon is clicked
        private void MouseTrashHandler(object sender, RoutedEventArgs e)
        {
            if (((UIElement)this.FindName("details_container")).Visibility != Visibility.Collapsed) return;
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
                socket.Send(BitConverter.GetBytes((int)messages.GET_DELETED_FILES));
                byte[] dim = new byte[4];
                int ricevuti = socket.Receive(dim);
                int dimension = BitConverter.ToInt32(dim, 0);
                byte[] buffer = new byte[dimension];
                ricevuti = socket.Receive(buffer);

                string s = Encoding.UTF8.GetString(buffer);
                List<JSONDeletedFile> items = JsonConvert.DeserializeObject<List<JSONDeletedFile>>(s);

                Dispatcher.Invoke(() => {
                    insertFilesFromJSON(items, true);
                });


            });
        }

        // behaviour when a file icon is clicked while we are in the trash
        private void MouseFileThrashHandler(object sender, RoutedEventArgs e)
        {
            string path = ((TextBlock)((Panel)sender).Children[2]).Text + "\\" + ((TextBlock)((Panel)sender).Children[1]).Text;
            string name = ((TextBlock)((Panel)sender).Children[1]).Text;
            //MessageBoxResult res =  MessageBox.Show("Vuoi davvero ripristinare il file " + path + "?", "Ripristino", MessageBoxButton.YesNo, MessageBoxImage.Question);
            var win2 = new Window2();
            var res = win2.Show_D(name);
            if (res == System.Windows.Forms.DialogResult.Cancel) return;
            if(res== System.Windows.Forms.DialogResult.Yes)downloadFile(path, null);
            if(res == System.Windows.Forms.DialogResult.Abort)removeFilePermanently(path);
        }

        // starts the animation which closes the right panel
        private void closeVersions(object sender, MouseButtonEventArgs e)
        {
            Monitor.Enter(this);
            if (!canMoveSidebar)
            {
                Monitor.Exit(this);
                return;
            }
            else Monitor.Exit(this);

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

        // makes the right panel invisible
        private void closeSidebar(object sender, EventArgs e)
        {
           

            ((UIElement)this.FindName("details_container")).Visibility = Visibility.Collapsed;
            //Grid.SetColumnSpan((UIElement)this.FindName("fs_grid"), 7);
            // rowElements = 10;
            
            if (!currentDirectory.Equals("trash"))
            {
                ((StackPanel)this.FindName("fs_grid")).Children.Clear();
                addCurrentFoderInfo(currentDirectory);
            }
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
