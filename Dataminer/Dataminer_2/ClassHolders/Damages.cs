using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataminer
{
    public class Damages
    {
        public float Damage;
        public string Damage_Type;

        public static List<Damages> ParseDamageList(DamageList list)
        {
            var damages = new List<Damages>();

            foreach (DamageType type in list.List)
            {
                damages.Add(ParseDamageType(type));
            }

            return damages;
        }

        public static List<Damages> ParseDamageArray(DamageType[] types)
        {
            List<Damages> damages = new List<Damages>();

            foreach (DamageType type in types)
            {
                damages.Add(ParseDamageType(type));
            }

            return damages;
        }

        public static Damages ParseDamageType(DamageType damage)
        {
            return new Damages
            {
                Damage = damage.Damage,
                Damage_Type = damage.Type.ToString()
            };
        }
    }
}
