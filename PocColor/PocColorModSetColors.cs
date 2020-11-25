using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using System.Diagnostics;
using SandBox.View.Map;
using PocColor.Config;
using NUnit.Framework;
using TaleWorlds.ObjectSystem;
using System.Threading;
using System.Runtime.CompilerServices;
using SandBox.GauntletUI;


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
		public const int BANNER_LINKED = 2;
		public const int BANNER_LINKED_RANDOM = 21;
		public const int BANNER_LINKED_RANDOM_FULL = 22;
		public const int OVERRIDE = 3;

		public const int COLOR_COUNT = 158;

		public static Random rnd = new Random();

		//A map to store the units names allows resolving them from anywhere
		public static Map<string, string> unitNames = new Map<string, string>();
		public static Map<string, Integer> extraColorMap = new Map<string, Integer>();


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

		static private (uint?, uint?, string, string) getColorFromConfig(ref Banner banner, string unitName)
		{
			string bannerStr = banner is object ? PocColorMod.SerializeBanner(banner) : null;
			return getColorFromConfig(ref banner, ref bannerStr, unitName);
		}
		static private (uint?, uint?, string, string) getColorFromConfig(ref string bannerStr, string unitName)
		{
			Banner banner = bannerStr is object ? new Banner(bannerStr) : null;
			return getColorFromConfig(ref banner, ref bannerStr, unitName);
		}

		static private (string, string) getBannerFromConfig(string bannerStr, string unitName)
		{

			string newBanner = null;
			string newShield = null;

			try
			{
				//Log.write("Finding: " + unitName + ", " + bannerStr);

				bool isPlayer = (unitName == Hero.MainHero?.Name.ToString());

				string clan = PocColorMod.bannerClanCache[bannerStr] ?? "";

				string kingdom = PocColorMod.bannerKingdomCache[bannerStr] ?? "";

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

				Log.write("> character is: [" + unitName + "] of clan [" + clan + "] of kingdom [" + kingdom + "] isPlayerKingdom [" + isPlayerKingdom + "] isPlayerClan [" + isPlayerClan + "] isPlayer [" + isPlayer + "]");
				(int mode, string[] colors, string[] colors2, string[] banners, string[] shields ) = PocColorMod.config.GetConfig(kingdom, clan, unitName, isPlayerKingdom, isPlayerClan, isPlayer);
				
				string colorsstr = colors is object ? string.Join(",", colors) : "";
				string colors2str = colors2 is object ? string.Join(",", colors2) : "";
				string shieldsStr = shields is object ? string.Join(",", shields) : "";
				string bannersStr = banners is object ? string.Join(",", banners) : "";

				Log.write("==> Applying colors according to: mode[" + mode + "] colors[" + colorsstr + "] colors2[" + colors2str + "] newBanner[" + bannersStr + "] shields[" + shieldsStr + "]");

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


		static private (uint?, uint?, string, string) getColorFromConfig(ref Banner banner, ref string bannerStr, string unitName)
		{

			uint? color1 = null;
			uint? color2 = null;
			string newBanner = null;
			string newShield = null;

			try
			{
				bool isPlayer = (unitName == Hero.MainHero.Name.ToString());

				string clan = PocColorMod.bannerClanCache[bannerStr] ?? "";

				string kingdom = PocColorMod.bannerKingdomCache[bannerStr] ?? "";

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

				Log.write("> character is: [" + unitName + "] of clan [" + clan + "] of kingdom [" + kingdom + "] isPlayerKingdom [" + isPlayerKingdom + "] isPlayerClan [" + isPlayerClan + "] isPlayer [" + isPlayer + "]");

				(int mode, string[] colors, string[] colors2, string[] banners, string[] shields ) = PocColorMod.config.GetConfig(kingdom, clan, unitName, isPlayerKingdom, isPlayerClan, isPlayer);

				string colorsstr = colors is object ? string.Join(",", colors) : "";
				string colors2str = colors2 is object ? string.Join(",", colors2) : "";
				string shieldsStr = shields is object ? string.Join(",", shields) : "";
				string bannersStr = banners is object ? string.Join(",", banners) : "";

				Log.write("==> Applying colors according to: mode[" + mode + "] colors[" + colorsstr + "] colors2[" + colors2str + "] newBanner[" + bannersStr + "] shields[" + shieldsStr + "]");

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

				if (mode == BANNER_LINKED && banner is object && banner.BannerDataList is object && banner.BannerDataList.Count > 1)
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
			}
			catch (Exception e)
			{
				Log.write(e.Message);
			}
			return (color1, color2, newBanner, newShield);
		}

		static public bool updateAgentBuildDataColors(ref AgentBuildData __instance, ref uint color1, ref uint color2, bool first)
		{

			//synchro color with banner if banner is set: to prevent color override
			//Si on est en mode bannière, peut-être qu'à cette étape on peut ignorer la mise à jour de la couleur (en faisant un return false: si la bannière a été settée, on assume que la couleur est correcte)

			BasicCharacterObject character = __instance.AgentCharacter;
			Banner banner = __instance.AgentBanner;

			uint? c1, c2;
			string newbanner, newshield;
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
						(c1, c2, newbanner, newshield) = getColorFromConfig(ref banner, character.GetName().ToString());
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
								(c1, c2, newbanner, newshield) = getColorFromConfig(ref banner, character.GetName().ToString());
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

		[HarmonyPatch(typeof(AgentBuildData), "ClothingColor1")]
		internal class PocColorModColor1
		{
			public static bool Prefix(ref AgentBuildData __instance, ref uint color, ref AgentBuildData __result)
			{
				try { 
				
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

		[HarmonyPatch(typeof(AgentBuildData), "ClothingColor2")]
		internal class PocColorModColor2
		{
			public static bool Prefix(ref AgentBuildData __instance, ref uint color, ref AgentBuildData __result)
			{

				try
				{

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


		[HarmonyPatch(typeof(AgentBuildData), "Banner")]
		internal class PocColorModColorsFromBanner
		{
			public static bool Prefix(ref AgentBuildData __instance, ref Banner banner)
			{
				try
				{
					//On Banner update, colors might have to be updated
					BasicCharacterObject character = __instance.AgentCharacter;

					if (banner is object)
					{
						(uint? c1, uint? c2, string newbanner, string newshield) = getColorFromConfig(ref banner, character.GetName().ToString());
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

		[HarmonyPatch(typeof(CharacterViewModel), "ArmorColor1", MethodType.Setter)]
		internal class PocColorModVMColor1
		{
			public static bool Prefix(CharacterViewModel __instance, ref uint value)
			{
				string charName = unitNames[__instance.CharStringId];

				try
				{
					
					if (__instance.BannerCodeText is object)
					{
						string bannerStr = __instance.BannerCodeText;

						//update color only if we have a banner defined
						(uint? c1, uint? c2,string newbanner, string newshiel) = getColorFromConfig(ref bannerStr, charName);

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
				string charName = unitNames[__instance.CharStringId];

				try
				{	
					if (__instance.BannerCodeText is object)
					{
						string bannerStr = __instance.BannerCodeText;
						//update color only if we have a banner defined
						(uint? c1, uint? c2, string newbanner, string newshiel) = getColorFromConfig(ref bannerStr, charName);
					
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
				//Log.write("fillfrom CharacterViewModel: " + character.GetName().ToString());
				//We fetch the characterName when FillFrom is called
				unitNames.TryAdd(__instance.CharStringId, character.GetName().ToString());
			}
		}



		[HarmonyPatch(typeof(PartyVM), "RefreshCurrentCharacterInformation")]
		internal class PocColorModPartyVM
		{
			public static void Postfix(ref PartyVM __instance)
			{
				try { 

					string charName = __instance.CurrentCharacter?.Character?.Name?.ToString();
					if (!(charName is object)) {
						charName = unitNames[__instance.SelectedCharacter.CharStringId];
					}

					string bannerStr = __instance.SelectedCharacter?.BannerCodeText;
					//Log.write("PartyVM RefreshCurrentCharacterInformation [" + charName + "]");

					if (bannerStr is object)
					{
						Banner banner = new Banner(bannerStr);

						//updateCache(bannerStr, charName);

						(uint? c1, uint? c2, string newbanner, string newshield) = getColorFromConfig(ref banner, ref bannerStr, charName);
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

		[HarmonyPatch(typeof(HeroViewModel), "FillFrom")]
		internal class PocColorModHeroFillFrom
		{
			public static void Postfix(ref HeroViewModel __instance, Hero hero)
			{

				try
				{
					string charName = hero?.Name?.ToString();
					if (!(charName is object))
					{
						charName = unitNames[__instance.CharStringId];
					}

					string bannerStr = __instance?.BannerCodeText;
					//Log.write("HeroViewModel FillFrom [" + charName + "]");

					if (bannerStr is object)
					{
						Banner banner = new Banner(bannerStr);

						//updateCache(bannerStr, charName);

						(uint? c1, uint? c2, string newbanner, string newshield) = getColorFromConfig(ref banner, ref bannerStr, charName);
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


		[HarmonyPatch(typeof(SPInventoryVM), "UpdateCurrentCharacterIfPossible")]
		internal class PocColorModSPInventoryVM
		{
			public static void Postfix(ref SPInventoryVM __instance)
			{

				try { 

					CharacterObject current = (CharacterObject) Traverse.Create(__instance).Field("_currentCharacter").GetValue();

					string charName = current?.Name?.ToString();
					if (!(charName is object))
					{
						charName = unitNames[__instance.MainCharacter.CharStringId];
					}

					string bannerStr = __instance.MainCharacter?.BannerCodeText;
					//Log.write("SPInventoryVM UpdateCurrentCharacterIfPossible [" + charName + "]");

					if (bannerStr is object)
					{
						Banner banner = new Banner(bannerStr);

						//updateCache(bannerStr, charName);

						(uint? c1, uint? c2, string newbanner, string newshield) = getColorFromConfig(ref banner, ref bannerStr, charName);
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

		[HarmonyPatch(typeof(Hero), "ClanBanner", MethodType.Getter)]
		internal class PocColorModHero
		{
			public static void Postfix(ref Hero __instance, ref Banner __result)
			{
				try
				{
					string charName = __instance.Name?.ToString();
					string bannerKey = __result is object ? PocColorMod.SerializeBanner(__result) : null;

					//Log.write("--> get Clan Banner from Hero " + charName + " " + bannerKey);
					//Log.write(Environment.StackTrace);

					updateCache(bannerKey, charName);

					(string newbanner, string newshield) = getBannerFromConfig(bannerKey, charName);
					if (newshield is object)
					{
						//Log.write("===> update new shield for hero" + newshield);
						//Override Banner for this unit
						__result = new Banner(newshield);
											}
					else if (newbanner is object)
					{
						//Log.write("===> update new banner for hero" + newbanner);
						//Override Banner for this unit
						__result = new Banner(newbanner);
					}
				}
				catch (Exception e)
				{
					Log.write("Error caught:" + e.Message);
				}
			}

		}

		[HarmonyPatch(typeof(Agent), "EquipWeaponWithNewEntity")]
		internal class PocColorModAgentEquipNewEntity
		{
			public static bool Prefix(ref Agent __instance, ref MissionWeapon weapon)
			{
				try
				{
					Thread thread = Thread.CurrentThread;
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

						(string newbanner, string newshield) = getBannerFromConfig(bannerStr, __instance.Name);

						if (newbanner is Object)
						{
							//If newBanner exists we override clan Banner with It
							newOverrideBanner = new Banner(newbanner);
						}
						//if (newshield is Object)
						//{
							//If newBanner exists we override clan Banner with It
						//	Log.write("update item with new shield:" + newshield);
						//	newOverrideShield = new Banner(newshield);
						//}

						if (name.Equals("Campaign Banner Small"))
						{
							FieldInfo fi = typeof(MissionWeapon).GetField("<Banner>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
							TypedReference reference = __makeref(weapon);
							fi.SetValueDirect(reference, newOverrideBanner);

							weapon = new MissionWeapon(MBObjectManager.Instance.GetObject<ItemObject>("campaign_banner_small"), (ItemModifier)null, newOverrideBanner);
						}
						//else
						//{
							//Shield
						//	FieldInfo fi = typeof(MissionWeapon).GetField("<Banner>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
						//	TypedReference reference = __makeref(weapon);
						//	fi.SetValueDirect(reference, newOverrideShield);
						//}
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
						//Log.write("AddSkinArmorWeaponMultiMeshesToEntity");

						Thread thread = Thread.CurrentThread;
						AgentVisualsData data = (AgentVisualsData)Traverse.Create(__instance).Field("_data").GetValue();
						Banner banner = data.BannerData;
						string bannerStr = banner is object ? PocColorMod.SerializeBanner(banner) : null;

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

							(string newbanner, string newshield) = getBannerFromConfig(bannerStr, __instance.GetCharacterObjectID());

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

									if (name.Equals("Campaign Banner Small"))
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
							bannerKey = PocColorMod.SerializeBanner(banner);

						string charName = characterObject?.Name.ToString();

						//updateCache(bannerKey, charName);
						(uint? c1, uint? c2, string newbanner, string newshiel) = getColorFromConfig(ref bannerKey, charName);
						if (c1 is object && c2 is object)
						{
							teamColor1 = c1.Value;
							teamColor2 = c2.Value;
						}
					}
					catch (Exception e)
					{
						Log.write("Error caught:" + e.Message);
					}
					return true;
				}
			}

			[HarmonyPatch(typeof(Clan), "UpdateBannerColorsAccordingToKingdom")]
			internal class PocColorModOverrideBanner
			{

				public static void Postfix(ref Clan __instance)
				{

					try {

						//Log.write("------------- update clan banner colors START -----------");
						//Log.write("- kingdom colors were applied to banner (depending on config they will be reset)");

						if (__instance is null) {
							return;
						}

						//This function will restore the correct banner and/or colors if follow colors is false

						Kingdom kingdom = __instance.Kingdom;

						string playerClanName = Clan.PlayerClan?.Name?.ToString();
						string playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();

						bool isPlayerClan = (!string.IsNullOrEmpty(playerClanName) && __instance.Name?.ToString() == playerClanName);
						bool isPlayerKingdom = !string.IsNullOrEmpty(playerKingdomName) && kingdom?.Name?.ToString() == playerKingdomName && Clan.PlayerClan.IsKingdomFaction;


						if (PocColorMod.config is null) {
							//Log.write("- no config => do nothing");
							return;
						}

						(bool follow, string clanBanner, string primary, string secondary) = PocColorMod.config.GetClanConfig(kingdom?.Name?.ToString(), __instance.Name?.ToString(), isPlayerKingdom, isPlayerClan);

						uint color1 = __instance.Color;
						uint color2 = __instance.Color2;

						//Log.write("- clan color1: ["+ color1 +"]");
						//Log.write("- clan color2: ["+ color2 + "]");

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
					
						if (!follow && clanBanner is null)
						{
							//Log.write("- follow is false and banner is not set: resetting banner colors back to clan colors");
							//reset clan color to the banner
							__instance.Banner?.ChangePrimaryColor(color1);
							__instance.Banner?.ChangeIconColors(color2);
						}

						if (!follow && clanBanner is object)
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

						if (follow && clanBanner is object)
						{
							//Log.write("- follow is false and banner is not set: resetting banner colors back to clan colors");
							//reset clan color to the banner
							__instance.Banner?.ChangePrimaryColor(color1);
							__instance.Banner?.ChangeIconColors(color2);
						}


					//Log.write("------------- update clan banner colors END  -----------");

				}
					catch (Exception e)
					{
						Log.write(e.Message);
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

					if (PocColorMod.config is null)
					{
						Log.write("- no config => do nothing");
						return;
					}
					Clan playerClan = Traverse.Create((object)__instance).Field<Clan>("_clan").Value;

					Kingdom kingdom = playerClan.Kingdom;

					string playerClanName = Clan.PlayerClan?.Name?.ToString();
					string playerKingdomName = Clan.PlayerClan?.Kingdom?.Name?.ToString();

					bool isPlayerClan = (!string.IsNullOrEmpty(playerClanName) && playerClan.Name?.ToString() == playerClanName);
					bool isPlayerKingdom = !string.IsNullOrEmpty(playerKingdomName) && kingdom?.Name?.ToString() == playerKingdomName && Clan.PlayerClan.IsKingdomFaction;

					(bool follow, string clanBanner, string primary, string secondary) = PocColorMod.config.GetClanConfig(kingdom?.Name?.ToString(), playerClan.Name?.ToString(), isPlayerKingdom, isPlayerClan);
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
					Log.write("Error while applying colors");
				}
			}
		}


		[HarmonyPatch(typeof(Banner), "Serialize")]
			internal class PocColorModFixBannerSerialize
			{
				public static bool Prefix(ref Banner __instance, ref string __result)
				{
					//Fix the serialization that mess with bannerText Code
					__result = PocColorMod.SerializeBanner(__instance);
					return false;
				}
			}

	}
}