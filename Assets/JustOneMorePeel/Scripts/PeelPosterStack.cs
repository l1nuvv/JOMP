using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace JustOneMorePeel
{
    /// <summary>Logical depth with two reusable four-strip display buffers, regardless of depth.</summary>
    public sealed class PeelPosterStack : MonoBehaviour
    {
        [SerializeField, Min(2)] private int posterCount=128;
        [SerializeField] private int firstDesign;
        [SerializeField] private int secondDesign;
        [SerializeField] private int sequenceOffset;
        [SerializeField] private float maximumPaperSize;
        [SerializeField, Min(.1f)] private float firstLayerScale=1f;
        [SerializeField, Min(.1f)] private float deeperLayerScale=1f;
        [SerializeField] private float firstLayerOffset;
        [SerializeField] private PeelStrip[] front;
        [SerializeField] private PeelStrip[] back;
        [SerializeField, HideInInspector] private string persistentId;
        int sequenceStride;
        PeelGame game;
        float width;
        int removedMask;
        Mesh deeperMesh;
        MeshRenderer deeperRenderer;
        MeshCollider deeperCollider;
        Mesh collisionMesh;
        int[] deepDesigns, materialOrder;
        int deepCount, materialCount;
        int[] materialGroups;
        Material[] materials;
        Material[] orderedMaterials;
        readonly List<int> collisionTriangles=new List<int>();
        static readonly ProfilerMarker BuildMarker=new ProfilerMarker("Poster.BuildDeepLayers");
        static readonly ProfilerMarker VisibilityMarker=new ProfilerMarker("Poster.Visibility");
        float[] deepHeights,deepWidths;
        bool[] deepVisible;
        bool[] partialQuads;
        Rect[] coveredRects;
        List<Vector3> baseVertices;
        List<Vector2> baseUV;
        readonly List<Vector3> renderVertices=new List<Vector3>();
        readonly List<Vector2> renderUV=new List<Vector2>();
        readonly List<Vector3> renderNormals=new List<Vector3>();
        bool hadPartialGeometry;
        List<int>[] visibleTriangles;
        Vector3 lastCameraPosition;
        bool visibilityReady;
        internal bool IsRenderVisible { get; set; }=true;
        // Keep exposed buffers well apart while deeper sheets fit between stacks.
        const float LayerSpacing=.00001f;
        float LayerDepth(int layer)
        {
            // Depth belongs to the logical sheet, not the reusable display buffer.
            if(layer<=0)return -.001f;
            if(layer==1)return -.0008f;
            return -.0006f+(layer-2)*LayerSpacing;
        }
        public int PosterCount => posterCount;
        public string PersistentId => persistentId;
        public int ClearedPosters { get; private set; }
        public int RemainingPosters => posterCount-ClearedPosters;
        public PeelStrip[] CurrentStrips => front;
        public bool NeedsAdvance => removedMask==15;
        internal void RestoreProgress(int cleared, float[] progress)
        {
            ClearedPosters=cleared;removedMask=0;ShowBuffers();
            if(cleared>=posterCount)return;
            for(int i=0;i<4;i++)
            {
                bool removed=progress[i]>=1f;
                front[i].RestoreProgress(progress[i],removed);
                if(removed)removedMask|=1<<i;
            }
        }
        public bool IsCurrent(PeelStrip strip) { return RemainingPosters>0 && front[strip.BufferIndex]==strip; }

        public void Initialize(PeelGame owner)
        {
            if(front==null || back==null || front.Length!=4 || back.Length!=4)
                throw new InvalidOperationException("A poster stack needs two four-strip buffers: "+name);
            game=owner;width=maximumPaperSize>0?maximumPaperSize:front[0].width*4;
            // Each patch has a local ten-design rotation drawn from the full
            // catalogue. This bounds deep-mesh material groups as content grows.
            sequenceStride=7;
            while(Gcd(sequenceStride,game.Settings.PaperCount)!=1)sequenceStride++;
            ClearedPosters=0;removedMask=0;
            ShowBuffers();
        }
        int Design(int layer)
        {
            if(layer==0)return firstDesign;
            int count=game.Settings.PaperCount;
            int second=secondDesign==firstDesign ? (secondDesign+1)%count : secondDesign;
            if(layer==1)return second;
            int start=sequenceOffset%count;
            if(start==second)start=(start+sequenceStride)%count;
            return (start+((layer-2)%Mathf.Min(10,count))*sequenceStride)%count;
        }
        static int Gcd(int a,int b) { while(b!=0){int next=a%b;a=b;b=next;}return a; }
        void ShowBuffers()
        {
            Setup(front,ClearedPosters,LayerDepth(ClearedPosters),true);
            Setup(back,ClearedPosters+1,LayerDepth(ClearedPosters+1),false);
            UpdateDeeperLayers();
        }
        void Setup(PeelStrip[] buffer,int layer,float z,bool exposed)
        {
            bool visible=layer<posterCount;
            Material material=visible ? game.Settings.Paper(Design(layer)) : null;
            float aspect=visible ? game.Settings.PaperAspect(Design(layer)) : 1;
            float paperWidth=width/Mathf.Max(1,aspect),height=paperWidth*aspect;
            float layerScale=layer==0?firstLayerScale:deeperLayerScale;
            paperWidth*=layerScale;height*=layerScale;
            float offset=layer==0?firstLayerOffset:0f;
            for(int i=0;i<4;i++)
            {
                var strip=buffer[i];strip.Stack=this;strip.BufferIndex=i;
                strip.covering=exposed?null:front[i];
                strip.ResetForPoster(game,paperWidth/4,height,new Vector3(offset+(i-1.5f)*paperWidth/4,0,z),material,visible);
            }
        }
        // All lower sheets are present from the beginning, even where their outlines
        // extend beyond the two interactive display buffers.
        void UpdateDeeperLayers()
        {
            using var sample=BuildMarker.Auto();
            if(!deeperRenderer)
            {
                var go=new GameObject("Underlying poster layers");
                go.transform.SetParent(transform,false);
                deeperRenderer=go.AddComponent<MeshRenderer>();
                deeperRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                deeperRenderer.receiveShadows=false;
                deeperMesh=new Mesh { name="Underlying poster layers" };
                go.AddComponent<MeshFilter>().sharedMesh=deeperMesh;
                // Visible lower edges must occlude other stacks, even though they
                // cannot be gripped until their own front sheet is cleared.
                deeperCollider=go.AddComponent<MeshCollider>();
                collisionMesh=new Mesh {name="Complete lower paper collision"};
            }
            deepCount=Mathf.Max(0,posterCount-ClearedPosters-2);
            // Capacity follows the deepest configuration seen, not every layer advance.
            // deepCount is the live range; spare entries never participate in rendering.
            if(deepDesigns==null || deepDesigns.Length<deepCount)
            {
                deepDesigns=new int[deepCount];deepHeights=new float[deepCount];deepWidths=new float[deepCount];deepVisible=new bool[deepCount];
                partialQuads=new bool[deepCount];coveredRects=new Rect[deepCount];
            }
            if(baseVertices==null)baseVertices=new List<Vector3>();
            if(baseUV==null)baseUV=new List<Vector2>();
            // A domain reload can restore a formerly null private array as empty.
            int designCount=game.Settings.PaperCount;
            if(materialGroups==null || materialGroups.Length!=designCount)
            {materialGroups=new int[designCount];materials=new Material[designCount];}
            if(materialOrder==null || materialOrder.Length!=designCount)materialOrder=new int[designCount];
            if(visibleTriangles==null || visibleTriangles.Length!=designCount)
            {
                visibleTriangles=new List<int>[designCount];
                for(int i=0;i<designCount;i++)visibleTriangles[i]=new List<int>();
            }
            baseVertices.Clear();baseUV.Clear();collisionTriangles.Clear();materialCount=0;
            for(int i=0;i<designCount;i++) { materialGroups[i]=-1;materials[i]=game.Settings.Paper(i); }
            for(int layer=ClearedPosters+2;layer<posterCount;layer++)
            {
                int design=Design(layer);var material=materials[design];
                if(materialGroups[design]<0)
                {materialGroups[design]=materialCount;materialOrder[materialCount++]=design;}
                float aspect=game.Settings.PaperAspect(design),w=width/Mathf.Max(1,aspect),h=w*aspect;
                w*=deeperLayerScale;h*=deeperLayerScale;
                deepDesigns[layer-ClearedPosters-2]=design;deepHeights[layer-ClearedPosters-2]=h;deepWidths[layer-ClearedPosters-2]=w;
                float z=LayerDepth(layer);int v=baseVertices.Count;
                baseVertices.Add(new Vector3(-w*.5f,h*.5f,z));baseVertices.Add(new Vector3(w*.5f,h*.5f,z));
                baseVertices.Add(new Vector3(-w*.5f,-h*.5f,z));baseVertices.Add(new Vector3(w*.5f,-h*.5f,z));
                baseUV.Add(new Vector2(0,1));baseUV.Add(new Vector2(1,1));baseUV.Add(new Vector2(0,0));baseUV.Add(new Vector2(1,0));
                collisionTriangles.Add(v);collisionTriangles.Add(v+1);collisionTriangles.Add(v+2);
                collisionTriangles.Add(v+1);collisionTriangles.Add(v+3);collisionTriangles.Add(v+2);
            }
            deeperCollider.sharedMesh=null;
            if(orderedMaterials==null || orderedMaterials.Length!=materialCount)orderedMaterials=new Material[materialCount];
            for(int i=0;i<materialCount;i++)orderedMaterials[i]=materials[materialOrder[i]];
            deeperRenderer.sharedMaterials=orderedMaterials;
            deeperCollider.enabled=deepCount>0;
            collisionMesh.Clear();collisionMesh.SetVertices(baseVertices);
            collisionMesh.SetTriangles(collisionTriangles,0);collisionMesh.RecalculateBounds();
            if(deepCount>0)deeperCollider.sharedMesh=collisionMesh;
            // Upload clipped render geometry once. Collision retains every complete sheet.
            deeperMesh.Clear();deeperMesh.bounds=collisionMesh.bounds;
            visibilityReady=false;hadPartialGeometry=false;UpdateVisibility();
        }
        void LateUpdate() { if(IsRenderVisible)UpdateVisibility(); }
        void UpdateVisibility()
        {
            using var sample=VisibilityMarker.Auto();
            if(!game || !game.view)return;
            // Unity restores serialized arrays on a Play Mode script reload but
            // nested List caches are not serialized. Rebuild the complete set.
            if(deepDesigns==null || deepWidths==null || visibleTriangles==null || baseVertices==null || baseUV==null)
            { UpdateDeeperLayers();return; }
            Vector3 camera=transform.InverseTransformPoint(game.view.transform.position);
            if(visibilityReady && camera==lastCameraPosition)return;
            bool changed=!visibilityReady || hadPartialGeometry;lastCameraPosition=camera;visibilityReady=true;
            bool hasPartialGeometry=false;
            float opaqueZ=0,opaqueHeight=0,opaqueWidth=0;bool hasOpaque=false;
            for(int i=0;i<deepCount;i++)
            {
                float z=LayerDepth(ClearedPosters+2+i);
                bool hidden=false;
                partialQuads[i]=false;
                if(hasOpaque && camera.z<opaqueZ)
                {
                    // Project the opaque sheet's silhouette onto this deeper plane.
                    // Only the intersection is hidden; keep textured border strips.
                    float t=(z-camera.z)/(opaqueZ-camera.z);
                    float x0=Mathf.Max(-deepWidths[i]*.5f,camera.x+(-opaqueWidth*.5f-camera.x)*t);
                    float x1=Mathf.Min(deepWidths[i]*.5f,camera.x+(opaqueWidth*.5f-camera.x)*t);
                    float y0=Mathf.Max(-deepHeights[i]*.5f,camera.y+(-opaqueHeight*.5f-camera.y)*t);
                    float y1=Mathf.Min(deepHeights[i]*.5f,camera.y+(opaqueHeight*.5f-camera.y)*t);
                    if(x1>x0 && y1>y0 && t>1)
                    {
                        hidden=x0<=-deepWidths[i]*.5f && x1>=deepWidths[i]*.5f && y0<=-deepHeights[i]*.5f && y1>=deepHeights[i]*.5f;
                        if(!hidden){partialQuads[i]=true;hasPartialGeometry=true;coveredRects[i]=Rect.MinMaxRect(x0,y0,x1,y1);}
                    }
                }
                bool visible=!hidden;changed|=deepVisible[i]!=visible;deepVisible[i]=visible;
                // Only fully opaque source images may hide deeper geometry.
                // Tear-offs/remnants retain full collision and visible cutouts.
                if(game.Settings.IsPaperOpaque(deepDesigns[i]) && (!hasOpaque || deepHeights[i]*deepWidths[i]>opaqueHeight*opaqueWidth))
                {hasOpaque=true;opaqueZ=z;opaqueHeight=deepHeights[i];opaqueWidth=deepWidths[i];}
            }
            changed|=hasPartialGeometry;hadPartialGeometry=hasPartialGeometry;
            if(!changed)return;
            renderVertices.Clear();renderVertices.AddRange(baseVertices);
            renderUV.Clear();renderUV.AddRange(baseUV);renderNormals.Clear();
            for(int i=0;i<baseVertices.Count;i++)renderNormals.Add(Vector3.back);
            foreach(var list in visibleTriangles)list.Clear();
            for(int i=0;i<deepCount;i++)if(deepVisible[i])
            {
                int group=materialGroups[deepDesigns[i]],v=i*4;
                var list=visibleTriangles[group];
                if(partialQuads[i])
                {
                    var cut=coveredRects[i];float w=deepWidths[i],h=deepHeights[i],z=LayerDepth(ClearedPosters+2+i);
                    AddBorder(list,-w*.5f,-h*.5f,cut.xMin,h*.5f,w,h,z);
                    AddBorder(list,cut.xMax,-h*.5f,w*.5f,h*.5f,w,h,z);
                    AddBorder(list,cut.xMin,-h*.5f,cut.xMax,cut.yMin,w,h,z);
                    AddBorder(list,cut.xMin,cut.yMax,cut.xMax,h*.5f,w,h,z);
                }
                else {list.Add(v);list.Add(v+1);list.Add(v+2);list.Add(v+1);list.Add(v+3);list.Add(v+2);}
            }
            // The clipped vertex count can shrink as the camera moves. Remove
            // old indices first so none reference vertices outside the new buffer.
            var bounds=deeperMesh.bounds;
            deeperMesh.Clear();deeperMesh.subMeshCount=materialCount;
            deeperMesh.SetVertices(renderVertices);deeperMesh.SetUVs(0,renderUV);deeperMesh.SetNormals(renderNormals);
            for(int i=0;i<materialCount;i++)deeperMesh.SetTriangles(visibleTriangles[i],i,false);
            deeperMesh.bounds=bounds;
        }
        void AddBorder(List<int> triangles,float x0,float y0,float x1,float y1,float paperWidth,float height,float z)
        {
            if(x1<=x0 || y1<=y0)return;
            int v=renderVertices.Count;
            renderVertices.Add(new Vector3(x0,y1,z));renderVertices.Add(new Vector3(x1,y1,z));
            renderVertices.Add(new Vector3(x0,y0,z));renderVertices.Add(new Vector3(x1,y0,z));
            renderUV.Add(new Vector2(x0/paperWidth+.5f,y1/height+.5f));renderUV.Add(new Vector2(x1/paperWidth+.5f,y1/height+.5f));
            renderUV.Add(new Vector2(x0/paperWidth+.5f,y0/height+.5f));renderUV.Add(new Vector2(x1/paperWidth+.5f,y0/height+.5f));
            for(int i=0;i<4;i++)renderNormals.Add(Vector3.back);
            triangles.Add(v);triangles.Add(v+1);triangles.Add(v+2);triangles.Add(v+1);triangles.Add(v+3);triangles.Add(v+2);
        }
        void OnDestroy() { if(deeperMesh)Destroy(deeperMesh);if(collisionMesh)Destroy(collisionMesh); }
        public void RecordRemoval(int index) { removedMask|=1<<index; }
        public void AdvanceIfComplete()
        {
            if(!NeedsAdvance)return;
            ClearedPosters++;removedMask=0;game.PosterCleared(this);
            var old=front;front=back;back=old;
            ShowBuffers();
            game.PosterMaterialsChanged(this);
        }
        public void RefreshMaterials()
        {
            if(!game)return;
            Refresh(front,ClearedPosters);Refresh(back,ClearedPosters+1);UpdateDeeperLayers();
        }
        void Refresh(PeelStrip[] buffer,int layer)
        {
            if(layer>=posterCount)return;
            var material=game.Settings.Paper(Design(layer));
            foreach(var strip in buffer)strip.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}
