using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JustOneMorePeel
{
    /// <summary>Fixed-size visual-only pool. Logical poster depth does not allocate falling objects.</summary>
    public sealed class PeelScrapPool : MonoBehaviour
    {
        public const int Capacity=32;
        sealed class Scrap
        {
            public GameObject go;
            public Mesh mesh;
            public MeshRenderer renderer;
            public Vector3 origin;
            public Quaternion initialRotation;
            public float age;
            public float opacity;
            public Color color;
            public MaterialPropertyBlock properties;
        }
        readonly Scrap[] scraps=new Scrap[Capacity];
        readonly Dictionary<Material,Material> fallingMaterials=new Dictionary<Material,Material>();
        Camera view;
        CharacterController player;
        int cursor;
        public int TotalEmissions { get; private set; }
        void Awake() { EnsurePool(); }
        void OnEnable() { EnsurePool(); }
        public void Initialize(Camera camera,GameObject playerObject)
        { view=camera;player=playerObject?playerObject.GetComponent<CharacterController>():null; }
        void EnsurePool()
        {
            for(int i=0;i<Capacity;i++)
            {
                if(scraps[i]!=null && scraps[i].go && scraps[i].mesh && scraps[i].renderer)continue;
                var child=transform.Find("Falling paper "+i);
                var go=child ? child.gameObject : new GameObject("Falling paper "+i);
                if(!child)go.transform.SetParent(transform,false);
                var filter=go.GetComponent<MeshFilter>();if(!filter)filter=go.AddComponent<MeshFilter>();
                var mesh=filter.sharedMesh;
                if(!mesh){mesh=new Mesh {name="Pooled paper scrap"};mesh.MarkDynamic();filter.sharedMesh=mesh;}
                var renderer=go.GetComponent<MeshRenderer>();if(!renderer)renderer=go.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                go.SetActive(false);scraps[i]=new Scrap {go=go,mesh=mesh,renderer=renderer,properties=new MaterialPropertyBlock()};
            }
        }
        Material FallingMaterial(Material source)
        {
            if(!source)return null;
            if(fallingMaterials.TryGetValue(source,out var cached))return cached;
            var material=new Material(source) { name=source.name+" (falling)" };
            if(source.shader.name=="JustOneMorePeel/BasementBlackout")
            {
                var shader=Resources.Load<Shader>("BasementFallingPaper");
                if(shader)material.shader=shader;
                else Debug.LogError("Missing basement falling paper shader",this);
            }
            else if(source.shader.name=="Universal Render Pipeline/Lit")
            {
                material.SetFloat("_Surface",1);
                material.SetFloat("_Blend",0);
                material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);
                material.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite",0);
                material.SetFloat("_AlphaToMask",0);
                material.SetFloat("_Cutoff",.01f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.SetOverrideTag("RenderType","Transparent");
                material.SetShaderPassEnabled("DepthOnly",false);
                material.renderQueue=(int)RenderQueue.Transparent;
            }
            fallingMaterials.Add(source,material);return material;
        }
        void SetMaterial(Scrap scrap,Material source)
        {
            var falling=FallingMaterial(source);
            if(source && falling && source.HasProperty("_DoorOpen") && falling.HasProperty("_DoorOpen"))
                falling.SetFloat("_DoorOpen",source.GetFloat("_DoorOpen"));
            scrap.renderer.sharedMaterial=falling;
            scrap.color=source && source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):Color.white;
            scrap.opacity=1f;
            ApplyOpacity(scrap);
        }
        static void ApplyOpacity(Scrap scrap)
        {
            var color=scrap.color;color.a*=scrap.opacity;
            scrap.renderer.GetPropertyBlock(scrap.properties);
            scrap.properties.SetColor("_BaseColor",color);
            scrap.renderer.SetPropertyBlock(scrap.properties);
        }
        public void Emit(PeelStrip strip)
        {
            if(scraps[cursor]==null || !scraps[cursor].go || !scraps[cursor].mesh || !scraps[cursor].renderer)EnsurePool();
            var scrap=scraps[cursor];cursor=(cursor+1)%Capacity;
            TotalEmissions++;
            strip.CopyMeshTo(scrap.mesh);
            SetMaterial(scrap,strip.GetComponent<Renderer>().sharedMaterial);
            scrap.origin=strip.transform.position;scrap.age=0;
            scrap.go.transform.SetPositionAndRotation(strip.transform.position,strip.transform.rotation);
            scrap.initialRotation=strip.transform.rotation;
            scrap.go.transform.localScale=strip.transform.lossyScale;scrap.go.SetActive(true);
        }
        public void EmitPoster(PeelPosterStack stack)
        {
            if(scraps[cursor]==null || !scraps[cursor].go || !scraps[cursor].mesh || !scraps[cursor].renderer)EnsurePool();
            var scrap=scraps[cursor];cursor=(cursor+1)%Capacity;TotalEmissions++;
            var strip=stack.CurrentStrips[0];strip.CopyWholePosterTo(scrap.mesh);
            SetMaterial(scrap,strip.GetComponent<Renderer>().sharedMaterial);
            float centerX=strip.transform.localPosition.x+strip.width*1.5f;
            scrap.origin=stack.transform.TransformPoint(new Vector3(centerX,0,strip.transform.localPosition.z));scrap.age=0;
            scrap.go.transform.SetPositionAndRotation(scrap.origin,stack.transform.rotation);
            scrap.initialRotation=stack.transform.rotation;
            scrap.go.transform.localScale=stack.transform.lossyScale;scrap.go.SetActive(true);
        }
        void Update()
        {
            foreach(var scrap in scraps)
            {
                if(scrap==null || !scrap.go || !scrap.go.activeSelf)continue;
                scrap.age+=Time.deltaTime;
                // Leave along the paper face, while gravity remains in world space.
                float drift=(1f-Mathf.Cos(scrap.age*4f))*.04f;
                Vector3 localMotion=new Vector3(drift,0,-scrap.age*.35f);
                scrap.go.transform.position=scrap.origin+scrap.initialRotation*localMotion+Vector3.down*(scrap.age*scrap.age*1.8f);
                scrap.go.transform.rotation=scrap.initialRotation*Quaternion.Euler(45f*scrap.age,35f*scrap.age,55f*scrap.age);
                if(scrap.age>2.2f)scrap.go.SetActive(false);
            }
            foreach(var scrap in scraps)
            {
                if(scrap==null || !scrap.go || !scrap.go.activeSelf)continue;
                var bounds=scrap.renderer.bounds;
                bool nearPlayer=(view && bounds.SqrDistance(view.transform.position)<.16f)
                    || (player && player.enabled && bounds.Intersects(player.bounds));
                bool overlapping=false;
                foreach(var other in scraps)
                {
                    if(other==null || other==scrap || !other.go || !other.go.activeSelf)continue;
                    if(bounds.Intersects(other.renderer.bounds)){overlapping=true;break;}
                }
                float target=nearPlayer?.18f:overlapping?.5f:1f;
                scrap.opacity=Mathf.MoveTowards(scrap.opacity,target,Time.deltaTime*5f);
                ApplyOpacity(scrap);
            }
        }
        void OnDestroy()
        {
            foreach(var scrap in scraps)if(scrap!=null && scrap.mesh)Destroy(scrap.mesh);
            foreach(var material in fallingMaterials.Values)if(material)Destroy(material);
        }
    }
}
