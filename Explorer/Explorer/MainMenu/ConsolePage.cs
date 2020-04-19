using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using Mono.CSharp;
using System.IO;

/*
 * Credits to ManlyMarco (BepInEx team) for the REPL in RuntimeUnityEditor.
 * This is a 'lite' version of that.
*/

namespace Explorer
{
    public class ConsolePage : MainMenu.WindowPage
    {
        public override string Name { get => "Console"; set => base.Name = value; }

        private ScriptEvaluator _evaluator;
        private readonly StringBuilder _sb = new StringBuilder();

        private string m_code = "";

        public override void Init()
        {
            _evaluator = new ScriptEvaluator(new StringWriter(_sb)) { InteractiveBaseClass = typeof(REPL) };

            m_code = "// Welcome to the Outward Explorer Console!" +
                    "\r\n// Enter your C# here as though you were declaring a method body. Press 'Run Code' to evaluate." +
                    "\r\n" +
                    "\r\nDebug.Log(\"Hello World!\");";

            var envSetup = "using System;" +
                           "using UnityEngine;" +
                           "using System.Linq;" +
                           "using System.Collections;" +
                           "using System.Collections.Generic;";

            Evaluate(envSetup);
        }

        public override void Update() { }

        public override void DrawWindow()
        {
            GUILayout.Label("<b><size=15><color=cyan>Dynamic Method Console</color></size></b>");

            m_code = GUILayout.TextArea(m_code, GUILayout.Height(500));

            if (GUILayout.Button("<color=cyan>Run code</color>"))
            {
                try
                {
                    m_code = m_code.Trim();

                    if (!string.IsNullOrEmpty(m_code))
                    {
                        var result = Evaluate(m_code);

                        if (result != null && !Equals(result, VoidType.Value))
                        {
                            Debug.Log("[Console Output]\r\n" + result.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception compiling!\r\nMessage: " + e.Message + "\r\nStack: " + e.StackTrace);
                }
            }
        }

        public object Evaluate(string str)
        {
            object ret = VoidType.Value;

            _evaluator.Compile(str, out var compiled);

            try
            {
                if (compiled == null)
                {
                    Debug.LogWarning("Error compiling!");
                }
                else
                {
                    compiled.Invoke(ref ret);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.ToString());
            }

            return ret;
        }

        private class VoidType
        {
            public static readonly VoidType Value = new VoidType();
            private VoidType() { }
        }

    }

    internal class ScriptEvaluator : Evaluator, IDisposable
    {
        private static readonly HashSet<string> StdLib =
                new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { "mscorlib", "System.Core", "System", "System.Xml" };

        private readonly TextWriter _logger;

        public ScriptEvaluator(TextWriter logger) : base(BuildContext(logger))
        {
            _logger = logger;

            ImportAppdomainAssemblies(ReferenceAssembly);
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            _logger.Dispose();
        }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            string name = args.LoadedAssembly.GetName().Name;
            if (StdLib.Contains(name))
                return;
            ReferenceAssembly(args.LoadedAssembly);
        }

        private static CompilerContext BuildContext(TextWriter tw)
        {
            var reporter = new StreamReportPrinter(tw);

            var settings = new CompilerSettings
            {
                Version = LanguageVersion.Experimental,
                GenerateDebugInfo = false,
                StdLib = true,
                Target = Target.Library,
                WarningLevel = 0,
                EnhancedWarnings = false
            };

            return new CompilerContext(settings, reporter);
        }

        private static void ImportAppdomainAssemblies(Action<Assembly> import)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (StdLib.Contains(name))
                    continue;
                import(assembly);
            }
        }
    }
}