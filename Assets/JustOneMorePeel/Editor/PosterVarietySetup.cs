using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JustOneMorePeel
{
    /// <summary>Content-only migration; preserves wall placement, depth and gameplay.</summary>
    public static class PosterVarietySetup
    {
        [Serializable] class Entry { public string name; public bool opaque; public int width,height; }
        [Serializable] class Catalogue { public Entry[] notices; }
        const string Art="Assets/JustOneMorePeel/Art/";
        static readonly int[] Front={0,10,11,2,14,16,13,17,1,22,19,23,15,18,8,20,25,28,29};
        static readonly int[] Back={27,12,21,24,6,5,26,3,9,11,10,16,14,7,13,25,22,19,4};

        [MenuItem("Just One More Peel/Expand notice catalogue to 30")]
        public static void Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.path!="Assets/JustOneMorePeel/Scenes/FirstShift.unity")
                throw new InvalidOperationException("Open FirstShift in Edit Mode.");
            var catalogue=JsonUtility.FromJson<Catalogue>(File.ReadAllText("SourceArt/StreetRevision/notice-catalogue.json"));
            if(catalogue.notices.Length!=30)throw new InvalidOperationException("Expected thirty paired notices.");
            var game=UnityEngine.Object.FindAnyObjectByType<PeelGame>();
            var stacks=UnityEngine.Object.FindObjectsByType<PeelPosterStack>();
            if(stacks.Length!=19 || stacks.Sum(s=>s.PosterCount)!=2432)
                throw new InvalidOperationException("Unexpected live wall. Do not rebuild it.");
            // Validate every source before changing any serialized scene references.
            foreach(var entry in catalogue.notices)
                foreach(string language in new[]{"","English/"})
                    if(!File.Exists(Art+"Notices/"+language+entry.name+".png"))
                        throw new FileNotFoundException(entry.name+" / "+language);
            var ru=new Material[30];var en=new Material[30];
            var reference=AssetDatabase.LoadAssetAtPath<Material>(Art+"Integrated/02_Cinema.mat");
            for(int i=0;i<30;i++)
            {
                var entry=catalogue.notices[i];
                foreach(bool english in new[]{false,true})
                {
                    string path=Art+"Notices/"+(english?"English/":"")+entry.name+".png";
                    AssetDatabase.ImportAsset(path);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
                    importer.isReadable=false;importer.mipmapEnabled=true;
                    importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.anisoLevel=1;
                    // Unity leaves these NPOT prints uncompressed with None.
                    // Store physical print aspect separately so resizing cannot
                    // stretch the document on the wall or during a language swap.
                    importer.npotScale=TextureImporterNPOTScale.ToNearest;
                    importer.alphaIsTransparency=true;importer.maxTextureSize=1024;
                    importer.textureCompression=TextureImporterCompression.CompressedHQ;
                    var platform=importer.GetPlatformTextureSettings("Standalone");
                    platform.overridden=true;platform.maxTextureSize=1024;
                    platform.format=TextureImporterFormat.BC7;platform.compressionQuality=100;
                    importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
                    string materialPath=Art+"Integrated/"+(english?"English_":"")+entry.name+".mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(!material){material=new Material(reference);AssetDatabase.CreateAsset(material,materialPath);}
                    material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    EditorUtility.SetDirty(material);(english?en:ru)[i]=material;
                }
            }
            var settings=new SerializedObject(game.GetComponent<PeelSettings>());
            SetMaterials(settings.FindProperty("russianPapers"),ru);SetMaterials(settings.FindProperty("englishPapers"),en);
            var opaque=settings.FindProperty("opaquePapers");opaque.arraySize=30;
            var aspects=settings.FindProperty("paperAspects");aspects.arraySize=30;
            for(int i=0;i<30;i++)
            {
                opaque.GetArrayElementAtIndex(i).boolValue=catalogue.notices[i].opaque;
                aspects.GetArrayElementAtIndex(i).floatValue=(float)catalogue.notices[i].height/catalogue.notices[i].width;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            var config=new SerializedObject(game);SetMaterials(config.FindProperty("papers"),ru);config.ApplyModifiedPropertiesWithoutUndo();
            foreach(var stack in stacks)
            {
                int index=int.Parse(stack.name.Substring("Street collage ".Length));
                var serialized=new SerializedObject(stack);
                var size=serialized.FindProperty("maximumPaperSize");
                if(size.floatValue<=0)
                {
                    var original=(PeelStrip)serialized.FindProperty("front").GetArrayElementAtIndex(0).objectReferenceValue;
                    size.floatValue=original.width*4;
                }
                float maximumSize=size.floatValue;
                serialized.FindProperty("firstDesign").intValue=Front[index];
                serialized.FindProperty("secondDesign").intValue=Back[index];
                serialized.FindProperty("sequenceOffset").intValue=(index*11+5)%30;
                foreach(string buffer in new[]{"front","back"})
                {
                    int design=buffer=="front"?Front[index]:Back[index];
                    var strips=serialized.FindProperty(buffer);
                    for(int i=0;i<strips.arraySize;i++)
                    {
                        var strip=(PeelStrip)strips.GetArrayElementAtIndex(i).objectReferenceValue;
                        float aspect=(float)catalogue.notices[design].height/catalogue.notices[design].width;
                        strip.width=maximumSize/Mathf.Max(1,aspect)/4;strip.height=strip.width*4*aspect;
                        strip.GetComponent<Renderer>().sharedMaterial=ru[design];
                        EditorUtility.SetDirty(strip);EditorUtility.SetDirty(strip.GetComponent<Renderer>());
                    }
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("POSTER_VARIETY: 30 EN/RU designs; 19 existing stacks / 2432 posters preserved; BC7, mipmaps, shared materials.");
        }
        static void SetMaterials(SerializedProperty property,Material[] values)
        {property.arraySize=values.Length;for(int i=0;i<values.Length;i++)property.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
    }
}
