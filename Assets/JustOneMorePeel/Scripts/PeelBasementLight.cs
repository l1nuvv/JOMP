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
        internal bool SwitchIsOn { get; private set; }
        readonly List<Renderer> surfaces=new List<Renderer>();
        readonly List<Material[]> originalMaterials=new List<Material[]>();
        readonly Dictionary<Material,Material> dimMaterials=new Dictionary<Material,Material>();
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor");
        static readonly Color SwitchOnColor=new Color(.23f,.72f,.42f);
        Renderer switchIndicator;
        MaterialPropertyBlock switchIndicatorProperties;
        Color switchOffColor;
        const float SwitchClickGain=.48f;
        AudioSource switchClick;
        float fullLampIntensity;
        const float LampDelaySeconds=.55f, LampWarmupSeconds=.38f, LampStartLevel=.12f;
        float lampDelayRemaining;
        PeelRoutePrototype route;
        public bool IsDark => game && game.player && !IsOn && game.player.transform.position.z>=entryZ && game.player.transform.position.z<exitZ;
        void Start()
        {
            if (!game || !lightSwitch || !basementLamp || !basementRoot || !darknessShader) { Debug.LogError("Basement light: missing references",this);enabled=false;return; }
            route=game.GetComponent<PeelRoutePrototype>();
            InitializeSwitchFeedback();
            foreach(Transform child in basementRoot)
            {
                if(!child.name.StartsWith("Basement "))continue;
                foreach(var renderer in child.GetComponentsInChildren<Renderer>(true))
                {
                    if(renderer.name=="Switch indicator")continue;
                    surfaces.Add(renderer);originalMaterials.Add(renderer.sharedMaterials);
                }
            }
            fullLampIntensity=basementLamp.intensity;
            SetLight(false);
            if(game.Settings)game.Settings.Changed+=ApplySettings;
        }
        void InitializeSwitchFeedback()
        {
            var indicator=lightSwitch.transform.Find("Switch indicator");
            if(indicator && indicator.TryGetComponent(out switchIndicator) && switchIndicator.sharedMaterial && switchIndicator.sharedMaterial.HasProperty(BaseColor))
            {
                switchOffColor=switchIndicator.sharedMaterial.GetColor(BaseColor);
                switchIndicatorProperties=new MaterialPropertyBlock();
                switchIndicator.GetPropertyBlock(switchIndicatorProperties);
            }

            var clip=Resources.Load<AudioClip>("BasementSwitchClick");
            if(!clip)return;
            switchClick=lightSwitch.gameObject.AddComponent<AudioSource>();
            switchClick.playOnAwake=false;
            switchClick.clip=clip;
            switchClick.volume=game.Settings.SoundVolume*SwitchClickGain;
            switchClick.spatialBlend=1f;
            switchClick.dopplerLevel=0f;
            switchClick.rolloffMode=AudioRolloffMode.Linear;
            switchClick.minDistance=.95f;
            switchClick.maxDistance=4f;
        }
        void OnDestroy()
        {
            if(game && game.Settings)game.Settings.Changed-=ApplySettings;
            foreach(var material in dimMaterials.Values)if(material)Destroy(material);
        }
        void Update()
        {
            if(SwitchIsOn && !IsOn)
            {
                lampDelayRemaining-=Time.deltaTime;
                if(lampDelayRemaining<=0f)SetLight(true,true);
            }
            else if(IsOn && basementLamp.intensity<fullLampIntensity)
                basementLamp.intensity=Mathf.MoveTowards(basementLamp.intensity,fullLampIntensity,
                    fullLampIntensity*(1f-LampStartLevel)*Time.deltaTime/LampWarmupSeconds);
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
            SetSwitch(!SwitchIsOn);
            if(SwitchIsOn)lampDelayRemaining=LampDelaySeconds;
            else SetLight(false);
            if(switchClick && game.Settings.SoundVolume>0f)
            {
                switchClick.pitch=SwitchIsOn ? 1f : .88f;
                switchClick.Play();
            }
            return true;
        }
        internal void RestoreProgress(bool on)
        {
            SetSwitch(on);
            SetLight(on);
        }
        void SetSwitch(bool on)
        {
            SwitchIsOn=on;
            if(switchIndicator && switchIndicatorProperties!=null)
            {
                switchIndicatorProperties.SetColor(BaseColor,on ? SwitchOnColor : switchOffColor);
                switchIndicator.SetPropertyBlock(switchIndicatorProperties);
            }
        }
        void SetLight(bool on,bool warmUp=false)
        {
            IsOn=on;
            basementLamp.intensity=on && warmUp ? fullLampIntensity*LampStartLevel : fullLampIntensity;
            basementLamp.enabled=on;
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
        void ApplySettings()
        {
            if(switchClick)switchClick.volume=game.Settings.SoundVolume*SwitchClickGain;
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
