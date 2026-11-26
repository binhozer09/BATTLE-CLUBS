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
        if (old != null) Object.DestroyImmediate(old);
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
        GameObject house = new GameObject(name);
        house.transform.SetParent(root.transform);
        house.transform.position = origin;
        house.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        float width = widthModules * GRID_UNIT;
        for (int floor = 0; floor < floors; floor++)
        {
            float y = floor * FLOOR_HEIGHT;
            bool isGround = floor == 0;
            string wallMat = isGround ? groundMaterial : "plaster";
            string wallSet = isGround
                ? (groundMaterial == "stone" ? "UnevenBrick" : "UnevenBrick")
                : "Plaster";
            PlaceFloorSlab(house, y, width, wallMat);
            for (int i = 0; i < widthModules; i++)
            {
                float localX = (i - (widthModules - 1) / 2f) * GRID_UNIT;
                bool isCenter = (i == widthModules / 2);
                string frontPiece = (isGround && isCenter)
                    ? $"Wall_{wallSet}_Door_Flat"
                    : $"Wall_{wallSet}_Window_Wide_Flat";
                PlaceTextured(frontPiece, house, new Vector3(localX, y, width / 2f), Quaternion.identity, wallMat);
                PlaceTextured($"Wall_{wallSet}_Straight", house, new Vector3(localX, y, -width / 2f), Quaternion.Euler(0, 180, 0), wallMat);
            }
            string cornerPiece = $"Corner_Exterior_{(isGround ? "Brick" : "Wood")}";
            PlaceTextured(cornerPiece, house, new Vector3(width / 2f, y, 0f), Quaternion.identity, wallMat);
            PlaceTextured(cornerPiece, house, new Vector3(-width / 2f, y, 0f), Quaternion.Euler(0, 180, 0), wallMat);
            if (hasBalcony && floor == floors - 2 && floors >= 2)
            {
                PlaceTextured("Balcony_Simple_Straight", house, new Vector3(0f, y + FLOOR_HEIGHT * 0.5f, width / 2f + 0.3f), Quaternion.identity, "wood");
                PlaceTextured("Overhang_Plaster_Long", house, new Vector3(0f, y, width / 2f + 0.15f), Quaternion.identity, "plaster");
            }
        }
        float topY = floors * FLOOR_HEIGHT;
        string roofPiece = PickClosestRoof(widthModules);
        PlaceTextured(roofPiece, house, new Vector3(0f, topY, 0f), Quaternion.identity, "roof");
        // Crista de metal ornamentada na cumeeira (como na foto de referência)
        PlaceTextured("Prop_MetalFence_Ornament", house, new Vector3(0f, topY + 1.4f, 0f), Quaternion.identity, "wood");
        PlaceTextured("Prop_Chimney", house, new Vector3(width / 2f - 0.3f, topY + 0.4f, -width / 2f + 0.3f), Quaternion.identity, "brick");
        if (rng.NextDouble() > 0.3)
            PlaceTextured("Prop_Vine1", house, new Vector3(width / 2f - 0.2f, FLOOR_HEIGHT, width / 2f), Quaternion.identity, "wood");
        if (hasStairs)
        {
            PlaceTextured("Stairs_Exterior_Straight", house, new Vector3(0f, 0f, width / 2f + 1.6f), Quaternion.identity, "stone");
        }
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
        GameObject player = new GameObject("Jogador");
        player.transform.SetParent(root.transform);
        player.transform.position = new Vector3(0f, 1f, 13f);
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.radius = 0.35f;
        GameObject camGO = new GameObject("Camera_Jogador");
        camGO.transform.SetParent(player.transform);
        camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Camera cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.gameObject != camGO)
            Object.DestroyImmediate(mainCam.gameObject);
        cam.tag = "MainCamera";
        var controllerType = Type.GetType("SimpleFirstPersonController");
        if (controllerType != null)
            player.AddComponent(controllerType);
        else
            Debug.LogWarning("SimpleFirstPersonController.cs não encontrado — adicione manualmente ao Jogador.");
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
        if (instance == null) instance = Object.Instantiate(prefab);
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