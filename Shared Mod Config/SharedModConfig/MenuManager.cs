using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SideLoader;
using UnityEngine.EventSystems;
using static CustomKeybindings;

namespace SharedModConfig
{
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance;

        private string MenuKey = "Shared Mod Config Menu";

        // UI canvas
        private GameObject m_ConfigCanvas;
        private bool m_lastCanvasState;

        private Button m_closeButton;

        private GameObject m_modListHolder;
        private GameObject m_ModListButtonPrefab;

        private GameObject m_SettingsHolder;
        private GameObject m_currentActiveSettings;
        private GameObject m_SettingsPrefab;

        private bool m_initDone;
        public bool IsInitDone()
        {
            return m_initDone;
        }

        internal void Awake()
        {
            Instance = this;
            StartCoroutine(SetupCoroutine());

            AddAction(MenuKey, KeybindingsCategory.Menus, ControlType.Both, 5, InputActionType.Button);
        }

        internal void Update()
        {
            foreach (SplitPlayer player in SplitScreenManager.Instance.LocalPlayers)
            {
                if (m_playerInputManager[player.RewiredID].GetButtonDown(MenuKey))
                {
                    bool active = m_ConfigCanvas.activeSelf;
                    m_ConfigCanvas.SetActive(!active);
                }
            }

            if (Global.Lobby.PlayersInLobbyCount > 0 && !NetworkLevelLoader.Instance.IsGameplayPaused)
            {
                MenuMouseFix();
            }
        }

        private IEnumerator SetupCoroutine()
        {
            while (!SL.Instance.IsInitDone())
            {
                yield return new WaitForSeconds(0.1f);
            }

            SetupCanvas();

            m_initDone = true;
        }

        private void SetupCanvas()
        {
            // Debug.Log(ModBase.ModName + " started, version: " + ModBase.ModVersion);

            var bundle = SL.Instance.LoadedBundles["sharedmodconfig"];

            if (bundle.LoadAsset("SharedModConfigCanvas") is GameObject canvasAsset)
            {
                m_ConfigCanvas = Instantiate(canvasAsset);
                DontDestroyOnLoad(m_ConfigCanvas);

                var headerHolder = m_ConfigCanvas.transform.Find("Panel").Find("Header_Holder");

                // close button (X)
                m_closeButton = headerHolder.transform.Find("CloseButton").gameObject.GetComponent<Button>();
                m_closeButton.onClick.AddListener(CloseButton);

                // mod list and buttons
                m_modListHolder = headerHolder.Find("ModList_Holder").Find("ModList_Viewport").Find("ModList_Content").gameObject;
                m_ModListButtonPrefab = m_modListHolder.transform.Find("Button_Holder").gameObject;
                var modlistButton = m_ModListButtonPrefab.GetComponent<Button>();
                modlistButton.onClick.AddListener(ModListButton);
                m_ModListButtonPrefab.SetActive(false);

                // mod settings template
                m_SettingsHolder = m_ConfigCanvas.transform.Find("Panel").Find("Settings_Holder").gameObject;

                m_SettingsPrefab = m_SettingsHolder.transform.Find("Settings_Prefab").gameObject;
                m_SettingsPrefab.SetActive(false);

                // disable canvas
                m_ConfigCanvas.SetActive(false);
            }
        }

        private void CloseButton()
        {
            m_ConfigCanvas.SetActive(false);
        }

        private void ModListButton()
        {
            var buttontext = EventSystem.current.currentSelectedGameObject.GetComponentInChildren<Text>().text;
            Debug.Log(buttontext);
            if (m_SettingsPrefab.transform.parent.Find(buttontext) is Transform t)
            {
                if (m_currentActiveSettings)
                {
                    m_currentActiveSettings.SetActive(false);
                }

                t.gameObject.SetActive(true);
                m_currentActiveSettings = t.gameObject;
            }
            else
            {
                Debug.LogError("[SharedModConfig] Error! Could not find transform " + buttontext);
            }
        }

        public void AddConfig(ModConfig config)
        {
            // add button to mod list
            var newButton = Instantiate(m_ModListButtonPrefab);
            newButton.name = config.ModName;
            newButton.transform.SetParent(m_ModListButtonPrefab.transform.parent, false);

            var text = newButton.GetComponentInChildren<Text>();
            text.text = config.ModName;

            var button = newButton.GetComponentInChildren<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(ModListButton);

            newButton.SetActive(true);

            // setup canvas
            var newCanvas = Instantiate(m_SettingsPrefab);
            newCanvas.name = config.ModName;
            newCanvas.transform.SetParent(m_SettingsHolder.transform, false);

            config.m_linkedPanel = newCanvas;

            var title = newCanvas.transform.Find("Title").GetComponent<Text>();
            title.text = config.ModName;

            var contentHolder = newCanvas.transform.Find("SettingsScroll_Holder").Find("Settings_Viewport").Find("Settings_Content");

            var boolPrefab = contentHolder.Find("Toggle_Holder").gameObject;
            var stringPrefab = contentHolder.Find("InputField_Holder").gameObject;
            var floatPrefab = contentHolder.Find("HSlider_Holder").gameObject;

            // setup settings
            foreach (BBSetting setting in config.Settings)
            {
                switch (setting.GetType().Name)
                {
                    case "BoolSetting":
                    case "SharedModConfig.BoolSetting":
                        SetupBoolSetting(contentHolder, boolPrefab, setting as BoolSetting);
                        break;
                    case "StringSetting":
                    case "SharedModConfig.StringSetting":
                        SetupStringSetting(contentHolder, stringPrefab, setting as StringSetting);
                        break;
                    case "FloatSetting":
                    case "SharedModConfig.FloatSetting":
                        SetupFloatSetting(contentHolder, floatPrefab, setting as FloatSetting);
                        break;
                    default:
                        Debug.LogError("[SharedModConfig] Could not parse BBSetting of type " + setting.GetType().Name);
                        break;
                }
            }

            // cleanup the prefab templates
            Destroy(boolPrefab);
            Destroy(stringPrefab);
            Destroy(floatPrefab);
        }

        private void SetupBoolSetting(Transform contentHolder, GameObject prefab, BoolSetting setting)
        {
            var newPrefab = SetupBasicSetting(contentHolder, prefab, setting);

            var Toggle = newPrefab.GetComponentInChildren<Toggle>();
            var Label = newPrefab.GetComponentInChildren<Text>();

            Toggle.isOn = setting.m_value;
            Label.text = setting.Description ?? setting.Name;
        }

        private void SetupFloatSetting(Transform contentHolder, GameObject prefab, FloatSetting setting)
        {
            var newPrefab = SetupBasicSetting(contentHolder, prefab, setting);

            var label = newPrefab.transform.Find("Label").GetComponent<Text>();
            label.text = setting.Description ?? setting.Name;

            var slider = newPrefab.GetComponentInChildren<Slider>();
            slider.minValue = setting.MinValue;
            slider.maxValue = setting.MaxValue;
            slider.value = setting.m_value;
            if (setting.RoundTo >= 0)
            {
                slider.value = (float)Math.Round(setting.m_value, setting.RoundTo);
            }

            var textvalue = slider.transform.parent.Find("SliderValue").GetComponent<Text>();
            string s = setting.m_value + (setting.ShowPercent ? "%" : "");
            textvalue.text = s;
        }

        private void SetupStringSetting(Transform contentHolder, GameObject prefab, StringSetting setting)
        {
            var newPrefab = SetupBasicSetting(contentHolder, prefab, setting);

            var Label = newPrefab.transform.Find("Label").GetComponent<Text>();
            Label.text = setting.Description ?? setting.Name;

            var inputField = newPrefab.GetComponentInChildren<InputField>();
            inputField.text = setting.m_value;
        }

        private GameObject SetupBasicSetting(Transform contentHolder, GameObject prefab, BBSetting setting)
        {
            var newPrefab = Instantiate(prefab);
            newPrefab.transform.SetParent(contentHolder, false);
            setting.LinkedGameObject = newPrefab;
            newPrefab.SetActive(true);
            return newPrefab;
        }

        private void MenuMouseFix()
        {
            if (m_lastCanvasState != m_ConfigCanvas.activeSelf)
            {
                m_lastCanvasState = m_ConfigCanvas.activeSelf;

                Character c = CharacterManager.Instance.GetFirstLocalCharacter();

                if (c.CharacterUI.PendingDemoCharSelectionScreen is Panel panel)
                {
                    if (m_lastCanvasState)
                        panel.Show();
                    else
                        panel.Hide();
                }
                else if (m_lastCanvasState)
                {
                    GameObject obj = new GameObject();
                    obj.transform.parent = c.transform;
                    obj.SetActive(true);

                    Panel newPanel = obj.AddComponent<Panel>();
                    At.SetValue(newPanel, typeof(CharacterUI), c.CharacterUI, "PendingDemoCharSelectionScreen");
                    newPanel.Show();
                }
            }
        }
    }
}
