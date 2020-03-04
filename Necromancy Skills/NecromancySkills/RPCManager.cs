using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Photon;
using UnityEngine;
//using SinAPI;

namespace NecromancerSkills
{
    public class RPCManager : Photon.MonoBehaviour
    {
        // This class is not used much, just for a little bit of custom Photon functions for setting up summoned AIs and custom trainer NPCs.

        // If you want to a function to be remote-callable, you must declare the [PunRPC] tag above it.
        // This will allow you to do a photonView.RPC("MethodName", PhotonTargets.All, new object[] { parameters for method go here });
        // Note: your parameter arguments MUST be primitive types! (string, int, float, bool, etc).

        public static RPCManager Instance;

        internal void Update()
        {
            if (PhotonNetwork.inRoom && this.photonView == null)
            {
                this.gameObject.AddComponent(new PhotonView() { viewID = 900 });
                Instance = this;
            }
        }

        // ===================== SUMMON MANAGER RPC ===================== // 

        [PunRPC]
        public void SendSummonSpawn(string ownerUID, string summonUID, int sceneViewID, bool insidePlagueAura)
        {
            //Debug.Log("SendSummonSpawn received with UID: " + summonUID + " and scene view ID: " + sceneViewID);

            if (CharacterManager.Instance.GetCharacter(summonUID) is Character c)
            {
                SummonManager.Instance.AddLocalSummon(c, ownerUID, summonUID, sceneViewID, insidePlagueAura);
            }
            else
            {
                StartCoroutine(SummonSpawnCoroutine(ownerUID, summonUID, sceneViewID, insidePlagueAura));
            }
        }

        private IEnumerator SummonSpawnCoroutine(string ownerUID, string summonUID, int sceneViewID, bool insidePlagueAura)
        {
            // Debug.Log("Couldn't get character immediately, starting coroutine to find character...");

            float t = Time.time;

            while (Time.time - t < 5)
            {
                if (CharacterManager.Instance.GetCharacter(summonUID) is Character c)
                {
                    SummonManager.Instance.AddLocalSummon(c, ownerUID, summonUID, sceneViewID, insidePlagueAura);
                    break;
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            //if (!CharacterManager.Instance.GetCharacter(summonUID))
            //{
            //    OLogger.Warning("Timed out trying to find character!");
            //}
        }

        // ===================== TRAINER MANAGER RPC ===================== // 

        [PunRPC]
        public void SendTrainerSpawn(string trainerUID, int sceneViewID)
        {
            //Debug.Log("TrainerSpawn received with UID: " + trainerUID + " and scene view ID: " + sceneViewID);

            if (CharacterManager.Instance.GetCharacter(trainerUID) is Character c)
            {
                TrainerManager.Instance.LocalTrainerSetup(c.gameObject, sceneViewID);
            }
            else
            {
                StartCoroutine(TrainerSetupCoroutine(trainerUID, sceneViewID));
            }
        }

        private IEnumerator TrainerSetupCoroutine(string trainerUID, int trainerViewID)
        {
            // Debug.Log("Couldn't get character immediately, starting coroutine to find character...");

            float t = Time.time;

            while (Time.time - t < 5)
            {
                if (CharacterManager.Instance.GetCharacter(trainerUID) is Character c)
                {
                    TrainerManager.Instance.LocalTrainerSetup(c.gameObject, trainerViewID);
                    break;
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            //if (!CharacterManager.Instance.GetCharacter(trainerUID))
            //{
            //    OLogger.Warning("Timed out trying to find character!");
            //}
        }
    }
}
