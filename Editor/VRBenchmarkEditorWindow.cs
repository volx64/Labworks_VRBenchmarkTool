#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace VRBenchWorks
{
    public class VRBenchmarkEditorWindow : EditorWindow
    {


        VRBenchmarkPlaybackData playbackData;
        public VRBenchmarkPlaybackData newPlaybackData;

        SerializedObject serializedData;
        ReorderableList reorderableList;
        List<VRBenchmarkPointGizmo> sceneGizmos = new List<VRBenchmarkPointGizmo>();
        GameObject parentGizmo;

        float debugTimeCamera;

        // Add menu item
        [MenuItem("Labworks/VR Benchmark Editor")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow<VRBenchmarkEditorWindow>("VR Benchmark Editor");


        }

        public static void ShowWindow(VRBenchmarkPlaybackData data) 
        {
            VRBenchmarkEditorWindow localWindow = EditorWindow.GetWindow<VRBenchmarkEditorWindow>("VR Benchmark Editor");

            localWindow.newPlaybackData = data;
        }


        void OnGUI()
        {
            using (var l = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUIStyle headerStyle = new GUIStyle()
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    normal = new GUIStyleState()
                    {
                        textColor = Color.white
                    }
                };
                GUILayout.Label($"VR Benchmark Editor", headerStyle);
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(20);


            using (var l = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"VR Benchmark Editor");
                GUILayout.FlexibleSpace();
            }

            using (var l = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20f);
                newPlaybackData = (VRBenchmarkPlaybackData)EditorGUILayout.ObjectField("Benchmark Data",
                          newPlaybackData,
                          typeof(VRBenchmarkPlaybackData),
                          false);
                GUILayout.Space(20f);
            }

          

            GUILayout.FlexibleSpace();


            if (newPlaybackData == null) 
            {
                using (var l = new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(20);
                    if (GUILayout.Button("Create New Benchmark Data", GUILayout.Height(100))) 
                    {
                        CreateNewBenchmark();
                    }
                    GUILayout.Space(20);
                }


                using (var l = new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUIStyle headerStyle = new GUIStyle()
                    {
                        fontSize = 30,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.UpperCenter,
                        normal = new GUIStyleState()
                        {
                            textColor = Color.red
                        }
                    };
                    GUILayout.Label($"    Benchmark Data Is Null!\nPlease Populate Benchmark Field", headerStyle);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace(); 
                GUILayout.FlexibleSpace();
                return;
            }


            EditUI();
        }

        private void DebugJumpToFrame(float time) 
        {
            JumpEditorCamToFrameTime(time, playbackData);
        }

        public static void JumpEditorCamToFrameTime(float time, VRBenchmarkPlaybackData playbackData)
        {
            if (playbackData == null) 
            {
                Debug.LogError("No playback data found, cannot jump to frameTime");
                return;
            }


            float totalTime = 0;
            for (int i = 0; i < playbackData.playbackData.Count; i++)
            {
                if (totalTime + playbackData.playbackData[i].time > time) //Found frame
                {
                    float lerpValue = (time - totalTime) / (playbackData.playbackData[i].time);

                    // Get Scene view camera
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    if (sceneView == null || sceneView.camera == null)
                    {
                        EditorUtility.DisplayDialog("Null Scene Camera", "Could not find scene view! Please open a scene view to grab from", "Ok");
                        return;
                    }
                    Camera cam = sceneView.camera;

                    sceneView.pivot = Vector3.Lerp(playbackData.playbackData[i - 1].position,
                        playbackData.playbackData[i].position, lerpValue);
                    sceneView.rotation = Quaternion.Lerp(playbackData.playbackData[i].rotation,
                        playbackData.playbackData[i].rotation, lerpValue);
                    sceneView.size = 0f;
                    sceneView.Repaint();

                    return;
                }
                totalTime += playbackData.playbackData[i].time;
            }

        }


        /// <summary>
        /// Assumes benchmark data has been populated properly!
        /// </summary>
        private void EditUI()
        {
            if (!CheckExistingGizmos())
            {
                playbackData = newPlaybackData;
                CreateBenchmarkGizmos();
            }

            using (var l = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);

                debugTimeCamera = EditorGUILayout.FloatField("Frame Time", debugTimeCamera);

                GUILayout.Space(80);
            }

            GUILayout.Space(5);

            using (var l = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(120);

                if (GUILayout.Button("Jump To Frame", GUILayout.Height(40)))
                {
                    DebugJumpToFrame(debugTimeCamera);
                }
                GUILayout.Space(20);
            }


            GUILayout.Space(20);



            using (var l = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                if (GUILayout.Button("Create New Point From Scene Camera", GUILayout.Height(40)))
                {
                    AddSceneCameraPoint();
                }
                GUILayout.Space(20);
            }


            GUILayout.Space(40);


            DrawPlaybackArray();
            

            GUILayout.FlexibleSpace();

        }

        private void AddSceneCameraPoint() 
        {
            // Get Scene view camera
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                EditorUtility.DisplayDialog("Null Scene Camera", "Could not find scene view! Please open a scene view to grab from", "Ok");
                return;
            }
            Camera cam = sceneView.camera;

            VRBenchmarkPointData newPoint = new VRBenchmarkPointData()
            {
                position = cam.transform.position,
                rotation = cam.transform.rotation
            };

            Undo.RecordObject(playbackData, "Add Scene View to array");
            playbackData.playbackData.Add(newPoint);
            EditorUtility.SetDirty(playbackData);
        }

        private void DrawPlaybackArray()
        {
            SaveGizmoPositions();
            EditorGUI.BeginChangeCheck();

            Editor.CreateEditor(playbackData).OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
            {
                CreateBenchmarkGizmos();
            }
        }

        private void CreateNewBenchmark()
        {
            string path = EditorUtility.SaveFilePanel("Save New Benchmark", "Assets/", "BenchmarkData", "asset");

            if (string.IsNullOrEmpty(path)) 
            {
                return;
            }


            VRBenchmarkPlaybackData freshData = CreateInstance<VRBenchmarkPlaybackData>();

            // Convert absolute path to project-relative path
            string projectPath = "Assets" + path.Substring(Application.dataPath.Length);

            AssetDatabase.CreateAsset(freshData, projectPath);
            AssetDatabase.SaveAssets();

            newPlaybackData = freshData;
        }
        private void DestroyExistingGizmos() 
        {
            VRBenchmarkPointGizmo[] existingGizmos = Resources.FindObjectsOfTypeAll<VRBenchmarkPointGizmo>();

            foreach(VRBenchmarkPointGizmo gizmos in existingGizmos) 
            {
                DestroyImmediate(gizmos.gameObject);
            }
            DestroyImmediate(parentGizmo);
        }
        private void CreateBenchmarkGizmos() 
        {
            DestroyExistingGizmos();

            sceneGizmos = new List<VRBenchmarkPointGizmo>();
            parentGizmo = new GameObject("Benchmark Point Gizmos");

            for (int i = 0; i < playbackData.playbackData.Count; i++) 
            {
                CreateGizmosObject(playbackData.playbackData[i], i);
            }
        }

        private void CreateGizmosObject(VRBenchmarkPointData pointData, int arrayPos) 
        {
            GameObject newGizmos = new GameObject("PointGizmo");
            VRBenchmarkPointGizmo gizmos = newGizmos.AddComponent<VRBenchmarkPointGizmo>();
            gizmos.data = playbackData;
            gizmos.arrayNumber = arrayPos;
            gizmos.transform.parent = parentGizmo.transform;
            gizmos.transform.position = pointData.position;
            gizmos.transform.rotation = pointData.rotation;
            sceneGizmos.Add(gizmos);
            newGizmos.hideFlags = HideFlags.DontSaveInEditor; // Replace with hide and don't save
        }

        //Check validaity
        private bool CheckExistingGizmos() 
        {
            if (newPlaybackData != playbackData) 
            {
                return false;
            }
            else 
            {
                if (sceneGizmos.Count != playbackData.playbackData.Count) 
                {
                    return false;
                }
                else 
                {
                    foreach(VRBenchmarkPointGizmo gizmo in sceneGizmos) 
                    {
                        if (gizmo == null)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private void SaveGizmoPositions() 
        {
            foreach(var gizmo in sceneGizmos) 
            {
                playbackData.playbackData[gizmo.arrayNumber].position = gizmo.transform.position;
                playbackData.playbackData[gizmo.arrayNumber].rotation = gizmo.transform.rotation;
            }
        }

        private void OnDestroy()
        {
            SaveGizmoPositions();
            if (playbackData != null)
                newPlaybackData.playbackData = playbackData.playbackData;
            DestroyExistingGizmos();
        }
    }
}
#endif