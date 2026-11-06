using UnityEngine;
using Unity.Profiling;
namespace JustOneMorePeel
{
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider))]
    public class PeelStrip : MonoBehaviour
    {
        public PeelGame game;
        public PeelStrip covering;
        public float width=.3f, height=1.1f;
        public float uvLeft, uvRight=1;
        public float progress;
        public int SoundSample { get; set; }
        public PeelPosterStack Stack { get; internal set; }
        public int BufferIndex { get; internal set; }
        public bool IsRemoved { get; private set; }
        public bool IsExposed { get { return !IsRemoved && (Stack ? Stack.IsCurrent(this) : (!covering || covering.IsRemoved)); } }
        Mesh mesh;
        Vector3[] vertices;
        Vector2[] uv;
        int[] triangles;
        static readonly Vector3[] wholeVertices=new Vector3[(rows+1)*2];
        static readonly Vector2[] wholeUv=new Vector2[(rows+1)*2];
        MeshRenderer paperRenderer;
        BoxCollider paperCollider;
        const int rows=24;
        bool pulling;
        int lastDeformFrame=-1;
        static readonly ProfilerMarker DeformMarker=new ProfilerMarker("Poster.Deform");
        public void Prepare()
        {
            paperRenderer=GetComponent<MeshRenderer>();paperCollider=GetComponent<BoxCollider>();
            if(mesh)DestroyImmediate(mesh);
            mesh=new Mesh {name="Flexible paper strip"}; mesh.MarkDynamic();
            vertices=new Vector3[(rows+1)*2]; uv=new Vector2[vertices.Length];
            triangles=new int[rows*6];
            for(int i=0;i<=rows;i++)
            {
                uv[i*2]=new Vector2(uvLeft,1f-(float)i/rows);
                uv[i*2+1]=new Vector2(uvRight,1f-(float)i/rows);
                if(i==rows) continue;
                int k=i*6, a=i*2;
                triangles[k]=a;triangles[k+1]=a+1;triangles[k+2]=a+2;
                triangles[k+3]=a+1;triangles[k+4]=a+3;triangles[k+5]=a+2;
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;
            GetComponent<MeshFilter>().sharedMesh=mesh;
            // Match the resting sheet: a thick box can intercept rays meant for
            // another poster when the sheets overlap or are viewed at an angle.
            paperCollider.center=Vector3.zero;
            paperCollider.size=new Vector3(width,height,.00002f);
            Deform();
        }
        void Awake() { if(mesh==null) Prepare(); enabled=false; }
        public void Begin() { if(IsExposed) {pulling=true;enabled=true;} }
        public void End() { pulling=false;enabled=false; }
        public void ResetForPoster(PeelGame owner,float w,float h,Vector3 localPosition,Material material,bool visible)
        {
            if(mesh==null)Prepare();
            game=owner;width=w;height=h;progress=0;SoundSample=0;IsRemoved=!visible;pulling=false;enabled=false;
            transform.localPosition=localPosition;
            paperCollider.center=Vector3.zero;
            paperCollider.size=new Vector3(width,height,.00002f);paperCollider.enabled=visible;
            paperRenderer.sharedMaterial=material;paperRenderer.enabled=visible;Deform();
        }
        public void CopyMeshTo(Mesh destination)
        {
            destination.Clear();destination.vertices=vertices;destination.uv=uv;destination.triangles=triangles;
            destination.RecalculateNormals();destination.RecalculateBounds();
        }
        public void CopyWholePosterTo(Mesh destination)
        {
            for(int i=0;i<vertices.Length;i++)
            {wholeVertices[i]=new Vector3(vertices[i].x*4,vertices[i].y,vertices[i].z);wholeUv[i]=new Vector2(i%2,uv[i].y);}
            destination.Clear();destination.vertices=wholeVertices;destination.uv=wholeUv;destination.triangles=triangles;
            destination.RecalculateNormals();destination.RecalculateBounds();
        }
        public void Pull(float amount)
        {
            if(!IsExposed || amount<=0) return;
            progress=Mathf.Clamp01(progress+amount);
            Deform();
            if(progress>=1) Detach();
        }
        internal void RestoreProgress(float value, bool removed)
        {
            progress=value;IsRemoved=removed;pulling=false;enabled=false;
            paperCollider.enabled=!removed;paperRenderer.enabled=!removed;Deform();
        }
        void Deform()
        {
            if(mesh==null) return;
            using var sample=DeformMarker.Auto();
            lastDeformFrame=Time.frameCount;
            float boundary=height*(1-progress);
            for(int i=0;i<=rows;i++)
            {
                float y=height*.5f-height*i/rows;
                float fromBottom=y+height*.5f;
                float loose=Mathf.Max(0,fromBottom-boundary);
                float curl=loose>0 ? Mathf.Sin(Mathf.Min(loose*4,Mathf.PI*.85f))*.15f+loose*.42f : 0;
                float lift=loose>0 ? loose*.45f : 0;
                for(int s=0;s<2;s++)
                {
                    float x=(s==0 ? -.5f : .5f)*width;
                    float edge=loose>0 ? Mathf.Sin(i*.8f+Time.time*12)*.008f : 0;
                    vertices[i*2+s]=new Vector3(x,y-lift,-curl+edge);
                }
            }
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        void Detach()
        {
            if(IsRemoved) return;
            IsRemoved=true;pulling=false;enabled=false;
            paperCollider.enabled=false;
            if(game)game.Pay(this);
            paperRenderer.enabled=false;
            if(Stack)Stack.RecordRemoval(BufferIndex);
            if(game && game.NeedsStripScrap(this) && game.Scraps)game.Scraps.Emit(this);
        }
        void LateUpdate()
        {
            // Pull already updates geometry synchronously. Retain idle held flutter
            // after gameplay Update, regardless of component execution order.
            if(pulling && lastDeformFrame!=Time.frameCount) Deform();
        }
        void OnDestroy() { if(mesh) Destroy(mesh); }
    }
}
