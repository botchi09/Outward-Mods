using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataminer_2
{
    public class PunctualDamageHolder : EffectHolder
    {
        public List<Damages> Damage = new List<Damages>();
        public List<Damages> Damages_AI = new List<Damages>();
        public float Knockback;
        public bool HitInventory;

        public static PunctualDamageHolder ParsePunctualDamage(PunctualDamage damage, EffectHolder effectHolder)
        {
            var punctualDamageHolder = new PunctualDamageHolder
            {
                Knockback = damage.Knockback,
                HitInventory = damage.HitInventory
            };

            At.InheritBaseValues(punctualDamageHolder, effectHolder);

            punctualDamageHolder.Damage = Damages.ParseDamageArray(damage.Damages);
            punctualDamageHolder.Damages_AI = Damages.ParseDamageArray(damage.DamagesAI);

            if (damage is WeaponDamage)
            {
                return WeaponDamageHolder.ParseWeaponDamage(damage as WeaponDamage, punctualDamageHolder);
            }

            return punctualDamageHolder;
        }
    }
}
