using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocColor.Config
{
    public class ConfigSection
    {
        public int? mode { get; set; }

        public string[] color { get; set; }

        public string[] color2 { get; set; }

        public string banner { get; set; }

        public string[] shields { get; set; }

        public string[] banners { get; set; }

        public string BearRulerColors { get; set; }

        public string BearRulerBanner { get; set; }

        public string BearRulerShield { get; set; }

        public int? rulerMode { get; set; }

        public string[] rulerColor { get; set; }

        public string[] rulerColor2 { get; set; }

        public string rulerBanner { get; set; }

        public string rulerShield { get; set; }

        public string[] combatShields { get; set; }

        public string[] combatBanners { get; set; }

        public Map<String, string[]>[] vars { get; set; }
    }
}
