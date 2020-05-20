using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using BepInEx;
using HarmonyLib;

// ORIGINAL MOD BY ASHNAL AND STIMMEDCOW
// CUSTOM KEYBINDINGS BY STIAN

// Fixed by Sinai

namespace ExtendedQuickslots2
{
    public class Settings
    {
        public int NumberOfQuickSlotsToAdd;
        public bool CenterQuickSlots;
    }

    [BepInPlugin(GUID, NAME, VERSION)]
    public class ExtendedQuickslots : BaseUnityPlugin
    {
        public const string GUID = "com.sinai.ExtendedQuickslots2";
        public const string NAME = "Extended Quickslots 2";
        public const string VERSION = "2.5";

        public static Settings settings = new Settings() 
        { 
            NumberOfQuickSlotsToAdd = 8, 
            CenterQuickSlots = true, 
        };

        private static bool fixedDictionary = false;

        private static readonly bool[] fixedPositions = new bool[2] { false, false };

        internal void Awake()
        {
            LoadSettings();

            var harmony = new Harmony(GUID);
            harmony.PatchAll();

            // Quickslot Keybindings
            for (int x = 0; x < settings.NumberOfQuickSlotsToAdd; x++)
            {
                CustomKeybindings.AddAction(
                    "QS_Instant" + (x + 12), 
                    CustomKeybindings.KeybindingsCategory.QuickSlot, 
                    CustomKeybindings.ControlType.Both, 
                    5, 
                    CustomKeybindings.InputActionType.Button);
            }
        }

        // ============== GLOBAL UPDATE ==============

        internal void Update()
        {
            // update control input
            foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby.Where(x => x.ControlledCharacter.IsLocalPlayer))
            {
                Character c = ps.ControlledCharacter;
                int playerID = c.OwnerPlayerSys.PlayerID;

                for (int x = 0; x < settings.NumberOfQuickSlotsToAdd; x++)
                {
                    if (CustomKeybindings.m_playerInputManager[playerID].GetButtonDown("QS_Instant" + (x + 12)))
                    {
                        c.QuickSlotMngr.QuickSlotInput(x + 11);
                        break;
                    }
                }
            }

            // Fix for CustomKeybindings localization text. Instead of showing "QS_Instant[number]", show a pretty-print "Quick Slot [number]".
            // The ugly format is required for the actual mapping name, so that quickslots work correctly with the game systems.

            if (LocalizationManager.Instance.IsLoading) { fixedDictionary = false; }

            if (!fixedDictionary && LocalizationManager.Instance.Loaded)
            {
                var m_generalLocalization = At.GetValue(typeof(LocalizationManager), LocalizationManager.Instance, "m_generalLocalization") as Dictionary<string, string>;

                for (int i = 0; i < settings.NumberOfQuickSlotsToAdd; i++)
                {
                    m_generalLocalization["InputAction_QS_Instant" + (i + 12)] = "Quick Slot " + (i + 9);
                }

                At.SetValue(m_generalLocalization, typeof(LocalizationManager), LocalizationManager.Instance, "m_generalLocalization");

                fixedDictionary = true;
            }
        }

        // ============== HOOKS ==============

        // Quickslot update hook, just for custom initialization

        [HarmonyPatch(typeof(QuickSlotPanel), "Update")]
        public class QuickSlotPanel_Update
        {
            [HarmonyPrefix]
            public static bool Prefix(QuickSlotPanel __instance)
            {
                var self = __instance;

                UIElement _base = self as UIElement;

                // UIElement.Update() fix:
                if ((bool)At.GetValue(typeof(UIElement), _base, "m_hideWanted") && _base.IsDisplayed)
                {
                    At.Call(_base, "OnHide", null);
                }

                // get private fields
                var m_active = (bool)At.GetValue(typeof(QuickSlotPanel), self, "m_active");
                var m_initialized = (bool)At.GetValue(typeof(QuickSlotPanel), self, "m_initialized");
                var m_quickSlotDisplays = At.GetValue(typeof(QuickSlotPanel), self, "m_quickSlotDisplays") as QuickSlotDisplay[];
                var m_lastCharacter = At.GetValue(typeof(QuickSlotPanel), self, "m_lastCharacter") as Character;

                // check init
                if ((_base.LocalCharacter == null || m_lastCharacter != _base.LocalCharacter) && m_initialized)
                {
                    m_initialized = false;
                    At.SetValue(m_initialized, typeof(QuickSlotPanel), self, "m_initialized");
                }

                // normal update when initialized
                if (m_initialized)
                {
                    if (self.UpdateInputVisibility)
                    {
                        for (int i = 0; i < m_quickSlotDisplays.Count(); i++)
                        {
                            m_quickSlotDisplays[i].SetInputTargetAlpha((!m_active) ? 0f : 1f);
                        }
                    }
                }
                // custom initialize setup
                else if (_base.LocalCharacter != null)
                {
                    At.SetValue(_base.LocalCharacter, typeof(QuickSlotPanel), self, "m_lastCharacter");
                    At.SetValue(true, typeof(QuickSlotPanel), self, "m_initialized");

                    // set quickslot display refs (orig function)
                    for (int j = 0; j < m_quickSlotDisplays.Length; j++)
                    {
                        int refSlotID = m_quickSlotDisplays[j].RefSlotID;
                        m_quickSlotDisplays[j].SetQuickSlot(_base.LocalCharacter.QuickSlotMngr.GetQuickSlot(refSlotID));
                    }

                    // if its a keyboard quickslot, set up the custom display stuff
                    if (_base.name == "Keyboard" && _base.transform.parent.name == "QuickSlot")
                    {
                        ExtendedQuickslots.SetupKeyboardQuickslotDisplay(_base, m_quickSlotDisplays);
                    }

                }

                return false;
            }
        }

        private static void SetupKeyboardQuickslotDisplay(UIElement _base, QuickSlotDisplay[] m_quickSlotDisplays)
        {
            if (fixedPositions[_base.PlayerID] == false)
            {
                //Debug.Log("Fixing position for " + _base.LocalCharacter.Name + " (Player " + _base.PlayerID + ")");

                // original mod doesn't seem to check if the stability bar belongs to the right character. I added a check here just in case.
                var stabilityDisplay = Resources.FindObjectsOfTypeAll<StabilityDisplay_Simple>()
                    .ToList()
                    .Find(x => x.LocalCharacter == _base.LocalCharacter);

                // Drop the stability bar to 1/3 of its original height
                stabilityDisplay.transform.position = new Vector3(
                    stabilityDisplay.transform.position.x,
                    stabilityDisplay.transform.position.y / 3f,
                    stabilityDisplay.transform.position.z
                );

                // Get stability bar rect bounds
                Vector3[] stabilityRect = new Vector3[4];
                stabilityDisplay.RectTransform.GetWorldCorners(stabilityRect);

                // Set new quickslot bar height
                float newY = stabilityRect[1].y + stabilityRect[0].y;
                _base.transform.parent.position = new Vector3(
                    _base.transform.parent.position.x,
                    newY,
                    _base.transform.parent.position.z
                );

                if (settings.CenterQuickSlots)
                {
                    Debug.Log("Centering quickslots " + _base.LocalCharacter.Name + " (Player " + _base.PlayerID + ")");

                    // Get first two quickslots to calculate margins.
                    List<Vector3[]> matrix = new List<Vector3[]> { new Vector3[4], new Vector3[4] };
                    for (int i = 0; i < 2; i++) { m_quickSlotDisplays[i].RectTransform.GetWorldCorners(matrix[i]); }

                    // do some math
                    var iconW = matrix[0][2].x - matrix[0][1].x;             // The width of each icon
                    var margin = matrix[1][0].x - matrix[0][2].x;            // The margin between each icon
                    var elemWidth = iconW + margin;                          // Total space per icon+margin pair
                    var totalWidth = elemWidth * m_quickSlotDisplays.Length; // How long our bar really is

                    // Re-center it based on actual content
                    _base.transform.parent.position = new Vector3(
                        totalWidth / 2.0f + elemWidth / 2.0f,
                        _base.transform.parent.position.y,
                        _base.transform.parent.position.z
                    );
                }

                fixedPositions[_base.PlayerID] = true;
            }
        }

        // Keyboard quickslot initialize hook. Add our custom slots first.

        [HarmonyPatch(typeof(KeyboardQuickSlotPanel), "InitializeQuickSlotDisplays")]
        public class KeyboardQSPanel_Init
        {
            [HarmonyPrefix]
            public static void Prefix(KeyboardQuickSlotPanel __instance)
            {
                var self = __instance;

                Array.Resize(ref self.DisplayOrder, self.DisplayOrder.Length + settings.NumberOfQuickSlotsToAdd);
                int s = 12;
                for (int x = settings.NumberOfQuickSlotsToAdd; x >= 1; x--)
                {
                    self.DisplayOrder[self.DisplayOrder.Length - x] = (QuickSlot.QuickSlotIDs)(s++);
                }
            }
        }

        // character quickslot manager awake hook. Add our custom slots first.
        [HarmonyPatch(typeof(CharacterQuickSlotManager), "Awake")]
        public class CharacterQSMgr_Awake
        {
            public static void Prefix(CharacterQuickSlotManager __instance)
            {
                var self = __instance;

                Transform m_quickslotTrans = self.transform.Find("QuickSlots");
                At.SetValue(m_quickslotTrans, typeof(CharacterQuickSlotManager), self, "m_quickslotTrans");
                for (int x = 0; x < settings.NumberOfQuickSlotsToAdd; x++)
                {
                    GameObject gameObject = new GameObject(string.Format("EXQS_{0}", x));
                    QuickSlot qs = gameObject.AddComponent<QuickSlot>();
                    qs.name = "" + (x + 12);
                    gameObject.transform.SetParent(m_quickslotTrans);
                }
            }
        }

        // ============== SETTINGS ==============

        private const string savePath = @"Mods\ExtendedQuickslots.json";

        private void LoadSettings()
        {
            try
            {
                string path = savePath;
                Settings s2 = JsonUtility.FromJson<Settings>(File.ReadAllText(path));
                if (s2 != null)
                {
                    settings = s2;
                }
            }
            catch { SaveSettings(); }
        }

        internal void OnDisable()
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            Directory.CreateDirectory(@"Mods");
            string path = savePath;
            if (File.Exists(path)) { File.Delete(path); }
            File.WriteAllText(path, JsonUtility.ToJson(settings, true));
        }
    }
}
