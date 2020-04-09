using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dismantler
{
	public class InteractionDestroy : InteractionDisassemble
	{
		protected override string DefaultHoldText => "Destroy";

		protected override void OnActivate()
		{
			//base.OnActivate();

			var script = At.GetValue(typeof(InteractionDisassemble), this as InteractionDisassemble, "m_deployableScript") as Deployable;

			if (script)
			{
				Debug.Log("Destroying " + script.Item.Name);

				ItemManager.Instance.DestroyItem(script.Item.UID);
			}
		}
	}
}
