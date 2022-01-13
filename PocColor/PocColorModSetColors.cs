using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using HarmonyLib;

using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

using SandBox.View.Map;
using TaleWorlds.ObjectSystem;
using System.Threading;
using SandBox.GauntletUI;
using SandBox.ViewModelCollection.Nameplate;

using PocColor.Config;
using NUnit.Framework;

namespace PocColor
{
	class PocColorModSetColors
	{

		/*
		 * 
		 * color mode:
		 *  
		 *  - 0: default
		 *  - 1: randomized. If no array is specified: randomize all colors. If color arrays are specified, colors are picked from this arrays (if only one array, each color is picked in this array; If two color arrays: each color is picked in its own palette array).
		 *  - 11: randomized but same index in color array is used for both color1 and color2:   if color = [ A,B ,C ]  and color2 = [ D, E, F ]   => it allows to set colors by pair:  A/D , B/E, C/F ... (the max index possible is the min length of the two arrays). If only one array, or no array: a unique color is used.
		 *  - 2: banner linked
		 *  - 3: overriden with custom values
		 *  - other value: default
		 * 
		 */
		public const int DEFAULT_MODE = 0;
		public const int RANDOMIZED = 1;
		public const int RANDOMIZED_LINKED = 11;
		public const int SHIELD_LINKED = 2;
		public const int SHIELD_LINKED_RANDOM = 21;
		public const int SHIELD_LINKED_RANDOM_FULL = 22;
		public const int BANNER_LINKED = 4;
		public const int BANNER_LINKED_RANDOM = 41;
		public const int BANNER_LINKED_RANDOM_FULL = 42;

		public const int OVERRIDE = 3;

		public const int COLOR_COUNT = 158;

		public static Random rnd = new Random();

		//A map to store the units names allows resolving them from anywhere
		public static Map<string, CharInfo > unitNames = new Map<string, CharInfo>();
		public static Map<string, Integer> extraColorMap = new Map<string, Integer>();

		public static string DUMMY_BANNER = "30.83.88.1536.1536.768.768.1.0.0.522.40.40.924.924.762.775.0.0.0";

		public static uint parseColor(string colorStr)
		{

			if (colorStr.StartsWith("#"))
			{
				string hex = "FF" + colorStr.Substring(1, 6);
				return Convert.ToUInt32(hex, 16);
			}
			else
			{
				try
				{
					int i = int.Parse(colorStr);
					return BannerManager.GetColor(i);
				}
				catch (Exception e)
				{
					return BannerManager.GetColor(40);
				}
			}
		}

		public static uint getRandomColor()
		{
			int r = rnd.Next(0, COLOR_COUNT);
			return BannerManager.GetColor(r);
		}

		public static uint getRandomColorFromColors(string[] colors)
		{
			int r = rnd.Next(0, colors.Count());
			return parseColor((colors[r]));
		}


		public static string getRandomBannerFromBanners(string[] banners)
		{
			int r = rnd.Next(0, banners.Count());
			return banners[r];
		}

		public static(uint?, uint?) getRandomPairColorsFromColors(string[] colors, string[] colors2)
		{
			int min = Math.Min(colors.Count(), colors2.Count());
			int r = rnd.Next(0, min);
			return (parseColor(colors[r]), parseColor(colors2[r]));
		}

		static private (int, uint?, uint?, string, string) getColorFromConfig(ref Banner banner, string unitName, bool isHero)
		{
			string bannerStr = banner is object ? PocColorMod.SerializeBanner(banner) : null;
			return getColorFromConfig(ref banner, ref bannerStr, unitName, isHero);
		}
		static private (int, uint?, uint?, string, string) getColorFromConfig(ref string bannerStr, string unitName, bool isHero)
		{
			Banner banner = bannerStr is object ? new Banner(bannerStr) : null;
			return getColorFromConfig(ref banner, ref bannerStr, unitName, isHero);
		}

		static private (int, uint?, uint?, string, string) getBattleColorFromConfig(ref Banner banner, string unitName, bool isHero)
		{
			string bannerStr = banner is object ? PocColorMod.SerializeBanner(banner) : null;
			return getBattleColorFromConfig(ref banner, ref bannerStr, unitName, isHero);
		}
		static private (int, uint?, uint?, string, string) getBattleColorFromConfig(ref string bannerStr, string unitName, bool isHero)
		{
			Banner banner = bannerStr is object ? new Banner(bannerStr) : null;
			return getBattleColorFromConfig(ref banner, ref bannerStr, unitName, isHero);
		}

		static private (string, string) getBannerFromConfig(string bannerStr, string unitName, bool isHero)
		{

			string newBanner = null;
			string newShield = null;

			try
			{
				//Log.write("Finding: " + unitName + ", " + bannerStr);

				bool isPlayer = (unitName == Hero.MainHero.Name.ToString());

				string clan = PocColorMod.bannerClanCache[bannerStr] ?? "";
				string kingdom = PocColorMod.bannerKingdomCache[bannerStr] ?? "";

				Kingdom myKingdom = null;
				for (int i = 0; i < Campaign.Current.Kingdoms.Count; i++)
				{
					myKingdom = Campaign.Current.Kingdoms[i].Name.ToString() == kingdom ? Campaign.Current.Kingdoms[i] : null;
					if (myKingdom is object) break;
				}

				bool isClan(Clan c) { return c.Name.ToString() == clan; }
				Clan myclan = Clan.FindFirst(isClan);

				bool isKing = false;
				bool isLeader = false;

				if ( unitName is object) isKing = (myKingdom?.Leader?.Name?.ToString() == unitName);
				if (unitName is object) isLeader = (myclan?.Leader?.Name.ToString() == unitName);

				string playerClanName = Clan.PlayerClan?.Name?.ToString();
				string playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();

				if (isPlayer && clan != playerClanName && !string.IsNullOrEmpty(playerClanName) && !string.IsNullOrEmpty(clan))
				{
					//Wait a minute! this should be playerClan: how come it has not been found? Ok! the banner or clan name has changed: lets update them
					//Log.write("> player banner or clan name has changed, updating cache...");

					string cl;
					string kd;

					//Try to remove any existing entry
					PocColorMod.bannerClanCache.TryRemove(bannerStr, out cl);
					PocColorMod.bannerKingdomCache.TryRemove(bannerStr, out kd);
					//Add the new entries 

					PocColorMod.bannerClanCache.TryAdd(bannerStr, playerClanName);
					clan = playerClanName;
					if (playerKingdomName is object)
					{
						PocColorMod.bannerKingdomCache.TryAdd(bannerStr, playerKingdomName);
						kingdom = playerKingdomName;
					}

				}

				bool isPlayerClan = (!string.IsNullOrEmpty(playerClanName) && clan == playerClanName) || (isPlayer && !string.IsNullOrEmpty(clan));
				bool isPlayerKingdom = !string.IsNullOrEmpty(playerKingdomName) && kingdom == playerKingdomName && Clan.PlayerClan.IsKingdomFaction;

				if (PocColorMod.doLog) Log.write("==> character is: [" + unitName + "] of clan [" + clan + "] of kingdom [" + kingdom + "] isPlayerKingdom [" + isPlayerKingdom + "] isPlayerClan [" + isPlayerClan + "] isPlayer [" + isPlayer + "] isKing [" + isKing + "] isLeader [" + isLeader + "] isHero [" + isHero + "]");
				(int mode, string[] colors, string[] colors2, string[] banners, string[] shields ) = PocColorMod.config.GetConfig(kingdom, clan, unitName, isPlayerKingdom, isPlayerClan, isPlayer, isKing, isLeader, isHero);
				
				string colorsstr = colors is object ? string.Join(",", colors) : "";
				string colors2str = colors2 is object ? string.Join(",", colors2) : "";
				string shieldsStr = shields is object ? string.Join(",", shields) : "";
				string bannersStr = banners is object ? string.Join(",", banners) : "";

				if (PocColorMod.doLog) Log.write("> Applying colors according to: mode[" + mode + "] colors[" + colorsstr + "] colors2[" + colors2str + "] newBanner[" + bannersStr + "] shields[" + shieldsStr + "]");

				if (banners is object && banners.Length > 0)
				{
					newBanner = getRandomBannerFromBanners(banners);

					//Log.write("==> new Banner to apply : " + newBanner);

					PocColorMod.bannerClanCache.TryAdd(newBanner, clan);
					PocColorMod.bannerKingdomCache.TryAdd(newBanner, kingdom);
				}
				if (shields is object && shields.Length > 0)
				{
					newShield = getRandomBannerFromBanners(shields);

					//Log.write("==> new Shield to apply : " + newShield);

					PocColorMod.bannerClanCache.TryAdd(newShield, clan);
					PocColorMod.bannerKingdomCache.TryAdd(newShield, kingdom);
				}
			}
			catch (Exception e)
			{
				Log.write(e.Message);
			}
			return (newBanner, newShield);
		}

		static private void updateCache(string bannerStr, string unitName)
		{

			try
			{
				//Log.write("Updating cache: " + unitName + ", " + bannerStr);

				bool isPlayer = (unitName == Hero.MainHero.Name.ToString());

				string clan = PocColorMod.bannerClanCache[bannerStr] ?? "";
				string playerClanName = Clan.PlayerClan?.Name?.ToString();
				string playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();

				if (isPlayer && clan != playerClanName && playerClanName is object)
				{
					//Wait a minute! this should be playerClan: how come it has not been found? Ok! the banner or clan name has changed: lets update them
					//Log.write("> player banner or clan name has changed, updating cache...");

					string cl;
					string kd;

					//Try to remove any existing entry
					PocColorMod.bannerClanCache.TryRemove(bannerStr, out cl);
					PocColorMod.bannerKingdomCache.TryRemove(bannerStr, out kd);
					//Add the new entries 

					PocColorMod.bannerClanCache.TryAdd(bannerStr, playerClanName);
					clan = playerClanName;
					if (playerKingdomName is object)
					{
						PocColorMod.bannerKingdomCache.TryAdd(bannerStr, playerKingdomName);
					}
				}
			}
			catch (Exception e)
			{
				Log.write(e.Message);
			}
		}


		static private string getIconFromConfig(ref string bannerStr, string unitName, bool isHero)
		{
			string shield;
			string a, b, c;
			bool success, successBG;

			bool isPlayer = (unitName == Hero.MainHero.Name.ToString());

			string clan = PocColorMod.bannerClanCache[bannerStr] ?? "";
			string kingdom = PocColorMod.bannerKingdomCache[bannerStr] ?? "";

			string playerClanName = Clan.PlayerClan?.Name?.ToString();
			string playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();

			bool isPlayerKingdom = !string.IsNullOrEmpty(playerKingdomName) && kingdom == playerKingdomName && Clan.PlayerClan.IsKingdomFaction;
			bool isPlayerClan = (!string.IsNullOrEmpty(playerClanName) && clan == playerClanName) || (isPlayer && !string.IsNullOrEmpty(clan));

			(success, successBG, a, shield, b, c) = PocColorMod.config.GetClanConfig(kingdom, clan, isPlayerKingdom, isPlayerClan);
			if (shield is object) return shield;

			(a, shield, b, c) = PocColorMod.config.GetKingdomConfig(kingdom, isPlayerKingdom);
			if (shield is object) return shield;

			(int mode, uint? c1, uint? c2, string newBanner, string newShield) = getColorFromConfig(ref bannerStr, unitName, isHero);
			if (newShield is object) return newShield;

			return bannerStr;
		}

		static private (int, uint?, uint?, string, string) getColorFromConfig(ref Banner banner, ref string bannerStr, string unitName, bool isHero)
		{

			uint? color1 = null;
			uint? color2 = null;
			string newBanner = null;
			string newShield = null;
			int mode = 2;
			try
			{
				bool isPlayer = (unitName == Hero.MainHero.Name.ToString());

				string clan = PocColorMod.bannerClanCache[bannerStr] ?? "";
				string kingdom = PocColorMod.bannerKingdomCache[bannerStr] ?? "";

				Kingdom myKingdom = null;
				for (int i = 0; i < Campaign.Current.Kingdoms.Count; i++)
				{
					myKingdom = Campaign.Current.Kingdoms[i].Name.ToString() == kingdom ? Campaign.Current.Kingdoms[i] : null;
					if (myKingdom is object) break;
				}

				bool isClan(Clan c) { return c.Name.ToString() == clan; }
				Clan myclan = Clan.FindFirst(isClan);

				bool isKing = false;
				bool isLeader = false;

				if (unitName is object) isKing = (myKingdom?.Leader?.Name?.ToString() == unitName);
				if (unitName is object) isLeader = (myclan?.Leader?.Name.ToString() == unitName);

				string playerClanName = Clan.PlayerClan?.Name?.ToString();
				string playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();


				if (isPlayer && clan != playerClanName && !string.IsNullOrEmpty(playerClanName) && !string.IsNullOrEmpty(clan))
				{
					//Wait a minute! this should be playerClan: how come it has not been found? Ok! the banner or clan name has changed: lets update them
					//Log.write("> player banner or clan name has changed, updating cache...");

					string cl;
					string kd;

					//Try to remove any existing entry
					PocColorMod.bannerClanCache.TryRemove(bannerStr, out cl);
					PocColorMod.bannerKingdomCache.TryRemove(bannerStr, out kd);
					//Add the new entries 

					PocColorMod.bannerClanCache.TryAdd(bannerStr, playerClanName);
					clan = playerClanName;
					if (playerKingdomName is object)
					{
						PocColorMod.bannerKingdomCache.TryAdd(bannerStr, playerKingdomName);
						kingdom = playerKingdomName;
					}

				}

				bool isPlayerClan = (!string.IsNullOrEmpty(playerClanName) && clan == playerClanName) || (isPlayer && !string.IsNullOrEmpty(clan));
				bool isPlayerKingdom = !string.IsNullOrEmpty(playerKingdomName) && kingdom == playerKingdomName && Clan.PlayerClan.IsKingdomFaction;

				if (PocColorMod.doLog) Log.write("==> character is: [" + unitName + "] of clan [" + clan + "] of kingdom [" + kingdom + "] isPlayerKingdom [" + isPlayerKingdom + "] isPlayerClan [" + isPlayerClan + "] isPlayer [" + isPlayer + "] isKing [" + isKing + "] isLeader [" + isLeader + "] isHero [" + isHero + "]");

				(int mode2, string[] colors, string[] colors2, string[] banners, string[] shields ) = PocColorMod.config.GetConfig(kingdom, clan, unitName, isPlayerKingdom, isPlayerClan, isPlayer, isKing, isLeader, isHero);
				mode = mode2;
				
				string colorsstr = colors is object ? string.Join(",", colors) : "";
				string colors2str = colors2 is object ? string.Join(",", colors2) : "";
				string shieldsStr = shields is object ? string.Join(",", shields) : "";
				string bannersStr = banners is object ? string.Join(",", banners) : "";

				if (PocColorMod.doLog) Log.write("> Applying colors according to: mode[" + mode + "] colors[" + colorsstr + "] colors2[" + colors2str + "] newBanner[" + bannersStr + "] shields[" + shieldsStr + "]");

				if (banners != null && banners.Length > 0)
				{
					//IF SHIELD NOT DEFINED, WE USED BANNER
					newBanner = getRandomBannerFromBanners(banners);
					bannerStr = newBanner;
					banner = new Banner(newBanner);

					//A new banner in cache: otherwise the kingdom and clan of the unit can not be found
					PocColorMod.bannerClanCache.TryAdd(newBanner, clan);
					PocColorMod.bannerKingdomCache.TryAdd(newBanner, kingdom);
				}

				//BANNER LINKED: banner colors are used
				if (mode == BANNER_LINKED && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 1)
				{
					int colorId = banner.BannerDataList[0].ColorId;
					int colorId2 = banner.BannerDataList[1].ColorId;

					//Log.write("Banner Colors: " + colorId.ToString() + "[ " + BannerManager.GetColor(colorId) + "] , " + colorId2.ToString() + "[" + BannerManager.GetColor(colorId2) + "]");

					color1 = BannerManager.GetColor(colorId);
					color2 = BannerManager.GetColor(colorId2);
				} else if (mode == BANNER_LINKED_RANDOM && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 1)
				{
					int colorId = banner.BannerDataList[0].ColorId;
					int colorId2 = banner.BannerDataList[1].ColorId;

					string[] bannerColors = new string[2] { colorId.ToString(), colorId2.ToString() };
					string[] bannerColorsAlt = new string[2] { colorId2.ToString(), colorId.ToString() };
					//Log.write("Banner Colors: " + colorId.ToString() + ", " + colorId2.ToString());

					(color1, color2) = getRandomPairColorsFromColors(bannerColors, bannerColorsAlt);

				} else if (mode == BANNER_LINKED_RANDOM_FULL && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 0)
				{
					HashSet<string> bannerColors = new HashSet<string>();
					for (int i = 0; i < banner.BannerDataList.Count; i++)
					{
						bannerColors.Add(banner.BannerDataList[i].ColorId.ToString());
					}
					color1 = getRandomColorFromColors(bannerColors.ToArray());
					color2 = getRandomColorFromColors(bannerColors.ToArray());
				}

				if (shields is object && shields.Length > 0)
				{
					//DEFAULT IS SHIELD
					newShield = getRandomBannerFromBanners(shields);
					bannerStr = newShield;
					banner = new Banner(newShield);

					//Log.write("set new shield:" + bannerStr);

					//A new shields in cache: otherwise the kingdom and clan of the unit can not be found
					PocColorMod.bannerClanCache.TryAdd(newShield, clan);
					PocColorMod.bannerKingdomCache.TryAdd(newShield, kingdom);

					//A new banner in cache: otherwise the kingdom and clan of the unit can not be found	
				}

				//SHIELD LINKED: shield colors are used
				if (mode == SHIELD_LINKED && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 1)
				{
					int colorId = banner.BannerDataList[0].ColorId;
					int colorId2 = banner.BannerDataList[1].ColorId;

					//Log.write("Banner Colors: " + colorId.ToString() + "[ " + BannerManager.GetColor(colorId) + "] , " + colorId2.ToString() + "[" + BannerManager.GetColor(colorId2) + "]");

					color1 = BannerManager.GetColor(colorId);
					color2 = BannerManager.GetColor(colorId2);
				}
				else if (mode == RANDOMIZED)
				{
					if ((colors is null || colors.IsEmpty()) && (colors2 is null || colors2.IsEmpty()))
					{
						color1 = getRandomColor();
						color2 = getRandomColor();
					}
					else
					{
						if (colors is null)
						{
							colors = colors2;
						}
						if (colors2 is null)
						{
							colors2 = colors;
						}

						color1 = getRandomColorFromColors(colors);
						colors2 = colors2 is null ? colors : colors2;
						color2 = getRandomColorFromColors(colors2);
					}
				}
				else if (mode == RANDOMIZED_LINKED)
				{
					if ((colors is null || colors.IsEmpty()) && (colors2 is null || colors2.IsEmpty()))
					{
						color1 = getRandomColor();
						color2 = color1;
					}
					else
					{
						if (colors is null)
						{
							colors = colors2;
						}
						if (colors2 is null)
						{
							colors2 = colors;
						}
						(color1, color2) = getRandomPairColorsFromColors(colors, colors2);
					}

				}
				else if (mode == OVERRIDE)
				{
					color1 = parseColor(colors[0]);
					color2 = parseColor(colors[1]);
				}
				else if (mode == SHIELD_LINKED_RANDOM && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 1)
				{
					int colorId = banner.BannerDataList[0].ColorId;
					int colorId2 = banner.BannerDataList[1].ColorId;

					string[] bannerColors = new string[2] { colorId.ToString(), colorId2.ToString() };
					string[] bannerColorsAlt = new string[2] { colorId2.ToString(), colorId.ToString() };
					//Log.write("Banner Colors: " + colorId.ToString() + ", " + colorId2.ToString());

					(color1, color2) = getRandomPairColorsFromColors(bannerColors, bannerColorsAlt);

				}
				else if (mode == SHIELD_LINKED_RANDOM_FULL && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 0)
				{
					HashSet<string> bannerColors = new HashSet<string>();
					for (int i = 0; i < banner.BannerDataList.Count; i++)
					{
						bannerColors.Add(banner.BannerDataList[i].ColorId.ToString());
					}
					color1 = getRandomColorFromColors(bannerColors.ToArray());
					color2 = getRandomColorFromColors(bannerColors.ToArray());
				}
			}
			catch (Exception e)
			{
				Log.write(e.Message);
			}
			return (mode, color1, color2, newBanner, newShield);
		}

		static private (int, uint?, uint?, string, string) getBattleColorFromConfig(ref Banner banner, ref string bannerStr, string unitName, bool isHero)
		{

			uint? color1 = null;
			uint? color2 = null;
			string newBanner = null;
			string newShield = null;
			int mode = 2;
			try
			{
				bool isPlayer = (unitName == Hero.MainHero.Name.ToString());

				string clan = PocColorMod.bannerClanCache[bannerStr] ?? "";
				string kingdom = PocColorMod.bannerKingdomCache[bannerStr] ?? "";

				Kingdom myKingdom = null;
				for (int i = 0; i < Campaign.Current.Kingdoms.Count; i++)
				{
					myKingdom = Campaign.Current.Kingdoms[i].Name.ToString() == kingdom ? Campaign.Current.Kingdoms[i] : null;
					if (myKingdom is object) break;
				}

				bool isClan(Clan c) { return c.Name.ToString() == clan; }
				Clan myclan = Clan.FindFirst(isClan);

				bool isKing = false;
				bool isLeader = false;

				if (unitName is object) isKing = (myKingdom?.Leader?.Name?.ToString() == unitName);
				if (unitName is object) isLeader = (myclan?.Leader?.Name.ToString() == unitName);

				string playerClanName = Clan.PlayerClan?.Name?.ToString();
				string playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();


				if (isPlayer && clan != playerClanName && !string.IsNullOrEmpty(playerClanName) && !string.IsNullOrEmpty(clan))
				{
					//Wait a minute! this should be playerClan: how come it has not been found? Ok! the banner or clan name has changed: lets update them
					//Log.write("> player banner or clan name has changed, updating cache...");

					string cl;
					string kd;

					//Try to remove any existing entry
					PocColorMod.bannerClanCache.TryRemove(bannerStr, out cl);
					PocColorMod.bannerKingdomCache.TryRemove(bannerStr, out kd);
					//Add the new entries 

					PocColorMod.bannerClanCache.TryAdd(bannerStr, playerClanName);
					clan = playerClanName;
					if (playerKingdomName is object)
					{
						PocColorMod.bannerKingdomCache.TryAdd(bannerStr, playerKingdomName);
						kingdom = playerKingdomName;
					}

				}

				bool isPlayerClan = (!string.IsNullOrEmpty(playerClanName) && clan == playerClanName) || (isPlayer && !string.IsNullOrEmpty(clan));
				bool isPlayerKingdom = !string.IsNullOrEmpty(playerKingdomName) && kingdom == playerKingdomName && Clan.PlayerClan.IsKingdomFaction;

				if (PocColorMod.doLog) Log.write("==> character is: [" + unitName + "] of clan [" + clan + "] of kingdom [" + kingdom + "] isPlayerKingdom [" + isPlayerKingdom + "] isPlayerClan [" + isPlayerClan + "] isPlayer [" + isPlayer + "] isKing [" + isKing + "] isLeader [" + isLeader + "] isHero [" + isHero + "]");

				(int mode2, string[] colors, string[] colors2, string[] banners, string[] shields) = PocColorMod.config.GetBattleConfig(kingdom, clan, unitName, isPlayerKingdom, isPlayerClan, isPlayer, isKing, isLeader, isHero);
				
				mode = mode2;
				string colorsstr = colors is object ? string.Join(",", colors) : "";
				string colors2str = colors2 is object ? string.Join(",", colors2) : "";
				string shieldsStr = shields is object ? string.Join(",", shields) : "";
				string bannersStr = banners is object ? string.Join(",", banners) : "";

				if (PocColorMod.doLog) Log.write("> Applying colors according to: mode[" + mode + "] colors[" + colorsstr + "] colors2[" + colors2str + "] newBanner[" + bannersStr + "] shields[" + shieldsStr + "]");

				if (banners != null && banners.Length > 0)
				{
					//IF SHIELD NOT DEFINED, WE USED BANNER
					newBanner = getRandomBannerFromBanners(banners);
					bannerStr = newBanner;
					banner = new Banner(newBanner);

					//A new banner in cache: otherwise the kingdom and clan of the unit can not be found
					PocColorMod.bannerClanCache.TryAdd(newBanner, clan);
					PocColorMod.bannerKingdomCache.TryAdd(newBanner, kingdom);
				}

				//BANNER LINKED: banner colors are used
				if (mode == BANNER_LINKED && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 1)
				{
					int colorId = banner.BannerDataList[0].ColorId;
					int colorId2 = banner.BannerDataList[1].ColorId;

					//Log.write("Banner Colors: " + colorId.ToString() + "[ " + BannerManager.GetColor(colorId) + "] , " + colorId2.ToString() + "[" + BannerManager.GetColor(colorId2) + "]");

					color1 = BannerManager.GetColor(colorId);
					color2 = BannerManager.GetColor(colorId2);
				}
				else if (mode == BANNER_LINKED_RANDOM && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 1)
				{
					int colorId = banner.BannerDataList[0].ColorId;
					int colorId2 = banner.BannerDataList[1].ColorId;

					string[] bannerColors = new string[2] { colorId.ToString(), colorId2.ToString() };
					string[] bannerColorsAlt = new string[2] { colorId2.ToString(), colorId.ToString() };
					//Log.write("Banner Colors: " + colorId.ToString() + ", " + colorId2.ToString());

					(color1, color2) = getRandomPairColorsFromColors(bannerColors, bannerColorsAlt);

				}
				else if (mode == BANNER_LINKED_RANDOM_FULL && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 0)
				{
					HashSet<string> bannerColors = new HashSet<string>();
					for (int i = 0; i < banner.BannerDataList.Count; i++)
					{
						bannerColors.Add(banner.BannerDataList[i].ColorId.ToString());
					}
					color1 = getRandomColorFromColors(bannerColors.ToArray());
					color2 = getRandomColorFromColors(bannerColors.ToArray());
				}

				if (shields is object && shields.Length > 0)
				{
					//DEFAULT IS SHIELD
					newShield = getRandomBannerFromBanners(shields);
					bannerStr = newShield;
					banner = new Banner(newShield);

					//Log.write("set new shield:" + bannerStr);

					//A new shields in cache: otherwise the kingdom and clan of the unit can not be found
					PocColorMod.bannerClanCache.TryAdd(newShield, clan);
					PocColorMod.bannerKingdomCache.TryAdd(newShield, kingdom);

					//A new banner in cache: otherwise the kingdom and clan of the unit can not be found	
				}

				//SHIELD LINKED: shield colors are used
				if (mode == SHIELD_LINKED && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 1)
				{
					int colorId = banner.BannerDataList[0].ColorId;
					int colorId2 = banner.BannerDataList[1].ColorId;

					//Log.write("Banner Colors: " + colorId.ToString() + "[ " + BannerManager.GetColor(colorId) + "] , " + colorId2.ToString() + "[" + BannerManager.GetColor(colorId2) + "]");

					color1 = BannerManager.GetColor(colorId);
					color2 = BannerManager.GetColor(colorId2);
				}
				else if (mode == RANDOMIZED)
				{
					if ((colors is null || colors.IsEmpty()) && (colors2 is null || colors2.IsEmpty()))
					{
						color1 = getRandomColor();
						color2 = getRandomColor();
					}
					else
					{
						if (colors is null)
						{
							colors = colors2;
						}
						if (colors2 is null)
						{
							colors2 = colors;
						}

						color1 = getRandomColorFromColors(colors);
						colors2 = colors2 is null ? colors : colors2;
						color2 = getRandomColorFromColors(colors2);
					}
				}
				else if (mode == RANDOMIZED_LINKED)
				{
					if ((colors is null || colors.IsEmpty()) && (colors2 is null || colors2.IsEmpty()))
					{
						color1 = getRandomColor();
						color2 = color1;
					}
					else
					{
						if (colors is null)
						{
							colors = colors2;
						}
						if (colors2 is null)
						{
							colors2 = colors;
						}
						(color1, color2) = getRandomPairColorsFromColors(colors, colors2);
					}

				}
				else if (mode == OVERRIDE)
				{
					color1 = parseColor(colors[0]);
					color2 = parseColor(colors[1]);
				}
				else if (mode == SHIELD_LINKED_RANDOM && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 1)
				{
					int colorId = banner.BannerDataList[0].ColorId;
					int colorId2 = banner.BannerDataList[1].ColorId;

					string[] bannerColors = new string[2] { colorId.ToString(), colorId2.ToString() };
					string[] bannerColorsAlt = new string[2] { colorId2.ToString(), colorId.ToString() };
					//Log.write("Banner Colors: " + colorId.ToString() + ", " + colorId2.ToString());

					(color1, color2) = getRandomPairColorsFromColors(bannerColors, bannerColorsAlt);

				}
				else if (mode == SHIELD_LINKED_RANDOM_FULL && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 0)
				{
					HashSet<string> bannerColors = new HashSet<string>();
					for (int i = 0; i < banner.BannerDataList.Count; i++)
					{
						bannerColors.Add(banner.BannerDataList[i].ColorId.ToString());
					}
					color1 = getRandomColorFromColors(bannerColors.ToArray());
					color2 = getRandomColorFromColors(bannerColors.ToArray());
				}
			}
			catch (Exception e)
			{
				Log.write(e.Message);
			}
			return (mode, color1, color2, newBanner, newShield);
		}

		static public bool updateAgentBuildDataColors(ref AgentBuildData __instance, ref uint color1, ref uint color2, bool first)
		{

			//synchro color with banner if banner is set: to prevent color override
			//Si on est en mode bannière, peut-être qu'à cette étape on peut ignorer la mise à jour de la couleur (en faisant un return false: si la bannière a été settée, on assume que la couleur est correcte)

			BasicCharacterObject character = __instance.AgentCharacter;
			Banner banner = __instance.AgentBanner;

			uint? c1, c2;
			string newbanner, newshield;
			int mode = 2;
			
			try
			{
				if (character is object)
				{
					//Integer extraColor = extraColorMap[character.StringId];
					//if (extraColor is object)
					//{
					//	//Color has already been defined by previous call
					//	if (first) color1 = extraColor.value;
					//	else color2 = extraColor.value;

					//	extraColorMap.TryRemove(character.StringId, out extraColor);
					//	return true;
					//}

					if (banner is object)
					{
						(mode, c1, c2, newbanner, newshield) = getColorFromConfig(ref banner, character.GetName().ToString(), character.IsHero );
						//Log.write("character [" + character.StringId + "] colors updated");

						if (c1 is object && c2 is object)
						{
							color1 = c1.Value;
							color2 = c2.Value;
							
							if (first) extraColorMap.TryAdd(character.StringId, new Integer(color2));
							else extraColorMap.TryAdd(character.StringId, new Integer(color1));

							//UPDATE ALL COLORS AND BANNER (so they match)
							//Log.write("===> Update Agent colors and Shield: " + color1 + " " + color2);

							Traverse.Create(__instance).Field("AgentBanner").SetValue(banner);
							__instance.AgentData.ClothingColor1(color1);
							__instance.AgentData.ClothingColor2(color2);
						}
					}
					else
					{
						IAgentOriginBase origin = __instance.AgentOrigin;
						if (origin is object)
						{
							banner = origin.Banner;
							if (banner is object)
							{
								(mode, c1, c2, newbanner, newshield) = getColorFromConfig(ref banner, character.GetName().ToString(), character.IsHero );
								//Log.write("character [" + character.StringId + "] colors updated");

								if (c1 is object && c2 is object)
								{
									color1 = c1.Value;
									color2 = c2.Value;

									//if (first) extraColorMap.TryAdd(character.StringId, new Integer(color2));
									//else extraColorMap.TryAdd(character.StringId, new Integer(color1));

									//UPDATE ALL COLORS AND BANNER (so they match)
									//Log.write("===> Update Agent colors and Shield: " + color1 + " " + color2 );

									Traverse.Create(__instance).Field("AgentBanner").SetValue(banner);
									__instance.AgentData.ClothingColor1(color1);
									__instance.AgentData.ClothingColor2(color2);

								}
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.write("Error caught:" + e.Message);
			}
			return true;
		}


		/*
		 * **********************************
		 * THIS IS 3D CHARACTER IN BATTLE
		 * **********************************
		 */
		[HarmonyPatch(typeof(AgentBuildData), "Banner")]
		internal class PocColorModColorsFromBanner
		{
			public static bool Prefix(ref AgentBuildData __instance, ref Banner banner)
			{
				try
				{
					//Log.write("AgentBuildData: Banner");

					//On Banner update, colors might have to be updated
					BasicCharacterObject character = __instance.AgentCharacter;

					string charName = character?.GetName()?.ToString();
					
					if (banner is object)
					{
						(int mode, uint? c1, uint? c2, string newbanner, string newshield) = getBattleColorFromConfig(ref banner, charName, character.IsHero);
						if (c1 is object && c2 is object)
						{
							//Log.write("===> Update Agent colors and Shield: " + c1 + " " + c2);
							__instance.AgentData.ClothingColor1(c1.Value);
							__instance.AgentData.ClothingColor2(c2.Value);
						}

					}
				}
				catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
				return true;
			}
		}

		/*
		 * **********************************
		 * THIS IS 3D CHARACTER IN BATTLE
		 * **********************************
		 */
		[HarmonyPatch(typeof(AgentBuildData), "ClothingColor1")]
		internal class PocColorModColor1
		{
			public static bool Prefix(ref AgentBuildData __instance, ref uint color, ref AgentBuildData __result)
			{
				try
				{
					//Log.write("AgentBuildData: ClothingColor1");

					BasicCharacterObject character = __instance.AgentCharacter;
					Banner banner = __instance.AgentBanner;
					if (character is object && banner is object)
					{
						//Banner already set, ignore to prevent override
						__result = __instance;
						return false;
					}

					uint color2 = __instance.AgentClothingColor2;
					updateAgentBuildDataColors(ref __instance, ref color, ref color2, true);
					
					//Log.write("===> Update Agent color1: " + color);

				}
				catch (Exception e)
				{
					Log.write(e.Message);
				}
				return true;
			}
		}


		/*
		 * **********************************
		 * THIS IS 3D CHARACTER IN BATTLE
		 * **********************************
		 */
		[HarmonyPatch(typeof(AgentBuildData), "ClothingColor2")]
		internal class PocColorModColor2
		{
			public static bool Prefix(ref AgentBuildData __instance, ref uint color, ref AgentBuildData __result)
			{
				try
				{
					//Log.write("AgentBuildData: ClothingColor2");

					BasicCharacterObject character = __instance.AgentCharacter;
					Banner banner = __instance.AgentBanner;
					if (character is object && banner is object)
					{
						//Banner already set, ignore to prevent override
						__result = __instance;
						return false;
					}

					uint color1 = __instance.AgentClothingColor1;
					updateAgentBuildDataColors(ref __instance, ref color1, ref color, false);

					//Log.write("===>  Update Agent color2: " + color);

				}
				catch (Exception e)
				{
					Log.write(e.Message);
				}
				return true;
			}
		}


		[HarmonyPatch(typeof(CharacterViewModel), "ArmorColor1", MethodType.Setter)]
		internal class PocColorModVMColor1
		{
			public static bool Prefix(CharacterViewModel __instance, ref uint value)
			{

				
				try
				{
					//Log.write("CharacterViewModel c1");
					CharInfo character = unitNames[__instance.CharStringId];
					//Log.write("CharacterViewModel ArmorColor1 [" + character?.name + "]");

					if (__instance.BannerCodeText is object)
					{
						string bannerStr = __instance.BannerCodeText;

						//update color only if we have a banner defined
						(int mode, uint? c1, uint? c2,string newbanner, string newshiel) = getColorFromConfig(ref bannerStr, character.name, character.isHero );

						if (c1 is object && c2 is object)
						{
							return false;
						}
					}
				}
				catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
				//Log.write("Update CharacterViewModel color1 " + value + " for char " + charName);
				return true;
			}
		}

		[HarmonyPatch(typeof(CharacterViewModel), "ArmorColor2", MethodType.Setter)]
		internal class PocColorModVMColor2
		{
			public static bool Prefix(CharacterViewModel __instance, ref uint value)
			{
				//Log.write("CharacterViewModel c2");

				try
				{
					CharInfo character = unitNames[__instance.CharStringId];
					//Log.write("CharacterViewModel ArmorColor2 [" + character?.name + "]");

					if (__instance.BannerCodeText is object)
					{
						string bannerStr = __instance.BannerCodeText;
						//update color only if we have a banner defined
						(int mode, uint? c1, uint? c2, string newbanner, string newshiel) = getColorFromConfig(ref bannerStr, character.name, character.isHero );
					
						if (c1 is object && c2 is object)
						{
							return false;
						}
					
					}
				}
				catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
				//Log.write("Update CharacterViewModel color2: " + value + " for char " + charName);
				return true;
			}
		}

		[HarmonyPatch(typeof(CharacterViewModel), "FillFrom")]
		internal class PocColorModGetCharName
		{
			public static void Postfix(ref CharacterViewModel __instance, BasicCharacterObject character)
			{
				//Log.write("CharacterViewModel FillFrom");
				//We fetch the characterName when FillFrom is called
				try
				{
					//Log.write("CharacterViewModel FillFrom [" + character?.GetName().ToString() + "]");
					unitNames.TryAdd(__instance.CharStringId, new CharInfo(character.GetName().ToString(),character.IsHero));
				}
				catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
			}
		}

		/*
		 * ************************************************
		 * THIS IS PARTY SCREEN: ALL CHARACTERS
		 * ************************************************
		 */
		[HarmonyPatch(typeof(PartyVM), "RefreshCurrentCharacterInformation")]
		internal class PocColorModPartyVM
		{
			public static void Postfix(ref PartyVM __instance)
			{
				try {
					//Log.write("PartyVM RefreshCurrentCharacterInformation");
					CharInfo character = null;
					
					string charName = __instance.CurrentCharacter?.Character?.Name?.ToString();
					bool isHero = __instance.CurrentCharacter?.Character.IsHero ?? false;

					if (!(charName is object)) {
						character = unitNames[__instance.SelectedCharacter.CharStringId];
						charName = character.name;
						isHero = character.isHero;
					}
					//Log.write("PartyVM RefreshCurrentCharacterInformation [" + charName + "]");

					string bannerStr = __instance.SelectedCharacter?.BannerCodeText;
					
					if (bannerStr is object)
					{
						Banner banner = new Banner(bannerStr);

						//updateCache(bannerStr, charName);

						(int mode, uint? c1, uint? c2, string newbanner, string newshield) = getColorFromConfig(ref banner, ref bannerStr, charName, isHero);
						if (c1 is object && c2 is object)
						{
							//Log.write("===> Update Current Character colors and banner: " + c1 + " " + c2 + " " + bannerStr + " for char:" + charName);
						
							__instance.SelectedCharacter.BannerCodeText = bannerStr;
							
							Traverse.Create(__instance.SelectedCharacter).Field("_armorColor1").SetValue(c1);
							Traverse.Create(__instance.SelectedCharacter).Field("_armorColor2").SetValue(c2);
							__instance.SelectedCharacter.OnPropertyChanged(nameof(__instance.SelectedCharacter.ArmorColor1));
							__instance.SelectedCharacter.OnPropertyChanged(nameof(__instance.SelectedCharacter.ArmorColor2));
						}
					}
					
				}
				catch (Exception e)
				{
					Log.write(e.Message);
				}
			}
		}


		/*
		 * **********************************
		 * 3D HERO: party and screens
		 * **********************************
		 */
		[HarmonyPatch(typeof(HeroViewModel), "FillFrom")]
		internal class PocColorModHeroFillFrom
		{
			public static void Postfix(ref HeroViewModel __instance, ref Hero hero)
			{
			
				try
				{
					//Log.write("HeroViewModel FillFrom");
					CharInfo character = null;
					
					string charName = hero?.Name?.ToString();
					if (!(charName is object))
					{
						character = unitNames[__instance.CharStringId];
						charName = character.name;
					}

					string bannerStr = __instance?.BannerCodeText;
					//Log.write("HeroViewModel FillFrom [" + charName + "]");

					if (bannerStr is object)
					{

						Banner banner = new Banner(bannerStr);

						//updateCache(bannerStr, charName);

						(int mode, uint? c1, uint? c2, string newbanner, string newshield) = getColorFromConfig(ref banner, ref bannerStr, charName, true);
						if (c1 is object && c2 is object)
						{
							//Log.write("===> Update Current Character colors and banner: " + c1 + " " + c2 + " " + bannerStr + " for char:" + charName);

							__instance.BannerCodeText = bannerStr;

							Traverse.Create(__instance).Field("_armorColor1").SetValue(c1);
							Traverse.Create(__instance).Field("_armorColor2").SetValue(c2);
							__instance.OnPropertyChanged(nameof(__instance.ArmorColor1));
							__instance.OnPropertyChanged(nameof(__instance.ArmorColor2));
						}
					}
				}
				catch (Exception e)
				{
					Log.write(e.Message);
				}
			}
		}

		[HarmonyPatch(typeof(ClanLordItemVM), "UpdateProperties")]
		internal class PocColorModClanLordItemVM
		{
			public static void Postfix(ref ClanLordItemVM __instance)
			{
				try
				{
					//Log.write("ClanLordItemVM:");

					string bannerStr = __instance?.HeroModel?.BannerCodeText;
					if (bannerStr is object)
					{
						__instance.Banner_9 = new ImageIdentifierVM(BannerCode.CreateFrom(new Banner(bannerStr)), true);
					}
				}
				catch (Exception e)
				{
					Log.write(e.Message);
				}
			}
		}

		[HarmonyPatch(typeof(HeroVM))]
		[HarmonyPatch(MethodType.Constructor)]
		[HarmonyPatch(new Type[] { typeof(Hero) , typeof(bool) })]
		internal class PocColorModHeroVM
		{
			public static void Postfix(ref HeroVM __instance, Hero hero)
			{
				try
				{
					//Log.write("HeroVM:");

					Banner banner = hero?.ClanBanner;
					string charName = hero?.Name?.ToString();

					if (banner is object )
					{
						(int mode, uint? c1, uint? c2, string newBanner, string newShield) = getColorFromConfig(ref banner, charName, true);
						if (newShield is object)
						{
							banner = new Banner(newShield);
						}
						else if (newBanner is object) {
							banner = new Banner(newBanner);
						}
						__instance.ClanBanner = new ImageIdentifierVM(banner);
						__instance.ClanBanner_9 = new ImageIdentifierVM(BannerCode.CreateFrom(new Banner(banner)), true);

						if (c1 is object && c2 is object ) {
							CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(hero.CharacterObject, false);
							characterCode.Color1 = c1.Value;
							characterCode.Color2 = c2.Value;
							__instance.ImageIdentifier = new ImageIdentifierVM(characterCode);
						}
					}
				}
				catch (Exception e)
				{
					Log.write(e.Message);
				}
			}
		}

		/*
		 * **********************************
		 * 3D HERO: INVENTORY
         * **********************************
         */
		[HarmonyPatch(typeof(SPInventoryVM), "UpdateCurrentCharacterIfPossible")]
		internal class PocColorModSPInventoryVM
		{
			public static void Postfix(ref SPInventoryVM __instance)
			{

				try {
					//Log.write("SPInventoryVM UpdateCurrentCharacterIfPossible");
					CharInfo character = null;

					CharacterObject current = (CharacterObject) Traverse.Create(__instance).Field("_currentCharacter").GetValue();
					
					string charName = current?.Name?.ToString();
					bool isHero = current?.IsHero ?? false;

					if (!(charName is object))
					{
						character = unitNames[__instance.MainCharacter.CharStringId];
						charName = character.name;
						isHero = character.isHero;
					}

					string bannerStr = __instance.MainCharacter?.BannerCodeText;
					//Log.write("SPInventoryVM UpdateCurrentCharacterIfPossible [" + charName + "]");

					if (bannerStr is object)
					{
						Banner banner = new Banner(bannerStr);

						//updateCache(bannerStr, charName);

						(int mode, uint? c1, uint? c2, string newbanner, string newshield) = getColorFromConfig(ref banner, ref bannerStr, charName, isHero);
						if (c1 is object && c2 is object)
						{
							//Log.write("===> Update Current Character colors and banner: " + c1 + " " + c2 + " " + bannerStr + " for char:" + charName);

							__instance.MainCharacter.BannerCodeText = bannerStr;

							Traverse.Create(__instance.MainCharacter).Field("_armorColor1").SetValue(c1);
							Traverse.Create(__instance.MainCharacter).Field("_armorColor2").SetValue(c2);
							__instance.MainCharacter.OnPropertyChanged(nameof(__instance.MainCharacter.ArmorColor1));
							__instance.MainCharacter.OnPropertyChanged(nameof(__instance.MainCharacter.ArmorColor2));
						}
					}

				}
				catch (Exception e)
				{
					Log.write(e.Message);
				}
			}
		}


		/// <summary>
		/// 
		/// Peut-être faudrait-il surcharger ceci, pour mettre à jour les item dans les autres situations
		/// 
		/// EquipItemsFromSpawnEquipment()
		/// 
		/// </summary>
		/// 

		[HarmonyPatch(typeof(Agent), "EquipItemsFromSpawnEquipment")]
		internal class PocColorModAgentEquipItemsFromSpawnEquipment
		{
			public static bool Prefix(ref Agent __instance)
			{

				try
				{
					
					if (__instance?.Equipment is null)
					{
						return true;
					}
					
					MissionWeapon? weaponOpt = __instance?.Equipment[EquipmentIndex.ExtraWeaponSlot];
					
					if (weaponOpt is object && "mod_banner_1".Equals(weaponOpt?.Item?.ToString()) )
					{
						
						MissionWeapon weapon = (MissionWeapon) weaponOpt;

						Banner banner = weapon.Banner;
						string bannerStr = banner is object ? PocColorMod.SerializeBanner(banner) : null;

						if (bannerStr is object)
						{
							string clan = PocColorMod.bannerClanCache[bannerStr] ?? "";
							string kingdom = PocColorMod.bannerKingdomCache[bannerStr] ?? "";

							bool isClan(Clan c) { return c.Name.ToString() == clan; }
							Clan myclan = Clan.FindFirst(isClan);

							Banner newOverrideBanner = banner;
							Banner newOverrideShield = banner;

							if (myclan is Object)
							{
								newOverrideBanner = myclan.Banner;
							}
							// i can getConfig to fetch the corresponding banner and shields and see if i need to override
							
							//(string newbanner, string newshield) = getBannerFromConfig(bannerStr, __instance.Name);
							(int mode, uint? c1, uint? c2, string newbanner, string newshield) = getBattleColorFromConfig(ref bannerStr, __instance.Name, __instance.IsHero);

							if (newbanner is Object)
							{
								//If newBanner exists we override clan Banner with It
								newOverrideBanner = new Banner(newbanner);
							}
							if (newshield is Object)
							{
								//If newBanner exists we override clan Banner with It
								//	Log.write("update item with new shield:" + newshield);
								newOverrideShield = new Banner(newshield);
							}

							//Update all items with shield code
							for (EquipmentIndex index = EquipmentIndex.WeaponItemBeginSlot; index < EquipmentIndex.ExtraWeaponSlot; ++index)
							{
								__instance.Equipment[index] = new MissionWeapon(__instance.Equipment[index].Item, __instance.Equipment[index].ItemModifier, newOverrideShield);
							}
							
							FieldInfo fi = typeof(MissionWeapon).GetField("<Banner>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
							TypedReference reference = __makeref(weapon);
							fi.SetValueDirect(reference, newOverrideBanner);

							String item = weapon.Item.ToString();
							
							//Update banner with banner code
							__instance.Equipment[EquipmentIndex.ExtraWeaponSlot] = new MissionWeapon(MBObjectManager.Instance.GetObject<ItemObject>(item), (ItemModifier)null, newOverrideBanner);

							//Reset colors according to Banner
							if (c1 is object && c2 is object)
							{
								__instance.SetClothingColor1((uint)c1);
								__instance.SetClothingColor2((uint)c2);
							}
						}
					}
				}
				catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(Agent), "EquipWeaponWithNewEntity")]
		internal class PocColorModAgentEquipNewEntity
		{
			public static bool Prefix(ref Agent __instance, ref MissionWeapon weapon)
			{
				try
				{
					//Log.write("Agent EquipWeaponWithNewEntity");

					//Log.write("EquipWeaponWithNewEntity [" + thread.ManagedThreadId  + "]");

					//Check Item Type
					string name = weapon.GetModifiedItemName()?.ToString() ?? "";
					Banner banner = weapon.Banner;
					string bannerStr = banner is object ? PocColorMod.SerializeBanner(banner) : null;

					if (bannerStr is object)
					{

						string clan = PocColorMod.bannerClanCache[bannerStr] ?? "";
						string kingdom = PocColorMod.bannerKingdomCache[bannerStr] ?? "";

						bool isClan(Clan c) { return c.Name.ToString() == clan; }
						Clan myclan = Clan.FindFirst(isClan);

						Banner newOverrideBanner = banner;
						Banner newOverrideShield = banner;

						if (myclan is Object)
						{
							newOverrideBanner = myclan.Banner;
						}
						// i can getConfig to fetch the corresponding banner and shields and see if i need to override

						//(string newbanner, string newshield) = getBannerFromConfig(bannerStr, __instance.Name);
						(int mode, uint? c1, uint? c2, string newbanner, string newshield) = getBattleColorFromConfig(ref bannerStr, __instance.Name, __instance.IsHero);


						if (newbanner is Object)
						{
							//If newBanner exists we override clan Banner with It
							newOverrideBanner = new Banner(newbanner);
						}
						if (newshield is Object)
						{
							//If newBanner exists we override clan Banner with It
							//	Log.write("update item with new shield:" + newshield);
							newOverrideShield = new Banner(newshield);
						}

						if ( name.Equals("Campaign Banner Small") || name.Equals("Mod Banner 1") || name.Equals("Mod Banner 2") )
						{
							//Update all items with shield code
							for (EquipmentIndex index = EquipmentIndex.WeaponItemBeginSlot; index < EquipmentIndex.NumAllWeaponSlots; ++index)
							{
								__instance.Equipment[index] = new MissionWeapon(__instance.Equipment[index].Item, __instance.Equipment[index].ItemModifier, newOverrideShield);
							}

							FieldInfo fi = typeof(MissionWeapon).GetField("<Banner>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
							TypedReference reference = __makeref(weapon);
							fi.SetValueDirect(reference, newOverrideBanner);

							String item = weapon.Item.ToString();

							//Update banner with banner code
							weapon = new MissionWeapon(MBObjectManager.Instance.GetObject<ItemObject>(item), (ItemModifier)null, newOverrideBanner);

							//Reset colors according to Banner
							if (c1 is object && c2 is object)
							{
								__instance.SetClothingColor1((uint)c1);
								__instance.SetClothingColor2((uint)c2);
							}
						}
						else
						{
							//Update Shield
							FieldInfo fi = typeof(MissionWeapon).GetField("<Banner>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
							TypedReference reference = __makeref(weapon);
							fi.SetValueDirect(reference, newOverrideShield);

							//Update Banner
							for (EquipmentIndex index = EquipmentIndex.WeaponItemBeginSlot; index < EquipmentIndex.NumAllWeaponSlots; ++index)
							{
								//Edit banner for synchronization
								string itemName = __instance.Equipment[index].Item?.Name?.ToString();
								if ( itemName.Equals("Campaign Banner Small") || itemName.Equals("Mod Banner 1") || itemName.Equals("Mod Banner 2") ) {
									String item = __instance.Equipment[index].Item.ToString();
									__instance.Equipment[index] = new MissionWeapon(MBObjectManager.Instance.GetObject<ItemObject>(item), (ItemModifier)null, newOverrideBanner);
									break;
								}
							}

							//Reset colors according to Shield
							if (c1 is object && c2 is object)
							{
								__instance.SetClothingColor1((uint)c1);
								__instance.SetClothingColor2((uint)c2);
							}
							//Locate Banner
						}

						__instance.EquipItemsFromSpawnEquipment();
					}
				}
				catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
				return true;
			}
		}


		[HarmonyPatch(typeof(AgentVisuals), "AddSkinArmorWeaponMultiMeshesToEntity")]
		internal class PocColorModAgentVisualsAddMeshes
		{
			public static void Postfix(ref AgentVisuals __instance, uint teamColor1, uint teamColor2, bool needBatchedVersion)
			{
				try
				{
					Thread thread = Thread.CurrentThread;
					AgentVisualsData data = (AgentVisualsData)Traverse.Create(__instance).Field("_data").GetValue();
					Banner banner = data.BannerData;
					string bannerStr = banner is object ? PocColorMod.SerializeBanner(banner) : null;

					//Log.write("AgentVisuals AddSkinArmorWeaponMultiMeshesToEntity: " + data.CharacterObjectStringIdData );

					if (bannerStr != null)
					{
						string clan = PocColorMod.bannerClanCache[bannerStr] ?? "";
						string kingdom = PocColorMod.bannerKingdomCache[bannerStr] ?? "";
						
						bool isClan(Clan c) { return c.Name.ToString() == clan; }
						Clan myclan = Clan.FindFirst(isClan);

						Banner newOverrideBanner = banner;
						if (myclan is Object)
						{
							newOverrideBanner = myclan.Banner;
						}
						// i can getConfig to fetch the corresponding banner and shields and see if i need to override

						(int mode, uint? c1, uint? c2, string newbanner, string newshield) = getColorFromConfig(ref bannerStr, __instance.GetCharacterObjectID(), true);

						//(string newbanner, string newshield) = getBannerFromConfig(bannerStr, __instance.GetCharacterObjectID(), true);
												
						if (newbanner is Object)
						{
							//If newBanner exists we override clan Banner with It
							newOverrideBanner = new Banner(newbanner);
						}
						int hashCode = data.BodyPropertiesData.GetHashCode();
						for (int slotIndex = 0; slotIndex < 5; ++slotIndex)
						{
							EquipmentElement equipmentElement = data.EquipmentData[slotIndex];
							if (!equipmentElement.IsEmpty)
							{
								ItemObject primaryItem = equipmentElement.Item;

								//Check Item Type
								string name = primaryItem?.Name?.ToString() ?? "";
								equipmentElement = data.EquipmentData[slotIndex];
								ItemModifier itemModifier = equipmentElement.ItemModifier;
								
								MissionWeapon missionWeapon;

								if ( name.Equals("Campaign Banner Small") || name.Equals("Mod Banner 1") || name.Equals("Mod Banner 2") )
								{
									missionWeapon = new MissionWeapon(primaryItem, itemModifier, newOverrideBanner);
								}
								else
								{
									//Shield
									missionWeapon = new MissionWeapon(primaryItem, itemModifier, banner);
								}
								if (data.AddColorRandomnessData)
									missionWeapon.SetRandomGlossMultiplier(hashCode);
								WeaponData weaponData = missionWeapon.GetWeaponData(needBatchedVersion);
								WeaponData ammoWeaponData = missionWeapon.GetAmmoWeaponData(needBatchedVersion);
								data.AgentVisuals.AddWeaponToAgentEntity(slotIndex, in weaponData, missionWeapon.GetWeaponStatsData(), in ammoWeaponData, missionWeapon.GetAmmoWeaponStatsData(), data.GetCachedWeaponEntity((EquipmentIndex)slotIndex));
								weaponData.DeinitializeManagedPointers();
								ammoWeaponData.DeinitializeManagedPointers();
							}
						}

						//Reset colors according to Banner
						if (c1 is object && c2 is object)
						{
							data.ClothColor1((uint)c1);
							data.ClothColor2((uint)c2);
						}

						data.AgentVisuals.SetWieldedWeaponIndices(data.RightWieldedItemIndexData, data.LeftWieldedItemIndexData);
					}
				}
				catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
			}
		}

		[HarmonyPatch(typeof(AgentVisuals), "Create")]
		internal class PocColorModAgentVisualsCreate
		{
			public static bool Prefix(ref AgentVisualsData data, ref string name)
			{
				try
				{
					//Log.write("AgentVisuals Create:" + name);
					if (name.StartsWith("PartyIcon"))
					{
						string charname = name.Substring(10);
						data.CharacterObjectStringId(charname);
					}
				}
				catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(PartyVisual), "AddCharacterToPartyIcon")]
		internal class PocColorModPartyVisual
		{
			public static bool Prefix(CharacterObject characterObject, ref string bannerKey, ref uint teamColor1, ref uint teamColor2)
			{
				try
				{
					//There is a serialize in code that mess bannerKey: so we reset it correctly
					Banner banner = characterObject?.HeroObject?.ClanBanner;
					if (banner is object)
					{
						bannerKey = PocColorMod.SerializeBanner(banner);
						string charName = characterObject?.Name.ToString();
						updateCache(bannerKey, charName);

						//Log.write("PartyVisual AddCharacterToPartyIcon:" + charName);

						(int mode, uint? c1, uint? c2, string newbanner, string newshield) = getColorFromConfig(ref bannerKey, charName, characterObject.IsHero);

						if (newshield is object)
						{
							bannerKey = newshield;
						}
						else if (newbanner is object)
						{
							bannerKey = newbanner;
						}

						if (c1 is object && c2 is object)
						{
							teamColor1 = c1.Value;
							teamColor2 = c2.Value;
						}
					}
				}
				catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(PartyNameplateVM), "PartyBanner", MethodType.Setter )]
		internal class PocColorModPartyBanner
		{
			public static bool Prefix(ref PartyNameplateVM __instance, ref ImageIdentifierVM value )
			{
				//Log.write("update Party Banner:");

				if (__instance.Party.LeaderHero is object ) {

					string bannerKey = PocColorMod.SerializeBanner(__instance.Party.LeaderHero?.ClanBanner);
					string charName = __instance.Party.LeaderHero.Name.ToString();

					string newshield = getIconFromConfig(ref bannerKey, charName, true);
					value = new ImageIdentifierVM(BannerCode.CreateFrom(newshield), true);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(Clan), "UpdateBannerColorsAccordingToKingdom")]
		internal class PocColorModOverrideBanner
		{
			public static bool Prefix(ref Clan __instance, ref string __state)
			{
				try
				{
					if (PocColorMod.config is null)
					{
						return true;
					}

					__state = PocColorMod.SerializeBanner(__instance.Banner);
				}
				catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
				return true;
			}

			public static void Postfix(ref Clan __instance, string __state)
			{

				try
				{

					if (PocColorMod.config is null)
					{	
						return;
					}
					//Log.write("------------- update clan banner colors START -----------");
					//Log.write("- kingdom colors were applied to banner (depending on config they will be reset)");
					//Log.write("---- Clan is: " + __instance.Name.ToString() );
					//This function will restore the correct banner and/or colors if follow colors is false

					Kingdom kingdom = __instance.Kingdom;

					string playerClanName = Clan.PlayerClan?.Name?.ToString();
					string playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();

					bool isPlayerClan = (!string.IsNullOrEmpty(playerClanName) && __instance.Name?.ToString() == playerClanName);
					bool isPlayerKingdom = !string.IsNullOrEmpty(playerKingdomName) && kingdom?.Name?.ToString() == playerKingdomName && Clan.PlayerClan.IsKingdomFaction;

					(bool follow, bool followBG, string clanBanner, string clanShield, string primary, string secondary) = PocColorMod.config.GetClanConfig(kingdom?.Name?.ToString(), __instance.Name?.ToString(), isPlayerKingdom, isPlayerClan);

					//Log.write("follow: [" + follow + "]");
					//Log.write("clanBanner: [" + clanBanner + "]");
					//Log.write("primary: [" + primary + "]");
					//Log.write("secondary: [" + secondary + "]");

					uint color1 = __instance.Color;
					uint color2 = __instance.Color2;

					//Log.write("- clan color1: ["+ BannerManager.GetColorId(color1) +"]");
					//Log.write("- clan color2: ["+ BannerManager.GetColorId(color2) + "]");

					if (primary is object)
					{
						color1 = PocColorModSetColors.parseColor(primary);
						//Log.write("- clan color1 overriden with primaryColor config: [" + color1 + "]");
					}

					if (secondary is object)
					{
						color2 = PocColorModSetColors.parseColor(secondary);
						//Log.write("- clan color2 overriden with secondaryColor config: [" + color2 + "]");
					}

					if (!follow && !followBG && clanBanner is null)
					{
						//Log.write("- follow is false and banner is not set: reset banner to prior value");
						//reset clan color to the banner
						//Log.write("clanBanner: [" + __state + "]");
						Traverse.Create(__instance).Field("_banner").SetValue(new Banner(__state));
					}

					if (!follow && !followBG && clanBanner is object)
					{
						//Log.write("- follow is false and banner is set: setting clan banner to config clanBanner");

						//reset clanBanner
						Banner banner = new Banner(clanBanner);
						Traverse.Create(__instance).Field("_banner").SetValue(banner);
					
						if (primary is object)
						{
							//Apply color if set
							//Log.write("- config primaryColor is defined: updating banner primary color");
							__instance.Banner?.ChangePrimaryColor(color1);
						}
						if (secondary is object)
						{
							//Apply color if set
							//Log.write("- config secondaryColor is defined: updating banner icons colors");
							__instance.Banner?.ChangeIconColors(color2);
						}
					}

					if ( (follow || followBG) && clanBanner is object)
					{
						//reset clanBanner
						Banner banner = new Banner(clanBanner);
						Traverse.Create(__instance).Field("_banner").SetValue(banner);

						//Log.write("- follow is true and banner is set: resetting banner colors back to kingdom colors");
						//reset clan color to the banner
						__instance.Banner?.ChangePrimaryColor(kingdom.PrimaryBannerColor);
						if (!followBG) __instance.Banner?.ChangeIconColors(kingdom.SecondaryBannerColor);
					}
					
				if (__instance.Banner is object && PocColorMod.bannerClanCache is object)
				{
					//Update clan Banner in cache after update
					String bannerStr = PocColorMod.SerializeBanner(__instance.Banner);
					PocColorMod.bannerClanCache.TryAdd(bannerStr, __instance?.Name.ToString());
					if (kingdom is object)
					{
						PocColorMod.bannerKingdomCache.TryAdd(bannerStr, kingdom?.Name.ToString());
					}
				}
				//Log.write("> updated Clan banner:" + PocColorMod.SerializeBanner(__instance.Banner));
				//Log.write("------------- update clan banner colors END  -----------");
					
			}
			catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
			}
		}
		

		[HarmonyPatch(typeof(BannerEditorVM), "SetClanRelatedRules")]
		internal class BannerEditorVMSetClanRules
		{
			private static bool Prefix(BannerEditorVM __instance, ref bool canChangeBackgroundColor)
			{
					canChangeBackgroundColor = true;
					return true;
			}
		}

		[HarmonyPatch(typeof(MBBannerEditorGauntletScreen), "OnDone")]
		internal class BannerEditorGauntletScreen_OnDone
		{
			private static void Postfix(ref MBBannerEditorGauntletScreen __instance)
			{
				try
				{

					Clan playerClan = Traverse.Create((object)__instance).Field<Clan>("_clan").Value;

					Kingdom kingdom = playerClan.Kingdom;

					string playerClanName = Clan.PlayerClan?.Name?.ToString();
					string playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();

					bool isPlayerClan = (!string.IsNullOrEmpty(playerClanName) && playerClan.Name?.ToString() == playerClanName);
					bool isPlayerKingdom = !string.IsNullOrEmpty(playerKingdomName) && kingdom?.Name?.ToString() == playerKingdomName && Clan.PlayerClan.IsKingdomFaction;

					(bool follow, bool followBG, string clanBanner, string clanShield, string primary, string secondary) = PocColorMod.config.GetClanConfig(kingdom?.Name?.ToString(), playerClan.Name?.ToString(), isPlayerKingdom, isPlayerClan);
					//Log.write("> clan: [" + playerClan.Name?.ToString() + "] follow: " + follow + ", primary: " + primary + ", secondary:" + secondary + ", clanbanner:" + clanBanner );

					uint color1 = playerClan.Color;
					uint color2 = playerClan.Color2;

					//Log.write("- clan color1: ["+ color1 +"]");
					//Log.write("- clan color2: ["+ color2 + "]");

					if (clanBanner is object) {
						//override clanBanner if set
						Banner banner = new Banner(clanBanner);
						Traverse.Create(playerClan).Field("_banner").SetValue(banner);
					}

					if (primary is object)
					{
						//override primary
						color1 = PocColorModSetColors.parseColor(primary);
						playerClan.Banner?.ChangePrimaryColor(color1);
						//Log.write("- clan color1 overriden with primaryColor config: [" + color1 + "]");
					}
					if (secondary is object)
					{
						//override secondary
						color2 = PocColorModSetColors.parseColor(secondary);
						playerClan.Banner?.ChangeIconColors(color2);
						//Log.write("- clan color2 overriden with secondaryColor config: [" + color2 + "]");
					}
				}
				catch (Exception ex)
				{
					Log.write("Error while applying colors: " + ex.Message );
				}
			}
		}


		[HarmonyPatch(typeof(Banner), "Serialize")]
		internal class PocColorModFixBannerSerialize
		{
			public static bool Prefix(ref Banner __instance, ref string __result)
			{
				try
				{
					//Fix the serialization that mess with bannerText Code
					__result = PocColorMod.SerializeBanner(__instance);
					return false;
				}
				catch (Exception ex)
				{
					Log.write("Error caught while Serializing banner:" + ex.Message);
				}
				return true;
			}
		}


		[HarmonyPatch(typeof(Clan), "Banner", MethodType.Getter)]
		internal class PocColorModclanBanner
		{ 
			public static void Postfix(ref Clan __instance, ref Banner __result)
			{
				try
				{
					//Fix the serialization that mess with bannerText Code
					__result = (Banner)Traverse.Create(__instance).Field("_banner").GetValue();
				}
				catch (Exception ex)
				{
					Log.write("Error caught:" + ex.Message);
				}
			}
		}


	}
}