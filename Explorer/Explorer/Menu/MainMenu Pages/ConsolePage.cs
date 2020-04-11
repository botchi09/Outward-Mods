using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
//using Mono.CSharp;

namespace Explorer
{
    public class ConsolePage : MenuManager.WindowPage
    {
        public override string Name { get => "Console"; set => base.Name = value; }

        private string m_code = "";

        public override void Init()
        {
            //foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            //{
            //    Evaluator.ReferenceAssembly(asm);
            //}
        }

        public override void DrawWindow()
        {
            GUILayout.Label("<b><size=15><color=cyan>Dynamic Method Console</color></size></b>");

            GUILayout.Label("This doesn't work yet, because Unity 5.6.1 with .NET 3.5 uses Mono 2.0.0, which doesn't have enough power.");

            //m_code = GUILayout.TextArea(m_code, GUILayout.Height(500));

            //if (GUILayout.Button("<color=cyan>Run Method Body</color>"))
            //{
            //    try
            //    {
            //        Run(m_code);
            //        //MonoCompiler(m_code);
            //    }
            //    catch (Exception e)
            //    {
            //        Debug.LogError("Exception compiling!\r\nMessage: " + e.Message + "\r\nStack: " + e.StackTrace);
            //    }
            //}
        }

        public override void Update()
        {
        }

        //public bool Run(string cmd)
        //{
        //    Debug.Log("Running: " + cmd);
        //    bool result = Evaluator.Run(cmd);
        //    Debug.Log(result);
        //    return result;
        //}
    }
}