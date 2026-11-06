using UnityEngine;

namespace JustOneMorePeel
{
    // Navigation blockout; sheet counts in areas 2 and 3 are test values.
    [DefaultExecutionOrder(50)]
    public sealed class PeelRoutePrototype : MonoBehaviour
    {
        [SerializeField] PeelGame game;
        [SerializeField] PeelPosterStack[] courtyard;
        [SerializeField] PeelPosterStack[] basement;
        [SerializeField] PeelPosterStack[] hall;
        [SerializeField] GameObject courtyardDoor;
        [SerializeField] GameObject basementDoor;
        [SerializeField] GameObject finalReveal;
        bool courtyardUnlocked, courtyardOpen, basementOpen, courtyardKnockHeard;
        public bool Completed { get; private set; }
        public bool ReviewMode { get; private set; }
        public bool CourtyardDoorOpen => courtyardOpen;
        public bool BasementDoorOpen => basementOpen;
        int Area => game.player.transform.position.z < 3.35f ? 0 : game.player.transform.position.z < 14f ? 1 : 2;
        public int CurrentArea => Area;
        static int Left(PeelPosterStack[] stacks)
        { int count=0; foreach(var s in stacks) if(s)count+=s.RemainingPosters; return count; }
        public int RemainingHere => Area==0 ? Left(courtyard) : Area==1 ? Left(basement) : Left(hall);
        public string Objective
        {
            get
            {
                bool ru=game.Settings.Language==PeelLanguage.Russian;
                if(ReviewMode)return ru ? "Осмотр маршрута: проходы открыты" : "Route review: passages are open";
                if(Completed)return ru ? "Конец прототипа маршрута" : "End of the route prototype";
                if(Area==0)
                {
                    if(courtyardOpen)return ru ? "Дверь открыта: пройди внутрь" : "The door is open: go inside";
                    if(courtyardUnlocked)return ru ? "Подойди к двери и открой её" : "Approach and open the door";
                    if(courtyardKnockHeard)return ru ? "Стук за стеной... Что скрывают объявления?" : "Knocking behind the wall... What are the notices hiding?";
                    return ru ? "Очисти стену и узнай, что за ней" : "Clear the wall and find out what is behind it";
                }
                if(Area==1)return basementOpen ? (ru ? "Проход открыт: найди архив" : "Passage open: find the archive") : (ru ? "Очисти объявления у следующей двери" : "Clear the notices at the next door");
                return ru ? "Очисти стену: что скрывают объявления?" : "Clear the wall: what are the notices hiding?";
            }
        }
        public string FinishText(string time) => game.Settings.Language==PeelLanguage.Russian ? "Прототип маршрута завершён.\nВремя: "+time : "Route prototype complete.\nTime: "+time;
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
            courtyardUnlocked=Left(courtyard)==0;
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
            foreach(var stack in courtyard)
            {
                bool complete=false;
                foreach(var item in data.stacks)
                    if(item.id==stack.PersistentId){complete=item.cleared==stack.PosterCount;break;}
                if(!complete)return false;
            }
            return true;
        }
#if UNITY_EDITOR
        public void ClearCurrentAreaForTesting()
        {
            var selected=Area==0 ? courtyard : Area==1 ? basement : hall;
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
        public void ReviewLightingChanged(bool lightOn)
        {
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
            gameObject.AddComponent<PeelKnockCue>().Initialize(game,this,courtyardDoor,basementDoor);
            finalReveal.AddComponent<PeelArchiveClue>().Initialize(game.Settings);
            var basementProps=Resources.Load<GameObject>("BasementStorageCandidate");
            var archiveProps=Resources.Load<GameObject>("ArchiveStorageCandidates");
            if(basementProps)Instantiate(basementProps,basementDoor.transform.parent,false).name="Basement service storage candidate";
            if(archiveProps)Instantiate(archiveProps,basementDoor.transform.parent,false);

            game.PosterWasCleared+=OnPosterCleared;
            if(game.Settings)game.Settings.Changed+=DisableBasementCollisions;
        }
        void OnDestroy() { if(game){game.PosterWasCleared-=OnPosterCleared;if(game.Settings)game.Settings.Changed-=DisableBasementCollisions;} }
        void OnPosterCleared(PeelPosterStack stack)
        {
            if(!courtyardUnlocked && Left(courtyard)==0)courtyardUnlocked=true;
            if(!basementOpen && Left(basement)==0){basementOpen=true;basementDoor.SetActive(false);Physics.SyncTransforms();}
            if(!Completed && Left(hall)==0){Completed=true;finalReveal.SetActive(true);}
        }
    }
}
