using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dataminer
{
    public class WeaponHolder : EquipmentHolder
    {
        public string WeaponType;
        public float StamCost;

        public static WeaponHolder ParseWeapon(Weapon weapon, EquipmentHolder equipmentHolder)
        {
            var weaponHolder = new WeaponHolder
            {
                WeaponType = weapon.Type.ToString(),
                StamCost = weapon.StamCost
            };

            At.InheritBaseValues(weaponHolder, equipmentHolder);

            weaponHolder.StatsHolder = WeaponStatsHolder.ParseWeaponStats(weapon.Stats, equipmentHolder.StatsHolder as EquipmentStatsHolder);

            return weaponHolder;
        }
    }
}
