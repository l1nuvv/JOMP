using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace JustOneMorePeel
{
    public static class PrototypeBuilder
    {
        const string Root="Assets/JustOneMorePeel";
        static Font font;
        static Texture2D atlas;
        static Material concrete, metal, ochre, dark, pale;
        [MenuItem("Just One More Peel/Create MVP scene")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
            System.IO.Directory.CreateDirectory(Root+"/Materials");
            System.IO.Directory.CreateDirectory(Root+"/Scenes");
            if(SceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveOpenScenes();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            concrete=Mat("Warm concrete",new Color(.38f,.38f,.33f));
            metal=Mat("Petrol metal",new Color(.08f,.19f,.19f));
            ochre=Mat("Safety yellow",new Color(.88f,.55f,.15f));
            dark=Mat("Asphalt",new Color(.12f,.17f,.17f));
            pale=Mat("Old paper",new Color(.86f,.80f,.64f));
            Cube("Courtyard floor",new Vector3(0,-.16f,0),new Vector3(14,.3f,14),dark);
            Cube("Work wall",new Vector3(0,2.1f,3.35f),new Vector3(11,4.2f,.5f),concrete);
            Cube("Left building",new Vector3(-6,3,1),new Vector3(1,6,10),concrete);
            Cube("Right building",new Vector3(6,3,1),new Vector3(1,6,10),concrete);
            Cube("Back boundary",new Vector3(0,1.3f,-6.5f),new Vector3(12,2.6f,.4f),metal);
            for(int i=0;i<16;i++)
                Cube("Paving seam",new Vector3(-6+i*.8f,.002f,0),new Vector3(.018f,.007f,12),concrete);
            Cube("Wall cap",new Vector3(0,4.25f,3.25f),new Vector3(11.3f,.18f,.8f),metal);
            Cube("Skirting",new Vector3(0,.14f,3.04f),new Vector3(11,.28f,.12f),metal);
            for(int i=0;i<7;i++)
            {
                Cube("Masonry joint",new Vector3(0,.5f+i*.5f,3.094f),new Vector3(10.8f,.016f,.012f),dark);
                for(int j=0;j<10;j++)
                    Cube("Masonry joint",new Vector3(-5+j*1.15f+(i%2)*.5f,.25f+i*.5f,3.094f),new Vector3(.014f,.49f,.012f),dark);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Cube("Drainpipe",new Vector3(side*5.15f,2,2.94f),new Vector3(.14f,4,.16f),metal);
                Cube("Lamp bracket",new Vector3(side*4.5f,3.8f,2.7f),new Vector3(.75f,.14f,.3f),metal);
                var light=new GameObject("Warm work light").AddComponent<Light>();
                light.type=LightType.Point;light.range=6;light.intensity=3;
                light.color=new Color(1,.75f,.43f);
                light.transform.position=new Vector3(side*4.4f,3.5f,2.3f);
                Cube("Lamp glow",light.transform.position,new Vector3(.5f,.08f,.1f),pale);
            }
            for(int i=0;i<3;i++)
            {
                Cube("Tool crate",new Vector3(-4.55f,.25f+i*.45f,-.3f),new Vector3(1,.4f,.8f),i%2==0?ochre:metal);
                Cube("Crate stripe",new Vector3(-4.55f,.25f+i*.45f,-.715f),new Vector3(.8f,.06f,.025f),pale);
            }
            Cube("Bench seat",new Vector3(4.6f,.55f,-1.4f),new Vector3(1.4f,.16f,2.1f),ochre);
            Cube("Bench foot",new Vector3(4.6f,.25f,-2),new Vector3(1,.5f,.15f),metal);
            Cube("Bench foot",new Vector3(4.6f,.25f,-.8f),new Vector3(1,.5f,.15f),metal);
            Sign("JUST ONE MORE PEEL",new Vector3(-3.25f,3.85f,3.065f),.12f,pale.color);
            Sign("01 / FIRST SHIFT",new Vector3(-4.85f,3.25f,3.06f),.045f,ochre.color);
            Sign("TOOLS",new Vector3(-4.9f,1.9f,2.95f),.09f,pale.color);
            Sign("TAKE IT\nONE LAYER\nAT A TIME.",new Vector3(3.85f,2.7f,3.06f),.065f,pale.color);
            Sign("THANK YOU\nFOR FINDING ME.",new Vector3(-2.8f,2.8f,3.07f),.13f,ochre.color);
            var sun=new GameObject("Afternoon sun").AddComponent<Light>();
            sun.type=LightType.Directional;sun.intensity=1.6f;sun.color=new Color(1,.88f,.7f);
            sun.transform.rotation=Quaternion.Euler(45,-35,0);sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.53f,.62f,.65f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.59f,.67f,.67f);
            RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.018f;
            var rig=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Starter Assets/Runtime/FirstPersonController/Prefabs/NestedParent_Unpack.prefab"));
            rig.name="Player rig (Unity Starter Assets)";
            var controller=rig.GetComponentsInChildren<MonoBehaviour>(true).First(c=>c.GetType().Name=="FirstPersonController");
            controller.transform.position=new Vector3(0,.15f,-1.6f);
            var camera=rig.GetComponentInChildren<Camera>(true);camera.farClipPlane=100;
            camera.backgroundColor=new Color(.59f,.67f,.67f);
            foreach(var r in controller.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
            var virtualCamera=rig.GetComponentsInChildren<MonoBehaviour>(true).First(c=>c.GetType().Name=="CinemachineCamera");
            var lensField=virtualCamera.GetType().GetField("Lens");
            var lens=lensField.GetValue(virtualCamera);
            lens.GetType().GetField("FieldOfView").SetValue(lens,70f);lensField.SetValue(virtualCamera,lens);
            var game=new GameObject("Shift / progress / tools").AddComponent<PeelGame>();
            game.view=camera;game.player=controller.gameObject;
            PrepareFont();
            var palette=new []{new Color(.94f,.58f,.24f),new Color(.2f,.46f,.47f),new Color(.8f,.27f,.24f),
                new Color(.79f,.74f,.56f),new Color(.42f,.47f,.65f),new Color(.82f,.5f,.57f)};
            var headlines=new []{"NIGHT SHIFT","LOST SIGNAL","NOISE CLUB","OPEN LATE","CITY RADIO","STAY CURIOUS"};
            game.papers=new Material[6];
            for(int i=0;i<6;i++)
            {
                var tex=Poster(i,palette[i],headlines[i]);
                var path=Root+"/Materials/Poster"+i+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if(existing) {UnityEngine.Object.DestroyImmediate(tex);tex=existing;} else AssetDatabase.CreateAsset(tex,path);
                var material=Mat("Poster "+i,Color.white);
                material.SetTexture("_BaseMap",tex);material.SetFloat("_Cull",0);
                game.papers[i]=material;
            }
            for(int row=0;row<2;row++)
            for(int col=0;col<4;col++)
            {
                var board=new GameObject("Poster patch "+(row*4+col+1));
                board.transform.position=new Vector3(-2.55f+col*1.7f,.99f+row*1.43f,3.04f);
                var previous=new PeelStrip[4];
                for(int layer=0;layer<3;layer++)
                for(int strip=0;strip<4;strip++)
                {
                    var go=new GameObject("Layer "+(layer+1)+" / strip "+(strip+1));
                    go.transform.SetParent(board.transform,false);
                    go.transform.localPosition=new Vector3((strip-1.5f)*.34f,0,-layer*.018f);
                    var paper=go.AddComponent<PeelStrip>();
                    paper.width=.34f;paper.height=1.3f;paper.uvLeft=strip/4f;paper.uvRight=(strip+1)/4f;
                    paper.game=game;
                    go.GetComponent<MeshRenderer>().sharedMaterial=game.papers[(col+row*2+layer*2)%6];
                    paper.Prepare();
                    if(previous[strip]) previous[strip].covering=paper;
                    previous[strip]=paper;
                }
            }
            game.total=96;
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/FirstShift.unity");
            EditorBuildSettings.scenes=new []{new EditorBuildSettingsScene(scene.path,true)};
            PlayerSettings.companyName="Just One More Peel";
            PlayerSettings.productName="Just One More Peel";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            AssetDatabase.SaveAssets();
            Debug.Log("MVP_READY: 96 paper strips, Starter Assets FPS rig, shop, completion.");
        }
        static Material Mat(string name,Color color)
        {
            string path=Root+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) {material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.SetFloat("_Smoothness",.12f);
            return material;
        }
        static GameObject Cube(string name,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=name;go.transform.position=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        static void Sign(string label,Vector3 position,float size,Color color)
        {
            var go=new GameObject(label);go.transform.position=position;
            var text=go.AddComponent<TextMesh>();text.text=label;text.characterSize=size*.4f;
            text.fontSize=64;text.color=color;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.anchor=TextAnchor.UpperLeft;
            var path=Root+"/Materials/World lettering.mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) {m=new Material(Shader.Find("JustOneMorePeel/WorldText"));AssetDatabase.CreateAsset(m,path);}
            m.mainTexture=text.font.material.mainTexture;
            text.GetComponent<Renderer>().sharedMaterial=m;
        }
        static void PrepareFont()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            font.RequestCharactersInTexture("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 /.-",64,FontStyle.Bold);
            var source=font.material.mainTexture;
            var rt=RenderTexture.GetTemporary(source.width,source.height);
            Graphics.Blit(source,rt);var previous=RenderTexture.active;RenderTexture.active=rt;
            atlas=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
            atlas.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);atlas.Apply();
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
        }
        static Texture2D Poster(int index,Color background,string headline)
        {
            var t=new Texture2D(384,512,TextureFormat.RGBA32,false);t.name=headline;
            var pixels=new Color[384*512];var ink=new Color(.07f,.12f,.13f);
            var rng=new System.Random(25+index);
            for(int y=0;y<512;y++)for(int x=0;x<384;x++)
            {
                var c=background*(.94f+(float)rng.NextDouble()*.08f);
                if(x<10||x>373||y<10||y>501)c=ink;
                float dx=x-192,dy=y-250;
                if(index%3==0 && dx*dx+dy*dy<110*110)c=ink;
                if(index%3==1 && Mathf.Abs(dx)+Mathf.Abs(dy)<130)c=ink;
                if(index%3==2 && y>145 && y<345 && ((x+y)/27)%2==0)c=ink;
                if(y>235&&y<260&&x>60&&x<324)c=background;
                pixels[x+y*384]=c;
            }
            t.SetPixels(pixels);
            Stamp(t,headline,23,445,.47f,ink);
            Stamp(t,"PEEL DISTRICT / 01",25,394,.28f,ink);
            Stamp(t,"FRIDAY  23.00",25,82,.48f,ink);
            Stamp(t,"DO NOT COVER THIS POSTER",25,38,.24f,ink);
            t.Apply();return t;
        }
        static void Stamp(Texture2D t,string text,int left,int baseline,float scale,Color color)
        {
            float cursor=left;
            foreach(char ch in text)
            {
                CharacterInfo info;
                if(!font.GetCharacterInfo(ch,out info,64,FontStyle.Bold))continue;
                int width=Mathf.CeilToInt(info.glyphWidth*scale),height=Mathf.CeilToInt(info.glyphHeight*scale);
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                {
                    float u=(x+.5f)/width,v=(y+.5f)/height;
                    var uv=Vector2.Lerp(Vector2.Lerp(info.uvBottomLeft,info.uvBottomRight,u),
                        Vector2.Lerp(info.uvTopLeft,info.uvTopRight,u),v);
                    float alpha=atlas.GetPixelBilinear(uv.x,uv.y).a;
                    int px=Mathf.RoundToInt(cursor+info.minX*scale)+x,py=baseline+Mathf.RoundToInt(info.minY*scale)+y;
                    if(px>=0&&px<t.width&&py>=0&&py<t.height)
                        t.SetPixel(px,py,Color.Lerp(t.GetPixel(px,py),color,alpha));
                }
                cursor+=info.advance*scale;
            }
        }
    }
}