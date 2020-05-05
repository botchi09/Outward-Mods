using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SideLoader;

namespace Combat_Dummy
{
    public class DummyCharacter
    {
        public const string DUMMY_NAME = "Combat Dummy";

        public string Name { get; set; }
        public DummyConfig Config { get; set; }

        public bool CharacterExists { get => m_character != null; }
        private Character m_character;

        public void DestroyCharacter()
        {
            CustomCharacters.DestroyCharacterRPC(m_character);
        }

        public void SpawnOrReset(DummyConfig _config = null)
        {
            if (_config == null && Config == null)
            {
                Debug.LogError("null config!");
                return;
            }
            else if (_config != null)
            {
                Config = _config;
            }

            var pos = CharacterManager.Instance.GetFirstLocalCharacter().transform.position;
            pos += new Vector3(1f, 0f, 1f);

            bool newspawn = false;
            if (m_character == null)
            {
                // create character with SideLoader
                var newCharacter = CustomCharacters.CreateCharacter(pos, UID.Generate(), Name ?? DUMMY_NAME);
                m_character = newCharacter.GetComponent<Character>();
                CustomCharacters.SetupBasicAI(m_character);

                newspawn = true;
            }

            // init
            m_character.gameObject.SetActive(true);

            CombatDummyMod.Instance.StartCoroutine(DelayedSetup(Config, pos, newspawn));
        }

        private IEnumerator DelayedSetup(DummyConfig _config, Vector3 pos, bool newSpawn)
        {
            yield return new WaitForSeconds(0.5f);

            // set and apply stats
            Config = _config;
            Config.ApplyToCharacter(m_character);

            if (newSpawn)
            {
                SetAIEnabled(false);
            }

            // heal or resurrect
            HealCharacter();

            // try repair
            try { m_character.Inventory.RepairEverything(); } catch { }

            // teleport
            try { m_character.Teleport(pos, Quaternion.identity); } catch { }
        }

        public void SetCharacterEnabled(bool enabled)
        {
            m_character.gameObject.SetActive(enabled);
        }

        public void SetAIEnabled(bool enabled)
        {
            if (m_character == null)
            {
                return;
            }

            var ai = m_character.GetComponent<CharacterAI>();
            foreach (var state in ai.AiStates)
            {
                if (state is AISCombat aiscombat)
                {
                    aiscombat.ChanceToDefend = enabled ? Config.ChanceToDefend : 0;
                }

                state.enabled = enabled;
            }
        }

        public void HealCharacter()
        {
            At.Call(m_character.Stats, "RefreshVitalMaxStat", new object[] { false });

            if (m_character.IsDead)
            {
                m_character.Resurrect();
            }

            if (m_character.StatusEffectMngr != null)
            {
                m_character.StatusEffectMngr.Purge();
            }

            m_character.Stats.RestoreAllVitals();
        }
    }

    public class DummyConfig
    {
        public const string StatSourceID = "DummyStat";

        // faction
        public Character.Factions Faction = Character.Factions.Bandits;

        // gear
        public int Weapon = 2000010;

        // ai
        public float ChanceToDefend = 0;

        // stats
        public float Health = 500;
        public float ImpactResist = 0;
        public float Protection = 0;
        public float[] Damage_Resists = new float[6] { 0f, 0f, 0f, 0f, 0f, 0f };

        public float[] Damage_Bonus = new float[6] { 0f, 0f, 0f, 0f, 0f, 0f };
    
        public void ApplyToCharacter(Character character)
        {
            // set faction
            character.ChangeFaction(Faction);

            // gear (just weapon atm)
            if (ResourcesPrefabManager.Instance.GetItemPrefab(Weapon) is Weapon wepPrefab)
            {
                var currentWep = character.CurrentWeapon;

                if (currentWep != null && wepPrefab.ItemID != currentWep.ItemID)
                {
                    character.Inventory.UnequipItem(currentWep);
                }

                if (currentWep == null || wepPrefab.ItemID != currentWep.ItemID)
                {
                    var weapon = ItemManager.Instance.GenerateItemNetwork(Weapon) as Weapon;
                    weapon.ChangeParent(character.Inventory.Equipment.GetMatchingEquipmentSlotTransform(weapon.EquipSlot));
                }
            }

            // AI
            var ai = character.GetComponent<CharacterAI>();
            foreach (var state in ai.AiStates)
            {
                if (state is AISCombat aisCombat)
                {
                    aisCombat.ChanceToDefend = ChanceToDefend;
                }
            }

            // stats
            var stats = character.GetComponent<CharacterStats>();

            var m_maxHealthStat = (Stat)At.GetValue(typeof(CharacterStats), stats, "m_maxHealthStat");
            m_maxHealthStat.AddStack(new StatStack(StatSourceID, Health - 100), false);

            var m_impactResistance = (Stat)At.GetValue(typeof(CharacterStats), stats, "m_impactResistance");
            m_impactResistance.AddStack(new StatStack(StatSourceID, ImpactResist), false);

            var m_damageProtection = (Stat[])At.GetValue(typeof(CharacterStats), stats, "m_damageProtection");
            m_damageProtection[0].AddStack(new StatStack(StatSourceID, Protection), false);

            var m_damageResistance = (Stat[])At.GetValue(typeof(CharacterStats), stats, "m_damageResistance");
            for (int i = 0; i < 6; i++)
            {            
                m_damageResistance[i].AddStack(new StatStack(StatSourceID, Damage_Resists[i]), false);
            }

            var m_damageTypesModifier = (Stat[])At.GetValue(typeof(CharacterStats), stats, "m_damageTypesModifier");
            for (int i = 0; i < 6; i++)
            {
                m_damageTypesModifier[i].AddStack(new StatStack(StatSourceID, Damage_Bonus[i]), false);
            }
        }
    }
}
