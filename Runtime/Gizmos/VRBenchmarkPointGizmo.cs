using UnityEngine;

namespace VRBenchWorks
{
    public class VRBenchmarkPointGizmo : MonoBehaviour
    {

        [HideInInspector] public int arrayNumber;
        [HideInInspector] public VRBenchmarkPlaybackData data;

        static public float textOffset = 12f;
        static public float startFont = 12;
        static public float visualScaling = 10;

        public float attachedTime 
        {
            get 
            {
                return data.playbackData[arrayNumber].time;
            }
            set 
            {
                data.playbackData[arrayNumber].time = value;
            }
        }

#if UNITY_EDITOR
        

        private void OnGUI()
        {
            attachedTime = UnityEditor.EditorGUILayout.FloatField(attachedTime);
        }


        private void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, "BaseVRBenchmarkPointGizmo", true);



            // Get Scene view camera
            UnityEditor.SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null) return;
            Camera cam = sceneView.camera;

            // Distance from camera to icon
            float distance = Vector3.Distance(cam.transform.position, transform.position);

            // Scale font based on distance
            int fontSize = Mathf.Max(1, Mathf.RoundToInt(startFont / distance * visualScaling));

            Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);

            // Only render if in front of the camera and within screen bounds
            if (viewportPos.z > 0 && viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1)
            {
                // Compute GUI point
                Vector3 guiPoint = UnityEditor.HandleUtility.WorldToGUIPoint(transform.position);

                GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = fontSize,
                    normal = { textColor = Color.yellow }
                };

                // Apply offset scaled by distance
                float scaledOffset = textOffset / distance * visualScaling;
                guiPoint += new Vector3(0, scaledOffset, 0);

                UnityEditor.Handles.BeginGUI();
                GUI.Label(new Rect(guiPoint.x - 32, guiPoint.y, 64, 64), $"[{arrayNumber}]", style);
                UnityEditor.Handles.EndGUI();
            }

            //Draw Arrow
            DrawArrow.ForGizmo(transform.position, transform.forward);

        }
#endif
    }
}
