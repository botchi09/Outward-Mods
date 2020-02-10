using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace OutwardExplorer
{
    public class DumperScript : MonoBehaviour
    {
        // public ExplorerScript script;
        public DumperUtils utils;
        public DumperSorter sorter;

        public string CurrentScenePretty = "";
        public bool EnemiesAggressive = false;

        public string saveDir = @"Dumps";
        //public Dictionary<string, string> Folders;

        public bool abortLoop = false;

        public void Init()
        {
            On.LoadingFade.Update += new On.LoadingFade.hook_Update(TitleHook);
            On.AICEnemyDetection.Update += new On.AICEnemyDetection.hook_Update(AIEnemyDetectionHook);
            On.AIESwitchState.SwitchState += new On.AIESwitchState.hook_SwitchState(AISwitchStateHook);

            utils.SetupDirectories();
        }

        public Dictionary<string, string> Folders = new Dictionary<string, string> // declare parents before subfolders
        {
            {"Prefabs", "Dumps/Prefabs" },
            {"Items", "Dumps/Prefabs/Items" },
            {"Effects", "Dumps/Prefabs/Effects" },
            {"Recipes", "Dumps/Prefabs/Recipes" },
            {"Droptables", "Dumps/Prefabs/Droptables" },

            {"Scenes", "Dumps/Scenes" }
        };


        // ====================================== prefab dumps =========================================

        public IEnumerator DumpItemPrefabs()
        {
            if (ResourcesPrefabManager.Instance.Loaded && ResourcesPrefabManager.AllPrefabs != null && ResourcesPrefabManager.AllPrefabs.Count > 0)
            {
                Dictionary<string,GameObject> allPrefabs = new Dictionary<string, GameObject>();
                for (int i = 0; i < ResourcesPrefabManager.AllPrefabs.Count; i++)
                {
                    if (ResourcesPrefabManager.AllPrefabs[i] is GameObject gameObject)
                    {
                        if (gameObject.GetComponent<Item>() is Item item)
                        {
                            allPrefabs.Add(item.Name + " (" + item.gameObject.name + ")", gameObject);
                        }
                        else
                        {
                            allPrefabs.Add(gameObject.name, gameObject);
                        }
                    }
                }

                foreach (GameObject gameObject in allPrefabs.Values.Where(x => x.GetComponent<Item>() is Item item && item.ItemID > 1999999))
                {
                    var item = gameObject.GetComponent<Item>();

                    Templates.ItemTemplate itemTemplate = new Templates.ItemTemplate
                    {
                        Type = item.GetType().ToString(),
                        ItemID = item.ItemID,
                        LegacyItemID = item.LegacyItemID,
                        Name = item.Name,
                        Description = item.Description
                    };

                    if (item.Stats != null)
                    {
                        itemTemplate.BaseValue = item.Stats.BaseValue;
                        itemTemplate.Durability = item.Stats.MaxDurability;
                        try { itemTemplate.Weight = item.Weight; } catch { itemTemplate.Weight = -1; }
                    }

                    bool consumeFlag = false;
                    if (gameObject.GetComponent<TagSource>() is TagSource tags)
                    {
                        itemTemplate.Tags = new List<string>();
                        foreach (Tag tag in tags.Tags)
                        {
                            if (tag.TagName == "Consummable") { consumeFlag = true; }
                            itemTemplate.Tags.Add(tag.TagName);
                        }
                    }

                    if (item is Equipment && item.Stats != null)
                    {
                        Templates.EquipmentTemplate equipTemplate = new Templates.EquipmentTemplate();

                        InheritBaseValues(equipTemplate, itemTemplate);

                        try { ParseEquipment(item, ref equipTemplate); } catch { }

                        if (item is Weapon)
                        {
                            Templates.WeaponTemplate weaponTemplate = new Templates.WeaponTemplate();

                            InheritBaseValues(weaponTemplate, equipTemplate);

                            try { ParseWeapon(item, ref weaponTemplate); } catch { }

                            SaveJsonOverwrite(weaponTemplate, Folders["Items"], item.name);

                        }
                        else if (item is Bag bag)
                        {
                            Templates.BagTemplate _bag = new Templates.BagTemplate();

                            InheritBaseValues(_bag, equipTemplate);

                            _bag.RestrictDodge = bag.RestrictDodge;
                            _bag.InventoryProtection = bag.InventoryProtection;

                            if (bag.GetComponentInChildren<Preserver>() is Preserver preserver)
                            {
                                _bag.PreservedTypes = new List<string>();
                                _bag.PreservationAmounts = new List<float>();

                                foreach (Preserver.PreservedElement element in GetValue(typeof(Preserver), preserver, "m_preservedElements") as List<Preserver.PreservedElement>)
                                {
                                    _bag.PreservedTypes.Add(element.Tag.Tag.TagName);
                                    _bag.PreservationAmounts.Add(element.Preservation);
                                }
                            }

                            SaveJsonOverwrite(_bag, Folders["Items"], item.name);
                        }
                        else
                        {
                            SaveJsonOverwrite(equipTemplate, Folders["Items"], item.name);
                        }
                    }
                    else if (item is PassiveSkill)
                    {
                        Templates.PassiveSkillTemplate passive = new Templates.PassiveSkillTemplate();

                        InheritBaseValues(passive, itemTemplate);

                        try { ParsePassiveSkill(item, ref passive); } catch { }

                        SaveJsonOverwrite(passive, Folders["Items"], item.name);
                    }
                    else if (item is AttackSkill)
                    {
                        Templates.ActiveSkillTemplate skill = new Templates.ActiveSkillTemplate();

                        InheritBaseValues(skill, itemTemplate);

                        try { ParseActiveSkill(item, ref skill); } catch { }

                        // saved in Parse function because of special stuff
                    }
                    else if (consumeFlag)
                    {
                        Templates.ConsumableTemplate consumableTemplate = new Templates.ConsumableTemplate();

                        InheritBaseValues(consumableTemplate, itemTemplate);

                        try { ParseConsumable(item, ref consumableTemplate); } catch { }

                        SaveJsonOverwrite(consumableTemplate, Folders["Items"], item.name);
                    }
                    else
                    {
                        SaveJsonOverwrite(itemTemplate, Folders["Items"], item.name);
                    }
                }
                yield return null;
            }
            Debug.LogWarning("ResourcesPrefabManager done!");
        }

        public IEnumerator DumpEffectPrefabs()
        {
            UnityEngine.Object[] array = Resources.LoadAll("_StatusEffects", typeof(EffectPreset));
            if (array != null && array.Length > 0)
            {
                foreach (EffectPreset effect in array)
                {

                    if (effect is ImbueEffectPreset || effect.GetComponent<StatusEffect>() != null)
                    {
                        Templates.StatusEffectTemplate statustemplate = new Templates.StatusEffectTemplate
                        {
                            EffectID = effect.PresetID
                        };

                        try { ParseStatusEffect(effect, ref statustemplate); } catch (Exception e) { Debug.Log(e.Message + "\r\n" + e.StackTrace); }

                        SaveJsonOverwrite(statustemplate, Folders["Effects"], effect.name);
                    }
                }
                yield return null;
            }

            Debug.LogWarning("Effects done!");
        }

        public void DumpRecipes()
        {
            DirectoryInfo di = new DirectoryInfo(@"Dumps\Prefabs\Recipes");
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            if (GetValue(typeof(RecipeManager), RecipeManager.Instance, "m_recipes") is Dictionary<string, Recipe> recipes)
            {
                //List<string> simpleList = new List<string>();

                foreach (Recipe recipe in recipes.Values)
                {
                    DumpRecipeSingle(recipe);
                }

                if (!Directory.Exists(sorter.Folders["Lists"])) { Directory.CreateDirectory(sorter.Folders["Lists"]); }

                //string path = sorter.Folders["Lists"] + "/Recipes.txt";
                //if (File.Exists(path)) { File.Delete(path); }
                //File.WriteAllLines(path, simpleList.ToArray());

                Debug.LogWarning("Recipes done!");
            }
        }

        // ============= item parsers =============

        public void ParseEquipment(Item item, ref Templates.EquipmentTemplate equipTemplate)
        {
            Equipment equip = item as Equipment;

            if (equip != null && equip.Stats is EquipmentStats)
            {
                try { equipTemplate.EquipmentSlot = equip.EquipSlot.ToString(); } catch { };

                equipTemplate.Protection = equip.Stats.GetDamageProtection(DamageType.Types.Physical);
                equipTemplate.DamageAttack = GetValue(typeof(EquipmentStats), equip.Stats, "m_damageAttack") as float[];
                equipTemplate.DamageResistance = GetValue(typeof(EquipmentStats), equip.Stats, "m_damageResistance") as float[];
                equipTemplate.ImpactResistance = equip.Stats.ImpactResistance;
                equipTemplate.StaminaUsePenalty = equip.Stats.StaminaUsePenalty;
                equipTemplate.ManaUseModifier = equip.Stats.ManaUseModifier;
                equipTemplate.MovementPenalty = equip.Stats.MovementPenalty;
                equipTemplate.PouchBonus = equip.Stats.PouchCapacityBonus;
                equipTemplate.HeatProtect = equip.Stats.HeatProtection;
                equipTemplate.ColdProtect = equip.Stats.ColdProtection;
            }
            return;
        }

        public void ParseWeapon(Item item, ref Templates.WeaponTemplate weaponTemplate)
        {
            weaponTemplate.WeaponType = (item as Weapon).Type.ToString();

            WeaponStats weaponStats = item.Stats as WeaponStats;
            weaponTemplate.BaseDamage = weaponStats.BaseDamage;
            weaponTemplate.Impact = weaponStats.Impact;
            weaponTemplate.AttackSpeed = weaponStats.AttackSpeed;
            weaponTemplate.AttackCount = weaponStats.AttackCount;
            weaponTemplate.Attacks = weaponStats.Attacks;

            if (item.GetComponentsInChildren<AddStatusEffectBuildUp>(true) is AddStatusEffectBuildUp[] statuses && statuses.Count() > 0)
            {
                weaponTemplate.HitEffects = new List<string>();
                weaponTemplate.HitEffects_Buildups = new List<float>();

                foreach (AddStatusEffectBuildUp status in statuses)
                {
                    if (status.Status.name.ToLower().Contains("mana")) { continue; }

                    weaponTemplate.HitEffects.Add(status.Status.IdentifierName);
                    weaponTemplate.HitEffects_Buildups.Add(status.BuildUpValue);
                }
            }
            return;
        }

        // =========== skill parsers ===========

        public void ParsePassiveSkill(Item item, ref Templates.PassiveSkillTemplate template)
        {
            template.AffectedStats = new List<string>();
            template.Values = new List<float>();

            if (item.GetComponentsInChildren<AffectStat>() is AffectStat[] affectStatList)
            {
                foreach (AffectStat stataffect in affectStatList)
                {
                    template.AffectedStats.Add(TagSourceManager.Instance.GetTag(stataffect.AffectedStat.SelectorValue).TagName);
                    template.Values.Add(stataffect.Value);
                }
            }
        }

        public void ParseActiveSkill(Item item, ref Templates.ActiveSkillTemplate template)
        {
            Skill skill = item as Skill;

            // ---------------------------

            template.StaminaCost = skill.StaminaCost;
            template.DurabilityCost = skill.DurabilityCost;
            template.ManaCost = skill.ManaCost;
            template.Cooldown = skill.Cooldown;
            template.Lifespan = skill.Lifespan;

            template.RequiredItems = new List<string>();
            template.ItemsConsumed = new List<bool>();
            foreach (Skill.ItemRequired itemReq in skill.RequiredItems)
            {
                template.RequiredItems.Add(itemReq.Item.Name + " (" + itemReq.Quantity + ")");
                template.ItemsConsumed.Add(itemReq.Consume);
            }

            template.Required_Mainhand_Types = new List<string>();
            template.Required_Offhand_Types = new List<string>();
            foreach (Weapon.WeaponType type in skill.GetRequiredWeaponTypes)
            {
                template.Required_Mainhand_Types.Add(type.ToString());
            }
            foreach (Weapon.WeaponType type in skill.GetRequiredOffHandTypes)
            {
                template.Required_Offhand_Types.Add(type.ToString());
            }

            // ====== skill damage parser ====== //
            List<PunctualDamage> components = new List<PunctualDamage>();

            foreach (PunctualDamage pdComp in skill.GetComponentsInChildren<PunctualDamage>(true))
            {
                components.Add(pdComp);
            }
            foreach (ShootBlast blast in skill.GetComponentsInChildren<ShootBlast>())
            {
                if (blast.BaseBlast is Blast baseBlast)
                {
                    foreach (PunctualDamage blastDmg in baseBlast.GetComponentsInChildren<PunctualDamage>())
                        components.Add(blastDmg);
                }
            }
            foreach (ShootProjectile proj in skill.GetComponentsInChildren<ShootProjectile>())
            {
                if (proj.BaseProjectile is Projectile baseProj)
                {
                    foreach (PunctualDamage projDmg in baseProj.GetComponentsInChildren<PunctualDamage>())
                        components.Add(projDmg);
                }
            }

            Dictionary<string, string> SkillDmgJsons = new Dictionary<string, string>();
            foreach (PunctualDamage dmg in components)
            {
                Templates.SkillDamage dmgTemplate = new Templates.SkillDamage();

                ParseSkillDmg(dmg, ref dmgTemplate);

                SkillDmgJsons.Add(dmg.name + " (" + dmg.transform.parent.name + ")", JsonUtility.ToJson(dmgTemplate, true));
            }

            string saveFix = JsonUtility.ToJson(template, true);

            saveFix = AppendJsonList(saveFix, SkillDmgJsons);

            string saveName = ReplaceInvalidChars(skill.name);
            if (File.Exists(Folders["Items"] + "/" + saveName + ".json")) { File.Delete(Folders["Items"] + "/" + saveName + ".json"); }
            File.WriteAllText(Folders["Items"] + "/" + saveName + ".json", saveFix);

        }

        public void ParseSkillDmg(PunctualDamage dmg, ref Templates.SkillDamage template)
        {
            template.Damages = dmg.Damages;
            template.Impact = dmg.Knockback;

            if (dmg is WeaponDamage wDmg)
            {
                template.AddWeaponDamage = true;
                template.DamageOverride = (int)wDmg.OverrideDType;
                template.DamageMultiplier = wDmg.WeaponDamageMult;
                template.ImpactMultiplier = wDmg.WeaponKnockbackMult;
            }

            template.HitEfects = new List<string>();
            foreach (AddStatusEffectBuildUp status in dmg.GetComponents<AddStatusEffectBuildUp>())
            {
                template.HitEfects.Add(status.Status.IdentifierName + " (" + status.BuildUpValue + ")");
            }
            foreach (AddStatusEffect status in dmg.GetComponents<AddStatusEffect>())
            {
                template.HitEfects.Add(status.Status.IdentifierName + " (" + status.BaseChancesToContract + ")");
            }
        }

        // =========== food parser ===========

        public void ParseConsumable(Item item, ref Templates.ConsumableTemplate template)
        {
            template.Effects = new List<string>();

            if (item is Food food)
            {
                if (food.GetType().GetField("m_affectFoodEffects", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(food) is List<AffectFood> affectFoodList)
                {
                    foreach (AffectFood affect in affectFoodList)
                    {
                        AffectNeed affectNeed = affect as AffectNeed;
                        template.Hunger += (float)GetValue(typeof(AffectNeed), affectNeed, "m_affectQuantity");
                    }
                }

                if (food.GetType().GetField("m_affectDrinkEffects", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(food) is List<AffectDrink> affectDrinkList)
                {
                    foreach (AffectDrink affect in affectDrinkList)
                    {
                        AffectNeed affectNeed = affect as AffectNeed;
                        template.Thirst += (float)GetValue(typeof(AffectNeed), affectNeed, "m_affectQuantity");
                    }
                }
            }

            foreach (Component c in item.GetComponentsInChildren<Component>(true).Where(x => x as AddStatusEffect != null))
            {
                var AddStatus = c as AddStatusEffect;

                if (AddStatus.Status.IdentifierName is string s)
                {
                    template.Effects.Add(s);
                }
            }
        }

        // =========== effect preset parsers ===========

        public void ParseStatusEffect(EffectPreset effect, ref Templates.StatusEffectTemplate template)
        {
            template.AffectedStats = new List<string>();
            template.Values = new List<string>();
            template.Values_AI = new List<string>();

            if (effect is ImbueEffectPreset imbueEffect)
            {
                template.Name = imbueEffect.Name;
                template.Type = "Imbue";

                if (effect.GetComponentInChildren<WeaponDamage>() is WeaponDamage imbueDmg)
                {
                    template.Imbue_Damage = imbueDmg.Damages;
                    template.Imbue_Multiplier = imbueDmg.WeaponDamageMult;
                }

                if (effect.GetComponentInChildren<AddStatusEffectBuildUp>(true) is AddStatusEffectBuildUp addStatus)
                {
                    template.Imbue_HitEffect = addStatus.Status.name;
                }

                foreach (AffectStat affectStat in effect.GetComponentsInChildren<AffectStat>(true))
                {
                    template.AffectedStats.Add(TagSourceManager.Instance.GetTag(affectStat.AffectedStat.SelectorValue).TagName);
                    template.Values.Add(affectStat.Value.ToString());
                }
            }

            if (effect.gameObject.GetComponent<StatusEffect>() is StatusEffect status)
            {
                try { template.Name = status.IdentifierName; }
                catch { template.Name = status.name; }

                template.Purgeable = status.Purgeable;
                template.Lifespan = status.StartLifespan;

                if (status.StatusData is StatusData data)
                {
                    var sig = GetValue(typeof(StatusData), data, "m_effectSignature") as EffectSignature;

                    int i = 0;
                    foreach (Effect effectSig in sig.Effects)
                    {
                        if (effectSig is AffectStat affectStat)
                        {
                            template.AffectedStats.Add(TagSourceManager.Instance.GetTag(affectStat.AffectedStat.SelectorValue).TagName);
                            template.Values.Add(status.StatusData.EffectsData[i].Data[0]);
                        }

                        if (effectSig is AffectHealth affectHealth)
                        {
                            template.AffectedStats.Add("Health");
                            template.Values.Add(status.StatusData.EffectsData[i].Data[0]);
                            template.Values_AI.Add(affectHealth.AffectQuantityOnAI.ToString());
                        }

                        if (effectSig is AffectMana affectMana)
                        {
                            template.AffectedStats.Add("Mana");
                            template.Values.Add(status.StatusData.EffectsData[i].Data[0]);
                        }

                        if (effectSig is AffectStamina affectStamina)
                        {
                            template.AffectedStats.Add("Stamina");
                            template.Values.Add(status.StatusData.EffectsData[i].Data[0]);
                        }

                        if (effectSig is PunctualDamage damageEffect)
                        {
                            template.AffectedStats.Add("Damage");
                            template.Values.Add(status.StatusData.EffectsData[i].Data[0]);
                            try { template.Values_AI.Add(damageEffect.DamagesAI[0].ToString()); } catch { }
                        }

                    }
                }
            }


            if (effect.GetComponent<BoonStatusFX>()) { template.Type = "Boon"; }
            else if (effect.GetComponent<HexStatusFX>()) { template.Type = "Hex"; }
            else if (template.Type != "Imbue") { template.Type = "Simple"; }
        }

        // ========= recipe parser ===========

        public void DumpRecipeSingle(Recipe recipe)
        {
            Templates.RecipeTemplate _recipe = new Templates.RecipeTemplate
            {
                RecipeType = recipe.CraftingStationType.ToString(),
                Result = recipe.Results[0].Item.Name,
                ResultCount = recipe.Results[0].Quantity
            };
            _recipe.Ingredients = new List<string>();
            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient.AddedIngredient != null)
                {
                    _recipe.Ingredients.Add(ingredient.AddedIngredient.Name);
                }
                else
                {
                    _recipe.Ingredients.Add(ingredient.AddedIngredientType.Tag.TagName);
                }
            }

            SaveRecipeRecursive(_recipe, recipe.Name, 1);
        }

        private void SaveRecipeRecursive(Templates.RecipeTemplate recipe, string name, int count)
        {
            if (count == 1 && !File.Exists(Folders["Recipes"] + "/" + name + ".json"))
            {
                SaveJsonOverwrite(recipe, Folders["Recipes"], name);
            }
            else if (!File.Exists(Folders["Recipes"] + "/" + name + "_" + (count + 1) + ".json"))
            {
                SaveJsonOverwrite(recipe, Folders["Recipes"], name + "_" + (count + 1));
            }
            else
            {
                int count2 = count + 1;
                SaveRecipeRecursive(recipe, name, count2);
            }
        }


        // ===================================== scene dumps ===========================================

        public void DisableCanvases()
        {
            var canvases = Resources.FindObjectsOfTypeAll(typeof(NodeCanvas.BehaviourTrees.BehaviourTreeOwner));

            foreach (NodeCanvas.BehaviourTrees.BehaviourTreeOwner tree in canvases)
            {
                tree.gameObject.SetActive(false);
            }
        }

        public bool currentSceneDumped = false;
        public bool merchantsDumped;
        public bool lootDumped;
        public bool enemiesDumped;

        public IEnumerator DumpAllScenes()
        {
            Debug.Log("beginning all scenes dump! ...");

            foreach (string s in utils.sceneBuildNames)
            {
                //if (!IsExperimental)
                //{
                //    if (s == "CierzoDestroyed"
                //        || s.ToLower().Contains("bosses"))
                //    {
                //        continue;
                //    } 
                //}

                currentSceneDumped = false;

                if (SceneManagerHelper.ActiveSceneName != s)
                {
                    try { NetworkLevelLoader.Instance.RequestSwitchArea(s, 0, 1.5f); } catch (Exception e) { Debug.LogError("Error getting level! Message: " + e.Message); }

                    Debug.Log("Loading " + s + "...");

                    yield return new WaitForSeconds(5);

                    while (NetworkLevelLoader.Instance.IsGameplayPaused)
                    {
                        Debug.Log("trying to resume gameplay");

                        NetworkLevelLoader loader = NetworkLevelLoader.Instance;

                        SetValue(true, typeof(NetworkLevelLoader), loader, "m_autoExitFade");
                        SetValue(true, typeof(NetworkLevelLoader), loader, "m_allPlayerReadyToContinue");
                        SetValue(true, typeof(NetworkLevelLoader), loader, "m_allPlayerDoneLoading");
                        SetValue(true, typeof(NetworkLevelLoader), loader, "m_continueAfterLoading");
                        MenuManager.Instance.HideMasterLoadingScreen();

                        yield return new WaitForSeconds(1);
                    }

                    yield return new WaitForSeconds(2);
                }

                StartCoroutine(DumpEntireScene());

                while (!currentSceneDumped) { yield return new WaitForSeconds(1); }
            }

            Debug.Log("all scenes complete!");

            yield return null;
        }

        public IEnumerator DumpEntireScene()
        {
            currentSceneDumped = false;
            merchantsDumped = false;
            lootDumped = false;
            enemiesDumped = false;

            Debug.Log("Dumping merchants...");

            StartCoroutine(DumpMerchants());

            while (!merchantsDumped) { yield return new WaitForSeconds(1); }

            Debug.Log("Dumping loot...");

            StartCoroutine(DumpLoot());

            while (!lootDumped) { yield return new WaitForSeconds(1); }

            Debug.Log("Dumping enemies...");

            StartCoroutine(DumpEnemies());

            while (!enemiesDumped) { yield return new WaitForSeconds(1); }

            currentSceneDumped = true;

            yield return null;
        }


        public IEnumerator DumpEnemies()
        {
            DisableCanvases();

            foreach (Character c in CharacterManager.Instance.Characters.Values.Where(x => x.IsAI))
            {
                if (abortLoop) { abortLoop = false; break; }

                if (c.Faction == Character.Factions.Player || c.Stats.MaxHealth <= 0) { continue; }

                Vector3 origpos = c.transform.position;

                c.transform.position = CharacterManager.Instance.GetFirstLocalCharacter().transform.position;

                Debug.Log(c.Name);

                c.gameObject.SetActive(true);
                c.transform.parent.gameObject.SetActive(true);
                try
                { // lazy force object active (some quest enemies are inside multiple disabled transforms. could do recursive but 5 seems enough)
                    c.transform.parent.parent.gameObject.SetActive(true);
                    c.transform.parent.parent.parent.gameObject.SetActive(true);
                    c.transform.parent.parent.parent.parent.gameObject.SetActive(true);
                    c.transform.parent.parent.parent.parent.parent.gameObject.SetActive(true);
                }
                catch { }

                // wait for character init
                float startTime = Time.time;
                while (!c.gameObject.activeSelf && !c.IsStartInitDone && !c.IsLateInitDone && Time.time - startTime < 5)
                {
                    yield return new WaitForSeconds(0.10f);
                }

                // sometimes bandits armor stats take a few more seconds to apply...
                startTime = Time.time;
                while ((c.name.ToLower().Contains("bandit") || c.name.ToLower().Contains("npc") || c.name.ToLower().Contains("witch")) 
                    && (Time.time - startTime < 5))
                {
                    if (GetValue(typeof(CharacterStats), c.Stats, "m_totalDamageResistance") is float[] res && res.Length > 0 && res[0] > 0.0998f) { break; }

                    yield return new WaitForSeconds(0.5f);
                }

                yield return new WaitForSeconds(0.5f); 

                Templates.EnemyTemplate enemy = new Templates.EnemyTemplate
                {
                    Name = c.Name,
                    Faction = c.Faction.ToString(),
                    UID = c.UID,
                    MaxHealth = c.Stats.MaxHealth,
                    Location = utils.GetCurrentLocation(origpos),
                    DamageMultipliers = GetValue(typeof(CharacterStats), c.Stats, "m_totalDamageAttack") as float[],
                    DamageResistances = GetValue(typeof(CharacterStats), c.Stats, "m_totalDamageResistance") as float[],
                    Protection = c.Stats.DamageProtection[0],
                    ImpactResistance = c.Stats.GetImpactResistance()
                };

                if (utils.GetCurrentLocation(c.transform.position).ToLower().Contains("vendavel") && c.Name.Contains("Bandit"))
                {
                    enemy.Name = "Vendavel " + c.Name;
                }

                if (c.TargetingSystem != null && c.TargetingSystem.TargetableFactions.Count() > 0)
                {
                    enemy.Targetable_Factions = new List<string>();
                    foreach (Character.Factions faction in c.TargetingSystem.TargetableFactions)
                    {
                        enemy.Targetable_Factions.Add(faction.ToString());
                    }
                }

                try { DumpEnemySingle(c, ref enemy); } catch (Exception e) { Debug.Log("Exception: " + e.Message + "\r\n Trace:" + e.StackTrace); }

                SaveJsonOverwrite(enemy, Folders["Scenes"]
                    + "/" + utils.GetCurrentRegion()
                    + "/" + utils.GetCurrentLocation(origpos)
                    + "/Enemies", c.name);

                c.transform.position = origpos;
            }

            Debug.Log("Enemy dump finished");
            enemiesDumped = true;
            yield return null;
        }

        public void DumpEnemySingle(Character c, ref Templates.EnemyTemplate enemy)
        {
            try { enemy.HealthRegen = c.Stats.HealthRegen; } catch { }

            enemy.Radius = c.CharacterController.radius;
            enemy.HoursToReset = c.HoursToReset;

            enemy.Status_Immunities = new List<string>();
            foreach (TagSourceSelector tagSelector in GetValue(typeof(CharacterStats), c.Stats, "m_statusEffectsNaturalImmunity") as TagSourceSelector[])
            {
                enemy.Status_Immunities.Add(tagSelector.Tag.TagName);
            }
            foreach (KeyValuePair<Tag, List<string>> entry in GetValue(typeof(CharacterStats), c.Stats, "m_statusEffectsImmunity") as Dictionary<Tag, List<string>>)
            {
                if (entry.Value.Count > 0)
                {
                    enemy.Status_Immunities.Add(entry.Key.TagName);
                }
            }

            enemy.Equipment = new List<string>();

            var enemyStartingItems = GetValue(typeof(Character), c, "m_startingEquipment") as StartingEquipment;

            if (enemyStartingItems != null)
            {
                for (int i = 0; i < 9; i++)
                {
                    EquipmentSlot.EquipmentSlotIDs slot = (EquipmentSlot.EquipmentSlotIDs)i;

                    if (enemyStartingItems.GetEquipment(slot) != null && enemyStartingItems.GetEquipment(slot) is Equipment e)
                    {
                        if (!enemy.Equipment.Contains(e.Name))
                            enemy.Equipment.Add(e.Name);

                        if (c.CurrentWeapon == null && e is Weapon && ResourcesPrefabManager.Instance.GetItemPrefab(e.ItemID) is Weapon w && w.Type != Weapon.WeaponType.Shield)
                        {
                            enemy.Weapon_Impact = w.Stats.Impact;
                            enemy.Weapon_Damage = w.Stats.BaseDamage.Clone();
                            if (enemy.Weapon_Damage != null)
                            {
                                for (int j = 0; j < 6; j++)
                                {
                                    float multi = enemy.DamageMultipliers[j];
                                    if (enemy.Weapon_Damage[(DamageType.Types)j] != null)
                                    {
                                        enemy.Weapon_Damage[(DamageType.Types)j].Damage *= multi;
                                    }
                                }
                            }

                            if (e.transform.Find("HitEffects") is Transform t)
                            {
                                enemy.Inflicts = GetHitEffects(t);
                            }
                        }
                        else if (c.CurrentWeapon != null)
                        {
                            enemy.Weapon_Impact = c.CurrentWeapon.Stats.Impact;
                            enemy.Weapon_Damage = c.CurrentWeapon.Stats.BaseDamage.Clone();
                            if (enemy.Weapon_Damage != null)
                            {
                                for (int j = 0; j < 6; j++)
                                {
                                    float multi = enemy.DamageMultipliers[j];
                                    if (enemy.Weapon_Damage[(DamageType.Types)j] != null)
                                    {
                                        enemy.Weapon_Damage[(DamageType.Types)j].Damage *= multi;
                                    }
                                }
                            }

                            if (c.CurrentWeapon.transform.Find("HitEffects") is Transform t)
                            {
                                enemy.Inflicts = GetHitEffects(t);
                            }
                        }
                    }
                }
            }

            if (c.GetComponentInChildren<CharacterKnowledge>() is CharacterKnowledge knowledge
                && GetValue(typeof(CharacterKnowledge), knowledge, "m_learnedItems") is List<Item> learned)
            {
                enemy.Skills = new List<string>();
                foreach (Item i in learned)
                {
                    enemy.Skills.Add(i.Name + " (" + i.ItemID + ")");
                }
            }

            enemy.GuaranteedDrops = new List<string>();
            enemy.GuaranteedQtys = new List<int>();
            enemy.GuaranteedIDs = new List<int>();
            enemy.DropTables = new List<string>();

            if (c.GetComponent<LootableOnDeath>() is LootableOnDeath deathcomp)
            {
                if (deathcomp.EnabledPouch
                    && enemyStartingItems.StartingPouchItems != null
                    && enemyStartingItems.StartingPouchItems.Count > 0)
                {
                    foreach (ItemQuantity item in enemyStartingItems.StartingPouchItems)
                    {
                        if (!enemy.GuaranteedDrops.Contains(item.Item.Name) && item.Quantity > 0)
                        {
                            enemy.GuaranteedDrops.Add(item.Item.Name);
                            enemy.GuaranteedQtys.Add(item.Quantity);
                            enemy.GuaranteedIDs.Add(item.Item.ItemID);
                        }
                    }
                }

                if (deathcomp.DropWeapons)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        EquipmentSlot.EquipmentSlotIDs slot = (EquipmentSlot.EquipmentSlotIDs)i;

                        if (enemyStartingItems.GetEquipment(slot) is Equipment e && e is Weapon)
                        {
                            enemy.GuaranteedDrops.Add(e.Name);
                            enemy.GuaranteedQtys.Add(1);
                            enemy.GuaranteedIDs.Add(e.ItemID);
                        }
                    }
                }

                if (GetValue(typeof(LootableOnDeath), deathcomp, "m_lootDroppers") is Dropable[] lootDroppers)
                {
                    foreach (Dropable dropper in lootDroppers)
                    {                       
                        string enemyTable = DumpDropTable(dropper);

                        string tableName = ReplaceInvalidChars(dropper.name);
                        string path = Folders["Droptables"] + "/" + tableName + ".json";

                        if (File.Exists(path))
                        {
                            var origFile = File.ReadAllText(path);
                            if (origFile.Length != enemyTable.Length)
                            {
                                // same droptable name, but they are not the same.
                                var uid = UID.Generate();
                                Debug.LogWarning("Dumping " + dropper.name + " but the new table is not equal to the orig! Generated UID: " + uid);

                                tableName += "_" + uid;
                                path = Folders["Droptables"] + "/" + tableName + ".json";
                                File.WriteAllText(path, enemyTable);
                                enemy.DropTables.Add(tableName);

                            }
                            else
                            {
                                // duplicate of existing table.
                                enemy.DropTables.Add(tableName);
                            }
                        }
                        else
                        {
                            File.WriteAllText(path, enemyTable); 
                            enemy.DropTables.Add(tableName);
                        }
                    }
                }
            }
        }

        private List<string> GetHitEffects(Transform t)
        {
            List<string> effects = new List<string>();

            if (t.GetComponents<AddStatusEffectBuildUp>() is AddStatusEffectBuildUp[] hitEffects)
            {
                foreach (AddStatusEffectBuildUp status in hitEffects)
                {
                    effects.Add(status.Status.name);
                }
            }

            if (t.GetComponents<AddStatusEffect>() is AddStatusEffect[] addEffects)
            {
                foreach (AddStatusEffect status in addEffects)
                {
                    effects.Add(status.Status.name);
                }
            }
            return effects;
        }


        public IEnumerator DumpMerchants()
        {
            DisableCanvases();

            foreach (Merchant c in Resources.FindObjectsOfTypeAll<Merchant>().Where(x => x.gameObject.scene != null))
            {
                if (abortLoop) { abortLoop = false; break; }

                Merchant merchant = c.GetComponentInChildren<Merchant>();

                DumpMerchantSingle(merchant, c.transform.position, c.HolderUID);

                yield return null;
            }

            Debug.Log("Merchant dump finished");
            merchantsDumped = true;
            yield return null;
        }

        public void DumpMerchantSingle(Merchant c, Vector3 origPos, string UID)
        {
            Debug.Log(c.ShopName);

            Templates.Merchant merchant = new Templates.Merchant
            {
                Name = c.ShopName,
                Location = utils.GetCurrentLocation(c.transform.position)
            };

            if (GetValue(typeof(Merchant), c, "m_dropableInventory") is Dropable dropper)
            {
                string merchantTable = DumpDropTable(dropper);
                string droppath = Folders["Droptables"] + "/" + utils.GetCurrentLocation(c.transform.position) + " - " + c.ShopName + "_" + UID + ".json";

                if (File.Exists(droppath)) { File.Delete(droppath); }
                File.WriteAllText(droppath, merchantTable);

                merchant.DropTables = new List<string> { c.ShopName + "_" + UID, };

                string path = Folders["Scenes"] + "/" + utils.GetCurrentRegion() + "/" + utils.GetCurrentLocation(origPos) + "/Merchants/" + c.ShopName + " (" + UID + ").json";

                if (File.Exists(path)) { File.Delete(path); }
                File.WriteAllText(path, JsonUtility.ToJson(merchant, true));
            }
        }


        public IEnumerator DumpLoot()
        {
            DisableCanvases();

            var allitems = Resources.FindObjectsOfTypeAll(typeof(Item)) as Item[];

            foreach (Item item in allitems
            .Where(x => IsValidLoot(x.transform)))
            {
                if (abortLoop) { abortLoop = false; break; }

                DumpLootSingle(item, item.transform.position);
            }
            Debug.Log("Loot dump finished");
            lootDumped = true;

            yield return null;
        }

        public void DumpLootSingle(Item item, Vector3 origPos)
        {
            Debug.Log(item.Name);

            if (item is SelfFilledItemContainer itemContainer)
            {
                Templates.ItemContainerTemplate containerTemplate = new Templates.ItemContainerTemplate
                {
                    Name = itemContainer.Name,
                    Type = item.GetType().ToString(),
                    ItemID = itemContainer.ItemID,
                    ContainerType = itemContainer.GetType().ToString()
                };

                if (GetValue(typeof(SelfFilledItemContainer), itemContainer, "m_drops") is List<Dropable> droppers)
                {
                    containerTemplate.DropTableNames = new List<string>();

                    foreach (Dropable dropper in droppers)
                    {
                        containerTemplate.DropTableNames.Add(dropper.name);

                        string tablejson = DumpDropTable(dropper);

                        string tableName = ReplaceInvalidChars(dropper.name);

                        if (File.Exists(Folders["Droptables"] + "/" + tableName + ".json"))
                        {
                            File.Delete(Folders["Droptables"] + "/" + tableName + ".json");
                        }
                        File.WriteAllText(Folders["Droptables"] + "/" + tableName + ".json", tablejson);
                    }
                }

                SaveJsonOverwrite(containerTemplate, Folders["Scenes"]
                    + "/" + utils.GetCurrentRegion()
                    + "/" + utils.GetCurrentLocation(origPos)
                    + "/Loot", item.Name + " (" + item.name + ")");
            }
            else
            {
                SaveJsonOverwrite(item, Folders["Scenes"]
                    + "/" + utils.GetCurrentRegion()
                    + "/" + utils.GetCurrentLocation(origPos)
                    + "/Loot/Spawns", item.Name + " (" + item.name + ")");
            }

            lootDumped = true;
        }

        public bool IsValidLoot(Transform t)
        {
            var item = t.GetComponent<Item>();

            if (t.gameObject.scene == null || item.UID == null || item.UID == UID.Empty || ItemManager.Instance.GetItem(item.UID) == null)
            {
                return false;
            }

            if (item.ParentContainer == null && item.OwnerCharacter == null && item.IsInWorld && (item.IsPickable || item is SelfFilledItemContainer || item.IsDeployable))
            {
                return true;
            }

            return false;
        }


        public string DumpDropTable(Dropable dropper)
        {
            //Debug.Log(dropper.name);

            Templates.DropTableContainer template = new Templates.DropTableContainer
            {
                Name = dropper.name,
            };

            template.GuaranteedDrops = new List<string>();
            template.GuaranteedIDs = new List<int>();
            template.GuaranteedMaxQtys = new List<int>();
            template.GuaranteedMinQtys = new List<int>();

            if (GetValue(typeof(Dropable), dropper, "m_allGuaranteedDrops") is List<GuaranteedDrop> guaranteedDrops)
            {
                foreach (GuaranteedDrop drop in guaranteedDrops)
                {
                    if (GetValue(typeof(GuaranteedDrop), drop, "m_itemDrops") is List<BasicItemDrop> basicItemDrops && basicItemDrops.Count > 0)
                    {
                        foreach (BasicItemDrop itemDrop in basicItemDrops)
                        {
                            if (template.GuaranteedIDs.Contains(itemDrop.DroppedItem.ItemID))
                            {
                                int index = template.GuaranteedIDs.IndexOf(itemDrop.DroppedItem.ItemID);

                                template.GuaranteedMaxQtys[index] += itemDrop.MaxDropCount;
                                template.GuaranteedMinQtys[index] += itemDrop.MinDropCount;
                            }
                            else
                            {
                                template.GuaranteedDrops.Add(itemDrop.DroppedItem.Name);
                                template.GuaranteedIDs.Add(itemDrop.DroppedItem.ItemID);
                                template.GuaranteedMinQtys.Add(itemDrop.MinDropCount);
                                template.GuaranteedMaxQtys.Add(itemDrop.MaxDropCount);
                            }
                        }
                    }
                }
            }

            Dictionary<string, string> DropTableJSONS = new Dictionary<string, string>();

            if (GetValue(typeof(Dropable), dropper, "m_mainDropTables") is List<DropTable> dropTables)
            {
                int j = 0;
                foreach (DropTable table in dropTables)
                {
                    j++;
                    Templates.DropTableTemplate tableTemplate = new Templates.DropTableTemplate
                    {
                        ItemChances = new List<string>(),
                        ItemChanceIDs = new List<int>(),
                        ChanceMinQtys = new List<int>(),
                        ChanceMaxQtys = new List<int>(),
                        ChanceDropChances = new List<float>(),

                        MaxDiceValue = (int)GetValue(typeof(DropTable), table, "m_maxDiceValue"),
                        MinNumberOfDrops = table.MinNumberOfDrops,
                        MaxNumberOfDrops = table.MaxNumberOfDrops
                    };

                    if (GetValue(typeof(DropTable), table, "m_emptyDropChance") is int i)
                    {
                        decimal emptyChance = (decimal)i / tableTemplate.MaxDiceValue;
                        tableTemplate.EmptyDropChance = (float)emptyChance * 100;
                    }

                    template.MinRandomDrops += table.MinNumberOfDrops;
                    template.MaxRandomDrops += table.MaxNumberOfDrops;

                    if (GetValue(typeof(DropTable), table, "m_itemDrops") is List<ItemDropChance> itemDrops)
                    {
                        foreach (ItemDropChance drop in itemDrops)
                        {
                            decimal percentage = (decimal)drop.DropChance / tableTemplate.MaxDiceValue;

                            if (percentage == 1)
                            {
                                template.GuaranteedDrops.Add(drop.DroppedItem.Name);
                                template.GuaranteedIDs.Add(drop.DroppedItem.ItemID);
                                template.GuaranteedMinQtys.Add(drop.MinDropCount);
                                template.GuaranteedMaxQtys.Add(drop.MaxDropCount);
                            }
                            else
                            {
                                tableTemplate.ItemChances.Add(drop.DroppedItem.Name);
                                tableTemplate.ItemChanceIDs.Add(drop.DroppedItem.ItemID);
                                tableTemplate.ChanceMinQtys.Add(drop.MinDropCount);
                                tableTemplate.ChanceMaxQtys.Add(drop.MaxDropCount);
                                tableTemplate.ChanceDropChances.Add((float)percentage * 100);
                            }
                        }
                    }

                    DropTableJSONS.Add("Droptable_" + j, JsonUtility.ToJson(tableTemplate, true));
                }
            }

            string saveFix = JsonUtility.ToJson(template, true);
            saveFix = AppendJsonList(saveFix, DropTableJSONS);

            return saveFix;
        }

        // json appender
        // the Dictionary Key is the name of the JSON file or whatever key you want the values to be. The key name cannot be the same as any existing top-level field name in the JSON.
        // the Dictionary value is the raw json dump from JSONUtility.ToJson()

        public string AppendJsonList(string orig, Dictionary<string, string> toAppend)
        {
            string saveFix = orig.Substring(0, orig.Length - 2); // remove the closing '}' and new line

            int i = 0;
            foreach (KeyValuePair<string, string> entry in toAppend)
            {
                if (i == 0) { saveFix += ","; }
                i++;
                saveFix += "\r\n	\"" + entry.Key + "\" : {"; // add the ', "objectname" : {'
                saveFix += entry.Value.Substring(1, entry.Value.Length - 2); // add the json without the opening and closing brackets
                saveFix += "	}";
                if (i < toAppend.Count)
                    saveFix += ",";
            }

            return saveFix += "\r\n}"; // restore the closing '}'
        }




        // ======================================== other =============================================

        // ================ file saving ==============

        public void SaveJsonOverwrite(object Object, string path, string saveName)
        {
            saveName = ReplaceInvalidChars(saveName);

            if (File.Exists(path + "/" + saveName + ".json"))
            {
                File.Delete(path + "/" + saveName + ".json");
            }
            File.WriteAllText(path + "/" + saveName + ".json", JsonUtility.ToJson(Object, true));
        }

        public string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        // =============== hooks ================

        // disable AI aggression
        private void AIEnemyDetectionHook(On.AICEnemyDetection.orig_Update orig, AICEnemyDetection self)
        {
            if (EnemiesAggressive)
                orig(self);
        }
        private void AISwitchStateHook(On.AIESwitchState.orig_SwitchState orig, AIESwitchState self)
        {
            if (EnemiesAggressive)
                orig(self);
        }


        // Pretty Print current scene title hook
        private void TitleHook(On.LoadingFade.orig_Update orig, LoadingFade self)
        {
            Text title = GetValue(typeof(LoadingFade), self, "m_lblTitle") as Text;

            if (title != null)
            {   // a space at the end of a name causes a bug with folder names. this map is the only case, but I should add more generic solution   
                if (title.text == "Unknown Arena " || title.text == "Unknown Arena")
                {
                    CurrentScenePretty = "Unknown Arena";
                }
                else if (title.text.Contains("Misc. Dungeons"))
                {
                    if (CharacterManager.Instance.GetFirstLocalCharacter() is Character c)
                    {
                        CurrentScenePretty = utils.GetCurrentLocation(c.transform.position);
                    }
                    else
                    {
                        CurrentScenePretty = "Misc. Dungeons (loading...)";
                    }
                }
                else if (SceneManagerHelper.ActiveSceneName.ToLower().Contains("destroyed"))
                {
                    CurrentScenePretty = "Cierzo (Destroyed)";
                }
                else if (SceneManagerHelper.ActiveSceneName.ToLower().Contains("tutorial"))
                {
                    CurrentScenePretty = "Chersonese (Shipwreck)";
                }
                else
                {
                    CurrentScenePretty = title.text;
                }

                // if (CurrentScenePretty.Contains("Misc.")) {inSmallDungeon = true; } else { inSmallDungeon = false; }

                orig(self);
            }
        }

        // =========== reflection ============

        public object Call(object obj, string method, params object[] args)
        {
            var methodInfo = obj.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            if (methodInfo != null)
            {
                return methodInfo.Invoke(obj, args);
            }
            return null;
        }

        public void SetValue<T>(T value, Type type, object obj, string field)
        {
            FieldInfo fieldInfo = type.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            fieldInfo.SetValue(obj, value);
        }

        public object GetValue(Type type, object obj, string value)
        {
            FieldInfo fieldInfo = type.GetField(value, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            return fieldInfo.GetValue(obj);
        }

        public void InheritBaseValues(object _derived, object _base)
        {
            foreach (FieldInfo fi in _base.GetType().GetFields())
            {
                try { _derived.GetType().GetField(fi.Name).SetValue(_derived, fi.GetValue(_base)); } catch { }
            }

            return;
        }


    }
}
