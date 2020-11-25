using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using TaleWorlds.CampaignSystem.ViewModelCollection.Craft;

namespace PocColor.Config
{
    public class PocColorModConfig
    {

        public const string PLAYER_NAME = "Player";
        public const string PLAYER_KINGDOM = "PlayerKingdom";
        public const string PLAYER_CLAN = "PlayerClan";

        public bool generateTemplate { get; set; } = false;
        public PocColorModConfigColorEntry defaultConfig { get; set; }

        public Map<String, PocColorModConfigKingdom> kingdoms { get; set; }

        public Map<String, PocColorModConfigClan> clans { get; set; }

        public Map<String, PocColorModConfigUnit> units { get; set; }

        public (bool, string, string, string) GetClanConfig(string kingdom, string clan, bool isPlayerKingdom, bool isPlayerClan) {

            PocColorModConfigKingdom kConfig = null;
            PocColorModConfigClan kcConfig = null;
            PocColorModConfigClan cConfig = null;

            string follow = null;
            string banner = null, primaryColor = null, secondaryColor = null;
            bool success = false;

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
                //    Log.write("- kconfig is defined");
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
                //   Log.write("- kcConfig is defined");
                //}

                if (kcConfig == null && isPlayerClan)
                {
                    kcConfig = kConfig?.clans?["PlayerClan"];
                    //if (kcConfig is object)
                    //{
                    //    Log.write("- PlayerClan config is defined within kingdoms.clans");
                    //}
                }

                banner = cConfig?.clanBanner ?? banner;
                //Log.write("=> clan banner: [" + banner + "]");
                banner = kcConfig?.clanBanner ?? banner;
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

            }
            catch (Exception e) {
                Log.write(e.Message);
            }

            return (success, banner, primaryColor, secondaryColor);
        }

        public (string, string, string) GetKingdomConfig(string kingdom, bool isPlayerKingdom)
        {
            PocColorModConfigKingdom kConfig = null;
            string banner = null;
            string primaryColor = null;
            string secondaryColor = null;

            try { 

                kConfig = this.kingdoms?[kingdom];
                if (kConfig == null && isPlayerKingdom)
                {
                    kConfig = this.kingdoms?["PlayerKingdom"];
                }
            
                banner = kConfig?.kingdomBanner ?? banner;
                primaryColor = kConfig?.primaryColor ?? primaryColor;
                secondaryColor = kConfig?.secondaryColor ?? secondaryColor;

            }
            catch (Exception e)
            {
                Log.write(e.Message);
            }
            return (banner, primaryColor, secondaryColor);
        }


        public (int, string[], string[], string[], string[]) GetConfig(string kingdom, string clan, string unit, bool isPlayerKingdom, bool isPlayerClan, bool isPlayer)
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

            try {



                //search config from lowest to highest priority
                //Kingdom
                //Clan
                //KingdomClan
                //Unit
                //KingdomUnit
                //ClanUnit
                //KingdomClanUnit
    
                kConfig = this.kingdoms?[kingdom];
                if (kConfig == null && isPlayerKingdom) {
                    kConfig = this.kingdoms?["PlayerKingdom"];
                }
                cConfig = this.clans?[clan];
                if (cConfig == null && isPlayerClan)
                {
                    cConfig = this.clans?["PlayerClan"];
                }
                kcConfig = kConfig?.clans?[clan];
                if (kcConfig == null && isPlayerClan)
                {
                    kcConfig = kConfig?.clans?["PlayerClan"];
                }
                uConfig = this.units?[unit];
                if (uConfig == null && isPlayer)
                {
                    uConfig = this.units?["Player"];
                }
                kuConfig = kConfig?.units?[unit];
                if (kuConfig == null && isPlayer)
                {
                    kuConfig = kConfig?.units?["Player"];
                }
                cuConfig = cConfig?.units?[unit];
                if (cuConfig == null && isPlayer)
                {
                    cuConfig = cConfig?.units?["Player"];
                }
                kcuConfig = kcConfig?.units?[unit];
                if (kcuConfig == null && isPlayer)
                {
                    kcuConfig = kcConfig?.units?["Player"];
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
                        else if (!String.IsNullOrEmpty(banner)) {
                            bannersFull = new string[1];
                            bannersFull[0] = banner;
                        }
                    }

            }
            catch (Exception e)
            {
                Log.write(e.Message);
            }
            return (mode, color, color2, bannersFull, shields);
        }

    }
}
