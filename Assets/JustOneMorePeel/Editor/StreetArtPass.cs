using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JustOneMorePeel
{
    /// <summary>Focused art migration for the existing FirstShift scene. Does not rebuild gameplay.</summary>
    public static class StreetArtPass
    {
        const string Art = "Assets/JustOneMorePeel/Art";
        const string Generated = Art + "/Integrated";
        static readonly string[] Notices = {"01_LostCat", "02_Cinema", "03_QuietYard", "04_ShoeRepair",
            "05_Garage", "06_Neighbours", "07_OldWarning", "08_Electrician", "09_Plants", "10_OldCinema"};

        [MenuItem("Just One More Peel/Apply courtyard art pass")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != "Assets/JustOneMorePeel/Scenes/FirstShift.unity")
                throw new InvalidOperationException("Open FirstShift in Edit Mode first.");
            Directory.CreateDirectory(Generated);
            AssetDatabase.Refresh();
            var game = Object.FindAnyObjectByType<PeelGame>();
            if(Object.FindAnyObjectByType<PeelPosterStack>())
                throw new InvalidOperationException("Historical art pass: do not rebuild the live poster-stack scene.");
            if (!game || Object.FindObjectsByType<PeelStrip>().Length != 152)
                throw new InvalidOperationException("Expected the existing 152-strip collage.");

            foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[] {Art}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!importer) continue;
                importer.maxTextureSize = 1024;
                importer.textureType = path.Contains("_nor_gl_") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.alphaIsTransparency = path.Contains("/Notices/");
                importer.wrapMode = path.Contains("/Notices/") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
            var noticeMaterials = Notices.Select(n => Surface(n, Color.white, Art + "/Notices/" + n + ".png", null, true)).ToArray();
            game.papers = noticeMaterials;
            ApplyNotices(noticeMaterials);

            var wall = GameObject.Find("Work wall");
            wall.GetComponent<Renderer>().sharedMaterial = Surface("Worn painted plaster", new Color(.82f,.81f,.74f),
                Art+"/PolyHaven/painted_plaster_wall_diff_1k.jpg", Art+"/PolyHaven/painted_plaster_wall_nor_gl_1k.jpg");
            wall.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(5.5f,2.1f);
            wall.GetComponent<Renderer>().enabled = true;
            RebuildRetainedShell();

            var scraper = ToolPrefab("Scraper");
            var steamer = ToolPrefab("Steamer");
            var config = new SerializedObject(game);
            config.FindProperty("scraperViewPrefab").objectReferenceValue = scraper;
            config.FindProperty("steamerViewPrefab").objectReferenceValue = steamer;
            config.ApplyModifiedPropertiesWithoutUndo();

            var old = GameObject.Find("Integrated courtyard props");
            if(old) Object.DestroyImmediate(old);
            var props = new GameObject("Integrated courtyard props");
            var cart = Prop("tool_cart", "Tool workbench", new Vector3(-4.35f,0,1.2f),1.5f,props.transform);
            cart.AddComponent<PeelWorkbench>();
            var cartBounds = BoundsOf(cart);
            var display = (GameObject)PrefabUtility.InstantiatePrefab(scraper);
            display.name="Scraper on tray"; display.transform.SetParent(cart.transform,true);
            display.transform.position=new Vector3(-4.62f,cartBounds.max.y+.025f,1.12f);
            display.transform.rotation=Quaternion.Euler(90,0,-15);
            display = (GameObject)PrefabUtility.InstantiatePrefab(steamer);
            display.name="Steamer on tray";display.transform.SetParent(cart.transform,true);
            display.transform.position=new Vector3(-4.08f,cartBounds.max.y+.17f,1.18f);
            display.transform.rotation=Quaternion.Euler(0,0,-7);

            game.total = Object.FindObjectsByType<PeelStrip>().Length;
            EditorUtility.SetDirty(game);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("STREET_ART_APPLIED: 10 notices, plaster wall, CC0 cart and Blender tool prefabs; 152 strips retained.");
        }

        static void ApplyNotices(Material[] materials)
        {
            // Keep the owner's approved overlapping composition; select content and formats intentionally.
            int[] front = {0,3,4,1,7,5,2,9,8,4,0,3,1,2,5,6,8,7,9};
            int[] back = {6,4,3,9,6,4,9,6,4,6,8,6,9,3,4,9,6,4,6};
            float[] widths = {1.03f,.85f,1.06f,1.12f,.72f,.82f,1.02f,1.02f,.79f,.92f,.9f,.82f,.99f,1.02f,.84f,.82f,.87f,.71f,.92f};
            for (int i=0;i<19;i++)
            {
                var board=GameObject.Find("Street collage "+i);
                var position=board.transform.position;position.z=3.075f-i*.002f;board.transform.position=position;
                float aspect=(float)materials[front[i]].mainTexture.width/materials[front[i]].mainTexture.height;
                foreach(var strip in board.GetComponentsInChildren<PeelStrip>())
                {
                    bool top=strip.covering==null;
                    strip.width=widths[i]*1.12f/4;strip.height=widths[i]*1.12f/aspect;
                    var p=strip.transform.localPosition;
                    p.x=((strip.uvLeft*4)-1.5f)*strip.width;
                    strip.transform.localPosition=p;
                    strip.GetComponent<Renderer>().sharedMaterial=materials[top?front[i]:back[i]];
                    strip.Prepare();EditorUtility.SetDirty(strip);
                }
            }
        }

        static Material Surface(string name, Color color, string diffuse=null, string normal=null, bool cutout=false)
        {
            string path=Generated+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) { material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path); }
            material.color=color;material.SetFloat("_Smoothness",.08f);
            if(diffuse!=null) material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse));
            if(normal!=null) {material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));material.SetFloat("_BumpScale",.35f);material.EnableKeyword("_NORMALMAP");}
            if(cutout) {material.SetFloat("_Cull",0);material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.5f);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;}
            EditorUtility.SetDirty(material);return material;
        }

        public static GameObject ToolPrefab(string name)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Tools/"+name+".fbx");
            if(!source) throw new InvalidOperationException("Missing Blender model: "+name);
            var root=new GameObject(name);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
            // Source shapes use a Y-up authoring convention; FBX converts Blender's Z-up axis.
            model.transform.localRotation=Quaternion.Euler(90,0,0)*model.transform.localRotation;
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>ToolMaterial(m.name)).ToArray();
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Generated+"/"+name+".prefab");
            Object.DestroyImmediate(root);return prefab;
        }
        static Material ToolMaterial(string name)
        {
            Color color;float metallic=0,smoothness=.15f;
            switch(name)
            {
                case "Steel":color=new Color(.48f,.5f,.46f);metallic=.7f;smoothness=.4f;break;
                case "Edge":color=new Color(.67f,.68f,.62f);metallic=.8f;smoothness=.48f;break;
                case "Enamel":color=new Color(.64f,.56f,.36f);metallic=.15f;break;
                case "Grip":color=new Color(.055f,.16f,.15f);break;
                case "CardPlastic":color=new Color(.75f,.73f,.63f);break;
                default:color=new Color(.033f,.041f,.037f);break;
            }
            var m=Surface("Tool "+name,color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);
            if(name=="CardPlastic")m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Tools/CardFace.png"));
            return m;
        }

        static GameObject Prop(string id,string name,Vector3 feet,float width,Transform parent)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/PolyHaven/"+id+"_1k.fbx");
            if(!asset)throw new InvalidOperationException("Missing CC0 model: "+id);
            var root=new GameObject(name);root.transform.SetParent(parent,false);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(asset);model.transform.SetParent(root.transform,false);
            var material=Surface(id,Color.white,Art+"/PolyHaven/"+id+"_diff_1k.jpg",Art+"/PolyHaven/"+id+"_nor_gl_1k.jpg");
            foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;
            var bounds=BoundsOf(root);
            root.transform.localScale=Vector3.one*(width/bounds.size.x);
            if(id=="painted_wooden_bench")
                root.transform.localScale=new Vector3(root.transform.localScale.x,root.transform.localScale.y*.75f,root.transform.localScale.z);
            bounds=BoundsOf(root);root.transform.position=feet-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            bounds=BoundsOf(root);
            var collider=root.AddComponent<BoxCollider>();
            collider.center=root.transform.InverseTransformPoint(bounds.center);
            var scale=root.transform.lossyScale;
            collider.size=new Vector3(bounds.size.x/scale.x,bounds.size.y/scale.y,bounds.size.z/scale.z);
            if(id=="painted_wooden_bench")
            {
                root.transform.rotation=Quaternion.Euler(0,-90,0);
                bounds=BoundsOf(root);
                root.transform.position+=feet-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            }
            return root;
        }
        public static Bounds BoundsOf(GameObject root)
        {
            var renderers=root.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);return bounds;
        }

        static void RebuildRetainedShell()
        {
            var roots=SceneManager.GetActiveScene().GetRootGameObjects();
            foreach(var go in roots.Where(g=>g.name.StartsWith("CourtyardMesh")))go.SetActive(false);
            var previous=GameObject.Find("Retained courtyard shell");if(previous)Object.DestroyImmediate(previous);
            var shell=new GameObject("Retained courtyard shell");
            var filters=roots.Where(g=>g && g.GetComponent<MeshFilter>() && AssetDatabase.GetAssetPath(g.GetComponent<MeshFilter>().sharedMesh)=="Library/unity default resources").ToArray();
            string[] replaced={"Tool crate","Crate stripe","Bench seat","Bench foot","Masonry joint"};
            foreach(var go in filters.Where(g=>replaced.Contains(g.name)))
            {
                go.GetComponent<Renderer>().enabled=false;
                foreach(var c in go.GetComponents<Collider>())c.enabled=false;
                foreach(var marker in go.GetComponents<PeelWorkbench>())Object.DestroyImmediate(marker);
            }
            var retained=filters.Where(g=>!replaced.Contains(g.name) && g.name!="Work wall");
            foreach(var group in retained.GroupBy(g=>g.GetComponent<Renderer>().sharedMaterial))
            {
                var mesh=new Mesh {name="Retained "+group.Key.name};
                mesh.CombineMeshes(group.Select(g=>new CombineInstance {mesh=g.GetComponent<MeshFilter>().sharedMesh,transform=g.transform.localToWorldMatrix}).ToArray());
                string path=Generated+"/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
                var go=new GameObject(mesh.name);go.transform.SetParent(shell.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
                foreach(var original in group)original.GetComponent<Renderer>().enabled=false;
            }
        }
    }
}
