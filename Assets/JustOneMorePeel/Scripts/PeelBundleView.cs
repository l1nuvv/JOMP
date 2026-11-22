using UnityEngine;
using UnityEngine.Rendering;

namespace JustOneMorePeel
{
    internal sealed class PeelBundleView
    {
        // Folded notices sit across the palm, with offset edges showing the growing pile.
        static readonly (int Design, Vector2 Size, Vector2 Offset, float Angle)[] Layers =
        {
            (30,new Vector2(.105f,.170f),new Vector2(-.009f,.002f),-12f),
            (28,new Vector2(.135f,.149f),new Vector2(.013f,-.008f),12f),
            (31,new Vector2(.155f,.168f),new Vector2(-.010f,.010f),-17f),
            (29,new Vector2(.149f,.144f),new Vector2(.015f,.013f),18f),
            (30,new Vector2(.145f,.161f),new Vector2(.006f,-.011f),-5f)
        };
        static readonly Vector3 RestPosition=new Vector3(-.235f,-.213f,.51f);
        const float PaperGripOffset=.125f;
        readonly PeelSettings settings;
        readonly Mesh paperMesh;
        readonly MeshRenderer[] papers=new MeshRenderer[Layers.Length];
        PeelLanguage language;
        int shownLayers;
        float presence;
        float workingOffset;

        internal Transform Root { get; }

        internal PeelBundleView(Transform parent,PeelSettings settings)
        {
            this.settings=settings;
            Root=new GameObject("Held paper bundle").transform;
            Root.SetParent(parent,false);
            Root.localRotation=Quaternion.Euler(-22f,-12f,8f);
            Root.gameObject.SetActive(false);
            paperMesh=CreatePaperMesh();
            for(int i=0;i<Layers.Length;i++)
            {
                var layer=Layers[i];
                var paper=new GameObject("Folded notice");
                paper.transform.SetParent(Root,false);
                paper.transform.SetLocalPositionAndRotation(
                    new Vector3(layer.Offset.x,i*.005f,PaperGripOffset+layer.Offset.y),Quaternion.Euler(0,layer.Angle,0));
                paper.transform.localScale=new Vector3(layer.Size.x,.13f,layer.Size.y);
                paper.AddComponent<MeshFilter>().sharedMesh=paperMesh;
                var renderer=paper.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                papers[i]=renderer;
                renderer.enabled=false;
            }
            RefreshMaterials();
        }

        void RefreshMaterials()
        {
            language=settings.Language;
            for(int i=0;i<papers.Length;i++)
                papers[i].sharedMaterial=settings.Paper(Layers[i].Design);
        }

        internal void UpdatePose(bool visible,int items,int capacity,bool working)
        {
            if(!visible)
            {
                presence=workingOffset=0f;
                if(Root.gameObject.activeSelf)Root.gameObject.SetActive(false);
                return;
            }
            presence=Mathf.MoveTowards(presence,items>0?1f:0f,Time.unscaledDeltaTime*9f);
            bool show=presence>0f;
            if(Root.gameObject.activeSelf!=show)Root.gameObject.SetActive(show);
            if(!show)return;

            if(language!=settings.Language)RefreshMaterials();
            if(items>0)
            {
                int layers=items==1 ? 1 : 2+(Layers.Length-2)*(items-2)/(capacity-2);
                if(shownLayers!=layers)
                {
                    shownLayers=layers;
                    for(int i=0;i<papers.Length;i++)papers[i].enabled=i<layers;
                }
            }
            workingOffset=Mathf.MoveTowards(workingOffset,working?.044f:0f,Time.unscaledDeltaTime*.34f);
            float rise=Mathf.SmoothStep(.13f,0f,presence);
            Root.localPosition=RestPosition+Vector3.down*(rise+workingOffset);
        }

        static Mesh CreatePaperMesh()
        {
            var mesh=new Mesh {name="Folded collected notice"};
            mesh.vertices=new[]
            {
                new Vector3(-.49f,.035f,-.47f),new Vector3(0,.008f,-.50f),new Vector3(.49f,.052f,-.45f),
                new Vector3(-.50f,.006f,0),Vector3.zero,new Vector3(.495f,.012f,.03f),
                new Vector3(-.47f,.059f,.48f),new Vector3(.01f,.018f,.50f),new Vector3(.47f,.080f,.47f)
            };
            mesh.uv=new[]
            {
                new Vector2(0,0),new Vector2(.5f,0),new Vector2(1,0),
                new Vector2(0,.5f),new Vector2(.5f,.5f),new Vector2(1,.5f),
                new Vector2(0,1),new Vector2(.5f,1),new Vector2(1,1)
            };
            mesh.triangles=new[]{0,3,1,1,3,4,1,4,2,2,4,5,3,6,4,4,6,7,4,7,5,5,7,8};
            mesh.RecalculateNormals();
            return mesh;
        }

        internal void Dispose() => Object.Destroy(paperMesh);
    }
}
