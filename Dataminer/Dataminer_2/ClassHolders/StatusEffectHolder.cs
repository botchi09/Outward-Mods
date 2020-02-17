using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;

namespace Dataminer
{
    public class StatusEffectHolder
    {
        public string Name;
        public int PresetID;

        public float Lifespan;
        public string LengthType;

        public List<string> Tags = new List<string>();

        public bool CanBeHealedBySleeping;
        public int StraightSleepHealTime = -1;

        public List<EffectHolder> Effects = new List<EffectHolder>();

        public static StatusEffectHolder ParseStatusEffect(EffectPreset effectPreset)
        {
            var statusEffectHolder = new StatusEffectHolder
            {
                PresetID = effectPreset.PresetID
            };

            if (effectPreset is ImbueEffectPreset imbuePreset)
            {
                statusEffectHolder.Name = imbuePreset.Name;

                foreach (Effect effect in effectPreset.GetComponentsInChildren<Effect>())
                {
                    var effectHolder = EffectHolder.ParseEffect(effect);
                    if (effectHolder != null)
                    {
                        statusEffectHolder.Effects.Add(effectHolder);
                    }
                }

            }
            else if (effectPreset.GetComponent<StatusEffect>() is StatusEffect status)
            {
                statusEffectHolder.Name = status.IdentifierName;
                statusEffectHolder.Lifespan = status.StartLifespan;
                statusEffectHolder.LengthType = status.LengthType.ToString();

                foreach (Tag tag in status.InheritedTags)
                {
                    statusEffectHolder.Tags.Add(tag.TagName);
                    ListManager.AddTagSource(tag, statusEffectHolder.Name);
                }

                if (status is Disease disease)
                {
                    statusEffectHolder.CanBeHealedBySleeping = disease.CanBeHealedBySleeping;
                    statusEffectHolder.StraightSleepHealTime = disease.StraightSleepHealTime;
                }

                foreach (Effect effect in effectPreset.GetComponentsInChildren<Effect>())
                {
                    var effectHolder = EffectHolder.ParseEffect(effect);
                    if (effectHolder != null)
                    {
                        statusEffectHolder.Effects.Add(effectHolder);
                    }
                }

                // Vital recovery effects (stack level)
                var sigmode = (StatusEffect.EffectSignatureModes)At.GetValue(typeof(StatusEffect), status, "m_effectSignatureMode");
                if (sigmode == StatusEffect.EffectSignatureModes.Reference)
                {
                    foreach (Effect effect in status.StatusEffectSignature?.GetComponentsInChildren<Effect>())
                    {
                        var effectHolder = EffectHolder.ParseEffect(effect);
                        if (effectHolder != null)
                        {
                            statusEffectHolder.Effects.Add(effectHolder);
                        }
                    }
                }

                for (int i = 0; i < status.StatusData.EffectsData.Length; i++)
                {
                    if (i >= statusEffectHolder.Effects.Count)
                    {
                        Debug.LogWarning("we exceeded our effectholder count...");
                        continue;
                    }

                    // burning and poison. 
                    // this ignores a lot of edge cases, but burning and poison are the only cases atm.
                    // both effects only use one value in the statusdata, used for the damage on players.
                    if (statusEffectHolder.Effects[i] is PunctualDamageHolder)
                    {
                        var strings = status.StatusData.EffectsData[i].Data[0].Split(new char[] { ':' });
                        var value = float.Parse(strings[0]);
                        (statusEffectHolder.Effects[i] as PunctualDamageHolder).Damage[0].Damage = value;
                    }
                    else
                    {
                        // everything else
                        try
                        {
                            FieldInfo fi = statusEffectHolder.Effects[i].GetType().GetField("AffectQuantity");
                            fi.SetValue(statusEffectHolder.Effects[i], float.Parse(status.StatusData.EffectsData[i].Data[0]));
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("Exception parsing StausData: " + e.Message);
                        }
                    }
                }
            }

            return statusEffectHolder;
        }

        public static void ParseAllEffects()
        {
            if (At.GetValue(typeof(ResourcesPrefabManager), null, "EFFECTPRESET_PREFABS") is Dictionary<int, EffectPreset> dict)
            {
                foreach (EffectPreset preset in dict.Values)
                {
                    Debug.Log("Parsing Effect Preset " + preset.gameObject.name);

                    var statusHolder = ParseStatusEffect(preset);

                    if (!string.IsNullOrEmpty(statusHolder.Name))
                    {
                        string dir = Folders.Prefabs + "/Effects";
                        string saveName = statusHolder.Name;

                        ListManager.Effects.Add(statusHolder.PresetID.ToString(), statusHolder);

                        Dataminer.SerializeXML(dir, saveName, statusHolder, typeof(StatusEffectHolder));
                    }
                }
            }
            else
            {
                Debug.LogError("Could not find Effect Prefabs!");
            }
        }
    }
}
