#if UNITY_EDITOR
using UnityEngine;
using System.IO;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VRBenchWorks
{

    public struct FramePointData 
    {
        //Time,Frame,Total_ms,GPU_ms,RAM,Version:{VERSION_NUMBER}
        public float Time;
        public float Frame;
        public float Total_MS;
        public float GPU_ms;
        public float RAMLeft;
        public float batches;
        public float materialSwaps;
        public float triangles;
        public float vertices;

        public float GetValueFromType(FramePointType type) 
        {
            switch (type)
            {
                case FramePointType.TotalMS:
                    return Total_MS;
                case FramePointType.GPUMS:
                    return GPU_ms;
                case FramePointType.Batches:
                    return batches;
                case FramePointType.Materials:
                    return materialSwaps;
                case FramePointType.RAMLeft:
                    return RAMLeft;
                case FramePointType.Triangles:
                    return triangles;
                case FramePointType.Vertices:
                    return vertices;
            }
            return 0;
        }
    }

    public class VRBenchmarkAnalysisWindow : EditorWindow
    {
        QuickEditor.Graphic.PlottingGraph plottingGraph;
        FramePointData[] _loadedCSVData;
        float graphStep = 0.2f;
        FramePointType graphPointType = FramePointType.TotalMS;
        PlatformTargetBenchmarkData targetDeviceValues;
        string mouseHoverPointString = "Waiting for Hoover over a point to check data!";
        Dictionary<FramePointType, MessageType> dataWarnings = new Dictionary<FramePointType, MessageType>();

        // Add menu item
        [MenuItem("Labworks/VR Benchmark Analysis")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow<VRBenchmarkAnalysisWindow>("VR Benchmark Analysis");
        }


        public static void ShowWindow(string filePath)
        {
            VRBenchmarkAnalysisWindow localWindow = EditorWindow.GetWindow<VRBenchmarkAnalysisWindow>("VR Benchmark Analysis");

            localWindow.LoadCSVFile(filePath);
        }

        private void Awake()
        {
            if (EditorPrefs.HasKey("Benchmark-TargetDevice")) 
            {
                string targetDevicePath = EditorPrefs.GetString("Benchmark-TargetDevice");
                if (AssetDatabase.AssetPathExists(targetDevicePath)) 
                {
                    targetDeviceValues = AssetDatabase.LoadAssetAtPath<PlatformTargetBenchmarkData>(targetDevicePath);
                }
            }
        }

        private void OnDestroy()
        {
            if (targetDeviceValues != null) 
            {
                EditorPrefs.SetString("Benchmark-TargetDevice", AssetDatabase.GetAssetPath(targetDeviceValues));
            }
        }


        void OnGUI()
        {
            GUILayout.Space(20);

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
                GUILayout.Label($"Benchmark Analysis", headerStyle);
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(20);


            using (var l = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                targetDeviceValues = (PlatformTargetBenchmarkData)EditorGUILayout.ObjectField(targetDeviceValues, typeof(PlatformTargetBenchmarkData), false);
                GUILayout.Space(20);
            }

            GUILayout.Space(20);

            using (var l = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                if (GUILayout.Button("Load CSV Data", GUILayout.Height(50)))
                {
                    string path = EditorUtility.OpenFilePanel("Open .CSV benchmark file", "", "csv");
                    if (path.Length != 0)
                    {
                        LoadCSVFile(path);
                    }
                }
                GUILayout.Space(20);
            }

            if (_loadedCSVData == null)
            {
                GUILayout.FlexibleSpace();
                using (var l = new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUIStyle headerStyle = new GUIStyle()
                    {
                        fontSize = 22,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.UpperCenter,
                        normal = new GUIStyleState()
                        {
                            textColor = Color.lightGoldenRod
                        }
                    };
                    GUILayout.Label($"Load CSV Data to render graphs".ToUpper(), headerStyle);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
            }
            else
            {
                DrawWarnings();

                DrawGraphButtons();

                Vector2[] graphData = SelectGraphData();

                DrawGraph(graphData);

                DrawPointHover();

                MouseClickOnGraph(graphData);
            }
        }

        private void DrawWarnings() 
        {

            foreach (FramePointType warning in dataWarnings.Keys)
            {
                if (dataWarnings[warning] == MessageType.None)
                    continue;
                string warningMsg;
                switch (warning)
                {
                    case (FramePointType.Batches):
                        warningMsg = "Batch Count has exceeded target device thresholds\n" +
                            "Suggestion: Reduce CPU usage by combining meshes through static batching or GPU instancing";
                        break;
                    case (FramePointType.Materials):
                        warningMsg = "Material Count has exceeded target device thresholds\n" +
    "                   Suggestion: Reduce unique material count or simplify material shader";
                        break;
                    case (FramePointType.Triangles):
                        warningMsg = "Triangle Count has exceeded target device thresholds\n" +
"                   Suggestion: Reduce geometric complexity of environment with Level Of Detail (LOD)";
                        break;
                    case (FramePointType.Vertices):
                        warningMsg = "Vertice Count has exceeded target device thresholds\n" +
"                   Suggestion: Reduce geometric complexity of environment with Level Of Detail (LOD)";
                        break;
                    default:
                        warningMsg = "Undefined Warning";
                        break;
                }

                GUILayout.Space(5);

                //Draw blanks to stop repaint errors 
                using (var x = new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(20);

                    EditorGUILayout.HelpBox(warningMsg, dataWarnings[warning]);

                    GUILayout.Space(20);
                }
            }
        }

        private void CheckDataWarnings() 
        {
            dataWarnings.Clear();
             /*
              * Check to see if any metrics exceeds target device thresholds
                 * RAMLeft,
                 * Batches,
                 * Materials,
                 * Triangles,
                 * Vertices
            */
            foreach(FramePointData point in _loadedCSVData) 
            {
                CheckPointForWarnings(point, FramePointType.RAMLeft);
                CheckPointForWarnings(point, FramePointType.Batches);
                CheckPointForWarnings(point, FramePointType.Materials);
                CheckPointForWarnings(point, FramePointType.Triangles);
                CheckPointForWarnings(point, FramePointType.Vertices);
            }
        }

        private void CheckPointForWarnings(FramePointData point, FramePointType type) 
        {
            float yellowVal = targetDeviceValues.GetMaxValue(type, 1);
            float OrangeVal = targetDeviceValues.GetMaxValue(type, 2);
            float RedVal = targetDeviceValues.GetMaxValue(type, 3);

            if (RedVal >= 0 && point.GetValueFromType(type) > RedVal)
            {
                if (dataWarnings.ContainsKey(type))
                    dataWarnings[type] = MessageType.Error;
                else
                    dataWarnings.Add(type, MessageType.Error); // Add error to warning list
            }
            else if (OrangeVal >= 0 && point.GetValueFromType(type) > OrangeVal)
            {
                if (dataWarnings.ContainsKey(type))
                {
                    if (dataWarnings[type] != MessageType.Error) // Only overwrite if not error
                        dataWarnings[type] = MessageType.Warning;
                }
                else
                    dataWarnings.Add(type, MessageType.Warning); // Add warning to warning list
            }
            else if (yellowVal >= 0 && point.GetValueFromType(type) > yellowVal) 
            {
                if (dataWarnings.ContainsKey(type)) 
                {
                    if (dataWarnings[type] == MessageType.None)
                        dataWarnings[type] = MessageType.Info;
                }
                else
                    dataWarnings.Add(type, MessageType.Info); // Add Log to warning list
            }
        }

        private void DrawPointHover()
        {
            Vector2 mousePoint = plottingGraph.GetMousePointHover();

            FramePointData currentHoverPoint = new FramePointData();

            if (mousePoint != new Vector2(0, 0))
            {
                currentHoverPoint = _loadedCSVData.FirstOrDefault(x => x.Time >= mousePoint.x);
                if (currentHoverPoint.Total_MS != 0)
                {
                    mouseHoverPointString = $" ======= SELECTED POINT DATA ======= \n" +
                        $"Point {currentHoverPoint.Frame}\n" +
                  $"Total MS {currentHoverPoint.Total_MS}\n" +
                  $"RAM {currentHoverPoint.RAMLeft}\n" +
                  "\n" +
                  " === RENDERING INFO ===\n" +
                  $"Batches {currentHoverPoint.batches}\n" +
                  $"Material Calls {currentHoverPoint.materialSwaps}\n" +
                  $"Triangles {currentHoverPoint.triangles}\n" +
                  $"Vertices {currentHoverPoint.vertices}";
                }
            }

            GUILayout.Space(20);

            //Draw blanks to stop repaint errors 
            using (var x = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                GUILayout.FlexibleSpace();

                GUILayout.Box(mouseHoverPointString, GUILayout.Width(400), GUILayout.Height(200));

                GUILayout.FlexibleSpace();
                GUILayout.Space(20);
            }
        }


        private void MouseClickOnGraph(Vector2[] points)
        {
            if (Event.current.type == EventType.MouseDown) 
            {
                Vector2 mousePoint = plottingGraph.GetMousePointHover();

                if (mousePoint != new Vector2(0, 0)) 
                {
                    VRBenchmarkPlaybackData playbackData = FindFirstObjectByType<VRBenchmarkPlayer>().playback;
                    VRBenchmarkEditorWindow.JumpEditorCamToFrameTime(mousePoint.x, playbackData);
                }
            }
        }        

        private void DrawGraphButtons() 
        {
            GUILayout.Space(20);
            using (var l = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                //Draw type buttons

                CreateGraphTypeButton("Total_MS", FramePointType.TotalMS);

                if (_loadedCSVData[0].GPU_ms != 0)
                {
                    CreateGraphTypeButton("GPU_MS", FramePointType.GPUMS);
                }

                CreateGraphTypeButton("RAM", FramePointType.RAMLeft);

                GUILayout.Space(20);

                CreateGraphTypeButton("Batches", FramePointType.Batches);

                CreateGraphTypeButton("Material passes", FramePointType.Materials);

                CreateGraphTypeButton("Triangles", FramePointType.Triangles);

                CreateGraphTypeButton("Vertices", FramePointType.Vertices);


                GUILayout.Space(10);

                //Draw Float input
                graphStep = EditorGUILayout.FloatField("Graph Step:", graphStep);
                if (graphStep < 0.05f)
                {
                    graphStep = 0.05f;
                }


                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(10);
        }

        private void CreateGraphTypeButton(string buttonTitle, FramePointType pointType) 
        {
            if (GUILayout.Button(buttonTitle, GUILayout.Height(50), GUILayout.Width(100)))
            {
                graphPointType = pointType;
            }
        }

        private Vector2[] SelectGraphData() 
        {
            float totalTime = _loadedCSVData[_loadedCSVData.Length - 1].Time;
            int displayedFrames = Mathf.FloorToInt(totalTime / graphStep);

            List<Vector2> graphPoints = new List<Vector2>();
            graphPoints.Capacity = displayedFrames;

            for(int i = 0; i < displayedFrames; i++) 
            {
                float currentTime = i * graphStep;

                List<FramePointData> pointsThisStep = _loadedCSVData.Where(x => x.Time > currentTime && x.Time < currentTime + graphStep).ToList();

                if (pointsThisStep.Count == 0)
                {
                    continue;
                }

                Vector2 newGraphPoint = new Vector2();
                newGraphPoint.x = currentTime;

                switch (graphPointType) 
                {
                    case (FramePointType.TotalMS):
                        newGraphPoint.y = pointsThisStep.Average(x => x.Total_MS);
                        break;
                    case (FramePointType.GPUMS):
                        newGraphPoint.y = pointsThisStep.Average(x => x.GPU_ms);
                        break;
                    case (FramePointType.RAMLeft):
                        newGraphPoint.y = pointsThisStep.Average(x => x.RAMLeft);
                        break;
                    case (FramePointType.Batches):
                        newGraphPoint.y = pointsThisStep.Average(x => x.batches);
                        break;
                    case (FramePointType.Materials):
                        newGraphPoint.y = pointsThisStep.Average(x => x.materialSwaps);
                        break;
                    case (FramePointType.Triangles):
                        newGraphPoint.y = pointsThisStep.Average(x => x.triangles);
                        break;
                    case (FramePointType.Vertices):
                        newGraphPoint.y = pointsThisStep.Average(x => x.vertices);
                        break;
                }

                graphPoints.Add(newGraphPoint);

            }

            return graphPoints.ToArray();
        }

        private Vector2[] SelectAllGraphPoints() 
        {
            Vector2[] graphPoints = new Vector2[_loadedCSVData.Length];

            for (int i = 0; i < _loadedCSVData.Length; i++)
            {
                graphPoints[i].x = _loadedCSVData[i].Time;

                switch (graphPointType)
                {
                    case (FramePointType.TotalMS):
                        graphPoints[i].y = _loadedCSVData[i].Total_MS;
                        break;
                    case (FramePointType.GPUMS):
                        graphPoints[i].y = _loadedCSVData[i].GPU_ms;
                        break;
                    case (FramePointType.RAMLeft):
                        graphPoints[i].y = _loadedCSVData[i].RAMLeft;
                        break;
                }
            }

            return graphPoints;
        }

        private void DrawGraph(Vector2[] graphData) 
        {
            string graphTitle = "";
            switch (graphPointType)
            {
                case (FramePointType.TotalMS):
                    graphTitle = "TOTAL_MS";
                    break;
                case (FramePointType.GPUMS):
                    graphTitle = "GPU_MS";
                    break;
                case (FramePointType.RAMLeft):
                    graphTitle = "RAM USAGE";
                    break;
                case (FramePointType.Batches):
                    graphTitle = "BATCHES";
                    break;
                case (FramePointType.Materials):
                    graphTitle = "MATERIAL PASSES";
                    break;
                case (FramePointType.Triangles):
                    graphTitle = "TRIANGLES";
                    break;
                case (FramePointType.Vertices):
                    graphTitle = "VERTICES";
                    break;
            }


            if (plottingGraph == null || plottingGraph.m_graphName != graphTitle)
            {
                plottingGraph = new QuickEditor.Graphic.PlottingGraph(graphTitle, graphData);
                plottingGraph.GraphPadding = 60;
            }
            else
            {
                plottingGraph.UpdateGraphData(graphData);
                plottingGraph.mouseSelectionWidth = 10 * (graphStep / 0.2f);
            }

            switch (graphPointType)
            {
                case (FramePointType.TotalMS):
                case (FramePointType.GPUMS):
                    plottingGraph.DrawLineGraph(0, targetDeviceValues.MainMS.Max, targetDeviceValues.MainMS);
                    break;
                case (FramePointType.RAMLeft):
                    plottingGraph.DrawLineGraph(0, targetDeviceValues.MaximumRAM);
                    break;
                case (FramePointType.Batches):
                    plottingGraph.DrawLineGraph(targetDeviceValues.BatchCounts);
                    break;
                case (FramePointType.Materials):
                    plottingGraph.DrawLineGraph(targetDeviceValues.MaterialCounts);
                    break;
                case (FramePointType.Triangles):
                    plottingGraph.DrawLineGraph(targetDeviceValues.TriangleCounts);
                    break;
                case (FramePointType.Vertices):
                    plottingGraph.DrawLineGraph(targetDeviceValues.VerticeCounts);
                    break;
            }

            Repaint();
        }

        private void LoadCSVFile(string path) 
        {
            if (!File.Exists(path)) 
            {
                Debug.LogError("File at path no longer exists!");
                return;
            }
            using (StreamReader reader = new StreamReader(path)) 
            {
                string HeaderData = reader.ReadLine();

                string fileVersion = HeaderData.Substring(HeaderData.IndexOf("Version:"));

                if (fileVersion != $"Version:{VRBenchmarkPlayer.VERSION_NUMBER}") 
                {
                    Debug.LogError($"File {Path.GetFileName(path)}, verison number is incompatible.\n" +
                        $"File is {fileVersion} expected Version:{VRBenchmarkPlayer.VERSION_NUMBER}");
                }

                //Time,Frame,Total_ms,GPU_ms,RAM,Version:{VERSION_NUMBER}

                List<FramePointData> frames = new List<FramePointData>();

                while (reader.Peek() >= 0) // For each line 
                {
                    string[] currentLine = reader.ReadLine().Split(',');

                    FramePointData framePoint = new FramePointData()
                    {
                        Time = float.Parse(currentLine[0]),
                        Frame = float.Parse(currentLine[1]),
                        Total_MS = float.Parse(currentLine[2]),
                        GPU_ms = float.Parse(currentLine[3]),
                        RAMLeft = float.Parse(currentLine[4]),
                        batches = int.Parse(currentLine[5]),
                        materialSwaps = int.Parse(currentLine[6]),
                        triangles = int.Parse(currentLine[7]),
                        vertices = int.Parse(currentLine[8])
                    };

                    frames.Add(framePoint);
                }

                _loadedCSVData = frames.ToArray();
                Debug.Log($"Loaded: {Path.GetFileName(path)}, Version:{fileVersion}");
                CheckDataWarnings();
            }
        }
    }
}
#endif