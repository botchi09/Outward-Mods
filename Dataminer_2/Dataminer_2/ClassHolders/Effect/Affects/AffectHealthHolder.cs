using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataminer_2
{
    public class AffectHealthHolder : EffectHolder
    {
        public float AffectQuantity;
        public bool IsModifier;

        public static AffectHealthHolder ParseAffectHealth(AffectHealth affectHealth, EffectHolder _effectHolder)
        {
            var affectHealthHolder = new AffectHealthHolder
            {
                AffectQuantity = affectHealth.AffectQuantity,
                IsModifier = affectHealth.IsModifier
            };

            At.InheritBaseValues(affectHealthHolder, _effectHolder);

            return affectHealthHolder;
        }
    }
}
