using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocColor.Config
{
    internal class ColorData
    {
        public int mode { get; set; }

        public uint? c1 { get; set; }

        public uint? c2 { get; set; }

        public string newbanner { get; set; }

        public string newshield { get; set; }

        public ColorData(int mode, uint? c1, uint? c2, string newbanner, string newshield)
        {
            this.mode = mode;
            this.c1 = c1;
            this.c2 = c2;
            this.newbanner = newbanner;
            this.newshield = newshield;
        }
            
    }
}
