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
                s = new Socket(SocketType.Dgram, ProtocolType.Tcp);
                IPAddress sAddr = new IPAddress(2130706433); //  127.0.0.1 --> 2130706433
                if (!s.Connected) throw new SocketException();
            }
            catch(SocketException se)
            {
                // todo: send a popup ( "impossibile connettersi al server " )
            }
            
        }

        private void btn_login_Click(object sender, RoutedEventArgs e)
        {

            if (s == null) createSocket(); // the socket is already connected
            string username = ((TextBox)this.FindName("text_user")).Text;
            string password = ((TextBox)this.FindName("text_pass")).Text; ;

            string message = "LOGIN " + username + " " + password;

            s.Send(System.Text.Encoding.UTF8.GetBytes(message));

            // todo: wait for answer. if ok proceed

            (new MainWindow()).Show();
            this.Close();
        }
    }
}
