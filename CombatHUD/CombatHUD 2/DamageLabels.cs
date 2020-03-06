using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CombatHUD
{
    public class DamageLabels : MonoBehaviour
    {
        public static DamageLabels Instance;

        public List<GameObject> LabelHolders = new List<GameObject>();
        public List<DamageLabel> ActiveLabels = new List<DamageLabel>();

        internal void Awake()
        {
            Instance = this;

            foreach (Transform child in this.transform)
            {
                LabelHolders.Add(child.gameObject);
            }

            SceneManager.sceneLoaded += OnSceneChange;

            // hooks
            On.PunctualDamage.DealHit += PunctualHook; // non-weapon skill hits, traps, effects, etc
            On.WeaponDamage.DealHit += WeaponSkillHook; // weapon skill hits
            On.Weapon.HasHit += WeaponHitHook; // melee weapon hit
            On.ProjectileWeapon.HasHit += ProjectileHitHook;
        }

        private void OnSceneChange(Scene scene, LoadSceneMode mode)
        {
            ActiveLabels.Clear();
            for (int j = 0; j < LabelHolders.Count; j++)
            {
                LabelHolders[j].SetActive(false);
            }
        }

        internal void Update()
        {
            //cleanup dead labels first
            for (int z = 0; z < ActiveLabels.Count; z++)
            {
                if (Time.time - ActiveLabels[z].CreationTime > (float)HUDManager.config.GetValue(Settings.LabelLifespan)
                    || ActiveLabels[z].Target == null)
                {
                    ActiveLabels.RemoveAt(z);
                    z--;
                }
            }

            float ceiling = (float)HUDManager.config.GetValue(Settings.DamageCeiling);
            int minsize = (int)(float)HUDManager.config.GetValue(Settings.MinFontSize);
            int maxsize = (int)(float)HUDManager.config.GetValue(Settings.MaxFontSize);

            if (maxsize < minsize)
                maxsize = minsize;

            for (int i = 0; i < SplitScreenManager.Instance.LocalPlayerCount; i++)
            {
                var splitplayer = SplitScreenManager.Instance.LocalPlayers[i];

                if (splitplayer.AssignedCharacter == null)
                {
                    continue;
                }

                var camera = splitplayer.CameraScript;
                int offset = i * 30;

                for (int j = 0 + offset; j < LabelHolders.Count; j++)
                {
                    if (j - offset >= ActiveLabels.Count)
                    {
                        if (LabelHolders[j].activeSelf)
                        {
                            LabelHolders[j].SetActive(false);
                        }
                    }
                    else
                    {
                        var labelInfo = ActiveLabels[j - offset];
                        var labelHolder = LabelHolders[j];

                        var pos = (bool)HUDManager.config.GetValue(Settings.LabelsStayAtHitPos) ? labelInfo.HitWorldPos : labelInfo.Target.CenterPosition;

                        float damageStrength = (float)((decimal)labelInfo.Damage / (decimal)ceiling); // set damage "strength"
                        float time = Time.time - labelInfo.CreationTime;
                        var timeOffset = Mathf.Lerp(0.3f, 0.07f, damageStrength) * time;

                        var screenPos = camera.WorldToViewportPoint(pos + new Vector3(0, timeOffset));
                        float distance = Vector3.Distance(splitplayer.AssignedCharacter.transform.position, pos);

                        if (IsScreenPosVisible(ref screenPos, i) && distance < (float)HUDManager.config.GetValue(Settings.MaxDistance))
                        {
                            screenPos += new Vector3
                            (
                                HUDManager.Rel(labelInfo.ranXpos),
                                HUDManager.Rel(labelInfo.ranYpos, true)
                            );

                            labelHolder.GetComponent<RectTransform>().position = screenPos;

                            var text = labelHolder.GetComponent<Text>();
                            text.text = Math.Round(labelInfo.Damage).ToString();
                            text.fontSize = (int)Mathf.Lerp(minsize, maxsize, damageStrength);
                            text.color = labelInfo.TextColor;

                            if (!LabelHolders[j].activeSelf)
                            {
                                LabelHolders[j].SetActive(true);
                            }
                        }
                        else if (LabelHolders[j].activeSelf)
                        {
                            LabelHolders[j].SetActive(false);
                        }
                    }
                }
            }


        }

        private bool IsScreenPosVisible(ref Vector3 screenPos, int splitID)
        {
            //var rect = HUDManager.Instance.HUDCanvas.GetComponent<RectTransform>().rect;

            float y1 = 0f;
            float y2 = 1080f;

            // x is always the same (1920)
            screenPos.x *= HUDManager.Rel(1920f);

            if (SplitScreenManager.Instance.LocalPlayerCount == 1)
            {
                // single player height is 1080
                screenPos.y *= HUDManager.Rel(1080f, true);

                bool flag = screenPos.z > 0f && screenPos.x >= 0f && screenPos.x <= 1920f && screenPos.y >= y1 && screenPos.y <= y2;

                return flag;
            }
            else
            {
                // split height is 540
                screenPos.y *= HUDManager.Rel(540f, true);

                if (splitID == 0)
                {
                    y1 = HUDManager.Rel(540f, true);
                    screenPos.y += y1;

                    bool flag = screenPos.z > 0f && screenPos.x >= 0f && screenPos.x <= HUDManager.Rel(1920f) && screenPos.y >= y1 && screenPos.y <= y2;
                    return flag;
                }
                else
                {
                    y2 = HUDManager.Rel(540f, true);

                    bool flag = screenPos.z > 0f && screenPos.x >= 0f && screenPos.x <= HUDManager.Rel(1920f) && screenPos.y >= y1 && screenPos.y <= y2;
                    return flag;
                }
            }
        }

        public void AddDamageLabel(DamageList damageList, Vector3 hitPosition, Character target)
        {
            if (damageList.TotalDamage < (float)HUDManager.config.GetValue(Settings.MinimumDamage))
            {
                return;
            }

            if (target.IsAI && !(bool)HUDManager.config.GetValue(Settings.PlayerDamageLabels))
            {
                return;
            }
            if (!target.IsAI && !(bool)HUDManager.config.GetValue(Settings.EnemyDamageLabels))
            {
                return;
            }

            Color damagecolor = Color.white;
            
            if (!(bool)HUDManager.config.GetValue(Settings.DisableColors))
            {
                float highest = 0f;
                foreach (DamageType type in damageList.List)
                {
                    if (type.Damage > highest)
                    {
                        highest = type.Damage;
                        damagecolor = GetDmgColor(type.Type);
                    }
                }
            }

            var x = HUDManager.Rel(30f);
            var y = HUDManager.Rel(15f, true);

            DamageLabel label = new DamageLabel
            {
                CreationTime = Time.time,
                TextColor = damagecolor,
                Damage = damageList.TotalDamage,
                HitWorldPos = hitPosition,
                Target = target,
                ranXpos = UnityEngine.Random.Range(-x, x),
                ranYpos = UnityEngine.Random.Range(-y, y)
            };

            ActiveLabels.Add(label);
        }

        public Color GetDmgColor(DamageType.Types dtype)
        {
            Color color;
            switch (dtype)
            {
                case DamageType.Types.Physical:
                    color = Global.WHITE_GRAY;
                    break;
                case DamageType.Types.Ethereal:
                    color = Color.magenta;
                    break;
                case DamageType.Types.Decay:
                    color = Global.LIGHT_GREEN;
                    break;
                case DamageType.Types.Electric:
                    color = Color.yellow;
                    break;
                case DamageType.Types.Frost:
                    color = Global.BLUE;
                    break;
                case DamageType.Types.Fire:
                    color = lightRed;
                    break;
                case DamageType.Types.Raw:
                default:
                    color = Color.white;
                    break;
            }
            return color;
        }

        public Color lightRed = new Color
        {
            r = 1.0f,
            g = 0.51f,
            b = 0.51f,
            a = 1.0f
        };

        // =============== HOOKS ================ //

        private void ProjectileHitHook(On.ProjectileWeapon.orig_HasHit orig, ProjectileWeapon self, Character _hitCharacter, Vector3 _hitPos, Vector3 _dir, bool _blocked)
        {
            Character selfChar = At.GetValue(typeof(Item), self as Item, "m_ownerCharacter") as Character;

            if (At.GetValue(typeof(Weapon), self as Weapon, "m_alreadyHitChars") is List<Character> alreadyhit)
            {
                bool eligible = (_hitCharacter != null) && (_hitCharacter != selfChar) && (self.CanHitEveryoneButOwner || selfChar.TargetingSystem.IsTargetable(_hitCharacter));

                if (eligible && !alreadyhit.Contains((Character)_hitCharacter))
                {
                    if (!_blocked)
                    {
                        DamageList damages = self.GetDamage(0);
                        _hitCharacter.Stats.GetMitigatedDamage((Tag[])null, ref damages);

                        AddDamageLabel(damages, _hitPos, _hitCharacter);
                    }
                    else
                    {
                        // Attack was blocked.
                    }
                }
            }

            orig(self, _hitCharacter, _hitPos, _dir, _blocked);
        }

        private void WeaponHitHook(On.Weapon.orig_HasHit orig, Weapon self, RaycastHit _hit, Vector3 _dir)
        {
            // basically copies the orig function, but instead of applying any damage, it just adds the label
            var target = _hit.collider.GetComponent<Hitbox>();
            var item = self as Item;
            Character selfChar = At.GetValue(typeof(Item), item, "m_ownerCharacter") as Character;

            if (At.GetValue(typeof(Weapon), self, "m_alreadyHitChars") is List<Character> alreadyhit)
            {
                bool eligible = (target.OwnerChar != null) && (target.OwnerChar != selfChar) && (self.CanHitEveryoneButOwner || selfChar.TargetingSystem.IsTargetable(target.OwnerChar));

                if (eligible && !alreadyhit.Contains(target.OwnerChar))
                {
                    float num = Vector3.Angle(target.OwnerChar.transform.forward, selfChar.transform.position - target.OwnerChar.transform.position);

                    if (!self.Unblockable && target.OwnerChar.Blocking && num < (float)((!target.OwnerChar.ShieldEquipped) ? Weapon.BLOCK_ANGLE : Weapon.SHIELD_BLOCK_ANGLE))
                    {
                        // Debug.Log("Blocked!");
                    }
                    else
                    {
                        var getID = At.GetValue(typeof(Weapon), self, "m_attackID");
                        if (getID is int attackID && attackID >= 0)
                        {
                            DamageList damages = self.GetDamage(attackID);

                            target.OwnerChar.Stats.GetMitigatedDamage(null, ref damages);

                            AddDamageLabel(damages, _hit.point, target.OwnerChar);
                        }
                    }
                }
            }

            // orig
            orig(self, _hit, _dir);
        }

        private void PunctualHook(On.PunctualDamage.orig_DealHit orig, PunctualDamage self, Character target)
        {
            // orig
            orig(self, target);

            // custom
            if (target.Alive)
            {
                if (At.GetValue(typeof(PunctualDamage), self, "m_tempList") is DamageList damagelist)
                {
                    DamageList damages = damagelist.Clone();
                    target.Stats.GetMitigatedDamage(null, ref damages);

                    AddDamageLabel(damages, target.CenterPosition, target);
                }
            }
        }

        private void WeaponSkillHook(On.WeaponDamage.orig_DealHit orig, WeaponDamage self, Character target)
        {
            // orig
            orig(self, target);

            // custom
            if (target.Alive)
            {
                PunctualDamage punctualSelf = self as PunctualDamage;

                if (At.GetValue(typeof(PunctualDamage), punctualSelf, "m_tempList") is DamageList damagelist)
                {
                    DamageList damages = damagelist.Clone();
                    target.Stats.GetMitigatedDamage(null, ref damages);

                    AddDamageLabel(damages, target.CenterPosition, target);
                }
            }
        }

        public class DamageLabel
        {
            public float CreationTime;
            public Vector3 HitWorldPos;
            public Character Target;

            public float Damage;
            public Color TextColor;

            public float ranXpos;
            public float ranYpos;
        }

        
    }
}
