using UnityEngine;
using UnityEngine.Rendering;

namespace JustOneMorePeel
{
    // A provisional cue: the source and answer to the knocking remain unknown.
    public sealed class PeelKnockCue : MonoBehaviour
    {
        [SerializeField] float firstDelayMin=10f, firstDelayMax=20f;
        [SerializeField] float repeatDelayMin=25f, repeatDelayMax=50f;
        [SerializeField] float courtyardVolume=.18f, basementVolume=.8f;
        PeelGame game;
        PeelRoutePrototype route;
        GameObject courtyardDoor, basementDoor;
        AudioSource sound;
        AudioLowPassFilter filter;
        Transform doorVisual;
        float nextEvent, nextBeat, shakeTime;
        int lastArea=-1, beatsLeft;

        public void Initialize(PeelGame owner,PeelRoutePrototype routeOwner,GameObject firstDoor,GameObject secondDoor)
        {
            game=owner;route=routeOwner;courtyardDoor=firstDoor;basementDoor=secondDoor;
            var audioObject=new GameObject("Distant knocking audio");audioObject.transform.SetParent(transform,false);
            sound=audioObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=1;
            sound.rolloffMode=AudioRolloffMode.Linear;sound.minDistance=4;sound.maxDistance=30;
            sound.clip=Resources.Load<AudioClip>("DoorKnock");
            filter=audioObject.AddComponent<AudioLowPassFilter>();
            if(!sound.clip)Debug.LogError("Knock cue: DoorKnock audio is missing",this);

            // Move only a renderer copy: the actual door and collider stay fixed.
            var original=basementDoor.GetComponent<MeshRenderer>();
            var mesh=basementDoor.GetComponent<MeshFilter>();
            if(original && mesh && mesh.sharedMesh)
            {
                var visual=new GameObject("Basement door animated surface");
                visual.transform.SetParent(basementDoor.transform,false);
                visual.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
                var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterials=original.sharedMaterials;
                renderer.shadowCastingMode=original.shadowCastingMode;renderer.receiveShadows=original.receiveShadows;
                renderer.renderingLayerMask=original.renderingLayerMask;
                original.enabled=false;doorVisual=visual.transform;
            }
            else Debug.LogWarning("Knock cue: basement door has no mesh to animate",this);
        }
        void Update()
        {
            if(!game || !route || !sound || !sound.clip)return;
            int area=route.CurrentArea;
            bool active=game.CurrentScreen==PeelScreen.Playing && area<2 && basementDoor && basementDoor.activeInHierarchy;
            if(!active)
            {
                if(sound.isPlaying)sound.Stop();
                beatsLeft=0;lastArea=-1;shakeTime=0;
                if(doorVisual)doorVisual.localPosition=Vector3.zero;
                return;
            }
            if(area!=lastArea)
            {
                sound.Stop();beatsLeft=0;shakeTime=0;lastArea=area;
                nextEvent=Random.Range(firstDelayMin,firstDelayMax);
            }
            sound.transform.position=area==0 ? courtyardDoor.transform.position : basementDoor.transform.position;
            sound.volume=game.Settings.SoundVolume*(area==0 ? courtyardVolume : basementVolume);
            filter.cutoffFrequency=area==0 ? 900f : 4500f;
            float dt=Time.deltaTime;
            if(beatsLeft==0)
            {
                nextEvent-=dt;
                if(nextEvent<=0){beatsLeft=Random.Range(2,4);nextBeat=0;}
            }
            if(beatsLeft>0)
            {
                nextBeat-=dt;
                if(nextBeat<=0)
                {
                    sound.PlayOneShot(sound.clip);beatsLeft--;
                    route.KnockPlayed(area);
                    if(area==1)shakeTime=.38f;
                    nextBeat=Random.Range(.22f,.33f);
                    if(beatsLeft==0)nextEvent=Random.Range(repeatDelayMin,repeatDelayMax);
                }
            }
            if(doorVisual)
            {
                if(shakeTime>0 && area==1)
                {
                    shakeTime=Mathf.Max(0,shakeTime-dt);
                    float impulse=Mathf.Sin((.38f-shakeTime)*42f)*(shakeTime/.38f);
                    // Convert metres to local space: the door mesh has non-uniform scale.
                    doorVisual.localPosition=basementDoor.transform.InverseTransformVector(new Vector3(impulse*.035f,0,impulse*.025f));
                }
                else doorVisual.localPosition=Vector3.zero;
            }
        }
    }
}
