// MedievalGameBuilder.cs
// Gera o cenário de exploração medieval usando somente modelos e texturas existentes no MedievalVillage.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MedievalGameBuilder
{
    private const string ROOT = "Assets/MedievalVillage";
    private static readonly Dictionary<string, GameObject> Models = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
    private static int missing;

    [MenuItem("Vilarejo/Jogo Completo/Gerar Mundo Medieval")]
    public static void BuildWorld()
    {
        Models.Clear(); Mats.Clear(); missing = 0;
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.55f,.68f,.78f);
        RenderSettings.ambientEquatorColor = new Color(.48f,.48f,.40f);
        RenderSettings.ambientGroundColor = new Color(.22f,.25f,.19f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.62f,.69f,.67f); RenderSettings.fogDensity = .0035f;

        GameObject world = new GameObject("MEDIEVAL_WORLD");
        SetupLighting(world);
        BuildGround(world);
        BuildRoads(world);
        BuildVillage(world);
        BuildPlayer(world);

        EditorSceneManager.MarkSceneDirty(scene);
        const string scenePath = "Assets/Scenes/BATTLE_CLUBS_Medieval_Exploration.unity";
        if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new Exception("Falha ao salvar " + scenePath);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        Debug.Log("[MEDIEVAL WORLD] Cena salva: " + scenePath + " | modelos ausentes: " + missing);
        Selection.activeGameObject = world;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    private static void SetupLighting(GameObject root)
    {
        var sun = new GameObject("Sol_Medieval"); sun.transform.SetParent(root.transform);
        var light = sun.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.15f;
        light.color = new Color(1f,.88f,.72f); sun.transform.rotation = Quaternion.Euler(48f,-32f,0f);
    }

    private static void BuildGround(GameObject root)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Terreno_Grama"; ground.transform.SetParent(root.transform,false);
        ground.transform.position = new Vector3(0,-.12f,38f); ground.transform.localScale = new Vector3(12f,1f,15f);
        Apply(ground, "plaster");
        ground.GetComponent<Collider>().name = "Colisao_Terreno";
    }

    private static void BuildRoads(GameObject root)
    {
        Plane(root,"Estrada_Principal",new Vector3(0,-.045f,34f),new Vector3(1.15f,1f,8.5f),"brick");
        Plane(root,"Praca_Central",new Vector3(0,-.035f,26f),new Vector3(2.8f,1f,1.8f),"stone");
        Plane(root,"Caminho_Lateral_Esquerdo",new Vector3(-13,-.04f,40f),new Vector3(.65f,1f,5.5f),"stone");
        Plane(root,"Caminho_Lateral_Direito",new Vector3(13,-.04f,43f),new Vector3(.65f,1f,5.5f),"stone");
    }

    private static void Plane(GameObject parent,string name,Vector3 pos,Vector3 scale,string mat)
    {
        var p=GameObject.CreatePrimitive(PrimitiveType.Plane); p.name=name; p.transform.SetParent(parent.transform,false);
        p.transform.localPosition=pos; p.transform.localScale=scale; Apply(p,mat);
    }

    private static void BuildVillage(GameObject world)
    {
        // Casa principal de três andares; demais casas usam a mesma linguagem arquitetônica.
        BuildHouse(world,"Casa_01_Grande",new Vector3(0,0,22),3,true,true);
        BuildHouse(world,"Casa_02_Mercador",new Vector3(-13,0,34),2,true,false);
        BuildHouse(world,"Casa_03_Artesao",new Vector3(13,0,37),2,false,false);
        BuildHouse(world,"Casa_04_Pedra",new Vector3(-15,0,53),2,false,true);
        BuildHouse(world,"Casa_05_Madeira",new Vector3(14,0,57),2,true,false);
        BuildHouse(world,"Casa_06_Pequena",new Vector3(0,0,69),2,false,false);
        BuildProps(world);
        BuildFences(world);
    }

    private static void BuildHouse(GameObject world,string name,Vector3 origin,int floors,bool balcony,bool stairs)
    {
        var house=new GameObject(name); house.transform.SetParent(world.transform,false); house.transform.position=origin;
        const float step=3.12f;
        for(int f=0;f<floors;f++)
        {
            float y=f*step; bool ground=f==0;
            string wall=ground?"Wall_UnevenBrick":"Wall_Plaster";
            string material=ground?"brick":"plaster";
            // Frente e fundo: módulos originais de 2m do pacote.
            for(int i=0;i<3;i++)
            {
                float x=(i-1)*2f;
                string front=ground?(i==1?"Wall_UnevenBrick_Door_Flat":"Wall_UnevenBrick_Straight"):
                    (i==1?"Wall_Plaster_Window_Wide_Flat":"Wall_Plaster_Window_Thin_Round");
                Place(front,house,new Vector3(x,y+1.56f,2f),Quaternion.identity,material,ground);
                Place(wall+"_Straight",house,new Vector3(x,y+1.56f,-2f),Quaternion.Euler(0,180,0),material,true);
            }
            // Laterais com peças de parede reais, sem paredes improvisadas.
            for(int side=-1;side<=1;side+=2)
                for(int j=0;j<2;j++)
                    Place(wall+"_Straight",house,new Vector3(side*3f,y+1.56f,(j==0?-1f:1f)),
                        Quaternion.Euler(0,side<0?90:-90,0),material,true);

            // Pisos planos recebem a textura de madeira/pedra disponível.
            Plane(house,"Piso_"+f,new Vector3(0,y+.005f,0),new Vector3(.30f,1f,.20f),ground?"stone":"wood");
            if(balcony && f==floors-2)
            {
                Place("Balcony_Simple_Straight",house,new Vector3(0,y+.42f,2.45f),Quaternion.identity,"wood",true);
                Place("Balcony_Simple_Corner",house,new Vector3(-2,y+.42f,2.45f),Quaternion.identity,"wood",false);
            }
            if(f>0)
            {
                Place("Prop_Vine1",house,new Vector3(-2.8f,y-.2f,2.0f),Quaternion.identity,"wood",false);
                if(f==floors-1) Place("Prop_Vine2",house,new Vector3(2.8f,y-.2f,2.0f),Quaternion.identity,"wood",false);
            }
        }
        Place(floors==3?"Roof_RoundTiles_6x8":"Roof_RoundTiles_6x6",house,
            new Vector3(0,floors*step+1.7f,0),Quaternion.identity,"roof",false);
        Place("Prop_Chimney",house,new Vector3(2.05f,floors*step+2.0f,-1.1f),Quaternion.identity,"stone",false);
        if(stairs) Place("Stairs_Exterior_Straight",house,new Vector3(0,.48f,3.35f),Quaternion.identity,"stone",true);
    }

    private static void BuildProps(GameObject world)
    {
        var props=new GameObject("Decoracao_Vila"); props.transform.SetParent(world.transform,false);
        Place("Prop_Wagon",props,new Vector3(-4f,0,19f),Quaternion.Euler(0,24,0),"wood",true);
        Place("Prop_Crate",props,new Vector3(-2.7f,.35f,19f),Quaternion.identity,"wood",false);
        Place("Prop_Crate",props,new Vector3(-2.7f,.95f,19f),Quaternion.Euler(0,12,0),"wood",false);
        Place("Prop_Wagon",props,new Vector3(10f,0,48f),Quaternion.Euler(0,-18,0),"wood",false);
        Place("Prop_Vine4",props,new Vector3(-13f,1.6f,34f),Quaternion.identity,"wood",false);
        Place("Prop_Vine5",props,new Vector3(13f,1.6f,37f),Quaternion.identity,"wood",false);
    }

    private static void BuildFences(GameObject world)
    {
        var fence=new GameObject("Cercas_Madeira"); fence.transform.SetParent(world.transform,false);
        for(int i=0;i<8;i++)
        {
            float z=27+i*1.8f;
            Place("Prop_WoodenFence_Single",fence,new Vector3(-6.5f,0,z),Quaternion.identity,"wood",false);
            Place("Prop_WoodenFence_Single",fence,new Vector3(6.5f,0,z),Quaternion.Euler(0,180,0),"wood",false);
        }
    }

    private static void BuildPlayer(GameObject world)
    {
        var player=new GameObject("Jogador_Explorador"); player.transform.SetParent(world.transform,false);
        player.transform.position=new Vector3(0,1f,8f);
        var cc=player.AddComponent<CharacterController>(); cc.height=1.8f; cc.radius=.34f; cc.center=new Vector3(0,.9f,0);
        var visual=GameObject.CreatePrimitive(PrimitiveType.Capsule); visual.name="Visual_Provisorio_Explorador";
        visual.transform.SetParent(player.transform,false); visual.transform.localPosition=new Vector3(0,0,0);
        UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
        var camRoot=new GameObject("CameraPivot"); camRoot.transform.SetParent(player.transform,false); camRoot.transform.localPosition=new Vector3(0,1.5f,0);
        var camObj=new GameObject("Main Camera"); camObj.tag="MainCamera"; camObj.transform.SetParent(camRoot.transform,false);
        camObj.transform.localPosition=new Vector3(0,.5f,-5.5f); camObj.transform.localRotation=Quaternion.identity;
        camObj.AddComponent<Camera>(); camObj.AddComponent<AudioListener>();
        player.AddComponent<MedievalThirdPersonController>();
    }

    private static GameObject Place(string name,GameObject parent,Vector3 localPos,Quaternion localRot,string material,bool collider)
    {
        var prefab=FindModel(name);
        if(prefab==null){missing++;Debug.LogWarning("[MedievalWorld] Modelo não encontrado: "+name);return null;}
        var instance=PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if(instance==null) instance=UnityEngine.Object.Instantiate(prefab);
        instance.name=name; instance.transform.SetParent(parent.transform,false); instance.transform.localRotation=localRot;
        Vector3 target=parent.transform.TransformPoint(localPos);
        Bounds b; if(GetBounds(instance,out b)) instance.transform.position+=target-b.center;
        else instance.transform.localPosition=localPos;
        Apply(instance,material);
        if(collider) AddModelColliders(instance);
        return instance;
    }

    private static GameObject FindModel(string name)
    {
        if(Models.TryGetValue(name,out var cached)) return cached;
        string[] ids=AssetDatabase.FindAssets(name+" t:Model",new[]{ROOT});
        foreach(string id in ids)
        {
            string path=AssetDatabase.GUIDToAssetPath(id);
            if(Path.GetFileNameWithoutExtension(path).Equals(name,StringComparison.OrdinalIgnoreCase))
            { var model=AssetDatabase.LoadAssetAtPath<GameObject>(path); Models[name]=model; return model; }
        }
        Models[name]=null; return null;
    }

    private static bool GetBounds(GameObject go,out Bounds b)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length==0){b=new Bounds(go.transform.position,Vector3.zero);return false;}
        b=rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds); return true;
    }

    private static void AddModelColliders(GameObject go)
    {
        foreach(var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if(mf.sharedMesh==null || mf.GetComponent<Collider>()!=null) continue;
            var mc=mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh=mf.sharedMesh;
        }
    }

    private static void Apply(GameObject go,string key)
    {
        Material m=GetMaterial(key); if(m==null)return;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial=m;
    }

    private static Material GetMaterial(string key)
    {
        if(Mats.TryGetValue(key,out var cached))return cached;
        string baseName=key switch {
            "roof"=>"T_RoundTiles_BaseColor","wood"=>"T_WoodTrim_BaseColor",
            "plaster"=>"T_Plaster_BaseColor","stone"=>"T_RockTrim_BaseColor",
            _=>"T_UnevenBrick_BaseColor"};
        string basePath=FindTexturePath(baseName), normalPath=FindTexturePath(baseName.Replace("BaseColor","Normal"));
        if(string.IsNullOrEmpty(basePath)){Debug.LogWarning("[MedievalWorld] Textura ausente: "+baseName);return null;}
        Texture2D tex=AssetDatabase.LoadAssetAtPath<Texture2D>(basePath);
        Texture2D normal=string.IsNullOrEmpty(normalPath)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        string folder=ROOT+"/GeneratedMaterials";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(ROOT,"GeneratedMaterials");
        string matPath=folder+"/MV_"+key+".mat";
        Material mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null)shader=Shader.Find("Standard");
        if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}
        mat.shader=shader;
        if(mat.HasProperty("_BaseMap"))mat.SetTexture("_BaseMap",tex);
        if(mat.HasProperty("_MainTex"))mat.SetTexture("_MainTex",tex);
        if(normal!=null && mat.HasProperty("_BumpMap")){mat.SetTexture("_BumpMap",normal);mat.EnableKeyword("_NORMALMAP");}
        mat.mainTexture=tex; mat.SetFloat("_Glossiness",.12f);
        EditorUtility.SetDirty(mat); Mats[key]=mat; return mat;
    }

    private static string FindTexturePath(string name)
    {
        string[] ids=AssetDatabase.FindAssets(name+" t:Texture2D",new[]{ROOT});
        foreach(string id in ids){string p=AssetDatabase.GUIDToAssetPath(id);if(Path.GetFileNameWithoutExtension(p).Equals(name,StringComparison.OrdinalIgnoreCase))return p;}
        return null;
    }
}
#endif
