using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGreenHouse.Models
{
    public class Sensors
    {
        public double Temperature { get; set; }
        public double AirHumidity { get; set; }
        public int SoilHumidity { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
