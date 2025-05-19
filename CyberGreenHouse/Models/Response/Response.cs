using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGreenHouse.Models.Response
{
    public class Response
    {
        public Channel Channel { get; set; } = new Channel();
        public List<Feed> Feeds { get; set; } = new List<Feed>();

        public override string ToString() => Channel.Name;
    }
}
