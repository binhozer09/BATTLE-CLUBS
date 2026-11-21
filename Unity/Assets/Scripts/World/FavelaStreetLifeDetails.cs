using System.Collections.Generic;
using UnityEngine;

namespace BattleClubs.World
{
    /// <summary>
    /// Adds small lived-in details that make the hillside community read as a
    /// Brazilian favela rather than a grid of bare blockout buildings.
    /// </summary>
    public sealed class FavelaStreetLifeDetails : MonoBehaviour
    {
        [SerializeField, Min(1)] private int blocksX = 5;
        [SerializeField, Min(1)] private int blocksZ = 5;
        [SerializeField, Min(8f)] private float blockSize = 18f;
        [SerializeField, Min(3f)] private float roadWidth = 7f;
        [SerializeField, Min(2f)] private float alleyWidth = 5f;
        [SerializeField] private int randomSeed = 24061;

        private readonly List<GameObject> generated = new List<GameObject>();

        private static readonly Color[] LaundryColors =
        {
            new Color(0.82f, 0.78f, 0.66f),
            new Color(0.22f, 0.38f, 0.62f),
            new Color(0.68f, 0.25f, 0.20f),
            new Color(0.78f, 0.68f, 0.34f),
            new Color(0.28f, 0.42f, 0.31f)
        };

        private void Start() => Generate();

        [ContextMenu("Generate Favela Street Life Details")]
        public void Generate()
        {
            ClearGenerated();
            Random.State previous = Random.state;
            Random.InitState(randomSeed);

            float cell = blockSize + roadWidth;
            Vector3 origin = transform.position;

            for (int x = 0; x < blocksX; x++)
            for (int z = 0; z < blocksZ; z++)
            {
                Vector3 center = origin + new Vector3(
                    x * cell + blockSize * 0.5f, 0f,
                    z * cell + blockSize * 0.5f);

                if ((x + z) % 2 == 0)
                    BuildLaundryLine(center);

                if ((x * 3 + z) % 3 == 0)
                    BuildFacadeShopfront(center, (x + z) % 2 == 0 ? -1 : 1);

                if ((x + z) % 3 == 1)
                    BuildRooftopSatellite(center);
            }

            Random.state = previous;
        }

        private void BuildLaundryLine(Vector3 center)
        {
            float halfSpan = alleyWidth * 0.42f;
            Vector3 start = center + new Vector3(-0.1f, 5.35f, -halfSpan);
            Vector3 end = center + new Vector3(0.1f, 5.15f, halfSpan);
            CreateCylinderBetween("Laundry_Line", start, end, 0.018f,
                new Color(0.12f, 0.12f, 0.11f));

            int count = 4;
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                Vector3 point = Vector3.Lerp(start, end, t);
                float width = Random.Range(0.42f, 0.62f);
                float height = Random.Range(0.55f, 0.82f);
                Color fabric = LaundryColors[Random.Range(0, LaundryColors.Length)];

                CreateCube("Hanging_Laundry", point + Vector3.down * (height * 0.5f),
                    new Vector3(width, height, 0.045f), fabric,
                    Random.Range(-7f, 7f));
                CreateCube("Laundry_Clothespin", point + Vector3.down * 0.04f,
                    new Vector3(0.055f, 0.16f, 0.07f),
                    new Color(0.72f, 0.61f, 0.38f));
            }
        }

        private void BuildFacadeShopfront(Vector3 center, int side)
        {
            // A shutter, sign and simple awning mark selected ground-floor homes as local shops.
            float faceZ = center.z - side * (alleyWidth * 0.5f + 0.12f);
            Vector3 facade = new Vector3(center.x, 1.55f, faceZ);

            CreateCube("Local_Shop_Rolling_Shutter", facade,
                new Vector3(2.65f, 2.25f, 0.12f), new Color(0.31f, 0.33f, 0.32f));
            for (int i = 0; i < 7; i++)
            {
                float y = 0.65f + i * 0.25f;
                CreateCube("Shutter_Slat",
                    new Vector3(center.x, y, faceZ - side * 0.075f),
                    new Vector3(2.5f, 0.035f, 0.025f),
                    new Color(0.19f, 0.21f, 0.21f));
            }

            Color signColor = new Color(0.86f, 0.72f, 0.38f);
            CreateCube("Small_Mercadinho_Sign",
                new Vector3(center.x, 2.95f, faceZ - side * 0.12f),
                new Vector3(2.9f, 0.48f, 0.12f), signColor);
            CreateCube("Shop_Awning",
                new Vector3(center.x, 2.55f, faceZ - side * 0.55f),
                new Vector3(3.15f, 0.12f, 0.9f),
                new Color(0.57f, 0.22f, 0.17f), side * 5f);
        }

        private void BuildRooftopSatellite(Vector3 center)
        {
            // The dish is intentionally low-profile so it does not dominate the skyline.
            Vector3 roof = center + new Vector3(
                Random.Range(-5.2f, 5.2f), 8.6f,
                Random.Range(-5.5f, 5.5f));

            CreateCube("Satellite_Dish_Mast", roof + Vector3.up * 0.45f,
                new Vector3(0.07f, 0.9f, 0.07f),
                new Color(0.18f, 0.19f, 0.19f));

            GameObject dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dish.name = "Rooftop_Satellite_Dish";
            dish.transform.SetParent(transform, true);
            dish.transform.position = roof + Vector3.up * 0.9f;
            dish.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            dish.transform.localScale = new Vector3(0.9f, 0.075f, 0.9f);
            SetColor(dish, new Color(0.48f, 0.5f, 0.49f));
            generated.Add(dish);
        }

        private void CreateCylinderBetween(string objectName, Vector3 start, Vector3 end,
            float radius, Color color)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            item.name = objectName;
            item.transform.SetParent(transform, true);
            Vector3 delta = end - start;
            item.transform.position = (start + end) * 0.5f;
            item.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            item.transform.localScale = new Vector3(radius * 2f, delta.magnitude * 0.5f, radius * 2f);
            SetColor(item, color);
            generated.Add(item);
        }

        private void CreateCube(string objectName, Vector3 position, Vector3 scale,
            Color color, float yaw = 0f)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = objectName;
            item.transform.SetParent(transform, true);
            item.transform.position = position;
            item.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            item.transform.localScale = scale;
            SetColor(item, color);
            generated.Add(item);
        }

        private static void SetColor(GameObject item, Color color)
        {
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;
            Material material = new Material(shader);
            material.color = color;
            renderer.sharedMaterial = material;
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