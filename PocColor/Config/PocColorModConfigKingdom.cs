using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PocColor.Config
{
    public class PocColorModConfigKingdom
    {
        public int? mode { get; set; }

        public string[] color { get; set; }

        public string[] color2 { get; set; }

        public string kingdomBanner { get; set; }

        public string primaryColor { get; set; }

        public string secondaryColor { get; set; }

        public string FollowKingdomColors { get; set; }

        public string banner { get; set; }

        public string[] shields { get; set; }

        public string[] banners { get; set; }

        public Map<String, PocColorModConfigUnit> units { get; set; }

        public Map<String, PocColorModConfigClan> clans { get; set; }
    }
}
