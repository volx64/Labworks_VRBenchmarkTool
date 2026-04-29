#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace VRBenchWorks
{
    [CustomEditor(typeof(VRBenchmarkPointGizmo))]
    public class VRBenchmarkGizmoInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            VRBenchmarkPointGizmo gizmo = (VRBenchmarkPointGizmo)target;

            Undo.RecordObject(gizmo, "Change Time");

            gizmo.attachedTime = EditorGUILayout.FloatField("Time To Reach", gizmo.attachedTime);

            if (GUI.changed) 
            {
                EditorUtility.SetDirty(gizmo);
            }
        }
    }
}
#endif