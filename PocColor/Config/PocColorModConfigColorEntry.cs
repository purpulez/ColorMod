using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocColor.Config
{
    public class PocColorModConfigColorEntry
    {
        public int? mode { get; set; }

        public string[] color { get; set; }

        public string[] color2 { get; set; }

        public string FollowKingdomColors { get; set; }

        public string FollowKingdomBackgroundOnly { get; set; }

        public string BearRulerColors { get; set; }

        public string BearRulerBanner { get; set; }

        public string BearRulerShield { get; set; }

        public Map<String, string[]>[] vars { get; set; }


    }
}
