using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace CombatAndDodgeOverhaul
{
    public class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance;

        private List<string> FixedEnemies = new List<string>();
        private string CurrentScene = "";
        private bool SceneChanged = false;

        private Settings m_currentSyncInfos;
        private string m_currentHostUID = "";
        public float TimeOfLastSyncSend = -1f; // host set this when sends info
        private float m_timeOfLastSyncRequest = -5f; // non-host set this when receive info
        //private Coroutine _FindHostCoroutine;

        internal void Awake()
        {
            Instance = this;

            On.CharacterStats.ApplyCoopStats += new On.CharacterStats.hook_ApplyCoopStats(ApplyCoopStatsHook);
        }

        // just for resetting FixedEnemies on any scene change
        internal void Update()
        {
            if (CurrentScene != SceneManagerHelper.ActiveSceneName)
            {
                SceneChanged = true;
                CurrentScene = SceneManagerHelper.ActiveSceneName;
            }

            if (Global.Lobby.PlayersInLobbyCount < 1 || NetworkLevelLoader.Instance.IsGameplayPaused)
            {
                return;
            }

            if (SceneChanged)
            {
                SceneChanged = false;
                m_currentHostUID = "";
                FixedEnemies.Clear();
            }
        }

        // hook for when the game applies Co-op stats
        private void ApplyCoopStatsHook(On.CharacterStats.orig_ApplyCoopStats orig, CharacterStats self)
        {
            var character = self.GetComponent<Character>();
            if (!character.IsAI)
            {
                orig(self);
                return;
            }

            if (!PhotonNetwork.isNonMasterClientInRoom)
            {
                // if we are world host....
                if (!OverhaulGlobal.settings.Enable_Enemy_Mods)
                {
                    orig(self);
                    return;
                }
                else
                {
                    SetEnemyMods(OverhaulGlobal.settings, self, character);
                }
            }
            else // else we are not host
            {
                //Debug.Log("non-host Apply stats hook");
                if (UpdateSyncStats() || m_currentSyncInfos == null)
                {
                    //Debug.Log("Settings need update - sent request for sync, and starting delayed orig(self)");
                    StartCoroutine(DelayedOrigSelf(orig, self));
                }
                else
                {
                    if (m_currentSyncInfos != null && m_currentSyncInfos.Enable_Enemy_Mods)
                    {
                        //Debug.Log("No sync required, setting up stats from current cache");
                        SetEnemyMods(m_currentSyncInfos, self, character); 
                    }
                    if (m_currentSyncInfos == null || !m_currentSyncInfos.Enable_Enemy_Mods || !m_currentSyncInfos.Enemy_Balancing)
                    {
                        orig(self);
                        return;
                    }
                }
            }
        }

        private IEnumerator DelayedOrigSelf(On.CharacterStats.orig_ApplyCoopStats orig, CharacterStats self)
        {
            float start = Time.time;
            while (Time.time - start < 5f && m_currentSyncInfos == null)
            {
                //Debug.Log("Delayed orig self - waiting for sync infos");
                if (!NetworkLevelLoader.Instance.AllPlayerDoneLoading)
                {
                    start += 1f;
                }
                yield return new WaitForSeconds(1.0f);
            }
            if (m_currentSyncInfos == null || !m_currentSyncInfos.Enemy_Balancing || !m_currentSyncInfos.Enable_Enemy_Mods)
            {
                try 
                { 
                    orig(self);
                }
                catch { }
            }
        }

        private bool UpdateSyncStats() // returns true if update is performed, false if no change.
        {
            if (CharacterManager.Instance.GetWorldHostCharacter() is Character host)
            {
                if (host.UID != m_currentHostUID)
                {
                    if (PhotonNetwork.isNonMasterClientInRoom)
                    {
                        if (Time.time - m_timeOfLastSyncRequest > 5f)
                        {
                            m_timeOfLastSyncRequest = Time.time;
                            m_currentSyncInfos = null;
                            RPCManager.Instance.RequestSettings();
                        }
                    }
                    else
                    {
                        m_currentHostUID = host.UID;
                        m_currentSyncInfos = OverhaulGlobal.settings;
                    }
                    return true;
                }
                else // host UID is already updated.
                {
                    return false;
                }
            }
            else
            {
                Debug.Log("CombatOverhaul: Could not find host!");
                //if (_FindHostCoroutine == null)
                //{
                    
                //    _FindHostCoroutine = StartCoroutine(FindHostCoroutine());
                //}
                return true;
            }
        }

        //private IEnumerator FindHostCoroutine()
        //{
        //    var host = CharacterManager.Instance.GetWorldHostCharacter();
        //    float start = Time.time;

        //    while (Time.time - start < 10f && host == null)
        //    {
        //        yield return new WaitForSeconds(0.1f);
        //        Debug.Log(Time.time + " | Searching for host...");
        //        host = CharacterManager.Instance.GetWorldHostCharacter();
        //    }
        //    if (host != null)
        //    {
        //        UpdateSyncStats();
        //    }
        //    else
        //    {
        //        Debug.LogError("Timeout! Could not find host!");
        //    }
        //}

        // called from RPC manager
        public void SetSyncInfo(bool modsEnabled, bool enemiesAllied, bool customStats, float healthModifier, float damageModifier, float impactRes, float damageRes, float impactDmg)
        {
            m_currentHostUID = CharacterManager.Instance.GetWorldHostCharacter()?.UID;
            //Debug.Log("Received sync from host uid: " + m_currentHostUID);

            this.m_currentSyncInfos = new Settings
            {
                Enable_Enemy_Mods = modsEnabled,
                All_Enemies_Allied = enemiesAllied,
                Enemy_Balancing = customStats,
                Enemy_Health = healthModifier,
                Enemy_Damages = damageModifier,
                Enemy_Resistances = damageRes,
                Enemy_ImpactRes = impactRes,
                Enemy_ImpactDmg = impactDmg
            };

            if (modsEnabled)
            {
                //Debug.Log("Updating all current characters");
                foreach (Character c in CharacterManager.Instance.Characters.Values.Where(x => x.IsAI))
                {
                    SetEnemyMods(m_currentSyncInfos, c.Stats, c);
                }
            }
        }

        // actual function to set an enemy's stats
        private void SetEnemyMods(Settings _settings, CharacterStats _stats, Character m_character)
        {
            if (m_character == null || !m_character.IsAI || m_character.Faction == Character.Factions.Player)
            {
                //Debug.Log("trying to set stats for a null character, or a non-AI character");
                return;
            }

            if (FixedEnemies.Contains(m_character.UID))
            {
                // Debug.Log("Fixed enemies already contains " + m_character.Name);
                return;
            }

            if (_settings.Enemy_Balancing)
            {
                string stackSource = "CombatOverhaul";

                if (!PhotonNetwork.isNonMasterClientInRoom)
                {
                    // set health modifier
                    var healthTag = TagSourceManager.Instance.GetTag("77");  // 77 = max health
                    var healthStack = new StatStack(stackSource, _settings.Enemy_Health - 1);
                    _stats.RemoveStatStack(healthTag, stackSource, true);
                    _stats.AddStatStack(healthTag, healthStack, true);
                    At.SetValue(_stats.CurrentHealth * _settings.Enemy_Health, typeof(CharacterStats), _stats, "m_health");
                }

                // set impact resistance
                var impactTag = TagSourceManager.Instance.GetTag("84"); // 84 = impact res
                _stats.RemoveStatStack(impactTag, stackSource, false);
                var impactStack = new StatStack(stackSource, _settings.Enemy_ImpactRes);
                _stats.AddStatStack(impactTag, impactStack, false); 

                // damage bonus
                var damageTag = TagSourceManager.Instance.GetTag("96"); // 96 = all damage bonus
                _stats.RemoveStatStack(damageTag, stackSource, true);
                var damageStack = new StatStack(stackSource, _settings.Enemy_Damages * 0.01f);
                _stats.AddStatStack(damageTag, damageStack, true);

                // impact modifier
                var impactModifier = At.GetValue(typeof(CharacterStats), _stats, "m_impactModifier") as Stat;
                impactModifier.RemoveStack(stackSource, true);
                impactModifier.AddStack(new StatStack(stackSource, _settings.Enemy_ImpactDmg * 0.01f), true);

                for (int i = 0; i < 6; i++)
                {
                    // damage resistance (Capped at 99, unless already 100)
                    float currentRes = m_character.Stats.GetDamageResistance((DamageType.Types)i);
                    if (currentRes < 100)
                    {
                        var valueToSet = _settings.Enemy_Resistances;

                        if (currentRes + valueToSet >= 99)
                        {
                            valueToSet = 99 - currentRes;
                        }

                        int tag = 113 + i;  // 113 to 118 = damage resistance stats
                        var damageResTag = TagSourceManager.Instance.GetTag(tag.ToString());
                        _stats.RemoveStatStack(damageResTag, stackSource, true);
                        var resStack = new StatStack(stackSource, valueToSet);
                        _stats.AddStatStack(damageResTag, resStack, false);
                    }
                }
            }

            if (_settings.All_Enemies_Allied)
            {
                m_character.ChangeFaction(Character.Factions.Bandits);
                m_character.TargetingSystem.AlliedToSameFaction = true;

                Character.Factions[] targets = new Character.Factions[] { Character.Factions.Player };
                At.SetValue(targets, typeof(TargetingSystem), m_character.TargetingSystem, "TargetableFactions");

                // fix skills
                foreach (var uid in m_character.Inventory.SkillKnowledge.GetLearnedActiveSkillUIDs())
                {
                    if (ItemManager.Instance.GetItem(uid) is Skill skill)
                    {
                        foreach (Shooter shooter in skill.GetComponentsInChildren<Shooter>())
                        {
                            shooter.Setup(targets, shooter.transform.parent);
                        }
                    }
                }
            }

            FixedEnemies.Add(m_character.UID);
        }
    }
}
