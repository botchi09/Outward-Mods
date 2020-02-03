using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using NodeCanvas.Framework;
using NodeCanvas.Tasks;
using NodeCanvas.Tasks.Actions;
using System.IO;

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
        public bool Shared_Quest_Rewards = true;
        public bool Shared_World_Drops = true;
        //public bool Shared_Quest_Progression_BETA = false;
    }

    public class SharedCoopRewards : MonoBehaviour
    {
        public ModBase _base;

        public Settings settings = new Settings();
        private static readonly string savePath = @"Mods\SharedCoopRewards.json";

        public void Init()
        {
            LoadSettings();

            // Quest reward hook
            On.NodeCanvas.Framework.ActionList.OnExecute += ListExecuteHook;
            
            // drop table hook
            On.Dropable.GenerateContents_1 += GenerateContentsHook;
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

            if (settings.Shared_Quest_Rewards)
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
            int count = settings.Shared_World_Drops ? Global.Lobby.PlayersInLobbyCount : 1;

            for (int i = 0; i < count; i++)
            {
                orig(self, _container);
            }
        }

        // settings
        private void LoadSettings()
        {
            if (File.Exists(savePath))
            {
                bool failsafe = false;
                try
                {
                    string json = File.ReadAllText(savePath);
                    if (JsonUtility.FromJson<Settings>(json) is Settings s2)
                    {
                        settings = s2;
                    }
                    else { failsafe = true; }
                }
                catch { failsafe = true; }

                if (failsafe)
                {
                    File.Delete(savePath);
                    settings = new Settings();
                    SaveSettings();
                }
                //Log("Loaded settings!\r\n" + json);
            }
            else
            {
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            if (!Directory.Exists(@"Mods")) { Directory.CreateDirectory("Mods"); }

            if (File.Exists(savePath)) { File.Delete(savePath); }

            File.WriteAllText(savePath, JsonUtility.ToJson(settings, true));
        }
    }
}
