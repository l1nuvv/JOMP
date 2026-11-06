using UnityEngine;
using UnityEditor;
using System.Linq;
namespace JustOneMorePeel {
public static class ArtRevision {
public static void Apply() {
var game=Object.FindFirstObjectByType<PeelGame>();
foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
 if(go.name.StartsWith("Poster patch")||go.name=="Paving seam"||go.GetComponent<TextMesh>()) Object.DestroyImmediate(go);
var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
font.RequestCharactersInTexture("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ0123456789 /.-№",64,FontStyle.Bold);
var source=font.material.mainTexture;var rt=RenderTexture.GetTemporary(source.width,source.height);Graphics.Blit(source,rt);
var old=RenderTexture.active;RenderTexture.active=rt;var atlas=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);atlas.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);atlas.Apply();RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);
string[][] lines={
new[]{"ПРОПАЛ","РЫЖИЙ КОТ","ОТЗЫВАЕТСЯ НА ШУМ","ПАКЕТА С ЕДОЙ","ДВОР №17"},
new[]{"РЕМОНТ","ОБУВИ","ВХОД СО ДВОРА","СРЕДА - ВОСКРЕСЕНЬЕ","МАСТЕРСКАЯ №17"},
new[]{"КИНО","ВО ДВОРЕ","В ПЯТНИЦУ / 21.00","СТУЛ ПРИНЕСИ С СОБОЙ","ВХОД СВОБОДНЫЙ"},
new[]{"СДАМ","ГАРАЖ","СУХОЙ. БЕЗ ЯМЫ.","СПРАШИВАТЬ У СТОРОЖА","ГАРАЖ №17"},
new[]{"КОНЦЕРТ","НА КРЫШЕ","СУББОТА / 19.30","ДОЖДЬ НАМ НЕ ПОМЕХА","ДВОР №17"},
new[]{"ОБЪЯВЛЕНИЯ","НЕ КЛЕИТЬ","СТЕНА ОКРАШЕНА","ОТВЕТСТВЕННЫЙ: ДВОРНИК","1987"}};
var mats=new Material[6];
for(int n=0;n<6;n++){
var tex=new Texture2D(384,512,TextureFormat.RGBA32,false);var colors=new Color[384*512];var rng=new System.Random(71+n);
Color paper=n==2?new Color(.58f,.65f,.65f):n==4?new Color(.74f,.55f,.43f):new Color(.77f,.73f,.61f);
for(int y=0;y<512;y++)for(int x=0;x<384;x++){
float stain=Mathf.PerlinNoise(x*.016f+n,y*.016f)*.16f;
float noise=(float)rng.NextDouble()*.07f;
Color c=paper*(.85f+noise+stain);
int edge=3+(int)(Mathf.PerlinNoise(y*.09f,n)*10);
c.a=(x<edge||x>383-edge||y<3+(x%19)/4||y>505-(x%31)/5)?0:1;
if(n%2==0&&y<62&&x%38<2)c=Color.Lerp(c,Color.black,.4f);
colors[x+y*384]=c;}
tex.SetPixels(colors);
for(int l=0;l<5;l++) Stamp(tex,font,atlas,lines[n][l],22,440-l*82,l<2?.65f:.24f,new Color(.13f,.13f,.11f));
tex.Apply();
string path="Assets/JustOneMorePeel/Materials/StreetNotice"+n+".asset";
var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
if(existing){Object.DestroyImmediate(tex);tex=existing;}else AssetDatabase.CreateAsset(tex,path);
var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetTexture("_BaseMap",tex);mat.SetFloat("_Cull",0);mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.5f);mat.EnableKeyword("_ALPHATEST_ON");mat.SetFloat("_Smoothness",0);mat.renderQueue=2450;
AssetDatabase.CreateAsset(mat,"Assets/JustOneMorePeel/Materials/StreetNotice"+n+".mat");mats[n]=mat;
}
game.papers=mats;
var random=new System.Random(1703);
for(int i=0;i<19;i++){
float w=.65f+(float)random.NextDouble()*.75f,h=.8f+(float)random.NextDouble()*.7f;
var board=new GameObject("Street collage "+i);
board.transform.position=new Vector3(-3.5f+(i%6)*1.25f+((float)random.NextDouble()-.5f)*.6f,.85f+(i/6)*.58f+((float)random.NextDouble()-.5f)*.38f,3.025f-i*.007f);
board.transform.rotation=Quaternion.Euler(0,0,((float)random.NextDouble()-.5f)*24);
var previous=new PeelStrip[4];
for(int layer=0;layer<2;layer++)for(int strip=0;strip<4;strip++){
var go=new GameObject("Paper "+layer+" / "+strip);go.transform.SetParent(board.transform,false);go.transform.localPosition=new Vector3((strip-1.5f)*w/4,0,-layer*.006f);
var p=go.AddComponent<PeelStrip>();p.width=w/4;p.height=h;p.uvLeft=strip/4f;p.uvRight=(strip+1)/4f;p.game=game;
go.GetComponent<MeshRenderer>().sharedMaterial=mats[(i+layer*3)%6];p.Prepare();if(previous[strip])previous[strip].covering=p;previous[strip]=p;
}}
game.total=152;
var plaque=GameObject.CreatePrimitive(PrimitiveType.Cube);plaque.name="Enamel yard plate";plaque.transform.position=new Vector3(-4.3f,3.35f,2.98f);plaque.transform.localScale=new Vector3(.85f,.45f,.07f);plaque.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/JustOneMorePeel/Materials/Petrol metal.mat");
var label=new GameObject("Yard number").AddComponent<TextMesh>();label.text="17";label.font=font;label.fontSize=64;label.characterSize=.075f;label.anchor=TextAnchor.MiddleCenter;label.transform.position=new Vector3(-4.3f,3.35f,2.93f);label.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/JustOneMorePeel/Materials/World lettering.mat");
var secret=new GameObject("Old wall writing").AddComponent<TextMesh>();secret.text="ЗДЕСЬ БЫЛО\nНАШЕ ЛЕТО";secret.font=font;secret.fontSize=64;secret.characterSize=.035f;secret.color=new Color(.37f,.21f,.13f);secret.transform.position=new Vector3(-2,2.4f,3.07f);secret.GetComponent<Renderer>().sharedMaterial=label.GetComponent<Renderer>().sharedMaterial;
foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if(r.name=="Masonry joint") r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
Object.DestroyImmediate(atlas);
UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
}
static void Stamp(Texture2D t,Font font,Texture2D atlas,string text,int left,int baseline,float scale,Color color){
float cursor=left;
foreach(char ch in text){CharacterInfo info;if(!font.GetCharacterInfo(ch,out info,64,FontStyle.Bold))continue;
int w=Mathf.CeilToInt(info.glyphWidth*scale),h=Mathf.CeilToInt(info.glyphHeight*scale);
for(int y=0;y<h;y++)for(int x=0;x<w;x++){float u=(x+.5f)/w,v=(y+.5f)/h;var uv=Vector2.Lerp(Vector2.Lerp(info.uvBottomLeft,info.uvBottomRight,u),Vector2.Lerp(info.uvTopLeft,info.uvTopRight,u),v);float a=atlas.GetPixelBilinear(uv.x,uv.y).a;int px=Mathf.RoundToInt(cursor+info.minX*scale)+x,py=baseline+Mathf.RoundToInt(info.minY*scale)+y;if(px>=0&&px<t.width&&py>=0&&py<t.height){var c=Color.Lerp(t.GetPixel(px,py),color,a);c.a=t.GetPixel(px,py).a;t.SetPixel(px,py,c);}}
cursor+=info.advance*scale;
}}
}}
