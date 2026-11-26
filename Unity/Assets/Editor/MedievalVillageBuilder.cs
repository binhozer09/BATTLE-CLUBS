#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleClubs.Editor
{
    /// <summary>
    /// Builds a separate medieval village scene from the modular FBX kit.
    /// Keeps the existing player, camera, HUD and match setup by copying those
    /// scene roots from BattleClubs_Main; the old map is not modified.
    /// </summary>
    public static class MedievalVillageBuilder
    {
        private const string MainScene = "Assets/Scenes/BattleClubs_Main.unity";
        private const string OutputScene = "Assets/Scenes/BattleClubs_MedievalVillage.unity";
        private const string ModelFolder = "Assets/MedievalVillage/FBX";
        private const string TextureFolder = "Assets/MedievalVillage/Textures";

        private static readonly Dictionary<string, GameObject> Models = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private static int placedModels;
        private static int missingModels;
        private static Transform placementParent;
        private const string GeneratedMaterialFolder = "Assets/MedievalVillage/GeneratedMaterials";

        [MenuItem("BATTLE CLUBS/World/Build Medieval Village Scene")]
        public static void Build()
        {
            placedModels = 0;
            missingModels = 0;
            if (!AssetDatabase.IsValidFolder(ModelFolder))
                throw new Exception("Pasta FBX não encontrada: " + ModelFolder + ". Importe primeiro os modelos do Dropbox.");

            // A cena principal é gerada pelo próprio projeto. Criá-la aqui, quando
            // ainda não existe, evita exigir uma etapa manual e não altera o mapa-fonte.
            if (!File.Exists(MainScene))
                BattleClubsFullSceneBuilder.CreateMainGameScene();

            LoadModels();
            if (Models.Count < 150)
                throw new Exception("Foram encontrados apenas " + Models.Count + " modelos FBX; eram esperados 176. Confira a importação antes de gerar a cena.");

            EnsureGeneratedMaterialFolder();
            Scene target = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CopyPlayableRootsFromMainScene(target);
            SceneManager.SetActiveScene(target);
            ConfigureLighting();
            CreateGroundAndRoads();
            CreateVillageHouses();
            CreateCentralPlaza();
            CreateVillageDetails();
            EditorSceneManager.SaveScene(target, OutputScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MEDIEVAL_VILLAGE_SCENE_CREATED=" + OutputScene +
                " | modelos colocados=" + placedModels + " | modelos ausentes=" + missingModels);
        }

        private static void CopyPlayableRootsFromMainScene(Scene target)
        {
            if (!System.IO.File.Exists(MainScene))
                throw new Exception("Cena principal não encontrada: " + MainScene);

            Scene source = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Additive);
            GameObject[] roots = source.GetRootGameObjects();
            int moved = 0;
            foreach (GameObject root in roots)
            {
                if (root == null || root.name == "BATTLE_CLUBS_WORLD")
                    continue;
                SceneManager.MoveGameObjectToScene(root, target);
                moved++;
            }
            EditorSceneManager.CloseScene(source, true);
            if (moved == 0)
                throw new Exception("Não encontrei Player/Camera na cena principal para preservar.");
        }

        private static void LoadModels()
        {
            Models.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ModelFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model != null) Models[model.name] = model;
            }
        }

        private static GameObject FindModel(params string[] tokens)
        {
            foreach (string token in tokens)
                foreach (KeyValuePair<string, GameObject> entry in Models)
                    if (entry.Key.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                        return entry.Value;
            return null;
        }

        private static GameObject Place(string label, Vector3 center, Vector3 size, float yaw, params string[] modelTokens)
        {
            GameObject source = FindModel(modelTokens);
            if (source == null)
            {
                missingModels++;
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null) instance = UnityEngine.Object.Instantiate(source);
            if (placementParent != null) instance.transform.SetParent(placementParent, true);
            instance.name = label;
            instance.transform.position = center;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Bounds bounds;
            if (!TryGetBounds(instance, out bounds))
            {
                UnityEngine.Object.DestroyImmediate(instance);
                missingModels++;
                return null;
            }

            Vector3 current = bounds.size;
            Vector3 scale = new Vector3(
                size.x / Mathf.Max(0.01f, current.x),
                size.y / Mathf.Max(0.01f, current.y),
                size.z / Mathf.Max(0.01f, current.z));
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scale);
            if (TryGetBounds(instance, out bounds))
                instance.transform.position += center - bounds.center;

            ApplyKitMaterial(instance, label);
            AddStaticMeshColliders(instance);
            placedModels++;
            return instance;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private static void AddStaticMeshColliders(GameObject root)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter filter in filters)
            {
                if (filter == null || filter.sharedMesh == null || filter.GetComponent<Collider>() != null)
                    continue;
                MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = false;
            }
        }

        private static void ApplyKitMaterial(GameObject root, string hint)
        {
            string key = hint.IndexOf("roof", StringComparison.OrdinalIgnoreCase) >= 0 ? "roof" :
                hint.IndexOf("wood", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hint.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0 ? "wood" :
                hint.IndexOf("plaster", StringComparison.OrdinalIgnoreCase) >= 0 ? "plaster" : "brick";
            Material material = GetMaterial(key);
            if (material == null) return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer != null) renderer.sharedMaterial = material;
        }

        private static Material GetMaterial(string key)
        {
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            string baseName = key == "roof" ? "T_RoundTiles_BaseColor" :
                key == "wood" ? "T_WoodTrim_BaseColor" :
                key == "plaster" ? "T_Plaster_BaseColor" : "T_UnevenBrick_BaseColor";
            string normalName = key == "roof" ? "T_RoundTiles_Normal" :
                key == "wood" ? "T_WoodTrim_Normal" :
                key == "plaster" ? "T_Plaster_Normal" : "T_UnevenBrick_Normal";
            Texture2D texture = FindTexture(baseName);
            if (texture == null && key == "brick") texture = FindTexture("T_Brick_BaseColor");
            Texture2D normal = FindTexture(normalName);
            if (normal == null && key == "brick") normal = FindTexture("T_Brick_Normal");

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;

            string materialPath = GeneratedMaterialFolder + "/MV_" + key + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "MV_" + key };
                if (texture != null)
                {
                    if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                    if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                    material.SetTextureScale(material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex", new Vector2(2f, 2f));
                }
                if (normal != null)
                {
                    EnsureNormalMap(normal);
                    normal = FindTexture(normalName);
                    if (material.HasProperty("_BumpMap"))
                    {
                        material.SetTexture("_BumpMap", normal);
                        material.EnableKeyword("_NORMALMAP");
                    }
                }
                AssetDatabase.CreateAsset(material, materialPath);
            }
            Materials[key] = material;
            return material;
        }

        private static Texture2D FindTexture(string token)
        {
            string[] guids = AssetDatabase.FindAssets(token + " t:Texture2D", new[] { TextureFolder });
            return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void EnsureNormalMap(Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureGeneratedMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/MedievalVillage"))
                AssetDatabase.CreateFolder("Assets", "MedievalVillage");
            if (!AssetDatabase.IsValidFolder(GeneratedMaterialFolder))
                AssetDatabase.CreateFolder("Assets/MedievalVillage", "GeneratedMaterials");
        }

        private static void CreateGroundAndRoads()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "MedievalVillage_Ground_Collision";
            ground.transform.position = new Vector3(0f, -0.35f, 0f);
            ground.transform.localScale = new Vector3(132f, 0.7f, 132f);
            SetColor(ground, new Color(0.28f, 0.30f, 0.22f));

            CreateRoad(new Vector3(0f, 0.015f, 0f), new Vector3(11f, 0.08f, 116f));
            CreateRoad(new Vector3(0f, 0.02f, 0f), new Vector3(116f, 0.08f, 11f));
            for (int i = -2; i <= 2; i++)
            {
                CreateRoad(new Vector3(i * 22f, 0.015f, 0f), new Vector3(4.2f, 0.08f, 116f));
                CreateRoad(new Vector3(0f, 0.015f, i * 22f), new Vector3(116f, 0.08f, 4.2f));
            }
        }

        private static void CreateRoad(Vector3 position, Vector3 scale)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "Village_Cobblestone_Road";
            road.transform.position = position;
            road.transform.localScale = scale;
            SetColor(road, new Color(0.36f, 0.34f, 0.29f));
        }

        private static void CreateVillageHouses()
        {
            int id = 0;
            for (int x = -2; x <= 2; x++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    if (Mathf.Abs(x) <= 1 && Mathf.Abs(z) <= 1) continue;
                    Vector3 center = new Vector3(x * 22f + (z % 2 == 0 ? 3f : -3f), 0f, z * 22f);
                    BuildHouse("House_" + (++id).ToString("00"), center, (id % 3) * 90f, id);
                }
            }

            BuildHouse("House_Plaza_West", new Vector3(-20f, 0f, 0f), 90f, 31);
            BuildHouse("House_Plaza_East", new Vector3(20f, 0f, 0f), 270f, 32);
            BuildHouse("House_Plaza_North", new Vector3(0f, 0f, 20f), 180f, 33);
            BuildHouse("House_Plaza_South", new Vector3(0f, 0f, -20f), 0f, 34);
        }

        private static void BuildHouse(string name, Vector3 center, float yaw, int variation)
        {
            float w = 8.5f + (variation % 3) * 0.4f;
            float h = 4.2f + (variation % 2) * 0.5f;
            float d = 8f;
            GameObject root = new GameObject(name);
            Transform previousParent = placementParent;
            placementParent = root.transform;

            Place(name + "_Floor", center + Vector3.up * 0.08f, new Vector3(w, 0.16f, d),
                yaw, "Floor_UnevenBrick", "Floor_Brick", "Floor_RedBrick");
            Place(name + "_Front", center + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, h * 0.5f, -d * 0.5f),
                new Vector3(w, h, 0.35f), yaw, "Wall_UnevenBrick_Door_Flat", "Wall_Plaster_Door_Flat", "Wall_UnevenBrick_Straight");
            Place(name + "_Back", center + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, h * 0.5f, d * 0.5f),
                new Vector3(w, h, 0.35f), yaw + 180f, "Wall_UnevenBrick_Window_Wide_Flat", "Wall_Plaster_Window_Wide_Flat", "Wall_Plaster_Straight");
            Place(name + "_Left", center + Quaternion.Euler(0f, yaw, 0f) * new Vector3(-w * 0.5f, h * 0.5f, 0f),
                new Vector3(d, h, 0.35f), yaw + 90f, "Wall_UnevenBrick_Straight", "Wall_Plaster_Straight");
            Place(name + "_Right", center + Quaternion.Euler(0f, yaw, 0f) * new Vector3(w * 0.5f, h * 0.5f, 0f),
                new Vector3(d, h, 0.35f), yaw - 90f, "Wall_Plaster_Window_Thin_Round", "Wall_Plaster_Straight");
            Place(name + "_Roof", center + Vector3.up * (h + 0.95f),
                new Vector3(w + 1.1f, 1.9f, d + 1.1f), yaw, "Roof_RoundTiles_6x8", "Roof_RoundTiles_6x6", "Roof_Modular_RoundTiles");
            if (variation % 2 == 0)
                Place(name + "_Chimney", center + Vector3.up * (h + 2.2f) + Vector3.right * 2.2f,
                    new Vector3(0.8f, 1.8f, 0.8f), yaw, "Prop_Chimney", "Prop_Chimney2");
            Place(name + "_Steps", center + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.25f, -d * 0.58f),
                new Vector3(2.2f, 0.5f, 1.2f), yaw, "Stairs_Exterior_Straight", "Stairs_Exterior_Platform");

            placementParent = previousParent;
        }

        private static void CreateCentralPlaza()
        {
            GameObject plaza = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plaza.name = "Village_Central_Market_Piazza";
            plaza.transform.position = new Vector3(0f, 0.035f, 0f);
            plaza.transform.localScale = new Vector3(26f, 0.12f, 26f);
            SetColor(plaza, new Color(0.48f, 0.43f, 0.34f));

            Place("Market_Wagon", new Vector3(-5f, 0.8f, 3f), new Vector3(3.8f, 2.2f, 2.2f), 35f, "Prop_Wagon");
            Place("Market_Crate_01", new Vector3(4f, 0.45f, -3f), new Vector3(1f, 0.9f, 1f), 15f, "Prop_Crate");
            Place("Market_Crate_02", new Vector3(5.2f, 0.45f, -3.1f), new Vector3(1f, 0.9f, 1f), -8f, "Prop_Crate");
            Place("Village_Gate_Arch", new Vector3(0f, 2.4f, -55f), new Vector3(9f, 5f, 1.3f), 0f, "Wall_Arch");
            Place("Village_Tower_Roof", new Vector3(0f, 9f, 52f), new Vector3(9f, 4f, 9f), 0f, "Roof_Tower_RoundTiles");
        }

        private static void CreateVillageDetails()
        {
            System.Random random = new System.Random(2026);
            for (int i = 0; i < 24; i++)
            {
                float x = (float)(random.NextDouble() * 104.0 - 52.0);
                float z = (float)(random.NextDouble() * 104.0 - 52.0);
                if (Mathf.Abs(x) < 16f && Mathf.Abs(z) < 16f) continue;
                Place("Village_Fence_" + i, new Vector3(x, 0.55f, z),
                    new Vector3(2.8f, 1.1f, 0.18f), (float)random.Next(0, 4) * 90f, "Prop_WoodenFence_Single", "Prop_MetalFence_Simple");
                if (i % 3 == 0)
                    Place("Village_Vine_" + i, new Vector3(x + 1.5f, 1.4f, z),
                        new Vector3(1.5f, 2.4f, 0.25f), 0f, "Prop_Vine1", "Prop_Vine2", "Prop_Vine4");
            }
        }

        private static void ConfigureLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.66f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.43f, 0.42f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.19f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.68f, 0.64f, 0.53f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0015f;
            Light light = RenderSettings.sun;
            if (light == null)
            {
                Light[] sceneLights = UnityEngine.Object.FindObjectsOfType<Light>();
                foreach (Light candidate in sceneLights)
                {
                    if (candidate != null && candidate.type == LightType.Directional)
                    {
                        light = candidate;
                        break;
                    }
                }
            }
            if (light == null)
            {
                GameObject sun = new GameObject("MedievalVillage_Sun");
                light = sun.AddComponent<Light>();
                light.type = LightType.Directional;
            }
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.88f, 0.69f);
            light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            RenderSettings.sun = light;
        }

        private static void SetColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null) renderer.sharedMaterial = new Material(shader) { color = color };
        }
    }
}
#endif
