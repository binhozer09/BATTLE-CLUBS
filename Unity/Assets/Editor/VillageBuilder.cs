// VillageBuilder.cs
// ------------------------------------------------------------------
// Gera uma cena de vilarejo medieval no Unity Editor, usando as peças
// FBX + texturas do "Medieval Village MegaKit" (Quaternius, CC0).
// Sem rede, sem "agente remoto", sem sistema de combate — só a cena.
//
// COMO USAR:
// 1. Importe a pasta "Medieval Village MegaKit[Standard]/FBX" E a pasta
//    "Medieval Village MegaKit[Standard]/Textures" para dentro de Assets/
//    no seu projeto Unity (ex: Assets/VillageKit/FBX e
//    Assets/VillageKit/Textures).
// 2. Ajuste as constantes MODEL_FOLDER e TEXTURE_FOLDER logo abaixo se
//    você usou nomes de pasta diferentes.
// 3. Coloque este arquivo em Assets/Editor/.
// 4. No Unity: menu "Vilarejo > Construir Vilarejo".
// 5. Aperte Play pra andar (precisa também do SimpleFirstPersonController.cs
//    que te passei antes, em Assets/Scripts/).
//
// IMPORTANTE: eu não tenho Unity aqui pra renderizar e comparar com a
// imagem de verdade. Isso é uma aproximação da composição da foto, não
// uma cópia pixel a pixel — ajuste fino de posição costuma ser necessário
// depois de rodar, olhando a Scene view.
// ------------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class VillageBuilder : EditorWindow
{
    // ---- Pastas onde você importou o asset pack ----
    private const string MODEL_FOLDER = "Assets";   // busca em todo Assets/ por padrão
    private const string TEXTURE_FOLDER = "Assets"; // idem

    // ---- Calibração do grid (ajuste se as peças ficarem desalinhadas) ----
    private const float GRID_UNIT = 2f;
    private const float FLOOR_HEIGHT = 2.6f;

    private static Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, Material> materialCache = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
    private static System.Random rng = new System.Random(2026);

    [MenuItem("Vilarejo/Construir Vilarejo")]
    public static void BuildVillage()
    {
        modelCache.Clear();
        materialCache.Clear();

        GameObject old = GameObject.Find("Vilarejo_Gerado");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("Vilarejo_Gerado");

        SetupLighting(root);
        BuildGroundAndPath(root);

        // 🏡 Casa senhorial: dois andares, fachada larga, sacada e escada.
        BuildHouse(root, "Casa_Senhorial", new Vector3(-9.5f, 0f, 4f), -5f,
            3, 2, 2, "UnevenBrick", true, true, false, true);

        // 🏚️ Casa antiga: baixa, alvenaria irregular, venezianas abertas e trepadeira.
        BuildHouse(root, "Casa_Antiga", new Vector3(-4.5f, 0f, -3f), -12f,
            3, 2, 1, "UnevenBrick", false, false, true, false);

        // 🏠 Casa tradicional clara, dois pavimentos e janelas alinhadas.
        BuildHouse(root, "Casa_Tradicional", new Vector3(-0.5f, 0f, -7.5f), 8f,
            3, 2, 2, "Plaster", false, false, false, false);

        // Torre redonda ao fundo, preservada como marco da vila.
        BuildTower(root, "Torre_Central", new Vector3(3.8f, 0f, -9.5f));

        // 🏘️ Pequeno conjunto de três casas distintas formando uma rua.
        BuildHouseCluster(root, new Vector3(7.0f, 0f, -8f));

        // Casa de apoio à direita, com acabamento claro e sacada.
        BuildHouse(root, "Casa_Direita", new Vector3(10f, 0f, 2f), 170f,
            3, 2, 2, "Plaster", true, false, false, false);

        BuildLowWall(root);
        BuildProps(root);
        SpawnPlayer(root);

        const string scenePath = "Assets/Scenes/BATTLE_CLUBS_Medieval_Exploration.unity";
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new Exception("Não foi possível salvar a cena: " + scenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VILAREJO] CENA_SALVA=" + scenePath);

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log("Vilarejo gerado. Confira o Console acima por avisos de peças/texturas não encontradas.");
    }

    [MenuItem("Vilarejo/Ferramentas/Medir Tamanho do Objeto Selecionado")]
    public static void MeasureSelected()
    {
        if (Selection.activeGameObject == null) { Debug.LogWarning("Selecione um objeto primeiro."); return; }
        Renderer r = Selection.activeGameObject.GetComponentInChildren<Renderer>();
        if (r == null) { Debug.LogWarning("Sem Renderer no objeto selecionado."); return; }
        Debug.Log($"Tamanho de '{Selection.activeGameObject.name}': {r.bounds.size}");
    }

    // ------------------------------------------------------------
    private static void SetupLighting(GameObject root)
    {
        GameObject lightGO = new GameObject("Luz_Direcional");
        lightGO.transform.SetParent(root.transform);
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.color = new Color(1f, 0.95f, 0.85f);
        lightGO.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.60f, 0.78f, 0.92f);
        RenderSettings.ambientGroundColor = new Color(0.28f, 0.30f, 0.22f);
    }

    private static void BuildGroundAndPath(GameObject root)
    {
        // Terreno completo de 180 x 180 m. O caminho é montado com pisos reais do kit.
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Chao_Grama_180m";
        ground.transform.SetParent(root.transform, false);
        ground.transform.localScale = new Vector3(18f, 1f, 18f);
        ground.transform.position = new Vector3(0f, -0.03f, 0f);
        ApplyFlatColor(ground, new Color(0.29f, 0.40f, 0.22f));

        GameObject roads = new GameObject("Caminhos_Pedra_Kit");
        roads.transform.SetParent(root.transform, false);
        for (int i = 0; i < 66; i++)
        {
            float z = -66f + i * 2f;
            float x = Mathf.Sin(i * 0.075f) * 8f;
            PlaceTextured("Floor_UnevenBrick", roads, new Vector3(x, 0.015f, z),
                Quaternion.Euler(0f, Mathf.Sin(i * 0.075f) * 7f, 0f), "stone");
            PlaceTextured("Floor_UnevenBrick", roads, new Vector3(x + 2f, 0.015f, z),
                Quaternion.Euler(0f, Mathf.Sin(i * 0.075f) * 7f, 0f), "stone", false);
        }

        GameObject square = new GameObject("Praca_Central_Pedra");
        square.transform.SetParent(root.transform, false);
        for (int x = -3; x <= 3; x++)
        for (int z = -2; z <= 2; z++)
            PlaceTextured((x + z) % 4 == 0 ? "Floor_Brick" : "Floor_UnevenBrick",
                square, new Vector3(x * 2f, 0.025f, z * 2f + 2f),
                Quaternion.identity, "stone", false);
    }

    private static void BuildLowWall(GameObject root)
    {
        GameObject wall = new GameObject("Muro_Baixo_Direita");
        wall.transform.SetParent(root.transform);
        for (int i = 0; i < 8; i++)
        {
            float z = 10f - i * GRID_UNIT;
            Place("Prop_ExteriorBorder_Straight1", wall, new Vector3(2.2f, 0f, z), Quaternion.identity);
        }
    }

    /// Monta uma casa modular: paredes por andar, piso, sacada opcional,
    /// escada de pedra opcional, telhado e crista decorativa no topo.
    private static void BuildHouse(GameObject root, string name, Vector3 origin, float rotationY,
                                    int frontModules, int depthModules, int floors, string wallSet,
                                    bool hasBalcony, bool hasStairs, bool worn = false, bool timberUpper = false)
    {
        // Cada módulo do kit mede 2 m de largura por 3 m de altura.
        // As paredes começam no piso (Y=0); não são centralizadas na altura,
        // evitando que as casas fiquem suspensas ou com andares sobrepostos.
        const float module = 2f;
        const float storey = 3f;
        float halfWidth = frontModules * module * 0.5f;
        float halfDepth = depthModules * module * 0.5f;

        GameObject house = new GameObject(name);
        house.transform.SetParent(root.transform, false);
        house.transform.localPosition = origin;
        house.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);

        for (int floor = 0; floor < floors; floor++)
        {
            float y = floor * storey;
            bool groundFloor = floor == 0;
            string finishSet = (timberUpper && !groundFloor) ? "Plaster" : wallSet;
            string finishMaterial = finishSet == "UnevenBrick" ? "brick" : "plaster";
            string floorModel = groundFloor ? "Floor_UnevenBrick" : "Floor_WoodDark";

            // Piso contínuo, peça por peça, dentro das paredes.
            for (int x = 0; x < frontModules; x++)
            for (int z = 0; z < depthModules; z++)
            {
                float px = (x - (frontModules - 1) * 0.5f) * module;
                float pz = (z - (depthModules - 1) * 0.5f) * module;
                PlaceTextured(floorModel, house, new Vector3(px, y + 0.025f, pz),
                    Quaternion.identity, groundFloor ? "stone" : "wood", false);
            }

            // Fachada: porta central somente no térreo; janelas nos módulos restantes.
            for (int x = 0; x < frontModules; x++)
            {
                float px = (x - (frontModules - 1) * 0.5f) * module;
                string frontModel;
                if (groundFloor && x == frontModules / 2)
                    frontModel = "Wall_" + finishSet + "_Door_Flat";
                else
                    frontModel = "Wall_" + finishSet + "_Window_Wide_Flat";

                PlaceTextured(frontModel, house, new Vector3(px, y, halfDepth),
                    Quaternion.identity, finishMaterial);

                // Parede traseira fechada, sem aberturas aleatórias.
                PlaceTextured("Wall_" + finishSet + "_Straight", house,
                    new Vector3(px, y, -halfDepth), Quaternion.Euler(0f, 180f, 0f),
                    finishMaterial, false);
            }

            // Laterais fechadas, com comprimento correspondente à profundidade da casa.
            for (int z = 0; z < depthModules; z++)
            {
                float pz = (z - (depthModules - 1) * 0.5f) * module;
                PlaceTextured("Wall_" + finishSet + "_Straight", house,
                    new Vector3(-halfWidth, y, pz), Quaternion.Euler(0f, 90f, 0f),
                    finishMaterial, false);
                PlaceTextured("Wall_" + finishSet + "_Straight", house,
                    new Vector3(halfWidth, y, pz), Quaternion.Euler(0f, -90f, 0f),
                    finishMaterial, false);
            }

            // Molduras de madeira sob as janelas e beirais discretos.
            if (!groundFloor)
            {
                for (int x = 0; x < frontModules; x++)
                {
                    if (x == frontModules / 2) continue;
                    float px = (x - (frontModules - 1) * 0.5f) * module;
                    PlaceTextured("WindowShutters_Wide_Flat_Closed", house,
                        new Vector3(px, y, halfDepth + 0.06f), Quaternion.identity,
                        "wood", false);
                }
            }

            if (hasBalcony && floor == floors - 2)
            {
                PlaceTextured("Balcony_Simple_Straight", house,
                    new Vector3(0f, y + 0.05f, halfDepth + 0.72f),
                    Quaternion.identity, "wood", false);
            }
        }

        // Telhado de duas águas do kit dimensionado para a planta 6 x 4 m.
        // O modelo tem a cumeeira elevada em relação aos beirais.
        float roofBase = floors * storey + 0.55f;
        PlaceTextured("Roof_RoundTiles_6x4", house, new Vector3(0f, roofBase, 0f),
            Quaternion.identity, "roof");
        PlaceTextured("Prop_Chimney", house,
            new Vector3(halfWidth - 0.55f, roofBase + 1.1f, -halfDepth + 0.45f),
            Quaternion.identity, "brick", false);

        if (hasStairs)
            PlaceTextured("Stairs_Exterior_Straight", house,
                new Vector3(0f, 0.02f, halfDepth + 1.15f),
                Quaternion.identity, "stone", false);

        if (worn)
        {
            // Casa velha: venezianas abertas e trepadeira; mantém estrutura íntegra.
            PlaceTextured("WindowShutters_Wide_Flat_Open", house,
                new Vector3(-module, storey, halfDepth + 0.07f),
                Quaternion.identity, "wood", false);
            PlaceTextured("Prop_Vine1", house,
                new Vector3(halfWidth - 0.12f, 0.2f, halfDepth + 0.08f),
                Quaternion.identity, "wood", false);
        }
        else
        {
            PlaceTextured("Prop_Vine1", house,
                new Vector3(halfWidth - 0.12f, 0.2f, halfDepth + 0.08f),
                Quaternion.identity, "wood", false);
        }
    }

    // Conjunto de três casas próximas, com alturas e acabamentos diferentes,
    // para formar visualmente uma pequena rua medieval e não uma única caixa.
    private static void BuildHouseCluster(GameObject root, Vector3 origin)
    {
        BuildHouse(root, "Vila_Casinha_1", origin + new Vector3(-3.6f, 0f, 0f), -8f,
            3, 2, 1, "UnevenBrick", false, false, true, false);
        BuildHouse(root, "Vila_Casinha_2", origin + new Vector3(2.8f, 0f, -1.0f), 9f,
            3, 2, 2, "Plaster", false, false, false, false);
        BuildHouse(root, "Vila_Casinha_3", origin + new Vector3(0.0f, 0f, -6.0f), 3f,
            3, 2, 1, "Plaster", false, false, false, false);
    }

    private static void PlaceFloorSlab(GameObject house, float y, float width, string mat)
    {
        GameObject found = PlaceTextured("Floor_WoodDark", house, new Vector3(0f, y, 0f), Quaternion.identity, "wood", logIfMissing: false);
        if (found == null)
        {
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Piso_Fallback";
            slab.transform.SetParent(house.transform);
            slab.transform.localPosition = new Vector3(0f, y - 0.05f, 0f);
            slab.transform.localScale = new Vector3(width, 0.1f, width);
            ApplyFlatColor(slab, new Color(0.4f, 0.3f, 0.2f));
        }
        else
        {
            found.transform.localScale = new Vector3(width / GRID_UNIT, 1f, width / GRID_UNIT);
        }
    }

    private static string PickClosestRoof(int widthModules)
    {
        if (widthModules <= 2) return "Roof_RoundTiles_4x4";
        if (widthModules == 3) return "Roof_RoundTiles_6x6";
        return "Roof_RoundTiles_8x8";
    }

    private static void BuildTower(GameObject root, string name, Vector3 origin)
    {
        GameObject tower = new GameObject(name);
        tower.transform.SetParent(root.transform);
        tower.transform.position = origin;

        int sides = 8;
        float radius = GRID_UNIT * 0.9f;
        int floors = 4;

        for (int floor = 0; floor < floors; floor++)
        {
            float y = floor * FLOOR_HEIGHT;
            for (int s = 0; s < sides; s++)
            {
                float angle = (360f / sides) * s;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                string piece = (floor == 1 && s % 2 == 0) ? "Wall_UnevenBrick_Window_Thin_Round" : "Wall_UnevenBrick_Straight";
                PlaceTextured(piece, tower, dir * radius + Vector3.up * y, Quaternion.Euler(0, angle, 0), "stone");
            }
        }

        PlaceTextured("Roof_Tower_RoundTiles", tower, new Vector3(0f, floors * FLOOR_HEIGHT, 0f), Quaternion.identity, "roof");
    }

    private static void BuildProps(GameObject root)
    {
        GameObject props = new GameObject("Props_Decoracao");
        props.transform.SetParent(root.transform);

        PlaceTextured("Prop_Wagon", props, new Vector3(-11.5f, 0f, 7.5f), Quaternion.Euler(0, 25f, 0), "wood");
        PlaceTextured("Prop_Crate", props, new Vector3(-10.3f, 0f, 6.7f), Quaternion.Euler(0, 10f, 0), "wood");
        PlaceTextured("Prop_Crate", props, new Vector3(-10.6f, 0.6f, 6.5f), Quaternion.Euler(0, -15f, 0), "wood");

        for (int i = 0; i < 6; i++)
        {
            float z = 3f - i * GRID_UNIT;
            PlaceTextured("Prop_WoodenFence_Single", props, new Vector3(2.0f, 0f, z), Quaternion.identity, "wood");
            PlaceTextured("Prop_WoodenFence_Single", props, new Vector3(-2.0f, 0f, z), Quaternion.Euler(0, 180, 0), "wood");
        }
    }

    private static void SpawnPlayer(GameObject root)
    {
        // Usa o modelo humanoide real do pacote CC0; não cria cápsula como visual.
        string modelPath = BattleClubs.Editor.HumanoidAssetBootstrap.EnsureHumanoidModelImported();
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null) throw new Exception("Modelo humanoide não carregou: " + modelPath);

        GameObject player = new GameObject("Jogador_Explorador");
        player.transform.SetParent(root.transform, false);
        player.transform.position = new Vector3(0f, 0.1f, 13f);
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.9f;
        cc.center = new Vector3(0f, 0.95f, 0f);
        cc.radius = 0.34f;
        cc.stepOffset = 0.25f;
        cc.skinWidth = 0.04f;

        GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        if (visual == null) visual = UnityEngine.Object.Instantiate(modelAsset);
        visual.name = "Visual_Humanoide";
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.isHuman)
            throw new Exception("O personagem importado não possui Avatar Humanoid válido.");
        animator.applyRootMotion = false;

        Bounds bounds = new Bounds();
        bool hasBounds = false;
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (hasBounds && bounds.size.y > 0.01f)
            visual.transform.localScale = Vector3.one * (1.8f / bounds.size.y);

        GameObject pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(pivot.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0.45f, -5.5f);
        cameraObject.transform.localRotation = Quaternion.identity;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 62f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 300f;
        cameraObject.AddComponent<AudioListener>();

        player.AddComponent<MedievalThirdPersonController>();
    }

    // ------------------------------------------------------------
    // Busca/instancia modelos e aplica material com textura real
    // ------------------------------------------------------------
    private static GameObject PlaceTextured(string modelName, GameObject parent, Vector3 localPos, Quaternion localRot, string materialKey, bool logIfMissing = true)
    {
        GameObject instance = Place(modelName, parent, localPos, localRot, logIfMissing);
        if (instance != null)
        {
            Material mat = GetMaterial(materialKey);
            if (mat != null)
            {
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = mat;
            }
        }
        return instance;
    }

    private static GameObject Place(string modelName, GameObject parent, Vector3 localPos, Quaternion localRot, bool logIfMissing = true)
    {
        GameObject prefab = FindModel(modelName);
        if (prefab == null)
        {
            if (logIfMissing)
                Debug.LogWarning($"[Vilarejo] Modelo '{modelName}' não encontrado. Confira se o FBX foi importado em {MODEL_FOLDER}.");
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent.scene) as GameObject;
        if (instance == null) instance = UnityEngine.Object.Instantiate(prefab);
        instance.transform.SetParent(parent.transform);
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = localRot;
        instance.name = modelName;
        return instance;
    }

    private static GameObject FindModel(string name)
    {
        if (modelCache.TryGetValue(name, out GameObject cached)) return cached;

        string[] guids = AssetDatabase.FindAssets(name + " t:Model", new[] { MODEL_FOLDER });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                modelCache[name] = model;
                return model;
            }
        }
        GameObject fallback = guids.Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(m => m != null);
        modelCache[name] = fallback;
        return fallback;
    }

    private static Material GetMaterial(string key)
    {
        if (materialCache.TryGetValue(key, out Material cached)) return cached;

        string baseName = key switch
        {
            "roof" => "T_RoundTiles_BaseColor",
            "wood" => "T_WoodTrim_BaseColor",
            "plaster" => "T_Plaster_BaseColor",
            "stone" => "T_RockTrim_BaseColor",
            _ => "T_UnevenBrick_BaseColor",
        };
        string normalName = baseName.Replace("BaseColor", "Normal");

        Texture2D baseTex = FindTexture(baseName);
        Texture2D normalTex = FindTexture(normalName);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) return null;

        Material mat = new Material(shader) { name = "MV_" + key };

        if (baseTex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseTex);
        }
        if (normalTex != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", normalTex);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (baseTex == null)
        {
            // sem textura encontrada: usa uma cor aproximada só pra não ficar branco
            Color fallback = key switch
            {
                "roof" => new Color(0.55f, 0.18f, 0.12f),
                "wood" => new Color(0.42f, 0.28f, 0.16f),
                "plaster" => new Color(0.85f, 0.80f, 0.70f),
                "stone" => new Color(0.55f, 0.55f, 0.55f),
                _ => new Color(0.50f, 0.32f, 0.25f),
            };
            mat.color = fallback;
            Debug.LogWarning($"[Vilarejo] Textura '{baseName}' não encontrada em {TEXTURE_FOLDER} — usando cor sólida provisória.");
        }

        materialCache[key] = mat;
        return mat;
    }

    private static Texture2D FindTexture(string token)
    {
        string[] guids = AssetDatabase.FindAssets(token + " t:Texture2D", new[] { TEXTURE_FOLDER });
        return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static void ApplyFlatColor(GameObject go, Color c)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = c;
        r.sharedMaterial = mat;
    }
}