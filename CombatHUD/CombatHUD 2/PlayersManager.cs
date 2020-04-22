using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CombatHUD
{
    public class PlayersManager : MonoBehaviour
    {
        public static PlayersManager Instance;

        private List<GameObject> m_labelHolders = new List<GameObject>();

        internal void Awake()
        {
            Instance = this;

            foreach (Transform child in this.transform)
            {
                m_labelHolders.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }
        }

        internal void Update()
        {
            if (NetworkLevelLoader.Instance.IsGameplayLoading || NetworkLevelLoader.Instance.IsGameplayPaused)
            {
                return;
            }

            List<StatusEffectInfo> statusInfos = new List<StatusEffectInfo>();

            for (int i = 0; i < SplitScreenManager.Instance.LocalPlayers.Count; i++)
            {
                if (SplitScreenManager.Instance.LocalPlayers[i].AssignedCharacter == null)
                {
                    continue;
                }

                UpdateVitalText(SplitScreenManager.Instance.LocalPlayers[i].AssignedCharacter);

                if ((bool)CombatHUD.config.GetValue(Settings.PlayerStatusTimers))
                {
                    try
                    {
                        UpdatePlayerStatuses(i, ref statusInfos);
                    }
                    catch //(Exception e)
                    {
                        //Debug.LogError("Error updating statuses: " + e.Message);
                    }
                }
            }

            // update text holders
            for (int i = 0; i < m_labelHolders.Count; i++)
            {
                if (i >= statusInfos.Count || !(bool)CombatHUD.config.GetValue(Settings.PlayerStatusTimers))
                {
                    if (m_labelHolders[i].activeSelf)
                    {
                        m_labelHolders[i].SetActive(false);
                    }
                }
                else
                {
                    var text = m_labelHolders[i].GetComponent<Text>();

                    var iconRect = statusInfos[i].LinkedIcon.RectTransform;
                    var posOffset = new Vector3(0, CombatHUD.Rel(25f, true), 0);
                    text.GetComponent<RectTransform>().position = iconRect.position + posOffset;

                    TimeSpan t = TimeSpan.FromSeconds(statusInfos[i].TimeRemaining);
                    text.text = t.Minutes + ":" + t.Seconds.ToString("00");

                    if (statusInfos[i].TimeRemaining < 15)
                    {
                        text.color = Color.red;
                    }
                    else
                    {
                        text.color = Color.white;
                    }
                    if (!m_labelHolders[i].activeSelf)
                    {
                        m_labelHolders[i].SetActive(true);
                    }
                }
            }
        }

        private void UpdateVitalText(Character player)
        {
            CharacterBarListener manager = player.CharacterUI.transform.Find("Canvas/GameplayPanels/HUD/MainCharacterBars").GetComponent<CharacterBarListener>();

            if (manager == null) { return; } // OLogger.Error("BarManager is null"); return; }

            if (At.GetValue(typeof(CharacterBarListener), manager, "m_healthBar") is Bar healthBar
                && At.GetValue(typeof(Bar), healthBar, "m_lblValue") is Text healthText
                && At.GetValue(typeof(CharacterBarListener), manager, "m_manaBar") is Bar manaBar
                && At.GetValue(typeof(Bar), manaBar, "m_lblValue") is Text manaText
                && At.GetValue(typeof(CharacterBarListener), manager, "m_staminaBar") is Bar stamBar
                && At.GetValue(typeof(Bar), stamBar, "m_lblValue") is Text stamText)
            {
                healthText.fontSize = 14;
                manaText.fontSize = 14;
                stamText.fontSize = 14;

                healthBar.TextValueDisplayed = (bool)CombatHUD.config.GetValue(Settings.PlayerVitals);
                manaBar.TextValueDisplayed = (bool)CombatHUD.config.GetValue(Settings.PlayerVitals);
                stamBar.TextValueDisplayed = (bool)CombatHUD.config.GetValue(Settings.PlayerVitals);
            }
        }

        private void UpdatePlayerStatuses(int splitID, ref List<StatusEffectInfo> statusInfos)
        {
            var player = SplitScreenManager.Instance.LocalPlayers[splitID];

            if (player == null || player.AssignedCharacter == null)
            {
                return;
            }

            var effectsManager = player.AssignedCharacter.StatusEffectMngr;
            var panel = player.CharUI.GetComponentInChildren<StatusEffectPanel>();

            if (!panel || !effectsManager)
            {
                Debug.LogError("Could not find status effect managers for " + player.AssignedCharacter.Name);
                return;
            }

            var activeIcons = At.GetValue(typeof(StatusEffectPanel), panel, "m_statusIcons") as Dictionary<string, StatusEffectIcon>;

            foreach (KeyValuePair<string, StatusEffectIcon> entry in activeIcons)
            {
                if (!entry.Value.gameObject.activeSelf)
                {
                    continue;
                }

                float remainingLifespan = 0f;

                StatusEffect status = effectsManager.Statuses.Find(s => s.IdentifierName == entry.Key);
                if (status)
                {
                    remainingLifespan = status.RemainingLifespan;
                }
                else
                {
                    // some statuses use an identifier tag instead of their own status name for the icon...
                    switch (entry.Key.ToLower())
                    {
                        case "imbuemainweapon":
                            remainingLifespan = (panel as UIElement).LocalCharacter.CurrentWeapon.FirstImbue.RemainingLifespan;
                            break;
                        case "imbueoffweapon":
                            remainingLifespan = (panel as UIElement).LocalCharacter.LeftHandWeapon.FirstImbue.RemainingLifespan;
                            break;
                        case "summonweapon":
                            remainingLifespan = (panel as UIElement).LocalCharacter.CurrentWeapon.SummonedEquipment.RemainingLifespan;
                            break;
                        case "summonghost":
                            remainingLifespan = (panel as UIElement).LocalCharacter.CurrentSummon.RemainingLifespan;
                            break;
                        case "129": // marsh poison uses "129" for its tag, I think that's its effect preset ID?
                            if (effectsManager.Statuses.Find(z => z.IdentifierName.Equals("Hallowed Marsh Poison Lvl1")) is StatusEffect marshpoison)
                                remainingLifespan = marshpoison.RemainingLifespan;
                            break;
                        default:
                            //Debug.Log("[CombatHUD] Unhandled Status Identifier! Key: " + entry.Key);
                            continue;
                    }
                }

                if (remainingLifespan > 0f && entry.Value != null)
                {
                    statusInfos.Add(new StatusEffectInfo
                    {
                        TimeRemaining = remainingLifespan,
                        LinkedIcon = entry.Value
                    });
                }
            }
        }

        public class StatusEffectInfo
        {
            public float TimeRemaining;
            public StatusEffectIcon LinkedIcon;
        }
    }
}
