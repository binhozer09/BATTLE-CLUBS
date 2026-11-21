using System.Collections.Generic;
using UnityEngine;

namespace BattleClubs.World
{
    /// <summary>
    /// Creates a playable greybox community map from Unity primitives.
    /// Use this as a layout prototype; replace primitives with licensed art later.
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

        private readonly List<GameObject> generated = new List<GameObject>();

        private void Start()
        {
            if (generateOnStart) Generate();
        }

        [ContextMenu("Generate Community Blockout")]
        public void Generate()
        {
            ClearGenerated();
            Random.InitState(randomSeed);

            float cell = blockSize + roadWidth;
            float totalX = blocksX * blockSize + (blocksX - 1) * roadWidth;
            float totalZ = blocksZ * blockSize + (blocksZ - 1) * roadWidth;
            Vector3 origin = transform.position;

            CreatePrimitive(PrimitiveType.Cube, "Ground", origin + new Vector3(totalX * 0.5f, -0.6f, totalZ * 0.5f),
                new Vector3(totalX + roadWidth, 1f, totalZ + roadWidth), new Color(0.23f, 0.25f, 0.24f));

            for (int x = 0; x < blocksX; x++)
            {
                for (int z = 0; z < blocksZ; z++)
                {
                    Vector3 center = origin + new Vector3(x * cell + blockSize * 0.5f, 0f, z * cell + blockSize * 0.5f);
                    BuildBlock(center, x, z);
                }
            }

            if (createSpawnMarkers)
                CreateSpawns(origin, totalX, totalZ);

            CreatePrimitive(PrimitiveType.Cube, "Central Court", origin + new Vector3(totalX * 0.5f, 0.08f, totalZ * 0.5f),
                new Vector3(12f, 0.15f, 9f), new Color(0.18f, 0.35f, 0.22f));
        }

        private void BuildBlock(Vector3 center, int blockX, int blockZ)
        {
            float half = blockSize * 0.5f;
            float lane = Mathf.Clamp(alleyWidth, 2f, blockSize * 0.35f);
            float buildingDepth = (blockSize - lane) * 0.5f;
            float sideOffset = (lane + buildingDepth) * 0.5f;

            // Two rows of small houses leave a traversable alley through each block.
            for (int side = -1; side <= 1; side += 2)
            {
                float height = Random.Range(minBuildingHeight, Mathf.Max(minBuildingHeight + 0.1f, maxBuildingHeight));
                float width = Random.Range(blockSize * 0.34f, blockSize * 0.47f);
                float depth = buildingDepth * Random.Range(0.72f, 0.96f);
                float xOffset = Random.Range(-half * 0.12f, half * 0.12f);
                Vector3 position = center + new Vector3(xOffset, height * 0.5f, side * sideOffset);
                Color wall = Color.Lerp(new Color(0.58f, 0.48f, 0.36f), new Color(0.72f, 0.68f, 0.55f), Random.value);
                CreatePrimitive(PrimitiveType.Cube, "House_" + blockX + "_" + blockZ + "_" + side,
                    position, new Vector3(width, height, depth), wall);

                // Flat rooftop access/cover detail.
                CreatePrimitive(PrimitiveType.Cube, "Roof_" + blockX + "_" + blockZ + "_" + side,
                    position + Vector3.up * (height * 0.5f + 0.12f),
                    new Vector3(width + 0.25f, 0.24f, depth + 0.25f), new Color(0.25f, 0.25f, 0.24f));
            }

            // Short wall segments make corners and cover without sealing the alley.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 wallPos = center + new Vector3(side * (half - 0.35f), 0.75f, 0f);
                CreatePrimitive(PrimitiveType.Cube, "CoverWall", wallPos,
                    new Vector3(0.7f, 1.5f, lane * 0.9f), new Color(0.38f, 0.39f, 0.36f));
            }
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
                Material material = new Material(Shader.Find("Standard"));
                material.color = color;
                renderer.sharedMaterial = material;
            }

            generated.Add(item);
            return item;
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