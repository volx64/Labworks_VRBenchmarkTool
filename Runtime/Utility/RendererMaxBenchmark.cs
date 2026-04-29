using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRBenchWorks
{

    public class RendererMaxBenchmark : MonoBehaviour
    {
        public enum RenderMode
        {
            Batches,
            SetPass,
            Triangle,
            Vertice
        }
        [SerializeField] private VRBenchmarkPlayer player;

        [SerializeField] private RenderMode renderMode;

        [Header("Render Load Parameters")]
        [SerializeField] private int targetEndMaxObjects = 100000;
        [SerializeField] private float waitTime = 1;
        [SerializeField] private float spawnTime = .5f;
        [SerializeField] private MeshFilter baseReference;
        [SerializeField] private bool ReplaceMeshWithSingleTriangle = false;
        [SerializeField] private Transform point1, point2;

        [Header("SetPass Params")]
        [SerializeField] private Transform meshRendererParent;


        private int targetObjectsPerStep;
        private float totalTime;
        private float currentBenchmarkTime;

        private int currentObjectCount = 0;

        //SetPass Test
        private MeshRenderer[] meshRenderers;


        //Vertice Test
        private List<Vector3> vertices;
        private List<int> indicies;
        private List<int> tris;


        private void Start()
        {
            this.enabled = false;

            if (ReplaceMeshWithSingleTriangle)
            {
                CreateSingleTriangle();
            }

            //Start for each render mode
            switch (renderMode)
            {
                case RenderMode.Batches:
                    break;
                case RenderMode.SetPass:
                    meshRenderers = meshRendererParent.GetComponentsInChildren<MeshRenderer>();
                    GraphicsSettings.useScriptableRenderPipelineBatching = false;
                    break;
                case RenderMode.Triangle:
                    baseReference.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // allow over 65K vertices
                    tris = new List<int>();
                    break;
                case RenderMode.Vertice:
                    baseReference.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // allow over 65K vertices
                    indicies = new List<int>(baseReference.mesh.GetIndices(0));
                    vertices = new List<Vector3>(baseReference.mesh.vertices);
                    break;
            }
        }

        [ContextMenu("Spawn SetPass Children")]
        private void SpawnSetPassChildren() 
        {
            Transform[] children = meshRendererParent.GetComponentsInChildren<Transform>();

            foreach (Transform child in children) 
            {
                if (child != meshRendererParent)
                    DestroyImmediate(child.gameObject);
            }

            for (int i =0; i < targetEndMaxObjects; i++) 
            {

                Vector3 randomPosition = new Vector3()
                {
                    x = Random.Range(point1.position.x, point2.position.x),
                    y = Random.Range(point1.position.y, point2.position.y),
                    z = Random.Range(point1.position.z, point2.position.z)
                };

                Instantiate(baseReference, meshRendererParent).transform.position = randomPosition;
            }
        }

        private void SetupTriangleMesh()
        {
            Mesh mesh = new Mesh();

            List<Vector3> vertices = new List<Vector3>();

            const int verticeAmount = 4;

            for(int x = -verticeAmount; x < verticeAmount; x++) 
            {
                for (int y = -verticeAmount; y < verticeAmount; y++)
                {
                    for (int z = -verticeAmount; z < verticeAmount; z++)
                    {
                        vertices.Add(new Vector3(x, y, z));
                    }
                }
            }


            mesh.vertices = vertices.ToArray();

            baseReference.mesh = mesh;
        }

        public void OnStartBenchmark()
        {
            this.enabled = true;
            totalTime = player.playback.TotalTime;

            int totalSteps = Mathf.CeilToInt(totalTime / (waitTime + spawnTime));
            targetObjectsPerStep = targetEndMaxObjects / totalSteps;
        }

        private void CreateSingleTriangle() 
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[3]
            {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0)
            };
            mesh.vertices = vertices;

            int[] tris = new int[3]
            {
            // lower left triangle
            0, 2, 1
            };
            mesh.triangles = tris;

            Vector3[] normals = new Vector3[3]
            {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[3]
            {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1)
            };
            mesh.uv = uv;

            baseReference.mesh = mesh;
        }


        private void Update()
        {
            currentBenchmarkTime = player.BenchmarkTime;

            if (currentBenchmarkTime > totalTime) 
            {
                this.enabled = false;
                return;
            }

            CalculateTargetParameter();

        }

        private bool IsWaitTime() 
        {
            float clampedTime = currentBenchmarkTime % (waitTime + spawnTime);

            return clampedTime > spawnTime;
        }


        private void CalculateTargetParameter() 
        {
            if (IsWaitTime())
                return;
            else 
            {
                float clampedTime = currentBenchmarkTime % (waitTime + spawnTime);

                int currentStep = Mathf.CeilToInt(currentBenchmarkTime / (waitTime + spawnTime));


                int targetObjectsThisFrame = Mathf.CeilToInt(Mathf.Lerp(targetObjectsPerStep * Mathf.Clamp(currentStep - 1, 0, Mathf.Infinity), 
                    targetObjectsPerStep * currentStep, 
                    clampedTime / spawnTime));



                switch (renderMode)
                {
                    case RenderMode.Batches:
                        UpdateBatchToTarget(targetObjectsThisFrame);
                        break;
                    case RenderMode.Vertice:
                        UpdateVerticeToTarget(targetObjectsPerStep * currentStep);
                        break;
                    case RenderMode.SetPass:
                        UpdateSetPassToTarget(targetObjectsThisFrame);
                        break;
                    case RenderMode.Triangle:
                        UpdateTriangleToTarget(targetObjectsPerStep * currentStep);
                        break;
                    default:
                        Debug.LogError("UNIMPLEMENTED RENDER TEST");
                        break;
                }
            }
        }

        private void UpdateBatchToTarget(int targetNumber) 
        {
            int spawnNumber = targetNumber - currentObjectCount;

            if (spawnNumber <= 0)
                return;

            for (int i = 0; i < spawnNumber; i++) 
            {
                GameObject newObject = Instantiate(baseReference.gameObject);

                Vector3 randomPosition = new Vector3()
                {
                    x = Random.Range(point1.position.x, point2.position.x),
                    y = Random.Range(point1.position.y, point2.position.y),
                    z = Random.Range(point1.position.z, point2.position.z)
                };

                newObject.transform.position = randomPosition;
                newObject.transform.rotation = Random.rotation;
                currentObjectCount++;
            }
        }

        private void UpdateSetPassToTarget(int targetNumber) 
        {
            Material material = baseReference.GetComponent<MeshRenderer>().sharedMaterial;

            for (int i = 0; i < targetNumber; i++) 
            {
                if (meshRenderers[i].material.color == material.color) 
                {
                    meshRenderers[i].material.renderQueue = i;
                    meshRenderers[i].material.color = Random.ColorHSV();
                }

            }
        }

        private void UpdateTriangleToTarget(int targetNumber) 
        {
            int spawnNumber = targetNumber - currentObjectCount;

            if (spawnNumber <= 0)
                return;            

            for (int i = 0; i < spawnNumber; i++)
            {
                tris.Add(0);
                tris.Add(2);
                tris.Add(1);
                currentObjectCount++;
            }

            baseReference.mesh.triangles = tris.ToArray();
            baseReference.mesh.RecalculateBounds();
        }

        private void UpdateVerticeToTarget(int targetNumber) 
        {
            int spawnNumber = targetNumber - currentObjectCount;

            if (spawnNumber <= 0)
                return;

            for (int i = 0; i < spawnNumber; i++)
            {
                
                Vector3 randomPosition = new Vector3()
                {
                    x = Random.Range(point1.position.x, point2.position.x),
                    y = Random.Range(point1.position.y, point2.position.y),
                    z = Random.Range(point1.position.z, point2.position.z)
                };
                vertices.Add(randomPosition);
                indicies.Add(vertices.Count - 1);
                
                currentObjectCount++;
            }

            // assign the local vertices array into the vertices array of the Mesh.
            baseReference.mesh.vertices = vertices.ToArray();
            baseReference.mesh.SetIndices(indicies, MeshTopology.Points, 0);
            baseReference.mesh.RecalculateBounds();
        }
    }
}