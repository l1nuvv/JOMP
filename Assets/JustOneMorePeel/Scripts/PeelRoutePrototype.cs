using System;
using System.Collections;
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
        [SerializeField] Light archiveRevealLamp;
        [SerializeField] Material passageLettering;
        [SerializeField] bool mainRoute;
        [SerializeField] float basementEntryZ=3.35f;
        [SerializeField] float archiveEntryZ=14f;
        PeelPosterStack[] localCourtyard, optionalCourtyard;
        TextMesh passageNote, archiveSign;
        Material passagePaper;
        Renderer courtyardLever;
        Material courtyardLeverSteel, courtyardLeverReady;
        PeelArchiveClue archiveClue;
        PeelKnockCue knockCue;
        Vector3 basementDoorClosedPosition;
        Coroutine basementDoorOpening;
        const float BasementDoorOpenDuration=.65f;
        bool courtyardUnlocked, courtyardOpen, basementOpen, courtyardKnockHeard;
        public bool Completed { get; private set; }
        public bool ReviewMode { get; private set; }
        public bool CourtyardDoorOpen => courtyardOpen;
        public bool BasementDoorOpen => basementOpen;
        public bool UsesLocalCourtyardGate => true;
        internal System.Collections.Generic.IReadOnlyList<PeelPosterStack> DoorPosters => doorPosters;
        PeelPosterStack[] CourtyardGoal => localCourtyard;
        int Area => AreaAt(game.player.transform.position.z);
        internal int AreaAt(float z) => z<basementEntryZ ? 0 : z<archiveEntryZ ? 1 : 2;
        internal bool IsMainRoute => mainRoute;
        internal Vector3 CourtyardEntry { get; private set; }
        internal Transform CourtyardCart { get; private set; }
        public int CurrentArea => Area;
        internal bool KnockActive => knockCue && knockCue.KnockActive;
        static int Left(PeelPosterStack[] stacks)
        { int count=0; foreach(var s in stacks) if(s)count+=s.RemainingPosters; return count; }
        static int Total(PeelPosterStack[] stacks)
        { int count=0; foreach(var s in stacks) if(s)count+=s.PosterCount; return count; }
        public int RemainingHere => RemainingInArea(Area);
        internal int RemainingInArea(int area) => Left(area==0 ? CourtyardGoal : area==1 ? basement : hall);
        internal int RequiredNoticesTotal => Total(CourtyardGoal)+Total(basement)+Total(hall);
        internal int RequiredNoticesLeft => Left(CourtyardGoal)+Left(basement)+Left(hall);
        public bool IsCourtyardGoal(PeelPosterStack stack) => Array.IndexOf(CourtyardGoal,stack)>=0;
        internal int OptionalCourtyardNoticesLeft => Left(optionalCourtyard);
        internal bool IsOptionalCourtyardNotice(PeelPosterStack stack) => Array.IndexOf(optionalCourtyard,stack)>=0;
        internal Texture2D ArchiveNoteTexture => archiveClue ? archiveClue.CurrentTexture : null;
        internal bool CanReadArchiveNote() => Completed && !ReviewMode && archiveClue && archiveClue.CanRead(game.view);
        public string Objective
        {
            get
            {
                bool ru=game.Settings.Language==PeelLanguage.Russian;
                if(ReviewMode)return ru ? "Осмотр маршрута: проходы открыты" : "Route review: passages are open";
                if(Completed)
                {
                    if(Area==0)
                    {
                        int optionalLeft=OptionalCourtyardNoticesLeft;
                        if(optionalLeft==0)return ru ? "Двор полностью очищен" : "Courtyard fully cleared";
                        return ru ? $"Основная работа выполнена · во дворе осталось необязательных листов — {optionalLeft}"
                            : $"Required work complete · optional courtyard notices left — {optionalLeft}";
                    }
                    return ru ? "Обязательные листы сняты. Записка на дальней стене архива."
                        : "Required notices cleared. The note is on the far archive wall.";
                }
                if(Area==0)
                {
                    if(courtyardOpen)return ru ? "Дверь открыта · спустись в подвал" : "Door open · go down to the basement";
                    if(courtyardUnlocked)return ru ? "Дверь готова · подойди, посмотри на неё и нажми E"
                        : "Door ready · walk up, look at it and press E";
                    if(mainRoute)return ru ? "Заявка №17. Сними листы с двери, чтобы открыть спуск."
                        : "Job 17. Clear the service door to open the way downstairs.";
                    if(courtyardKnockHeard)return ru ? "Опять стучит за дверью. Сними листы с двери, стены можно оставить."
                        : "There it is again, behind the door. Clear the door. The walls can wait.";
                    return ru ? "Заявка №17. Что стучит под двором? Сними объявления с двери."
                        : "Job 17. What’s knocking under the courtyard? Peel the notices off the door.";
                }
                if(Area==1)
                {
                    if(basementDoorOpening!=null)return ru ? "Дверь в архив открывается" : "The archive door is opening";
                    return basementOpen ? (ru ? "Архив открыт · пройди через дальний проём" : "Archive open · go through the doorway ahead")
                        : (ru ? "Бирка на двери архива: «Проверить защёлку». Сними листы в подвале."
                            : "Archive door tag: “Check latch.” Clear the basement notices.");
                }
                return ru ? "Сними листы в архиве. На дальней стене остался ответ по заявке №17."
                    : "Clear the archive notices. The reply to job 17 is on the far wall.";
            }
        }
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
            courtyardOpen=true;
            RefreshCourtyardLever();
            Physics.SyncTransforms();
        }
        internal void RestoreProgress(bool firstDoorOpen)
        {
            courtyardUnlocked=Left(CourtyardGoal)==0;
            if(firstDoorOpen && courtyardUnlocked && !courtyardOpen)OpenCourtyardDoor();
            else RefreshCourtyardLever();
            SetBasementDoorOpen(Left(basement)==0,false);
            Completed=Left(hall)==0;
            finalReveal.SetActive(Completed);
            Physics.SyncTransforms();
        }
        void SetBasementDoorOpen(bool open,bool animate)
        {
            if(basementDoorOpening!=null)
            {
                StopCoroutine(basementDoorOpening);
                basementDoorOpening=null;
            }

            basementOpen=open;
            basementDoor.transform.localPosition=basementDoorClosedPosition;
            if(open && animate && basementDoor.activeInHierarchy)
                basementDoorOpening=StartCoroutine(LiftBasementDoor());
            else basementDoor.SetActive(!open);
        }
        IEnumerator LiftBasementDoor()
        {
            var door=basementDoor.transform;
            var travel=door.localRotation*Vector3.up*(Mathf.Abs(door.localScale.y)+.08f);
            for(float elapsed=0f;elapsed<BasementDoorOpenDuration;elapsed+=Time.deltaTime)
            {
                float lift=Mathf.SmoothStep(0f,1f,elapsed/BasementDoorOpenDuration);
                door.localPosition=basementDoorClosedPosition+travel*lift;
                yield return null;
            }
            basementDoor.SetActive(false);
            door.localPosition=basementDoorClosedPosition;
            basementDoorOpening=null;
            Physics.SyncTransforms();
        }
        internal bool IsSavedArchiveCleared(PeelWorldSave data)
        {
            foreach(var stack in hall)
            {
                var saved=Array.Find(data.stacks,item=>item.id==stack.PersistentId);
                if(saved==null || saved.cleared!=stack.PosterCount)return false;
            }
            return true;
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
            data.version=PeelWorldSave.AuthoredLayoutVersion;
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
            RefreshCourtyardLever();
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
            if(mainRoute)
            {
                CourtyardEntry=courtyardDoor.transform.position+Vector3.up*1.2f;
                var cart=FindAnyObjectByType<PeelWorkbench>();
                if(cart)CourtyardCart=cart.transform;
            }
            if(!game || courtyard==null || doorPosters==null || basement==null || hall==null || !courtyardDoor || !basementDoor || !finalReveal)
            {Debug.LogError("Route prototype: missing scene references",this);enabled=false;return;}
            // The trial uses stacks centered within the doorway, including the overlapping edge sheets.
            localCourtyard=doorPosters;
            optionalCourtyard=Array.FindAll(courtyard,stack=>stack && !IsCourtyardGoal(stack));
            InitializeCourtyardLever();
            basementDoorClosedPosition=basementDoor.transform.localPosition;
            knockCue=gameObject.AddComponent<PeelKnockCue>();
            knockCue.Initialize(game,this,courtyardDoor,basementDoor);
            if(!finalReveal.TryGetComponent(out archiveClue))
                archiveClue=finalReveal.AddComponent<PeelArchiveClue>();
            archiveClue.Initialize(game.Settings,archiveRevealLamp);
            CreatePassageNote();
            if(!mainRoute)
            {
                var basementProps=Resources.Load<GameObject>("BasementStorageCandidate");
                var archiveProps=Resources.Load<GameObject>("ArchiveStorageCandidates");
                if(basementProps)Instantiate(basementProps,basementDoor.transform.parent,false).name="Basement service storage";
                if(archiveProps)Instantiate(archiveProps,basementDoor.transform.parent,false).name="Archive storage";
            }

            game.PosterWasCleared+=OnPosterCleared;
            if(game.Settings)game.Settings.Changed+=RefreshRouteLanguage;
        }
        void InitializeCourtyardLever()
        {
            var handle=courtyardDoor.transform.Find("Door lever");
            if(!handle)return;
            courtyardLever=handle.GetComponentInChildren<Renderer>();
            if(!courtyardLever || !courtyardLever.sharedMaterial)return;

            courtyardLeverSteel=courtyardLever.sharedMaterial;
            courtyardLeverReady=new Material(courtyardLeverSteel) { name="Unlocked courtyard door lever" };
            courtyardLeverReady.SetColor("_BaseColor",new Color(.94f,.70f,.31f));
            courtyardLeverReady.SetFloat("_Metallic",.24f);
            courtyardLeverReady.EnableKeyword("_EMISSION");
            courtyardLeverReady.SetColor("_EmissionColor",new Color(.20f,.09f,.02f));
        }
        void RefreshCourtyardLever()
        {
            if(courtyardLever && courtyardLeverReady)
                courtyardLever.sharedMaterial=courtyardUnlocked && !courtyardOpen && !ReviewMode ? courtyardLeverReady : courtyardLeverSteel;
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
            note.transform.localScale=new Vector3(.72f,.23f,1f);
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
                ? "АРХИВ · №17\nПроверить защёлку" : "ARCHIVE · 17\nCheck latch";
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
            if(courtyardLeverReady)Destroy(courtyardLeverReady);
        }
        void OnPosterCleared(PeelPosterStack stack)
        {
            if(!courtyardUnlocked && Left(CourtyardGoal)==0)
            {
                courtyardUnlocked=true;
                RefreshCourtyardLever();
            }
            if(!basementOpen && Left(basement)==0)SetBasementDoorOpen(true,true);
            if(!Completed && Left(hall)==0)
            {
                Completed=true;
                finalReveal.SetActive(true);
                if(!ReviewMode)game.PlayArchiveReveal();
            }
        }
    }
}
