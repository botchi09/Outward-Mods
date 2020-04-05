using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NecromancerSkills
{
    public class Frenzy : Effect
    {
        #region Frenzy Skill
        public static void SetupFrenzy()
        {
            var frenzySkill = ResourcesPrefabManager.Instance.GetItemPrefab(8890105) as Skill;

            // setup skill
            frenzySkill.CastSheathRequired = -1;

            // destroy the existing skills, but keep the rest (VFX / Sound).
            DestroyImmediate(frenzySkill.transform.Find("Lightning").gameObject);
            DestroyImmediate(frenzySkill.transform.Find("SummonSoul").gameObject);

            // set custom spellcast anim
            At.SetValue(Character.SpellCastType.Focus, typeof(Item), frenzySkill as Item, "m_activateEffectAnimType");

            var effects = new GameObject("Effects");
            effects.transform.parent = frenzySkill.transform;

            effects.AddComponent<Frenzy>();
        }
        #endregion

        protected override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            if (SummonManager.Instance == null) { return; }

            if (SummonManager.Instance.FindWeakestSummon(_affectedCharacter.UID) is GameObject summonObj)
            {
                bool insideSigil = PlagueAuraProximityCondition.IsInsidePlagueAura(_affectedCharacter.transform.position);

                float healSummon = insideSigil ? 0.66f : 0.33f;

                // restores HP to the summon
                var summonChar = summonObj.GetComponentInChildren<Character>();
                summonChar.Stats.AffectHealth(summonChar.ActiveMaxHealth * healSummon);

                // add status effects
                summonChar.StatusEffectMngr.AddStatusEffect(ResourcesPrefabManager.Instance.GetStatusEffectPrefab("Rage"));
                summonChar.StatusEffectMngr.AddStatusEffect(ResourcesPrefabManager.Instance.GetStatusEffectPrefab("Possessed"));
                summonChar.StatusEffectMngr.AddStatusEffect(ResourcesPrefabManager.Instance.GetStatusEffectPrefab("Speed Up"));

                if (insideSigil)
                {
                    // add decay imbue
                    summonChar.CurrentWeapon.AddImbueEffect(ResourcesPrefabManager.Instance.GetEffectPreset(211) as ImbueEffectPreset, 180f);
                }

            }
            else
            {
                //_affectedCharacter.CharacterUI.ShowInfoNotification("You need a summon to do that!");
                //// refund the cooldown
                //if (this.ParentItem is Skill skill)
                //{
                //    skill.ResetCoolDown();
                //    float manacost = m_affectedCharacter.Stats.GetFinalManaConsumption(new Tag[] { Tag.None }, ManaCost);
                //    _affectedCharacter.Stats.SetMana(m_affectedCharacter.Stats.CurrentMana + manacost);
                //}
            }
        }
    }
}
