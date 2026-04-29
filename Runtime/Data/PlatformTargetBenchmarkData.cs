using UnityEngine;

namespace VRBenchWorks
{
    public enum FramePointType
    {
        TotalMS,
        GPUMS,
        RAMLeft,
        Batches,
        Materials,
        Triangles,
        Vertices
    }


    [CreateAssetMenu(menuName = "Labworks/BenchmarkTargetDevice")]
    public class PlatformTargetBenchmarkData : ScriptableObject
    {
        [Header("FPS Timings")]
        public FPSLineColours MainMS;

        public FPSLineColours BatchCounts;
        public FPSLineColours MaterialCounts;
        public FPSLineColours TriangleCounts;
        public FPSLineColours VerticeCounts;

        public float MaximumRAM = 4096;

        public float GetMaxValue(FramePointType pointType, int severity) 
        {
            switch (pointType) 
            {
                case FramePointType.TotalMS:
                case FramePointType.GPUMS:
                    return MainMS.GetValue(severity);
                case FramePointType.Batches:
                    return BatchCounts.GetValue(severity);
                case FramePointType.Materials:
                    return MaterialCounts.GetValue(severity);
                case FramePointType.RAMLeft:
                    return MaximumRAM;
                case FramePointType.Triangles:
                    return TriangleCounts.GetValue(severity);
                case FramePointType.Vertices:
                    return VerticeCounts.GetValue(severity);
            }

            return Mathf.Infinity;
        }

        public PlatformTargetBenchmarkData() 
        {
            MainMS = new FPSLineColours()
            {
                Max = 40,
                Red = 33.33333f,
                Orange = 22.22222f,
                Yellow = 16.666667f,
                Green = 11.11111f
            };
        }
    }

    [System.Serializable]
    public struct FPSLineColours 
    {
        public float Max;
        public float Red;
        public float Orange;
        public float Yellow;
        public float Green;

        public float GetValue(int severity) 
        {
            switch (severity) 
            {
                case 0:
                    return Green;
                case 1:
                    return Yellow;
                case 2:
                    return Orange;
                case 3:
                    return Red;
                default:
                    return Max;
            }
        }
    }
}