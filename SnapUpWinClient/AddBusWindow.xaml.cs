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
        private WebHelperFunctions WebHelper = new WebHelperFunctions();

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
            if (!WebHelper.CheckConnection())
            {
                MessageBox.Show("Using this function requires you to be connected to the internet. Please reconnect.", "Error");
                return;
            }
            this.IsEnabled = false;
            string statusCode = String.Empty;
            string statusMessage = String.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost/MVCWebApp/Buses/PairBus?id=" + Application.Current.Properties["PCId"] + "&code=" + textBoxCode.Text);
            using (WebResponse jsonResponse = request.GetResponse())
            {
                dynamic jsonData = WebHelper.JSONResponseToObject(jsonResponse);
                statusCode = jsonData.statusCode;
                statusMessage = jsonData.statusMessage;
            }
            this.IsEnabled = true;
            if (statusCode == "403" || statusCode == "404" || statusCode == "409")
            {
                Debug.Write(statusCode + ": " + statusMessage + ".\n");
                MessageBox.Show(statusCode + ": " + statusMessage + ".\n", "Error");
            }
            else if (statusCode == "200")
            {
                Debug.Write(statusCode + ": Success. Bus paired.\n");
                this.busDestination.code = textBoxCode.Text;
                this.busDestination.busName = "example";
                this.busDestination.openFolder = false;
                this.Close();
            }
        }
    }
}
