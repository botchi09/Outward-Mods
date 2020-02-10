using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dataminer_2
{
    public class ItemHolder
    {
        public int ItemID;
        public string Name;
        public string Description;
        public int LegacyItemID;

        public ItemStatsHolder StatsHolder;

        public List<string> Tags = new List<string>();

        public List<EffectTransformHolder> EffectTransforms = new List<EffectTransformHolder>();

        public static ItemHolder ParseItem(Item item)
        {
            // Debug.Log(item.Name);

            var itemHolder = new ItemHolder
            {
                Name = item.Name,
                Description = item.Description,
                ItemID = item.ItemID,
                LegacyItemID = item.LegacyItemID,
            };

            if (item.Stats != null)
            {
                itemHolder.StatsHolder = ItemStatsHolder.ParseItemStats(item.Stats);
            }

            if (item.Tags != null)
            {
                foreach (Tag tag in item.Tags)
                {
                    itemHolder.Tags.Add(tag.TagName);
                }
            }

            foreach (Transform child in item.transform)
            {
                var effectsChild = EffectTransformHolder.ParseTransform(child);

                if (effectsChild.ChildEffects.Count > 0 || effectsChild.Effects.Count > 0 || effectsChild.EffectConditions.Count > 0)
                {
                    itemHolder.EffectTransforms.Add(effectsChild);
                }
            }

            if (item is Equipment equipment)
            {
                return EquipmentHolder.ParseEquipment(equipment, itemHolder);
            }
            else if (item is DeployableTrap trap)
            {
                return TrapHolder.ParseTrap(trap, itemHolder);
            }
            else
            {
                return itemHolder;
            }
        }
    }
}
