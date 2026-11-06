using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace JustOneMorePeel
{
    public static class PeelProgressionSetup
    {
        const string Art="Assets/JustOneMorePeel/Art";
        static readonly string[] Notices={"01_LostCat","02_Cinema","03_QuietYard","04_ShoeRepair","05_Garage",
            "06_Neighbours","07_OldWarning","08_Electrician","09_Plants","10_OldCinema"};
        [MenuItem("Just One More Peel/Configure poster progression and languages")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!="Assets/JustOneMorePeel/Scenes/FirstShift.unity")
                throw new InvalidOperationException("Open FirstShift in Edit Mode.");
            var game=Object.FindAnyObjectByType<PeelGame>();
            var russian=Notices.Select(n=>AssetDatabase.LoadAssetAtPath<Material>(Art+"/Integrated/"+n+".mat")).ToArray();
            var english=new Material[10];
            for(int i=0;i<10;i++)
            {
                string texturePath=Art+"/Notices/English/"+Notices[i]+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.maxTextureSize=1024;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
                string path=Art+"/Integrated/English_"+Notices[i]+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material){material=new Material(russian[i]);AssetDatabase.CreateAsset(material,path);}
                material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);EditorUtility.SetDirty(material);english[i]=material;
            }
            var settings=game.GetComponent<PeelSettings>();if(!settings)settings=game.gameObject.AddComponent<PeelSettings>();
            var serialized=new SerializedObject(settings);
            SetObjects(serialized.FindProperty("russianPapers"),russian);
            SetObjects(serialized.FindProperty("englishPapers"),english);
            var writing=GameObject.Find("Old wall writing");
            if(writing)serialized.FindProperty("wallWriting").objectReferenceValue=writing.GetComponent<TextMesh>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            for(int i=0;i<19;i++)
            {
                var board=GameObject.Find("Street collage "+i);
                var strips=board.GetComponentsInChildren<PeelStrip>();
                var front=strips.Where(s=>!s.covering).OrderBy(s=>s.uvLeft).ToArray();
                var back=strips.Where(s=>s.covering).OrderBy(s=>s.uvLeft).ToArray();
                if(front.Length!=4 || back.Length!=4)throw new InvalidOperationException("Unexpected paper buffers at "+board.name);
                var stack=board.GetComponent<PeelPosterStack>();if(!stack)stack=board.AddComponent<PeelPosterStack>();
                var config=new SerializedObject(stack);
                config.FindProperty("posterCount").intValue=128;
                config.FindProperty("sequenceOffset").intValue=i%16;
                config.FindProperty("firstDesign").intValue=Array.IndexOf(russian,front[0].GetComponent<Renderer>().sharedMaterial);
                config.FindProperty("secondDesign").intValue=Array.IndexOf(russian,back[0].GetComponent<Renderer>().sharedMaterial);
                SetObjects(config.FindProperty("front"),front);SetObjects(config.FindProperty("back"),back);
                config.ApplyModifiedPropertiesWithoutUndo();
            }
            var gameConfig=new SerializedObject(game);
            gameConfig.FindProperty("starterViewPrefab").objectReferenceValue=StreetArtPass.ToolPrefab("Card");
            gameConfig.ApplyModifiedPropertiesWithoutUndo();
            var bench=GameObject.Find("Worn courtyard bench");if(bench)Object.DestroyImmediate(bench);
            game.money=0;game.removed=0;game.toolLevel=0;game.activeTool=0;game.steamUnlocked=false;game.total=19*128*4;
            EditorUtility.SetDirty(game);AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
            Debug.Log("POSTER_PROGRESS_CONFIGURED: 2432 logical posters, 152 display strips, EN/RU settings, starter card; bench removed.");
        }
        static void SetObjects(SerializedProperty property,Object[] values)
        {property.arraySize=values.Length;for(int i=0;i<values.Length;i++)property.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
    }
}
