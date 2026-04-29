#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace VRBenchWorks
{
    [InitializeOnLoad]
    public static class GizmoInstaller
    {
        public static string PackagePath = "Packages/com.LabworksGames.VRBenchmark/Gizmos";
        public static string DestinationPath = "Assets/Gizmos";

        //Draw gizmos ONLY works with assets/gizmos :(
        static GizmoInstaller() 
        {
            Install();
        }

        public static void Install()
        {
            if (!Directory.Exists(PackagePath))
            {
                Debug.LogWarning("Gizmo was not installed! Package path does not exist");
                return;
            }

            if (!Directory.Exists(DestinationPath))
                Directory.CreateDirectory(DestinationPath);


            foreach(string file in Directory.GetFiles(PackagePath)) 
            {
                string fileName = Path.GetFileName(file);
                string newPath = Path.Combine(DestinationPath, fileName);

                if (!File.Exists(newPath)) 
                {
                    File.Copy(file, newPath);
                }
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif