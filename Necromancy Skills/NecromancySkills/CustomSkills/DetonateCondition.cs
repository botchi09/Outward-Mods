using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
//using SinAPI;
//using OModAPI;

namespace NecromancerSkills
{
    public class DetonateCondition : EffectCondition
    {
        public int RequiredSummonEquipment = -1;

        protected override bool CheckIsValid(Character _affectedCharacter)
        {
            //if (RequiredSummonEquipment == -1) { OLogger.Error("Required summon equip has not been set!"); return false; }

            var targetSummon = SummonManager.Instance.FindWeakestSummon(_affectedCharacter.UID);

            if (targetSummon && targetSummon.GetComponentInChildren<Character>() is Character c && c.Inventory.HasEquipped(RequiredSummonEquipment))
            {
                return true;
            }

            return false;
        }
    }
}
