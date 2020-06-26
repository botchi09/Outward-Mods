using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace CombatAndDodgeOverhaul
{
	//Manages stamina usage
	public class StaminaUsagePatch
	{
		[HarmonyPatch(typeof(WeaponStats), "GetAttackStamCost")]
		public class WeaponStats_GetAttackStamCost
		{
			[HarmonyPostfix]
			static void Postfix(WeaponStats __instance, ref float __result)
			{
				float newStamCost = __result * ((float)CombatOverhaul.config.GetValue(Settings.Weapon_Stamina_Cost_Stat)*0.01f);
				__result = newStamCost;
			}
		}

		//Mostly code cloned from decompile
		/*[HarmonyPatch(typeof(Character), "HitStarted")]
		public class Character_HitStarted
		{
			[HarmonyPrefix]
			static bool Prefix(Character __instance, int _attackID)
			{
				Debug.Log("Called!");
				Action notify = (Action)At.GetValue(typeof(Character), __instance, "NotifyNearbyAIOfAttack");
				
				if (_attackID != -2)
				{
					if (__instance.SkillMeleeDetector != null)
					{
						__instance.SkillMeleeDetector.HitStarted(_attackID);
						notify.Invoke();
						return false;
					}
					if (__instance.CurrentWeapon != null)
					{
						if (_attackID != -1)
						{
							((CharacterStats)At.GetValue(typeof(Character), __instance, "m_characterStats")).UseStamina(null, __instance.CurrentWeapon.GetStamCost(_attackID), -10f);
						}
						__instance.CurrentWeapon.HitStarted(_attackID);
						notify.Invoke();
						return false;
					}
				}
				else if (__instance.LeftHandWeapon != null)
				{
					__instance.LeftHandWeapon.HitStarted(_attackID);
					notify.Invoke();
				}
				return false;
			}
		}*/

		//This doesn't work because i suck at reflection

		[HarmonyPatch(typeof(CharacterStats), "UseStamina", new Type[] { typeof(float), typeof(float) })]
		public class CharacterStats_UseStamina
		{
			[HarmonyPrefix]
			public static bool Prefix(CharacterStats __instance, ref float _staminaConsumed, Character ___m_character)
			{
				if ((bool)CombatOverhaul.config.GetValue(Settings.Stamina_Burn_Offset))
				{
					//staminaConsumed = total
					// get 1x value.  staminaconsumed divided by staminacost
					// staminaConsumed - 1x value = our chosen value
					//__result = 1x value

					Character m_character = (Character)At.GetValue(typeof(CharacterStats), __instance, "m_character");
					float m_timeOfLastStamUse = (float)At.GetValue(typeof(CharacterStats), __instance, "m_timeOfLastStamUse");
					float beforeMultStamina; //1x stamina (before setting applied)
					float staminaCostMult = 0.01f*(float)CombatOverhaul.config.GetValue(Settings.Weapon_Stamina_Cost_Stat);

					if (staminaCostMult >= 1f)
					{
						beforeMultStamina = _staminaConsumed / (staminaCostMult);
					}
					else
					{
						beforeMultStamina = _staminaConsumed * staminaCostMult;
					}

					float m_stamina = (float)At.GetValue(typeof(CharacterStats), __instance, "m_stamina");
					float totalStaminaUse = (_staminaConsumed) - (beforeMultStamina);
					if (m_character.IsPhotonPlayerLocal)
					{
						At.SetValue(m_stamina - totalStaminaUse, typeof(CharacterStats), __instance, "m_stamina");
						_staminaConsumed = beforeMultStamina;
					}
					
				}
				return true;
			}
		}
		
		/*[HarmonyPatch(typeof(Skill), "GetAttackStamCost")]
		public class Skill_HasEnoughStamina
		{
			[HarmonyPostfix]
			static void Postfix(Skill __instance, ref bool __result)
			{
				//TODO: add below 0 stamina config
				if (__instance.OwnerCharacter.Stamina >= 0)
				{
					__result = true;
				}
			}
		}*/

		[HarmonyPatch(typeof(Character), "HasEnoughStamina")]
		public class Character_HasEnoughStamina
		{
			[HarmonyPostfix]
			static void  HasEnoughStamina(Character __instance, ref bool __result)
			{
				//TODO:stam below 0 cfg
				__result = (!__instance.IsPhotonPlayerLocal || __instance.Stamina >= 0);
			}
		}

	}
}
