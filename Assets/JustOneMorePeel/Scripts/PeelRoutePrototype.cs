using System;
using UnityEngine;

namespace JustOneMorePeel
{
    [DefaultExecutionOrder(50)]
    public sealed class PeelRoutePrototype : MonoBehaviour
    {
        [SerializeField] PeelGame game;
        [SerializeField] PeelPosterStack[] courtyard;
        [SerializeField] PeelPosterStack[] doorPosters;
        [SerializeField] PeelPosterStack[] basement;
        [SerializeField] PeelPosterStack[] hall;
        [SerializeField] GameObject courtyardDoor;
        [SerializeField] GameObject basementDoor;
        [SerializeField] GameObject finalReveal;
        [SerializeField] Material passageLettering;
        PeelPosterStack[] localCourtyard;
        TextMesh passageNote, archiveSign;
        Material passagePaper;
        bool courtyardUnlocked, courtyardOpen, basementOpen, courtyardKnockHeard;
        public bool Completed { get; private set; }
        public bool ReviewMode { get; private set; }
        public bool CourtyardDoorOpen => courtyardOpen;
        public bool BasementDoorOpen => basementOpen;
        public bool UsesLocalCourtyardGate => true;
        internal System.Collections.Generic.IReadOnlyList<PeelPosterStack> DoorPosters => doorPosters;
        PeelPosterStack[] CourtyardGoal => localCourtyard;
        int Area => game.player.transform.position.z < 3.35f ? 0 : game.player.transform.position.z < 14f ? 1 : 2;
        public int CurrentArea => Area;
        static int Left(PeelPosterStack[] stacks)
        { int count=0; foreach(var s in stacks) if(s)count+=s.RemainingPosters; return count; }
        public int RemainingHere => Area==0 ? Left(CourtyardGoal) : Area==1 ? Left(basement) : Left(hall);
        public bool IsCourtyardGoal(PeelPosterStack stack) => Array.IndexOf(CourtyardGoal,stack)>=0;
        public string Objective
        {
            get
            {
                bool ru=game.Settings.Language==PeelLanguage.Russian;
                if(ReviewMode)return ru ? "Осмотр маршрута: проходы открыты" : "Route review: passages are open";
                if(Completed)return ru ? "Смена завершена · записка на дальней стене архива" : "Shift complete · the note is on the far archive wall";
                if(Area==0)
                {
                    if(courtyardOpen)return ru ? "Дверь открыта: пройди внутрь" : "The door is open: go inside";
                    if(courtyardUnlocked)return ru ? "Подойди к двери и открой её" : "Approach and open the door";
                    if(courtyardKnockHeard && UsesLocalCourtyardGate)
                        return ru ? "Стук за дверью. Сними листы с двери. Листы на стенах можно оставить."
                            : "Knocking behind the door. Clear its notices. Wall notices are optional.";
                    if(UsesLocalCourtyardGate)return ru ? "Чтобы открыть дверь, сними листы с неё. Листы на стенах можно оставить." : "Clear the door to open it. You can leave the notices on the walls.";
                    if(courtyardKnockHeard)return ru ? "Стук за стеной... Что скрывают объявления?" : "Knocking behind the wall... What are the notices hiding?";
                    return ru ? "Очисти стену и узнай, что за ней" : "Clear the wall and find out what is behind it";
                }
                if(Area==1)return basementOpen ? (ru ? "Архив открыт · пройди через дальний проём" : "Archive open · go through the doorway ahead") : (ru ? "Сними объявления с дальней стены, чтобы открыть проход в архив" : "Clear the notices on the far wall to open the archive passage");
                return ru ? "Сними объявления в архиве, чтобы открыть находку на дальней стене" : "Clear the archive notices to reveal what is on the far wall";
            }
        }
        public string FinishText(string time) => game.Settings.Language==PeelLanguage.Russian ? "Записка открыта. Смена завершена.\nВремя: "+time : "Note uncovered. Shift complete.\nTime: "+time;
        internal void KnockPlayed(int area)
        {
            if(area==0 && !ReviewMode && !courtyardUnlocked)courtyardKnockHeard=true;
        }
        public bool CanOpenDoor()
        {
            if(!courtyardUnlocked || courtyardOpen || !game || !game.view || !courtyardDoor || !courtyardDoor.activeInHierarchy)return false;
            return Physics.Raycast(game.view.transform.position,game.view.transform.forward,out var hit,2.5f) && hit.collider.gameObject==courtyardDoor;
        }
        public bool TryOpenDoor()
        {
            if(!CanOpenDoor())return false;
            OpenCourtyardDoor();return true;
        }
        void OpenCourtyardDoor()
        {
            var door=courtyardDoor.transform;
            var hinge=new GameObject("Prototype door hinge").transform;
            hinge.position=door.position+Vector3.left*door.localScale.x*.5f;
            door.SetParent(hinge,true);
            hinge.rotation=Quaternion.Euler(0,90,0);
            courtyardOpen=true;Physics.SyncTransforms();
        }
        internal void RestoreProgress(bool firstDoorOpen)
        {
            courtyardUnlocked=Left(CourtyardGoal)==0;
            if(firstDoorOpen && courtyardUnlocked && !courtyardOpen)OpenCourtyardDoor();
            basementOpen=Left(basement)==0;
            basementDoor.SetActive(!basementOpen);
            Completed=Left(hall)==0;
            finalReveal.SetActive(Completed);
            Physics.SyncTransforms();
        }
        internal bool CanRestoreProgress(PeelWorldSave data)
        {
            if(!data.firstDoorOpen)return true;
            foreach(var stack in CourtyardGoal)
            {
                bool complete=false;
                foreach(var item in data.stacks)
                    if(item.id==stack.PersistentId){complete=item.cleared==stack.PosterCount;break;}
                if(!complete)return false;
            }
            return true;
        }
        internal void MigrateInteriorSave(PeelWorldSave data)
        {
            if(data.stacks==null)throw new System.IO.InvalidDataException("Missing saved stacks.");
            foreach(var group in new[]{basement,hall})
            foreach(var stack in group)
            {
                var entry=Array.Find(data.stacks,item=>item!=null && item.id==stack.PersistentId);
                if(entry==null)continue;
                int oldDepth=stack.PosterCount/2;
                if(entry.cleared>oldDepth || !PeelWorldSave.ValidStrips(entry.strips,entry.cleared==oldDepth))
                    throw new System.IO.InvalidDataException("Invalid legacy interior progress.");
                if(entry.cleared==oldDepth)entry.cleared=stack.PosterCount;
            }
            data.version=PeelWorldSave.CurrentVersion;
        }
        internal void MigrateCourtyardSave(PeelWorldSave data)
        {
            if(data == null || data.version != 1 || data.stacks == null)return;
            var entries=new System.Collections.Generic.List<PeelStackSave>(data.stacks);
            for(int group=0;group<5;group++)
            {
                var first=doorPosters[group*10];
                var original=entries.Find(entry => entry != null && entry.id==first.PersistentId);
                if(original==null)continue;
                bool legacy=entries.Find(entry => entry != null && entry.id==doorPosters[group*10+1].PersistentId)==null;
                if(!legacy)continue;
                int cleared=original.cleared;
                if(cleared<0 || cleared>50 || !PeelWorldSave.ValidStrips(original.strips,cleared==50))
                    throw new System.IO.InvalidDataException("Invalid legacy courtyard progress.");
                if(data.firstDoorOpen && cleared==13)cleared=50;
                var partial=original.strips;
                for(int i=0;i<10;i++)
                {
                    var stack=doorPosters[group*10+i];
                    var item=i==0 ? original : new PeelStackSave {id=stack.PersistentId};
                    item.cleared=Mathf.Clamp(cleared-i*5,0,5);
                    item.strips=item.cleared<5 && cleared>=i*5 && cleared<i*5+5 ? partial : new float[4];
                    if(i>0)entries.Add(item);
                }
            }
            data.stacks=entries.ToArray();
            bool oldOpenDoor=data.firstDoorOpen;
            foreach(var stack in localCourtyard)
            {
                var item=Array.Find(data.stacks, entry => entry != null && entry.id==stack.PersistentId);
                if(item == null || item.cleared!=13)oldOpenDoor=false;
            }
            foreach(var stack in courtyard)
            {
                var item=Array.Find(data.stacks, entry => entry != null && entry.id==stack.PersistentId);
                if(item == null)continue;
                if(oldOpenDoor && IsCourtyardGoal(stack))
                {
                    item.cleared=stack.PosterCount;
                    item.strips=new float[4];
                }
                else if(!IsCourtyardGoal(stack) && item.cleared>stack.PosterCount && item.cleared<=14)
                {
                    item.cleared=stack.PosterCount;
                    item.strips=new float[4];
                }
            }
        }
#if UNITY_EDITOR
        public void ClearCurrentAreaForTesting()
        {
            var selected=Area==0 ? CourtyardGoal : Area==1 ? basement : hall;
            foreach(var stack in selected)
            {
                if(!stack || !stack.gameObject.activeInHierarchy)continue;
                while(stack.RemainingPosters>0)
                {
                    foreach(var strip in stack.CurrentStrips)if(strip.IsExposed)strip.Pull(1f);
                    if(!stack.NeedsAdvance)break;
                    stack.AdvanceIfComplete();
                }
            }
            Physics.SyncTransforms();
        }
#endif
        public void OpenForReview()
        {
            ReviewMode=true;courtyardUnlocked=true;courtyardOpen=true;basementOpen=true;
            courtyardDoor.SetActive(false);
            // In the route-only review, paper covering the passages is hidden so
            // the geometry can be walked without clearing hundreds of sheets.
            foreach(var stack in courtyard) if(stack)stack.gameObject.SetActive(false);
            DisableBasementCollisions();
            Physics.SyncTransforms();
        }
        public void BasementLightingChanged(bool lightOn)
        {
            if(passageNote)passageNote.transform.parent.gameObject.SetActive(lightOn);
            if(!ReviewMode || !basementDoor)return;
            basementDoor.SetActive(!lightOn);
            Physics.SyncTransforms();
        }
        void DisableBasementCollisions()
        {
            if(!ReviewMode)return;
            foreach(var stack in basement)
                if(stack)foreach(var collider in stack.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        }
        void Start()
        {
            if(!game || courtyard==null || basement==null || hall==null || !courtyardDoor || !basementDoor || !finalReveal)
            {Debug.LogError("Route prototype: missing scene references",this);enabled=false;return;}
            // The trial uses stacks centered within the doorway, including the overlapping edge sheets.
            localCourtyard=doorPosters;
            gameObject.AddComponent<PeelKnockCue>().Initialize(game,this,courtyardDoor,basementDoor);
            finalReveal.AddComponent<PeelArchiveClue>().Initialize(game.Settings);
            CreatePassageNote();
            var basementProps=Resources.Load<GameObject>("BasementStorageCandidate");
            var archiveProps=Resources.Load<GameObject>("ArchiveStorageCandidates");
            if(basementProps)Instantiate(basementProps,basementDoor.transform.parent,false).name="Basement service storage";
            if(archiveProps)
            {
                var storage=Instantiate(archiveProps,basementDoor.transform.parent,false);
                storage.name="Archive storage";
                foreach(Transform item in storage.transform)
                    item.localPosition+=new Vector3(0,0,5.5f);
            }

            game.PosterWasCleared+=OnPosterCleared;
            if(game.Settings)game.Settings.Changed+=RefreshRouteLanguage;
        }
        void CreatePassageNote()
        {
            if(!passageLettering)return;
            var door=basementDoor.transform;
            var note=GameObject.CreatePrimitive(PrimitiveType.Quad);
            note.name="Archive passage note";
            Destroy(note.GetComponent<Collider>());
            note.transform.position=door.TransformPoint(new Vector3(0f,.45f,-.55f));
            note.transform.rotation=door.rotation;
            note.transform.localScale=new Vector3(.58f,.23f,1f);
            note.transform.SetParent(door,true);
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            passagePaper=new Material(shader) { color=new Color(.82f,.73f,.54f) };
            passagePaper.SetFloat("_Smoothness",0f);
            note.GetComponent<Renderer>().sharedMaterial=passagePaper;

            passageNote=new GameObject("Passage note lettering").AddComponent<TextMesh>();
            passageNote.transform.SetPositionAndRotation(note.transform.position-door.forward*.001f,door.rotation);
            passageNote.transform.SetParent(note.transform,true);
            passageNote.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            passageNote.GetComponent<Renderer>().sharedMaterial=passageLettering;
            passageNote.fontSize=64;
            passageNote.characterSize=.0105f;
            passageNote.anchor=TextAnchor.MiddleCenter;
            passageNote.alignment=TextAlignment.Center;
            passageNote.color=new Color(.19f,.13f,.08f);

            // This sign sits beyond the barrier and stays with the opening.
            var sign=Instantiate(note,door.TransformPoint(new Vector3(0f,.40f,.58f)),door.rotation,door.parent);
            sign.name="Archive doorway sign";
            sign.transform.localScale=new Vector3(.83f,.22f,1f);
            var signCollider=sign.GetComponent<Collider>();
            if(signCollider)Destroy(signCollider);
            archiveSign=sign.GetComponentInChildren<TextMesh>();
            archiveSign.characterSize=.018f;
            RefreshRouteLanguage();
            note.SetActive(false);
        }
        void RefreshRouteLanguage()
        {
            if(passageNote)passageNote.text=game.Settings.Language==PeelLanguage.Russian
                ? "АРХИВ\nза этой дверью" : "ARCHIVE\nthrough this door";
            if(archiveSign)archiveSign.text=game.Settings.Language==PeelLanguage.Russian ? "АРХИВ" : "ARCHIVE";
            DisableBasementCollisions();
        }
        void OnDestroy()
        {
            if(game)
            {
                game.PosterWasCleared-=OnPosterCleared;
                if(game.Settings)game.Settings.Changed-=RefreshRouteLanguage;
            }
            if(passagePaper)Destroy(passagePaper);
        }
        void OnPosterCleared(PeelPosterStack stack)
        {
            if(!courtyardUnlocked && Left(CourtyardGoal)==0)courtyardUnlocked=true;
            if(!basementOpen && Left(basement)==0){basementOpen=true;basementDoor.SetActive(false);Physics.SyncTransforms();}
            if(!Completed && Left(hall)==0){Completed=true;finalReveal.SetActive(true);}
        }
    }
}
