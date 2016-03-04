using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            this.busDestination.code = textBoxCode.Text;
            this.busDestination.busName = "example";
            this.busDestination.downloadLocation = null;
            this.Close();
        }
    }
}
