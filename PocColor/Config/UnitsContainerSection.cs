using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocColor.Config
{
    internal interface UnitsContainerSection
    {
        Map<String, PocColorModConfigUnit> units { get; set; }
    }
}
