using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CombatAndDodgeOverhaul
{
	//Class to manually determine bosses
	static class EnemyClass
	{
		
		static string[] bossLocNames = new string[] {
			"name_unpc_GoldLich_01",
			"name_unpc_JadeLich_01",
			"Undead_Hivelord",
			"Undead_Hivelord2",
			"Golem_ForgeRedHot",
			"name_unpc_unknown_01",
			"EliteBeastGolem"
		};
		static string[] miniBossLocNames = new string[] {
			"Undead_GhostVanasse",
			"Golem_Basic2",
			"Undead_Skeleton2",
			"Wildlife_Manticore2",
			"Wildlife_Wendigo2",
			"Giant_Plate",
			"Wildlife_Wendigo",
			"Wildlife_Bug_Mantis",
			"Wildlife_Shrimp",
			"Wildlife_Bug_MantisMana",
			"Giant_Priest",
			"name_unpc_immaculate_01",
			"Horror_Immaculate",
			"Horror_Immaculate2",
			"Wildlife_Manticore",
			"Horror_Shell",
			"Wildlife_PearlBird2"


		};
		static string[] miniBossObjectNames = new string[]
		{
			"Alpha",
			"Manticore",
			"Mantis",
			"BoozuProudBeast",
			"ElementalParasite",
			"AshGiant",
			"HiveLord",
			"EliteMantisShrimp",
			"EliteCrescentShark",
			"Wendigo",
			"GolemShielded",
			"PureIlluminator",
			"SublimeShell",
			"Phytoflora",
			"Grotesque",
			"ForgeGolemRedHot"
		};
		public static string[] bossObjectNames = new string[]
		{
			"EliteAshGiantPriest",
			"EliteAshGiant",
			"GiantHorror",
			"NewBandit",
			"EliteBrandSquire",
			"NewBanditEquip_DesertBasic2_D",
			"ImmaculateButcher",
			"LichRust",
			"LichGold",
			"LichJade",
			"RoyalManticore",
			"EliteCalixa",
			"EliteBeastGolem",
			"EliteTuanosaurAlpha",
			"EliteSupremeShell",
			"EliteBurningMan",
			"EliteTroglodyteQueen",
			"TitanGolemHammer",
			"EliteObsidianElemental",


		};

		public static string[] weaklingObjectNames = new string[]
		{
		};
		public static string[] weaklingLocNames = new string[]
		{
			"Bandit_"
		};

		private static bool arrayContains(string[] stringArray, string stringToCheck)
		{
			for (int i=0;i<stringArray.Length;i++)
			{

				if (stringToCheck.IndexOf(stringArray[i]) >= 0)
				{
					return true;
				}
			}
			return false;
		}
		private static bool isBoss(String locName, String objectName)
		{
			return arrayContains(bossLocNames, locName) || arrayContains(bossObjectNames, objectName);
		}

		private static bool isMiniBoss(String locName, String objectName)
		{
			return arrayContains(miniBossLocNames, locName) || arrayContains(miniBossObjectNames, objectName);
		}

		private static bool isWeakling(String locName, String objectName)
		{
			return arrayContains(weaklingLocNames, locName) || arrayContains(weaklingObjectNames, objectName);
		}

		public static string getCleanName(Character character)
		{
			string cleanObjectName = character.gameObject.name;
			cleanObjectName = cleanObjectName.Substring(0, cleanObjectName.Length - (character.UID.ToString().Length + 1));
			cleanObjectName = Regex.Replace(cleanObjectName, @"\s*[(][\d][)]$", "");

			return cleanObjectName;
		}

		public static bool isPlayer(Character character) {
			return getCleanName(character).StartsWith("PlayerChar");
		}

		public static EnemyLevel getEnemyLevel(Character character)
		{

			string locName = (String)At.GetValue(typeof(Character), character, "m_nameLocKey");

			//thanks sinai
			string cleanObjectName = getCleanName(character);

			if (isPlayer(character))
			{
				return EnemyLevel.NORMAL;
			}

			EnemyLevel enemyLevel = EnemyLevel.NORMAL;

			if (isMiniBoss(locName, cleanObjectName))
			{
				enemyLevel = EnemyLevel.MINIBOSS;
			}

			if (isBoss(locName, cleanObjectName))
			{
				enemyLevel = EnemyLevel.BOSS;
			}

			if (isWeakling(locName, cleanObjectName))
			{
				enemyLevel = EnemyLevel.WEAKLING;
			}

			return enemyLevel;
		}

	}
}
