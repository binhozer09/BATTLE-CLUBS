using System.Collections.Generic;
using UnityEngine;

namespace BattleClubs.World
{
    /// <summary>
    /// Adds recognizable community infrastructure: concrete stair flights,
    /// overhead utility cables, rooftop water pipes and small retaining walls.
    /// Geometry is generated once and kept under this component for cleanup.
    /// </summary>
    public sealed class CommunityArtDetails : MonoBehaviour
    {
        [SerializeField, Min(1)] private int blocksX = 5;
        [SerializeField, Min(1)] private int blocksZ = 5;
        [SerializeField, Min(8f)] private float blockSize = 18f;
        [SerializeField, Min(3f)] private float roadWidth = 7f;
        [SerializeField, Min(3f)] private float alleyWidth = 5f;
        [SerializeField] private float cableHeight = 7.5f;
        [SerializeField] private int randomSeed = 8031;

        private readonly List<GameObject> generated = new List<GameObject>();
        private static readonly Color StepColor = new Color(0.38f, 0.37f, 0.34f);
        private static readonly Color CableColor = new Color(0.055f, 0.06f, 0.065f);
        private static readonly Color PipeColor = new Color(0.20f, 0.25f, 0.26f);

        private void Start() => Generate();

        [ContextMenu("Generate Community Art Details")]
        public void Generate()
        {
            Clear();
            Random.State oldState = Random.state;
            Random.InitState(randomSeed);
            float cell = blockSize + roadWidth;
            float totalX = blocksX * blockSize + (blocksX - 1) * roadWidth;
            float totalZ = blocksZ * blockSize + (blocksZ - 1) * roadWidth;
            Vector3 origin = transform.position;

            BuildStairFlights(origin, cell);
            BuildOverheadCables(origin, totalX, totalZ, cell);
            BuildDrainageAndRetainingWalls(origin, totalX, totalZ, cell);
            Random.state = oldState;
        }

        private void BuildStairFlights(Vector3 origin, float cell)
        {
            // Concrete stair flights climb through selected alleys, not across every lane.
            for (int x = 0; x < blocksX; x++)
            for (int z = 0; z < blocksZ; z++)
            {
                // Keep the reserved central plaza free of stair geometry.
                if ((x + z) % 2 != 0 || (blocksX >= 3 && blocksZ >= 3 &&
                    x == blocksX / 2 && z == blocksZ / 2)) continue;
                float lane = Mathf.Min(alleyWidth * 0.72f, 3.4f);
                float startX = origin.x + x * cell + 1.5f;
                float centerZ = origin.z + z * cell + blockSize * 0.5f;
                int steps = 9;
                float tread = (blockSize - 3f) / steps;
                float rise = 0.13f + Random.Range(0f, 0.035f);
                float yaw = Random.value > 0.5f ? 0f : 180f;

                for (int i = 0; i < steps; i++)
                {
                    float y = (i + 0.5f) * rise;
                    Vector3 pos = new Vector3(startX + (i + 0.5f) * tread, y, centerZ);
                    CreateCube("Community_Stair_Step", pos,
                        new Vector3(tread + 0.035f, 0.22f, lane), StepColor, yaw);
                    CreateCube("Stair_Riser_Edge",
                        pos + new Vector3(-tread * 0.46f, -0.055f, 0f),
                        new Vector3(0.08f, 0.18f, lane), new Color(0.29f, 0.29f, 0.27f), yaw);
                }

                // Side curbs keep the stair silhouette readable from the gameplay camera.
                float endX = startX + steps * tread;
                for (int side = -1; side <= 1; side += 2)
                {
                    CreateCube("Stair_Side_Curb",
                        new Vector3((startX + endX) * 0.5f, steps * rise * 0.5f,
                            centerZ + side * (lane * 0.5f + 0.12f)),
                        new Vector3(steps * tread, 0.34f, 0.22f), StepColor, yaw);
                }
            }
        }

        private void BuildOverheadCables(Vector3 origin, float width, float depth, float cell)
        {
            // Utility cables sag slightly between poles/building corners.
            for (int x = 0; x < blocksX - 1; x++)
            {
                float roadX = origin.x + (x + 1) * blockSize + x * roadWidth + roadWidth * 0.5f;
                for (int z = 0; z < blocksZ; z += 2)
                {
                    float z0 = origin.z + z * cell + 2f;
                    float z1 = Mathf.Min(origin.z + depth - 2f, z0 + blockSize + roadWidth);
                    Vector3 a = new Vector3(roadX - 2.8f, cableHeight, z0);
                    Vector3 b = new Vector3(roadX + 2.8f, cableHeight - 0.45f, z1);
                    CreateCable("Power_Cable", a, b, 0.035f);
                }
            }

            for (int z = 0; z < blocksZ - 1; z++)
            {
                float roadZ = origin.z + (z + 1) * blockSize + z * roadWidth + roadWidth * 0.5f;
                for (int x = 0; x < blocksX; x += 2)
                {
                    float x0 = origin.x + x * cell + 2f;
                    float x1 = Mathf.Min(origin.x + width - 2f, x0 + blockSize + roadWidth);
                    Vector3 a = new Vector3(x0, cableHeight - 0.35f, roadZ - 2.5f);
                    Vector3 b = new Vector3(x1, cableHeight, roadZ + 2.5f);
                    CreateCable("Power_Cable_Cross", a, b, 0.028f);
                }
            }
        }

        private void BuildDrainageAndRetainingWalls(Vector3 origin, float width, float depth, float cell)
        {
            // Open concrete drains beside selected roads and retaining walls at the map edge.
            for (int x = 0; x < blocksX - 1; x++)
            {
                float roadX = origin.x + (x + 1) * blockSize + x * roadWidth + roadWidth * 0.5f;
                CreateCube("Road_Drain_Channel",
                    new Vector3(roadX, -0.02f, origin.z + depth * 0.5f),
                    new Vector3(0.34f, 0.08f, depth), new Color(0.12f, 0.13f, 0.12f));
                for (float z = 4f; z < depth - 3f; z += 3.2f)
                    CreateCube("Drain_Grate",
                        new Vector3(roadX, 0.035f, origin.z + z),
                        new Vector3(0.42f, 0.035f, 0.08f), new Color(0.28f, 0.29f, 0.27f));
            }

            CreateCube("Perimeter_Retaining_Wall",
                new Vector3(origin.x + width * 0.5f, 0.8f, origin.z - 1.3f),
                new Vector3(width + 5f, 1.6f, 0.55f), new Color(0.34f, 0.34f, 0.32f));
            CreateCube("Perimeter_Retaining_Wall_Back",
                new Vector3(origin.x + width * 0.5f, 1.1f, origin.z + depth + 1.3f),
                new Vector3(width + 5f, 2.2f, 0.55f), new Color(0.34f, 0.34f, 0.32f));
        }

        private void CreateCable(string objectName, Vector3 start, Vector3 end, float thickness)
        {
            GameObject cable = new GameObject(objectName);
            cable.transform.SetParent(transform, true);
            LineRenderer line = cable.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 9;
            line.startWidth = thickness;
            line.endWidth = thickness;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            Vector3 delta = end - start;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                point.y -= Mathf.Sin(t * Mathf.PI) * 0.55f;
                line.SetPosition(i, point);
            }

            line.sharedMaterial = CommunityMaterialLibrary.Get(CableColor);
            generated.Add(cable);
        }

        private void CreateCube(string objectName, Vector3 position, Vector3 scale, Color color, float yaw = 0f)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = objectName;
            item.transform.SetParent(transform, true);
            item.transform.position = position;
            item.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            item.transform.localScale = scale;
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = CommunityMaterialLibrary.Get(color);
            generated.Add(item);
        }

        private void Clear()
        {
            for (int i = generated.Count - 1; i >= 0; i--)
            {
                if (generated[i] == null) continue;
                if (Application.isPlaying) Destroy(generated[i]);
                else DestroyImmediate(generated[i]);
            }
            generated.Clear();
        }
    }
}