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
            Add(archive.transform,"bookcaseopen",new Vector3(-2.25f,-2.01f,33.35f),2f,wood,metal,red,paper,green,true);
            Add(archive.transform,"bookcaseclosed",new Vector3(-3.13f,-2.01f,33.35f),2f,wood,metal,red,paper,green,true);
            Add(archive.transform,"bookcaseopen",new Vector3(3.65f,-2.01f,33.35f),2f,wood,metal,red,paper,green,true);
            Add(archive.transform,"books",new Vector3(-2.61f,-1.27f,33.21f),2f,wood,metal,red,paper,green,false);
            Add(archive.transform,"books",new Vector3(-2.36f,-.79f,33.25f),2f,wood,metal,red,paper,green,false);
            Add(archive.transform,"books",new Vector3(3.49f,-1.27f,33.19f),2f,wood,metal,red,paper,green,false);
            Add(archive.transform,"books",new Vector3(3.31f,-.79f,33.23f),2f,wood,metal,red,paper,green,false);
            Add(archive.transform,"desk",new Vector3(-5.08f,-2.01f,22.25f),2f,wood,metal,red,paper,green,true);
            Add(archive.transform,"books",new Vector3(-5.5f,-1.23f,22.14f),2f,wood,metal,red,paper,green,false);
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
