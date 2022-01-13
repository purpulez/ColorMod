using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Engine.Internal;

namespace PocColor.Config
{
    public class PocColorModGroupEntry : PocColorModConfigUnit
    {

        public string[] units { get; set; }


        public PocColorModConfigUnit toUnit() {
            
            PocColorModConfigUnit unit = new PocColorModConfigUnit();
            unit.mode = this.mode;
            unit.color = this.color;
            unit.color2 = this.color2;
            unit.banner = this.banner;
            unit.banners = this.banners;
            unit.shields = this.shields;
            unit.combatBanners = this.combatBanners;
            unit.combatShields = this.combatShields;
            unit.BearRulerColors = this.BearRulerColors;
            unit.BearRulerBanner = this.BearRulerBanner;
            unit.BearRulerShield = this.BearRulerShield;
            unit.rulerMode = this.rulerMode;
            unit.rulerColor = this.rulerColor;
            unit.rulerColor2 = this.rulerColor2;
            unit.rulerBanner = this.rulerBanner;
            unit.rulerShield = this.rulerShield;
            return unit;
        }

        public Map<String, PocColorModConfigUnit> toUnits()
        {
            Map<String, PocColorModConfigUnit> unitMap = new Map<String, PocColorModConfigUnit>();
            PocColorModConfigUnit unitConfig = this.toUnit();
            if (units is object)
            {
                foreach (string unit in units)
                {
                    unitMap[unit] = unitConfig;
                }
            }
            return unitMap;
        }


    }
}
