using System;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
#if UNITY_ANDROID
using UnityEngine.XR.OpenXR.Features.Meta;
#endif

namespace VRBenchWorks
{
    public class VRBenchmarkPlugin
    {
        private static VRBenchmarkPlugin instance;

        public static string RunID;

        public bool doBenchmark = true;

        public static bool CheckBenchmarkToggle() 
        {
            if (instance == null) 
            {
                return false;
            }
            else 
            {
                return instance.doBenchmark;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() 
        {
            RunID = ParseTimeToFileName();
#if UNITY_ANDROID
            SetRefreshRate();
#endif
        }

        #if UNITY_ANDROID
        private static void SetRefreshRate() 
        {
            if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null || XRGeneralSettings.Instance.Manager.activeLoader == null)
                return;
            XRDisplaySubsystem displaySubsystem = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();

            bool success = displaySubsystem.TryRequestDisplayRefreshRate(120);

            if (!success) 
            {
                displaySubsystem.TryRequestDisplayRefreshRate(90);
            }
        }
        #endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() 
        {
            RunID = null;
        }


        public static string ParseTimeToFileName()
        {
            return DateTime.Now.ToString("M") + " " + DateTime.Now.ToString("hh") + DateTime.Now.ToString("mm");
        }


        /// <summary>
        /// Create benchmark instance if not already added to the project.
        /// </summary>
        public static VRBenchmarkPlugin CreateInstance() 
        {
            if (instance == null)
            {
                instance = new VRBenchmarkPlugin();
                return instance;    
            }
            else
                return instance;
        }

        public static void EnableBenchmarks() 
        {
            CreateInstance().doBenchmark = true;
        }

        public static void DisableBenchmarks()
        {
            CreateInstance().doBenchmark = false;
        }

        public string GetLogPath() 
        {
            return Path.Combine(Application.persistentDataPath, RunID);
        }

    }
}
