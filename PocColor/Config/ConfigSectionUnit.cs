using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocColor.Config
{
    public class ConfigSectionUnit : ConfigSectionCulture
    {
        public Map<String, ConfigSectionCulture> cultures { get; set; }
    }
}
