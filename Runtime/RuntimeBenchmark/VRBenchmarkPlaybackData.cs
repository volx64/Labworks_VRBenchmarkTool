using UnityEngine;
using System.Collections.Generic;


namespace VRBenchWorks
{

    [CreateAssetMenu(fileName = "BenchmarkAsset", menuName = "Labworks/Benchmark")]
    public class VRBenchmarkPlaybackData : ScriptableObject
    {
        public List<VRBenchmarkPointData> playbackData = new List<VRBenchmarkPointData>();


        private float internalTotal = -1;
        public float TotalTime 
        {
            get 
            {
                if (internalTotal == -1)
                {
                    internalTotal = 0;

                    foreach (VRBenchmarkPointData pointData in playbackData)
                    {
                        internalTotal += pointData.time;
                    }
                }
                return internalTotal;
            }
        }
    }
}