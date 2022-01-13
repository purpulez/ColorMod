using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using TaleWorlds.CampaignSystem.ViewModelCollection.Craft;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using NUnit.Framework;

namespace PocColor.Config
{
    public class PocColorModConfig
    {

        public bool log { get; set; } = false;

        public bool generateTemplate { get; set; } = false;

        public PocColorModConfigColorEntry defaultConfig { get; set; }

        public Map<String, PocColorModConfigKingdom> kingdoms { get; set; }

        public Map<String, PocColorModConfigClan> clans { get; set; }

        public Map<String, PocColorModConfigUnit> units { get; set; }

        public PocColorModGroupEntry[] groups { get; set; }


        public static Random rnd = new Random();

        public (bool, bool, string, string, string, string) GetClanConfig(string kingdom, string clan, bool isPlayerKingdom, bool isPlayerClan)
        {

            PocColorModConfigKingdom kConfig = null;
            PocColorModConfigClan kcConfig = null;
            PocColorModConfigClan cConfig = null;

            string follow = null;
            string followBG = "false";

            string banner = null, shield = null, primaryColor = null, secondaryColor = null;
            bool success = false;
            bool successBG = false;

            try
            {


                //Log.write("-------------------- get clan config START --------------------------"  );
                //Log.write("");
                //Log.write("==> initialized variables:");
                //Log.write("follow: [" + follow + "]");
                //Log.write("banner: [" + banner + "]");
                //Log.write("primaryColor: [" + primaryColor + "]");
                //Log.write("secondaryColor: [" + secondaryColor + "]");
                //Log.write("");
                //Log.write("");

                kConfig = this.kingdoms?[kingdom];

                //if (kConfig is object) {
                //Log.write("- kconfig is defined");
                //}
                if (kConfig == null && isPlayerKingdom)
                {
                    //Log.write("- kconfig is undefined");
                    kConfig = this.kingdoms?["PlayerKingdom"];
                    //if (kConfig is object)
                    //{
                    //    Log.write("- PlayerKingdom config is defined");
                    //}
                }
                cConfig = this.clans?[clan];
                //if (cConfig is object)
                //{
                //    Log.write("- cConfig is defined");
                //}

                if (cConfig == null && isPlayerClan)
                {

                    cConfig = this.clans?["PlayerClan"];
                    //if (cConfig is object)
                    //{
                    //    Log.write("- PlayerClan config is defined within clans");
                    //}
                }
                //Log.write("");
                kcConfig = kConfig?.clans?[clan];
                //if (kcConfig is object)
                //{
                //Log.write("- kcConfig is defined");
                //}

                if (kcConfig == null && isPlayerClan)
                {
                    kcConfig = kConfig?.clans?["PlayerClan"];
                    //if (kcConfig is object)
                    //{
                    //    Log.write("- PlayerClan config is defined within kingdoms.clans");
                    //}
                }
                banner = kConfig?.clanBanner ?? banner;
                //Log.write("=> clan banner: [" + banner + "]");
                banner = cConfig?.clanBanner ?? banner;
                //Log.write("=> clan banner: [" + banner + "]");
                banner = kcConfig?.clanBanner ?? banner;
                //Log.write("=> kingdom.clan banner: [" + banner + "]");

                shield = kConfig?.clanShield ?? shield;
                //Log.write("=> clan banner: [" + banner + "]");
                shield = cConfig?.clanShield ?? shield;
                //Log.write("=> clan banner: [" + banner + "]");
                shield = kcConfig?.clanShield ?? shield;
                //Log.write("=> kingdom.clan banner: [" + banner + "]");


                follow = this.defaultConfig?.FollowKingdomColors ?? follow;
                //Log.write("=> defaultConfig follow: [" + follow + "]");

                follow = kConfig?.FollowKingdomColors ?? follow;
                //Log.write("=> kingdom follow: [" + follow + "]");
                follow = cConfig?.FollowKingdomColors ?? follow;
                //Log.write("=> clan follow: [" + follow + "]");
                follow = kcConfig?.FollowKingdomColors ?? follow;
                //Log.write("=> kingdom.clan follow: [" + follow + "]");

                if (follow is null)
                {
                    //Log.write("- follow is undefined");
                    if (banner is object)
                    {
                        //Log.write("- banner is defined => follow should be false");
                        //if banner is set and , by default follow will be false
                        follow = "false";
                    }
                    else
                    {
                        //Log.write("- banner is undefined => follow should be true");
                        follow = "true";
                    }
                }
                bool couldParse = Boolean.TryParse(follow.ToLower(), out success);


                followBG = this.defaultConfig?.FollowKingdomBackgroundOnly ?? followBG;
                //Log.write("=> defaultConfig follow: [" + follow + "]");
                followBG = kConfig?.FollowKingdomBackgroundOnly ?? followBG;
                //Log.write("=> kingdom follow: [" + follow + "]");
                followBG = cConfig?.FollowKingdomBackgroundOnly ?? followBG;
                //Log.write("=> clan follow: [" + follow + "]");
                followBG = kcConfig?.FollowKingdomBackgroundOnly ?? followBG;

                couldParse = Boolean.TryParse(followBG.ToLower(), out successBG);

                //Log.write("- could parse follow value successfully: " + couldParse);
                //Log.write("=> Parsed follow value: [" + success + "]");

                primaryColor = cConfig?.primaryColor ?? primaryColor;
                //Log.write("=> clan primaryColor: [" + primaryColor + "]");

                primaryColor = kcConfig?.primaryColor ?? primaryColor;
                //Log.write("=> kingdom.clan primaryColor: [" + primaryColor + "]");

                secondaryColor = cConfig?.secondaryColor ?? secondaryColor;
                //Log.write("=> clan secondaryColor: [" + secondaryColor + "]");

                secondaryColor = kcConfig?.secondaryColor ?? secondaryColor;
                //Log.write("=> kingdom.clan secondaryColor: [" + secondaryColor + "]");
                //Log.write("");

                //Log.write("==> returned variables:");

                //Log.write("follow: [" + success + "]");
                //Log.write("banner: [" + banner + "]");
                //Log.write("primaryColor: [" + primaryColor + "]");
                //Log.write("secondaryColor: [" + secondaryColor + "]");

                //Log.write("-------------------- get clan config END ---------------------------");

                Map<String, string[]>[] vars = this.defaultConfig?.vars ?? null;
                vars = kConfig?.vars ?? vars;
                vars = cConfig?.vars ?? vars;
                vars = kcConfig?.vars ?? vars;
                
                Map<string, string> values = null;
                if (vars is object)
                {
                    values = GetVarsValues(vars);
                }

                if (values is object)
                {
                    banner = resolveBanner(banner, values);
                    shield = resolveBanner(shield, values);
                }

            }
            catch (Exception e)
            {
                Log.write(e.Message);
            }

            return (success, successBG, banner, shield, primaryColor, secondaryColor);
        }

        public (string, string, string, string) GetKingdomConfig(string kingdom, bool isPlayerKingdom)
        {
            PocColorModConfigKingdom kConfig = null;
            string banner = null, shield = null;
            string primaryColor = null;
            string secondaryColor = null;

            try
            {

                kConfig = this.kingdoms?[kingdom];
                if (kConfig == null && isPlayerKingdom)
                {
                    kConfig = this.kingdoms?["PlayerKingdom"];
                }

                banner = kConfig?.kingdomBanner ?? banner;
                shield = kConfig?.kingdomShield ?? shield;

                primaryColor = kConfig?.primaryColor ?? primaryColor;
                secondaryColor = kConfig?.secondaryColor ?? secondaryColor;

                Map<String, string[]>[] vars = this.defaultConfig?.vars ?? null;
                vars = kConfig?.vars ?? vars;
                
                Map<string, string> values = null;
                if (vars is object)
                {
                    values = GetVarsValues(vars);
                }

                if (values is object)
                {
                    banner = resolveBanner(banner, values);
                    shield = resolveBanner(shield, values);
                }
            }
            catch (Exception e)
            {
                Log.write(e.Message);
            }
            return (banner, shield, primaryColor, secondaryColor);
        }

        public (bool, int, string[], string[], string[], string[]) GetRulerConfig(string clan, int modeOrig, string[] colorOrig, string[] colorOrig2, string[] bannerOrig, string[] shieldOrig, PocColorModConfigKingdom kConfig, PocColorModConfigClan kcConfig, PocColorModConfigUnit kuConfig, PocColorModConfigUnit kcuConfig, PocColorModConfigClan cConfig, PocColorModConfigUnit cuConfig, PocColorModConfigUnit uConfig)
        {

            int mode = modeOrig;
            string[] color = colorOrig;
            string[] color2 = colorOrig2;
            string[] banners = bannerOrig;
            string[] shields = shieldOrig;
            string banner = null;
            string shield = null;

            string bearRulerColorsStr = this.defaultConfig?.BearRulerColors ?? "true";
            string bearRulerBannerStr = this.defaultConfig?.BearRulerBanner ?? "true";
            string bearRulerShieldStr = this.defaultConfig?.BearRulerShield ?? "true";

            bool bearRulerColors;
            bool bearRulerBanner;
            bool bearRulerShield;

            bool success = false;

            bool isRuler = false;

            string kingdomBanner = null;

            try
            {

                //CHECK IF UNIT IS RULER
                if (clan is object && !clan.Equals(""))
                {
                    foreach (Clan clanObj in Campaign.Current.Clans)
                    {
                        if (clanObj.Name.ToString().Equals(clan))
                        {
                            if (clanObj.Kingdom?.RulingClan == clanObj)
                            {
                                isRuler = true;
                                kingdomBanner = PocColorMod.SerializeBanner(clanObj.Kingdom.Banner);
                                if (PocColorMod.doLog) Log.write("This unit is ruler!!");
                            }
                        }
                    }
                }
                if (!isRuler) return (false, mode, color, color2, banners, shields);

                //Check if this config entry should bear different color
                bearRulerColorsStr = kConfig?.BearRulerColors ?? bearRulerColorsStr;
                bearRulerColorsStr = cConfig?.BearRulerColors ?? bearRulerColorsStr;
                bearRulerColorsStr = kcConfig?.BearRulerColors ?? bearRulerColorsStr;
                bearRulerColorsStr = uConfig?.BearRulerColors ?? bearRulerColorsStr;
                bearRulerColorsStr = kuConfig?.BearRulerColors ?? bearRulerColorsStr;
                bearRulerColorsStr = cuConfig?.BearRulerColors ?? bearRulerColorsStr;
                bearRulerColorsStr = kcuConfig?.BearRulerColors ?? bearRulerColorsStr;

                Boolean.TryParse(bearRulerColorsStr.ToLower(), out bearRulerColors);

                //Check if this config entry should bear different banner
                bearRulerBannerStr = kConfig?.BearRulerBanner ?? bearRulerBannerStr;
                bearRulerBannerStr = cConfig?.BearRulerBanner ?? bearRulerBannerStr;
                bearRulerBannerStr = kcConfig?.BearRulerBanner ?? bearRulerBannerStr;
                bearRulerBannerStr = uConfig?.BearRulerBanner ?? bearRulerBannerStr;
                bearRulerBannerStr = kuConfig?.BearRulerBanner ?? bearRulerBannerStr;
                bearRulerBannerStr = cuConfig?.BearRulerBanner ?? bearRulerBannerStr;
                bearRulerBannerStr = kcuConfig?.BearRulerBanner ?? bearRulerBannerStr;

                Boolean.TryParse(bearRulerBannerStr.ToLower(), out bearRulerBanner);

                //Check if this config entry should bear different shield
                bearRulerShieldStr = kConfig?.BearRulerShield ?? bearRulerShieldStr;
                bearRulerShieldStr = cConfig?.BearRulerShield ?? bearRulerShieldStr;
                bearRulerShieldStr = kcConfig?.BearRulerShield ?? bearRulerShieldStr;
                bearRulerShieldStr = uConfig?.BearRulerShield ?? bearRulerShieldStr;
                bearRulerShieldStr = kuConfig?.BearRulerShield ?? bearRulerShieldStr;
                bearRulerShieldStr = cuConfig?.BearRulerShield ?? bearRulerShieldStr;
                bearRulerShieldStr = kcuConfig?.BearRulerShield ?? bearRulerShieldStr;

                Boolean.TryParse(bearRulerShieldStr.ToLower(), out bearRulerShield);

                //Get ruling colors
                if (bearRulerColors)
                {
                    mode = kConfig?.rulerMode ?? 2;
                    mode = cConfig?.rulerMode ?? mode;
                    mode = kcConfig?.rulerMode ?? mode;
                    mode = uConfig?.rulerMode ?? mode;
                    mode = kuConfig?.rulerMode ?? mode;
                    mode = cuConfig?.rulerMode ?? mode;
                    mode = kcuConfig?.rulerMode ?? mode;

                    color = kConfig?.rulerColor;
                    color = cConfig?.rulerColor ?? color;
                    color = kcConfig?.rulerColor ?? color;
                    color = uConfig?.rulerColor ?? color;
                    color = kuConfig?.rulerColor ?? color;
                    color = cuConfig?.rulerColor ?? color;
                    color = kcuConfig?.rulerColor ?? color;

                    color2 = kConfig?.rulerColor2;
                    color2 = cConfig?.rulerColor2 ?? color2;
                    color2 = kcConfig?.rulerColor2 ?? color2;
                    color2 = uConfig?.rulerColor2 ?? color2;
                    color2 = kuConfig?.rulerColor2 ?? color2;
                    color2 = cuConfig?.rulerColor2 ?? color2;
                    color2 = kcuConfig?.rulerColor2 ?? color2;
                }

                //Get ruling banner
                if (bearRulerBanner)
                {
                    //Set kingdom banner by default
                    banner = kingdomBanner;

                    banner = kConfig?.rulerBanner ?? banner;
                    banner = cConfig?.rulerBanner ?? banner;
                    banner = kcConfig?.rulerBanner ?? banner;
                    banner = uConfig?.rulerBanner ?? banner;
                    banner = kuConfig?.rulerBanner ?? banner;
                    banner = cuConfig?.rulerBanner ?? banner;
                    banner = kcuConfig?.rulerBanner ?? banner;

                    banners = new string[] { banner };
                }

                //Get ruling shield
                if (bearRulerShield)
                {
                    //Set kingdom banner by default
                    shield = kingdomBanner;

                    shield = kConfig?.rulerShield ?? shield;
                    shield = cConfig?.rulerShield ?? shield;
                    shield = kcConfig?.rulerShield ?? shield;
                    shield = uConfig?.rulerShield ?? shield;
                    shield = kuConfig?.rulerShield ?? shield;
                    shield = cuConfig?.rulerShield ?? shield;
                    shield = kcuConfig?.rulerShield ?? shield;

                    shields = new string[] { shield };
                }

            }
            catch (Exception e)
            {
                Log.write(e.Message);
            }
            return (true, mode, color, color2, banners, shields);
        }


        public (int, string[], string[], string[], string[]) GetConfig(string kingdom, string clan, string unit, bool isPlayerKingdom, bool isPlayerClan, bool isPlayer, bool isKing, bool isLeader, bool isHero)
        {
            int mode = 0;
            string[] color = null;
            string[] color2 = null;
            string banner = null;
            string[] shields = null;
            string[] banners = null;
            string[] bannersFull = null;

            PocColorModConfigKingdom kConfig = null;
            PocColorModConfigClan kcConfig = null;
            PocColorModConfigUnit kuConfig = null;
            PocColorModConfigUnit kcuConfig = null;
            PocColorModConfigClan cConfig = null;
            PocColorModConfigUnit cuConfig = null;
            PocColorModConfigUnit uConfig = null;

            try
            {

                //search config from lowest to highest priority
                //Kingdom
                //Clan
                //KingdomClan
                //Unit
                //KingdomUnit
                //ClanUnit
                //KingdomClanUnit
                
                (kConfig, kcConfig, kuConfig, kcuConfig, cConfig, cuConfig, uConfig) = getConfigScopes(kConfig, kcConfig, kuConfig, kcuConfig, cConfig, cuConfig, uConfig, kingdom, clan, unit, isPlayerKingdom, isPlayerClan, isPlayer, isKing, isLeader, isHero);

                Map<String, string[]>[] vars = this.defaultConfig?.vars ?? null;
                vars = kConfig?.vars ?? vars;
                vars = cConfig?.vars ?? vars;
                vars = kcConfig?.vars ?? vars;
                vars = uConfig?.vars ?? vars;
                vars = kuConfig?.vars ?? vars;
                vars = cuConfig?.vars ?? vars;
                vars = kcuConfig?.vars ?? vars;

                Map<string, string> values = null;
                if (vars is object)
                {
                    values = GetVarsValues(vars);
                }

                mode = this.defaultConfig?.mode ?? 2;

                mode = kConfig?.mode ?? mode;
                mode = cConfig?.mode ?? mode;
                mode = kcConfig?.mode ?? mode;
                mode = uConfig?.mode ?? mode;
                mode = kuConfig?.mode ?? mode;
                mode = cuConfig?.mode ?? mode;
                mode = kcuConfig?.mode ?? mode;

                color = this.defaultConfig?.color;
                color = kConfig?.color ?? color;
                color = cConfig?.color ?? color;
                color = kcConfig?.color ?? color;
                color = uConfig?.color ?? color;
                color = kuConfig?.color ?? color;
                color = cuConfig?.color ?? color;
                color = kcuConfig?.color ?? color;

                color2 = this.defaultConfig?.color2;
                color2 = kConfig?.color2 ?? color2;
                color2 = cConfig?.color2 ?? color2;
                color2 = kcConfig?.color2 ?? color2;
                color2 = uConfig?.color2 ?? color2;
                color2 = kuConfig?.color2 ?? color2;
                color2 = cuConfig?.color2 ?? color2;
                color2 = kcuConfig?.color2 ?? color2;

                banner = kConfig?.banner;
                banner = cConfig?.banner ?? banner;
                banner = kcConfig?.banner ?? banner;
                banner = uConfig?.banner ?? banner;
                banner = kuConfig?.banner ?? banner;
                banner = cuConfig?.banner ?? banner;
                banner = kcuConfig?.banner ?? banner;

                shields = kConfig?.shields;
                shields = cConfig?.shields ?? shields;
                shields = kcConfig?.shields ?? shields;
                shields = uConfig?.shields ?? shields;
                shields = kuConfig?.shields ?? shields;
                shields = cuConfig?.shields ?? shields;
                shields = kcuConfig?.shields ?? shields;

                banners = kConfig?.banners;
                banners = cConfig?.banners ?? banners;
                banners = kcConfig?.banners ?? banners;
                banners = uConfig?.banners ?? banners;
                banners = kuConfig?.banners ?? banners;
                banners = cuConfig?.banners ?? banners;
                banners = kcuConfig?.banners ?? banners;

                if (!String.IsNullOrEmpty(banner) && (banners != null) && banners.Length > 0)
                {
                    bannersFull = new string[banners.Length + 1];
                    banners.CopyTo(bannersFull, 0);
                    bannersFull[banners.Length] = banner;
                }
                else
                {
                    if (banners != null && banners.Length > 0)
                    {
                        bannersFull = banners;
                    }
                    else if (!String.IsNullOrEmpty(banner))
                    {
                        bannersFull = new string[1];
                        bannersFull[0] = banner;
                    }
                }

                //Apply ruling config
                bool isRuler = false;
                (isRuler, mode, color, color2, bannersFull, shields) = GetRulerConfig(clan, mode, color, color2, bannersFull, shields, kConfig, kcConfig, kuConfig, kcuConfig, cConfig, cuConfig, uConfig);

                //RESOLVE SHIELDS AND BANNERS
                //RESOLVE BANNERS AND SHIELDS
                if (values is object)
                {
                    bannersFull = Array.ConvertAll(bannersFull, x => resolveBanner(x, values));
                    shields = Array.ConvertAll(shields, x => resolveBanner(x, values));
                }

            }
            catch (Exception e)
            {
                Log.write("Error while retrieving config: "+ e.Message);
            }
            return (mode, color, color2, bannersFull, shields);
        }

        public (int, string[], string[], string[], string[]) GetBattleConfig(string kingdom, string clan, string unit, bool isPlayerKingdom, bool isPlayerClan, bool isPlayer, bool isKing, bool isLeader, bool isHero)
        {
            int mode = 0;
            string[] color = null;
            string[] color2 = null;
            string[] shields = null;
            string[] banners = null;

            string[] combatShields = null;
            string[] combatBanners = null;

            PocColorModConfigKingdom kConfig = null;
            PocColorModConfigClan kcConfig = null;
            PocColorModConfigUnit kuConfig = null;
            PocColorModConfigUnit kcuConfig = null;
            PocColorModConfigClan cConfig = null;
            PocColorModConfigUnit cuConfig = null;
            PocColorModConfigUnit uConfig = null;

            (kConfig, kcConfig, kuConfig, kcuConfig, cConfig, cuConfig, uConfig) = getConfigScopes(kConfig, kcConfig, kuConfig, kcuConfig, cConfig, cuConfig, uConfig, kingdom, clan, unit, isPlayerKingdom, isPlayerClan, isPlayer, isKing, isLeader, isHero);

            (mode, color, color2, banners, shields) = GetConfig(kingdom, clan, unit, isPlayerKingdom, isPlayerClan, isPlayer, isKing, isLeader, isHero);

            Map<String, string[]>[] vars = this.defaultConfig?.vars ?? null;
            vars = kConfig?.vars ?? vars;
            vars = cConfig?.vars ?? vars;
            vars = kcConfig?.vars ?? vars;
            vars = uConfig?.vars ?? vars;
            vars = kuConfig?.vars ?? vars;
            vars = cuConfig?.vars ?? vars;
            vars = kcuConfig?.vars ?? vars;

            Map<string, string> values = null;
            if (vars is object)
            {
                values = GetVarsValues(vars);
            }

            combatShields = kConfig?.combatShields;
            combatShields = cConfig?.combatShields ?? combatShields;
            combatShields = kcConfig?.combatShields ?? combatShields;
            combatShields = uConfig?.combatShields ?? combatShields;
            combatShields = kuConfig?.combatShields ?? combatShields;
            combatShields = cuConfig?.combatShields ?? combatShields;
            combatShields = kcuConfig?.combatShields ?? combatShields;

            if (combatShields is object && combatShields.Length > 0)
            {
                shields = combatShields;
            }

            combatBanners = kConfig?.combatBanners ?? combatBanners;
            combatBanners = cConfig?.combatBanners ?? combatBanners;
            combatBanners = kcConfig?.combatBanners ?? combatBanners;
            combatBanners = uConfig?.combatBanners ?? combatBanners;
            combatBanners = kuConfig?.combatBanners ?? combatBanners;
            combatBanners = cuConfig?.combatBanners ?? combatBanners;
            combatBanners = kcuConfig?.combatBanners ?? combatBanners;

            if (combatBanners is object && combatBanners.Length > 0)
            {
                banners = combatBanners;
            }

            //RESOLVE BANNERS AND SHIELDS
            if (values is object)
            {
                banners = Array.ConvertAll(banners, x => resolveBanner(x, values));
                shields = Array.ConvertAll(shields, x => resolveBanner(x, values));
            }

            return (mode, color, color2, banners, shields);
        }


        public Map<string, string[]>[] GetVars(string kingdom, string clan, string unit, bool isPlayerKingdom, bool isPlayerClan, bool isPlayer, bool isKing, bool isLeader, bool isHero)
        {

            PocColorModConfigKingdom kConfig = null;
            PocColorModConfigClan kcConfig = null;
            PocColorModConfigUnit kuConfig = null;
            PocColorModConfigUnit kcuConfig = null;
            PocColorModConfigClan cConfig = null;
            PocColorModConfigUnit cuConfig = null;
            PocColorModConfigUnit uConfig = null;

            (kConfig, kcConfig, kuConfig, kcuConfig, cConfig, cuConfig, uConfig) = getConfigScopes(kConfig, kcConfig, kuConfig, kcuConfig, cConfig, cuConfig, uConfig, kingdom, clan, unit, isPlayerKingdom, isPlayerClan, isPlayer, isKing, isLeader, isHero);

            Map<string, string[]>[] vars = this.defaultConfig?.vars ?? null;
            vars = kConfig?.vars ?? vars;
            vars = cConfig?.vars ?? vars;
            vars = kcConfig?.vars ?? vars;
            vars = uConfig?.vars ?? vars;
            vars = kuConfig?.vars ?? vars;
            vars = cuConfig?.vars ?? vars;
            vars = kcuConfig?.vars ?? vars;

            return vars;
        }

        public string[] GetVarList(Map<string, string[]>[] vars)
        {
            HashSet<string> varList = new HashSet<string>();
            foreach (Map<String, string[]> group in vars)
            {
                varList.UnionWith(group.Keys);
            }

            String[] stringArray = new String[varList.Count];
            varList.CopyTo(stringArray);

            return stringArray;
        }


        public Map<string, string> GetVarsValues(Map<string, string[]>[] vars)
        {
            //Loop through the groups of variables to assign a value to them
            Map<string, string> values = new Map<string, string>();
            foreach (Map<string, string[]> group in vars)
            {
                int min = 100000;
                foreach (string key in group.Keys) {
                    string[] vals = group[key];
                    min = Math.Min(min, vals.Length);
                }
                //Generate an index for that group
                int ind = rnd.Next(0, min);
                //Fetch the value for each var
                foreach (string key in group.Keys)
                {
                    string[] vals = group[key];
                    values[key] = vals[ind];
                }
            }
            return values;
        }

        public string resolveBanner(string bannerText, Map<string, string> values)
        {
            string banner = bannerText;
            foreach (string key in values.Keys)
            {
                banner = banner.Replace(key, values[key]);
            }
            return banner;
        }


        public (PocColorModConfigKingdom, PocColorModConfigClan, PocColorModConfigUnit, PocColorModConfigUnit, PocColorModConfigClan, PocColorModConfigUnit, PocColorModConfigUnit ) getConfigScopes(PocColorModConfigKingdom kConfig, PocColorModConfigClan kcConfig, PocColorModConfigUnit kuConfig, PocColorModConfigUnit kcuConfig, PocColorModConfigClan cConfig, PocColorModConfigUnit cuConfig, PocColorModConfigUnit uConfig, string kingdom, string clan, string unit, bool isPlayerKingdom, bool isPlayerClan, bool isPlayer, bool isKing, bool isLeader, bool isHero)
        {
            //KINGOM
            kConfig = this.kingdoms?[kingdom];
            if (kConfig == null && isPlayerKingdom)
            {
                kConfig = this.kingdoms?["PlayerKingdom"];
            }
            //CLAN
            cConfig = this.clans?[clan];
            if (cConfig == null && isPlayerClan)
            {
                cConfig = this.clans?["PlayerClan"];
            }
            //KINGDOMCLAN
            kcConfig = kConfig?.clans?[clan];
            if (kcConfig == null && isPlayerClan)
            {
                kcConfig = kConfig?.clans?["PlayerClan"];
            }

            //UNIT
            uConfig = this.units?[unit];
            if (uConfig == null && isPlayer)
            {
                uConfig = this.units?["Player"];
            }
            if (uConfig == null && isKing)
            {
                uConfig = this.units?["King"];
            }
            if (uConfig == null && isLeader)
            {
                uConfig = this.units?["Leader"];
            }
            if (uConfig == null && isHero)
            {
                uConfig = this.units?["Hero"];
            }
            else if (uConfig == null && !isHero)
            {
                uConfig = this.units?["Trooper"];
            }

            //KINGDOM UNIT
            kuConfig = kConfig?.units?[unit];
            if (kuConfig == null && isPlayer)
            {
                kuConfig = kConfig?.units?["Player"];
            }
            if (kuConfig == null && isKing)
            {
                kuConfig = kConfig?.units?["King"];
            }
            if (kuConfig == null && isLeader)
            {
                kuConfig = kConfig?.units?["Leader"];
            }
            if (kuConfig == null && isHero)
            {
                kuConfig = kConfig?.units?["Hero"];
            }
            else if (kuConfig == null && !isHero)
            {
                kuConfig = kConfig?.units?["Trooper"];
            }

            //CLAN UNIT
            cuConfig = cConfig?.units?[unit];
            if (cuConfig == null && isPlayer)
            {
                cuConfig = cConfig?.units?["Player"];
            }
            if (cuConfig == null && isKing)
            {
                cuConfig = cConfig?.units?["King"];
            }
            if (cuConfig == null && isLeader)
            {
                cuConfig = cConfig?.units?["Leader"];
            }
            if (cuConfig == null && isHero)
            {
                cuConfig = cConfig?.units?["Hero"];
            }
            else if (cuConfig == null && !isHero)
            {
                cuConfig = cConfig?.units?["Trooper"];
            }

            //KINGDOM CLAN UNIT
            kcuConfig = kcConfig?.units?[unit];
            if (kcuConfig == null && isPlayer)
            {
                kcuConfig = kcConfig?.units?["Player"];
            }
            if (kcuConfig == null && isKing)
            {
                kcuConfig = kcConfig?.units?["King"];
            }
            if (kcuConfig == null && isLeader)
            {
                kcuConfig = kcConfig?.units?["Leader"];
            }
            if (kcuConfig == null && isHero)
            {
                kcuConfig = kcConfig?.units?["Hero"];
            }
            else if (kcuConfig == null && !isHero)
            {
                kcuConfig = kcConfig?.units?["Trooper"];
            }

            return (kConfig, kcConfig, kuConfig, kcuConfig, cConfig, cuConfig, uConfig);
        }

        public void parseGroups()
        {
            //Parse Standalone Groups
            if (this.groups is object)
            {
                foreach (PocColorModGroupEntry group in this.groups)
                {
                    Map<String, PocColorModConfigUnit> units = group.toUnits();
                    if (this.units is object)
                    {
                        foreach (string unitName in this.units.Keys)
                        {
                            units[unitName] = this.units[unitName];
                        }
                    }
                    this.units = units;
                }
            }
            //Parse Clans Groups
            if (this.clans is object)
            {
                foreach (string clanName in this.clans.Keys)
                {
                    PocColorModConfigClan clanConfig = this.clans[clanName];
                    if (clanConfig.groups is object)
                    {
                        foreach (PocColorModGroupEntry group in clanConfig.groups)
                        {
                            Map<String, PocColorModConfigUnit> units = group.toUnits();
                            if (this.clans[clanName].units is object)
                            {
                                foreach (string unitName in this.clans[clanName].units.Keys)
                                {
                                    units[unitName] = this.clans[clanName].units[unitName];
                                }
                            }
                            this.clans[clanName].units = units;
                        }
                    }
                }
            }
            //Parse Kingdoms Groups
            if (this.kingdoms is object)
            {
                foreach (string kingdomName in this.kingdoms.Keys)
                {
                    PocColorModConfigKingdom kingdomConfig = this.kingdoms[kingdomName];
                    if (kingdomConfig.groups is object)
                    {
                        foreach (PocColorModGroupEntry group in kingdomConfig.groups)
                        {
                            Map<String, PocColorModConfigUnit> units = group.toUnits();
                            if (this.kingdoms[kingdomName].units is object)
                            {
                                foreach (string unitName in this.kingdoms[kingdomName].units.Keys)
                                {
                                    units[unitName] = this.kingdoms[kingdomName].units[unitName];
                                }
                            }
                            this.kingdoms[kingdomName].units = units;
                        }
                    }
                    //Parse Kingdoms/Clans
                    if (kingdomConfig.clans is object)
                    {
                        foreach (string clanName in kingdomConfig.clans.Keys)
                        {
                            PocColorModConfigClan clanConfig = kingdomConfig.clans[clanName];
                            if (clanConfig.groups is object)
                            {
                                foreach (PocColorModGroupEntry group in clanConfig.groups)
                                {
                                    Map<String, PocColorModConfigUnit> units = group.toUnits();
                                    if (kingdomConfig.clans[clanName].units is object)
                                    {
                                        foreach (string unitName in kingdomConfig.clans[clanName].units.Keys)
                                        {
                                            units[unitName] = kingdomConfig.clans[clanName].units[unitName];
                                        }
                                    }
                                    this.kingdoms[kingdomName].clans[clanName].units = units;
                                }
                            }
                        }
                    }
                }
            }

        }

    }


}

