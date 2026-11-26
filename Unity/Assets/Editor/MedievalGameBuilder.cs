// MedievalGameBuilder.cs
// Gera um vilarejo medieval exploravel com os modelos e mapas de textura do pacote MedievalVillage.
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
    private const string SCENE_PATH = "Assets/Scenes/BATTLE_CLUBS_Medieval_Exploration.unity";
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
        RenderSettings.ambientSkyColor = new Color(.55f,.70f,.86f);
        RenderSettings.ambientEquatorColor = new Color(.52f,.57f,.43f);
        RenderSettings.ambientGroundColor = new Color(.20f,.24f,.17f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.68f,.75f,.72f); RenderSettings.fogDensity = .0018f;
        RenderSettings.skybox = null;

        GameObject world = new GameObject("MEDIEVAL_WORLD");
        SetupLighting(world);
        BuildTerrain(world);
        BuildWindingRoads(world);
        BuildVillage(world);
        BuildLandscape(world);
        BuildPlayer(world);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, SCENE_PATH)) throw new Exception("Falha ao salvar " + SCENE_PATH);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SCENE_PATH, true) };
        Debug.Log("[MEDIEVAL WORLD] Cena salva: " + SCENE_PATH + " | modelos ausentes: " + missing + " | casas: 10 | mapa: 180 x 180 m");
        Selection.activeGameObject = world;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    public static void ExportScenePackage()
    {
        if (!File.Exists(SCENE_PATH)) throw new Exception("A cena ainda nao foi gerada: " + SCENE_PATH);
        string outputFolder = "Build/MedievalVillage";
        Directory.CreateDirectory(outputFolder);
        string output = outputFolder + "/BATTLE_CLUBS_Medieval_Exploration.unitypackage";
        var assets = new List<string> { SCENE_PATH };
        foreach (string folder in new[] { ROOT + "/FBX", ROOT + "/OBJ", ROOT + "/Textures", ROOT + "/GeneratedMaterials", "Assets/Scripts" })
            if (AssetDatabase.IsValidFolder(folder)) assets.Add(folder);
        AssetDatabase.ExportPackage(assets.ToArray(), output, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
        if (!File.Exists(output) || new FileInfo(output).Length == 0) throw new Exception("O pacote Unity nao foi exportado.");
        Debug.Log("[MEDIEVAL WORLD] Pacote exportado: " + output);
    }

    private static void SetupLighting(GameObject root)
    {
        var sun = new GameObject("Sol_Medieval"); sun.transform.SetParent(root.transform);
        var light = sun.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.25f;
        light.color = new Color(1f,.91f,.78f); sun.transform.rotation = Quaternion.Euler(48f,-32f,0f);
        var fill = new GameObject("Luz_Ambiente"); fill.transform.SetParent(root.transform);
        var point = fill.AddComponent<Light>(); point.type = LightType.Directional; point.intensity = .22f;
        point.color = new Color(.70f,.80f,1f); fill.transform.rotation = Quaternion.Euler(25f,145f,0f);
    }

    private static void BuildTerrain(GameObject root)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Terreno_Grama_180m"; ground.transform.SetParent(root.transform,false);
        ground.transform.position = new Vector3(0,-.16f,0); ground.transform.localScale = new Vector3(18f,1f,18f);
        Apply(ground, "grass");
        ground.GetComponent<Collider>().name = "Colisao_Terreno";
    }

    private static void BuildWindingRoads(GameObject root)
    {
        var roads = new GameObject("Caminhos_Pedra_Modular"); roads.transform.SetParent(root.transform,false);
        // Caminho principal em curva, feito com pisos reais do kit, nao com uma faixa de cor.
        for (int i=0;i<62;i++)
        {
            float z=-72f+i*2.35f;
            float x=Mathf.Sin(i*.105f)*10f;
            Place(i%5==0?"Floor_Brick":"Floor_UnevenBrick",roads,new Vector3(x,.015f,z),
                Quaternion.Euler(0,Mathf.Sin(i*.105f)*9f,0),"stone",true);
            if(i%2==0)
                Place("Floor_UnevenBrick",roads,new Vector3(x+1.75f,.015f,z),
                    Quaternion.Euler(0,Mathf.Sin(i*.105f)*9f,0),"stone",true);
        }
        // Pequenos ramais ligam as casas a rua principal.
        for(int branch=0;branch<4;branch++)
        {
            float z=-38f+branch*25f;
            for(int j=0;j<7;j++)
            {
                float x=(branch%2==0?-1f:1f)*(12f+j*2f);
                Place("Floor_UnevenBrick",roads,new Vector3(x,.012f,z+j*1.4f),
                    Quaternion.Euler(0,branch%2==0?35f:-35f,0),"stone",true);
            }
        }
        var square=new GameObject("Praca_Central_Pedra"); square.transform.SetParent(root.transform,false);
        for(int x=-3;x<=3;x++) for(int z=-2;z<=2;z++)
            Place((x+z)%4==0?"Floor_Brick":"Floor_UnevenBrick",square,
                new Vector3(x*2f,.02f,z*2f+2f),Quaternion.identity,"stone",true);
    }

    private static void BuildVillage(GameObject world)
    {
        BuildHouse(world,"Casa_01_Casao_do_Senhor",new Vector3(-15,0,-18),3,true,true,true);
        BuildHouse(world,"Casa_02_Mercador",new Vector3(16,0,-30),2,true,false,false);
        BuildHouse(world,"Casa_03_Ferreiro",new Vector3(-29,0,2),2,false,true,true);
        BuildHouse(world,"Casa_04_Taverna",new Vector3(29,0,10),2,true,true,false);
        BuildHouse(world,"Casa_05_Pedra",new Vector3(-34,0,34),2,false,false,true);
        BuildHouse(world,"Casa_06_Madeira",new Vector3(31,0,43),2,true,false,false);
        BuildHouse(world,"Casa_07_Campones",new Vector3(-12,0,57),2,false,false,false);
        BuildHouse(world,"Casa_08_Artesao",new Vector3(13,0,67),2,true,false,true);
        BuildHouse(world,"Casa_09_Entrada",new Vector3(-48,0,-48),2,false,false,false);
        BuildHouse(world,"Casa_10_Moinho_Anexo",new Vector3(49,0,-48),2,false,false,false);
        BuildMarketAndProps(world);
    }

    private static void BuildHouse(GameObject world,string name,Vector3 origin,int floors,bool balcony,bool stairs,bool brickGround)
    {
        var house=new GameObject(name); house.transform.SetParent(world.transform,false); house.transform.position=origin;
        const float storey=3.12f;
        string lowerWall=brickGround?"Wall_UnevenBrick_Straight":"Wall_Plaster_Straight";
        for(int f=0;f<floors;f++)
        {
            float y=f*storey; bool ground=f==0;
            string wall=ground?lowerWall:"Wall_Plaster_Straight";
            string finish=ground?(brickGround?"brick":"plaster"):"plaster";
            // Frente e fundo: portas e janelas sao pecas de parede do proprio kit.
            for(int i=0;i<3;i++)
            {
                float x=(i-1)*2f;
                string front=ground && i==1
                    ? (brickGround?"Wall_UnevenBrick_Door_Flat":"Wall_Plaster_Door_Flat")
                    : (ground?"Wall_UnevenBrick_Window_Wide_Flat":"Wall_Plaster_Window_Wide_Flat");
                Place(front,house,new Vector3(x,y+1.56f,2f),Quaternion.identity,finish,true);
                Place(wall,house,new Vector3(x,y+1.56f,-2f),Quaternion.Euler(0,180,0),finish,true);
                if(i!=1 || !ground)
                {
                    Place("WindowShutters_Wide_Flat_Closed",house,new Vector3(x,y+1.6f,2.16f),Quaternion.identity,"wood",false);
                }
            }
            for(int side=-1;side<=1;side+=2)
                for(int j=0;j<2;j++)
                    Place(wall,house,new Vector3(side*3f,y+1.56f,(j==0?-1f:1f)),
                        Quaternion.Euler(0,side<0?90:-90,0),finish,true);
            string floorModel=ground?"Floor_UnevenBrick":"Floor_WoodDark";
            for(int fx=-1;fx<=1;fx++) for(int fz=-1;fz<=0;fz++)
                Place(floorModel,house,new Vector3(fx*2f,y+.025f,fz*2f),
                    Quaternion.identity,ground?"stone":"wood",true);
            if(balcony && f==floors-2)
            {
                Place("Balcony_Simple_Straight",house,new Vector3(0,y+.48f,2.55f),Quaternion.identity,"wood",true);
                Place("Balcony_Simple_Corner",house,new Vector3(-2,y+.48f,2.55f),Quaternion.identity,"wood",false);
            }
            if(f>0)
            {
                Place("Prop_Vine1",house,new Vector3(-2.8f,y-.25f,2.05f),Quaternion.identity,"vine",false);
                Place("Prop_Vine4",house,new Vector3(2.8f,y-.25f,2.05f),Quaternion.identity,"vine",false);
            }
        }
        // Telhado completo fica sobre o ultimo pavimento; cumeeira e testeira fecham as pontas.
        string roof=floors==3?"Roof_RoundTiles_6x8":"Roof_RoundTiles_6x6";
        Place(roof,house,new Vector3(0,floors*storey+1.42f,0),Quaternion.identity,"roof",false);
        Place("Roof_Front_Brick6",house,new Vector3(0,floors*storey+1.35f,2.75f),Quaternion.identity,"brick",false);
        Place("Roof_Front_Brick6",house,new Vector3(0,floors*storey+1.35f,-2.75f),Quaternion.Euler(0,180,0),"brick",false);
        Place("Prop_Chimney",house,new Vector3(2.05f,floors*storey+1.9f,-1.1f),Quaternion.identity,"stone",false);
        if(stairs) Place("Stairs_Exterior_Straight",house,new Vector3(0,.5f,3.55f),Quaternion.identity,"stone",true);
    }

    private static void BuildMarketAndProps(GameObject world)
    {
        var market=new GameObject("Mercado_e_Vida_da_Vila"); market.transform.SetParent(world.transform,false);
        Place("Prop_Wagon",market,new Vector3(-5f,0,3f),Quaternion.Euler(0,24,0),"wood",true);
        Place("Prop_Crate",market,new Vector3(-3.4f,.3f,3f),Quaternion.identity,"wood",true);
        Place("Prop_Crate",market,new Vector3(-3.4f,.9f,3f),Quaternion.Euler(0,12,0),"wood",false);
        Place("Prop_Wagon",market,new Vector3(8f,0,22f),Quaternion.Euler(0,-18,0),"wood",true);
        Place("Prop_Vine5",market,new Vector3(-15f,1.8f,-18f),Quaternion.identity,"vine",false);
        Place("Prop_Vine6",market,new Vector3(16f,1.8f,-30f),Quaternion.identity,"vine",false);
        var fence=new GameObject("Cercas_e_Limites"); fence.transform.SetParent(market.transform,false);
        for(int i=0;i<16;i++)
        {
            float z=-15+i*2.2f;
            Place("Prop_WoodenFence_Single",fence,new Vector3(-43f,0,z),Quaternion.identity,"wood",true);
            Place("Prop_WoodenFence_Single",fence,new Vector3(43f,0,z),Quaternion.Euler(0,180,0),"wood",true);
        }
    }

    private static void BuildLandscape(GameObject world)
    {
        var nature=new GameObject("Natureza_Arvores_e_Grama"); nature.transform.SetParent(world.transform,false);
        // Arvores simples de baixo custo para dar escala e enquadrar a area exploravel.
        for(int i=0;i<54;i++)
        {
            float a=i*2.39996f, radius=61f+(i%5)*5f;
            Vector3 p=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
            var tree=new GameObject("Arvore_"+i); tree.transform.SetParent(nature.transform,false); tree.transform.position=p;
            var trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder); trunk.name="Tronco"; trunk.transform.SetParent(tree.transform,false);
            trunk.transform.localPosition=new Vector3(0,2.0f,0); trunk.transform.localScale=new Vector3(.38f,2f,.38f);
            var bark=GetMaterial("wood"); if(bark!=null) trunk.GetComponent<Renderer>().sharedMaterial=bark;
            var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere); crown.name="Copa"; crown.transform.SetParent(tree.transform,false);
            crown.transform.localPosition=new Vector3(0,4.7f,0); crown.transform.localScale=new Vector3(3.4f,4.2f,3.4f);
            crown.GetComponent<Renderer>().sharedMaterial=GetNatureMaterial();
            UnityEngine.Object.DestroyImmediate(crown.GetComponent<Collider>());
        }
        // Cercas, vegetacao e vinhas mantem a vila integrada na paisagem.
        for(int i=0;i<22;i++)
        {
            float x=-48f+i*4.5f;
            Place("Prop_Vine9",nature,new Vector3(x,0,-58f),Quaternion.identity,"vine",false);
        }
    }

    private static void BuildPlayer(GameObject world)
    {
        var player=new GameObject("Jogador_Explorador"); player.transform.SetParent(world.transform,false);
        player.transform.position=new Vector3(0,1.2f,-78f);
        var cc=player.AddComponent<CharacterController>(); cc.height=1.8f; cc.radius=.34f; cc.center=new Vector3(0,.9f,0);
        var visual=GameObject.CreatePrimitive(PrimitiveType.Capsule); visual.name="Humanoide_Provisorio";
        visual.transform.SetParent(player.transform,false); visual.transform.localPosition=new Vector3(0,0,0);
        UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
        var camRoot=new GameObject("CameraPivot"); camRoot.transform.SetParent(player.transform,false); camRoot.transform.localPosition=new Vector3(0,1.5f,0);
        var camObj=new GameObject("Main Camera"); camObj.tag="MainCamera"; camObj.transform.SetParent(camRoot.transform,false);
        camObj.transform.localPosition=new Vector3(0,.5f,-5.5f); camObj.transform.localRotation=Quaternion.identity;
        camObj.AddComponent<Camera>(); camObj.AddComponent<AudioListener>();
        player.AddComponent<MedievalThirdPersonController>();
    }

    private static Material GetNatureMaterial()
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null)shader=Shader.Find("Standard");
        var mat=new Material(shader); mat.name="Verde_Folhagem_Procedural";
        if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",new Color(.19f,.34f,.12f));
        if(mat.HasProperty("_Color"))mat.SetColor("_Color",new Color(.19f,.34f,.12f));
        return mat;
    }

    private static GameObject Place(string name,GameObject parent,Vector3 localPos,Quaternion localRot,string material,bool collider)
    {
        var prefab=FindModel(name);
        if(prefab==null){missing++;return null;}
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
        if(Models.TryGetValue(name,out var cached))return cached;
        string[] modelFolders={ROOT+"/FBX",ROOT+"/OBJ"};
        foreach(string folder in modelFolders)
        {
            if(!AssetDatabase.IsValidFolder(folder))continue;
            string[] ids=AssetDatabase.FindAssets(name+" t:Model",new[]{folder});
            foreach(string id in ids)
            {
                string path=AssetDatabase.GUIDToAssetPath(id);
                if(!Path.GetFileNameWithoutExtension(path).Equals(name,StringComparison.OrdinalIgnoreCase))continue;
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(model!=null){Models[name]=model;return model;}
            }
        }
        Models[name]=null;
        Debug.LogWarning("[MedievalWorld] Modelo nao encontrado em FBX/OBJ: "+name);
        return null;
    }

    private static bool GetBounds(GameObject go,out Bounds b)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length==0){b=new Bounds(go.transform.position,Vector3.zero);return false;}
        b=rs[0].bounds; for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds); return true;
    }

    private static void AddModelColliders(GameObject go)
    {
        foreach(var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if(mf.sharedMesh==null||mf.GetComponent<Collider>()!=null)continue;
            var mc=mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh=mf.sharedMesh;
        }
    }

    private static void Apply(GameObject go,string key)
    {
        Material m=GetMaterial(key); if(m==null)return;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=m;
    }

    private static Material GetMaterial(string key)
    {
        if(Mats.TryGetValue(key,out var cached))return cached;
        string baseName=key switch {
            "grass"=>"T_Noise_Terrain","roof"=>"T_RoundTiles_BaseColor","wood"=>"T_WoodTrim_BaseColor",
            "plaster"=>"T_Plaster_BaseColor","stone"=>"T_RockTrim_BaseColor","brick"=>"T_UnevenBrick_BaseColor",
            "redbrick"=>"T_RedBrick_BaseColor","vine"=>"T_VineLeaf",
            _=>"T_Brick_BaseColor"};
        string basePath=FindTexturePath(baseName);
        if(string.IsNullOrEmpty(basePath)){Debug.LogWarning("[MedievalWorld] Textura ausente: "+baseName);return null;}
        Texture2D tex=AssetDatabase.LoadAssetAtPath<Texture2D>(basePath);
        string normalName=baseName.Replace("BaseColor","Normal");
        string normalPath=FindTexturePath(normalName);
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
        if(normal!=null&&mat.HasProperty("_BumpMap")){mat.SetTexture("_BumpMap",normal);mat.EnableKeyword("_NORMALMAP");}
        if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",key=="grass"?new Color(.30f,.52f,.20f):Color.white);
        if(mat.HasProperty("_Color"))mat.SetColor("_Color",key=="grass"?new Color(.30f,.52f,.20f):Color.white);
        mat.mainTexture=tex;
        if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",.12f);
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
