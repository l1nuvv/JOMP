using System.Collections.Generic;
using System.IO;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace JustOneMorePeel
{
    public enum PeelScreen { Intro, Playing, Paused, Shop, Settings, ConfirmRestart }

    [RequireComponent(typeof(PeelSettings))]
    public class PeelGame : MonoBehaviour
    {
        public Camera view;
        public GameObject player;
        public Material[] papers;
        public Material brick;
        [SerializeField] private GameObject starterViewPrefab;
        [SerializeField] private GameObject scraperViewPrefab;
        [SerializeField] private GameObject steamerViewPrefab;
        public int money, removed, total, toolLevel;
        public bool steamUnlocked;
        public bool steamExpanded;
        public float reach=3.4f;
        public const int SteamCost=480;
        public const int SteamExpansionCost=800;
        public PeelSettings Settings { get; private set; }
        public PeelScrapPool Scraps { get; private set; }
        public PeelPosterOutline Outline { get; private set; }
        public PeelScreen CurrentScreen { get; private set; }=PeelScreen.Intro;
        public bool AwaitingRelease { get; private set; }
        public bool IsPeeling => workingStrips.Count>0;
        internal System.Collections.Generic.IReadOnlyList<PeelPosterStack> WorkingStacks => workingStacks;
        public float GripProgress => workingStrips.Count>0 && workingStrips[0] ? workingStrips[0].progress : 0f;
        public bool InDarkness => basementLight && basementLight.IsDark;
        public int TotalPosters { get; private set; }
        public int ClearedPosters { get; private set; }
        public event System.Action<PeelPosterStack> PosterWasCleared;
        public float ElapsedPlayTime { get; private set; }
        public float Progress => total==0?0:(float)removed/total;
        public int UpgradeCost => toolLevel==0?25:toolLevel==1?160:640;
        public int activeTool;
#if UNITY_EDITOR
        bool testFastPeeling;
#endif

        readonly List<PeelPosterStack> stacks=new List<PeelPosterStack>(19);
        readonly List<PeelStrip> workingStrips=new List<PeelStrip>(12);
        readonly List<PeelPosterStack> workingStacks=new List<PeelPosterStack>(3);
        readonly List<PeelPosterStack> wholePosterVisuals=new List<PeelPosterStack>(3);
        PeelStrip target, soundStrip;
        PeelScreen settingsReturn=PeelScreen.Intro;
        FirstPersonController controller;
        StarterAssetsInputs inputs;
        float originalRotationSpeed=1, noticeUntil;
        bool lookFrozen, appliedSettings, focusMenu=true;
        PeelLanguage appliedLanguage;
        int menuSelection;
        int payoutAmount, payoutFrame=-1;
        float payoutUntil;
        AudioSource audioSource, musicSource, basementMusicSource;
        float basementMusicMix;
        PeelWallVisibility wallVisibility;
        PeelAreaExperiment areaExperiment;
        PeelRoutePrototype routePrototype;
        PeelBasementLight basementLight;
        bool worldSession, saveBlocked, saveWriteFailed, saveSucceeded, restarting;
        float nextWorldSave, recoveryNoticeUntil, completionNoticeUntil;
        bool completionSeen;
#if UNITY_EDITOR
        public static string WorldSavePathOverride;
#endif
        string WorldSavePath
        {
            get
            {
#if UNITY_EDITOR
                if(!string.IsNullOrEmpty(WorldSavePathOverride))return WorldSavePathOverride;
#endif
                return PeelWorldSave.DefaultPath;
            }
        }
        readonly List<PeelPosterStack> nearbyStacks=new List<PeelPosterStack>();
        readonly List<PeelPosterStack> previewStacks=new List<PeelPosterStack>(5);
        AudioClip rip, detachClip;
        AudioSource detachSource;
        float soundEnvelope;
        bool soundFadingOut;
        const float SoundFadeIn=.04f, SoundFadeOut=.12f;
        PeelText notice=PeelText.Welcome;
        [System.NonSerialized] GUIStyle title, text, small, center, button, shopBalance;

        void Start()
        {
            payoutAmount=0;payoutFrame=-1;payoutUntil=0;
            title=text=small=center=button=null;
            if(!view)view=Camera.main;
            if(!view || !player){Debug.LogError("PeelGame: missing player or camera",this);enabled=false;return;}
            Application.runInBackground=true;
            controller=player.GetComponentInChildren<FirstPersonController>();
            inputs=player.GetComponentInChildren<StarterAssetsInputs>();
            if(controller)originalRotationSpeed=controller.RotationSpeed;
            Settings=GetComponent<PeelSettings>();Settings.Load();
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;
            rip=Resources.Load<AudioClip>("PaperRustleCandidate");
            if(!rip)Debug.LogWarning("PeelGame: paper sound candidate missing",this);
            audioSource.clip=rip;audioSource.loop=true;
            detachClip=Resources.Load<AudioClip>("PaperDetach");
            detachSource=gameObject.AddComponent<AudioSource>();
            detachSource.playOnAwake=false;
            var paperFilter=gameObject.AddComponent<AudioLowPassFilter>();paperFilter.cutoffFrequency=7000f;
            var musicObject=new GameObject("Background music");
            musicObject.transform.SetParent(transform,false);
            musicSource=musicObject.AddComponent<AudioSource>();
            musicSource.spatialBlend=0;musicSource.loop=true;musicSource.playOnAwake=false;
            musicSource.clip=Resources.Load<AudioClip>("AceMusicCandidate");
            if(musicSource.clip)musicSource.Play();
            else Debug.LogWarning("PeelGame: background music candidate missing",this);
            basementMusicSource=musicObject.AddComponent<AudioSource>();
            basementMusicSource.spatialBlend=0;basementMusicSource.loop=true;basementMusicSource.playOnAwake=false;
            basementMusicSource.clip=Resources.Load<AudioClip>("BasementMusicCandidate");
            if(!basementMusicSource.clip)Debug.LogWarning("PeelGame: basement music candidate missing",this);
            Scraps=new GameObject("Bounded paper scrap pool").AddComponent<PeelScrapPool>();
            Scraps.Initialize(view,player);
            Outline=new GameObject("Target poster outline").AddComponent<PeelPosterOutline>();
            foreach(var stack in FindObjectsByType<PeelPosterStack>())
            {stacks.Add(stack);stack.Initialize(this);TotalPosters+=stack.PosterCount;}
            wallVisibility=gameObject.AddComponent<PeelWallVisibility>();wallVisibility.Initialize(view,stacks);
            areaExperiment=GetComponent<PeelAreaExperiment>();
            routePrototype=GetComponent<PeelRoutePrototype>();
            basementLight=GetComponent<PeelBasementLight>();
            total=TotalPosters*4;removed=0;ClearedPosters=0;
            Settings.Changed+=ApplySettings;ApplySettings();
            new GameObject("First person tool").AddComponent<PeelToolView>().Initialize(this,starterViewPrefab,scraperViewPrefab,steamerViewPrefab);
            CurrentScreen=PeelScreen.Intro;AwaitingRelease=true;noticeUntil=Time.time+5;RefreshCursor();
        }
        void ApplySettings()
        {
            if(audioSource)audioSource.volume=Settings.SoundVolume*.55f*soundEnvelope;
            UpdateMusicVolumes();
            if(controller)controller.RotationSpeed=originalRotationSpeed*Settings.LookSensitivity;
            if(!appliedSettings || appliedLanguage!=Settings.Language)
            {
                foreach(var stack in stacks)stack.RefreshMaterials();
                Settings.ApplyWorldText();appliedLanguage=Settings.Language;appliedSettings=true;
            }
        }
        void Update()
        {
            UpdatePeelSound(Time.unscaledDeltaTime);
            UpdateAreaMusic(Time.unscaledDeltaTime);
            if(routePrototype && routePrototype.Completed && !completionSeen)
            {completionSeen=true;completionNoticeUntil=Time.unscaledTime+6f;}
            if(CurrentScreen==PeelScreen.Playing && (!routePrototype || !routePrototype.Completed))ElapsedPlayTime+=Time.unscaledDeltaTime;
            if(worldSession && CurrentScreen==PeelScreen.Playing && Time.unscaledTime>=nextWorldSave)
            { SaveWorld();nextWorldSave=Time.unscaledTime+5f; }
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
#if UNITY_EDITOR
                if(CurrentScreen==PeelScreen.Playing || CurrentScreen==PeelScreen.Shop)
                {
                    if(keyboard.f6Key.wasPressedThisFrame)money+=1000;
                    if(keyboard.f7Key.wasPressedThisFrame)testFastPeeling=!testFastPeeling;
                    if(CurrentScreen==PeelScreen.Playing && keyboard.f8Key.wasPressedThisFrame && routePrototype)
                    {Release();routePrototype.ClearCurrentAreaForTesting();}
                }
#endif
                if(keyboard.escapeKey.wasPressedThisFrame)
                {
                    if(CurrentScreen==PeelScreen.Playing)SetScreen(PeelScreen.Paused);
                    else Back();
                    return;
                }
                if(CurrentScreen!=PeelScreen.Playing)
                {
                    if(CurrentScreen==PeelScreen.Shop && keyboard.eKey.wasPressedThisFrame){Back();return;}
                    NavigateMenu(keyboard);return;
                }
                if(keyboard.eKey.wasPressedThisFrame && routePrototype && routePrototype.TryOpenDoor())return;
                if(keyboard.eKey.wasPressedThisFrame && basementLight && basementLight.TryToggle())return;
                if(keyboard.eKey.wasPressedThisFrame && CanUseWorkbench()){SetScreen(PeelScreen.Shop);return;}
                if(keyboard.digit1Key.wasPressedThisFrame){Release();activeTool=0;}
                if(keyboard.digit2Key.wasPressedThisFrame && steamUnlocked){Release();activeTool=1;}
            }
            if(CurrentScreen!=PeelScreen.Playing)return;
            var mouse=Mouse.current;
            if(mouse!=null)StepPeeling(mouse.leftButton.isPressed,mouse.delta.ReadValue(),Time.deltaTime);
        }
        void UpdateAreaMusic(float dt)
        {
            bool inBasement=routePrototype && routePrototype.CurrentArea>0 && basementMusicSource && basementMusicSource.clip;
            if(inBasement && !basementMusicSource.isPlaying)basementMusicSource.Play();
            basementMusicMix=Mathf.MoveTowards(basementMusicMix,inBasement?1f:0f,dt/3f);
            UpdateMusicVolumes();
        }
        void UpdateMusicVolumes()
        {
            if(!Settings)return;
            if(musicSource)musicSource.volume=Settings.SoundVolume*.2f*(1f-basementMusicMix);
            if(basementMusicSource)basementMusicSource.volume=Settings.SoundVolume*.2f*basementMusicMix;
        }
        public void BeginShift()
        {
            if(CurrentScreen!=PeelScreen.Intro)return;
            if(routePrototype && !routePrototype.ReviewMode)
            {
                worldSession=true;LoadWorld();nextWorldSave=Time.unscaledTime+5f;
            }
            SetScreen(PeelScreen.Playing);
        }
        public void OpenSettings()
        {
            if(CurrentScreen!=PeelScreen.Intro && CurrentScreen!=PeelScreen.Paused)return;
            settingsReturn=CurrentScreen;SetScreen(PeelScreen.Settings);
        }
        public void Back()
        {
            if(CurrentScreen==PeelScreen.Settings){Settings.Save();SetScreen(settingsReturn);}
            else if(CurrentScreen==PeelScreen.ConfirmRestart)SetScreen(PeelScreen.Paused);
            else if(CurrentScreen==PeelScreen.Paused || CurrentScreen==PeelScreen.Shop)SetScreen(PeelScreen.Playing);
        }
        public void RequestRestart()
        {
            if(CurrentScreen==PeelScreen.Paused)SetScreen(PeelScreen.ConfirmRestart);
        }
        public void RestartShift()
        {
            Settings.Save();
            if(routePrototype && !routePrototype.ReviewMode)
            {
                try
                {
                    if(File.Exists(WorldSavePath+".bak"))File.Delete(WorldSavePath+".bak");
                    if(File.Exists(WorldSavePath))File.Delete(WorldSavePath);
                }
                catch(System.Exception e){saveWriteFailed=true;Debug.LogError("Could not reset world save: "+e.Message,this);return;}
            }
            restarting=true;worldSession=false;
            var scene=SceneManager.GetActiveScene();
#if UNITY_EDITOR
            if(SceneUtility.GetBuildIndexByScenePath(scene.path)<0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path,new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(scene.name);
        }
        void SetScreen(PeelScreen screen)
        {
            Release();
            if(worldSession && CurrentScreen==PeelScreen.Playing && screen!=PeelScreen.Playing)SaveWorld();
            CurrentScreen=screen;if(Outline && screen!=PeelScreen.Playing)Outline.SetTarget(null);menuSelection=0;focusMenu=true;RefreshCursor();
        }
        bool SaveIndex(out Dictionary<string,PeelPosterStack> index)
        {
            if(PeelPersistenceIds.TryIndex(stacks,out index,out var error))return true;
            Debug.LogError("World save disabled: "+error,this);saveBlocked=true;return false;
        }
        internal void SaveWorld()
        {
            if(!worldSession || saveBlocked || restarting || !routePrototype || routePrototype.ReviewMode || !SaveIndex(out var index))return;
            saveSucceeded=false;
            var data=new PeelWorldSave {version=1,scene=SceneManager.GetActiveScene().path,money=money,toolLevel=toolLevel,activeTool=activeTool,
                steamUnlocked=steamUnlocked,steamExpanded=steamExpanded,elapsed=ElapsedPlayTime,position=player.transform.position,
                rotation=player.transform.rotation,firstDoorOpen=routePrototype.CourtyardDoorOpen,lightOn=basementLight && basementLight.IsOn,
                stacks=new PeelStackSave[index.Count]};
            if(controller && controller.CinemachineCameraTarget)
            {
                data.hasLookPitch=true;
                data.lookPitch=Mathf.DeltaAngle(0,controller.CinemachineCameraTarget.transform.localEulerAngles.x);
            }
            int n=0;
            foreach(var pair in index)
            {
                var stack=pair.Value;var item=new PeelStackSave {id=pair.Key,cleared=stack.ClearedPosters,strips=new float[4]};
                if(stack.RemainingPosters>0)
                {
                    int done=0;
                    for(int i=0;i<4;i++){item.strips[i]=stack.CurrentStrips[i].progress;if(item.strips[i]>=1)done++;}
                    if(done==4){item.cleared++;item.strips=new float[4];}
                }
                data.stacks[n++]=item;
            }
            if(!PeelWorldSave.Validate(data,SceneManager.GetActiveScene().path,index,out var error))
            {Debug.LogError("World save rejected: "+error,this);saveBlocked=true;return;}
            try { PeelWorldSave.Write(WorldSavePath,data);saveWriteFailed=false;saveSucceeded=true; }
            catch(System.Exception e){saveWriteFailed=true;Debug.LogError("Could not write world save: "+e.Message,this);}
        }
        string PauseSaveStatus()
        {
            bool ru=Settings.Language==PeelLanguage.Russian;
            if(routePrototype && routePrototype.ReviewMode)return ru ? "Осмотр не сохраняется" : "Route review is not saved";
            if(!worldSession)return "";
            if(saveBlocked || saveWriteFailed)return ru ? "Не удалось сохранить прогресс" : "Progress could not be saved";
            return saveSucceeded ? (ru ? "Прогресс сохранён" : "Progress saved") : "";
        }
        void LoadWorld()
        {
            if((!File.Exists(WorldSavePath) && !File.Exists(WorldSavePath+".bak")) || !SaveIndex(out var index))return;
            try
            {
                PeelWorldSave data;
                try { data=ReadValidatedWorld(WorldSavePath,index); }
                catch(System.NotSupportedException) { throw; }
                catch(System.Exception)
                {
                    data=ReadValidatedWorld(WorldSavePath+".bak",index);
                    PeelWorldSave.Recover(WorldSavePath);
                    recoveryNoticeUntil=Time.unscaledTime+10f;
                }
                int savedRemoved=0,savedCleared=0;
                foreach(var item in data.stacks)
                {
                    var stack=index[item.id];stack.RestoreProgress(item.cleared,item.strips);
                    if(basementLight)basementLight.RefreshAfterPosterAdvance(stack);
                    savedCleared+=item.cleared;savedRemoved+=item.cleared*4;
                    foreach(float p in item.strips)if(p>=1)savedRemoved++;
                }
                money=data.money;toolLevel=data.toolLevel;activeTool=data.activeTool;steamUnlocked=data.steamUnlocked;steamExpanded=data.steamExpanded;
                removed=savedRemoved;ClearedPosters=savedCleared;ElapsedPlayTime=data.elapsed;
                player.transform.SetPositionAndRotation(data.position,data.rotation);
                if(data.hasLookPitch && controller)controller.RestoreLookPitch(data.lookPitch);
                routePrototype.RestoreProgress(data.firstDoorOpen);
                if(basementLight)basementLight.RestoreProgress(data.lightOn);
                Physics.SyncTransforms();
            }
            catch(System.Exception e)
            {
                saveBlocked=true;Debug.LogError("World save could not be loaded; the file was preserved: "+e.Message,this);
            }
        }
        PeelWorldSave ReadValidatedWorld(string path,Dictionary<string,PeelPosterStack> index)
        {
            var data=PeelWorldSave.Read(path);
            if(data!=null && data.version!=0 && data.version!=1)throw new System.NotSupportedException("Unsupported save version: "+data.version);
            if(!PeelWorldSave.Validate(data,SceneManager.GetActiveScene().path,index,out var error))
                throw new System.IO.InvalidDataException(error);
            if(!routePrototype.CanRestoreProgress(data))throw new System.IO.InvalidDataException("Open door with uncleared wall");
            return data;
        }
        void NavigateMenu(Keyboard keyboard)
        {
            int count=CurrentScreen==PeelScreen.Intro?(routePrototype?3:2):CurrentScreen==PeelScreen.ConfirmRestart?2:CurrentScreen==PeelScreen.Settings || CurrentScreen==PeelScreen.Shop?4:3;
            if(keyboard.tabKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {menuSelection=(menuSelection+1)%count;focusMenu=true;}
            if(keyboard.upArrowKey.wasPressedThisFrame){menuSelection=(menuSelection+count-1)%count;focusMenu=true;}
            if(CurrentScreen==PeelScreen.Settings)
            {
                int direction=keyboard.rightArrowKey.wasPressedThisFrame?1:keyboard.leftArrowKey.wasPressedThisFrame?-1:0;
                if(direction!=0)
                {
                    if(menuSelection==0)Settings.SetLanguage(direction>0?PeelLanguage.Russian:PeelLanguage.English);
                    if(menuSelection==1)Settings.SetSoundVolume(Settings.SoundVolume+direction*.1f);
                    if(menuSelection==2)Settings.SetLookSensitivity(Settings.LookSensitivity+direction*.1f);
                }
            }
            if(!keyboard.enterKey.wasPressedThisFrame && !keyboard.numpadEnterKey.wasPressedThisFrame)return;
            if(CurrentScreen==PeelScreen.Intro){if(menuSelection==0)BeginShift();else if(menuSelection==1)OpenSettings();else if(routePrototype){routePrototype.OpenForReview();BeginShift();}}
            else if(CurrentScreen==PeelScreen.Paused){if(menuSelection==0)Back();else if(menuSelection==1)OpenSettings();else RequestRestart();}
            else if(CurrentScreen==PeelScreen.ConfirmRestart){if(menuSelection==0)Back();else RestartShift();}
            else if(CurrentScreen==PeelScreen.Settings)
            {if(menuSelection==0)Settings.SetLanguage(Settings.Language==PeelLanguage.English?PeelLanguage.Russian:PeelLanguage.English);else if(menuSelection==3)Back();}
            else if(CurrentScreen==PeelScreen.Shop){if(menuSelection==0)TryUpgrade();else if(menuSelection==1)TryBuySteam();else if(menuSelection==2)TryExpandSteam();else Back();}
        }

        public bool CanUseWorkbench()
        {
            return view && player && Physics.Raycast(view.transform.position,view.transform.forward,out var hit,2.5f)
                && hit.collider.GetComponentInParent<PeelWorkbench>()!=null;
        }
        public void StepPeeling(bool pressed,Vector2 delta,float seconds)
        {
            target=null;
            if(Physics.Raycast(view.transform.position,view.transform.forward,out var hit,reach))target=hit.collider.GetComponent<PeelStrip>();
            previewStacks.Clear();
            if(workingStrips.Count>0)previewStacks.AddRange(workingStacks);
            else if(target && target.IsExposed)
            {
                if(toolLevel>0 && activeTool==1 && steamUnlocked)SelectSteamTargets(target.Stack,previewStacks);
                else previewStacks.Add(target.Stack);
            }
            if(Outline)Outline.SetTargets(previewStacks);
            if(!pressed){Release();AwaitingRelease=false;return;}
            if(AwaitingRelease)return;
            if(workingStrips.Count>0 && Vector3.Distance(view.transform.position,workingStrips[0].transform.position)>reach+1){Release();AwaitingRelease=false;return;}
            if(workingStrips.Count==0 && target && target.IsExposed)
            {
                if(toolLevel==0)workingStrips.Add(target);
                else
                {
                    if(activeTool==1 && steamUnlocked)
                    {
                        foreach(var stack in previewStacks)AddPoster(stack);
                    }
                    else AddPoster(target.Stack);
                }
                if(target.Stack && !workingStacks.Contains(target.Stack))workingStacks.Add(target.Stack);
                foreach(var strip in workingStrips)strip.Begin();
                soundStrip=target;
                if(audioSource && rip)
                {
                    audioSource.volume=0;audioSource.Stop();soundEnvelope=0;soundFadingOut=false;
                    audioSource.pitch=toolLevel==0?1.12f:(activeTool==1 && steamUnlocked?.88f:1f);
                    audioSource.timeSamples=Mathf.Clamp(soundStrip.SoundSample,0,rip.samples-1);
                    audioSource.Play();
                }
                lookFrozen=true;RefreshCursor();
            }
            if(workingStrips.Count==0)return;
            float amount=PeelAmount(delta,seconds,toolLevel,activeTool==1 && steamUnlocked);
#if UNITY_EDITOR
            if(testFastPeeling)amount*=10f;
#endif
            int removedBefore=removed;
            bool finished=true;
            foreach(var strip in workingStrips)
            {if(!strip.IsRemoved)strip.Pull(amount);if(!strip.IsRemoved)finished=false;}
            int detached=removed-removedBefore;
            if(detached>0 && detachClip && Settings.SoundVolume>0f)
            {
                // One accent for this pull, even if several strips pay together.
                detachSource.pitch=audioSource.pitch;
                float strength=Mathf.Lerp(.2f,.42f,Mathf.Clamp01((detached-1)/7f));
                detachSource.PlayOneShot(detachClip,Settings.SoundVolume*strength);
            }
            if(finished)
            {
                foreach(var strip in workingStrips)strip.End();
                workingStrips.Clear();StopPeelSound();AdvanceStacks();
                // The next frame raycasts again, so a held button can continue only on a reachable visible sheet.
                lookFrozen=false;RefreshCursor();
            }
        }
        void AddPoster(PeelPosterStack stack)
        {
            if(!stack)return;
            workingStacks.Add(stack);
            int intact=0;foreach(var strip in stack.CurrentStrips)if(strip.IsExposed)intact++;
            if(intact==4)wholePosterVisuals.Add(stack);
            foreach(var strip in stack.CurrentStrips)if(strip.IsExposed)workingStrips.Add(strip);
        }
        public bool NeedsStripScrap(PeelStrip strip) { return !wholePosterVisuals.Contains(strip.Stack); }
        void SelectSteamTargets(PeelPosterStack origin,List<PeelPosterStack> result)
        {
            result.Add(origin);
            float radius=steamExpanded?2.5f:1.5f;
            wallVisibility.FindNearby(origin.transform.position,radius,nearbyStacks);
            for(int i=0;i<(steamExpanded?4:2);i++)
            {
                PeelPosterStack closest=null;float distance=radius*radius;
                foreach(var candidate in nearbyStacks)
                {
                    if(candidate.RemainingPosters==0 || result.Contains(candidate))continue;
                    float d=(candidate.transform.position-origin.transform.position).sqrMagnitude;
                    if(d>=distance || !VisiblePoster(candidate))continue;
                    closest=candidate;distance=d;
                }
                if(closest)result.Add(closest);
            }
        }
        bool VisiblePoster(PeelPosterStack stack)
        {
            Vector3 aim=stack.transform.position+Vector3.back*.001f;
            Vector3 direction=aim-view.transform.position;
            return direction.magnitude<=reach && Vector3.Dot(view.transform.forward,direction.normalized)>.65f
                && Physics.Raycast(view.transform.position,direction.normalized,out var hit,direction.magnitude+.08f)
                && hit.collider.TryGetComponent<PeelStrip>(out var strip) && strip.Stack==stack && strip.IsExposed;
        }
        public static float PeelAmount(Vector2 delta,float seconds,int level,bool steam)
        {
            float dt=Mathf.Clamp(seconds,0,.1f);
            float rate=level==0?.4f:.35f+Mathf.Clamp(level,1,3)*.1f;
            float horizontalSpeed=seconds>0?Mathf.Abs(delta.x)/seconds:0;
            float drag=level>0?Mathf.Clamp01(horizontalSpeed/1200f)*.8f:0;
            return dt*rate*(steam && level>0?2.4f:1f)*(1+drag);
        }
        void AdvanceStacks()
        {
            bool changed=false;
            foreach(var stack in workingStacks)if(stack.NeedsAdvance)
            {if(wholePosterVisuals.Contains(stack))Scraps.EmitPoster(stack);stack.AdvanceIfComplete();changed=true;}
            workingStacks.Clear();wholePosterVisuals.Clear();if(changed)Physics.SyncTransforms();
        }
        void UpdatePeelSound(float seconds)
        {
            if(!audioSource || !Settings)return;
            float targetVolume=soundFadingOut?0f:(soundStrip?1f:0f);
            float fade=targetVolume>soundEnvelope?SoundFadeIn:SoundFadeOut;
            soundEnvelope=Mathf.MoveTowards(soundEnvelope,targetVolume,Mathf.Max(0,seconds)/fade);
            audioSource.volume=Settings.SoundVolume*.55f*soundEnvelope;
            if(soundFadingOut && soundEnvelope<=0f){audioSource.Stop();soundFadingOut=false;}
        }
        void StopPeelSound()
        {
            if(audioSource && soundStrip)
            {
                if(audioSource.isPlaying) soundStrip.SoundSample=audioSource.timeSamples;
                soundFadingOut=audioSource.isPlaying;
            }
            soundStrip=null;
        }
        void Release()
        {
            foreach(var strip in workingStrips)if(strip)strip.End();
            workingStrips.Clear();StopPeelSound();AdvanceStacks();AwaitingRelease=true;lookFrozen=false;RefreshCursor();
        }
        public bool TryUpgrade()
        {
            if(toolLevel>=3 || money<UpgradeCost)return false;
            Release();money-=UpgradeCost;toolLevel++;
            Notify(toolLevel==1?PeelText.Upgraded:PeelText.ScraperSharpened);return true;
        }
        public bool TryBuySteam()
        {
            if(toolLevel==0 || steamUnlocked || money<SteamCost)return false;
            Release();money-=SteamCost;steamUnlocked=true;activeTool=1;Notify(PeelText.SteamBought);return true;
        }
        public bool TryExpandSteam()
        {
            if(!steamUnlocked || steamExpanded || money<SteamExpansionCost)return false;
            Release();money-=SteamExpansionCost;steamExpanded=true;Notify(PeelText.SteamExpanded);return true;
        }
        public void Pay(PeelStrip strip)
        {
            removed++;money+=2;
            if(payoutFrame!=Time.frameCount)payoutAmount=0;
            payoutAmount+=2;payoutFrame=Time.frameCount;payoutUntil=Time.unscaledTime+1.25f;
            if(removed==total)Notify(PeelText.Finished);
            else if(removed%40==0)Notify(PeelText.Milestone);
        }
        public void PosterCleared(PeelPosterStack stack) { ClearedPosters++;PosterWasCleared?.Invoke(stack); }
        public void PosterMaterialsChanged(PeelPosterStack stack)
        { if(basementLight)basementLight.RefreshAfterPosterAdvance(stack); }
        void Notify(PeelText key) { notice=key;noticeUntil=Time.time+4; }
        void RefreshCursor()
        {
            bool playing=CurrentScreen==PeelScreen.Playing;
            if(inputs)
            {
                inputs.cursorLocked=playing;inputs.cursorInputForLook=playing && !lookFrozen;
                if(!playing || lookFrozen)inputs.look=Vector2.zero;
            }
            Cursor.lockState=playing?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!playing;
            if(controller)controller.enabled=playing && !lookFrozen;
        }
        void LateUpdate()
        {
            if(CurrentScreen!=PeelScreen.Playing && (Cursor.lockState!=CursorLockMode.None || !Cursor.visible))RefreshCursor();
        }
        void OnApplicationFocus(bool focused)
        {
            if(!Settings)return;
            if(!focused){Release();if(worldSession)SaveWorld();if(Outline)Outline.SetTarget(null);}else RefreshCursor();
        }
        void OnApplicationPause(bool paused) { if(paused && worldSession){Release();SaveWorld();} }
        void OnApplicationQuit() { if(worldSession){Release();SaveWorld();} }
        void OnDisable() {lookFrozen=false;CurrentScreen=PeelScreen.Intro;RefreshCursor();}
        void OnDestroy() {if(Settings)Settings.Changed-=ApplySettings;}

        string T(PeelText key) => Settings.Text(key);
        void Styles()
        {
            if(title!=null && title.fontSize==32 && text!=null && text.fontSize==19)return;
            title=new GUIStyle(GUI.skin.label){fontSize=32,fontStyle=FontStyle.Bold};title.normal.textColor=new Color(.97f,.87f,.63f);
            text=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};text.normal.textColor=Color.white;
            small=new GUIStyle(text){fontSize=15};small.normal.textColor=new Color(.78f,.85f,.83f);
            center=new GUIStyle(text){alignment=TextAnchor.MiddleCenter};
            shopBalance=new GUIStyle(text){alignment=TextAnchor.MiddleRight,fontStyle=FontStyle.Bold};
            shopBalance.normal.textColor=new Color(.97f,.87f,.63f);
            button=new GUIStyle(GUI.skin.button){fontSize=18,wordWrap=true};
            button.normal.textColor=button.hover.textColor=button.active.textColor=button.focused.textColor=Color.white;
        }
        void Panel(Rect rect)
        {var old=GUI.color;GUI.color=new Color(.045f,.075f,.08f,.96f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        bool MenuButton(Rect rect,string label,int index)
        {
            GUI.SetNextControlName("peel-menu-"+index);
            var old=GUI.backgroundColor;GUI.backgroundColor=menuSelection==index?new Color(.77f,.87f,.73f):Color.white;
            bool clicked=GUI.Button(rect,label,button);GUI.backgroundColor=old;
            if(clicked)menuSelection=index;
            return clicked;
        }
        void FocusMenu()
        {if(focusMenu && Event.current.type==EventType.Repaint){GUI.FocusControl("peel-menu-"+menuSelection);focusMenu=false;}}
        string SharpeningPreview()
        {
            const float step=.1f;
            float current=step/PeelAmount(Vector2.zero,step,toolLevel,false);
            if(toolLevel>=3)
                return string.Format(T(PeelText.ShopSharpenMaxTime),current);

            float next=step/PeelAmount(Vector2.zero,step,toolLevel+1,false);
            return string.Format(T(PeelText.ShopSharpenEffect),current,next);
        }
        bool ShopOffer(float y,int index,string label,string effect,bool owned,int cost,string requirement=null)
        {
            var oldColor=GUI.color;
            GUI.color=menuSelection==index?new Color(.20f,.27f,.24f,.98f):new Color(.10f,.15f,.15f,.98f);
            GUI.DrawTexture(new Rect(48,y-5,480,90),Texture2D.whiteTexture);
            if(menuSelection==index)
            {
                GUI.color=new Color(.97f,.78f,.42f);
                GUI.DrawTexture(new Rect(48,y-5,4,90),Texture2D.whiteTexture);
            }
            GUI.color=oldColor;
            var wasEnabled=GUI.enabled;
            GUI.enabled=wasEnabled && !owned && requirement==null && money>=cost;
            bool clicked=MenuButton(new Rect(58,y,460,37),label,index);
            GUI.enabled=wasEnabled;
            GUI.Label(new Rect(66,y+39,444,22),effect,small);
            string status=requirement;
            if(status==null && !owned && money<cost)status=string.Format(T(PeelText.ShopCashShortfall),cost-money);
            if(status!=null)GUI.Label(new Rect(66,y+60,444,22),status,small);
            return clicked;
        }
        void OnGUI()
        {
            if(!Settings)return;
            Styles();GUI.color=Color.white;GUI.contentColor=Color.white;
            var guiEvent=Event.current;
            if(guiEvent.type==EventType.KeyDown && (guiEvent.keyCode==KeyCode.Return || guiEvent.keyCode==KeyCode.KeypadEnter || guiEvent.keyCode==KeyCode.Tab || guiEvent.keyCode==KeyCode.UpArrow || guiEvent.keyCode==KeyCode.DownArrow || guiEvent.keyCode==KeyCode.LeftArrow || guiEvent.keyCode==KeyCode.RightArrow))guiEvent.Use();
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            if(CurrentScreen==PeelScreen.Settings){DrawSettings(w,h);FocusMenu();return;}
            if(CurrentScreen==PeelScreen.Intro)
            {
                Panel(new Rect(38,h-326,625,288));
                GUI.Label(new Rect(60,h-310,580,45),"Just One More Peel",title);
                string intro=routePrototype ? (Settings.Language==PeelLanguage.Russian ? "Выйди из подъезда и исследуй стену объявлений.\nДержи ЛКМ, чтобы снимать бумагу.\nНа тележке можно купить инструменты." : "Leave your building and investigate the wall of notices.\nHold LMB to keep peeling paper.\nFind tools at the cart.") : T(PeelText.Intro);
                GUI.Label(new Rect(60,h-252,580,112),intro,text);
                string startLabel=routePrototype ? (File.Exists(WorldSavePath) || File.Exists(WorldSavePath+".bak") ? (Settings.Language==PeelLanguage.Russian ? "Продолжить · Enter" : "Continue · Enter") : (Settings.Language==PeelLanguage.Russian ? "Начать · Enter" : "Start · Enter")) : T(PeelText.Start);
                if(routePrototype)
                {
                    if(MenuButton(new Rect(60,h-102,180,42),startLabel,0))BeginShift();
                    if(MenuButton(new Rect(250,h-102,180,42),T(PeelText.Settings),1))OpenSettings();
                    if(MenuButton(new Rect(440,h-102,195,42),Settings.Language==PeelLanguage.Russian?"Осмотр маршрута":"Review route",2)){routePrototype.OpenForReview();BeginShift();}
                }
                else
                {
                    if(MenuButton(new Rect(60,h-102,280,42),startLabel,0))BeginShift();
                    if(MenuButton(new Rect(355,h-102,280,42),T(PeelText.Settings),1))OpenSettings();
                }
                FocusMenu();return;
            }
            string tool=activeTool==1?T(PeelText.Steamer):toolLevel>0?T(PeelText.Scraper):T(PeelText.Card);
            if(saveBlocked || saveWriteFailed)GUI.Label(new Rect(w/2-320,85,640,50),Settings.Language==PeelLanguage.Russian ? "Не удалось сохранить прогресс. Проверь файл сохранения." : "Could not save progress. Check the save file.",center);
            else if(CurrentScreen==PeelScreen.Playing && Time.unscaledTime<recoveryNoticeUntil)
            {
                Panel(new Rect(w/2-360,85,720,60));
                GUI.Label(new Rect(w/2-350,90,700,50),Settings.Language==PeelLanguage.Russian ? "Загружена резервная копия. Последние изменения могли потеряться." : "Backup loaded. Recent progress may have been lost.",center);
            }
            Panel(new Rect(0,h-49,w,49));
            int remaining=routePrototype ? routePrototype.RemainingHere : areaExperiment ? areaExperiment.RemainingHere : TotalPosters-ClearedPosters;
            string counter=routePrototype && routePrototype.ReviewMode ? (Settings.Language==PeelLanguage.Russian?"Осмотр маршрута":"Route review") : T(PeelText.RemainingPosters)+": "+remaining;
            GUI.Label(new Rect(30,h-44,w-60,28),"$"+money+"    "+counter+"    "+tool+"    "+T(PeelText.SwitchTools),small);
            if(CurrentScreen==PeelScreen.Playing)
            {
                if(Time.unscaledTime<payoutUntil)
                {
                    Panel(new Rect(20,h-83,130,29));
                    GUI.Label(new Rect(30,h-82,110,28),"+$"+payoutAmount,text);
                }
#if UNITY_EDITOR
                string testHint=Settings.Language==PeelLanguage.Russian
                    ? "Тест: F6 +$1000 · F7 скорость ×10: "+(testFastPeeling?"ВКЛ":"ВЫКЛ")+(routePrototype?" · F8 очистить участок":"")
                    : "Test: F6 +$1000 · F7 speed ×10: "+(testFastPeeling?"ON":"OFF")+(routePrototype?" · F8 clear area":"");
                Panel(new Rect(12,49,660,32));
                GUI.Label(new Rect(20,54,650,26),testHint,small);
#endif
                if(areaExperiment || routePrototype)
                {
                    Panel(new Rect(w/2-440,8,880,35));
                    string objective=basementLight && basementLight.IsDark ? (Settings.Language==PeelLanguage.Russian?"Темно — найди выключатель":"It's dark — find the light switch") : routePrototype ? routePrototype.Objective : areaExperiment.Objective;
                    GUI.Label(new Rect(w/2-430,8,860,35),objective,center);
                }
                GUI.Label(new Rect(w/2-10,h/2-14,20,28),IsPeeling?"·":"+",center);
                string hint=AwaitingRelease?T(PeelText.ReleaseGrip):IsPeeling?
                    T(activeTool==1?(steamExpanded?PeelText.SteamExpandedHint:PeelText.SteamHint):toolLevel>0?PeelText.ScraperHint:PeelText.CardHint):
                    target && target.IsExposed?T(activeTool==1 && steamUnlocked?(steamExpanded?PeelText.HoldExpandedSteam:PeelText.HoldSteam):toolLevel>0?PeelText.DragScraper:PeelText.HoldCard):"";
                if(hint.Length==0 && toolLevel==0 && !(routePrototype && routePrototype.ReviewMode))
                {
                    bool ru=Settings.Language==PeelLanguage.Russian;
                    hint=money>=UpgradeCost
                        ? (ru?"Шпатель доступен — купи его на тележке за $":"Scraper available — buy it at the cart for $")+UpgradeCost
                        : (ru?"Шпатель на тележке: $":"Scraper at the cart: $")+money+" / $"+UpgradeCost;
                }
                if(hint.Length>0)Panel(new Rect(w/2-475,h-104,950,44));
                GUI.Label(new Rect(w/2-470,h-104,940,44),hint,center);
                if(CanUseWorkbench())GUI.Label(new Rect(w/2-280,h-150,560,30),T(PeelText.Workbench),center);
                if(routePrototype && routePrototype.CanOpenDoor())
                    GUI.Label(new Rect(w/2-280,h-150,560,30),Settings.Language==PeelLanguage.Russian?"E — открыть дверь":"E — open door",center);
                if(basementLight && basementLight.CanInteract())
                {
                    string lightHint=Settings.Language==PeelLanguage.Russian ? (basementLight.IsOn?"E — выключить свет":"E — включить свет") : (basementLight.IsOn?"E — switch off light":"E — switch on light");
                    GUI.Label(new Rect(w/2-280,h-150,560,30),lightHint,center);
                }
            }
            if(CurrentScreen==PeelScreen.Shop)
            {
                Panel(new Rect(38,70,500,435));GUI.Label(new Rect(58,90,290,42),T(PeelText.Tools),title);
                GUI.Label(new Rect(350,94,155,30),"$"+money,shopBalance);
                string upgrade=toolLevel>=3?T(PeelText.MaxUpgrade):string.Format(T(toolLevel==0?PeelText.BuyScraper:PeelText.Upgrade),UpgradeCost);
                string scraperEffect=toolLevel==0?T(PeelText.ShopScraperEffect):SharpeningPreview();
                if(ShopOffer(145,0,upgrade,scraperEffect,toolLevel>=3,UpgradeCost))TryUpgrade();
                string steam=steamUnlocked?T(PeelText.SteamOwned):string.Format(T(PeelText.BuySteam),SteamCost);
                if(ShopOffer(240,1,steam,T(PeelText.SteamDescription),steamUnlocked,SteamCost,toolLevel==0?T(PeelText.SteamLocked):null))TryBuySteam();
                string expansion=steamExpanded?T(PeelText.SteamExpandedOwned):string.Format(T(PeelText.BuySteamExpansion),SteamExpansionCost);
                if(ShopOffer(335,2,expansion,T(PeelText.ShopWiderSteamEffect),steamExpanded,SteamExpansionCost,!steamUnlocked?T(PeelText.ShopRequiresSteam):null))TryExpandSteam();
                if(MenuButton(new Rect(58,448,460,40),T(PeelText.Close),3))Back();FocusMenu();
            }
            if(CurrentScreen==PeelScreen.Paused)
            {
                float x=w/2-210,y=h/2-150;Panel(new Rect(x,y,420,300));GUI.Label(new Rect(x+25,y+15,370,45),T(PeelText.Pause),title);
                var elapsed=System.TimeSpan.FromSeconds(ElapsedPlayTime);
                GUI.Label(new Rect(x+220,y+29,175,28),T(PeelText.PlayTime)+" "+((int)elapsed.TotalHours).ToString("00")+":"+elapsed.Minutes.ToString("00")+":"+elapsed.Seconds.ToString("00"),small);
                GUI.Label(new Rect(x+25,y+55,370,24),PauseSaveStatus(),small);
                if(MenuButton(new Rect(x+25,y+82,370,44),T(PeelText.Resume),0))Back();
                if(MenuButton(new Rect(x+25,y+145,370,44),T(PeelText.Settings),1))OpenSettings();
                if(MenuButton(new Rect(x+25,y+208,370,44),T(PeelText.Restart),2))RequestRestart();FocusMenu();
            }
            if(CurrentScreen==PeelScreen.ConfirmRestart)
            {
                bool ru=Settings.Language==PeelLanguage.Russian;
                bool review=routePrototype && routePrototype.ReviewMode;
                float x=w/2-250,y=h/2-160;
                Panel(new Rect(x,y,500,320));
                GUI.Label(new Rect(x+25,y+18,450,45),ru ? "Начать заново?" : "Start over?",title);
                string explanation=review
                    ? (ru ? "Текущий осмотр начнётся заново. Сохранённое прохождение останется." : "The current review will restart. Your saved game will remain.")
                    : (ru ? "Прогресс, деньги и инструменты будут сброшены." : "Progress, money and tools will be reset.");
                GUI.Label(new Rect(x+25,y+75,450,75),explanation,text);
                if(MenuButton(new Rect(x+25,y+160,450,44),ru ? "Отмена" : "Cancel",0))Back();
                if(MenuButton(new Rect(x+25,y+222,450,44),ru ? "Начать заново" : "Start over",1))RestartShift();
                FocusMenu();
            }
            if(routePrototype && routePrototype.Completed && CurrentScreen==PeelScreen.Playing)
            {
                if(Time.unscaledTime<completionNoticeUntil)
                {
                    var elapsed=System.TimeSpan.FromSeconds(ElapsedPlayTime);
                    string time=((int)elapsed.TotalHours).ToString("00")+":"+elapsed.Minutes.ToString("00")+":"+elapsed.Seconds.ToString("00");
                    Panel(new Rect(w/2-260,h-145,520,86));
                    GUI.Label(new Rect(w/2-245,h-140,490,76),routePrototype.FinishText(time),center);
                }
                return;
            }
            float noticeY=(areaExperiment || routePrototype) && CurrentScreen==PeelScreen.Playing ? 48 : 30;
            if(ClearedPosters==TotalPosters)GUI.Label(new Rect(w/2-360,noticeY,720,50),T(PeelText.Finished),center);
            else if(Time.time<noticeUntil && (!routePrototype || notice!=PeelText.Welcome))GUI.Label(new Rect(w/2-450,noticeY,900,50),T(notice),center);
        }
        void DrawSettings(float w,float h)
        {
            float x=w/2-285,y=h/2-230;Panel(new Rect(x,y,570,460));
            GUI.Label(new Rect(x+25,y+16,520,45),T(PeelText.Settings),title);
            GUI.Label(new Rect(x+25,y+83,180,32),T(PeelText.Language),text);
            if(MenuButton(new Rect(x+205,y+78,155,40),Settings.Language==PeelLanguage.English?"• English":"English",0))Settings.SetLanguage(PeelLanguage.English);
            if(GUI.Button(new Rect(x+373,y+78,170,40),Settings.Language==PeelLanguage.Russian?"• Русский":"Русский",button))Settings.SetLanguage(PeelLanguage.Russian);
            GUI.Label(new Rect(x+25,y+144,510,32),T(PeelText.Sound)+"   "+Mathf.RoundToInt(Settings.SoundVolume*100)+"%",text);
            GUI.SetNextControlName("peel-menu-1");float volume=GUI.HorizontalSlider(new Rect(x+28,y+187,510,20),Settings.SoundVolume,0,1);
            if(!Mathf.Approximately(volume,Settings.SoundVolume))Settings.SetSoundVolume(volume);
            GUI.Label(new Rect(x+25,y+224,510,32),T(PeelText.Sensitivity)+"   "+Settings.LookSensitivity.ToString("0.00")+"×",text);
            GUI.SetNextControlName("peel-menu-2");float sensitivity=GUI.HorizontalSlider(new Rect(x+28,y+267,510,20),Settings.LookSensitivity,.25f,2);
            if(!Mathf.Approximately(sensitivity,Settings.LookSensitivity))Settings.SetLookSensitivity(sensitivity);
            GUI.Label(new Rect(x+25,y+307,520,67),T(PeelText.SettingsHelp),small);
            if(MenuButton(new Rect(x+25,y+391,520,42),T(PeelText.Back),3))Back();
        }
    }
}
