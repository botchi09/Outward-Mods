using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
//using SinAPI;
using SideLoader;

namespace NecromancerSkills
{
    public class SkillManager : MonoBehaviour
    {
        //public ModBase global;
        public static SkillManager Instance;

        public static SkillSchool NecromancyTree;

        internal void Awake()
        {
            Instance = this;

            SL.OnPacksLoaded += Setup;

            // temporary hook
            On.Skill.HasBaseRequirements += ActivationConditionHook;
        }

        private void Setup()
        {
            SetupSkills(); // setup the actual skills

            SetupSkillTree(); // setup the trainer menu, and link these skills to the SkillTreeHolder (the game's skill manager)
        }

        // hooks. temporary fixes for custom activation conditions, a better implementation would be creating a custom SummonSkill : Skill class.
        private bool ActivationConditionHook(On.Skill.orig_HasBaseRequirements orig, Skill self, bool _tryingToActivate)
        {
            if (_tryingToActivate) // only do this when trying to activate, otherwise it runs to summon manager check every frame which is a bit overkill.
            {
                // custom check for Life Ritual and Death Ritual (requires a summoned skeleton)
                if (self.ItemID == 8890105 || self.ItemID == 8890106)
                {
                    if (!SummonManager.Instance.FindWeakestSummon(self.OwnerCharacter.UID))
                    {
                        self.OwnerCharacter.CharacterUI.ShowInfoNotification("You need a Summon to do that!");
                        return false;
                    }
                }
            }

            return orig(self, _tryingToActivate);
        }

        #region Actual Skills Setup
        private void SetupSkills()
        {
            // I use the SideLoader to do the preliminary setup (clone existing skill, change ID / name / etc..)

            SetupPassives();

            SummonSkeleton.SetupSummon();

            ShootTendrils.SetupTendrils();

            Frenzy.SetupFrenzy();

            DetonateBlast.SetupDetonate();

            PlagueAura.SetupPlagueAura();
        }

        #region Passive Skills
        private void SetupPassives()
        {
            // Vital Attunement is now set up completely by SideLoader :)

            //// =============== bonus stats vital attunement ===============
            //var vitalAttunement = ResourcesPrefabManager.Instance.GetItemPrefab(8890101) as PassiveSkill;
            //var affectHealth = vitalAttunement.GetComponentInChildren<AffectStat>();
            //affectHealth.Value = ModBase.settings.VitalAttunement_HealthBonus;
            //var affectStamina = affectHealth.gameObject.AddComponent<AffectStat>();
            //affectStamina.AffectedStat = new TagSourceSelector(TagSourceManager.Instance.GetTag("79"));
            //affectStamina.Value = ModBase.settings.VitalAttunement_StaminaBonus;
            //affectStamina.IsModifier = false;

            // =============== transendence ===============

            var transcendence = ResourcesPrefabManager.Instance.GetItemPrefab(8890104) as PassiveSkill;
            var passiveTransform = transcendence.transform.Find("Passive");
            DestroyImmediate(passiveTransform.GetComponent<AffectStat>());

            // add elemental bonuses using custom ManaPointAffectStat class

            passiveTransform.gameObject.AddComponent(new ManaPointAffectStat() { SelectedUID = "98" });
            passiveTransform.gameObject.AddComponent(new ManaPointAffectStat() { SelectedUID = "99" });
            passiveTransform.gameObject.AddComponent(new ManaPointAffectStat() { SelectedUID = "100" });
            passiveTransform.gameObject.AddComponent(new ManaPointAffectStat() { SelectedUID = "101" });
            passiveTransform.gameObject.AddComponent(new ManaPointAffectStat() { SelectedUID = "102" });


            // ======== strong resurrect (no longer need any setup here, handled by the Summon Skill itself)

            //var strongRes = ResourcesPrefabManager.Instance.GetItemPrefab(8890108) as PassiveSkill;
            //passiveTransform = strongRes.transform.Find("Passive");
            //DestroyImmediate(passiveTransform.GetComponent<AffectStat>());
        }
        #endregion

        #endregion

        #region Skill Tree setup, and add to SkillTreeHolder (game manager)

        private void SetupSkillTree()
        {
            // load the dev template for skill progression tree
            if (CustomSkills.CreateSkillSchool("Necromancy").gameObject is GameObject necroTree)
            {
                NecromancyTree = necroTree.GetComponent<SkillSchool>();

                NecromancyTree.SchoolSigil = Sprite.Create(new Texture2D(0, 0), new Rect(0, 0, 0, 0), Vector2.zero); // TODO

                // row 1
                var row1 = necroTree.transform.Find("Row1");
                CustomItems.DestroyChildren(row1.transform);
                var resurrect = CustomSkills.CreateSkillSlot(row1, "Resurrect", 8890103, 50, null, false, 2);
                var statboost = CustomSkills.CreateSkillSlot(row1, "PassiveStatBoost", 8890101, 50, null, false, 3);

                // row 2
                var row2 = necroTree.transform.Find("Row2");
                CustomItems.DestroyChildren(row2.transform);
                var frenzy = CustomSkills.CreateSkillSlot(row2, "Frenzy", 8890105, 100, resurrect, false, 2);
                CustomSkills.CreateSkillSlot(row2, "Tendrils", 8890100, 100, statboost, false, 3);

                // row 3
                var row3 = necroTree.transform.Find("Row3");
                CustomItems.DestroyChildren(row3);
                var transcendence = CustomSkills.CreateSkillSlot(row3, "Transcendence", 8890104, 600, frenzy, true, 2);

                // row 4
                var row4 = necroTree.transform.Find("Row4");
                CustomItems.DestroyChildren(row4);
                var detonate = CustomSkills.CreateSkillSlot(row4, "Detonate", 8890106, 600, transcendence, false, 2);

                // row 5
                var row5 = new GameObject("Row5");
                row5.transform.parent = necroTree.transform;
                row5.AddComponent(new SkillBranch() { ParentTree = NecromancyTree });
                var choiceObj2 = new GameObject("Choice2");
                choiceObj2.transform.parent = row5.transform;
                var choice2 = choiceObj2.AddComponent<SkillSlotFork>();
                At.SetValue(2, typeof(BaseSkillSlot), choice2 as BaseSkillSlot, "m_columnIndex");
                At.SetValue(detonate, typeof(BaseSkillSlot), choice2 as BaseSkillSlot, "m_requiredSkillSlot");
                CustomSkills.CreateSkillSlot(choice2.transform, "DeathCloud", 8890107, 600, detonate, false, 2);
                CustomSkills.CreateSkillSlot(choice2.transform, "StrongResurrect", 8890108, 600, detonate, false, 2);

                NecromancyTree.gameObject.SetActive(true);
            }
        }

        #endregion

    }

    #region Component Tools
    public static class ComponentTools
    {
        public static T GetCopyOf<T>(this Component comp, T other) where T : Component
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;// | BindingFlags.Default | BindingFlags.DeclaredOnly | BindingFlags.Static;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }
    }
    #endregion
}
