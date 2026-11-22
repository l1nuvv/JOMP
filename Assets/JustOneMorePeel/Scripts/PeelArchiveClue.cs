using UnityEngine;

namespace JustOneMorePeel
{
    public sealed class PeelArchiveClue : MonoBehaviour
    {
        [SerializeField] PeelSettings placedNoteSettings;
        PeelSettings settings;
        Material material;
        Texture2D russian, english;
        Transform page;
        Light revealLamp;
        LightType originalLampType;
        Quaternion originalLampRotation;
        float originalSpotAngle, originalInnerSpotAngle;

        internal Texture2D CurrentTexture => settings && settings.Language==PeelLanguage.Russian ? russian : english;

        void Start()
        {
            if (placedNoteSettings)
                Initialize(placedNoteSettings, null, GetComponent<Renderer>());
        }

        public void Initialize(PeelSettings owner, Light lamp) => Initialize(owner, lamp, null);

        void Initialize(PeelSettings owner, Light lamp, Renderer placedPage)
        {
            settings=owner;
            russian=Resources.Load<Texture2D>("ArchiveClue_ru");
            english=Resources.Load<Texture2D>("ArchiveClue_en");
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(!settings || !russian || !english || !shader)
            { Debug.LogError("Archive clue: missing settings, texture or shader",this);return; }

            if (!placedPage)
            {
                var placeholder=GetComponent<Renderer>();
                if(placeholder)placeholder.enabled=false;
                var note=GameObject.CreatePrimitive(PrimitiveType.Quad);
                note.name="Archive note";
                note.transform.SetParent(transform,false);
                note.transform.localPosition=new Vector3(0,0,-.6f);
                note.transform.localScale=new Vector3(.47f,.625f,1f);
                Destroy(note.GetComponent<Collider>());
                placedPage=note.GetComponent<Renderer>();
            }
            page=placedPage.transform;
            material=new Material(shader);
            material.SetFloat("_Smoothness",0f);
            placedPage.sharedMaterial=material;
            settings.Changed+=RefreshLanguage;
            RefreshLanguage();
            revealLamp=lamp;
            if(revealLamp)
            {
                originalLampType=revealLamp.type;
                originalLampRotation=revealLamp.transform.localRotation;
                originalSpotAngle=revealLamp.spotAngle;
                originalInnerSpotAngle=revealLamp.innerSpotAngle;
                if(isActiveAndEnabled)FocusLamp();
            }
        }

        void OnEnable() => FocusLamp();

        void FocusLamp()
        {
            if(!revealLamp || !page)return;

            revealLamp.type=LightType.Spot;
            revealLamp.spotAngle=48f;
            revealLamp.innerSpotAngle=26f;
            revealLamp.transform.rotation=Quaternion.LookRotation(page.position-revealLamp.transform.position);
        }

        void OnDisable()
        {
            if(!revealLamp)return;

            revealLamp.type=originalLampType;
            revealLamp.spotAngle=originalSpotAngle;
            revealLamp.innerSpotAngle=originalInnerSpotAngle;
            revealLamp.transform.localRotation=originalLampRotation;
        }

        internal bool CanRead(Camera view)
        {
            if(!isActiveAndEnabled || !page || !view || !CurrentTexture)return false;

            var ray=new Ray(view.transform.position,view.transform.forward);
            var plane=new Plane(page.forward,page.position);
            if(!plane.Raycast(ray,out float distance) || distance>2.5f || Vector3.Dot(ray.direction,page.forward)<=0f)return false;

            var point=page.InverseTransformPoint(ray.GetPoint(distance));
            return Mathf.Abs(point.x)<=.5f && Mathf.Abs(point.y)<=.5f &&
                !Physics.Raycast(ray,distance-.01f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        }

        void RefreshLanguage()
        {
            if(material)material.SetTexture("_BaseMap",CurrentTexture);
        }
        void OnDestroy()
        {
            if(settings)settings.Changed-=RefreshLanguage;
            if(material)Destroy(material);
        }
    }
}
