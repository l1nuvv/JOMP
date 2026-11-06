using UnityEditor;
using UnityEngine;

namespace JustOneMorePeel.Editor
{
    public static class InteriorCandidateSetup
    {
        const string Art="Assets/JustOneMorePeel/Art/InteriorCandidates/";
        const string ResourcesPath="Assets/JustOneMorePeel/Resources/";

        [MenuItem("Tools/Just One More Peel/Build Interior Candidates")]
        public static void Build()
        {
            var wood=Material("Interior muted wood",new Color(.39f,.32f,.24f));
            var metal=Material("Interior muted metal",new Color(.28f,.34f,.32f));
            var red=Material("Interior book red",new Color(.40f,.25f,.21f));
            var paper=Material("Interior book paper",new Color(.72f,.68f,.57f));
            var green=Material("Interior book green",new Color(.31f,.37f,.29f));
            var archive=new GameObject("Archive storage candidates");
            Add(archive.transform,"bookcaseopen",new Vector3(-3.2f,-2.01f,20f),2f,wood,metal,red,paper,green,true);
            Add(archive.transform,"bookcaseclosed",new Vector3(-3.2f,-2.01f,23f),2f,wood,metal,red,paper,green,true);
            Add(archive.transform,"bookcaseopen",new Vector3(3.6f,-2.01f,25f),2f,wood,metal,red,paper,green,true);
            Add(archive.transform,"books",new Vector3(-3.30f,-1.27f,19.92f),2f,wood,metal,red,paper,green,false);
            Add(archive.transform,"books",new Vector3(-3.65f,-.79f,19.92f),2f,wood,metal,red,paper,green,false);
            Add(archive.transform,"books",new Vector3(3.48f,-1.27f,24.92f),2f,wood,metal,red,paper,green,false);
            Add(archive.transform,"books",new Vector3(3.10f,-.79f,24.92f),2f,wood,metal,red,paper,green,false);
            Add(archive.transform,"desk",new Vector3(-3.1f,-2.01f,17.4f),2f,wood,metal,red,paper,green,true);
            Add(archive.transform,"books",new Vector3(-3.7f,-1.23f,17.1f),2f,wood,metal,red,paper,green,false);
            PrefabUtility.SaveAsPrefabAsset(archive,ResourcesPath+"ArchiveStorageCandidates.prefab");
            Object.DestroyImmediate(archive);
            var basement=new GameObject("Basement service storage candidate");
            Add(basement.transform,"bookcaseclosed",new Vector3(-4.5f,-2.01f,11.4f),2f,metal,metal,red,paper,green,true);
            PrefabUtility.SaveAsPrefabAsset(basement,ResourcesPath+"BasementStorageCandidate.prefab");
            Object.DestroyImmediate(basement);
            AssetDatabase.SaveAssets();
        }
        static Material Material(string name,Color color)
        {
            string path=Art+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.15f);
            AssetDatabase.CreateAsset(material,path);return material;
        }
        static void Add(Transform parent,string model,Vector3 position,float scale,Material wood,Material metal,Material red,Material paper,Material green,bool collision)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Art+model+".fbx");
            if(!asset)throw new System.InvalidOperationException("Missing interior candidate: "+model);
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(asset);
            obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=asset.transform.localScale*scale;
            foreach(var renderer in obj.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var n=materials[i] ? materials[i].name.ToLowerInvariant() : "";
                    materials[i]=n.Contains("metal")?metal:n.Contains("carpetdarker")?red:n.Contains("carpetwhite")?paper:n.Contains("plant")?green:wood;
                }
                renderer.sharedMaterials=materials;
            }
            if(collision)
            {
                var renderer=obj.GetComponentInChildren<Renderer>();
                var bounds=renderer.localBounds;
                var collider=renderer.gameObject.AddComponent<BoxCollider>();
                collider.center=bounds.center;collider.size=bounds.size;
            }
        }
    }
}
