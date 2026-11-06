using System.Collections.Generic;
using UnityEngine;

namespace JustOneMorePeel
{
    // Greybox lighting is applied to the room itself, even when seen from outside.
    [DefaultExecutionOrder(100)]
    public sealed class PeelBasementLight : MonoBehaviour
    {
        [SerializeField] PeelGame game;
        [SerializeField] Collider lightSwitch;
        [SerializeField] Light basementLamp;
        [SerializeField] Transform basementRoot;
        [SerializeField] Shader darknessShader;
        [SerializeField] float entryZ=3.35f;
        [SerializeField] float exitZ=14f;
        [SerializeField] float interactionReach=2.2f;
        public bool IsOn { get; private set; }
        readonly List<Renderer> surfaces=new List<Renderer>();
        readonly List<Material[]> originalMaterials=new List<Material[]>();
        readonly Dictionary<Material,Material> dimMaterials=new Dictionary<Material,Material>();
        PeelRoutePrototype route;
        public bool IsDark => game && game.player && !IsOn && game.player.transform.position.z>=entryZ && game.player.transform.position.z<exitZ;
        void Start()
        {
            if (!game || !lightSwitch || !basementLamp || !basementRoot || !darknessShader) { Debug.LogError("Basement light: missing references",this);enabled=false;return; }
            route=game.GetComponent<PeelRoutePrototype>();
            foreach(Transform child in basementRoot)
            {
                if(!child.name.StartsWith("Basement "))continue;
                foreach(var renderer in child.GetComponentsInChildren<Renderer>(true))
                {
                    if(renderer.name=="Switch indicator")continue;
                    surfaces.Add(renderer);originalMaterials.Add(renderer.sharedMaterials);
                }
            }
            SetLight(false);
            if(game.Settings)game.Settings.Changed+=RefreshMaterials;
        }
        void OnDestroy()
        {
            if(game && game.Settings)game.Settings.Changed-=RefreshMaterials;
            foreach(var material in dimMaterials.Values)if(material)Destroy(material);
        }
        void Update()
        {
            float doorway=route && route.CourtyardDoorOpen ? 1f : 0f;
            foreach(var material in dimMaterials.Values)if(material)material.SetFloat("_DoorOpen",doorway);
        }
        Material DimMaterial(Material source)
        {
            if(!source)return null;
            if(dimMaterials.TryGetValue(source,out var dim))return dim;
            dim=new Material(darknessShader);
            string textureProperty=source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            if(source.HasProperty(textureProperty))
            {
                dim.SetTexture("_BaseMap",source.GetTexture(textureProperty));
                dim.SetTextureScale("_BaseMap",source.GetTextureScale(textureProperty));
                dim.SetTextureOffset("_BaseMap",source.GetTextureOffset(textureProperty));
            }
            if(source.HasProperty("_BaseColor"))dim.SetColor("_BaseColor",source.GetColor("_BaseColor"));
            else if(source.HasProperty("_Color"))dim.SetColor("_BaseColor",source.GetColor("_Color"));
            dim.SetFloat("_DoorOpen",route && route.CourtyardDoorOpen ? 1f : 0f);
            dimMaterials.Add(source,dim);return dim;
        }
        public bool CanInteract()
        {
            if (!game || !game.view || !game.player || !lightSwitch || !IsInBasement()) return false;
            return Physics.Raycast(game.view.transform.position,game.view.transform.forward,out var hit,interactionReach) && hit.collider==lightSwitch;
        }
        bool IsInBasement() => game.player.transform.position.z>=entryZ && game.player.transform.position.z<exitZ;
        public bool TryToggle()
        {
            if (!CanInteract()) return false;
            SetLight(!IsOn);return true;
        }
        internal void RestoreProgress(bool on) { SetLight(on); }
        void SetLight(bool on)
        {
            IsOn=on;basementLamp.enabled=on;
            if(route)route.BasementLightingChanged(on);
            for(int i=0;i<surfaces.Count;i++)
            {
                if(!surfaces[i])continue;
                if(on){surfaces[i].sharedMaterials=originalMaterials[i];continue;}
                ApplyDarkMaterials(i);
            }
        }
        void ApplyDarkMaterials(int i)
        {
            var materials=originalMaterials[i];
            var dim=new Material[materials.Length];
            for(int n=0;n<dim.Length;n++)dim[n]=DimMaterial(materials[n]);
            surfaces[i].sharedMaterials=dim;
        }
        void RefreshMaterials()
        {
            // PeelGame refreshes localized materials before this subscriber runs.
            for(int i=0;i<surfaces.Count;i++)
            {
                if(!surfaces[i])continue;
                CaptureChangedMaterials(i);
                if(!IsOn)ApplyDarkMaterials(i);
            }
        }
        public void RefreshPosterMaterials(PeelPosterStack stack)
        {
            if(!stack || !stack.transform.IsChildOf(basementRoot))return;
            for(int i=0;i<surfaces.Count;i++)
            {
                if(!surfaces[i] || !surfaces[i].transform.IsChildOf(stack.transform))continue;
                CaptureChangedMaterials(i);
                if(!IsOn)ApplyDarkMaterials(i);
            }
        }
        void CaptureChangedMaterials(int i)
        {
            var materials=surfaces[i].sharedMaterials;
            if(materials.Length!=originalMaterials[i].Length){originalMaterials[i]=materials;return;}
            for(int n=0;n<materials.Length;n++)
                if(materials[n] && materials[n].shader!=darknessShader)originalMaterials[i][n]=materials[n];
        }
    }
}
