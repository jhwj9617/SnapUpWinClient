using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SnapUpWinClient
{
    public class BusDestination
    {
        public String code;
        public String busName;
        public String downloadLocation;
        public bool openFolder;

        public BusDestination() { }

        public BusDestination(String code, String busName, String downloadLocation)
        {
            this.code = code;
            this.busName = busName;
            this.downloadLocation = downloadLocation;
        }
    }
}
