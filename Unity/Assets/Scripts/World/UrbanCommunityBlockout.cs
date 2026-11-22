using System.Collections.Generic;
using UnityEngine;
using BattleClubs.Art;

namespace BattleClubs.World
{
    /// <summary>
    /// Generates a stylized Brazilian hillside community map for the first playable
    /// visual pass. All geometry is procedural placeholder art and can be replaced
    /// by optimized, licensed models without changing gameplay scripts.
    /// </summary>
    public sealed class UrbanCommunityBlockout : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField, Min(1)] private int blocksX = 5;
        [SerializeField, Min(1)] private int blocksZ = 5;
        [SerializeField, Min(8f)] private float blockSize = 18f;
        [SerializeField, Min(3f)] private float alleyWidth = 5f;
        [SerializeField, Min(2f)] private float roadWidth = 7f;
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool createSpawnMarkers = true;

        [Header("Buildings")]
        [SerializeField, Min(3f)] private float minBuildingHeight = 4f;
        [SerializeField, Min(3f)] private float maxBuildingHeight = 10f;
        [SerializeField] private int randomSeed = 2026;

        [Header("Street details")]
        [SerializeField] private bool addStreetLights = true;
        [SerializeField] private bool addRooftopTanks = true;
        [SerializeField] private bool addCourtMarkings = true;

        private readonly List<GameObject> generated = new List<GameObject>();
        private readonly List<Mesh> generatedMeshes = new List<Mesh>();

        private static readonly Color Asphalt = new Color(0.19f, 0.205f, 0.20f);
        private static readonly Color Concrete = new Color(0.43f, 0.43f, 0.40f);
        private static readonly Color DarkMetal = new Color(0.12f, 0.14f, 0.15f);
        private static readonly Color Glass = new Color(0.075f, 0.16f, 0.19f);
        private static readonly Color[] FacadePalette =
        {
            new Color(0.78f, 0.48f, 0.29f), new Color(0.78f, 0.68f, 0.48f),
            new Color(0.43f, 0.61f, 0.58f), new Color(0.68f, 0.67f, 0.57f),
            new Color(0.66f, 0.39f, 0.34f), new Color(0.77f, 0.73f, 0.62f),
            new Color(0.48f, 0.55f, 0.42f)
        };

        private void Start()
        {
            if (generateOnStart) Generate();
        }

        [ContextMenu("Generate Community Map")]
        public void Generate()
        {
            ClearGenerated();
            Random.State previousRandomState = Random.state;
            Random.InitState(randomSeed);

            float cell = blockSize + roadWidth;
            float totalX = blocksX * blockSize + (blocksX - 1) * roadWidth;
            float totalZ = blocksZ * blockSize + (blocksZ - 1) * roadWidth;
            Vector3 origin = transform.position;

            CreatePrimitive(PrimitiveType.Cube, "Map_Ground",
                origin + new Vector3(totalX * 0.5f, -0.65f, totalZ * 0.5f),
                new Vector3(totalX + roadWidth, 1f, totalZ + roadWidth), Asphalt);

            // Narrow sidewalks and the two rows of houses form readable combat lanes.
            for (int x = 0; x < blocksX; x++)
            {
                for (int z = 0; z < blocksZ; z++)
                {
                    // Reserve the central block as an open combat plaza. The court
                    // occupies this space, so surrounding houses must not intersect it.
                    if (blocksX >= 3 && blocksZ >= 3 &&
                        x == blocksX / 2 && z == blocksZ / 2)
                        continue;

                    Vector3 center = origin + new Vector3(x * cell + blockSize * 0.5f, 0f,
                        z * cell + blockSize * 0.5f);
                    BuildBlock(center, x, z);
                }
            }

            BuildMainStreetDetails(origin, totalX, totalZ);
            BuildCentralCourt(origin, totalX, totalZ);
            BuildHillsideBackdrop(origin, totalX, totalZ);

            if (createSpawnMarkers)
                CreateSpawns(origin, totalX, totalZ);

            TeamSpawnManager spawnManager = GetComponent<TeamSpawnManager>();
            if (spawnManager != null) spawnManager.RefreshSpawnPoints();

            Random.state = previousRandomState;
        }


        /// <summary>
        /// Builds a distant hillside silhouette around the playable neighborhood.
        /// This is background terrain, not a replacement for the walkable streets.
        /// </summary>
        private void BuildHillsideBackdrop(Vector3 origin, float width, float depth)
        {
            const float setback = 3f;
            const float reach = 46f;
            BuildHillsideStrip("Hillside_North", origin,
                new Vector2(0f, depth + setback), new Vector2(width, depth + setback),
                reach, false, 1f, 0.13f);
            BuildHillsideStrip("Hillside_South", origin,
                new Vector2(width, -setback), new Vector2(0f, -setback),
                reach, false, -1f, 0.37f);
            BuildHillsideStrip("Hillside_East", origin,
                new Vector2(width + setback, depth), new Vector2(width + setback, 0f),
                reach, true, 1f, 0.59f);
            BuildHillsideStrip("Hillside_West", origin,
                new Vector2(-setback, 0f), new Vector2(-setback, depth),
                reach, true, -1f, 0.83f);
        }

        private void BuildHillsideStrip(string objectName, Vector3 origin,
            Vector2 start, Vector2 end, float reach, bool extendAlongX, float outwardSign, float noiseOffset)
        {
            const int alongSegments = 28;
            const int depthSegments = 10;
            var vertices = new Vector3[(alongSegments + 1) * (depthSegments + 1)];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[alongSegments * depthSegments * 6];

            for (int z = 0; z <= depthSegments; z++)
            {
                float d = z / (float)depthSegments;
                for (int x = 0; x <= alongSegments; x++)
                {
                    float t = x / (float)alongSegments;
                    Vector2 edge = Vector2.Lerp(start, end, t);
                    float ridge = Mathf.PerlinNoise(t * 3.8f + noiseOffset * 17f, d * 2.6f + randomSeed * 0.001f);
                    float detail = Mathf.PerlinNoise(t * 15f + 4f, d * 11f + noiseOffset * 31f);
                    float rise = 7f + ridge * 15f + detail * 2.5f;
                    float terrace = Mathf.Floor(rise / 2.2f) * 2.2f;
                    float distance = d * reach;
                    Vector2 point = extendAlongX
                        ? edge + new Vector2(outwardSign * distance, 0f)
                        : edge + new Vector2(0f, outwardSign * distance);
                    int vertexIndex = z * (alongSegments + 1) + x;
                    vertices[vertexIndex] = new Vector3(point.x, terrace - d * 1.8f, point.y);
                    // Explicit UVs are required: without them the hillside samples
                    // one texel and appears as a flat color despite its material.
                    uvs[vertexIndex] = new Vector2(t * 7f, d * 3f);
                }
            }

            int index = 0;
            for (int z = 0; z < depthSegments; z++)
            for (int x = 0; x < alongSegments; x++)
            {
                int a = z * (alongSegments + 1) + x;
                int b = a + 1;
                int c = a + alongSegments + 1;
                int d = c + 1;
                triangles[index++] = a; triangles[index++] = c; triangles[index++] = b;
                triangles[index++] = b; triangles[index++] = c; triangles[index++] = d;
            }

            Mesh mesh = new Mesh { name = objectName + "_Mesh" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            generatedMeshes.Add(mesh);

            GameObject hill = new GameObject(objectName);
            hill.transform.SetParent(transform, false);
            hill.transform.position = origin;
            hill.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = hill.AddComponent<MeshRenderer>();
            CommunityMaterialLibrary.ApplySurface(renderer,
                new Color(0.24f, 0.27f, 0.22f),
                ProceduralSurfaceTextures.Surface.Concrete, new Vector2(7f, 3f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            generated.Add(hill);
        }

        private void BuildBlock(Vector3 center, int blockX, int blockZ)
        {
            float half = blockSize * 0.5f;
            float lane = Mathf.Clamp(alleyWidth, 2f, blockSize * 0.35f);
            float buildingDepth = (blockSize - lane) * 0.5f;
            float sideOffset = (lane + buildingDepth) * 0.5f;

            for (int side = -1; side <= 1; side += 2)
            {
                float height = Random.Range(minBuildingHeight, Mathf.Max(minBuildingHeight + 0.1f, maxBuildingHeight));
                float width = Random.Range(blockSize * 0.34f, blockSize * 0.47f);
                float depth = buildingDepth * Random.Range(0.72f, 0.96f);
                float xOffset = Random.Range(-half * 0.12f, half * 0.12f);
                Vector3 position = center + new Vector3(xOffset, height * 0.5f, side * sideOffset);
                Color wall = FacadePalette[Random.Range(0, FacadePalette.Length)];

                CreatePrimitive(PrimitiveType.Cube, "House_" + blockX + "_" + blockZ + "_" + side,
                    position, new Vector3(width, height, depth), wall);

                // Flat roof, low parapet and rooftop utility details create a recognizable skyline.
                Vector3 roof = position + Vector3.up * (height * 0.5f + 0.12f);
                CreatePrimitive(PrimitiveType.Cube, "Roof",
                    roof, new Vector3(width + 0.3f, 0.24f, depth + 0.3f), new Color(0.24f, 0.25f, 0.24f));
                CreatePrimitive(PrimitiveType.Cube, "RoofParapet",
                    roof + Vector3.up * 0.28f + Vector3.forward * (side * depth * 0.43f),
                    new Vector3(width + 0.25f, 0.48f, 0.16f), new Color(0.31f, 0.31f, 0.29f));

                BuildFacade(position, width, height, depth, side, blockX, blockZ);

                if (addRooftopTanks && Random.value > 0.28f)
                {
                    Vector3 tankPosition = roof + Vector3.up * 0.95f
                        + new Vector3(Random.Range(-width * 0.25f, width * 0.25f), 0f,
                            Random.Range(-depth * 0.2f, depth * 0.2f));
                    CreatePrimitive(PrimitiveType.Cylinder, "WaterTank",
                        tankPosition, new Vector3(1.15f, 0.75f, 1.15f), new Color(0.12f, 0.19f, 0.22f));
                    CreatePrimitive(PrimitiveType.Cylinder, "WaterTankLid",
                        tankPosition + Vector3.up * 0.78f, new Vector3(1.2f, 0.06f, 1.2f), DarkMetal);
                }
            }

            // Keep both ends of the alley completely open. Full-width cover walls
            // here used to seal the passage between neighboring blocks and cut off
            // the stair flights generated by CommunityArtDetails. Cover belongs
            // beside combat lanes, never across their only traversal connection.

            // A small streetlight at alternating corners adds depth without cluttering every lane.
            if (addStreetLights && (blockX + blockZ) % 2 == 0)
                BuildStreetLight(center + new Vector3(-half + 0.7f, 0f, -half + 0.7f));
        }

        private void BuildFacade(Vector3 house, float width, float height, float depth,
            int side, int blockX, int blockZ)
        {
            // The front of each row faces the playable alley. Small architectural
            // layers break up the old flat-box silhouette without adding asset dependencies.
            float frontZ = house.z - side * (depth * 0.5f + 0.055f);
            float windowY = Mathf.Min(height * 0.67f, height - 1.25f);
            float windowSpacing = width * 0.27f;
            Color trim = new Color(0.84f, 0.79f, 0.67f);
            Color shutter = new Color(0.22f, 0.25f, 0.24f);

            // A continuous upper cornice gives each facade a readable roof edge.
            CreatePrimitive(PrimitiveType.Cube, "FacadeCornice",
                new Vector3(house.x, house.y + height * 0.5f - 0.12f, frontZ - side * 0.10f),
                new Vector3(width + 0.22f, 0.20f, 0.24f), trim);

            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 window = new Vector3(house.x + i * windowSpacing, windowY, frontZ);
                Vector3 faceOffset = new Vector3(0f, 0f, -side * 0.11f);
                CreatePrimitive(PrimitiveType.Cube, "WindowFrame", window,
                    new Vector3(1.12f, 1.05f, 0.12f), trim);
                CreatePrimitive(PrimitiveType.Cube, "WindowGlass", window + faceOffset,
                    new Vector3(0.82f, 0.76f, 0.06f), Glass);
                CreatePrimitive(PrimitiveType.Cube, "WindowDivider", window + faceOffset * 1.45f,
                    new Vector3(0.07f, 0.8f, 0.05f), DarkMetal);
                CreatePrimitive(PrimitiveType.Cube, "WindowSill",
                    window + new Vector3(0f, -0.56f, -side * 0.14f),
                    new Vector3(1.28f, 0.12f, 0.30f), Concrete);

                // Occasional metal shutters make neighboring houses less repetitive.
                if ((blockX + blockZ + i) % 3 == 0)
                {
                    CreatePrimitive(PrimitiveType.Cube, "WindowShutter",
                        window + new Vector3(i * 0.57f, 0f, -side * 0.15f),
                        new Vector3(0.12f, 1.02f, 0.16f), shutter);
                }
            }

            Vector3 door = new Vector3(house.x, 1.05f, frontZ - side * 0.025f);
            CreatePrimitive(PrimitiveType.Cube, "DoorFrame", door + Vector3.up * 0.04f,
                new Vector3(1.24f, 2.22f, 0.18f), trim);
            CreatePrimitive(PrimitiveType.Cube, "FrontDoor", door + new Vector3(0f, 0f, -side * 0.12f),
                new Vector3(1.05f, 2.1f, 0.16f), new Color(0.27f, 0.19f, 0.14f));
            CreatePrimitive(PrimitiveType.Sphere, "DoorHandle",
                door + new Vector3(0.34f, 0f, -side * 0.24f),
                new Vector3(0.09f, 0.09f, 0.09f), new Color(0.82f, 0.67f, 0.34f));
            CreatePrimitive(PrimitiveType.Cube, "DoorStep",
                door + new Vector3(0f, -0.98f, -side * 0.25f),
                new Vector3(1.42f, 0.16f, 0.52f), Concrete);

            // A small awning over the entrance adds depth and a distinct street-level silhouette.
            if ((blockX * 3 + blockZ) % 2 == 0)
            {
                CreatePrimitive(PrimitiveType.Cube, "DoorAwning",
                    door + new Vector3(0f, 1.22f, -side * 0.48f),
                    new Vector3(1.55f, 0.12f, 0.78f), shutter);
                for (int post = -1; post <= 1; post += 2)
                    CreatePrimitive(PrimitiveType.Cylinder, "AwningSupport",
                        door + new Vector3(post * 0.64f, 0.63f, -side * 0.73f),
                        new Vector3(0.035f, 0.62f, 0.035f), DarkMetal);
            }

            // Balcony railings use individual uprights instead of a single floating bar.
            if (Random.value > 0.42f && height > 5.2f)
            {
                Vector3 balcony = new Vector3(house.x, windowY - 0.7f, frontZ - side * 0.45f);
                float balconyWidth = width * 0.62f;
                CreatePrimitive(PrimitiveType.Cube, "BalconySlab", balcony,
                    new Vector3(balconyWidth + 0.18f, 0.16f, 0.95f), Concrete);
                Vector3 railCenter = balcony + new Vector3(0f, 0.48f, -side * 0.42f);
                CreatePrimitive(PrimitiveType.Cube, "BalconyRail", railCenter,
                    new Vector3(balconyWidth, 0.08f, 0.08f), DarkMetal);
                int bars = Mathf.Max(4, Mathf.RoundToInt(balconyWidth / 0.28f));
                for (int bar = 0; bar <= bars; bar++)
                {
                    float x = -balconyWidth * 0.5f + balconyWidth * bar / bars;
                    CreatePrimitive(PrimitiveType.Cube, "BalconyBar",
                        railCenter + new Vector3(x, -0.23f, 0f),
                        new Vector3(0.045f, 0.48f, 0.055f), DarkMetal);
                }
            }

            // Air-conditioning unit with a drain pipe running down the wall.
            if (Random.value > 0.55f)
            {
                Vector3 ac = new Vector3(house.x + width * 0.37f, windowY - 0.35f,
                    frontZ - side * 0.16f);
                CreatePrimitive(PrimitiveType.Cube, "AirConditioner", ac,
                    new Vector3(0.65f, 0.42f, 0.34f), new Color(0.72f, 0.73f, 0.70f));
                CreatePrimitive(PrimitiveType.Cube, "ACVent",
                    ac + new Vector3(0f, 0f, -side * 0.19f),
                    new Vector3(0.43f, 0.18f, 0.035f), DarkMetal);
                CreatePrimitive(PrimitiveType.Cylinder, "ACDrainPipe",
                    ac + new Vector3(0.34f, -0.58f, -side * 0.08f),
                    new Vector3(0.035f, 0.68f, 0.035f), new Color(0.67f, 0.68f, 0.63f));
            }
        }

        private void BuildStreetLight(Vector3 basePosition)
        {
            CreatePrimitive(PrimitiveType.Cylinder, "LampPost",
                basePosition + Vector3.up * 2.4f, new Vector3(0.09f, 2.4f, 0.09f), DarkMetal);
            CreatePrimitive(PrimitiveType.Cube, "LampArm",
                basePosition + new Vector3(0.35f, 4.65f, 0f),
                new Vector3(0.75f, 0.08f, 0.08f), DarkMetal);
            CreatePrimitive(PrimitiveType.Cube, "LampHead",
                basePosition + new Vector3(0.68f, 4.55f, 0f),
                new Vector3(0.42f, 0.16f, 0.28f), new Color(0.92f, 0.78f, 0.48f));
        }

        private void BuildMainStreetDetails(Vector3 origin, float width, float depth)
        {
            // Dashed center markings on the wider cross streets.
            for (int x = 0; x < blocksX - 1; x++)
            {
                float roadX = origin.x + (x + 1) * blockSize + x * roadWidth + roadWidth * 0.5f;
                for (float z = 2f; z < depth - 1f; z += 5.5f)
                    CreatePrimitive(PrimitiveType.Cube, "RoadDash",
                        new Vector3(roadX, -0.08f, origin.z + z),
                        new Vector3(0.12f, 0.035f, 2.2f), new Color(0.72f, 0.68f, 0.53f));
            }

            for (int z = 0; z < blocksZ - 1; z++)
            {
                float roadZ = origin.z + (z + 1) * blockSize + z * roadWidth + roadWidth * 0.5f;
                for (float x = 2f; x < width - 1f; x += 5.5f)
                    CreatePrimitive(PrimitiveType.Cube, "RoadDash",
                        new Vector3(origin.x + x, -0.08f, roadZ),
                        new Vector3(2.2f, 0.035f, 0.12f), new Color(0.72f, 0.68f, 0.53f));
            }
        }

        private void BuildCentralCourt(Vector3 origin, float width, float depth)
        {
            // The central block is deliberately left clear by BuildBlock; this
            // court becomes a real open landmark instead of clipping through houses.
            Vector3 center = origin + new Vector3(width * 0.5f, 0.08f, depth * 0.5f);
            CreatePrimitive(PrimitiveType.Cube, "CentralCourt",
                center, new Vector3(12f, 0.16f, 9f), new Color(0.18f, 0.34f, 0.24f));

            if (!addCourtMarkings) return;
            Color line = new Color(0.88f, 0.86f, 0.74f);
            CreatePrimitive(PrimitiveType.Cube, "CourtLine_Long",
                center + Vector3.up * 0.09f, new Vector3(0.07f, 0.025f, 8.5f), line);
            CreatePrimitive(PrimitiveType.Cube, "CourtLine_ShortA",
                center + new Vector3(-5.8f, 0.09f, 0f), new Vector3(0.07f, 0.025f, 8.5f), line);
            CreatePrimitive(PrimitiveType.Cube, "CourtLine_ShortB",
                center + new Vector3(5.8f, 0.09f, 0f), new Vector3(0.07f, 0.025f, 8.5f), line);
            CreatePrimitive(PrimitiveType.Cylinder, "CourtCenterCircle",
                center + Vector3.up * 0.11f, new Vector3(2.4f, 0.015f, 2.4f), line);
        }

        private void CreateSpawns(Vector3 origin, float width, float depth)
        {
            // Five spawn anchors, one for each team in the canonical 5x4 format.
            // The fifth anchor sits along the north edge to keep all starts separated.
            Vector3[] positions =
            {
                origin + new Vector3(3f, 0.15f, 3f),
                origin + new Vector3(width - 3f, 0.15f, 3f),
                origin + new Vector3(3f, 0.15f, depth - 3f),
                origin + new Vector3(width - 3f, 0.15f, depth - 3f),
                origin + new Vector3(width * 0.5f, 0.15f, depth - 3f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject marker = new GameObject("TeamSpawn_" + (i + 1));
                marker.transform.SetParent(transform, false);
                marker.transform.position = positions[i];
                generated.Add(marker);
            }
        }

        private GameObject CreatePrimitive(PrimitiveType type, string objectName, Vector3 position,
            Vector3 scale, Color color)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = objectName;
            item.transform.SetParent(transform, true);
            item.transform.position = position;
            item.transform.localScale = scale;

            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                Texture texture = ProceduralSurfaceTextures.Get(ResolveSurface(objectName));
                renderer.sharedMaterial = CommunityMaterialLibrary.Get(color, texture);
                CommunityMaterialLibrary.SetTextureScale(renderer, new Vector2(
                    Mathf.Max(1f, scale.x * 0.35f), Mathf.Max(1f, scale.y * 0.35f)));
            }

            generated.Add(item);
            return item;
        }

        private static ProceduralSurfaceTextures.Surface ResolveSurface(string objectName)
        {
            if (objectName == "Map_Ground" || objectName == "RoadDash")
                return ProceduralSurfaceTextures.Surface.Asphalt;
            if (objectName.StartsWith("House_"))
            {
                int stableHash = 17;
                for (int i = 0; i < objectName.Length; i++)
                    stableHash = stableHash * 31 + objectName[i];
                return (stableHash & 3) == 0
                    ? ProceduralSurfaceTextures.Surface.Brick
                    : ProceduralSurfaceTextures.Surface.Plaster;
            }
            if (objectName == "CentralCourt")
                return ProceduralSurfaceTextures.Surface.Court;
            if (objectName.StartsWith("Court"))
                return ProceduralSurfaceTextures.Surface.Concrete;
            if (objectName == "Roof")
                return ProceduralSurfaceTextures.Surface.Asphalt;
            if (objectName == "RoofParapet" || objectName == "AlleyCover")
                return ProceduralSurfaceTextures.Surface.Concrete;
            if (objectName == "FrontDoor")
                return ProceduralSurfaceTextures.Surface.Wood;
            return ProceduralSurfaceTextures.Surface.Plaster;
        }

        private void ClearGenerated()
        {
            for (int i = generated.Count - 1; i >= 0; i--)
            {
                if (generated[i] == null) continue;
                if (Application.isPlaying) Destroy(generated[i]);
                else DestroyImmediate(generated[i]);
            }
            generated.Clear();

            for (int i = generatedMeshes.Count - 1; i >= 0; i--)
            {
                if (generatedMeshes[i] == null) continue;
                if (Application.isPlaying) Destroy(generatedMeshes[i]);
                else DestroyImmediate(generatedMeshes[i]);
            }
            generatedMeshes.Clear();
        }
    }
}