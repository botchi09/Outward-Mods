using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
//using SinAPI;
using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using NodeCanvas.Tasks.Actions;
using SideLoader;

namespace NecromancerSkills
{
    public class TrainerManager : MonoBehaviour
    {
        // This class is just used for setting up the Trainer NPC. The Skill Tree is set up by the SkillManager.

        //public ModBase global;
        public static TrainerManager Instance;

        public static string TrainerName = "Spectral Wanderer";
        public static string TrainerSceneName = "Hallowed_Dungeon4_Interior";
        public Vector3 TrainerLocation = new Vector3(-138.3397f, 58.99699f, -102.4192f); 
        public static List<int> TrainerEquipment = new List<int> { 3200040, 3200041, 3200042 }; // blue ghost robes

        internal void Awake()
        {
            Instance = this;

            SL.OnSceneLoaded += OnSceneChange;
        }

        private void OnSceneChange()
        {
            if (GameObject.Find("UNPC_" + TrainerName) is GameObject obj)
            {
                //OLogger.Warning("Trainer already exists on scene change, skipping setup!");
                if (SceneManagerHelper.ActiveSceneName != TrainerSceneName)
                {
                    //Debug.Log("Destroying trainer");
                    DestroyImmediate(obj);
                }
                return;
            }

            if (SceneManagerHelper.ActiveSceneName == TrainerSceneName && !PhotonNetwork.isNonMasterClientInRoom)
            {
                HostSetupTrainer();
            }
        }

        private GameObject HostSetupTrainer()
        {
            GameObject trainer;

            // ============= setup base NPC object ================

            var pos = TrainerLocation;

            var uid = UID.Generate().ToString();
            var viewID = PhotonNetwork.AllocateSceneViewID();

            trainer = CustomCharacters.InstantiatePlayerPrefab(pos, uid);

            // setup Equipment
            Character c = trainer.GetComponent<Character>();
            At.SetValue(CharacterManager.CharacterInstantiationTypes.Item, typeof(Character), c, "m_instantiationType");
            foreach (int id in TrainerEquipment)
            {
                c.Inventory.Equipment.EquipInstantiate(ResourcesPrefabManager.Instance.GetItemPrefab(id) as Equipment);
            }

            // set faction to NONE
            c.ChangeFaction(Character.Factions.NONE); 

            // call RPC for non-hosts to set up the character
            //RPCManager.Instance.SendTrainerSpawn(uid, viewID);
            RPCManager.Instance.photonView.RPC("SendTrainerSpawn", PhotonTargets.All, new object[] { uid.ToString(), viewID });

            trainer.SetActive(true);
            return trainer;
        }

        public void LocalTrainerSetup(GameObject trainer, int trainerViewID)
        {
            var obj = new GameObject("UNPC_" + TrainerName);
            obj.transform.position = TrainerLocation;

            trainer.transform.parent = obj.transform;
            trainer.transform.position = obj.transform.position;

            if (trainer.GetPhotonView() is PhotonView view)
            {
                Destroy(view);
            }
            trainer.AddComponent(new PhotonView
            {
                viewID = trainerViewID,
                onSerializeTransformOption = OnSerializeTransform.All
            });

            // remove unwanted components
            DestroyImmediate(trainer.GetComponent<PlayerCharacterStats>());
            DestroyImmediate(trainer.GetComponent<StartingEquipment>());

            // add NPCLookFollow component
            trainer.AddComponent<NPCLookFollow>();

            // =========== setup Trainer DialogueTree from the template ===========

            var trainertemplate = Instantiate(Resources.Load("editor/templates/TrainerTemplate")) as GameObject;
            trainertemplate.transform.parent = trainer.transform;
            trainertemplate.transform.position = trainer.transform.position;

            // set Dialogue Actor name
            var necroActor = trainertemplate.GetComponentInChildren<DialogueActor>();
            necroActor.SetName(TrainerName);

            // get "Trainer" component, and set the SkillTreeUID to our custom tree UID
            Trainer trainerComp = trainertemplate.GetComponentInChildren<Trainer>();
            At.SetValue(SkillManager.NecromancyTree.UID, typeof(Trainer), trainerComp, "m_skillTreeUID");

            // setup dialogue tree
            var graphController = trainertemplate.GetComponentInChildren<DialogueTreeController>();
            var graph = (graphController as GraphOwner<DialogueTreeExt>).graph;

            // the template comes with an empty ActorParameter, we can use that for our NPC actor.
            var actors = At.GetValue(typeof(DialogueTree), graph as DialogueTree, "_actorParameters") as List<DialogueTree.ActorParameter>;
            actors[0].actor = necroActor;
            actors[0].name = necroActor.name;

            // setup the actual dialogue now
            List<Node> nodes = At.GetValue(typeof(Graph), graph, "_nodes") as List<Node>;

            var rootStatement = graph.AddNode<StatementNodeExt>();
            rootStatement.statement = new Statement("Do you seek to harness the power of Corruption, traveler?");
            rootStatement.SetActorName(necroActor.name);

            var multiChoice1 = graph.AddNode<MultipleChoiceNodeExt>();
            multiChoice1.availableChoices.Add(new MultipleChoiceNodeExt.Choice { statement = new Statement { text = "I'm interested, what can you teach me?" } });
            multiChoice1.availableChoices.Add(new MultipleChoiceNodeExt.Choice { statement = new Statement { text = "Who are you?" } });
            multiChoice1.availableChoices.Add(new MultipleChoiceNodeExt.Choice { statement = new Statement { text = "What is this place?" } });

            // the template already has an action node for opening the Train menu. 
            // Let's grab that and change the trainer to our custom Trainer component (setup above).
            var openTrainer = nodes[1] as ActionNode;
            (openTrainer.action as TrainDialogueAction).Trainer = new BBParameter<Trainer>(trainerComp);

            // create some custom dialogue
            var answer1 = graph.AddNode<StatementNodeExt>();
            answer1.statement = new Statement("I wish I could remember...");
            answer1.SetActorName(necroActor.name);

            var answer2 = graph.AddNode<StatementNodeExt>();
            answer2.statement = new Statement("This is the fortress of the Plague Doctor, a powerful Lich. I've learned a lot about Corruption within these walls.");
            answer2.SetActorName(necroActor.name);

            // ===== finalize nodes =====
            nodes.Clear();
            // add the nodes we want to use
            nodes.Add(rootStatement);
            graph.primeNode = rootStatement;
            graph.ConnectNodes(rootStatement, multiChoice1);    // prime node triggers the multiple choice
            graph.ConnectNodes(multiChoice1, openTrainer, 0);   // choice1: open trainer
            graph.ConnectNodes(multiChoice1, answer1, 1);       // choice2: answer1
            graph.ConnectNodes(answer1, rootStatement);         // - choice2 goes back to root node
            graph.ConnectNodes(multiChoice1, answer2, 2);       // choice3: answer2
            graph.ConnectNodes(answer2, rootStatement);         // - choice3 goes back to root node
            // set root node    

            // set the trainer active
            obj.SetActive(true);
        }
    }
}
