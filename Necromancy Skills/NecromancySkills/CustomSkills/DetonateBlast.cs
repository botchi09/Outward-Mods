using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
//using SinAPI;
//using OModAPI;

namespace NecromancerSkills
{
    public class DetonateBlast : ShootBlast
    {
        // skill setup called by Skillmanager init
        #region Detonate Skill
        public static void SetupDetonate()
        {
            var detonateSkill = ResourcesPrefabManager.Instance.GetItemPrefab(8890106) as AttackSkill;

            // setup skill
            detonateSkill.CastSheathRequired = -1;
            detonateSkill.RequiredOffHandTypes.Clear();
            detonateSkill.RequiredTags = new TagSourceSelector[0];
            detonateSkill.StartVFX = null;

            // destroy these existing effects
            Destroy(detonateSkill.transform.Find("RunicRay").gameObject);
            DestroyImmediate(detonateSkill.transform.Find("Effects").gameObject);

            // hang onto the ShootBlast, we want to use this
            var shootBlast = detonateSkill.transform.Find("RunicBlast").GetChild(0).GetComponent<ShootBlast>();

            // ======== setup weak blast ============

            // create new Effects object
            var effects = new GameObject("Effects");
            effects.transform.parent = detonateSkill.transform;
            var detonateBlast = effects.AddComponent<DetonateBlast>();
            At.InheritBaseValues(detonateBlast, shootBlast);
            // destroy the old RunicBlast now that we stole the blast component
            Destroy(detonateSkill.transform.Find("RunicBlast").gameObject);

            // add condition. Required item on summon is Mertons Bones.
            var condition = effects.AddComponent<DetonateCondition>();
            condition.RequiredSummonEquipment = 3200030;
            condition.Invert = false;

            // disable clone target before cloning it
            var origBlast = detonateBlast.BaseBlast.gameObject;
            origBlast.SetActive(false);
            var blastObj = Instantiate(origBlast);
            DontDestroyOnLoad(blastObj);
            blastObj.name = "DetonateBlast";

            var blast = blastObj.GetComponentInChildren<CircularBlast>();
            detonateBlast.BaseBlast = blast;

            if (blast.GetComponentInChildren<PunctualDamage>() is PunctualDamage pDamage)
            {
                //pDamage.Damages = new DamageType[] 
                //{ 
                //    new DamageType(DamageType.Types.Decay, 40),
                //    new DamageType(DamageType.Types.Ethereal, 20) 
                //};
                pDamage.Damages = ModBase.settings.DeathRitual_WeakExplosionDamage;
                //pDamage.Knockback = 75;
                pDamage.Knockback = ModBase.settings.DeathRitual_WeakKnockback;
            }

            var explosionFX = blast.transform.Find("ExplosionFX").gameObject;
            foreach (ParticleSystem particles in explosionFX.GetComponentsInChildren<ParticleSystem>()) 
            {
                var m = particles.main;
                m.startColor = Color.green;
            }

            // =========== STRONG DETONATION (blue ghost) ================= //

            var effects2 = new GameObject("Effects");
            effects2.transform.parent = detonateSkill.transform;
            var detonateBlast2 = effects2.AddComponent<DetonateBlast>();
            At.InheritBaseValues(detonateBlast2, detonateBlast);

            // add condition. Required item on summon is Blue Ghost robes.
            var condition2 = effects2.AddComponent<DetonateCondition>();
            condition2.RequiredSummonEquipment = 3200040;
            condition2.Invert = false;

            origBlast.SetActive(false);
            var blastObj2 = Instantiate(origBlast);
            origBlast.SetActive(false);
            DontDestroyOnLoad(blastObj2);
            blastObj2.name = "StrongDetonateBlast";

            var blast2 = blastObj2.GetComponent<CircularBlast>();
            detonateBlast2.BaseBlast = blast2;

            if (blast2.GetComponentInChildren<PunctualDamage>() is PunctualDamage pDamage2)
            {
                pDamage2.Damages = new DamageType[]
                {
                    new DamageType(DamageType.Types.Decay, 50),
                    new DamageType(DamageType.Types.Ethereal, 20),
                    new DamageType(DamageType.Types.Frost, 20) 
                };
                pDamage2.Knockback = 150;

                pDamage2.gameObject.AddComponent(new AddStatusEffect { Status = ResourcesPrefabManager.Instance.GetStatusEffectPrefab("Slow Down") });
            }

            var explosionFX2 = blast2.transform.Find("ExplosionFX").gameObject;
            foreach (ParticleSystem particles in explosionFX2.GetComponentsInChildren<ParticleSystem>())
            {
                var m = particles.main;
                m.startColor = new Color() { r = 0.2f, g = 0.4f, b = 1, a = 1 }; // cyan-ish
            }
        }
        #endregion

        protected override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            if (SummonManager.Instance == null) { return; }

            if (SummonManager.Instance.FindWeakestSummon(_affectedCharacter.UID) is GameObject summonObj
                && summonObj.GetComponentInChildren<Character>() is Character summonChar
                && summonChar.isActiveAndEnabled)
            {

                // change blast position to the summon's position
                _infos[0] = summonChar.transform.position;
                base.ActivateLocally(_affectedCharacter, _infos);

                // kill the summon
                summonChar.Stats.ReceiveDamage(999f);

                // fix for cooldown not working on this skill for some reason
                var skill = this.ParentItem as Skill;
                At.SetValue(Time.time, typeof(Skill), skill, "m_lastActivationTime");
                At.SetValue(-1, typeof(Skill), skill, "m_lastCooldownProgress");

                // plague aura interaction
                if (PlagueAuraProximityCondition.IsInsidePlagueAura(summonChar.transform.position))
                {
                    // if you're inside a plague aura, detonate resets your Summon cooldown.
                    if (_affectedCharacter.Inventory.SkillKnowledge.GetItemFromItemID(8890103) is Skill summonSkill)
                    {
                        summonSkill.ResetCoolDown();
                    }
                }
            }
        }
    }
}
