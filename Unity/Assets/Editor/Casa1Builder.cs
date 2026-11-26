// Casa1Builder.cs
// Constrói somente a primeira casa do vilarejo com modelos FBX/glTF e texturas
// que já existam em Assets/MedievalVillage. Não cria paredes substitutas em cubos.
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class Casa1Builder
{
    private const string ASSET_ROOT = "Assets/MedievalVillage";
    private const float MODULE = 2f;
    private const float FLOOR_STEP = 3.12f;
    private static readonly Dictionary<string, GameObject> Models =
        new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Material> Materials =
        new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

    [MenuItem("Vilarejo/Construir/Casa 1 - Casa grande")]
    public static void Build()
    {
        // Cena exclusiva da Casa 1; não altera nem recompila o APK.
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Models.Clear();
        Materials.Clear();

        GameObject old = GameObject.Find("Casa_01_Grande");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);

        GameObject root = new GameObject("Casa_01_Grande");
        // Planta: 6 m de largura x 4 m de profundidade; 3 andares.
        // Paredes são alinhadas pelo centro dos bounds reais dos modelos importados.
        BuildFloor(root, 0, "Wall_UnevenBrick", true);
        BuildFloor(root, 1, "Wall_Plaster", false);
        BuildFloor(root, 2, "Wall_Plaster", false);

        // Telhado de telhas redondas, centralizado sobre a construção.
        PlaceCentered("Roof_RoundTiles_6x8", root,
            new Vector3(0f, FLOOR_STEP * 3f + 1.65f, 0f), Quaternion.identity, "roof");

        // Sacada corrida na fachada do segundo andar.
        PlaceCentered("Balcony_Simple_Straight", root,
            new Vector3(0f, FLOOR_STEP + 0.35f, 2.25f), Quaternion.identity, "wood");

        // Escada frontal e elementos reais de decoração do pacote.
        PlaceCentered("Stairs_Exterior_Straight", root,
            new Vector3(0f, 0.45f, 3.25f), Quaternion.identity, "stone");
        PlaceCentered("Prop_Chimney", root,
            new Vector3(2.0f, FLOOR_STEP * 3f + 2.3f, -1.1f), Quaternion.identity, "stone");
        PlaceCentered("Prop_Vine1", root,
            new Vector3(-2.85f, 2.0f, 2.05f), Quaternion.identity, "wood");
        PlaceCentered("Prop_Vine2", root,
            new Vector3(2.85f, 4.8f, 1.95f), Quaternion.identity, "wood");
        PlaceCentered("Prop_Wagon", root,
            new Vector3(-4.2f, 0.35f, 3.0f), Quaternion.Euler(0f, 18f, 0f), "wood");
        PlaceCentered("Prop_Crate", root,
            new Vector3(-3.25f, 0.45f, 3.0f), Quaternion.identity, "wood");

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        const string scenePath = "Assets/Scenes/Casa_01_Grande.unity";
        if (!EditorSceneManager.SaveScene(scene, scenePath))
            throw new Exception("[Casa 1] Não foi possível salvar a cena: " + scenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Casa 1] Cena montada e salva em " + scenePath + ". Veja os avisos para peças ausentes.");
    }

    private static void BuildFloor(GameObject root, int floor, string wallSet, bool ground)
    {
        float y = floor * FLOOR_STEP;
        float wallCenterY = y + 1.56f;

        // Fachada frontal: três módulos de 2 m. Porta no centro do térreo;
        // janelas altas nos dois pisos superiores.
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * MODULE;
            string front;
            if (ground)
                front = i == 1 ? wallSet + "_Door_Flat" : wallSet + "_Straight";
            else
                front = i == 1 ? wallSet + "_Window_Wide_Flat" : wallSet + "_Window_Thin_Round";

            PlaceCentered(front, root, new Vector3(x, wallCenterY, 2f),
                Quaternion.identity, ground ? "brick" : "plaster");
            PlaceCentered(wallSet + "_Straight", root,
                new Vector3(x, wallCenterY, -2f), Quaternion.Euler(0f, 180f, 0f),
                ground ? "brick" : "plaster");
        }

        // Laterais: dois módulos por lado, rotacionados 90 graus.
        for (int side = 0; side < 2; side++)
        {
            float x = side == 0 ? -3f : 3f;
            float rot = side == 0 ? 90f : -90f;
            for (int j = 0; j < 2; j++)
            {
                float z = (j == 0 ? -1f : 1f);
                PlaceCentered(wallSet + "_Straight", root,
                    new Vector3(x, wallCenterY, z), Quaternion.Euler(0f, rot, 0f),
                    ground ? "brick" : "plaster");
            }
        }

        // Pisos usam apenas a peça de piso do pacote, se estiver presente.
        // Se não existir, não é criado um substituto artificial.
        for (int x = 0; x < 3; x++)
        for (int z = 0; z < 2; z++)
        {
            string floorModel = ground ? "Floor_UnevenBrick" : "Floor_WoodDark";
            PlaceCentered(floorModel, root,
                new Vector3((x - 1) * MODULE, y + 0.02f, (z == 0 ? -1f : 1f)),
                Quaternion.identity, ground ? "stone" : "wood", false);
        }
    }

    private static GameObject PlaceCentered(string modelName, GameObject parent,
        Vector3 targetCenter, Quaternion rotation, string materialKey, bool warn = true)
    {
        GameObject prefab = FindModel(modelName);
        if (prefab == null)
        {
            if (warn) Debug.LogWarning("[Casa 1] Peça não encontrada; não foi substituída: " + modelName);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) instance = UnityEngine.Object.Instantiate(prefab);
        instance.name = modelName;
        instance.transform.SetParent(parent.transform, true);
        instance.transform.rotation = rotation;

        Bounds bounds;
        if (TryGetBounds(instance, out bounds))
            instance.transform.position += targetCenter - bounds.center;
        else
            instance.transform.position = targetCenter;

        Material mat = GetMaterial(materialKey);
        if (mat != null)
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = mat;

        return instance;
    }

    private static bool TryGetBounds(GameObject go, out Bounds bounds)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) { bounds = new Bounds(go.transform.position, Vector3.zero); return false; }
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    private static GameObject FindModel(string name)
    {
        if (Models.TryGetValue(name, out GameObject cached)) return cached;
        string[] guids = AssetDatabase.FindAssets(name + " t:Model", new[] { ASSET_ROOT });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Models[name] = model;
                return model;
            }
        }
        Models[name] = null;
        return null;
    }

    private static Material GetMaterial(string key)
    {
        if (Materials.TryGetValue(key, out Material cached)) return cached;
        string baseName;
        switch (key)
        {
            case "roof": baseName = "T_RoundTiles_BaseColor"; break;
            case "wood": baseName = "T_WoodTrim_BaseColor"; break;
            case "plaster": baseName = "T_Plaster_BaseColor"; break;
            case "stone": baseName = "T_RockTrim_BaseColor"; break;
            default: baseName = "T_UnevenBrick_BaseColor"; break;
        }

        Texture2D baseMap = FindTexture(baseName);
        Texture2D normalMap = FindTexture(baseName.Replace("BaseColor", "Normal"));
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        Material material = new Material(shader) { name = "Casa1_" + key };
        if (baseMap != null)
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", baseMap);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", baseMap);
        }
        if (normalMap != null && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normalMap);
            material.EnableKeyword("_NORMALMAP");
        }
        if (baseMap == null)
            Debug.LogWarning("[Casa 1] Textura ausente: " + baseName + " em " + ASSET_ROOT + "/Textures");
        Materials[key] = material;
        return material;
    }

    private static Texture2D FindTexture(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Texture2D", new[] { ASSET_ROOT });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).Equals(name, StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return null;
    }
}
