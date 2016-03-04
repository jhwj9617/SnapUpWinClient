using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SnapUpWinClient
{
    [XmlRoot("BusDestinationList")]
    [XmlInclude(typeof(BusDestination))]
    public class BusDestinationList
    {
        [XmlArray("busDestinations"), XmlArrayItem(typeof(BusDestination), ElementName = "BusDestination")]
        public List<BusDestination> busDestinations = new List<BusDestination>();

        public BusDestinationList() { }

        public void AddBusDestination(BusDestination busDestination)
        {
            busDestinations.Add(busDestination);
        }
    }
}
