using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Partiality.Modloader;
using UnityEngine;
using SinAPI;
using Localizer;
using static CustomKeybindings;

namespace PrefabEditor
{
    // partiality mod loader
    public class ModBase : PartialityMod
    {
        public static GameObject _obj = null;
        public static EditorGlobal itemScript;

        public ModBase()
        {
            this.ModID = "Prefab Editor";
            this.Version = "1.6";
            this.author = "Sinai";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (_obj == null)
            {
                _obj = new GameObject("PrefabEditor");
                GameObject.DontDestroyOnLoad(_obj);
            }

            itemScript = _obj.AddComponent<EditorGlobal>();
            itemScript._base = _obj;
            itemScript.Initialise();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }
    }

    public class EditorGlobal : MonoBehaviour
    {
        public bool init = false;
        public GameObject _base;
        public Settings cfg;
        public ModGUI gui;
        public StatHelpers statHlpr;

        public Item currentTarget = null;
        public ItemTemplate currentTemplate = null;

        public bool loadedItems = false;
        public Dictionary<string, Item> allItems = new Dictionary<string, Item>();
        public Dictionary<int, ItemTemplate> origStats = new Dictionary<int, ItemTemplate>();
        public List<Item> editedItems = new List<Item>();

        public static string saveDir = @"Mods/ItemEdits";
        public static string savePath = saveDir + "/PrefabEditor.json";

        public string ItemMenuKey = "Item Editor Menu";
        public bool GlobalHideGUI = false;

        public void Initialise()
        {
            //OLogger.CreateLog(new Rect(625, 5, 450, 150), "Default", true, true);

            cfg = new Settings()
            {
                Disable_Scaling = false,
                Hide_On_Startup = false,
            };

            if (!Directory.Exists(@"Mods")) { Directory.CreateDirectory(@"Mods"); }
            if (!Directory.Exists(saveDir)) { Directory.CreateDirectory(saveDir); }
            if (File.Exists(savePath))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(savePath), cfg);
            }
            if (cfg.Hide_On_Startup) { GlobalHideGUI = true; }

            gui = _base.AddComponent<ModGUI>();
            gui.global = this;

            statHlpr = _base.AddComponent<StatHelpers>();

            // custom keybindings
            AddAction(ItemMenuKey, KeybindingsCategory.Menus, ControlType.Both, 5);

            init = true;

            // OLogger.Log("Initialised item editor");
        }

        internal void Update()
        {
            if (!init) { return; }

            if (!loadedItems && ResourcesPrefabManager.Instance.Loaded)
            {
                LoadItems();
                loadedItems = true;
            }

            if (Global.Lobby.PlayersInLobbyCount > 0)
            {
                if (m_playerInputManager[0].GetButtonDown(ItemMenuKey))
                {
                    GlobalHideGUI = !GlobalHideGUI;
                }
            }
        }

        public void LoadItems()
        {
            for (int i = 0; i < ResourcesPrefabManager.AllPrefabs.Count; i++)
            {
                if (ResourcesPrefabManager.AllPrefabs[i] is GameObject gameObject)
                {
                    Item item = gameObject.GetComponent<Item>();

                    // items below 2000000 are buggy dev items, not recommended to use them (except duel baton)
                    if (item == null || (item.ItemID < 1999999 && item.ItemID != 350) || item.ItemID == 9000000) { continue; }

                    // unsupported types
                    if (item is ItemContainer || item is ItemFragment || item is Quest) { continue; }

                    allItems.Add(item.Name + " (" + item.gameObject.name + ")", item);
                }
            }

            // load existing edits
            string[] filePaths = Directory.GetFiles(saveDir, "*.json");

            foreach (string s in filePaths)
            {
                ItemTemplate edits = new ItemTemplate();

                string json = File.ReadAllText(s);
                JsonUtility.FromJsonOverwrite(json, edits);

                if (ResourcesPrefabManager.Instance.GetItemPrefab(edits.ItemID) is Item item)
                {
                    SetItemPrefab(item);
                    ApplyEdits(currentTemplate, item);
                }
            }

            currentTarget = null;
            currentTemplate = null;
        }

        public void SaveTemplate(ItemTemplate template, Item item)
        {
            string json = JsonUtility.ToJson(template, true);

            if (item is Skill)
            {
                if (item is PassiveSkill)
                {
                    int i = 0;
                    Dictionary<string, string> toAppend = new Dictionary<string, string>();
                    foreach (PassiveEffect pe in (template as PassiveTemplate).Effects)
                    {
                        toAppend.Add("PassiveEffects_" + i, JsonUtility.ToJson(pe, true));
                        i++;
                    }

                    json = Jt.AppendJsonList(json, toAppend);
                }
                else
                {
                    int i = 0;
                    Dictionary<string, string> toAppend = new Dictionary<string, string>();
                    foreach (SkillDamage damages in (template as SkillTemplate).Damages)
                    {
                        toAppend.Add("SkillDmgs_" + i, JsonUtility.ToJson(damages, true));
                        i++;
                    }

                    json = Jt.AppendJsonList(json, toAppend);
                }
            }

            string path = saveDir + @"\" + item.gameObject.name + ".json";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllText(path, json);
        }


        // =================== main two functions: set item and apply item =================== //

        public void SetItemPrefab(Item item)
        {
            // OLogger.Log("setting item prefab");

            currentTarget = item;

            gui.editFields.Clear();
            for (int i = 0; i < 200; i++)
            {
                gui.editFields.Add("");
            }

            string path = saveDir + "/" + item.gameObject.name + ".json";
            if (File.Exists(path))
            {
                ItemTemplate edits = new ItemTemplate();

                string json = File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(json, edits);

                if (edits != null)
                {
                    // fix for latest version
                    if (edits.m_activateEffectAnimType == 0 || edits.MobileCastMovementMult == 0)
                    {
                        edits.m_activateEffectAnimType = (int)item.ActivateEffectAnimType;
                        edits.CastModifier = (int)item.CastModifier;
                        edits.MobileCastMovementMult = item.MobileCastMovementMult;
                    }

                    if (item is Equipment)
                    {
                        EquipmentTemplate equipEdits = new EquipmentTemplate();
                        JsonUtility.FromJsonOverwrite(json, equipEdits);
                        edits = equipEdits;

                        if (item is Weapon)
                        {
                            WeaponTemplate weaponEdits = new WeaponTemplate();
                            JsonUtility.FromJsonOverwrite(json, weaponEdits);
                            edits = weaponEdits;
                        }
                    }
                    else if (item is Skill)
                    {
                        if (item is PassiveSkill)
                        {
                            PassiveTemplate passiveEdits = new PassiveTemplate();
                            JsonUtility.FromJsonOverwrite(json, passiveEdits);

                            List<PassiveEffect> effects = new List<PassiveEffect>();

                            List<object> effects2 = Jt.JsonDeAppender(typeof(PassiveEffect), "PassiveEffects", ref json);
                            foreach (object o in effects2)
                            {
                                if (o is PassiveEffect pe) { effects.Add(pe); }
                            }
                            passiveEdits.Effects = effects;

                            edits = passiveEdits;
                        }
                        else
                        {
                            SkillTemplate skillEdits = new SkillTemplate();
                            JsonUtility.FromJsonOverwrite(json, skillEdits);

                            // json fix for skill damages
                            List<SkillDamage> damages = new List<SkillDamage>();

                            // damages = SkillDmgJsonFix(ref json);
                            List<object> damages2 = Jt.JsonDeAppender(typeof(SkillDamage), "SkillDmgs", ref json);
                            foreach (object o in damages2)
                            {
                                if (o is SkillDamage d) { damages.Add(d); }
                            }

                            skillEdits.Damages = damages;

                            edits = skillEdits;
                        }
                    }
                }

                currentTemplate = edits;
            }
            else
            {
                currentTemplate = NewTemplate(item);
            }

            // set gui weapon type drop down
            if (item is Weapon)
            {
                for (int i = 0; i < statHlpr.weaponTypes.Count; i++)
                {
                    if (statHlpr.weaponTypes.ElementAt(i).Value == (Weapon.WeaponType)(currentTemplate as WeaponTemplate).WeaponType)
                    {
                        gui.WeaponDrop.selected = i;
                        break;
                    }
                }
            }

            // set skill cast stuff
            if (item is Skill skill)
            {
                gui.AnimTypeDrop.selected = (int)skill.ActivateEffectAnimType;
            }
        }

        public ItemTemplate NewTemplate(Item item)
        {
            ItemTemplate _template = new ItemTemplate();

            _template = SetupNewTemplate(_template, item);

            return _template;
        }

        public ItemTemplate SetupNewTemplate(ItemTemplate _template, Item item)
        {
            _template.Name = item.Name;
            _template.ItemID = item.ItemID;
            _template.Description = item.Description;

            _template.m_activateEffectAnimType = (int)item.ActivateEffectAnimType;
            _template.CastModifier = (int)item.CastModifier;
            _template.MobileCastMovementMult = item.MobileCastMovementMult;

            if (item.Stats is ItemStats stats)
            {
                if (At.GetValue(typeof(ItemStats), item.Stats, "m_baseValue") is int baseValue
                && At.GetValue(typeof(ItemStats), item.Stats, "m_rawWeight") is float rawWeight)
                {
                    _template.MaxDurability = item.Stats.MaxDurability;
                    _template.m_baseValue = baseValue;
                    _template.m_rawWeight = rawWeight;
                }

                if (item.Stats is EquipmentStats eStats && item is Equipment equipment)
                {
                    EquipmentTemplate _equipTemplate = new EquipmentTemplate();
                    At.InheritBaseValues(_equipTemplate, _template);

                    _equipTemplate.m_damageAttack = (float[])At.GetValue(typeof(EquipmentStats), eStats, "m_damageAttack");
                    _equipTemplate.m_damageResistance = (float[])At.GetValue(typeof(EquipmentStats), eStats, "m_damageResistance");
                    _equipTemplate.m_damageProtection = (float[])At.GetValue(typeof(EquipmentStats), eStats, "m_damageProtection");
                    _equipTemplate.m_impactResistance = (float)At.GetValue(typeof(EquipmentStats), eStats, "m_impactResistance");
                    _equipTemplate.m_manaUseModifier = (float)At.GetValue(typeof(EquipmentStats), eStats, "m_manaUseModifier");
                    _equipTemplate.m_maxHealthBonus = (float)At.GetValue(typeof(EquipmentStats), eStats, "m_movementPenalty");
                    _equipTemplate.m_movementPenalty = (float)At.GetValue(typeof(EquipmentStats), eStats, "m_staminaUsePenalty");
                    _equipTemplate.m_pouchCapacityBonus = (float)At.GetValue(typeof(EquipmentStats), eStats, "m_pouchCapacityBonus");
                    _equipTemplate.m_staminaUsePenalty = (float)At.GetValue(typeof(EquipmentStats), eStats, "m_maxHealthBonus");
                    _equipTemplate.m_coldProtection = (float)At.GetValue(typeof(EquipmentStats), eStats, "m_coldProtection");
                    _equipTemplate.m_heatProtection = (float)At.GetValue(typeof(EquipmentStats), eStats, "m_heatProtection");

                    _template = _equipTemplate;

                    if (stats is WeaponStats wStats)
                    {
                        WeaponTemplate _weaponTemplate = new WeaponTemplate();
                        At.InheritBaseValues(_weaponTemplate, _equipTemplate);

                        _weaponTemplate.Attacks = wStats.Attacks;
                        _weaponTemplate.AttackSpeed = wStats.AttackSpeed;
                        _weaponTemplate.BaseDamage = wStats.BaseDamage;
                        _weaponTemplate.Impact = wStats.Impact;

                        Weapon weapon = item as Weapon;
                        _weaponTemplate.WeaponType = (int)weapon.Type;

                        _weaponTemplate.hitEffects = new List<string>();

                        if (item.transform.Find("HitEffects") is Transform hiteffects)
                        {
                            if (hiteffects.GetComponents<AddStatusEffectBuildUp>() is AddStatusEffectBuildUp[] statuses)
                            {
                                foreach (AddStatusEffectBuildUp status in statuses)
                                {
                                    _weaponTemplate.hitEffects.Add(status.Status.IdentifierName);
                                }
                            }
                        }

                        if (equipment.EquipSlot == EquipmentSlot.EquipmentSlotIDs.LeftHand || equipment.EquipSlot == EquipmentSlot.EquipmentSlotIDs.RightHand)
                        {
                            if (equipment.TwoHand == Equipment.TwoHandedType.None)
                            {
                                _weaponTemplate.TwoHand = false;
                            }
                            else
                            {
                                _weaponTemplate.TwoHand = true;
                            }
                            if (equipment.EquipSlot == EquipmentSlot.EquipmentSlotIDs.LeftHand)
                            {
                                _weaponTemplate.OffHanded = true;
                            }
                        }

                        _template = _weaponTemplate;
                    }
                }
            }

            if (item is Skill skill)
            {
                if (skill is PassiveSkill passive)
                {
                    PassiveTemplate _passiveTemplate = new PassiveTemplate();
                    At.InheritBaseValues(_passiveTemplate, _template);

                    _passiveTemplate.Effects = new List<PassiveEffect>();

                    if (passive.GetComponentsInChildren<Effect>().ToList() is List<Effect> effects)
                    {
                        foreach (Effect effect in effects.Where(x => x is AffectStat || x is AffectHealth || x is AffectMana || x is AffectStamina))
                        {
                            PassiveEffect effectTemplate = new PassiveEffect();
                            effectTemplate.Type = effect.GetType().ToString();
                            effectTemplate.AffectedStat = -1;

                            if (effect is AffectMana aMana)
                            {
                                effectTemplate.IsModifier = aMana.IsModifier;
                                effectTemplate.Value = aMana.Value;
                            }
                            else if (effect is AffectHealth aHealth)
                            {
                                effectTemplate.IsModifier = aHealth.IsModifier;
                                effectTemplate.Value = aHealth.AffectQuantity;
                            }
                            else if (effect is AffectStamina aStamina)
                            {
                                effectTemplate.IsModifier = false;
                                effectTemplate.Value = aStamina.AffectQuantity;
                            }
                            else if (effect is AffectStat aStat)
                            {
                                if (int.TryParse(aStat.AffectedStat.SelectorValue.Value, out int value))
                                {
                                    effectTemplate.AffectedStat = value;
                                    effectTemplate.IsModifier = aStat.IsModifier;
                                    effectTemplate.Value = aStat.Value;
                                }
                            }

                            _passiveTemplate.Effects.Add(effectTemplate);
                        }
                    }

                    _template = _passiveTemplate;
                }
                else
                {
                    SkillTemplate _skillTemplate = new SkillTemplate();
                    At.InheritBaseValues(_skillTemplate, _template);

                    _skillTemplate.Cooldown = skill.Cooldown;
                    _skillTemplate.ManaCost = skill.ManaCost;
                    _skillTemplate.StaminaCost = skill.StaminaCost;

                    _skillTemplate.Damages = new List<SkillDamage>();

                    foreach (PunctualDamage comp in item.GetComponentsInChildren<PunctualDamage>(true))
                    {
                        SkillDamage damages = new SkillDamage();
                        damages = ParsePunctualDamage(comp);
                        damages.ComponentType = "Basic";

                        _skillTemplate.Damages.Add(damages);
                    }

                    foreach (ShootBlast blast in skill.GetComponentsInChildren<ShootBlast>())
                    {
                        if (blast.BaseBlast is Blast baseBlast)
                        {
                            foreach (PunctualDamage blastDmg in baseBlast.GetComponentsInChildren<PunctualDamage>())
                            {
                                SkillDamage damages = new SkillDamage();
                                damages = ParsePunctualDamage(blastDmg);

                                damages.transform = blast.name;
                                damages.ComponentType = "ShootBlast";

                                _skillTemplate.Damages.Add(damages);
                            }
                        }
                    }

                    foreach (ShootProjectile proj in skill.GetComponentsInChildren<ShootProjectile>())
                    {
                        if (proj.BaseProjectile is Projectile baseProj)
                        {
                            foreach (PunctualDamage projDmg in baseProj.GetComponentsInChildren<PunctualDamage>())
                            {
                                SkillDamage damages = new SkillDamage();
                                damages = ParsePunctualDamage(projDmg);

                                damages.transform = proj.name;
                                damages.ComponentType = "ShootProjectile";

                                _skillTemplate.Damages.Add(damages);
                            }
                        }
                    }

                    _template = _skillTemplate;
                }
            }

            return _template;
        }

        public SkillDamage ParsePunctualDamage(PunctualDamage comp)
        {
            SkillDamage damages = new SkillDamage
            {
                PunctualType = comp.GetType().ToString(),
                transform = comp.transform.name,
                Damages = comp.Damages,
                Impact = comp.Knockback,
                AddWeaponDamage = false
            };

            if (comp is WeaponDamage wComp)
            {
                damages.AddWeaponDamage = true;
                damages.DamageMultiplier = wComp.WeaponDamageMult;
                damages.ImpactMultiplier = wComp.WeaponKnockbackMult;
                damages.DamageOverride = (int)wComp.OverrideDType;
            }

            return damages;
        }

        public void ApplyEdits(ItemTemplate template, Item item)
        {
            if (item == null || template == null)
            {
                return;
            }

            if (!origStats.ContainsKey(template.ItemID))
            {
                ItemTemplate orig = NewTemplate(item);
                origStats.Add(template.ItemID, orig);
            }

            SetNameAndDesc(item, template.Name, template.Description);

            item.MobileCastMovementMult = template.MobileCastMovementMult;
            item.CastModifier = (Character.SpellCastModifier)template.CastModifier;
            At.SetValue((Character.SpellCastType)template.m_activateEffectAnimType, typeof(Item), item, "m_activateEffectAnimType");

            if (item.Stats is ItemStats stats)
            {
                item.Stats.MaxDurability = template.MaxDurability;
                At.SetValue(template.m_baseValue, typeof(ItemStats), item.Stats, "m_baseValue");
                At.SetValue(template.m_rawWeight, typeof(ItemStats), item.Stats, "m_rawWeight");

                if (item.Stats is EquipmentStats)
                {
                    ApplyEquipmentStats(template as EquipmentTemplate, stats);

                    if (stats is WeaponStats wStats)
                    {
                        ApplyWeaponStats(template as WeaponTemplate, wStats, item);
                    }
                }
            }

            if (item is Skill)
            {
                if (item is PassiveSkill)
                {
                    ApplyPassiveStats(template as PassiveTemplate, item as PassiveSkill);
                }
                else
                {
                    ApplySkillStats(template as SkillTemplate, item as Skill);
                }
            }

            if (!editedItems.Contains(item.ItemID))
            {
                editedItems.Add(item);
            }
        }

        private void SetNameAndDesc(Item item, string name, string desc)
        {
            ItemLocalization loc = new ItemLocalization(name, desc);

            At.SetValue(name, typeof(Item), item, "m_name");

            if (At.GetValue(typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalization") is Dictionary<int, ItemLocalization> dict)
            {
                if (dict.ContainsKey(item.ItemID))
                {
                    dict[item.ItemID] = loc;
                }
                else
                {
                    dict.Add(item.ItemID, loc);
                }
                At.SetValue(dict, typeof(LocalizationManager), LocalizationManager.Instance, "m_itemLocalization");
            }
        }

        private void ApplyPassiveStats(PassiveTemplate template, PassiveSkill passive)
        {
            List<Effect> effectComps = passive.GetComponentsInChildren<Effect>().Where(x => x is AffectStat || x is AffectMana || x is AffectHealth || x is AffectStamina).ToList();

            int i = 0;
            foreach (Effect effect in effectComps)
            {
                if (template.Effects[i].Type == effect.GetType().ToString())
                {
                    PassiveEffect effectTemplate = template.Effects[i];
                    if (effect is AffectStat aStat)
                    {
                        At.SetValue(effectTemplate.Value, typeof(AffectStat), aStat, "Value");
                        At.SetValue(effectTemplate.IsModifier, typeof(AffectStat), aStat, "IsModifier");
                        At.SetValue(new TagSourceSelector(new Tag(effectTemplate.AffectedStat.ToString())), typeof(AffectStat), aStat, "AffectedStat");
                    }
                    if (effect is AffectMana aMana)
                    {
                        At.SetValue(effectTemplate.Value, typeof(AffectMana), aMana, "Value");
                        At.SetValue(effectTemplate.IsModifier, typeof(AffectMana), aMana, "IsModifier");
                    }
                    if (effect is AffectHealth aHealth)
                    {
                        At.SetValue(effectTemplate.Value, typeof(AffectHealth), aHealth, "AffectQuantity");
                        At.SetValue(effectTemplate.IsModifier, typeof(AffectHealth), aHealth, "IsModifier");
                    }
                    else if (effect is AffectStamina aStam)
                    {
                        At.SetValue(effectTemplate.Value, typeof(AffectStamina), aStam, "AffectQuantity");
                    }
                }

                i++;
            }
        }

        private void ApplySkillStats(SkillTemplate template, Skill skill)
        {
            At.SetValue(template.Cooldown, typeof(Skill), skill, "Cooldown");
            At.SetValue(template.ManaCost, typeof(Skill), skill, "ManaCost");
            At.SetValue(template.StaminaCost, typeof(Skill), skill, "StaminaCost");

            foreach (SkillDamage damages in template.Damages)
            {
                if (skill.transform.FindInAllChildren(damages.transform) is Transform t)
                {
                    PunctualDamage comp = null;

                    if (damages.ComponentType == "Basic")
                    {
                        comp = t.GetComponent<PunctualDamage>();
                    }
                    else if (damages.ComponentType == "ShootBlast")
                    {
                        if (t.GetComponent<ShootBlast>() is ShootBlast blast && blast.BaseBlast != null
                            && blast.BaseBlast.GetComponentInChildren<PunctualDamage>() is PunctualDamage comp2)
                        {
                            comp = comp2;
                        }
                    }
                    else if (damages.ComponentType == "ShootProjectile")
                    {
                        if (t.GetComponent<ShootProjectile>() is ShootProjectile proj && proj.BaseProjectile != null
                            && proj.BaseProjectile.GetComponentInChildren<PunctualDamage>() is PunctualDamage comp2)
                        {
                            comp = comp2;
                        }
                    }

                    if (comp)
                    {
                        At.SetValue(damages.Damages, typeof(PunctualDamage), comp, "Damages");
                        At.SetValue(damages.Impact, typeof(PunctualDamage), comp, "Knockback");

                        if (comp is WeaponDamage wComp)
                        {
                            At.SetValue(damages.DamageMultiplier, typeof(WeaponDamage), wComp, "WeaponDamageMult");
                            At.SetValue(damages.ImpactMultiplier, typeof(WeaponDamage), wComp, "WeaponKnockbackMult");

                            if (damages.DamageOverride > -1 && damages.DamageOverride < 10)
                            {
                                DamageType.Types type = (DamageType.Types)damages.DamageOverride;
                                At.SetValue(type, typeof(WeaponDamage), wComp, "OverrideDType");
                            }
                        }
                    }
                }
            }
        }

        private void ApplyEquipmentStats(EquipmentTemplate template, ItemStats stats)
        {
            At.SetValue(template.m_damageAttack, typeof(EquipmentStats), stats, "m_damageAttack");
            At.SetValue(template.m_damageResistance, typeof(EquipmentStats), stats, "m_damageResistance");
            At.SetValue(template.m_damageProtection, typeof(EquipmentStats), stats, "m_damageProtection");
            At.SetValue(template.m_impactResistance, typeof(EquipmentStats), stats, "m_impactResistance");
            At.SetValue(template.m_manaUseModifier, typeof(EquipmentStats), stats, "m_manaUseModifier");
            At.SetValue(template.m_maxHealthBonus, typeof(EquipmentStats), stats, "m_maxHealthBonus");
            At.SetValue(template.m_movementPenalty, typeof(EquipmentStats), stats, "m_movementPenalty");
            At.SetValue(template.m_pouchCapacityBonus, typeof(EquipmentStats), stats, "m_pouchCapacityBonus");
            At.SetValue(template.m_staminaUsePenalty, typeof(EquipmentStats), stats, "m_staminaUsePenalty");
            At.SetValue(template.m_coldProtection, typeof(EquipmentStats), stats, "m_coldProtection");
            At.SetValue(template.m_heatProtection, typeof(EquipmentStats), stats, "m_heatProtection");
        }

        private void ApplyWeaponStats(WeaponTemplate template, WeaponStats wStats, Item item)
        {
            Weapon.WeaponType type = (Weapon.WeaponType)template.WeaponType;

            for (int i = 0; i < wStats.Attacks.Count(); i++)
            {
                List<float> stepDamage = new List<float>();
                float stepImpact = template.Impact;
                foreach (DamageType dtype in template.BaseDamage.List)
                {
                    stepDamage.Add(dtype.Damage);
                }

                statHlpr.SetScaledDamages(type, i, ref stepDamage, ref stepImpact);

                template.Attacks[i].Damage = stepDamage;
                template.Attacks[i].Knockback = stepImpact;
            }

            wStats.Attacks = template.Attacks;
            wStats.AttackSpeed = template.AttackSpeed;
            wStats.BaseDamage = template.BaseDamage;
            wStats.Impact = template.Impact;

            Weapon weapon = item as Weapon;
            At.SetValue(type, typeof(Weapon), weapon, "Type");

            var equip = item as Equipment;
            // handle two-hand / off-hand type
            if (equip.EquipSlot == EquipmentSlot.EquipmentSlotIDs.LeftHand || equip.EquipSlot == EquipmentSlot.EquipmentSlotIDs.RightHand)
            {
                if (template.OffHanded)
                    equip.EquipSlot = EquipmentSlot.EquipmentSlotIDs.LeftHand;
                else
                    equip.EquipSlot = EquipmentSlot.EquipmentSlotIDs.RightHand;

                if (template.TwoHand)
                {
                    if (template.WeaponType == 200)
                        equip.TwoHand = Equipment.TwoHandedType.TwoHandedLeft;
                    else
                        equip.TwoHand = Equipment.TwoHandedType.TwoHandedRight;
                }
                else
                    equip.TwoHand = Equipment.TwoHandedType.None;
            }

            // custom status effects
            if (weapon.transform.Find("HitEffects") is Transform origEffects)
            {
                origEffects.transform.parent = null;
                //OLogger.Log("destroying existing hiteffects");
                Destroy(origEffects.gameObject);
            }

            GameObject hiteffects = new GameObject() { name = "HitEffects" };
            hiteffects.transform.parent = item.transform;

            foreach (string s in template.hitEffects)
            {
                // OLogger.Log("adding status " + s);
                AddStatusEffectBuildUp newstatus = new AddStatusEffectBuildUp()
                {
                    BuildUpValue = 60.0f,
                    Status = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(s)
                };
                hiteffects.gameObject.AddComponent(newstatus);
            }
        }

        internal void OnDisable()
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            if (!Directory.Exists(@"Mods"))
            {
                Directory.CreateDirectory(@"Mods");
            }

            if (File.Exists(@"Mods\SimpleItemSettings.json"))
            {
                File.Delete(@"Mods\SimpleItemSettings.json");
            }
            File.WriteAllText(@"Mods\SimpleItemSettings.json", JsonUtility.ToJson(cfg, true));
        }

        // ------------------------- OTHER ------------------------ //

        public void SpawnObject()
        {
            var localPlayer = CharacterManager.Instance.GetFirstLocalCharacter();
            Vector3 vector = localPlayer.CenterPosition + localPlayer.transform.forward * 1.5f;
            if (Physics.Raycast(vector, localPlayer.transform.up * -1f, out RaycastHit raycastHit, 10f, Global.LargeEnvironmentMask))
            {
                Quaternion localRotation = localPlayer.transform.localRotation;
                Item spawnedItem = ItemManager.Instance.GenerateItemNetwork(currentTemplate.ItemID);
                spawnedItem.transform.position = vector;
                spawnedItem.transform.rotation = localRotation;
            }
        }
    }

    // =========================== templates =============================
    // intermediate class templates to make for easier JSON parsing

    public class ItemTemplate
    {
        // Item
        public int ItemID;
        public string Name;
        public string Description;

        // base item fields
        public int m_activateEffectAnimType;
        public int CastModifier;
        public float MobileCastMovementMult;

        // ItemStats                            // ID's for the EditFields (not strict values, just for my reference)
        public int MaxDurability;               // 0 = durability
        public int m_baseValue;                 // 1 = base value
        public float m_rawWeight;               // 2 = weight
    }

    public class EquipmentTemplate : ItemTemplate
    {

        // EquipmentStats
        public float[] m_damageAttack;          // 3,4,5,6,7,8 = damage bonus
        public float[] m_damageResistance;      // 9,10,11,12,13,14 = damage resistance
        public float[] m_damageProtection;      // 15 = prot
        public float m_impactResistance;        // 16 = imp resist
        public float m_manaUseModifier;         // 17 = mana modifier
        public float m_maxHealthBonus;          // 18 = max hp bonus
        public float m_movementPenalty;         // 19 = movement pen
        public float m_pouchCapacityBonus;      // 20 = pouch bonus
        public float m_staminaUsePenalty;       // 21 = stam pen
        public float m_coldProtection;          // 22 = cold protection
        public float m_heatProtection;          // 23 = heat protection
    }

    public class WeaponTemplate : EquipmentTemplate
    {
        // WeaponStats
        public float AttackSpeed;               // 24 = attack speed
        public float Impact;                    // 25 = weapon impact
        public DamageList BaseDamage;           // 26,27,28,29,30,31 = weapon base damage
        public WeaponStats.AttackData[] Attacks;

        // weapon
        public int WeaponType;
        public bool TwoHand;
        public bool OffHanded;

        // add status effect buildups
        public List<string> hitEffects;

    }

    public class SkillTemplate : ItemTemplate
    {
        public float Cooldown;
        public float StaminaCost;
        public float ManaCost;

        public List<SkillDamage> Damages;
    }

    public class SkillDamage
    {
        public string transform = "";
        public string PunctualType = ""; // one of: PunctualDamage, WeaponDamage 
        public string ComponentType = ""; // one of: Basic, Blast, Projectile

        public DamageType[] Damages;
        public bool AddWeaponDamage = false;
        public float DamageMultiplier = 1.0f;
        public int DamageOverride = -1;
        public float Impact;
        public float ImpactMultiplier = 1.0f;
    }

    public class PassiveTemplate : ItemTemplate
    {
        public List<PassiveEffect> Effects;
    }

    public class PassiveEffect
    {
        public string Type;
        public float Value;
        public int AffectedStat;
        public bool IsModifier;
    }

    public class Settings
    {
        public bool Disable_Scaling;
        public bool Hide_On_Startup;
    }
}
