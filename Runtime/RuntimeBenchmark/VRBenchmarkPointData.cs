using UnityEngine;


namespace VRBenchWorks
{
    [System.Serializable]
    public class VRBenchmarkPointData
    {
        public float time;

        [HideInInspector] public Vector3 position;
        [HideInInspector] public Quaternion rotation;

        public VRBenchmarkPointData() 
        {
            
        }

        public VRBenchmarkPointData(VRBenchmarkPointData old)
        {
            time = old.time;
            position = old.position;
            rotation = old.rotation;
        }
    }
}
