using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dataminer_2
{
    public class TrapHolder : ItemHolder
    {
        public List<TrapEffectHOlder> TrapEffects = new List<TrapEffectHOlder>();

        public static TrapHolder ParseTrap(DeployableTrap trap, ItemHolder itemHolder)
        {
            var trapHolder = new TrapHolder();
            At.InheritBaseValues(trapHolder, itemHolder);

            if (At.GetValue(typeof(DeployableTrap), trap, "m_trapRecipes") is TrapEffectRecipe[] recipes && recipes.Length > 0)
            {
                foreach (TrapEffectRecipe recipe in recipes)
                {
                    var effectHolder = TrapEffectHOlder.ParseTrapEffect(recipe);

                    trapHolder.TrapEffects.Add(effectHolder);
                }
            }

            return trapHolder;
        }
    }
}
