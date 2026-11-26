#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BattleClubs.Player;
using BattleClubs.Gameplay;

namespace BattleClubs.Editor
{
    /// <summary>
    /// Builds the medieval village as the new game environment.
    /// Reuses only the configured humanoid player from the previous gameplay
    /// scene; the previous map and unrelated scene roots are not carried over.
    /// </summary>
    public static class MedievalVillageBuilder
    {
        private const string MainScene = "Assets/Scenes/BattleClubs_HumanoidSource.unity";
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
                BattleClubsFullSceneBuilder.CreateHumanoidSourceScene();

            LoadModels();
            if (Models.Count < 150)
                throw new Exception("Foram encontrados apenas " + Models.Count + " modelos FBX; eram esperados 176. Confira a importação antes de gerar a cena.");

            EnsureGeneratedMaterialFolder();
            Scene target = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject player = CopyHumanoidPlayerFromMainScene(target);
            SceneManager.SetActiveScene(target);
            CreateVillageCamera(player);
            CreateTeamSetup(player.GetComponent<Combatant>());
            CreateSpawnMarkers();
            ConfigureLighting();
            CreateGroundAndRoads();
            CreateVillageHouses();
            CreateCentralPlaza();
            CreateVillageDetails();
            ValidateVillageScene(target, player);
            EditorSceneManager.SaveScene(target, OutputScene);
            // Make the medieval village the only enabled startup scene for builds.
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(OutputScene, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MEDIEVAL_VILLAGE_SCENE_CREATED=" + OutputScene +
                " | modelos colocados=" + placedModels + " | modelos ausentes=" + missingModels);
        }

        private static GameObject CopyHumanoidPlayerFromMainScene(Scene target)
        {
            if (!File.Exists(MainScene))
                throw new Exception("Cena de origem do humanoide não encontrada: " + MainScene);

            Scene source = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Additive);
            GameObject player = null;
            foreach (GameObject root in source.GetRootGameObjects())
            {
                if (root != null && root.name == "Player_Local")
                {
                    player = root;
                    break;
                }
            }

            if (player == null)
            {
                EditorSceneManager.CloseScene(source, true);
                throw new Exception("Não encontrei Player_Local na cena principal para reaproveitar o humanoide.");
            }

            SceneManager.MoveGameObjectToScene(player, target);
            player.transform.position = new Vector3(0f, 0.2f, -5f);
            EditorSceneManager.CloseScene(source, true);
            return player;
        }

        private static void CreateVillageCamera(GameObject player)
        {
            GameObject cameraRoot = new GameObject("Village_Player_Camera");
            Camera camera = cameraRoot.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            cameraRoot.AddComponent<AudioListener>();
            cameraRoot.AddComponent<BattleAIDiagnostic>();
            cameraRoot.AddComponent<BattleClubsRemoteAgent>();
            ThirdPersonCameraRig rig = cameraRoot.AddComponent<ThirdPersonCameraRig>();
            if (player == null)
                throw new Exception("Player_Local não foi transferido para a cena medieval.");
            rig.SetTarget(player.transform);
            ThirdPersonMotor motor = player.GetComponent<ThirdPersonMotor>();
            if (motor != null)
            {
                SerializedObject motorData = new SerializedObject(motor);
                SerializedProperty cameraField = motorData.FindProperty("cameraTransform");
                if (cameraField == null)
                    throw new Exception("Campo cameraTransform não encontrado no controlador do humanoide.");
                cameraField.objectReferenceValue = cameraRoot.transform;
                motorData.ApplyModifiedPropertiesWithoutUndo();
            }
            BatAttack attack = player.GetComponent<BatAttack>();
            if (attack != null)
                SetObjectReference(attack, "cameraRig", rig);
        }

        private static void SetObjectReference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null)
                throw new Exception("Campo serializado não encontrado: " + property);
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateTeamSetup(Combatant localPlayer)
        {
            if (localPlayer == null)
                throw new Exception("Combatant do humanoide não foi preservado.");
            GameObject rulesObject = new GameObject("Team_Match_Rules");
            TeamMatchRules rules = rulesObject.AddComponent<TeamMatchRules>();
            SerializedObject serialized = new SerializedObject(rules);
            serialized.FindProperty("playersPerTeam").intValue = 4;
            serialized.FindProperty("pointsPerEliminatedTeam").intValue = 3;
            SerializedProperty teams = serialized.FindProperty("teams");
            teams.arraySize = 5;
            string[] teamNames = { "Azul", "Vermelho", "Verde", "Amarelo", "Roxo" };
            for (int i = 0; i < teams.arraySize; i++)
            {
                SerializedProperty team = teams.GetArrayElementAtIndex(i);
                team.FindPropertyRelative("teamId").stringValue = "TEAM_" + (i + 1);
                team.FindPropertyRelative("clubId").stringValue = teamNames[i];
                team.FindPropertyRelative("players").arraySize = 0;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            rules.TryAddPlayer("TEAM_1", localPlayer);
        }

        private static void CreateSpawnMarkers()
        {
            GameObject root = new GameObject("TEAM_SPAWN_POINTS_5x4");
            Color[] colors = {
                new Color(0.12f, 0.35f, 0.85f), new Color(0.8f, 0.16f, 0.14f),
                new Color(0.15f, 0.62f, 0.25f), new Color(0.9f, 0.68f, 0.12f),
                new Color(0.48f, 0.2f, 0.72f)
            };
            for (int team = 0; team < 5; team++)
            {
                GameObject teamRoot = new GameObject("Team_" + (team + 1) + "_Spawns");
                teamRoot.transform.SetParent(root.transform, false);
                for (int member = 0; member < 4; member++)
                {
                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    marker.name = "Spawn_" + (member + 1);
                    marker.transform.SetParent(teamRoot.transform, false);
                    float angle = (team * 72f + member * 12f) * Mathf.Deg2Rad;
                    marker.transform.position = new Vector3(38f + Mathf.Cos(angle) * (14f + team * 3f), 0.08f,
                        38f + Mathf.Sin(angle) * (14f + team * 3f));
                    marker.transform.localScale = new Vector3(0.7f, 0.08f, 0.7f);
                    SetColor(marker, colors[team]);
                    Collider collider = marker.GetComponent<Collider>();
                    if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
                }
            }
        }

        private static void ValidateVillageScene(Scene scene, GameObject player)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new Exception("A cena medieval não está carregada corretamente.");
            if (player == null || player.name != "Player_Local" ||
                player.GetComponent<CharacterController>() == null ||
                player.GetComponent<ThirdPersonMotor>() == null)
                throw new Exception("O humanoide ou seu controlador não foi preservado.");
            Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
            int villageCameras = 0;
            foreach (Camera camera in cameras)
                if (camera != null && camera.name == "Village_Player_Camera" &&
                    camera.CompareTag("MainCamera"))
                    villageCameras++;
            if (villageCameras != 1)
                throw new Exception("A cena precisa ter exatamente uma câmera principal nova do vilarejo.");
            if (placedModels < 150)
                throw new Exception("Poucos modelos medievais foram posicionados: " + placedModels + " (mínimo esperado: 150).");
            if (missingModels > 0)
                throw new Exception("A cena ficou incompleta: " + missingModels + " peças do vilarejo não foram encontradas.");
            if (scene.GetRootGameObjects().Length < 8)
                throw new Exception("A cena medieval está incompleta: poucos grupos de cenário e jogo foram criados.");
            int meshColliders = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                meshColliders += root.GetComponentsInChildren<MeshCollider>(true).Length;
            if (meshColliders < 10)
                throw new Exception("As construções não receberam colisões MeshCollider suficientes: " + meshColliders);
            Debug.Log("MEDIEVAL_VILLAGE_VALIDATION_OK | player=Player_Local | cameras=" +
                villageCameras + " | placedModels=" + placedModels + " | meshColliders=" + meshColliders);
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

            ApplyKitMaterial(instance, label + " " + source.name);
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
                key == "plaster" ? "T_Plaster_BaseColor" :
                key == "stone" ? "T_RockTrim_BaseColor" : "T_UnevenBrick_BaseColor";
            string normalName = key == "roof" ? "T_RoundTiles_Normal" :
                key == "wood" ? "T_WoodTrim_Normal" :
                key == "plaster" ? "T_Plaster_Normal" :
                key == "stone" ? "T_RockTrim_Normal" : "T_UnevenBrick_Normal";
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
            // Área compacta: o terreno acompanha o núcleo construído, sem ruas
            // em grelha atravessando casas ou grandes vazios entre construções.
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "MedievalVillage_Ground_Collision";
            ground.transform.position = new Vector3(0f, -0.35f, 0f);
            ground.transform.localScale = new Vector3(104f, 0.7f, 104f);
            SetColor(ground, new Color(0.28f, 0.30f, 0.22f));
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null && GetMaterial("stone") != null)
                groundRenderer.sharedMaterial = GetMaterial("stone");

            // Praça central livre e ruas em anel: quatro acessos chegam à praça
            // sem cortar o espaço de circulação nem passar por dentro das casas.
            CreateRoad(new Vector3(0f, 0.015f, -34f), new Vector3(6f, 0.08f, 28f));
            CreateRoad(new Vector3(0f, 0.015f, 34f), new Vector3(6f, 0.08f, 28f));
            CreateRoad(new Vector3(-34f, 0.015f, 0f), new Vector3(28f, 0.08f, 6f));
            CreateRoad(new Vector3(34f, 0.015f, 0f), new Vector3(28f, 0.08f, 6f));

            // Ruas de contorno delimitam os quatro pequenos quarteirões.
            CreateRoad(new Vector3(0f, 0.012f, -15.5f), new Vector3(39f, 0.07f, 3.2f));
            CreateRoad(new Vector3(0f, 0.012f, 15.5f), new Vector3(39f, 0.07f, 3.2f));
            CreateRoad(new Vector3(-15.5f, 0.012f, 0f), new Vector3(3.2f, 0.07f, 39f));
            CreateRoad(new Vector3(15.5f, 0.012f, 0f), new Vector3(3.2f, 0.07f, 39f));

            // Pequenas ligações entre os quarteirões e as entradas do povoado.
        }

        private static void CreateRoad(Vector3 position, Vector3 scale)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "Village_Cobblestone_Road";
            road.transform.position = position;
            road.transform.localScale = scale;
            SetColor(road, new Color(0.36f, 0.34f, 0.29f));
            Renderer roadRenderer = road.GetComponent<Renderer>();
            if (roadRenderer != null && GetMaterial("brick") != null)
                roadRenderer.sharedMaterial = GetMaterial("brick");
        }

        private static void CreateVillageHouses()
        {
            // 4 quarteirões compactos ao redor da praça. As casas ficam alinhadas
            // em pequenos grupos, com fachadas voltadas para as ruas internas.
            int id = 0;
            float[] offsets = { 22f, 32f };
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    foreach (float x in offsets)
                    {
                        foreach (float z in offsets)
                        {
                            Vector3 center = new Vector3(sx * x, 0f, sz * z);
                            float yaw = sx < 0 ? 90f : 270f;
                            if (sz < 0) yaw += 180f;
                            BuildHouse("House_Quarter_" + (++id).ToString("00"), center, yaw, id);
                        }
                    }
                }
            }

            // Quatro construções de entrada/comércio fecham visualmente o núcleo
            // do povoado e dão continuidade às ruas que chegam à praça.
            BuildHouse("House_Gate_North", new Vector3(-10f, 0f, 42f), 180f, 25);
            BuildHouse("House_Gate_South", new Vector3(10f, 0f, -42f), 0f, 26);
            BuildHouse("House_Gate_West", new Vector3(-42f, 0f, 10f), 90f, 27);
            BuildHouse("House_Gate_East", new Vector3(42f, 0f, -10f), 270f, 28);
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
            Renderer plazaRenderer = plaza.GetComponent<Renderer>();
            if (plazaRenderer != null && GetMaterial("stone") != null)
                plazaRenderer.sharedMaterial = GetMaterial("stone");

            Place("Market_Wagon", new Vector3(-5f, 0.8f, 3f), new Vector3(3.8f, 2.2f, 2.2f), 35f, "Prop_Wagon");
            Place("Market_Crate_01", new Vector3(4f, 0.45f, -3f), new Vector3(1f, 0.9f, 1f), 15f, "Prop_Crate");
            Place("Market_Crate_02", new Vector3(5.2f, 0.45f, -3.1f), new Vector3(1f, 0.9f, 1f), -8f, "Prop_Crate");
            Place("Village_Gate_Arch", new Vector3(0f, 2.4f, -48f), new Vector3(9f, 5f, 1.3f), 0f, "Wall_Arch");
            Place("Village_Tower_Roof", new Vector3(0f, 9f, 47f), new Vector3(9f, 4f, 9f), 0f, "Roof_Tower_RoundTiles");
        }

        private static void CreateVillageDetails()
        {
            // Nada de espalhar adereços aleatoriamente pelo mapa. Cercas e trepadeiras
            // ficam agrupadas junto aos quintais dos quatro quarteirões.
            int id = 0;
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    // Mantém os detalhes nos quintais das casas do anel externo (offset de 32 m).
                    float x = sx * 32f;
                    float z = sz * 32f;
                    Place("Courtyard_Fence_" + (++id).ToString("00"),
                        new Vector3(x - sx * 3f, 0.55f, z - sz * 4f),
                        new Vector3(3.2f, 1.1f, 0.18f), sx < 0 ? 90f : 270f,
                        "Prop_WoodenFence_Single", "Prop_MetalFence_Simple");
                    Place("Courtyard_Fence_" + (++id).ToString("00"),
                        new Vector3(x + sx * 3f, 0.55f, z - sz * 4f),
                        new Vector3(3.2f, 1.1f, 0.18f), sx < 0 ? 90f : 270f,
                        "Prop_WoodenFence_Single", "Prop_MetalFence_Simple");
                    Place("Courtyard_Vine_" + (++id).ToString("00"),
                        new Vector3(x - sx * 4.5f, 1.35f, z),
                        new Vector3(1.4f, 2.3f, 0.25f), 0f,
                        "Prop_Vine1", "Prop_Vine2", "Prop_Vine4");
                }
            }

            // Pequenos elementos de mercado permanecem concentrados na praça.
            Place("Market_Barrel_01", new Vector3(-7f, 0.5f, 5f),
                new Vector3(0.9f, 1f, 0.9f), 0f, "Prop_Barrel", "Prop_Crate");
            Place("Market_Barrel_02", new Vector3(-5.8f, 0.5f, 5.2f),
                new Vector3(0.9f, 1f, 0.9f), 12f, "Prop_Barrel", "Prop_Crate");
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
