using UnityEngine;

namespace JustOneMorePeel
{
    // A provisional physical note replaces the old end marker without answering the mystery.
    public sealed class PeelArchiveClue : MonoBehaviour
    {
        PeelSettings settings;
        Material material;
        Texture2D russian, english;

        public void Initialize(PeelSettings owner)
        {
            settings=owner;
            russian=Resources.Load<Texture2D>("ArchiveClue_ru");
            english=Resources.Load<Texture2D>("ArchiveClue_en");
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(!settings || !russian || !english || !shader)
            { Debug.LogError("Archive clue: missing settings, texture or shader",this);return; }

            var placeholder=GetComponent<Renderer>();
            if(placeholder)placeholder.enabled=false;
            var note=GameObject.CreatePrimitive(PrimitiveType.Quad);
            note.name="Archive note";
            note.transform.SetParent(transform,false);
            note.transform.localPosition=new Vector3(0,0,-.6f);
            note.transform.localScale=new Vector3(.47f,.625f,1f);
            Destroy(note.GetComponent<Collider>());
            material=new Material(shader);
            material.SetFloat("_Smoothness",0f);
            note.GetComponent<Renderer>().sharedMaterial=material;
            settings.Changed+=RefreshLanguage;
            RefreshLanguage();
        }

        void RefreshLanguage()
        {
            if(material && settings)material.SetTexture("_BaseMap",settings.Language==PeelLanguage.Russian?russian:english);
        }
        void OnDestroy()
        {
            if(settings)settings.Changed-=RefreshLanguage;
            if(material)Destroy(material);
        }
    }
}
