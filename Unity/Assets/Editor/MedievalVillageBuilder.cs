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
            CreateUserReferenceVillageLayout();
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
            Debug.Log("MEDIEVAL_VILLAGE_REFERENCE_LAYOUT_CREATED=" + OutputScene +
                " | layout=VillageBuilder.cs | modelos colocados=" + placedModels + " | modelos ausentes=" + missingModels);
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
            // Marcadores de spawn são transforms vazios: não devem aparecer como
            // cilindros coloridos no cenário nem ficar fora dos limites do terreno.
            GameObject root = new GameObject("TEAM_SPAWN_POINTS_5x4");
            Vector3[] teamCenters =
            {
                new Vector3(-44f, 0.1f, -30f),
                new Vector3(-44f, 0.1f, 30f),
                new Vector3(44f, 0.1f, -30f),
                new Vector3(44f, 0.1f, 30f),
                new Vector3(0f, 0.1f, -48f)
            };

            for (int team = 0; team < teamCenters.Length; team++)
            {
                GameObject teamRoot = new GameObject("Team_" + (team + 1) + "_Spawns");
                teamRoot.transform.SetParent(root.transform, false);
                for (int member = 0; member < 4; member++)
                {
                    GameObject marker = new GameObject("Spawn_" + (member + 1));
                    marker.transform.SetParent(teamRoot.transform, false);
                    Vector3 offset = new Vector3((member % 2 == 0 ? -1.5f : 1.5f), 0f,
                        (member < 2 ? -1.5f : 1.5f));
                    marker.transform.position = teamCenters[team] + offset;
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
            // Disposição compacta igual ao VillageBuilder enviado: terreno de 60x60
            // e um único caminho de pedra longitudinal, sem ruas em grelha.
            GameObject groundRoot = new GameObject("Vilarejo_Ground_And_Path");
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Chao_Grama";
            ground.transform.SetParent(groundRoot.transform, false);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            SetColor(ground, new Color(0.30f, 0.45f, 0.20f));

            GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = "Caminho_Pedra_Central";
            path.transform.SetParent(groundRoot.transform, false);
            path.transform.position = new Vector3(0f, 0.02f, -2f);
            path.transform.localScale = new Vector3(3.2f, 0.04f, 30f);
            SetColor(path, new Color(0.55f, 0.52f, 0.50f));
            Renderer pathRenderer = path.GetComponent<Renderer>();
            if (pathRenderer != null && GetMaterial("stone") != null)
                pathRenderer.sharedMaterial = GetMaterial("stone");
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

        private static void CreateUserReferenceVillageLayout()
        {
            // Layout fiel às posições e à composição do VillageBuilder.cs enviado pelo usuário.
            // Casas grandes em primeiro plano, menores ao fundo e torre ao centro.
            GameObject houses = new GameObject("Vilarejo_Casas_Layout_Referencia");
            BuildReferenceHouse(houses.transform, "Casa_Esquerda_Grande",
                new Vector3(-9f, 0f, 2f), 0f, 3, 3, true, true);
            BuildReferenceHouse(houses.transform, "Casa_Esquerda_Media",
                new Vector3(-5f, 0f, -4f), -15f, 2, 2, true, true);
            BuildReferenceTower(houses.transform, "Torre_Central",
                new Vector3(2.5f, 0f, -9f));
            BuildReferenceHouse(houses.transform, "Casa_Fundo_Pequena_1",
                new Vector3(-1.5f, 0f, -6f), 10f, 2, 2, false, false);
            BuildReferenceHouse(houses.transform, "Casa_Fundo_Pequena_2",
                new Vector3(6f, 0f, -7f), -10f, 2, 2, false, false);
            BuildReferenceHouse(houses.transform, "Casa_Direita_Grande",
                new Vector3(9f, 0f, 1f), 180f, 3, 3, true, true);
        }

        private static void BuildReferenceHouse(Transform villageParent, string name,
            Vector3 origin, float rotationY, int widthModules, int floors, bool brickGround, bool hasBalcony)
        {
            const float gridUnit = 2f;
            const float floorHeight = 2.6f;
            GameObject house = new GameObject(name);
            house.transform.SetParent(villageParent, false);
            house.transform.position = origin;
            house.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            Transform previousParent = placementParent;
            placementParent = house.transform;
            float width = widthModules * gridUnit;

            for (int floor = 0; floor < floors; floor++)
            {
                float y = floor * floorHeight;
                bool groundFloor = floor == 0;
                string wallSet = groundFloor && brickGround ? "UnevenBrick" : "Plaster";

                // Piso inteiro por andar, dimensionado a partir da peça real do kit.
                Place("Piso_" + floor, origin + Vector3.up * (y - 0.05f),
                    new Vector3(width, 0.12f, width), rotationY, "Floor_WoodDark");

                for (int i = 0; i < widthModules; i++)
                {
                    float localX = (i - (widthModules - 1) / 2f) * gridUnit;
                    bool centerModule = i == widthModules / 2;
                    string frontPiece = groundFloor && centerModule
                        ? (brickGround ? "Wall_UnevenBrick_Door_Flat" : "Wall_Plaster_Door_Flat")
                        : "Wall_" + wallSet + "_Window_Wide_Flat";
                    Vector3 front = origin + Quaternion.Euler(0f, rotationY, 0f) *
                        new Vector3(localX, y + floorHeight * 0.5f, width * 0.5f);
                    Place("Parede_Frente_" + floor + "_" + i, front,
                        new Vector3(gridUnit, floorHeight, 0.22f), rotationY, frontPiece);

                    Vector3 back = origin + Quaternion.Euler(0f, rotationY, 0f) *
                        new Vector3(localX, y + floorHeight * 0.5f, -width * 0.5f);
                    Place("Parede_Fundo_" + floor + "_" + i, back,
                        new Vector3(gridUnit, floorHeight, 0.22f), rotationY + 180f,
                        "Wall_" + wallSet + "_Straight");
                }

                string corner = groundFloor && brickGround ? "Corner_Exterior_Brick" : "Corner_Exterior_Wood";
                Place("Canto_Direito_" + floor,
                    origin + Quaternion.Euler(0f, rotationY, 0f) * new Vector3(width * 0.5f, y + floorHeight * 0.5f, 0f),
                    new Vector3(gridUnit, floorHeight, gridUnit), rotationY, corner);
                Place("Canto_Esquerdo_" + floor,
                    origin + Quaternion.Euler(0f, rotationY, 0f) * new Vector3(-width * 0.5f, y + floorHeight * 0.5f, 0f),
                    new Vector3(gridUnit, floorHeight, gridUnit), rotationY + 180f, corner);

                if (hasBalcony && floor == floors - 2 && floors >= 2)
                {
                    Vector3 balconyPosition = origin + Quaternion.Euler(0f, rotationY, 0f) *
                        new Vector3(0f, y + floorHeight * 0.5f, width * 0.5f + 0.35f);
                    Place("Sacada_" + floor, balconyPosition,
                        new Vector3(width, 1.15f, 1.1f), rotationY, "Balcony_Simple_Straight");
                    Place("Beiral_Sacada_" + floor,
                        balconyPosition + Vector3.down * 0.45f,
                        new Vector3(width + 0.2f, 0.25f, 1.25f), rotationY, "Overhang_Plaster_Long");
                }
            }

            float topY = floors * floorHeight;
            string roof = widthModules <= 2 ? "Roof_RoundTiles_4x4" :
                widthModules == 3 ? "Roof_RoundTiles_6x6" : "Roof_RoundTiles_8x8";
            Place("Telhado", origin + Vector3.up * (topY + 0.7f),
                new Vector3(width + 0.8f, 1.8f, width + 0.8f), rotationY, roof);
            Place("Chamine", origin + Quaternion.Euler(0f, rotationY, 0f) *
                new Vector3(width * 0.5f - 0.35f, topY + 1.7f, -width * 0.5f + 0.35f),
                new Vector3(0.7f, 1.4f, 0.7f), rotationY, "Prop_Chimney");

            if (name == "Casa_Esquerda_Grande" || name == "Casa_Direita_Grande" ||
                name == "Casa_Esquerda_Media")
                Place("Trepadeira_Fachada",
                    origin + Quaternion.Euler(0f, rotationY, 0f) *
                    new Vector3(width * 0.5f - 0.2f, floorHeight, width * 0.5f + 0.1f),
                    new Vector3(0.8f, 2.4f, 0.25f), rotationY, "Prop_Vine1");

            placementParent = previousParent;
        }

        private static void BuildReferenceTower(Transform villageParent, string name, Vector3 origin)
        {
            const float gridUnit = 2f;
            const float floorHeight = 2.6f;
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(villageParent, false);
            tower.transform.position = origin;
            Transform previousParent = placementParent;
            placementParent = tower.transform;

            const int sides = 8;
            float radius = gridUnit * 0.9f;
            const int floors = 4;
            for (int floor = 0; floor < floors; floor++)
            {
                for (int side = 0; side < sides; side++)
                {
                    float angle = 360f / sides * side;
                    Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    Vector3 position = origin + direction * radius + Vector3.up * (floor * floorHeight);
                    string piece = floor == 1 && side % 2 == 0
                        ? "Wall_UnevenBrick_Window_Thin_Round"
                        : "Wall_UnevenBrick_Straight";
                    Place("Torre_Parede_" + floor + "_" + side,
                        position + Vector3.up * (floorHeight * 0.5f),
                        new Vector3(1.5f, floorHeight, 0.22f), angle, piece);
                }
            }
            Place("Torre_Telhado",
                origin + Vector3.up * (floors * floorHeight + 0.7f),
                new Vector3(5.5f, 3.2f, 5.5f), 0f, "Roof_Tower_RoundTiles");
            placementParent = previousParent;
        }

        private static void CreateCentralPlaza()
        {
            // O caminho longitudinal já define o eixo central da vila. Não criar
            // uma praça quadrada extra nem espalhar construções ao redor dela.
        }

        private static void CreateVillageDetails()
        {
            GameObject props = new GameObject("Props_Decoracao");
            Transform previousParent = placementParent;
            placementParent = props.transform;

            Place("Carroca", new Vector3(-10.5f, 0.9f, 6f),
                new Vector3(3.2f, 1.8f, 2f), 25f, "Prop_Wagon");
            Place("Caixote_01", new Vector3(-9.3f, 0.45f, 5.2f),
                new Vector3(0.9f, 0.9f, 0.9f), 10f, "Prop_Crate");
            Place("Caixote_02", new Vector3(-9.6f, 1.35f, 5f),
                new Vector3(0.9f, 0.9f, 0.9f), -15f, "Prop_Crate");

            // Cercas acompanham os dois lados do caminho, exatamente nas posições
            // do código de referência, em vez de ficarem distribuídas aleatoriamente.
            for (int i = 0; i < 6; i++)
            {
                float z = 2f - i * 2f;
                Place("Cerca_Direita_" + i, new Vector3(1.9f, 0.55f, z),
                    new Vector3(1.8f, 1.1f, 0.18f), 0f, "Prop_WoodenFence_Single");
                Place("Cerca_Esquerda_" + i, new Vector3(-1.9f, 0.55f, z),
                    new Vector3(1.8f, 1.1f, 0.18f), 180f, "Prop_WoodenFence_Single");
            }

            placementParent = previousParent;
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
