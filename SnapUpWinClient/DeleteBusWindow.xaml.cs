using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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

namespace SnapUpWinClient
{
    /// <summary>
    /// Interaction logic for DeleteBusWindow.xaml
    /// </summary>
    public partial class DeleteBusWindow : Window
    {
        private BusDestination busDestination;
        private bool messageBoxResult = false;

        public DeleteBusWindow(BusDestination busDestination)
        {
            this.busDestination = busDestination;
            InitializeComponent();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            string requestResponse = String.Empty;
            using (var webClient = new WebClient())
            {
                requestResponse = webClient.DownloadString("http://localhost/MVCWebApp/Buses/UnpairBus?id=1&code=" + busDestination.code);
                this.IsEnabled = true;
            }
            if (requestResponse == "403")
            {
                Debug.Write(requestResponse + ": User not found. Please reinstall SnapUpWinClient.\n");
                MessageBox.Show(requestResponse + ": User not found. Please reinstall SnapUpWinClient.", "Error");
            }
            else if (requestResponse == "404")
            {
                Debug.Write(requestResponse + ": Bus not found.\n");
                MessageBox.Show(requestResponse + ": Bus not found.", "Error");
            }
            else if (requestResponse == "200")
            {
                this.messageBoxResult = true;
                this.Close();
            }
        }

        public bool GetMessageBoxResult()
        {
            return this.messageBoxResult;
        }
    }
}
