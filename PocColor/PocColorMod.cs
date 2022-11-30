using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using HarmonyLib;
using PocColor.Config;
using Newtonsoft.Json;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using psai.Editor;

namespace PocColor
{
    public class PocColorMod : MBSubModuleBase
    {

        public const string MOD_VERSION = "1.1.2";

        public static PocColorModConfig config { get; set; }

        public static Map<string, string> bannerKingdomCache { get; set; }

        public static Map<string, string> bannerClanCache { get; set; }

        public static Boolean doLog = false;

        public static string SerializeBanner(Banner banner)
        {
            StringBuilder myStringBuilder = new StringBuilder();
            bool flag = true;
            foreach (BannerData bannerData in banner.BannerDataList)
            {

                try
                {
                    if (!flag)
                        myStringBuilder.Append(".");
                    flag = false;
                    myStringBuilder.Append(bannerData.MeshId);
                    myStringBuilder.Append('.');
                    myStringBuilder.Append(bannerData.ColorId);
                    myStringBuilder.Append('.');
                    myStringBuilder.Append(bannerData.ColorId2);
                    myStringBuilder.Append('.');
                    myStringBuilder.Append((int)bannerData.Size.x);
                    myStringBuilder.Append('.');
                    myStringBuilder.Append((int)bannerData.Size.y);
                    myStringBuilder.Append('.');
                    myStringBuilder.Append((int)bannerData.Position.x);
                    myStringBuilder.Append('.');
                    myStringBuilder.Append((int)bannerData.Position.y);
                    myStringBuilder.Append('.');
                    myStringBuilder.Append(bannerData.DrawStroke ? 1 : 0);
                    myStringBuilder.Append('.');
                    myStringBuilder.Append(bannerData.Mirror ? 1 : 0);
                    myStringBuilder.Append('.');
                    float rotationFlt = (float)bannerData.RotationValue / 0.00278f;
                    double rounded = Math.Round(rotationFlt, MidpointRounding.AwayFromZero);
                    int rotationInt = Int16.Parse(rounded.ToString());
                    myStringBuilder.Append(rotationInt);
                }
                catch (Exception e)
                {
                    Log.write(e.Message);
                }
            }
            return myStringBuilder.ToString();
        }


        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                new Harmony("com.prpl.poccolormod").PatchAll();
            }
            catch (Exception e)
            {
                Log.write("==> Harmony patching failed:" + e.Message);
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InformationManager.DisplayMessage(new InformationMessage("POC color mod ["+ MOD_VERSION +"]: loaded!"));
        }

        public override void OnGameInitializationFinished(Game game)
        {
            bool follow = false, followBG = false;
            string kingdomBannerStr = null;
            string kingdomShieldStr = null;
            string primary = null;
            string secondary = null;
            string clanBanner = null, clanShield = null;

            Map<string, string> bannerKingdomCacheTmp = new Map<string, string>();
            Map<string, string> bannerClanCacheTmp = new Map<string, string>();

            try
            {
                PocColorMod.config = new PocColorModConfig();
                Log.write("--- POC COLOR MOD: "+ MOD_VERSION +" ---");

                Log.write("read config file");
                PocColorMod.config = JsonConvert.DeserializeObject<PocColorModConfig>(File.ReadAllText(@"..\..\modules\PocColor\config.json"));
                PocColorMod.config.parseGroups();
            }
            catch (FileNotFoundException e)
            {
                Log.write("config file not found: " + e.Message);
            }
            catch (Exception e)  //ugly but needed!! 
            {
                Log.write("Could not parse the config file :(");
                Log.write(" =====>" + e.Message);
            }
            PocColorMod.doLog = PocColorMod.config.log;

            Log.write("Build Cache -- start");

            try
            {

                for (int i = 0; i< Campaign.Current.Kingdoms.Count; i++)
                {
                    Kingdom kingdom = Campaign.Current.Kingdoms[i];

                    Log.write("====> kingdom [" + kingdom.Name.ToString() + "]");

                    //Get Kingdom Banner And Set It
                    string playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();
                    bool isPlayerKingdom = !string.IsNullOrEmpty(playerKingdomName) && kingdom.Name?.ToString() == playerKingdomName && Clan.PlayerClan.IsKingdomFaction;

                    (kingdomBannerStr, kingdomShieldStr, primary, secondary) = PocColorMod.config.GetKingdomConfig(kingdom.Name?.ToString(), isPlayerKingdom);

                    //Add Original Kingdom banner in cache
                    String bannerStr = PocColorMod.SerializeBanner(kingdom.Banner);
                    bannerKingdomCacheTmp.TryAdd(bannerStr, kingdom.Name.ToString());
                    //Add Ruling clan Banner
                    bannerClanCacheTmp.TryAdd(bannerStr, kingdom.RulingClan?.Name.ToString());
                    if (PocColorMod.doLog) Log.write("> Initial Kingdom Banner loaded from game: [" + bannerStr + "]");
                    if (PocColorMod.doLog) Log.write("> Initial Kingdom primary color loaded from game: [" + BannerManager.GetColorId(kingdom.PrimaryBannerColor) + "]");
                    if (PocColorMod.doLog) Log.write("> Initial Kingdom secondary color loaded from game: [" + BannerManager.GetColorId(kingdom.SecondaryBannerColor) + "]");

                    if (primary is object)
                    {
                        //Update Kingdom Primary Color
                        uint color1 = PocColorModSetColors.parseColor(primary);
                    
                        FieldInfo fi = typeof(Kingdom).GetField("<PrimaryBannerColor>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                        TypedReference reference = __makeref(kingdom);
                        fi.SetValueDirect(reference, color1);

                        FieldInfo fi2 = typeof(Kingdom).GetField("<Color>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                        TypedReference reference2 = __makeref(kingdom);
                        fi2.SetValueDirect(reference2, color1);

                        //Update Kingdom Banner
                        if (PocColorMod.doLog)  Log.write("> updating primaryColor for kingdom: [" + PocColorMod.SerializeBanner(kingdom.Banner) + "]");
                        kingdom.Banner?.ChangePrimaryColor(color1);
                        if (PocColorMod.doLog)  Log.write("> updated Banner: [" + PocColorMod.SerializeBanner(kingdom.Banner) + "]");
                    }

                    if (secondary is object)
                    {
                        //Update Kingdom Secondary Color
                        uint color2 = PocColorModSetColors.parseColor(secondary);
                    
                        FieldInfo fi = typeof(Kingdom).GetField("<SecondaryBannerColor>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                        TypedReference reference = __makeref(kingdom);
                        fi.SetValueDirect(reference, color2);

                        FieldInfo fi2 = typeof(Kingdom).GetField("<Color2>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                        TypedReference reference2 = __makeref(kingdom);
                        fi2.SetValueDirect(reference2, color2);

                        //Update Kingdom Banner
                        if (PocColorMod.doLog)  Log.write("> updating SecondaryColor for kingdom: [" + PocColorMod.SerializeBanner(kingdom.Banner) + "]");
                        kingdom.Banner?.ChangeIconColors(color2);
                        if (PocColorMod.doLog)  Log.write("> updated Banner: [" + PocColorMod.SerializeBanner(kingdom.Banner) + "]");
                    }

                    if (kingdomBannerStr is object)
                    {
                        //override banner
                        if (PocColorMod.doLog) Log.write("> updating banner for kingdom: [" + kingdom.Name.ToString() + ", " + kingdomBannerStr + "]");
                        Banner kingdomBanner = new Banner(kingdomBannerStr);

                        //Traverse.Create(kingdom).Field("Banner").SetValue(kingdomBanner);
                        FieldInfo fi = typeof(Kingdom).GetField("<Banner>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                        TypedReference reference = __makeref(kingdom);
                        fi.SetValueDirect(reference, kingdomBanner);
                        if (PocColorMod.doLog)  Log.write("> updated KingdomBanner from Config: [" + PocColorMod.SerializeBanner(kingdom.Banner) + "]");
                    }

                    kingdomBannerStr = PocColorMod.SerializeBanner(kingdom.Banner);
                    Log.write("> adding new cache for banner: [" + kingdomBannerStr + "]");
                    
                    bannerKingdomCacheTmp.TryAdd(kingdomBannerStr, kingdom.Name.ToString());
                    bannerClanCacheTmp.TryAdd(kingdomBannerStr, kingdom.RulingClan?.Name.ToString());

                    foreach (Clan clan in kingdom.Clans)
                    {

                    try
                    {
                         Log.write("====> kingdom [" + kingdom.Name.ToString() + "]" + ", clan [" + clan.Name.ToString() + "]");

                        //Get Clan Banner: if defined, override it. 
                        string playerClanName = Clan.PlayerClan?.Name?.ToString();
                        playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();

                        bool isPlayerClan = (!string.IsNullOrEmpty(playerClanName) && clan.Name?.ToString() == playerClanName);
                        isPlayerKingdom = !string.IsNullOrEmpty(playerKingdomName) && kingdom.Name?.ToString() == playerKingdomName && Clan.PlayerClan.IsKingdomFaction;

                        Log.write("> clan: [" + clan.Name?.ToString() + "] kingdom: " + kingdom.Name?.ToString() + ", isplayerKingdom: " + isPlayerKingdom + ", isPlayerClan:" + isPlayerClan);
                        (follow, followBG, clanBanner, clanShield, primary, secondary) = PocColorMod.config.GetClanConfig(kingdom.Name?.ToString(), clan.Name?.ToString(), isPlayerKingdom, isPlayerClan);
                        Log.write("> clan: [" + clan.Name?.ToString() + "] follow colors: "+ follow + ", primary: " + primary + ", secondary:" + secondary );

                        if (PocColorMod.doLog) Log.write("> Initial clan Banner loaded from game: [" + PocColorMod.SerializeBanner(clan.Banner) + "]");
                        if (PocColorMod.doLog) Log.write("> Initial clan primary loaded from game: [" + BannerManager.GetColorId(clan.Color) + "]");
                        if (PocColorMod.doLog) Log.write("> Initial clan secondary loaded from game: [" + BannerManager.GetColorId(clan.Color2) + "]");

                            //if (follow && kingdom is object)
                            //{
                            //    Log.write("Follow: true; Apply kingdom colors to current Clan");
                            //    Traverse.Create(clan).Field("Color").SetValue(kingdom.PrimaryBannerColor);
                            //    Traverse.Create(clan).Field("Color2").SetValue(kingdom.SecondaryBannerColor);
                            //}

                            if (clanBanner is object)
                            {
                                //override banner
                                Log.write("> updating banner for clan: [" + clan.Name.ToString() + ", " + clanBanner + "]");
                                Banner banner = new Banner(clanBanner);
                                Traverse.Create(clan).Field("_banner").SetValue(banner);
                            }

                            if (primary is object)
                            {
                                Log.write("> updating primary color for clan");

                                uint color1 = PocColorModSetColors.parseColor(primary);
                                Traverse.Create(clan).Field("Color").SetValue(color1);
                                clan.Banner?.ChangePrimaryColor(color1);
                            }

                            if (secondary is object)
                            {
                                Log.write("> updating secondary color for clan");

                                uint color2 = PocColorModSetColors.parseColor(secondary);
                                Traverse.Create(clan).Field("Color2").SetValue(color2);
                                clan.Banner?.ChangeIconColors(color2);
                            }

                            if (String.IsNullOrEmpty(primary) && String.IsNullOrEmpty(secondary) && follow)
                            {
                                Log.write("> updating colors of clan from Kingdom");

                                //Force update of colors from kingdom (default behaviour)
                                Traverse.Create((object)clan).Method("UpdateBannerColorsAccordingToKingdom").GetValue();
                            }

                            bannerStr = PocColorMod.SerializeBanner(clan.Banner);

                            Log.write("> adding new cache for banner: [" + bannerStr + "]");
                            bannerClanCacheTmp.TryAdd(bannerStr, clan.Name.ToString());
                            bannerKingdomCacheTmp.TryAdd(bannerStr, kingdom?.Name?.ToString());

                        }
                    catch (Exception e)
                    {
                        Log.write(e.Message);
                    }
                }
            }
                foreach (Clan clan in Campaign.Current.Clans)
                {
                    Kingdom kingdom = clan.Kingdom;

                    Log.write("====> clan [" + clan.Name.ToString() + "]");

                    //Get Clan Banner: if defined, override it. 
                    string playerClanName = Clan.PlayerClan?.Name?.ToString();
                    bool isPlayerClan = (!string.IsNullOrEmpty(playerClanName) && clan.Name?.ToString() == playerClanName);
                    string playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();
                    bool isPlayerKingdom = !string.IsNullOrEmpty(playerKingdomName) && kingdom?.Name?.ToString() == playerKingdomName && Clan.PlayerClan.IsKingdomFaction;

                    Log.write("> clan: [" + clan.Name?.ToString() + "] kingdom: " + kingdom?.Name?.ToString() + ", isplayerKingdom: " + isPlayerKingdom + ", isPlayerClan:" + isPlayerClan);
                    (follow, followBG, clanBanner, clanShield, primary, secondary) = PocColorMod.config.GetClanConfig(kingdom?.Name?.ToString(), clan.Name?.ToString(), isPlayerKingdom, isPlayerClan);
                    Log.write("> clan: [" + clan.Name?.ToString() + "] follow: " + follow + ", follow Background: " + followBG + ", primary: " + primary + ", secondary:" + secondary);

                    if (PocColorMod.doLog) Log.write("> Initial clan Banner loaded from game: [" + PocColorMod.SerializeBanner(clan.Banner) + "]");
                    if (PocColorMod.doLog) Log.write("> Initial clan primary loaded from game: [" + BannerManager.GetColorId(clan.Color) + "]");
                    if (PocColorMod.doLog) Log.write("> Initial clan secondary loaded from game: [" + BannerManager.GetColorId(clan.Color2) + "]");


                    if (clanBanner is object)
                    {
                        //override banner
                        Log.write("> updating banner for clan: [" + clan.Name.ToString() + ", " + clanBanner + "]");
                        Banner banner = new Banner(clanBanner);
                        Traverse.Create(clan).Field("_banner").SetValue(banner);
                    }

                    if (primary is object)
                    {
                        Log.write("> updating primary color for clan");

                        uint color1 = PocColorModSetColors.parseColor(primary);
                        Traverse.Create(clan).Field("Color").SetValue(color1);
                        clan.Banner?.ChangePrimaryColor(color1);
                    }

                    if (secondary is object)
                    {
                        Log.write("> updating secondary color for clan");

                        uint color2 = PocColorModSetColors.parseColor(secondary);
                        Traverse.Create(clan).Field("Color2").SetValue(color2);
                        clan.Banner?.ChangeIconColors(color2);
                    }

                    if (String.IsNullOrEmpty(primary) && String.IsNullOrEmpty(secondary) && (follow || followBG) )
                    {
                        Log.write("> updating colors of clan from Kingdom");

                        //Force update of colors from kingdom (default behaviour)
                        Traverse.Create((object)clan).Method("UpdateBannerColorsAccordingToKingdom").GetValue();
                    }

                    string bannerStr = PocColorMod.SerializeBanner(clan.Banner);
                    
                    Log.write("> adding new cache for banner: [" + bannerStr + "]");
                    bannerClanCacheTmp.TryAdd(bannerStr, clan.Name.ToString());
                    bannerKingdomCacheTmp.TryAdd(bannerStr, kingdom?.Name?.ToString());
            }
            PocColorMod.bannerClanCache = bannerClanCacheTmp;
            PocColorMod.bannerKingdomCache = bannerKingdomCacheTmp;

            Log.write("Build Cache -- done");

            if (PocColorMod.config.generateTemplate )
            {
                Log.write("Building json template ...");
                PocColorModConfig newConfigTemplate = new PocColorModConfig();
                //Build a JSON template...
                newConfigTemplate.kingdoms = new Map<String, PocColorModConfigKingdom>();
                newConfigTemplate.clans = new Map<String, PocColorModConfigClan>();
                foreach (Kingdom kingdom in Campaign.Current.Kingdoms)
                {
                    //There you have kingdoms
                    PocColorModConfigKingdom k = new PocColorModConfigKingdom();
                    k.clans = new Map<String, PocColorModConfigClan>();
                    k.mode = 2;
                    k.color = new string[2] { "color1", "color2" };
                    k.color2 = new string[2] { "color1", "color2" };
                    k.banner = "dummy banner";
                    k.banners = new string[3] { "banner1", "banner2", "banner3" };
                    k.FollowKingdomColors = "false";
                    k.kingdomBanner = "the.super.kingdom.banner";
                    k.primaryColor = "88";
                    k.secondaryColor = "40";
                    k.shields = new string[3] { "shield1", "shield2", "shield3" };

                    foreach (Clan clan in kingdom.Clans)
                    {
                        PocColorModConfigClan c = new PocColorModConfigClan();
                        c.mode = 2;
                        c.color = new string[2] {"color1", "color2" };
                        c.color2 = new string[2] { "color1", "color2" };
                        c.units = new Map<String, PocColorModConfigUnit>();
                        c.banner = "dummy banner";
                        c.banners = new string[3] { "banner1", "banner2", "banner3" };
                        c.FollowKingdomColors = "false";
                        c.clanBanner = "the.super.kingdom.banner";
                        c.primaryColor = "83";
                        c.secondaryColor = "40";

                        c.shields = new string[3] { "shield1", "shield2", "shield3" };

                        //There you have clans
                        foreach (Hero hero in clan.Heroes)
                        {
                            //There you have heroes
                            PocColorModConfigUnit u = new PocColorModConfigUnit();
                            u.mode = 3;
                            u.color = new string[2] { "color1", "color2" };
                            u.color2 = new string[2] { "color1", "color2" };
                            u.banner = "dummy banner";
                            u.banners = new string[3] { "banner1", "banner2", "banner3" };
                            u.shields = new string[3] { "shield1", "shield2", "shield3" };

                            c.units.TryAdd(hero.Name.ToString(), u);
                        }
                        foreach (Hero hero in clan.Heroes)
                        {
                            PocColorModConfigUnit u = new PocColorModConfigUnit();
                            u.mode = 3;
                            u.color = new string[2] { "color1", "color2" };
                            u.color2 = new string[2] { "color1", "color2" };
                            u.banner = "dummy banner";
                            u.banners = new string[3] { "banner1", "banner2", "banner3" };
                            u.shields = new string[3] { "shield1", "shield2", "shield3" };

                            c.units.TryAdd(hero.Name.ToString(), u);
                            
                        }
                        k.clans.TryAdd(clan.Name.ToString(), c);

                    }
                    newConfigTemplate.kingdoms.TryAdd(kingdom.Name.ToString(), k);

                }
                foreach (Clan clan in Campaign.Current.Clans)
                {
                    if (clan.Kingdom != null) continue;

                    PocColorModConfigClan c = new PocColorModConfigClan();
                    c.mode = 2;
                    c.color = new string[2] { "color1", "color2" };
                    c.color2 = new string[2] { "color1", "color2" };
                    c.units = new Map<String, PocColorModConfigUnit>();
                    c.banner = "dummy banner";
                    c.banners = new string[3] { "banner1", "banner2", "banner3" };
                    c.FollowKingdomColors = "false";
                    c.clanBanner = "the.super.kingdom.banner";
                    c.primaryColor = "83";
                    c.secondaryColor = "40";

                    c.units = new Map<String, PocColorModConfigUnit>();

                    //There you have clans
                    foreach (Hero hero in clan.Heroes)
                    {
                        //There you have heroes
                        PocColorModConfigUnit u = new PocColorModConfigUnit();
                        u.mode = 3;
                        u.color = new string[2] { "color1", "color2" };
                        u.color2 = new string[2] { "color1", "color2" };
                        u.banner = "dummy banner";
                        u.banners = new string[3] { "banner1", "banner2", "banner3" };
                        u.shields = new string[3] { "shield1", "shield2", "shield3" };

                        c.units.TryAdd(hero.Name.ToString(), u);
                    }
                    foreach (Hero hero in clan.Heroes)
                    {
                           PocColorModConfigUnit u = new PocColorModConfigUnit();
                           u.mode = 3;
                           u.color = new string[2] { "color1", "color2" };
                           u.color2 = new string[2] { "color1", "color2" };
                           u.banner = "dummy banner";
                           u.banners = new string[3] { "banner1", "banner2", "banner3" };
                           u.shields = new string[3] { "shield1", "shield2", "shield3" };
                           c.units.TryAdd(hero.Name.ToString(), u);
                    }
                    newConfigTemplate.clans.TryAdd(clan.Name.ToString(), c);
                }

                JsonSerializer jsonWriter = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                using (StreamWriter writer = new StreamWriter(@"..\..\modules\PocColor\config.json.full.template"))
                {
                    jsonWriter.Serialize(writer, newConfigTemplate);
                }
                Log.write("Building json template - Done ");
            }

            }
            catch (Exception e)
            {
                Log.write(e.Message);
            }

            /*
            for (int i = 0; i < Campaign.Current.Kingdoms.Count; i++)
            {
                Kingdom kingdom = Campaign.Current.Kingdoms[i];
                kingdomBannerStr = PocColorMod.SerializeBanner(kingdom.Banner);
                Log.write("====> kingdom [" + kingdom.Name.ToString() + "]" + " banner: " + kingdomBannerStr);
            }
            */

        }
    }
}
