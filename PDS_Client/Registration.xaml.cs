
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using System.Net.Sockets;
using System.Net;
using System.Windows.Shapes;
using System.Windows.Media;

namespace PDS_Client
{
    /// <summary>
    /// Logica di interazione per Window1.xaml
    /// </summary>
    public partial class Registration : Window
    {

        Socket s = null;
        int status;
        string username, password,path;

        public Registration()
        {
            InitializeComponent();
            status = 0;
            username = null;
            password = null;
            path = null;

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
                // todo: send a popup ( "impossibile connettersi al server " )
            }
            
        }

     

        private void mouse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btn_x_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void txt_wizard_GotFocus(object sender, RoutedEventArgs e)
        {
           // ((TextBox)sender).Text = "";

        }

        private void elps_wizards_MouseOver(object sender, RoutedEventArgs e)
        {
            ((Ellipse)sender).Fill = new SolidColorBrush(Colors.CadetBlue);
        }
        private void elps_wizards_MouseLeave(object sender, RoutedEventArgs e)
        {
            int n = ((Ellipse)sender).Name.ToCharArray()[((Ellipse)sender).Name.Length - 1] - '0';
            if(n-1 <= status)
                ((Ellipse)sender).Fill = new SolidColorBrush(Colors.WhiteSmoke);
            else
                ((Ellipse)sender).Fill = ((Label)this.FindName("lbl_wizard_footer")).Background;
        }
        private void btn_next_Click(object sender, RoutedEventArgs e)
        {
            handleNewStatus();
        }


        private void elps_mouse_MouseDown(object sender, RoutedEventArgs e)
        {
            int n = ((Ellipse)sender).Name.ToCharArray()[((Ellipse)sender).Name.Length - 1] - '0';
            if (n - 1 <= status)
            {
                status = n - 2;
                handleNewStatus();
            }
        }
        


        private void handleNewStatus()
        {
            switch (status)
            {
                case -1:
                    username = null;
                    password = null;
                    path = null;
                    ((Label)this.FindName("lbl_wizard")).Content = "Inserisci il tuo username";
                    ((TextBox)this.FindName("txt_wizard")).Text = "Username";
                    ((Ellipse)this.FindName("circle1")).Fill = new SolidColorBrush(Colors.WhiteSmoke);
                    ((Ellipse)this.FindName("circle2")).Fill = ((Label)this.FindName("lbl_wizard_footer")).Background;
                    ((Ellipse)this.FindName("circle3")).Fill = ((Label)this.FindName("lbl_wizard_footer")).Background;
                    ((Ellipse)this.FindName("circle4")).Fill = ((Label)this.FindName("lbl_wizard_footer")).Background;
                    status = 0;
                    break;
                case 0:
                    username = ((TextBox)this.FindName("txt_wizard")).Text;
                    password = null;
                    path = null;
                    ((Label)this.FindName("lbl_wizard")).Content = "Inserisci la password";
                    ((TextBox)this.FindName("txt_wizard")).Text = "Password";
                    ((Ellipse)this.FindName("circle1")).Fill = new SolidColorBrush(Colors.WhiteSmoke);
                    ((Ellipse)this.FindName("circle2")).Fill = new SolidColorBrush(Colors.WhiteSmoke);
                    ((Ellipse)this.FindName("circle3")).Fill = ((Label)this.FindName("lbl_wizard_footer")).Background;
                    ((Ellipse)this.FindName("circle4")).Fill = ((Label)this.FindName("lbl_wizard_footer")).Background;
                    status = 1;
                    break;
                case 1:
                    username = ((TextBox)this.FindName("txt_wizard")).Text;
                    password = null;
                    path = null;
                    ((Label)this.FindName("lbl_wizard")).Content = "Reinserisci la password";
                    ((TextBox)this.FindName("txt_wizard")).Text = "Password";
                    ((Ellipse)this.FindName("circle1")).Fill = new SolidColorBrush(Colors.WhiteSmoke);
                    ((Ellipse)this.FindName("circle2")).Fill = new SolidColorBrush(Colors.WhiteSmoke);
                    ((Ellipse)this.FindName("circle3")).Fill = new SolidColorBrush(Colors.WhiteSmoke);
                    ((Ellipse)this.FindName("circle4")).Fill = ((Label)this.FindName("lbl_wizard_footer")).Background;
                    status = 2;
                    break;


            }
        }
    }
}
