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
    /// Interaction logic for AddBusWindow.xaml
    /// </summary>
    public partial class AddBusWindow : Window
    {
        private BusDestination busDestination;

        public AddBusWindow(BusDestination busDestination)
        {
            this.busDestination = busDestination;
            InitializeComponent();

            textBoxCode.Focus();
        }

        private void AddBusWindowCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddBusWindowConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            string requestResponse = String.Empty;
            using (var webClient = new WebClient())
            {
                requestResponse = webClient.DownloadString("http://localhost/MVCWebApp/Buses/PairBus?id=1&code=" + textBoxCode.Text);
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
            else if (requestResponse == "409")
            {
                Debug.Write(requestResponse + ": Bus already paired.\n");
                MessageBox.Show(requestResponse + ": Bus already paired.", "Error");
            }
            else if (requestResponse == "200")
            {
                Debug.Write(requestResponse + ": Success. Bus paired.\n");
                this.busDestination.code = textBoxCode.Text;
                this.busDestination.busName = "example";
                this.busDestination.openFolder = false;
                this.Close();
            }
        }
    }
}
