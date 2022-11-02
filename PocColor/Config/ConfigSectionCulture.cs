using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocColor.Config
{
    public class ConfigSectionCulture : ConfigSectionTier
    {
        public Map<int, ConfigSectionTier> tiers { get; set; }
    }
}
