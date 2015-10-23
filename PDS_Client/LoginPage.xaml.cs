using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;


namespace PDS_Client
{
    /// <summary>
    /// Logica di interazione per Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {

        Socket s = null;

        public Window1()
        {
            InitializeComponent();
            byte[] chifer = File.ReadAllBytes("./polihub.settings");
            string plain = Encoding.ASCII.GetString(ProtectedData.Unprotect(chifer, null, DataProtectionScope.CurrentUser));
            Debug.WriteLine(plain);
            string[] credentials = plain.Split('\n');
            if(credentials.Length == 3)
            {
                try
                {
                    createSocket();
                    if (doLogin(credentials[0], credentials[1])) { 
                    s.Close();



                    MainWindow main = new MainWindow();
                    NetworkHandler.createInstance(credentials[0], credentials[1]);
                    main.setCurrentDirectory(credentials[2]);
                    main.updateFolders();
                    main.Show();
                    main.sync();
                    this.Close();
                }
                }
                catch (SocketException)
                {
                    s = null;
                }
            }
        }


        public void createSocket()
        {
            
            try
            {
                s = new Socket(SocketType.Stream, ProtocolType.Tcp);
                IPAddress sAddr = IPAddress.Parse("127.0.0.1");
                s.Connect(sAddr, 7000);
                if (!s.Connected) throw new SocketException();
            }
            catch(SocketException se)
            {
                MessageBox.Show(se.Message);
                throw;
            }
            
        }

        private void btn_login_Click(object sender, RoutedEventArgs e)
        {
            if (s == null)
            {
                try {
                    createSocket(); // the socket is already connected
                }
                catch (SocketException)
                {
                    s = null;
                    return; // i already printed the error for the user. now i just do nothing 
                }
            }
            
            string username = ((TextBox)this.FindName("text_user")).Text;
            string password = ((PasswordBox)this.FindName("text_pass")).Password;

            if (!doLogin(username, password))
                return;


            

            byte[] buffer = new byte[255];
            
            string path = null;
            s.Send(BitConverter.GetBytes((int)messages.GET_USER_PATH));
            int r = s.Receive(buffer);

            Debug.WriteLine("ricevuti: " + r);

            // if error -1 is returned
            if (r == 4)
            {
                if (BitConverter.ToInt32(buffer, 0) == -1)
                {
                    MessageBox.Show("Errore: non posso ottenere il path", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }
            }
            if(r <= 0)
            {
                MessageBox.Show("Errore: non posso ottenere il path", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            path = Encoding.ASCII.GetString(buffer);

            Debug.Print("before delete: " + path + "\n");


            path = path.Remove(r);
            
            Debug.Print("after delete: " + path);
            s.Close();



            MainWindow main = new MainWindow();
            NetworkHandler.createInstance(username, password);
            main.setCurrentDirectory(path);
            main.updateFolders();
            main.Show();
            main.sync();
            this.Close();

        }

        private void mouse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btn_x_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_register_Click(object sender, RoutedEventArgs e)
        {
            Registration reg = new Registration();
            reg.Show();
            this.Close();
        }

        private bool doLogin(string username, string password)
        {
            s.Send(BitConverter.GetBytes((int)messages.LOGIN), SocketFlags.None); // LOGIN

            s.Send(Encoding.ASCII.GetBytes(username));
            byte[] buffer = new byte[255];
            s.Receive(buffer);
            if (Encoding.ASCII.GetString(buffer).Contains("ERR"))
            {
                MessageBox.Show("Errore: username non ricevuto correttamente", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            s.Send(Encoding.ASCII.GetBytes(password));
            s.Receive(buffer);
            string message = Encoding.ASCII.GetString(buffer);
            if (message.Contains("OK"))
                return true;
            else
            {
                MessageBox.Show("Errore: credenziali errate", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

        }
    }
}
