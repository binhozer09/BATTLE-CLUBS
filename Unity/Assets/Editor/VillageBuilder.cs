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

        // Composição inspirada na foto de referência:
        // Esquerda (perto da câmera): casa grande de madeira+pedra, 3 andares,
        //   sacada, escada de pedra na frente, trepadeiras
        BuildHouse(root, "Casa_Esquerda_Grande", new Vector3(-9.5f, 0f, 4f), -5f,
                   widthModules: 3, floors: 3, groundMaterial: "brick", hasBalcony: true, hasStairs: true);

        // Esquerda (meio-fundo): casa estreita e alta, com pequena varanda no topo
        BuildHouse(root, "Casa_Esquerda_Estreita", new Vector3(-4.5f, 0f, -3f), -12f,
                   widthModules: 2, floors: 3, groundMaterial: "stone", hasBalcony: true, hasStairs: false);

        // Centro-fundo: casinha clara, mais baixa
        BuildHouse(root, "Casa_Centro_Pequena", new Vector3(-0.5f, 0f, -7.5f), 8f,
                   widthModules: 2, floors: 2, groundMaterial: "plaster", hasBalcony: false, hasStairs: false);

        // Torre redonda, fundo centro-direita
        BuildTower(root, "Torre_Central", new Vector3(3.8f, 0f, -9.5f));

        // Casa pequena atrás da torre
        BuildHouse(root, "Casa_Atras_Torre", new Vector3(6.5f, 0f, -8f), -8f,
                   widthModules: 2, floors: 2, groundMaterial: "brick", hasBalcony: false, hasStairs: false);

        // Direita (perto da câmera): casa grande espelhada, com sacadas salientes
        BuildHouse(root, "Casa_Direita_Grande", new Vector3(10f, 0f, 2f), 170f,
                   widthModules: 3, floors: 3, groundMaterial: "stone", hasBalcony: true, hasStairs: false);

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
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Chao_Grama";
        ground.transform.SetParent(root.transform);
        ground.transform.localScale = new Vector3(6f, 1f, 6f);
        ApplyFlatColor(ground, new Color(0.32f, 0.47f, 0.22f));

        GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
        path.name = "Caminho_Pedra";
        path.transform.SetParent(root.transform);
        path.transform.position = new Vector3(0f, 0.02f, -2f);
        path.transform.localScale = new Vector3(3.4f, 0.04f, 32f);
        ApplyFlatColor(path, new Color(0.52f, 0.50f, 0.48f));
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
                                    int widthModules, int floors, string groundMaterial, bool hasBalcony, bool hasStairs)
    {
        // Cada casa é uma caixa fechada: frente, fundo, duas laterais, quatro cantos e pisos.
        // O kit usa módulos de 2 m e pavimentos de aproximadamente 3,12 m.
        GameObject house = new GameObject(name);
        house.transform.SetParent(root.transform, false);
        house.transform.position = origin;
        house.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

        const float module = 2f;
        const float storey = 3.12f;
        float half = widthModules * module * 0.5f;
        string groundWall = groundMaterial == "brick" ? "UnevenBrick" : "Plaster";
        string upperWall = "Plaster";

        for (int floor = 0; floor < floors; floor++)
        {
            float y = floor * storey;
            bool ground = floor == 0;
            string wallSet = ground ? groundWall : upperWall;
            string finish = ground ? groundMaterial : "plaster";

            // Piso contínuo em módulos reais do pacote, sem laje cúbica de fallback.
            string floorModel = ground ? "Floor_UnevenBrick" : "Floor_WoodDark";
            for (int x = 0; x < widthModules; x++)
            for (int z = 0; z < widthModules; z++)
                PlaceTextured(floorModel, house,
                    new Vector3((x - (widthModules - 1) * 0.5f) * module, y + 0.02f,
                                (z - (widthModules - 1) * 0.5f) * module),
                    Quaternion.identity, ground ? "stone" : "wood");

            // Frente: porta no centro do térreo; janelas nas demais seções.
            for (int i = 0; i < widthModules; i++)
            {
                float offset = (i - (widthModules - 1) * 0.5f) * module;
                string front = ground && i == widthModules / 2
                    ? "Wall_" + wallSet + "_Door_Flat"
                    : "Wall_" + wallSet + "_Window_Wide_Flat";
                PlaceTextured(front, house, new Vector3(offset, y + storey * 0.5f, half),
                    Quaternion.identity, finish);
                PlaceTextured("Wall_" + wallSet + "_Straight", house,
                    new Vector3(offset, y + storey * 0.5f, -half),
                    Quaternion.Euler(0f, 180f, 0f), finish);

                // Laterais completas: nenhuma parede fica aberta.
                PlaceTextured("Wall_" + wallSet + "_Straight", house,
                    new Vector3(-half, y + storey * 0.5f, offset),
                    Quaternion.Euler(0f, 90f, 0f), finish);
                PlaceTextured("Wall_" + wallSet + "_Straight", house,
                    new Vector3(half, y + storey * 0.5f, offset),
                    Quaternion.Euler(0f, -90f, 0f), finish);

                if (ground && i != widthModules / 2)
                {
                    PlaceTextured("WindowShutters_Wide_Flat_Closed", house,
                        new Vector3(offset, y + storey * 0.5f, half + 0.08f),
                        Quaternion.identity, "wood", false);
                }
            }

            string corner = "Corner_Exterior_" + (ground ? "Brick" : "Wood");
            foreach (float x in new[] { -half, half })
            foreach (float z in new[] { -half, half })
                PlaceTextured(corner, house, new Vector3(x, y + storey * 0.5f, z),
                    Quaternion.Euler(0f, x * z > 0f ? 180f : 0f, 0f), finish, false);

            if (hasBalcony && floor == floors - 2)
            {
                PlaceTextured("Balcony_Simple_Straight", house,
                    new Vector3(0f, y + 0.55f, half + 0.55f), Quaternion.identity, "wood", false);
                PlaceTextured("Overhang_Plaster_Long", house,
                    new Vector3(0f, y + storey - 0.12f, half + 0.12f), Quaternion.identity, "plaster", false);
            }
        }

        float roofY = floors * storey;
        string roof = widthModules <= 2 ? "Roof_RoundTiles_4x4" : "Roof_RoundTiles_6x6";
        PlaceTextured(roof, house, new Vector3(0f, roofY + 1.35f, 0f), Quaternion.identity, "roof");
        PlaceTextured("Roof_Front_Brick" + (widthModules <= 2 ? "4" : "6"), house,
            new Vector3(0f, roofY + 1.25f, half + 0.15f), Quaternion.identity, "brick", false);
        PlaceTextured("Roof_Front_Brick" + (widthModules <= 2 ? "4" : "6"), house,
            new Vector3(0f, roofY + 1.25f, -half - 0.15f), Quaternion.Euler(0f, 180f, 0f), "brick", false);
        PlaceTextured("Prop_Chimney", house,
            new Vector3(half - 0.45f, roofY + 1.65f, -half + 0.45f), Quaternion.identity, "brick", false);

        if (hasStairs)
            PlaceTextured("Stairs_Exterior_Straight", house,
                new Vector3(0f, 0.05f, half + 1.35f), Quaternion.identity, "stone", false);
        if (rng.NextDouble() > 0.35)
            PlaceTextured("Prop_Vine1", house, new Vector3(half - 0.15f, storey, half + 0.06f),
                Quaternion.identity, "wood", false);
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