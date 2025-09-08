using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RopeSystem
{
    /// <summary>
    /// RopeMesh Component
    /// Forked from GoGoGaGa with some optimization on calculation
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Rope))]
    public class RopeMesh : MonoBehaviour
    {
        [Range(3f, 25f)] public int OverallDivision = 6;
        [Range(0.01f, 10f)] public float ropeWidth = 0.3f;
        [Range(3f, 20f)] public int radialDivision = 8;

        [Tooltip("反照率材质")] public Material material;

        [Tooltip("绳子(每米)绘制密度")] public float tilingPerMeter = 1f;

        private Rope rope;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh ropeMesh;
        private bool isStartOrEndPointMissing;
        private readonly List<Vector3> vertices = new();
        private readonly List<int> triangles = new();
        private readonly List<Vector2> uvs = new();

        private void OnValidate()
        {
            InitializeComponents();
            if (rope.IsPrefab)
                return;
            SubscribeToRopeEvents();
            if (!(bool)(Object)meshRenderer || !(bool)(Object)material)
                return;
            meshRenderer.material = material;
        }

        private void Awake()
        {
            InitializeComponents();
            SubscribeToRopeEvents();
        }

        private void OnEnable()
        {
            SubscribeToRopeEvents();
        }

        private void OnDisable() => UnsubscribeFromRopeEvents();

        private void InitializeComponents()
        {
            if (!(bool)(Object)rope)
                rope = GetComponent<Rope>();
            if (!(bool)(Object)meshFilter)
                meshFilter = GetComponent<MeshFilter>();
            if (!(bool)(Object)meshRenderer)
                meshRenderer = GetComponent<MeshRenderer>();
            CheckEndPoints();
        }

        private void CheckEndPoints()
        {
            if (gameObject.scene.rootCount == 0)
                isStartOrEndPointMissing = false;
            else if (rope.StartPoint == null ||
                     rope.EndPoint == null)
            {
                isStartOrEndPointMissing = true;
                Debug.LogError("StartPoint和EndPoint不可以为空", gameObject);
            }
            else
                isStartOrEndPointMissing = false;
        }

        private void SubscribeToRopeEvents()
        {
            UnsubscribeFromRopeEvents();
            if (!(rope != null))
                return;
            rope.OnPointsChanged += GenerateMesh;
        }

        private void UnsubscribeFromRopeEvents()
        {
            if (!(rope != null))
                return;
            rope.OnPointsChanged -= GenerateMesh;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void CreateRopeMesh(Vector3[] points, float radius, int segmentsPerWire)
        {
            if (points == null || points.Length < 2)
            {
                Debug.LogError("Need at least two points to create a rope mesh.",
                    gameObject);
            }
            else
            {
                if (ropeMesh == null)
                {
                    var mesh = new Mesh
                    {
                        name = nameof(RopeMesh)
                    };
                    ropeMesh = mesh;
                    meshFilter.mesh = ropeMesh;
                }
                else
                    ropeMesh.Clear();

                vertices.Clear();
                triangles.Clear();
                uvs.Clear();
                var num1 = 0.0f;
                for (var index1 = 0; index1 < points.Length; ++index1)
                {
                    var quaternion = Quaternion.LookRotation(
                        index1 < points.Length - 1
                            ? points[index1 + 1] - points[index1]
                            : points[index1] - points[index1 - 1], Vector3.up);
                    for (var index2 = 0; index2 <= segmentsPerWire; ++index2)
                    {
                        var f = (float)(index2 * 3.1415927410125732 * 2.0) / segmentsPerWire;
                        var vector3 = new Vector3(Mathf.Cos(f), Mathf.Sin(f), 0.0f) * radius;
                        vertices.Add(transform.InverseTransformPoint(points[index1] + quaternion * vector3));
                        uvs.Add(new Vector2(index2 / (float)segmentsPerWire, num1 * tilingPerMeter));
                    }

                    if (index1 < points.Length - 1)
                        num1 += Vector3.Distance(points[index1], points[index1 + 1]);
                }

                for (var index3 = 0; index3 < points.Length - 1; ++index3)
                {
                    for (var index4 = 0; index4 < segmentsPerWire; ++index4)
                    {
                        var num2 = index3 * (segmentsPerWire + 1) + index4;
                        var num3 = num2 + 1;
                        var num4 = num2 + segmentsPerWire + 1;
                        var num5 = num4 + 1;
                        triangles.Add(num2);
                        triangles.Add(num3);
                        triangles.Add(num4);
                        triangles.Add(num3);
                        triangles.Add(num5);
                        triangles.Add(num4);
                    }
                }

                var count1 = vertices.Count;
                vertices.Add(transform.InverseTransformPoint(points[0]));
                uvs.Add(new Vector2(0.5f, 0.0f));
                var quaternion1 = Quaternion.LookRotation(points[1] - points[0]);
                for (var index = 0; index <= segmentsPerWire; ++index)
                {
                    var f = (float)(index * 3.1415927410125732 * 2.0) / segmentsPerWire;
                    var vector3 = new Vector3(Mathf.Cos(f), Mathf.Sin(f), 0.0f) * radius;
                    vertices.Add(transform.InverseTransformPoint(points[0] + quaternion1 * vector3));
                    if (index < segmentsPerWire)
                    {
                        triangles.Add(count1);
                        triangles.Add(count1 + index + 1);
                        triangles.Add(count1 + index + 2);
                    }

                    uvs.Add(new Vector2((float)((Mathf.Cos(f) + 1.0) / 2.0),
                        (float)((Mathf.Sin(f) + 1.0) / 2.0)));
                }

                var count2 = vertices.Count;
                vertices.Add(transform.InverseTransformPoint(points[^1]));
                uvs.Add(new Vector2(0.5f, num1 * tilingPerMeter));
                var quaternion2 = Quaternion.LookRotation(points[^1] - points[^2]);
                for (var index = 0; index <= segmentsPerWire; ++index)
                {
                    var f = (float)(index * 3.1415927410125732 * 2.0) / segmentsPerWire;
                    var vector3 = new Vector3(Mathf.Cos(f), Mathf.Sin(f), 0.0f) * radius;
                    vertices.Add(
                        transform.InverseTransformPoint(points[^1] + quaternion2 * vector3));
                    if (index < segmentsPerWire)
                    {
                        triangles.Add(count2);
                        triangles.Add(count2 + index + 1);
                        triangles.Add(count2 + index + 2);
                    }

                    uvs.Add(new Vector2((float)((Mathf.Cos(f) + 1.0) / 2.0),
                        (float)((Mathf.Sin(f) + 1.0) / 2.0)));
                }

                ropeMesh.vertices = vertices.ToArray();
                ropeMesh.triangles = triangles.ToArray();
                ropeMesh.uv = uvs.ToArray();
                ropeMesh.RecalculateNormals();
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void GenerateMesh()
        {
            if (!this ||
                !rope ||
                !meshFilter)
                return;
            if (isStartOrEndPointMissing)
            {
                if (!(meshFilter.sharedMesh))
                    return;
                meshFilter.sharedMesh.Clear();
            }
            else
            {
                var points = new Vector3[OverallDivision + 1];
                for (var index = 0; index < points.Length; ++index)
                    points[index] = rope.GetPointAt(index / (float)OverallDivision);
                CreateRopeMesh(points, ropeWidth, radialDivision);
            }
        }

        private void Update()
        {
            if (rope.IsPrefab || !Application.isPlaying)
                return;
            GenerateMesh();
        }

        private void DelayedGenerateMesh()
        {
            if (!(this != null))
                return;
            GenerateMesh();
        }

        private void OnDestroy()
        {
            UnsubscribeFromRopeEvents();
            if (meshRenderer != null)
                Destroy(meshRenderer);
            if (!(meshFilter != null))
                return;
            Destroy(meshFilter);
        }
    }
}