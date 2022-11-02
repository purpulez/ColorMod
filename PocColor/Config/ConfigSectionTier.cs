using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocColor.Config
{
    public class ConfigSectionTier : ConfigSection
    {
        public Map<string, ConfigSection> types { get; set; }
    }
}
