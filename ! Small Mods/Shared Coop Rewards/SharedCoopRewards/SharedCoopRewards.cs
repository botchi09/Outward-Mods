using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using NodeCanvas.Framework;
using NodeCanvas.Tasks;
using NodeCanvas.Tasks.Actions;
using System.IO;
using System.Reflection;
using SharedModConfig;

namespace SharedCoopRewards
{
    public class ModBase : PartialityMod
    {
        public static SharedCoopRewards Instance;

        public GameObject obj;
        public string ID = "Shared Rewards";
        public double version = 1.0;

        public ModBase()
        {
            this.author = "Sinai";
            this.ModID = ID;
            this.Version = version.ToString("0.00");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            obj = new GameObject(ID);
            GameObject.DontDestroyOnLoad(obj);

            Instance = obj.AddComponent<SharedCoopRewards>();
            Instance._base = this;
            Instance.Init();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class Settings
    {
        public static readonly string Shared_Quest_Rewards = "Shared_Quest_Rewards";
        public static readonly string Shared_ALL_Quest_Rewards = "Share ALL Quest Rewards";
        public static readonly string Shared_World_Drops = "Shared_World_Drops";
        //public bool Shared_Quest_Progression_BETA = false;
    }

    public class SharedCoopRewards : MonoBehaviour
    {
        public ModBase _base;

        public ModConfig config;

        public void Init()
        {
            // LoadSettings();
            config = SetupConfig();

            StartCoroutine(SetupCoroutine());

            // Quest reward hook
            On.NodeCanvas.Framework.ActionList.OnExecute += ListExecuteHook;

            // unsafe share hook
            On.NodeCanvas.Tasks.Actions.GiveReward.OnExecute += GiveRewardHook;
            
            // drop table hook
            On.Dropable.GenerateContents_1 += GenerateContentsHook;
        }

        private IEnumerator SetupCoroutine()
        {
            while (ConfigManager.Instance == null || !ConfigManager.Instance.IsInitDone())
            {
                yield return new WaitForSeconds(0.1f);
            }

            config.Register();
        }

        // experimental unsafe hook
        private void GiveRewardHook(On.NodeCanvas.Tasks.Actions.GiveReward.orig_OnExecute orig, GiveReward self)
        {
            if ((bool)config.GetValue(Settings.Shared_ALL_Quest_Rewards))
            {
                self.RewardReceiver = GiveReward.Receiver.Everyone;
            }

            orig(self);
        }

        // list reward hook - check if silver cost is one of the actions. if so, dont share this reward.
        private void ListExecuteHook(On.NodeCanvas.Framework.ActionList.orig_OnExecute orig, ActionList self)
        {
            try
            {
                self.actions.First(x => x is GiveReward);
            }
            catch (InvalidOperationException)
            {
                // Linq.First() will throw an InvalidOperationException if the list does not contain a GiveReward.
                // Since there is no reward, we can just orig(self) and return.
                orig(self);
                return;
            }

            if ((bool)config.GetValue(Settings.Shared_Quest_Rewards))
            {
                bool HasSilverCost = false;

                // check for "RemoveItem" tasks
                foreach (var task in self.actions.Where(x => x is RemoveItem))
                {
                    // check if the Items list contains Silver
                    if ((task as RemoveItem).Items.Where(x => x.value.ItemID == 9000010).Count() > 0)
                    {
                        // we are spending silver to get this reward. dont share.
                        HasSilverCost = true;
                        Debug.Log("Silver cost found! Not sharing, if there are rewards.");
                        break;
                    }
                }

                if (!HasSilverCost)
                {
                    Debug.Log("Reward does not cost silver. Sharing.");
                    foreach (var task in self.actions.Where(x => x is GiveReward))
                    {
                        (task as GiveReward).RewardReceiver = GiveReward.Receiver.Everyone;
                    }
                }
            }

            orig(self);
        }

        // Dropable.GenerateContents affects enemy and loot container contents
        private void GenerateContentsHook(On.Dropable.orig_GenerateContents_1 orig, Dropable self, ItemContainer _container)
        {
            int count = (bool)config.GetValue(Settings.Shared_World_Drops) ? Global.Lobby.PlayersInLobbyCount : 1;

            for (int i = 0; i < count; i++)
            {
                orig(self, _container);
            }
        }

        private ModConfig SetupConfig()
        {
            var newConfig = new ModConfig
            {
                ModName = "SharedCoopRewards",
                SettingsVersion = 1.1,
                Settings = new List<BBSetting>
                {
                    new BoolSetting
                    {
                        Name = Settings.Shared_Quest_Rewards,
                        Description = "Share Items and Skills from Quest Rewards (safer, may not cover everything)",
                        DefaultValue = true
                    },
                    new BoolSetting
                    {
                        Name = Settings.Shared_ALL_Quest_Rewards,
                        Description = "Share ALL Quest Rewards (may occasionally lead to unexpected things being shared)",
                        DefaultValue = false
                    },
                    new BoolSetting
                    {
                        Name = Settings.Shared_World_Drops,
                        Description = "Generate extra loot from Enemies and Loot Containers for each player",
                        DefaultValue = true
                    }
                }
            };

            return newConfig;
        }
    }
}
