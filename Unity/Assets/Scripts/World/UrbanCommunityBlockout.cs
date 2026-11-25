using System.Collections.Generic;
using UnityEngine;

namespace BattleClubs.World
{
    /// <summary>
    /// Generates a stylized Brazilian hillside community map for the first playable
    /// visual pass. All geometry is procedural placeholder art and can be replaced
    /// by optimized, licensed models without changing gameplay scripts.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
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
        [SerializeField] private Texture2D asphaltDamageTexture;
        [SerializeField] private Texture2D roadSurfaceTexture;
        [SerializeField] private bool addStreetLights = true;
        [SerializeField] private bool addRooftopTanks = true;
        [SerializeField] private bool addCourtMarkings = true;

        private readonly List<GameObject> generated = new List<GameObject>();
        private Material generatedDamageMaterial;

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

            GameObject roadGround = CreatePrimitive(PrimitiveType.Cube, "Map_Ground",
                origin + new Vector3(totalX * 0.5f, -0.65f, totalZ * 0.5f),
                new Vector3(totalX + roadWidth, 1f, totalZ + roadWidth), Asphalt);
            if (roadSurfaceTexture != null)
            {
                Renderer roadRenderer = roadGround.GetComponent<Renderer>();
                if (roadRenderer != null && roadRenderer.sharedMaterial != null)
                {
                    Material roadMaterial = roadRenderer.sharedMaterial;
                    if (roadMaterial.HasProperty("_MainTex")) roadMaterial.SetTexture("_MainTex", roadSurfaceTexture);
                    if (roadMaterial.HasProperty("_BaseMap")) roadMaterial.SetTexture("_BaseMap", roadSurfaceTexture);
                    if (roadMaterial.HasProperty("_Color")) roadMaterial.SetColor("_Color", Color.white);
                    if (roadMaterial.HasProperty("_BaseColor")) roadMaterial.SetColor("_BaseColor", Color.white);
                    roadMaterial.mainTextureScale = new Vector2(Mathf.Max(1f, totalX / 8f), Mathf.Max(1f, totalZ / 8f));
                }
            }

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
            BuildStreetDamage(origin, totalX, totalZ);

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

        private void BuildStreetDamage(Vector3 origin, float width, float depth)
        {
            if (asphaltDamageTexture == null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;

            generatedDamageMaterial = new Material(shader);
            if (generatedDamageMaterial.HasProperty("_MainTex"))
                generatedDamageMaterial.SetTexture("_MainTex", asphaltDamageTexture);
            if (generatedDamageMaterial.HasProperty("_BaseMap"))
                generatedDamageMaterial.SetTexture("_BaseMap", asphaltDamageTexture);
            if (generatedDamageMaterial.HasProperty("_Color"))
                generatedDamageMaterial.SetColor("_Color", Color.white);
            if (generatedDamageMaterial.HasProperty("_BaseColor"))
                generatedDamageMaterial.SetColor("_BaseColor", Color.white);

            // Transparent surface; PNG alpha is built from ambientCG's opacity map.
            if (shader.name == "Universal Render Pipeline/Unlit")
            {
                generatedDamageMaterial.SetFloat("_Surface", 1f);
                generatedDamageMaterial.SetFloat("_Blend", 0f);
                generatedDamageMaterial.SetFloat("_ZWrite", 0f);
                generatedDamageMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                generatedDamageMaterial.renderQueue = 3000;
            }
            else if (shader.name == "Standard")
            {
                generatedDamageMaterial.SetFloat("_Mode", 3f);
                generatedDamageMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                generatedDamageMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                generatedDamageMaterial.SetInt("_ZWrite", 0);
                generatedDamageMaterial.DisableKeyword("_ALPHATEST_ON");
                generatedDamageMaterial.EnableKeyword("_ALPHABLEND_ON");
                generatedDamageMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                generatedDamageMaterial.renderQueue = 3000;
            }

            float groundTop = -0.135f;
            float patchSize = 1.2f;
            float spacing = 4.8f;
            float cell = blockSize + roadWidth;

            // Main north-south streets between every block column.
            for (int x = 0; x < blocksX - 1; x++)
            {
                float roadX = origin.x + (x + 1) * blockSize + x * roadWidth + roadWidth * 0.5f;
                for (float z = origin.z + 2f; z < origin.z + depth - 1f; z += spacing)
                    AddStreetDamagePatch(new Vector3(roadX + Random.Range(-1.5f, 1.5f), groundTop, z), patchSize);
            }

            // Main east-west streets between every block row.
            for (int z = 0; z < blocksZ - 1; z++)
            {
                float roadZ = origin.z + (z + 1) * blockSize + z * roadWidth + roadWidth * 0.5f;
                for (float x = origin.x + 2f; x < origin.x + width - 1f; x += spacing)
                    AddStreetDamagePatch(new Vector3(x, groundTop, roadZ + Random.Range(-1.5f, 1.5f)), patchSize);
            }

            // Narrow alleys running between the two house rows inside each block.
            for (int x = 0; x < blocksX; x++)
            {
                for (int z = 0; z < blocksZ; z++)
                {
                    float alleyZ = origin.z + z * cell + blockSize * 0.5f;
                    for (float along = 2f; along < blockSize - 1f; along += spacing)
                    {
                        float alleyX = origin.x + x * cell + along;
                        AddStreetDamagePatch(new Vector3(alleyX, groundTop, alleyZ), patchSize);
                    }
                }
            }
        }

        private void AddStreetDamagePatch(Vector3 position, float size)
        {
            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Quad);
            patch.name = "AsphaltDamage001_Decal";
            patch.transform.SetParent(transform, true);
            patch.transform.position = position;
            patch.transform.rotation = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);
            patch.transform.localScale = new Vector3(size, size, 1f);

            Collider patchCollider = patch.GetComponent<Collider>();
            if (patchCollider != null)
            {
                if (Application.isPlaying) Destroy(patchCollider);
                else DestroyImmediate(patchCollider);
            }

            Renderer patchRenderer = patch.GetComponent<Renderer>();
            patchRenderer.sharedMaterial = generatedDamageMaterial;
            patchRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            patchRenderer.receiveShadows = false;
            generated.Add(patch);
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
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader != null)
                {
                    Material material = new Material(shader);
                    material.color = color;
                    renderer.sharedMaterial = material;
                }
            }

            generated.Add(item);
            return item;
        }

        private void ClearGenerated()
        {
            if (generatedDamageMaterial != null)
            {
                if (Application.isPlaying) Destroy(generatedDamageMaterial);
                else DestroyImmediate(generatedDamageMaterial);
                generatedDamageMaterial = null;
            }
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