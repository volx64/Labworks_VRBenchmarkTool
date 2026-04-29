using UnityEngine;
using UnityEngine.Events;
using Unity.Profiling;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text;

namespace VRBenchWorks
{
    public class VRBenchmarkPlayer : MonoBehaviour
    {
        public static string VERSION_NUMBER = "0.9.7";

        public static float warmup_seconds = 5;

        public float BenchmarkTime 
        {
            get 
            {
                return Time.realtimeSinceStartup - startTime;
            }
        }


        public VRBenchmarkPlaybackData playback;

        [Header("Use {scene} to replace at runtime with the current scene name")]
        [SerializeField] private string benchmarkName;

        [SerializeField] private bool forceRunBenchmark = true;

        [SerializeField] private GameObject playerObject;

        [SerializeField] private UnityEvent OnBenchmarkCompleted; // Ultevents are so much better but for others
        [SerializeField] private UnityEvent OnBenchmarkStart;
        [SerializeField] private UnityEvent OnNoBenchmark;

        private ProfilerRecorder mainProfiler;
        private ProfilerRecorder RAMProfiler;
        private ProfilerRecorder gpuProfiler;
        private ProfilerRecorder batchesRecorder;
        private ProfilerRecorder setPassRecorder;
        private ProfilerRecorder trianglesRecorder;
        private ProfilerRecorder verticesRecorder;

        private float startTime;

        private bool running = false;

        private VRBenchmarkPointData[] playbackGlobalTime;
        private int currentBenchmarkPoint = 0;

        //Saved Frame Times
        List<float> totalFrameTimesMs = new List<float>();
        List<float> cpuFrameTimesMs = new List<float>(); // calculated after benchmark
        List<float> gpuFrameTimesMs = new List<float>();
        List<float> ramSystemMB = new List<float>();

        //Rendering Data
        List<int> renderTotalBatches = new List<int>();
        List<int> renderMaterialSwap = new List<int>();
        List<int> renderTotalTriangles = new List<int>();
        List<int> renderTotalVertices = new List<int>();

        List<float> benchmarkTimeInFrame = new List<float>();

        public string GetBenchmarkName() 
        {
            string name = benchmarkName;

            if (name.Contains("{scene}")) 
            {
                StringBuilder builder = new StringBuilder(name);

                builder.Replace("{scene}", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                name = builder.ToString();
            }

            return name;
        }

        private void Awake()
        {
            if (!VRBenchmarkPlugin.CheckBenchmarkToggle() && !forceRunBenchmark)
            {
                this.enabled = false;
                playerObject.gameObject.SetActive(false);
                OnNoBenchmark?.Invoke();
                return;
            }

            ConvertPlaybackDataToGlobal();
        }

        private void ConvertPlaybackDataToGlobal()
        {
            playbackGlobalTime = new VRBenchmarkPointData[playback.playbackData.Count];

            float globalTime = 0;

            for (int i = 0; i < playbackGlobalTime.Length; i++)
            {
                VRBenchmarkPointData copy = new VRBenchmarkPointData(playback.playbackData[i]);

                globalTime += copy.time;
                copy.time = globalTime;

                playbackGlobalTime[i] = copy;
            }

        }


        private void Start()
        {
            mainProfiler = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            RAMProfiler = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            gpuProfiler = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time");

            batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            setPassRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");

            StartCoroutine(SceneWarmup());
        }

        private IEnumerator SceneWarmup()
        {
            yield return new WaitForSecondsRealtime(warmup_seconds);
            StartBenchmark();
        }

        private void StartBenchmark()
        {
            running = true;
            startTime = Time.realtimeSinceStartup;
            currentBenchmarkPoint = 0;
            OnBenchmarkStart.Invoke();
        }


        private void CaptureFrameTimes()
        {
            if (mainProfiler.Valid)
            {
                float mainMs = (float)(mainProfiler.LastValue / 1_000_000.0);
                totalFrameTimesMs.Add(mainMs);
            }

            if (RAMProfiler.Valid)
            {
                float RAM = RAMProfiler.LastValue / (1024 * 1024);
                ramSystemMB.Add(RAM);
            }

            if (gpuProfiler.Valid)
            {
                float gpuMs = (float)(gpuProfiler.LastValue / 1_000_000.0);
                gpuFrameTimesMs.Add(gpuMs);
            }

            renderTotalBatches.Add((int)batchesRecorder.LastValue);
            renderMaterialSwap.Add((int)setPassRecorder.LastValue);
            renderTotalTriangles.Add((int)trianglesRecorder.LastValue);
            renderTotalVertices.Add((int)verticesRecorder.LastValue);
        }

        private void Update()
        {
            if (!running)
                return;

            float benchmarkTime = Time.realtimeSinceStartup - startTime;

            benchmarkTimeInFrame.Add(benchmarkTime);
            CaptureFrameTimes();

            VRBenchmarkPointData currentPoint = playbackGlobalTime[currentBenchmarkPoint];
            VRBenchmarkPointData nextPoint = playbackGlobalTime[currentBenchmarkPoint + 1];

            float lerpToNextPoint = (benchmarkTime - currentPoint.time) / (nextPoint.time - currentPoint.time);

            playerObject.transform.position = Vector3.Lerp(currentPoint.position, nextPoint.position, lerpToNextPoint);
            playerObject.transform.rotation = Quaternion.Lerp(currentPoint.rotation, nextPoint.rotation, lerpToNextPoint);

            if (benchmarkTime > nextPoint.time)
            {
                currentBenchmarkPoint++;
                if (currentBenchmarkPoint >= playbackGlobalTime.Length - 1)
                {
                    running = false;
                    CompleteBenchmark();
                    return;
                }
            }
        }

        private void CompleteBenchmark()
        {
            if (gpuFrameTimesMs.Count == 0 || totalFrameTimesMs.Count == 0)
            {
                Debug.LogError("No CPU/GPU benchmark data recorded.");
                return;
            }

            CalculateCPUTime();

            string folder = Path.Combine(VRBenchmarkPlugin.CreateInstance().GetLogPath());

            Directory.CreateDirectory(folder);

            string benchmarkFileTime = DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm"); // Make sure same data has the same timings

            WriteCSVData(benchmarkFileTime); // WRITE CSV DATA FIRST BEFORE IT GETS SORTED!! 

            BenchmarkReport mainSum = RecordFrameTimes(totalFrameTimesMs, "Total");

            BenchmarkReport CPUSum = RecordFrameTimes(cpuFrameTimesMs, "Not GPU");

            BenchmarkReport GPUSum = RecordFrameTimes(gpuFrameTimesMs, "GPU");

            WriteJsonData(new BenchmarkReport[3] {mainSum, CPUSum, GPUSum }, benchmarkFileTime);

            OnBenchmarkCompleted?.Invoke();

            Debug.Log($"Benchmark - {GetBenchmarkName()} Complete! Saved to {folder}");

            this.enabled = false;
        }

        private void CalculateCPUTime() 
        {
            for (int i = 0; i < totalFrameTimesMs.Count; i++) 
            {
                if (gpuFrameTimesMs.Count < i)
                    return;
                cpuFrameTimesMs.Add(totalFrameTimesMs[i] - gpuFrameTimesMs[i]);
            }
        }

        private void WriteCSVData(string benchmarkFileTime) 
        {
            string path = Path.Combine(VRBenchmarkPlugin.CreateInstance().GetLogPath(), benchmarkFileTime + " " + GetBenchmarkName() + "_raw.csv");

            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine($"Time,Frame,Total_ms,GPU_ms,RAM,Batches,Materials,Triangles,Vertices,Version:{VERSION_NUMBER}");

                for (int i = 0; i < gpuFrameTimesMs.Count; i++)
                {
                    writer.WriteLine($"{benchmarkTimeInFrame[i].ToString("F3")},{i},{totalFrameTimesMs[i].ToString("F3")},{gpuFrameTimesMs[i].ToString("F3")},{ramSystemMB[i].ToString()}," +
                        $"{renderTotalBatches[i].ToString()},{renderMaterialSwap[i].ToString()},{renderTotalTriangles[i].ToString()},{renderTotalVertices[i].ToString()}"); ;
                }
            }
        }

        private void WriteJsonData(BenchmarkReport[] reports, string benchmarkFileTime)  
        {
            string path = Path.Combine(VRBenchmarkPlugin.CreateInstance().GetLogPath(), benchmarkFileTime + " " + GetBenchmarkName() + ".json");

            string jsonData = "";

            foreach(BenchmarkReport report in reports)
            {
                jsonData += JsonUtility.ToJson(report, true);
                jsonData += "\n";
            }
            File.WriteAllText(path, jsonData);
        }

        private BenchmarkReport RecordFrameTimes(List<float> frameTimes, string statType) 
        {
            frameTimes.Sort();

            float avgMs = frameTimes.Average();
            float p99Ms = CalculatePercentile(frameTimes, 0.99f);
            float p999Ms = CalculatePercentile(frameTimes, 0.999f);

            float avgFPS = MilisToFPS(avgMs);
            float low1FPS = MilisToFPS(p99Ms);
            float low01FPS = MilisToFPS(p999Ms);

            float stdDev = CalculateStandardDeviation(frameTimes, avgMs);

            BenchmarkReport summary = new BenchmarkReport
            {
                benchmark = GetBenchmarkName(),
                statisticType = statType,
                duration_seconds = playbackGlobalTime[playbackGlobalTime.Length - 1].time,
                average_fps = avgFPS,
                one_percent_low_fps = low1FPS,
                point_one_percent_low_fps = low01FPS,
                average_ms = avgMs,
                stddev = stdDev,
                frame_count = gpuFrameTimesMs.Count,
                timestamp = DateTime.Now.ToString("o")
            };
            return summary;
        }

        float CalculatePercentile(List<float> data, float percentile)
        {
            int index = Mathf.Clamp(
                Mathf.FloorToInt(data.Count * percentile),
                0,
                data.Count - 1
            );
            return data[index];
        }

        float CalculateStandardDeviation(List<float> data, float mean)
        {
            float variance = data
                .Select(v => Mathf.Pow(v - mean, 2))
                .Average();

            return Mathf.Sqrt(variance);
        }

        float MilisToFPS(float ms) => 1000f / ms;


        private void OnDisable()
        {
            if (mainProfiler.Valid)
                mainProfiler.Dispose();
            if (RAMProfiler.Valid)
                RAMProfiler.Dispose();
            if (gpuProfiler.Valid)
                gpuProfiler.Dispose();
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (playback == null)
                return;
            for(int i = 0; i < playback.playbackData.Count; i++) 
            {
                Gizmos.color = Color.grey;
                DrawSingleGizmo(playback.playbackData[i].position, playback.playbackData[i].rotation, i);
            }

        }

        private void DrawSingleGizmo(Vector3 position, Quaternion rotation, int arrayNumber) 
        {
            Gizmos.DrawIcon(position, "BaseVRBenchmarkPointGizmo", true);

            // Get Scene view camera
            UnityEditor.SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null) return;
            Camera cam = sceneView.camera;

            // Distance from camera to icon
            float distance = Vector3.Distance(cam.transform.position, position);

            // Scale font based on distance
            int fontSize = Mathf.Max(1, Mathf.RoundToInt(VRBenchmarkPointGizmo.startFont / distance * VRBenchmarkPointGizmo.visualScaling));

            Vector3 viewportPos = cam.WorldToViewportPoint(position);

            // Only render if in front of the camera and within screen bounds
            if (viewportPos.z > 0 && viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1)
            {
                // Compute GUI point
                Vector3 guiPoint = UnityEditor.HandleUtility.WorldToGUIPoint(position);

                GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = fontSize,
                    normal = { textColor = Color.grey }
                };

                // Apply offset scaled by distance
                float scaledOffset = VRBenchmarkPointGizmo.textOffset / distance * VRBenchmarkPointGizmo.visualScaling;
                guiPoint += new Vector3(0, scaledOffset, 0);

                UnityEditor.Handles.BeginGUI();
                GUI.Label(new Rect(guiPoint.x - 32, guiPoint.y, 64, 64), $"[{arrayNumber}]", style);
                UnityEditor.Handles.EndGUI();
            }

            //Draw Arrow
            DrawArrow.ForGizmo(position, rotation * Vector3.forward);
        }
#endif


        [System.Serializable]
        class BenchmarkReport
        {
            public string benchmark;
            public string statisticType;
            public float duration_seconds;
            public float average_fps;
            public float one_percent_low_fps;
            public float point_one_percent_low_fps;
            public float average_ms;
            public float stddev;
            public int frame_count;
            public string timestamp;
        }

        struct ReportRenderingData
        {
            public int batches;
            public int drawCalls;
            public int triangles;
            public int vertices;
        }
    }
}