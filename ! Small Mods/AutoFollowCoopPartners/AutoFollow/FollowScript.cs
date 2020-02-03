using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using static CustomKeybindings;

namespace AutoFollow
{
    public class FollowScript : MonoBehaviour
    {
        public List<Character> PlayerCharacters = new List<Character>(); // list of all player characters including online
        public Dictionary<int, Character> LocalPlayers = new Dictionary<int, Character>(); // key: local player ID, value: Character of player
        public Dictionary<string, string> CharactersFollowing = new Dictionary<string, string>(); // key: follower UID, value: target UID

        public string FollowKey = "Toggle Auto-Follow";

        public float MinFollowDistance = 1.5f; // minimum distance for autofollow
        public float RotateSpeed = 5f; // speed of camera rotation

        public void Init()
        {
            // CustomKeybindings.cs (credit: Stian)
            AddAction(FollowKey, KeybindingsCategory.Actions, ControlType.Both, 5);

            // sprint input hook
            On.ControlsInput.Sprint += new On.ControlsInput.hook_Sprint(SprintHook);
        }

        // update runs once per frame
        internal void Update()
        {
            // if no local characters, return (and clear lists)
            if (CharacterManager.Instance.PlayerCharacters.Count <= 0)
            {
                if (PlayerCharacters.Count > 0)
                {
                    PlayerCharacters.Clear();
                    LocalPlayers.Clear();
                    CharactersFollowing.Clear();
                }
                return;
            }

            // update character list on change
            if (CharacterManager.Instance.PlayerCharacters.Count != PlayerCharacters.Count())
            {
                UpdateCharacterLists();
            }

            // check each local character for follow input
            foreach (KeyValuePair<int, Character> player in LocalPlayers)
            {
                if (m_playerInputManager[player.Key].GetButtonDown(FollowKey))
                {
                    ToggleFollow(player.Value);
                }
            }
        }

        // toggle the autofollow on and off
        public void ToggleFollow(Character c)
        {
            string uid = c.UID;

            // if currently following, remove this character from the following list (breaks the follow function automatically)
            if (CharactersFollowing.ContainsKey(uid))
            {
                CharactersFollowing.Remove(uid);
            }
            else // otherwise, toggle it on
            {
                FindFollowTarget(c);
            }
        }

        public void FindFollowTarget(Character c)
        {
            string uid = c.UID;
            // find closest player character and follow it
            float currentLowest = -1;
            Character newTarget = null;

            // check all other characters
            foreach (Character c2 in PlayerCharacters.Where(x => x.UID != uid))
            {
                float distance = Vector3.Distance(c2.transform.position, c.transform.position);

                // if this is the first check, or if it is a new lowest distance
                if (currentLowest == -1 || distance < currentLowest)
                {
                    newTarget = c2;
                    currentLowest = distance;
                }
            }

            // if we found any character to follow
            if (newTarget)
            {
                // add the character UIDs to the currently following list, and start the coroutine
                CharactersFollowing.Add(uid, newTarget.UID);
                StartCoroutine(FollowTarget(c, newTarget));
            }
        }

        // follow target coroutine. runs until the UID is removed from the CharactersFollowing list.
        public IEnumerator FollowTarget(Character c, Character target)
        {
            // get the autoRun private field (bool)
            FieldInfo autoRun = c.CharacterControl.GetType().GetField("m_autoRun", BindingFlags.Instance | BindingFlags.NonPublic);

            while (CharactersFollowing.ContainsKey(c.UID))
            {
                // null check for player and target
                if (!c || !target)
                {
                    CharactersFollowing.Remove(c.UID);
                    break;
                }

                // check distance and handle autorun
                float distance = Vector3.Distance(c.transform.position, target.transform.position);

                autoRun.SetValue(c.CharacterControl, distance > MinFollowDistance);

                // rotate the camera to follow the target
                var targetRot = Quaternion.LookRotation(target.transform.position - c.transform.position);
                c.CharacterCamera.transform.rotation = Quaternion.Lerp(c.CharacterCamera.transform.rotation, targetRot, RotateSpeed * Time.deltaTime);

                yield return null;
            }

            // force stop autorun on exit
            if (c) { autoRun.SetValue(c.CharacterControl, false); }
        }

        // update lists of characters
        public void UpdateCharacterLists()
        {
            PlayerCharacters.Clear();
            LocalPlayers.Clear();

            // this list will be the same order that m_playerInputManager uses for local player IDs, so its safe to get them this "blind" way.
            // there will only ever be two local players, and the host character is always 0.
            int localID = 0;

            foreach (string uid in CharacterManager.Instance.PlayerCharacters.Values)
            {
                // add all players (including online) to main list
                Character c = CharacterManager.Instance.GetCharacter(uid);
                PlayerCharacters.Add(c);

                if (c.IsLocalPlayer)
                {
                    LocalPlayers.Add(localID, c);
                    localID++; // increment to local ID counter only when we find a LocalPlayer
                }
            }
        }

        // local ControlsInput sprint hook. overrides when player is following a target (returns target's Character.Sprinting bool)
        public bool SprintHook(On.ControlsInput.orig_Sprint orig, int _playerID)
        {
            // get the uid for this local player id (_playerID will either be 0 or 1)
            string uid = LocalPlayers[_playerID].UID;

            // check if this player is following anyone (if CharactersFollowing contains the uid as a key entry)
            if (CharactersFollowing.ContainsKey(uid))
            {
                // our CharactersFollowing[UID] entry returns the target uid value
                string targetUID = CharactersFollowing[uid];
                
                if (CharacterManager.Instance.GetCharacter(targetUID) is Character target)
                {
                    return target.Sprinting; // if target sprints, we sprint
                }
            }

            // fallback to orig function. its static, so no need for orig(self, _playerID), just orig(_playerID)
            return orig(_playerID);
        }
    }
}
