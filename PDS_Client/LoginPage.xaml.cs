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
               
                IPAddress sAddr = new IPAddress(2130706433); //  127.0.0.1 --> 2130706433
                s.Connect(sAddr,7000);
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

            string message = "LOGIN " + username + " " + password; // todo: substitute LOGIN with the correct int
            s.Send(Encoding.ASCII.GetBytes(message));

            // todo: wait for answer. if ok proceed

            MainWindow main = new MainWindow();
            main.setSocket(s);
            main.Show();
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
    }
}
