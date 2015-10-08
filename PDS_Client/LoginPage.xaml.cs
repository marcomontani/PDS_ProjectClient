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

using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Diagnostics;

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
            }
            
        }

        private void btn_login_Click(object sender, RoutedEventArgs e)
        {
            if (s == null) createSocket(); // the socket is already connected
            
            string username = ((TextBox)this.FindName("text_user")).Text;
            string password = ((PasswordBox)this.FindName("text_pass")).Password;

            s.Send(BitConverter.GetBytes(0), SocketFlags.None); // LOGIN
            
            s.Send(Encoding.ASCII.GetBytes(username));
            byte[] buffer = new byte[255];
            s.Receive(buffer);
            if (Encoding.ASCII.GetString(buffer).Contains("ERR"))
            {
                MessageBox.Show("Errore: username non ricevuto correttamente", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            s.Send(Encoding.ASCII.GetBytes(password));


            s.Receive(buffer);
            string message = Encoding.ASCII.GetString(buffer);
            if (message.Contains("OK")) { 
                MainWindow main = new MainWindow();
                main.setSocket(s);

                string path = null;
                s.Send(BitConverter.GetBytes(9));
                int r = s.Receive(buffer);
                if(r == 4) {
                    if(BitConverter.ToInt32(buffer, 0) == -1) {
                        MessageBox.Show("Errore: non posso ottenere il file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                path = Encoding.ASCII.GetString(buffer);

                Debug.Print("before delete: " + path + "\n");
                Debug.Print("ricevuti: " + r);

                path = path.Remove(r);

                Debug.Print("after delete: " + path);

                main.setCurrentDirectory(path);
                main.updateFolders();
                main.Show();
                //main.syncFolder();
                this.Close();
            }
            else MessageBox.Show("Errore: credenziali errate", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

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
    }
}
