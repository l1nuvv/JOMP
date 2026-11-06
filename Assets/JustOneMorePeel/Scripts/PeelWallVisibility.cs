using System.Collections.Generic;
using UnityEngine;

namespace JustOneMorePeel
{
    /// <summary>Conservative patch visibility and a shared spatial index for interaction.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PeelWallVisibility : MonoBehaviour
    {
        sealed class Patch
        {
            public PeelPosterStack stack;
            public Renderer[] renderers;
            public Bounds bounds;
        }
        const float CellSize=2f;
        [SerializeField,Min(10)] float drawDistance=60;
        readonly Dictionary<Vector3Int,List<PeelPosterStack>> cells=new Dictionary<Vector3Int,List<PeelPosterStack>>();
        readonly List<Patch> patches=new List<Patch>();
        readonly Plane[] planes=new Plane[6];
        Camera view;
        public int VisiblePatches { get; private set; }
        static Vector3Int Cell(Vector3 p) => Vector3Int.FloorToInt(p/CellSize);
        public void Initialize(Camera camera,List<PeelPosterStack> stacks)
        {
            OnDisable();patches.Clear();cells.Clear();
            view=camera;
            foreach(var stack in stacks)
            {
                var renderers=stack.GetComponentsInChildren<Renderer>();
                var bounds=renderers[0].bounds;
                foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                // Includes curling/falling motion while a sheet is still gripped.
                bounds.Expand(new Vector3(1,2.5f,3));
                patches.Add(new Patch {stack=stack,renderers=renderers,bounds=bounds});
                var cell=Cell(stack.transform.position);
                if(!cells.TryGetValue(cell,out var bucket)){bucket=new List<PeelPosterStack>();cells.Add(cell,bucket);}
                bucket.Add(stack);
            }
            LateUpdate();
        }
        public void FindNearby(Vector3 position,float radius,List<PeelPosterStack> result)
        {
            result.Clear();var low=Cell(position-Vector3.one*radius);var high=Cell(position+Vector3.one*radius);
            float squared=radius*radius;
            for(int x=low.x;x<=high.x;x++)for(int y=low.y;y<=high.y;y++)for(int z=low.z;z<=high.z;z++)
                if(cells.TryGetValue(new Vector3Int(x,y,z),out var bucket))
                    foreach(var stack in bucket)if((stack.transform.position-position).sqrMagnitude<=squared)result.Add(stack);
        }
        void LateUpdate()
        {
            if(!view)return;
            GeometryUtility.CalculateFrustumPlanes(view,planes);VisiblePatches=0;
            foreach(var patch in patches)
            {
                bool visible=patch.bounds.SqrDistance(view.transform.position)<=drawDistance*drawDistance && GeometryUtility.TestPlanesAABB(planes,patch.bounds);
                if(visible)VisiblePatches++;
                if(patch.stack.IsRenderVisible!=visible)
                {
                    patch.stack.IsRenderVisible=visible;
                    foreach(var renderer in patch.renderers)renderer.forceRenderingOff=!visible;
                }
            }
        }
        void OnDisable()
        {
            foreach(var patch in patches)
            {
                if(patch.stack)patch.stack.IsRenderVisible=true;
                foreach(var r in patch.renderers)if(r)r.forceRenderingOff=false;
            }
        }
    }
}
