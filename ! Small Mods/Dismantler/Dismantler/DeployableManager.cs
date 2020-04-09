using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Dismantler
{
    public class DeployableManager : MonoBehaviour
    {
        public static DeployableManager Instance;

        internal void Awake()
        {
            Instance = this;

			On.Deployable.InitFromItem += Deployable_InitFromItem;

			SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

		private void Deployable_InitFromItem(On.Deployable.orig_InitFromItem orig, Deployable self)
		{
			orig(self);

			if (self.IsDeployed && self.PackedStateItemPrefab == null)
			{
				AddDestroyInteraction(self);
			}
		}

		private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
		{
			StartCoroutine(WaitForSceneReady());
		}

		private IEnumerator WaitForSceneReady()
		{
			while (!NetworkLevelLoader.Instance.IsOverallLoadingDone || !NetworkLevelLoader.Instance.AllPlayerDoneLoading)
			{
				yield return null;
			}

			foreach (var deployable in ItemManager.Instance.WorldItems.Values.Where(x => x.GetExtension("Deployable") != null))
			{
				var comp = deployable.GetComponent<Deployable>();

				if (comp.IsDeployed && comp.PackedStateItemPrefab == null)
				{
					AddDestroyInteraction(comp);
				}
			}
		}

		private void AddDestroyInteraction(Deployable self)
		{
			if (self.PackedStateItemPrefab == null)
			{
				var m_item = self.Item;

				Debug.Log("DeployableManager: Adding InteractionDestroy to " + m_item.Name + " (" + m_item.UID + ")");


				var holder = m_item.InteractionHolder;

				// add custom "Destroy" component
				var comp = m_item.InteractionHolder.AddComponent<InteractionDestroy>();

				// set up the activator
				var activator = holder.GetComponent<InteractionActivator>();
				activator.AddHoldInteractionOverride(comp);

				var triggerbase = holder.GetComponent<InteractionTriggerBase>();				
				At.SetValue(triggerbase, typeof(Deployable), self, "m_disassembleInteractionTrigger");

				self.RefreshDisassembleCollider();
			}
		}
    }
}
