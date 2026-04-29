#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace VRBenchWorks
{
    [CustomEditor(typeof(VRBenchmarkPlayer))]
    public class VRBencmarkRuntimeInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            using (var l = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUIStyle headerStyle = new GUIStyle()
                {
                    fontSize = 24,
                    fontStyle = FontStyle.BoldAndItalic,
                    normal = new GUIStyleState()
                    {
                        textColor = Color.white
                    }
                };
                GUILayout.Label($"VR Benchmark Runtime Player", headerStyle);
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(20);


            if (GUILayout.Button("Select CSV for analysis", GUILayout.Height(50)))
            {
                string path = EditorUtility.OpenFilePanel("Open .CSV benchmark file", "", "csv");
                if (path.Length != 0)
                {
                    VRBenchmarkAnalysisWindow.ShowWindow(path);
                }
            }

            GUILayout.Space(20);


            if (GUILayout.Button("Open Benchmark Editor", GUILayout.Height(50))) 
            {
                VRBenchmarkEditorWindow.ShowWindow(((VRBenchmarkPlayer)target).playback);
            }

            GUILayout.Space(20);

            base.OnInspectorGUI();
        }
    }
}
#endif