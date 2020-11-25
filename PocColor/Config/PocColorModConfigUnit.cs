using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PocColor.Config
{
    public class PocColorModConfigUnit
    {
        public int? mode { get; set; }

        public string[] color { get; set; }

        public string[] color2 { get; set; }

        public string banner { get; set; }

        public string[] shields { get; set; }

        public string[] banners { get; set; }

    }
}
