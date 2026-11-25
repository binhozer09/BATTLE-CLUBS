using System.Collections.Generic;
using UnityEngine;

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
        private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

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
                    Vector3 center = origin + new Vector3(x * cell + blockSize * 0.5f, 0f,
                        z * cell + blockSize * 0.5f);
                    BuildBlock(center, x, z);
                }
            }

            BuildMainStreetDetails(origin, totalX, totalZ);
            BuildCentralCourt(origin, totalX, totalZ);

            if (createSpawnMarkers)
                CreateSpawns(origin, totalX, totalZ);

            Random.state = previousRandomState;
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

            // Low cover at the block ends; the central alley remains open.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 wallPos = center + new Vector3(side * (half - 0.35f), 0.75f, 0f);
                CreatePrimitive(PrimitiveType.Cube, "AlleyCover", wallPos,
                    new Vector3(0.7f, 1.5f, lane * 0.9f), Concrete);
            }

            // A small streetlight at alternating corners adds depth without cluttering every lane.
            if (addStreetLights && (blockX + blockZ) % 2 == 0)
                BuildStreetLight(center + new Vector3(-half + 0.7f, 0f, -half + 0.7f));

            BuildAlleyWires(center, lane, blockX, blockZ);
            BuildStreetClutter(center, lane, blockX, blockZ);
        }

        private void BuildFacade(Vector3 house, float width, float height, float depth,
            int side, int blockX, int blockZ)
        {
            // The front of each row faces the playable alley.
            float frontZ = house.z - side * (depth * 0.5f + 0.055f);
            float windowY = Mathf.Min(height * 0.67f, height - 1.25f);
            float windowSpacing = width * 0.27f;

            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 window = new Vector3(house.x + i * windowSpacing, windowY, frontZ);
                CreatePrimitive(PrimitiveType.Cube, "WindowFrame", window,
                    new Vector3(1.12f, 1.05f, 0.12f), new Color(0.82f, 0.79f, 0.68f));
                CreatePrimitive(PrimitiveType.Cube, "WindowGlass",
                    window + new Vector3(0f, 0f, -side * 0.075f),
                    new Vector3(0.82f, 0.76f, 0.06f), Glass);
                CreatePrimitive(PrimitiveType.Cube, "WindowDivider",
                    window + new Vector3(0f, 0f, -side * 0.12f),
                    new Vector3(0.07f, 0.8f, 0.05f), DarkMetal);
            }

            Vector3 door = new Vector3(house.x, 1.05f, frontZ);
            CreatePrimitive(PrimitiveType.Cube, "FrontDoor", door,
                new Vector3(1.05f, 2.1f, 0.16f), new Color(0.27f, 0.19f, 0.14f));
            CreatePrimitive(PrimitiveType.Sphere, "DoorHandle",
                door + new Vector3(0.34f, 0f, -side * 0.12f),
                new Vector3(0.09f, 0.09f, 0.09f), new Color(0.82f, 0.67f, 0.34f));

            // Simple projecting balcony/awning on some houses.
            if (Random.value > 0.42f && height > 5.2f)
            {
                Vector3 balcony = new Vector3(house.x, windowY - 0.7f, frontZ - side * 0.45f);
                CreatePrimitive(PrimitiveType.Cube, "BalconySlab", balcony,
                    new Vector3(width * 0.62f, 0.16f, 0.95f), Concrete);
                CreatePrimitive(PrimitiveType.Cube, "BalconyRail",
                    balcony + new Vector3(0f, 0.48f, -side * 0.42f),
                    new Vector3(width * 0.6f, 0.08f, 0.08f), DarkMetal);
            }

            // Air-conditioning unit mounted beside a window.
            if (Random.value > 0.55f)
            {
                Vector3 ac = new Vector3(house.x + width * 0.37f, windowY - 0.35f,
                    frontZ - side * 0.16f);
                CreatePrimitive(PrimitiveType.Cube, "AirConditioner", ac,
                    new Vector3(0.65f, 0.42f, 0.34f), new Color(0.72f, 0.73f, 0.70f));
                CreatePrimitive(PrimitiveType.Cube, "ACVent",
                    ac + new Vector3(0f, 0f, -side * 0.19f),
                    new Vector3(0.43f, 0.18f, 0.035f), DarkMetal);
            }
        }

        private void BuildAlleyWires(Vector3 center, float lane, int blockX, int blockZ)
        {
            // Thin overhead utility cables give the alleys a dense community skyline.
            float cableHeight = Random.Range(5.2f, 6.4f);
            for (int i = -1; i <= 1; i++)
            {
                float along = i * blockSize * 0.28f;
                Vector3 left = center + new Vector3(along, cableHeight, -lane * 0.48f);
                Vector3 right = center + new Vector3(along + Random.Range(-0.35f, 0.35f),
                    cableHeight - Random.Range(0.15f, 0.55f), lane * 0.48f);
                BuildCable(left, right, 0.025f, DarkMetal, "UtilityWire");
            }
        }

        private void BuildCable(Vector3 start, Vector3 end, float thickness, Color color, string objectName)
        {
            Vector3 delta = end - start;
            GameObject cable = CreatePrimitive(PrimitiveType.Cylinder, objectName,
                (start + end) * 0.5f, new Vector3(thickness, delta.magnitude * 0.5f, thickness), color);
            cable.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        }

        private void BuildStreetClutter(Vector3 center, float lane, int blockX, int blockZ)
        {
            // Keep props against the alley edges so they do not obstruct player movement.
            if ((blockX + blockZ) % 2 == 0)
            {
                Vector3 bin = center + new Vector3(-blockSize * 0.34f, 0.62f, -lane * 0.36f);
                CreatePrimitive(PrimitiveType.Cube, "StreetTrashBin", bin,
                    new Vector3(0.72f, 1.18f, 0.72f), new Color(0.16f, 0.22f, 0.18f));
                CreatePrimitive(PrimitiveType.Cube, "TrashBinLid",
                    bin + Vector3.up * 0.62f, new Vector3(0.78f, 0.1f, 0.78f), DarkMetal);
            }

            if ((blockX * 3 + blockZ) % 3 == 0)
            {
                Vector3 sack = center + new Vector3(blockSize * 0.34f, 0.28f, lane * 0.34f);
                GameObject bag = CreatePrimitive(PrimitiveType.Sphere, "TiedTrashSack", sack,
                    new Vector3(0.62f, 0.52f, 0.58f), new Color(0.075f, 0.085f, 0.08f));
                bag.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(-12f, 12f));
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
            // Spawn in the open central alley of each selected block, not at the map
            // corners where the procedural houses occupy the ground.
            int lastX = Mathf.Max(0, blocksX - 1);
            int lastZ = Mathf.Max(0, blocksZ - 1);
            int middleX = blocksX / 2;
            int middleZ = blocksZ / 2;
            int[] blockXs = { 0, lastX, 0, lastX, middleX };
            int[] blockZs = { 0, 0, lastZ, lastZ, middleZ };

            for (int i = 0; i < 5; i++)
            {
                float x = origin.x + blockXs[i] * (blockSize + roadWidth) + blockSize * 0.5f;
                float z = origin.z + blockZs[i] * (blockSize + roadWidth) + blockSize * 0.5f;
                GameObject marker = new GameObject("TeamSpawn_" + (i + 1));
                marker.transform.SetParent(transform, false);
                marker.transform.position = new Vector3(x, origin.y - 0.15f, z);
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
                renderer.sharedMaterial = GetSurfaceMaterial(objectName, color);

            generated.Add(item);
            return item;
        }


        private Material GetSurfaceMaterial(string objectName, Color tint)
        {
            string surface = GetSurfaceType(objectName);
            string key = surface + "_" + ColorUtility.ToHtmlStringRGBA(tint);
            if (materialCache.TryGetValue(key, out Material cached)) return cached;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;

            Material material = new Material(shader);
            material.name = "BC_" + key;
            material.color = tint;
            material.mainTexture = GetSurfaceTexture(surface);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", surface == "metal" ? 0.48f : surface == "glass" ? 0.72f : 0.12f);
            materialCache.Add(key, material);
            return material;
        }

        private string GetSurfaceType(string objectName)
        {
            string n = objectName.ToLowerInvariant();
            if (n.Contains("glass") || n.Contains("window")) return "glass";
            if (n.Contains("metal") || n.Contains("tank") || n.Contains("lamp") || n.Contains("rail") || n.Contains("handle") || n.Contains("vent")) return "metal";
            if (n.Contains("door")) return "wood";
            if (n.Contains("ground") || n.Contains("dash") || n.Contains("court")) return "asphalt";
            if (n.Contains("roof") || n.Contains("cover") || n.Contains("slab") || n.Contains("line") || n.Contains("sidewalk")) return "concrete";
            if (n.Contains("house")) return "brick";
            if (n.Contains("windowframe") || n.Contains("balcony") || n.Contains("airconditioner") || n.Contains("ac")) return "plaster";
            return "concrete";
        }

        private Texture2D GetSurfaceTexture(string surface)
        {
            if (textureCache.TryGetValue(surface, out Texture2D cached)) return cached;
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            texture.name = "BC_Tile_" + surface;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n1 = Mathf.PerlinNoise(x * 0.105f + 13.7f, y * 0.105f + 4.2f);
                    float n2 = Mathf.PerlinNoise(x * 0.027f + 31.1f, y * 0.027f + 8.9f);
                    float grain = (n1 - 0.5f) * 0.22f + (n2 - 0.5f) * 0.25f;
                    Color pixel;
                    if (surface == "asphalt")
                    {
                        float grit = Mathf.PerlinNoise(x * 0.42f + 2f, y * 0.42f + 9f);
                        float v = Mathf.Clamp01(0.43f + grain + (grit - 0.5f) * 0.28f);
                        pixel = new Color(v, v, v * 0.96f);
                    }
                    else if (surface == "brick")
                    {
                        // Repeating staggered masonry with recessed mortar and subtle weathering.
                        float row = y % 24;
                        int rowOffset = (y / 24) % 2 == 0 ? 0 : 16;
                        float col = (x + rowOffset) % 32;
                        bool mortar = row < 2 || col < 2;
                        float wear = Mathf.PerlinNoise(x * 0.075f + 5f, y * 0.075f + 19f);
                        float v = Mathf.Clamp01(0.72f + grain - (1f - wear) * 0.24f);
                        pixel = mortar ? new Color(0.34f, 0.32f, 0.29f) : new Color(v, v * 0.53f, v * 0.39f);
                    }
                    else if (surface == "concrete" || surface == "plaster")
                    {
                        float stain = Mathf.PerlinNoise(x * 0.055f + 7f, y * 0.055f + 17f);
                        float v = Mathf.Clamp01((surface == "plaster" ? 0.68f : 0.57f) + grain - (1f - stain) * (surface == "plaster" ? 0.24f : 0.17f));
                        pixel = new Color(v, v * 0.99f, v * 0.94f);
                    }
                    else if (surface == "metal")
                    {
                        float streak = Mathf.PerlinNoise(x * 0.035f, y * 0.24f);
                        float v = Mathf.Clamp01(0.48f + grain + (streak - 0.5f) * 0.18f);
                        pixel = new Color(v, v, v * 0.97f);
                    }
                    else if (surface == "wood")
                    {
                        float lines = Mathf.PerlinNoise(x * 0.045f, y * 0.22f);
                        float v = Mathf.Clamp01(0.52f + grain + (lines - 0.5f) * 0.3f);
                        pixel = new Color(v, v * 0.76f, v * 0.58f);
                    }
                    else
                    {
                        float reflection = Mathf.PerlinNoise(x * 0.025f, y * 0.12f);
                        float v = Mathf.Clamp01(0.25f + reflection * 0.22f);
                        pixel = new Color(v * 0.62f, v * 0.86f, v);
                    }
                    pixels[y * size + x] = pixel;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            textureCache.Add(surface, texture);
            return texture;
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
        }
    }
}