using System.Collections.Generic;
using System.IO;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace JustOneMorePeel
{
    public enum PeelScreen { Intro, Playing, Paused, Shop, Settings, ConfirmRestart, ReadingArchiveNote }

    [RequireComponent(typeof(PeelSettings))]
    public class PeelGame : MonoBehaviour, IPeelGameUIState
    {
        public Camera view;
        public GameObject player;
        public Material[] papers;
        public Material brick;
        [SerializeField] private GameObject starterViewPrefab;
        [SerializeField] private GameObject scraperViewPrefab;
        [SerializeField] private GameObject steamerViewPrefab;
        [SerializeField] private bool archiveIssueSlice;
        [SerializeField] private PeelPosterStack archiveIssueAddress;
        [SerializeField] private TextMesh archiveIssueShelfLabel;
        [SerializeField] private PeelWorkbench paperDropOff;
        public int money, removed, total, toolLevel;
        public bool steamUnlocked;
        public bool steamExpanded;
        public float reach=3.4f;
        public const int SteamCost=PeelProgressionController.SteamCost;
        public const int SteamExpansionCost=PeelProgressionController.SteamExpansionCost;
        const string ArchiveIssueSaveFile="main-archive-issue-v1.json";
        const string MainRouteSaveFile="main-route-save-v1.json";
        public PeelSettings Settings { get; private set; }
        public PeelScrapPool Scraps { get; private set; }
        public PeelPosterOutline Outline { get; private set; }
        public PeelScreen CurrentScreen => screenController.CurrentScreen;
        public bool AwaitingRelease => interaction.AwaitingRelease;
        public bool IsPeeling => interaction.IsPeeling;
        internal System.Collections.Generic.IReadOnlyList<PeelPosterStack> WorkingStacks => interaction.WorkingStacks;
        public float GripProgress => interaction.GripProgress;
        internal Vector3 ToolContactPoint => interaction.ToolContactPoint;
        public bool InDarkness => basementLight && basementLight.IsDark;
        public int TotalPosters { get; private set; }
        public int ClearedPosters { get; private set; }
        public event System.Action<PeelPosterStack> PosterWasCleared;
        internal event System.Action<int> PaperDetached;
        public float ElapsedPlayTime { get; private set; }
        public float Progress => total==0?0:(float)removed/total;
        public int UpgradeCost => PeelProgressionController.UpgradeCost(toolLevel);
        public int activeTool;
        internal bool ArchiveIssueSlice => archiveIssueSlice;
        internal bool SteamAvailable => PaperCollectionActive ? trialScraperOwned : archiveIssueSlice || steamUnlocked;
        internal bool CanPeelPaper => !PaperCollectionActive || progression.FreePaperSlots>0;
        bool PaperCollectionActive { get; set; }
        bool trialScraperOwned;

        readonly List<PeelPosterStack> stacks=new List<PeelPosterStack>(19);
        readonly PeelAudioController audio=new PeelAudioController();
        readonly PeelInteractionController interaction=new PeelInteractionController();
        readonly PeelScreenController screenController=new PeelScreenController();
        PeelSaveCoordinator save;
        PeelProgressionController progression;
        PeelGameUI ui;
        FirstPersonController controller;
        CharacterController playerCapsule;
        PeelRoutePrototype routePrototype;
        PeelBasementLight basementLight;
        PeelWorkbench shopWorkbench;
        PeelScreen settingsReturn=PeelScreen.Intro, restartReturn;
        float originalRotationSpeed=1;
        bool appliedSettings;
        PeelLanguage appliedLanguage;
        bool worldSession, restarting, resetFailed, completionSeen;
        float nextWorldSave, completionNoticeUntil;
        bool HasSavedShift => archiveIssueSlice || routePrototype;
        bool CanSaveShift => HasSavedShift && !PaperCollectionActive && (!routePrototype || !routePrototype.ReviewMode);
        int furthestAnnouncedArea;
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        Vector3 recoveryPosition;
        Quaternion recoveryRotation;
#if UNITY_EDITOR
        int furthestPlaytestArea = -1;
        public static string WorldSavePathOverride;
        public static bool IsTimingPlaytest { get; set; }
#endif
        string WorldSavePath
        {
            get
            {
#if UNITY_EDITOR
                if(!string.IsNullOrEmpty(WorldSavePathOverride))return WorldSavePathOverride;
                if(archiveIssueSlice)return Path.Combine(Application.dataPath,"..","Library",ArchiveIssueSaveFile);
#endif
                if(routePrototype && routePrototype.IsMainRoute)
                    return Path.Combine(Application.persistentDataPath,MainRouteSaveFile);
                return archiveIssueSlice ? Path.Combine(Application.persistentDataPath,ArchiveIssueSaveFile) : PeelWorldSave.DefaultPath;
            }
        }

        int IPeelGameUIState.Money => money;
        int IPeelGameUIState.ToolLevel => toolLevel;
        int IPeelGameUIState.ActiveTool => activeTool;
        bool IPeelGameUIState.SteamUnlocked => SteamAvailable;
        bool IPeelGameUIState.SteamExpanded => steamExpanded;
        bool IPeelGameUIState.WorldSession => worldSession;
        bool IPeelGameUIState.ArchiveIssueSlice => archiveIssueSlice;
        bool IPeelGameUIState.ArchiveIssueAddressFound => archiveIssueAddress && archiveIssueAddress.ClearedPosters>0;
        bool IPeelGameUIState.PaperCollectionActive => PaperCollectionActive;
        int IPeelGameUIState.CollectedItems => progression.CollectedItems;
        int IPeelGameUIState.PaperCapacity => progression.PaperCapacity;
        int IPeelGameUIState.NextPaperCapacity => progression.NextPaperCapacity;
        int IPeelGameUIState.PaperCapacityPrice => progression.PaperCapacityPrice;
        bool IPeelGameUIState.TrialScraperOwned => trialScraperOwned;
        PeelScreen IPeelGameUIState.RestartReturn => restartReturn;
        bool IPeelGameUIState.ResetFailed => resetFailed;
        float IPeelGameUIState.CompletionNoticeUntil => completionNoticeUntil;

        void Start()
        {
            if(!view)view=Camera.main;
            if(!view || !player){Debug.LogError("PeelGame: missing player or camera",this);enabled=false;return;}
            spawnPosition=player.transform.position;spawnRotation=player.transform.rotation;
            recoveryPosition=spawnPosition;recoveryRotation=spawnRotation;
            Application.runInBackground=true;
            controller=player.GetComponentInChildren<FirstPersonController>();
            playerCapsule=player.GetComponentInChildren<CharacterController>();
            var inputs=player.GetComponentInChildren<StarterAssetsInputs>();
            if(controller)originalRotationSpeed=controller.RotationSpeed;
            Settings=GetComponent<PeelSettings>();Settings.Load();
            screenController.Initialize(controller,inputs);
            audio.Initialize(this,Settings);
            Scraps=new GameObject("Bounded paper scrap pool").AddComponent<PeelScrapPool>();
            Scraps.Initialize(view,player);
            Outline=new GameObject("Target poster outline").AddComponent<PeelPosterOutline>();
            foreach(var stack in FindObjectsByType<PeelPosterStack>())
            {stacks.Add(stack);stack.Initialize(this);TotalPosters+=stack.PosterCount;}
            var wallVisibility=gameObject.AddComponent<PeelWallVisibility>();wallVisibility.Initialize(view,stacks);
            var posterField=GetComponent<MillionPosterField>();
            if(posterField)
            {
                posterField.Initialize(this);
                TotalPosters+=MillionPosterField.PosterCount;
            }
            var areaExperiment=GetComponent<PeelAreaExperiment>();
            routePrototype=GetComponent<PeelRoutePrototype>();
            if(routePrototype)gameObject.AddComponent<DoorPaperBatch>().Initialize(routePrototype.DoorPosters);
            basementLight=GetComponent<PeelBasementLight>();
            total=TotalPosters*4;removed=0;ClearedPosters=0;
            string saveScene=archiveIssueSlice ? PeelWorldSave.ArchiveIssueScene :
                routePrototype && routePrototype.IsMainRoute ? PeelWorldSave.MainRouteScene : PeelWorldSave.RouteScene;
            save=new PeelSaveCoordinator(this,()=>WorldSavePath,saveScene,stacks,TotalPosters,routePrototype,basementLight);
            interaction.Initialize(audio,wallVisibility,posterField,Outline,Scraps,screenController.SetLookFrozen,
                detached=>PaperDetached?.Invoke(detached));
            ui=new PeelGameUI(this,interaction,save,routePrototype,areaExperiment,basementLight);
            progression=new PeelProgressionController(Release,SavePurchase,NotifyProgression,ui.ShowPayout
#if UNITY_EDITOR
                ,LogRoutePlaytest
#endif
            );
            Settings.Changed+=ApplySettings;ApplySettings();
            new GameObject("First person tool").AddComponent<PeelToolView>().Initialize(this,starterViewPrefab,scraperViewPrefab,steamerViewPrefab);
            screenController.CurrentScreen=posterField?PeelScreen.Playing:PeelScreen.Intro;
            interaction.AwaitingRelease=true;ui.ShowWelcome();screenController.RefreshCursor();
        }
        void ApplySettings()
        {
            audio.ApplySettings();
            if(controller)controller.RotationSpeed=originalRotationSpeed*Settings.LookSensitivity;
            if(!appliedSettings || appliedLanguage!=Settings.Language)
            {
                foreach(var stack in stacks)stack.RefreshMaterials();
                if(archiveIssueShelfLabel)
                    archiveIssueShelfLabel.text=Settings.Language==PeelLanguage.Russian ? "Полка Б" : "Shelf B";
                if(paperDropOff)paperDropOff.RefreshPaperDropOffLabel(Settings);
                Settings.ApplyWorldText();appliedLanguage=Settings.Language;appliedSettings=true;
            }
        }
        void Update()
        {
            audio.UpdatePeelSound(Time.unscaledDeltaTime);
            audio.UpdateAreaAmbience(Time.unscaledDeltaTime,archiveIssueSlice ? 2 : routePrototype ? routePrototype.CurrentArea : 0,
                routePrototype && CurrentScreen==PeelScreen.Playing && routePrototype.CourtyardKnockActive);
            if(!screenController.ApplicationFocused)return;
            if(routePrototype && CurrentScreen==PeelScreen.Playing && player.transform.position.y < -10f)
            {
                Release();
                if(playerCapsule)playerCapsule.enabled=false;
                player.transform.SetPositionAndRotation(recoveryPosition,recoveryRotation);
                if(playerCapsule)playerCapsule.enabled=true;
                if(controller)controller.RestoreLookPitch(0);
                Physics.SyncTransforms();
            }
            if(routePrototype && routePrototype.Completed && !completionSeen)
            {
                completionSeen=true;completionNoticeUntil=Time.unscaledTime+6f;
#if UNITY_EDITOR
                LogRoutePlaytest("Shift complete");
#endif
            }
            if(CurrentScreen==PeelScreen.Playing && (!routePrototype || !routePrototype.Completed))ElapsedPlayTime+=Time.unscaledDeltaTime;
            if(CurrentScreen==PeelScreen.Playing && routePrototype && !routePrototype.Completed && !routePrototype.ReviewMode)
            {
                int area=routePrototype.CurrentArea;
                if(area>furthestAnnouncedArea)
                {
                    furthestAnnouncedArea=area;
                    ui.Notify(area==1 ? PeelText.EnterBasement : PeelText.EnterArchive);
                }
            }
#if UNITY_EDITOR
            if (CurrentScreen == PeelScreen.Playing && routePrototype && !routePrototype.Completed
                && !routePrototype.ReviewMode && routePrototype.CurrentArea > furthestPlaytestArea)
            {
                furthestPlaytestArea = routePrototype.CurrentArea;
                string chapter = furthestPlaytestArea == 0 ? "Courtyard" : furthestPlaytestArea == 1 ? "Basement" : "Archive";
                LogRoutePlaytest(chapter + " entered");
            }
#endif
            if(worldSession && CurrentScreen==PeelScreen.Playing && Time.unscaledTime>=nextWorldSave)
            { SaveWorld();nextWorldSave=Time.unscaledTime+5f; }
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
#if UNITY_EDITOR
                if(!IsTimingPlaytest && (CurrentScreen==PeelScreen.Playing || CurrentScreen==PeelScreen.Shop))
                {
                    if(keyboard.f6Key.wasPressedThisFrame)money+=1000;
                    if(keyboard.f7Key.wasPressedThisFrame)interaction.testFastPeeling=!interaction.testFastPeeling;
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
                    if((CurrentScreen==PeelScreen.Shop || CurrentScreen==PeelScreen.ReadingArchiveNote) && keyboard.eKey.wasPressedThisFrame){Back();return;}
                    if(PaperCollectionActive && CurrentScreen==PeelScreen.Shop && keyboard.fKey.wasPressedThisFrame){Back();return;}
                    ui.NavigateMenu(keyboard);return;
                }
                if(keyboard.eKey.wasPressedThisFrame && routePrototype && routePrototype.CanReadArchiveNote())
                {SetScreen(PeelScreen.ReadingArchiveNote);return;}
                if(keyboard.eKey.wasPressedThisFrame && routePrototype && routePrototype.TryOpenDoor())return;
                if(keyboard.eKey.wasPressedThisFrame && basementLight && basementLight.TryToggle())return;
                bool openPaperShop=PaperCollectionActive && keyboard.fKey.wasPressedThisFrame;
                if(keyboard.eKey.wasPressedThisFrame || openPaperShop)
                {
                    var workbench=WorkbenchInReach();
                    if(workbench)
                    {
                        if(PaperCollectionActive && !openPaperShop)DeliverPaper();
                        else {shopWorkbench=workbench;SetScreen(PeelScreen.Shop);}
                        return;
                    }
                }
                if(keyboard.digit1Key.wasPressedThisFrame)
                {
                    Release();activeTool=0;
                    if(archiveIssueSlice)toolLevel=0;
                }
                if(keyboard.digit2Key.wasPressedThisFrame && SteamAvailable)
                {
                    Release();
                    if(archiveIssueSlice){activeTool=0;toolLevel=1;}
                    else activeTool=1;
                }
                if(archiveIssueSlice && SteamAvailable && keyboard.digit3Key.wasPressedThisFrame)
                {
                    Release();activeTool=1;toolLevel=1;
                }
            }
            if(CurrentScreen!=PeelScreen.Playing)return;
            var mouse=Mouse.current;
            if(mouse!=null)
            {
                if(!archiveIssueSlice && steamUnlocked && Cursor.lockState==CursorLockMode.Locked && Mathf.Abs(mouse.scroll.ReadValue().y)>.01f &&
                    (keyboard==null || (!keyboard.digit1Key.wasPressedThisFrame && !keyboard.digit2Key.wasPressedThisFrame)))
                {
                    Release();
                    activeTool=1-activeTool;
                }
                if(mouse.leftButton.wasPressedThisFrame && Cursor.lockState!=CursorLockMode.Locked){Release();return;}
                StepPeeling(mouse.leftButton.isPressed,mouse.delta.ReadValue(),Time.deltaTime);
            }
        }

        public void BeginShift()
        {
            if(CurrentScreen!=PeelScreen.Intro)return;
            if(CanSaveShift)
            {
                if(!LoadWorld())return;
                if(routePrototype)furthestAnnouncedArea=routePrototype.CurrentArea;
                worldSession=true;nextWorldSave=Time.unscaledTime+5f;
            }
            SetScreen(PeelScreen.Playing);
        }
        public void BeginPaperCollection()
        {
            if(CurrentScreen!=PeelScreen.Intro || !archiveIssueSlice || !paperDropOff)return;
            PaperCollectionActive=true;
            paperDropOff.gameObject.SetActive(true);
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
            else if(CurrentScreen==PeelScreen.ConfirmRestart)SetScreen(restartReturn);
            else if(CurrentScreen==PeelScreen.Paused || CurrentScreen==PeelScreen.Shop || CurrentScreen==PeelScreen.ReadingArchiveNote)SetScreen(PeelScreen.Playing);
        }
        public void RequestRestart()
        {
            if(CurrentScreen!=PeelScreen.Paused && CurrentScreen!=PeelScreen.Intro)return;
            restartReturn=CurrentScreen;resetFailed=false;SetScreen(PeelScreen.ConfirmRestart);
        }
        public void RestartShift()
        {
            if(CurrentScreen!=PeelScreen.ConfirmRestart)return;
            Settings.Save();
            if(CanSaveShift)
            {
                try { save.Reset(); }
                catch(System.Exception e){resetFailed=true;ui.FocusFirst();Debug.LogError("Could not reset world save: "+e.Message,this);return;}
            }
            ReloadToIntro();
        }
        public void ReturnToMainMenu()
        {
            if (CurrentScreen != PeelScreen.Paused || !HasSavedShift) return;
            if (worldSession && CanSaveShift)
            {
                if (!save.Succeeded || save.WriteFailed) SaveWorld();
                if (!save.Succeeded || save.Failed) return;
            }
            ReloadToIntro();
        }
        void ReloadToIntro()
        {
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
            if(screen!=PeelScreen.Shop)shopWorkbench=null;
            screenController.CurrentScreen=screen;if(Outline && screen!=PeelScreen.Playing)Outline.SetTarget(null);ui.FocusFirst();screenController.RefreshCursor();
        }
        internal void SaveWorld()
        {
            if(!worldSession || restarting)return;
            WriteWorld(false);
        }
        bool SavePurchase()
        {
            if(!worldSession)return true;
            if(WriteWorld(true))return true;
            ui.NotifyPurchaseFailed();
            return false;
        }
        bool WriteWorld(bool checkpointPurchase) =>
            save.Write(money,toolLevel,activeTool,steamUnlocked,steamExpanded,ElapsedPlayTime,player,controller,checkpointPurchase);
        bool LoadWorld() => save.Load(RequestRestart,ApplySavedProgress,RestorePlayer);
        void ApplySavedProgress(PeelWorldSave data,int savedRemoved,int savedCleared)
        {
            money=data.money;toolLevel=data.toolLevel;activeTool=data.activeTool;steamUnlocked=data.steamUnlocked;steamExpanded=data.steamExpanded;
            removed=savedRemoved;ClearedPosters=savedCleared;ElapsedPlayTime=data.elapsed;
        }
        void RestorePlayer(PeelWorldSave data)
        {
            Vector3 position=data.position;
            Quaternion rotation=Quaternion.Euler(0,data.rotation.eulerAngles.y,0);
            float pitch=data.lookPitch;
            bool restorePitch=data.hasLookPitch;
            var capsule=player.GetComponentInChildren<CharacterController>();
            if(capsule)
            {
                Vector3 center=capsule.transform.position-player.transform.position+capsule.transform.TransformVector(capsule.center)+data.position;
                float radius=capsule.radius*.9f;
                Vector3 axis=Vector3.up*Mathf.Max(0,capsule.height*.5f-capsule.radius);
                bool blocked=false,supported=false;
                foreach(var obstacle in Physics.OverlapCapsule(center-axis,center+axis,radius,~0,QueryTriggerInteraction.Ignore))
                    if(!obstacle.transform.IsChildOf(player.transform)){blocked=true;break;}
                foreach(var hit in Physics.RaycastAll(center,Vector3.down,5f,~0,QueryTriggerInteraction.Ignore))
                    if(!hit.transform.IsChildOf(player.transform)){supported=true;break;}
                if(blocked || !supported)
                {
                    Debug.LogWarning("Saved position is blocked or unsupported; using the scene spawn.",this);
                    position=spawnPosition;rotation=spawnRotation;pitch=0;restorePitch=true;
                }
            }
            player.transform.SetPositionAndRotation(position,rotation);
            if(restorePitch && controller)controller.RestoreLookPitch(pitch);
            Physics.SyncTransforms();
        }
        PeelWorkbench WorkbenchInReach()
        {
            if(!view || !player || !Physics.Raycast(view.transform.position,view.transform.forward,out var hit,2.5f))return null;
            var workbench=hit.collider.GetComponentInParent<PeelWorkbench>();
            return workbench && (workbench==paperDropOff)==PaperCollectionActive ? workbench : null;
        }
        public bool CanUseWorkbench() => WorkbenchInReach()!=null;
        void DeliverPaper()
        {
            var (items,payout)=progression.DeliverPaper(ref money);
            if(items==0)return;
            Release();
            ui.ShowPaperReceipt(items,payout);
        }
        public void StepPeeling(bool pressed,Vector2 delta,float seconds) =>
            interaction.StepPeeling(pressed,delta,seconds,view,reach,toolLevel,activeTool,SteamAvailable,steamExpanded,
                PaperCollectionActive ? progression.FreePaperSlots : null,ref removed);
        public bool NeedsStripScrap(PeelStrip strip) => interaction.NeedsStripScrap(strip);
        public static float PeelAmount(Vector2 delta,float seconds,int level,bool steam) =>
            PeelInteractionController.PeelAmount(delta,seconds,level,steam);
        void Release()
        {
            interaction.Release();screenController.SetLookFrozen(false);
        }

        public bool TryUpgrade()
        {
            if(PaperCollectionActive && (CurrentScreen!=PeelScreen.Shop || trialScraperOwned))return false;
            if(!progression.TryUpgrade(ref money,ref toolLevel))return false;
            if(PaperCollectionActive)trialScraperOwned=true;
            return true;
        }
        public bool TryUpgradePaperCapacity() =>
            PaperCollectionActive && CurrentScreen==PeelScreen.Shop && progression.TryUpgradePaperCapacity(ref money);
        public bool TryBuySteam() => progression.TryBuySteam(ref money,toolLevel,ref activeTool,ref steamUnlocked);
        public bool TryExpandSteam() => progression.TryExpandSteam(ref money,steamUnlocked,ref steamExpanded);
        internal void RecordStripDetached(bool partOfSheet) =>
            progression.RecordStripDetached(ref money,ref removed,total,toolLevel,steamUnlocked,steamExpanded,PaperCollectionActive,partOfSheet);
        internal void CollectSheet(int stripCount) => progression.CollectPaper(stripCount);
        void NotifyProgression(PeelText notice)
        {
            if(archiveIssueSlice && (notice==PeelText.ScraperAvailable || notice==PeelText.Finished))return;
            ui.Notify(notice);
            if(CurrentScreen!=PeelScreen.Shop || !PeelProgressionController.IsPurchaseNotice(notice))return;

            if(shopWorkbench)shopWorkbench.ShowPurchase(notice is PeelText.SteamBought or PeelText.SteamExpanded);
        }
#if UNITY_EDITOR
        void LogRoutePlaytest(string milestone)
        {
            if (!routePrototype || routePrototype.ReviewMode) return;
            var elapsed = System.TimeSpan.FromSeconds(ElapsedPlayTime);
            Debug.Log($"[QA-08] {elapsed:c} · {milestone} · balance ${money}", this);
        }
#endif
        public void PosterCleared(PeelPosterStack stack) { ClearedPosters++;PosterWasCleared?.Invoke(stack); }
        public void PosterMaterialsChanged(PeelPosterStack stack)
        { if(basementLight)basementLight.RefreshPosterMaterials(stack); }

        void LateUpdate()
        {
            screenController.LateUpdate();
            if(routePrototype && CurrentScreen==PeelScreen.Playing && screenController.ApplicationFocused &&
                playerCapsule && playerCapsule.enabled && playerCapsule.isGrounded && player.transform.position.y>-8f)
            {
                recoveryPosition=player.transform.position;
                recoveryRotation=player.transform.rotation;
            }
            if(player)
                audio.UpdateFootsteps(player.transform.position,CurrentScreen==PeelScreen.Playing && screenController.ApplicationFocused &&
                    controller && controller.enabled,controller && controller.Grounded,archiveIssueSlice || (routePrototype && routePrototype.CurrentArea>0));
        }
        void OnGUI() => ui?.Draw();
        void OnApplicationFocus(bool focused)
        {
            if(!Settings)return;
            screenController.ApplicationFocused=focused;
            if(!focused)
            {
                Release();
                screenController.ClearMovement();
                if(worldSession)SaveWorld();
                if(Outline)Outline.SetTarget(null);
            }
            else screenController.RefreshCursor();
        }
        void OnApplicationPause(bool paused) { if(paused && worldSession){Release();SaveWorld();} }
        void OnApplicationQuit() { if(worldSession){Release();SaveWorld();} }
        void OnDisable() {screenController.CurrentScreen=PeelScreen.Intro;screenController.SetLookFrozen(false);}
        void OnDestroy() {if(Settings)Settings.Changed-=ApplySettings;}
    }
}
