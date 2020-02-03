using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
//using SinAPI;

namespace NecromancerSkills
{
	public class SummonManager : MonoBehaviour
    {
        //public ModBase global;
        public static SummonManager Instance;

		public Dictionary<string, List<string>> SummonedCharacters = new Dictionary<string, List<string>>(); // Key: Caster UID, Value: List of Summon UIDs

		private float lastUpdateTime = -1f;

		#region Initialization and Tick Update

		internal void Awake()
		{
			if (Instance == null) { Instance = this; }
		}

		// the tick update is limited to 0.5 secs, since its just for cleaning up dead summons and low priority stuff.
		internal void Update()
		{
			if (SummonedCharacters.Count > 0 && Time.time - lastUpdateTime > 0.5f)
			{
				lastUpdateTime = Time.time;
				UpdateSummonedCharacters();
			}
		}

		private void UpdateSummonedCharacters()
		{
			foreach (KeyValuePair<string, List<string>> entry in SummonedCharacters)
			{
				List<string> toRemove = new List<string>();

				foreach (string uid in entry.Value)
				{
					if (CharacterManager.Instance.GetCharacter(uid) is Character c)
					{
						// clear dead resurrects
						if (c.IsDead)
						{
							//OLogger.Warning(c.Name + " is dead! Removing from list and destroying object.");
							DestroySummon(c.transform.parent.gameObject);
							toRemove.Add(uid);
						}
					}
					else
					{
						//OLogger.Warning("CharacterManager GetCharacter " + uid + " is null! Removing from list.");
						toRemove.Add(uid);
					}
				}

				if (toRemove.Count() > 0)
				{
					foreach (string uid in toRemove)
					{
						entry.Value.Remove(uid);
					}
				}
			}
		}
		#endregion

		// find the weakest current summon for a character. can be used arbitrarily by anything.
		public GameObject FindWeakestSummon(string ownerUID)
		{
			UpdateSummonedCharacters(); // force update of characters to remove dead ones etc

			GameObject _obj = null;

			if (SummonedCharacters.ContainsKey(ownerUID) && SummonedCharacters[ownerUID].Count() > 0)
			{
				float lowest = float.MaxValue;
				foreach (string uid in SummonedCharacters[ownerUID])
				{
					if (CharacterManager.Instance.GetCharacter(uid) is Character c && c.Stats.CurrentHealth < lowest)
					{
						lowest = c.Stats.CurrentHealth;
						_obj = c.gameObject;
					}
				}
			}

			return _obj;
		}

		public static void DestroySummon(GameObject summon)
		{
			if (summon.GetComponentInChildren<Character>() is Character c)
			{
				// just disable to "kill" the npc. safest way for now to ensure smooth online play, can cause bugs if you try destroy a character.
				summon.SetActive(false);
			}
		}

		// Only HOST should call this directly. This is the Main function for creating a summon. In our case, a skeleton minion.
		// See the "SummonSkeleton" class for an example of how this works.

		public GameObject SummonSpawn(Character caster, string summonUID, int sceneViewID, bool insidePlagueAura)
		{
			//Debug.Log("Create base character: " + summonUID + ", scene view: " + sceneViewID);

			Vector3 castPos = caster.transform.position + (Vector3.forward * 0.5f);

			// use SideLoader's CustomCharacters to create a basic player prefab
			var playerPrefab = SideLoader.CustomCharacters.InstantiatePlayerPrefab(castPos, summonUID, sceneViewID);

			Character _char = playerPrefab.GetComponent<Character>();
			_char.SetUID(summonUID);
			_char.photonView.viewID = sceneViewID;

			float healthLoss = insidePlagueAura ? -2.5f : -0.75f;
			float maxHealth = insidePlagueAura ? 250f : 75f;

			// set custom stats
			At.SetValue(new Stat(healthLoss), typeof(CharacterStats), _char.Stats, "m_healthRegen");
			At.SetValue(new Stat(maxHealth), typeof(CharacterStats), _char.Stats, "m_maxHealthStat");
			At.SetValue(new Stat(500f), typeof(CharacterStats), _char.Stats, "m_maxStamina");

			if (insidePlagueAura)
			{
				foreach (int id in TrainerManager.TrainerEquipment) // equip the spectral ghost set
				{
					_char.Inventory.Equipment.EquipInstantiate(ResourcesPrefabManager.Instance.GetItemPrefab(id) as Equipment);
				}
				// 5400010_BackpackStatBooster2
				_char.Inventory.Equipment.EquipInstantiate(ResourcesPrefabManager.Instance.GetItemPrefab(5400010) as Equipment);
			}
			else
			{
				// equip Mertons Set
				_char.Inventory.EquipInstantiate(ResourcesPrefabManager.Instance.GetItemPrefab(3200030) as Equipment);
				_char.Inventory.EquipInstantiate(ResourcesPrefabManager.Instance.GetItemPrefab(3200031) as Equipment);
				_char.Inventory.EquipInstantiate(ResourcesPrefabManager.Instance.GetItemPrefab(3200032) as Equipment);
			}

			// setup custom weapon (using SideLoader, requires slightly different method to equip)
			var blade = ItemManager.Instance.GenerateItemNetwork(2598500) as Weapon;
			_char.Inventory.TakeItem(blade.UID);
			At.Call(_char.Inventory.Equipment, "EquipWithoutAssociating", new object[] { blade as Equipment, false });
			_char.SheatheInput(); //unsheathe

			//Debug.Log("(Host) Summoned Skeleton with UID: " + _char.UID + ", photon view ID: " + _char.GetComponent<PhotonView>().viewID);

			playerPrefab.SetActive(true);

			return playerPrefab;
		}

		// local summon setup. This is called from RPCmanager.SendSummonSpawn.
		// Used for doing the local init for a custom summon, including adding the AI

		public void AddLocalSummon(Character summonChar, string ownerUID, string summonUID, int sceneViewID, bool insidePlagueAura = false)
		{
			// setup parent transform
			GameObject rootObject = new GameObject(summonChar.Name + "_" + summonUID);
			rootObject.SetActive(false);
			summonChar.transform.parent = rootObject.transform;

			// set name
			At.SetValue("SkeletonAlly", typeof(Character), summonChar, "m_name");

			// setup basic AI components
			SideLoader.CustomCharacters.SetupBasicAI(summonChar);

			// only the host should do this bit
			if (!PhotonNetwork.isNonMasterClientInRoom)
			{
				// get the Wander state, and set the FollowTransfer to our caster character
				var wander = summonChar.GetComponent<CharacterAI>().AiStates[0] as AISWander;
				wander.FollowTransform = CharacterManager.Instance.GetCharacter(ownerUID).transform;

				// add auto-teleport component
				summonChar.gameObject.AddComponent(new SummonTeleport { m_character = summonChar, TargetCharacter = wander.FollowTransform });
			}

			// set photon view locally
			summonChar.photonView.viewID = sceneViewID;

			// restore stats locally
			summonChar.Stats.FullHealth();
			summonChar.Stats.FullStamina();

			// set skeleton active
			rootObject.SetActive(true);

			// add to dictionary
			if (SummonedCharacters.ContainsKey(ownerUID))
			{
				SummonedCharacters[ownerUID].Add(summonUID);
			}
			else
			{
				SummonedCharacters.Add(ownerUID, new List<string> { summonUID });
			}

			//Debug.Log("added local summon: " + summonUID);
		}
	}
}
