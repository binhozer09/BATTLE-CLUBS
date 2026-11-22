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

            GameObject ground = CreatePrimitive(PrimitiveType.Cube, "Map_Ground",
                origin + new Vector3(totalX * 0.5f, -0.5f, totalZ * 0.5f),
                new Vector3(totalX + roadWidth, 1f, totalZ + roadWidth), Asphalt);
            // Keep the playable floor's physics surface explicit and non-trigger.
            // CreatePrimitive normally adds this BoxCollider, but enforce it here so
            // later prefab or generator changes cannot leave a render-only floor.
            BoxCollider groundCollider = ground.GetComponent<BoxCollider>();
            if (groundCollider == null) groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.center = Vector3.zero;
            groundCollider.size = Vector3.one;
            groundCollider.isTrigger = false;
            groundCollider.enabled = true;

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

            // Extend the visible neighborhood up the surrounding hills. These
            // small settlements sit beyond the playable blocks, adding a real
            // urban skyline instead of leaving the terrain as empty green slopes.
            BuildDistantHillsideHomes(origin, width, depth, reach);
            BuildHillsideVegetation(origin, width, depth, reach);
        }

        private void BuildHillsideVegetation(Vector3 origin, float width, float depth, float reach)
        {
            Random.State previous = Random.state;
            Random.InitState(randomSeed + 1943);
            const int treesPerEdge = 9;
            for (int edge = 0; edge < 4; edge++)
            for (int i = 0; i < treesPerEdge; i++)
            {
                float t = (i + 0.5f) / treesPerEdge + Random.Range(-0.035f, 0.035f);
                float distance = Random.Range(9f, reach - 3f);
                Vector2 edgePoint, outward;
                if (edge == 0) { edgePoint = new Vector2(t * width, depth + 3f); outward = Vector2.up; }
                else if (edge == 1) { edgePoint = new Vector2(t * width, -3f); outward = Vector2.down; }
                else if (edge == 2) { edgePoint = new Vector2(width + 3f, t * depth); outward = Vector2.right; }
                else { edgePoint = new Vector2(-3f, t * depth); outward = Vector2.left; }

                Vector2 point = edgePoint + outward * distance;
                float ridge = Mathf.PerlinNoise(t * 3.8f + edge * 0.21f,
                    distance / reach * 2.6f + randomSeed * 0.001f);
                float detail = Mathf.PerlinNoise(t * 15f + 4f, distance / reach * 11f + edge * 0.17f);
                float groundY = Mathf.Floor((7f + ridge * 15f + detail * 2.5f) / 2.2f) * 2.2f
                    - distance / reach * 1.8f;
                Vector3 basePos = origin + new Vector3(point.x, groundY + 0.1f, point.y);

                if (Random.value < 0.68f)
                {
                    float trunkHeight = Random.Range(2.2f, 3.8f);
                    float canopy = Random.Range(2.2f, 3.5f);
                    CreatePrimitive(PrimitiveType.Cylinder, "Vegetation_Tree_Trunk",
                        basePos + Vector3.up * (trunkHeight * 0.5f),
                        new Vector3(Random.Range(0.22f, 0.38f), trunkHeight * 0.5f,
                            Random.Range(0.22f, 0.38f)), new Color(0.25f, 0.16f, 0.09f));
                    Vector3 crown = basePos + Vector3.up * (trunkHeight + canopy * 0.28f);
                    CreatePrimitive(PrimitiveType.Sphere, "Vegetation_Tree_Canopy",
                        crown, new Vector3(canopy * 1.5f, canopy, canopy * 1.4f),
                        new Color(Random.Range(0.68f, 0.9f), Random.Range(0.78f, 0.95f), 0.62f));
                    CreatePrimitive(PrimitiveType.Sphere, "Vegetation_Tree_Canopy",
                        crown + new Vector3(canopy * 0.42f, canopy * 0.16f, canopy * 0.12f),
                        new Vector3(canopy, canopy * 0.82f, canopy),
                        new Color(0.72f, 0.88f, 0.58f));
                }
                else
                {
                    CreatePrimitive(PrimitiveType.Sphere, "Vegetation_Shrub",
                        basePos + Vector3.up * 0.45f,
                        new Vector3(Random.Range(1.0f, 1.8f), 0.8f, Random.Range(1.0f, 1.8f)),
                        new Color(0.72f, 0.86f, 0.56f));
                }
            }
            Random.state = previous;
        }

        private void BuildDistantHillsideHomes(Vector3 origin, float width, float depth, float reach)
        {
            Random.State previous = Random.state;
            Random.InitState(randomSeed + 731);

            const int homesAlongEdge = 6;
            float[] distances = { 12f, 27f, 40f };
            for (int edge = 0; edge < 4; edge++)
            for (int row = 0; row < distances.Length; row++)
            for (int i = 0; i < homesAlongEdge; i++)
            {
                float t = (i + 0.5f) / homesAlongEdge + Random.Range(-0.025f, 0.025f);
                float distance = distances[row] + Random.Range(-2.2f, 2.2f);
                Vector2 edgePoint;
                Vector2 outward;
                if (edge == 0) { edgePoint = new Vector2(t * width, depth + 3f); outward = Vector2.up; }
                else if (edge == 1) { edgePoint = new Vector2(t * width, -3f); outward = Vector2.down; }
                else if (edge == 2) { edgePoint = new Vector2(width + 3f, t * depth); outward = Vector2.right; }
                else { edgePoint = new Vector2(-3f, t * depth); outward = Vector2.left; }

                Vector2 point = edgePoint + outward * distance;
                float terrainT = edge < 2 ? t : t;
                float ridge = Mathf.PerlinNoise(terrainT * 3.8f + edge * 0.21f,
                    distance / reach * 2.6f + randomSeed * 0.001f);
                float detail = Mathf.PerlinNoise(terrainT * 15f + 4f,
                    distance / reach * 11f + edge * 0.17f);
                float terrainY = Mathf.Floor((7f + ridge * 15f + detail * 2.5f) / 2.2f) * 2.2f
                    - distance / reach * 1.8f;

                float houseWidth = Random.Range(5.2f, 8.2f);
                float houseDepth = Random.Range(4.2f, 6.5f);
                float houseHeight = Random.Range(3.2f, 5.6f);
                float yaw = edge < 2 ? 0f : 90f;
                Vector3 position = origin + new Vector3(point.x, terrainY + houseHeight * 0.5f, point.y);
                Vector3 size = edge < 2
                    ? new Vector3(houseWidth, houseHeight, houseDepth)
                    : new Vector3(houseDepth, houseHeight, houseWidth);

                CreatePrimitive(PrimitiveType.Cube, "Hillside_House_" + edge + "_" + row + "_" + i,
                    position, size, new Color(0.88f, 0.88f, 0.84f));
                Vector3 roof = position + Vector3.up * (houseHeight * 0.5f + 0.12f);
                CreatePrimitive(PrimitiveType.Cube, "Hillside_House_Roof",
                    roof, new Vector3(size.x + 0.25f, 0.22f, size.z + 0.25f),
                    new Color(0.25f, 0.26f, 0.25f));
            }

            Random.state = previous;
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
            // The hillside is generated from a custom mesh, so unlike primitives
            // it does not receive a collider automatically.
            MeshCollider hillCollider = hill.AddComponent<MeshCollider>();
            hillCollider.sharedMesh = mesh;
            hillCollider.convex = false;
            MeshRenderer renderer = hill.AddComponent<MeshRenderer>();
            CommunityMaterialLibrary.ApplySurface(renderer,
                new Color(0.24f, 0.27f, 0.22f),
                ProceduralSurfaceTextures.Surface.HillsideDirt, new Vector2(7f, 3f));
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

            // Give every residential block a distinct, continuous concrete walkway
            // through its central alley. The main cross streets remain asphalt,
            // making the pedestrian route readable instead of one giant flat road.
            BuildAlleyWalkway(center, lane, blockX, blockZ);

            for (int side = -1; side <= 1; side += 2)
            {
                float height = Random.Range(minBuildingHeight, Mathf.Max(minBuildingHeight + 0.1f, maxBuildingHeight));
                float width = Random.Range(blockSize * 0.34f, blockSize * 0.47f);
                float depth = buildingDepth * Random.Range(0.72f, 0.96f);
                float xOffset = Random.Range(-half * 0.12f, half * 0.12f);
                Vector3 position = center + new Vector3(xOffset, height * 0.5f, side * sideOffset);
                // Exposed ordinary cement masonry blocks: keep the albedo neutral so the block texture is not tinted like clay brick or painted plaster.
                Color wall = new Color(0.92f, 0.92f, 0.89f);

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

                BuildRooftopDetails(roof, width, depth, blockX, blockZ, side);
            }

            // Keep both ends of the alley completely open. Full-width cover walls
            // here used to seal the passage between neighboring blocks and cut off
            // the stair flights generated by CommunityArtDetails. Cover belongs
            // beside combat lanes, never across their only traversal connection.

            // A small streetlight at alternating corners adds depth without cluttering every lane.
            if (addStreetLights && (blockX + blockZ) % 2 == 0)
                BuildStreetLight(center + new Vector3(-half + 0.7f, 0f, -half + 0.7f));
        }


        private void BuildAlleyWalkway(Vector3 center, float lane, int blockX, int blockZ)
        {
            Color walkwayColor = new Color(0.68f, 0.67f, 0.63f);
            CreatePrimitive(PrimitiveType.Cube, "Alley_Concrete_Walkway",
                center + Vector3.up * 0.025f,
                new Vector3(blockSize, 0.05f, Mathf.Max(1f, lane - 0.34f)),
                walkwayColor);

            // Low curbs frame the passage while leaving the stair flight's
            // walking width clear. They also separate the alley from door fronts.
            for (int side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(PrimitiveType.Cube, "Alley_Low_Curb",
                    center + new Vector3(0f, 0.065f, side * (lane * 0.5f - 0.11f)),
                    new Vector3(blockSize, 0.13f, 0.18f),
                    new Color(0.42f, 0.42f, 0.39f));
            }

            // Occasional worn repair patches keep the walkway from reading as
            // a perfectly repeated strip of new concrete.
            if ((blockX + blockZ) % 3 == 0)
            {
                CreatePrimitive(PrimitiveType.Cube, "Alley_Concrete_Repair",
                    center + new Vector3(-blockSize * 0.22f, 0.052f, 0f),
                    new Vector3(blockSize * 0.12f, 0.012f, lane * 0.42f),
                    new Color(0.55f, 0.54f, 0.50f));
            }
        }

        private void BuildRooftopDetails(Vector3 roof, float width, float depth,
            int blockX, int blockZ, int side)
        {
            // Small rooftop rooms, patched metal roofs, antennas and laundry make
            // the skyline feel inhabited instead of repeating identical flat boxes.
            int variation = Mathf.Abs(blockX * 17 + blockZ * 31 + side * 13) % 10;
            float roofTop = roof.y + 0.12f;

            if (variation <= 3)
            {
                Vector3 room = roof + new Vector3(width * 0.22f, 0.88f, side * depth * 0.12f);
                float roomWidth = Mathf.Clamp(width * 0.34f, 1.6f, 2.5f);
                float roomDepth = Mathf.Clamp(depth * 0.42f, 1.4f, 2.2f);
                CreatePrimitive(PrimitiveType.Cube, "Rooftop_Utility_Room",
                    room, new Vector3(roomWidth, 1.55f, roomDepth),
                    new Color(0.62f, 0.59f, 0.51f));
                CreatePrimitive(PrimitiveType.Cube, "Rooftop_Corrugated_Canopy",
                    room + Vector3.up * 0.82f,
                    new Vector3(roomWidth + 0.24f, 0.12f, roomDepth + 0.24f),
                    new Color(0.25f, 0.28f, 0.27f));
                CreatePrimitive(PrimitiveType.Cube, "Rooftop_Room_Door",
                    room + new Vector3(0f, -0.18f, -side * (roomDepth * 0.5f + 0.07f)),
                    new Vector3(0.52f, 1.05f, 0.07f), new Color(0.29f, 0.22f, 0.16f));
            }

            if (variation == 1 || variation == 4 || variation == 7)
            {
                Vector3 antenna = roof + new Vector3(-width * 0.28f, 1.05f, -side * depth * 0.22f);
                CreatePrimitive(PrimitiveType.Cylinder, "Rooftop_Antenna_Mast",
                    antenna, new Vector3(0.035f, 1.05f, 0.035f), DarkMetal);
                CreatePrimitive(PrimitiveType.Cube, "Rooftop_Antenna_Crossbar",
                    antenna + Vector3.up * 0.72f,
                    new Vector3(0.9f, 0.045f, 0.045f), DarkMetal);
                CreatePrimitive(PrimitiveType.Sphere, "Rooftop_Satellite_Dish",
                    antenna + new Vector3(0.42f, -0.15f, 0f),
                    new Vector3(0.52f, 0.12f, 0.52f), new Color(0.68f, 0.69f, 0.65f));
            }

            if (variation == 2 || variation == 5 || variation == 8)
            {
                float lineZ = roof.z - side * depth * 0.28f;
                Vector3 lineStart = new Vector3(roof.x - width * 0.28f, roofTop + 1.15f, lineZ);
                Vector3 lineEnd = new Vector3(roof.x + width * 0.28f, roofTop + 1.15f, lineZ);
                CreatePrimitive(PrimitiveType.Cylinder, "Rooftop_Laundry_Line",
                    (lineStart + lineEnd) * 0.5f,
                    new Vector3(0.018f, Vector3.Distance(lineStart, lineEnd) * 0.5f, 0.018f),
                    new Color(0.18f, 0.18f, 0.17f)).transform.rotation =
                    Quaternion.FromToRotation(Vector3.up, (lineEnd - lineStart).normalized);

                Color[] clothColors =
                {
                    new Color(0.72f, 0.22f, 0.18f), new Color(0.82f, 0.76f, 0.61f),
                    new Color(0.18f, 0.31f, 0.48f), new Color(0.48f, 0.55f, 0.37f)
                };
                for (int i = 0; i < 3; i++)
                {
                    Vector3 cloth = Vector3.Lerp(lineStart, lineEnd, (i + 1) / 4f);
                    CreatePrimitive(PrimitiveType.Cube, "Rooftop_Hanging_Cloth",
                        cloth + Vector3.down * 0.32f,
                        new Vector3(0.48f, 0.62f, 0.045f), clothColors[(variation + i) % clothColors.Length]);
                }
            }
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
            GameObject lampHead = CreatePrimitive(PrimitiveType.Cube, "LampHead",
                basePosition + new Vector3(0.68f, 4.55f, 0f),
                new Vector3(0.42f, 0.16f, 0.28f), new Color(0.92f, 0.78f, 0.48f));

            // The old lamp was only a colored prop. Give it a modest warm point
            // light so selected alleys and street corners are actually illuminated.
            // Shadows stay disabled to keep the mobile scene affordable.
            Light streetLight = lampHead.AddComponent<Light>();
            streetLight.type = LightType.Point;
            streetLight.color = new Color(1f, 0.78f, 0.52f);
            streetLight.intensity = 0.85f;
            streetLight.range = 9f;
            streetLight.shadows = LightShadows.None;
            streetLight.renderMode = LightRenderMode.ForceVertex;
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

            // Worn zebra crossings make every internal street junction readable.
            // Each crossing stays on its own road approach and leaves the junction
            // center open for combat movement.
            Color crossingPaint = new Color(0.82f, 0.81f, 0.75f);
            const int crossingStripes = 5;
            const float stripeWidth = 0.42f;
            const float stripeGap = 0.34f;
            float crossingOffset = roadWidth * 0.5f + 1.05f;
            for (int x = 0; x < blocksX - 1; x++)
            {
                float roadX = origin.x + (x + 1) * blockSize + x * roadWidth + roadWidth * 0.5f;
                for (int z = 0; z < blocksZ - 1; z++)
                {
                    float roadZ = origin.z + (z + 1) * blockSize + z * roadWidth + roadWidth * 0.5f;

                    for (int side = -1; side <= 1; side += 2)
                    {
                        float crossingZ = roadZ + side * crossingOffset;
                        for (int stripe = 0; stripe < crossingStripes; stripe++)
                        {
                            float zPosition = crossingZ - (crossingStripes - 1) * (stripeWidth + stripeGap) * 0.5f
                                + stripe * (stripeWidth + stripeGap);
                            CreatePrimitive(PrimitiveType.Cube, "Road_Crosswalk_Stripe",
                                new Vector3(roadX, -0.075f, zPosition),
                                new Vector3(roadWidth - 0.6f, 0.025f, stripeWidth), crossingPaint);
                        }
                    }

                    for (int side = -1; side <= 1; side += 2)
                    {
                        float crossingX = roadX + side * crossingOffset;
                        for (int stripe = 0; stripe < crossingStripes; stripe++)
                        {
                            float xPosition = crossingX - (crossingStripes - 1) * (stripeWidth + stripeGap) * 0.5f
                                + stripe * (stripeWidth + stripeGap);
                            CreatePrimitive(PrimitiveType.Cube, "Road_Crosswalk_Stripe",
                                new Vector3(xPosition, -0.075f, roadZ),
                                new Vector3(stripeWidth, 0.025f, roadWidth - 0.6f), crossingPaint);
                        }
                    }
                }
            }
        }

        private void BuildCentralCourt(Vector3 origin, float width, float depth)
        {
            // The central block is deliberately left clear by BuildBlock; this
            // court becomes a real open landmark instead of clipping through houses.
            Vector3 center = origin + new Vector3(width * 0.5f, 0.08f, depth * 0.5f);
            CreatePrimitive(PrimitiveType.Cube, "CentralCourt",
                center, new Vector3(12f, 0.16f, 9f), new Color(0.18f, 0.34f, 0.24f));

            // The court was only painted ground with no playable landmark at either
            // end. Add two compact futsal goals, including a visible net grid.
            BuildCourtGoals(center);

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

        private void BuildCourtGoals(Vector3 center)
        {
            const float goalWidth = 3.0f;
            const float goalHeight = 1.85f;
            const float post = 0.12f;
            const float goalDepth = 0.85f;
            Color frame = new Color(0.88f, 0.86f, 0.78f);
            Color net = new Color(0.12f, 0.17f, 0.15f);

            for (int end = -1; end <= 1; end += 2)
            {
                float frontX = center.x + end * 5.72f;
                float backX = frontX + end * goalDepth;
                float postZ = goalWidth * 0.5f;

                // Front frame and rear supports.
                for (int side = -1; side <= 1; side += 2)
                {
                    CreatePrimitive(PrimitiveType.Cube, "Court_Goal_Post",
                        new Vector3(frontX, center.y + goalHeight * 0.5f, center.z + side * postZ),
                        new Vector3(post, goalHeight, post), frame);
                    CreatePrimitive(PrimitiveType.Cube, "Court_Goal_Back_Support",
                        new Vector3(backX, center.y + goalHeight * 0.5f, center.z + side * postZ),
                        new Vector3(post, goalHeight, post), frame);
                    CreatePrimitive(PrimitiveType.Cube, "Court_Goal_Side_Rail",
                        new Vector3((frontX + backX) * 0.5f, center.y + goalHeight, center.z + side * postZ),
                        new Vector3(goalDepth, post, post), frame);
                }

                CreatePrimitive(PrimitiveType.Cube, "Court_Goal_Crossbar",
                    new Vector3(frontX, center.y + goalHeight, center.z),
                    new Vector3(post, post, goalWidth + post), frame);
                CreatePrimitive(PrimitiveType.Cube, "Court_Goal_Back_Bar",
                    new Vector3(backX, center.y + goalHeight, center.z),
                    new Vector3(post, post, goalWidth + post), frame);

                // A restrained square net pattern stays readable at gameplay distance.
                float netX = backX - end * 0.06f;
                for (int i = 0; i <= 4; i++)
                {
                    float z = center.z - postZ + i * (goalWidth / 4f);
                    CreatePrimitive(PrimitiveType.Cube, "Court_Goal_Net_Vertical",
                        new Vector3(netX, center.y + goalHeight * 0.5f, z),
                        new Vector3(0.035f, goalHeight, 0.035f), net);
                }
                for (int i = 0; i <= 4; i++)
                {
                    float y = center.y + 0.12f + i * ((goalHeight - 0.12f) / 4f);
                    CreatePrimitive(PrimitiveType.Cube, "Court_Goal_Net_Horizontal",
                        new Vector3(netX, y, center.z),
                        new Vector3(0.035f, 0.035f, goalWidth), net);
                }
            }
        }

        private void CreateSpawns(Vector3 origin, float width, float depth)
        {
            // Five spawn anchors, one for each team in the canonical 5x4 format.
            // The fifth anchor sits along the north edge to keep all starts separated.
            Vector3[] positions =
            {
                origin + new Vector3(3f, 0.05f, 3f),
                origin + new Vector3(width - 3f, 0.05f, 3f),
                origin + new Vector3(3f, 0.05f, depth - 3f),
                origin + new Vector3(width - 3f, 0.05f, depth - 3f),
                origin + new Vector3(width * 0.5f, 0.05f, depth - 3f)
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
                bool horizontalSurface = objectName == "Map_Ground" || objectName == "CentralCourt" ||
                    objectName == "Roof" || objectName == "RoofParapet" ||
                    objectName.StartsWith("Alley_") || objectName.StartsWith("Road_") ||
                    objectName.StartsWith("CourtLine") || objectName == "CourtCenterCircle";
                float textureU = Mathf.Max(1f, scale.x * 0.35f);
                float textureV = Mathf.Max(1f, (horizontalSurface ? scale.z : scale.y) * 0.35f);
                CommunityMaterialLibrary.SetTextureScale(renderer, new Vector2(textureU, textureV));
            }

            generated.Add(item);
            return item;
        }

        private static ProceduralSurfaceTextures.Surface ResolveSurface(string objectName)
        {
            if (objectName == "Map_Ground" || objectName == "RoadDash")
                return ProceduralSurfaceTextures.Surface.Asphalt;
            if (objectName.StartsWith("House_") || objectName.StartsWith("Hillside_House_"))
            {
                int stableHash = 17;
                for (int i = 0; i < objectName.Length; i++)
                    stableHash = stableHash * 31 + objectName[i];
                return ProceduralSurfaceTextures.Surface.ConcreteBlock;
            }
            if (objectName.StartsWith("Vegetation_Tree_Canopy") || objectName.StartsWith("Vegetation_Shrub"))
                return ProceduralSurfaceTextures.Surface.Foliage;
            if (objectName.StartsWith("Vegetation_Tree_Trunk") || objectName == "FrontDoor")
                return ProceduralSurfaceTextures.Surface.Wood;
            if (objectName == "Road_Crosswalk_Stripe" || objectName.StartsWith("CourtLine") || objectName == "CourtCenterCircle")
                return ProceduralSurfaceTextures.Surface.Concrete;
            if (objectName.StartsWith("Alley_Concrete") || objectName.StartsWith("Alley_Low_Curb") || objectName == "DoorStep")
                return ProceduralSurfaceTextures.Surface.Concrete;
            if (objectName == "CentralCourt")
                return ProceduralSurfaceTextures.Surface.Court;
            if (objectName.StartsWith("Court"))
                return ProceduralSurfaceTextures.Surface.Concrete;
            if (objectName == "Roof")
                return ProceduralSurfaceTextures.Surface.Concrete;
            if (objectName == "RoofParapet" || objectName == "AlleyCover")
                return ProceduralSurfaceTextures.Surface.ConcreteBlock;
            if (objectName.Contains("Corrugated") || objectName.Contains("Canopy"))
                return ProceduralSurfaceTextures.Surface.CorrugatedMetal;
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