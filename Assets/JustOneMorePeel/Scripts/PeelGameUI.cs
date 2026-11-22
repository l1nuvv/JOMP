using UnityEngine;
using UnityEngine.InputSystem;
using static JustOneMorePeel.PeelProgressionController;

namespace JustOneMorePeel
{
    internal interface IPeelGameUIState
    {
        PeelSettings Settings { get; }
        PeelScreen CurrentScreen { get; }
        float ElapsedPlayTime { get; }
        int TotalPosters { get; }
        int ClearedPosters { get; }
        int UpgradeCost { get; }
        int Money { get; }
        int ToolLevel { get; }
        int ActiveTool { get; }
        bool SteamUnlocked { get; }
        bool SteamExpanded { get; }
        bool WorldSession { get; }
        bool ArchiveIssueSlice { get; }
        bool ArchiveIssueAddressFound { get; }
        bool PaperCollectionActive { get; }
        int CollectedItems { get; }
        int PaperCapacity { get; }
        int NextPaperCapacity { get; }
        int PaperCapacityPrice { get; }
        bool TrialScraperOwned { get; }
        PeelScreen RestartReturn { get; }
        bool ResetFailed { get; }
        float CompletionNoticeUntil { get; }
        void BeginShift();
        void BeginPaperCollection();
        void CopyArchiveToPaperCollection();
        void OpenSettings();
        void Back();
        void RequestRestart();
        void RestartShift();
        void ReturnToMainMenu();
        bool TryUpgrade();
        bool TryUpgradePaperCapacity();
        bool TryBuySteam();
        bool TryExpandSteam();
        bool CanUseWorkbench();
    }

    internal sealed class PeelGameUI
    {
        readonly IPeelGameUIState session;
        readonly PeelInteractionController interaction;
        readonly PeelSaveCoordinator shiftSave, paperSave;
        readonly PeelRoutePrototype routePrototype;
        readonly PeelAreaExperiment areaExperiment;
        readonly PeelBasementLight basementLight;
        bool focusMenu=true;
        int menuSelection;
        int payoutAmount, payoutFrame=-1;
        int paperReceiptItems;
        float payoutUntil;
        PeelText notice=PeelText.Welcome;
        bool purchaseSaveFailed;
        GUIStyle title, text, small, center, button, shopBalance, hudLabel, hudValue, hudObjective;
        static readonly Color Ink=new Color(.070f,.094f,.088f,.97f);
        static readonly Color Paper=new Color(.95f,.91f,.80f);
        static readonly Color Amber=new Color(.96f,.75f,.42f);
        float noticeUntil;

        PeelSettings Settings => session.Settings;
        PeelScreen CurrentScreen => session.CurrentScreen;
        float ElapsedPlayTime => session.ElapsedPlayTime;
        int TotalPosters => session.TotalPosters;
        int ClearedPosters => session.ClearedPosters;
        int UpgradeCost => session.UpgradeCost;
        int money => session.Money;
        int toolLevel => session.ToolLevel;
        int activeTool => session.ActiveTool;
        bool steamUnlocked => session.SteamUnlocked;
        bool steamExpanded => session.SteamExpanded;
        bool worldSession => session.WorldSession;
        bool archiveIssueSlice => session.ArchiveIssueSlice;
        bool paperCollection => session.PaperCollectionActive;
        PeelSaveCoordinator save => paperCollection ? paperSave : shiftSave;
        bool HasAlternateStart => routePrototype || paperSave!=null;
        bool CanCopyArchive => paperSave!=null && paperSave.InspectIfPathChanged()==SaveEntry.New
            && shiftSave.InspectIfPathChanged() is SaveEntry.Continue or SaveEntry.Backup;
        int ShopOptionCount => paperCollection?3:4;
        bool hasSavedShift => archiveIssueSlice || routePrototype;
        PeelScreen restartReturn => session.RestartReturn;
        bool resetFailed => session.ResetFailed;
        float completionNoticeUntil => session.CompletionNoticeUntil;
        bool IsPeeling => interaction.IsPeeling;
        bool AwaitingRelease => interaction.AwaitingRelease;
        PeelStrip target => interaction.Target;

        internal PeelGameUI(IPeelGameUIState session,PeelInteractionController interaction,PeelSaveCoordinator save,
            PeelSaveCoordinator paperSave,PeelRoutePrototype route,PeelAreaExperiment area,PeelBasementLight light)
        {
            this.session=session;this.interaction=interaction;shiftSave=save;this.paperSave=paperSave;
            routePrototype=route;areaExperiment=area;basementLight=light;
        }

        internal void FocusFirst() { menuSelection=0;focusMenu=true; }
        internal void ShowWelcome() { noticeUntil=Time.time+5; }
        internal void Notify(PeelText key) { notice=key;noticeUntil=Time.time+4;purchaseSaveFailed=false; }
        internal void NotifyPurchaseFailed() { noticeUntil=0;purchaseSaveFailed=true; }
        internal void ShowPayout()
        {
            if(payoutFrame!=Time.frameCount)payoutAmount=0;
            payoutAmount+=StripValue;payoutFrame=Time.frameCount;payoutUntil=Time.unscaledTime+1.25f;
        }
        internal void ShowPaperReceipt(int items,int payout)
        {
            paperReceiptItems=items;payoutAmount=payout;payoutUntil=Time.unscaledTime+3f;
        }
        internal void ShowPaperDeliveryFailure() { payoutUntil=0;Notify(PeelText.PaperDropOffSaveFailed); }

        string PauseSaveStatus()
        {
            bool ru=Settings.Language==PeelLanguage.Russian;
            if(routePrototype && routePrototype.ReviewMode)return ru ? "Осмотр не сохраняется" : "Route review is not saved";
            if(!worldSession)return "";
            if(save.Failed)return ru ? "Не удалось сохранить прогресс" : "Progress could not be saved";
            if(save.CheckpointBackupPending)return ru ? "Резерв ещё не обновлён" : "Backup update pending";
            return save.Succeeded ? (ru ? "Прогресс сохранён" : "Progress saved") : (ru ? "Ожидается первая запись" : "Waiting for first save");
        }
        internal void NavigateMenu(Keyboard keyboard)
        {
            int count=CurrentScreen==PeelScreen.Intro?(CanCopyArchive?4:HasAlternateStart?3:2)
                :CurrentScreen==PeelScreen.ConfirmRestart?2:CurrentScreen==PeelScreen.Settings?4:3;
            if(CurrentScreen==PeelScreen.Shop)count=ShopOptionCount;
            if(CurrentScreen==PeelScreen.ReadingArchiveNote)count=1;
            if (CurrentScreen == PeelScreen.Paused && hasSavedShift) count = 4;
            bool reverseTab=keyboard.tabKey.wasPressedThisFrame && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            if((keyboard.tabKey.wasPressedThisFrame && !reverseTab) || keyboard.downArrowKey.wasPressedThisFrame)
            {menuSelection=(menuSelection+1)%count;focusMenu=true;}
            if(reverseTab || keyboard.upArrowKey.wasPressedThisFrame){menuSelection=(menuSelection+count-1)%count;focusMenu=true;}
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
            if(CurrentScreen==PeelScreen.Intro)
            {
                if(menuSelection==0)session.BeginShift();
                else if(menuSelection==1)session.OpenSettings();
                else if(menuSelection==2 && routePrototype){routePrototype.OpenForReview();session.BeginShift();}
                else if(menuSelection==2 && paperSave!=null)session.BeginPaperCollection();
                else if(menuSelection==3 && CanCopyArchive)session.CopyArchiveToPaperCollection();
            }
            else if(CurrentScreen==PeelScreen.Paused){if(menuSelection==0)session.Back();else if(menuSelection==1)session.OpenSettings();else if(menuSelection==2)session.RequestRestart();else session.ReturnToMainMenu();}
            else if(CurrentScreen==PeelScreen.ReadingArchiveNote)session.Back();
            else if(CurrentScreen==PeelScreen.ConfirmRestart){if(menuSelection==0)session.Back();else session.RestartShift();}
            else if(CurrentScreen==PeelScreen.Settings)
            {if(menuSelection==0)Settings.SetLanguage(Settings.Language==PeelLanguage.English?PeelLanguage.Russian:PeelLanguage.English);else if(menuSelection==3)session.Back();}
            else if(CurrentScreen==PeelScreen.Shop)
            {
                if(menuSelection==0)session.TryUpgrade();
                else if(menuSelection==ShopOptionCount-1)session.Back();
                else if(paperCollection)session.TryUpgradePaperCapacity();
                else if(menuSelection==1)session.TryBuySteam();
                else session.TryExpandSteam();
            }
        }

        string T(PeelText key) => Settings.Text(key);
        void Styles()
        {
            if(title!=null && title.fontSize==32 && text!=null && text.fontSize==19)return;
            title=new GUIStyle(GUI.skin.label){fontSize=32,fontStyle=FontStyle.Bold};title.normal.textColor=Paper;
            text=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};text.normal.textColor=Paper;
            small=new GUIStyle(text){fontSize=15};small.normal.textColor=new Color(.78f,.85f,.83f);
            center=new GUIStyle(text){alignment=TextAnchor.MiddleCenter};
            shopBalance=new GUIStyle(text){alignment=TextAnchor.MiddleRight,fontStyle=FontStyle.Bold};
            shopBalance.normal.textColor=new Color(.97f,.87f,.63f);
            button=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true,alignment=TextAnchor.MiddleCenter};
            button.normal.textColor=button.hover.textColor=button.active.textColor=button.focused.textColor=Paper;
            hudLabel=new GUIStyle(small){fontSize=14,wordWrap=false};
            hudValue=new GUIStyle(hudLabel){fontSize=22,fontStyle=FontStyle.Bold};
            hudValue.normal.textColor=Paper;
            hudObjective=new GUIStyle(small){fontSize=17,fontStyle=FontStyle.Bold};
            hudObjective.normal.textColor=Paper;
        }
        static void Fill(Rect rect,Color color,float radius=0f) =>
            GUI.DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0f,color,0f,radius);
        void Panel(Rect rect)
        { Fill(rect,Ink,9f); }
        bool MenuButton(Rect rect,string label,int index)
        {
            GUI.SetNextControlName("peel-menu-"+index);
            bool selected=menuSelection==index;
            bool hovered=GUI.enabled && rect.Contains(Event.current.mousePosition);
            if(selected || hovered)Fill(rect,selected ? Amber : Paper,7f);
            Fill(new Rect(rect.x+1f,rect.y+1f,rect.width-2f,rect.height-2f),
                selected || hovered ? new Color(.21f,.28f,.23f) : new Color(.13f,.17f,.15f),6f);
            bool clicked=GUI.Button(rect,label,button);
            if(clicked)menuSelection=index;
            return clicked;
        }
        void FocusMenu()
        {if(focusMenu && Event.current.type==EventType.Repaint){GUI.FocusControl("peel-menu-"+menuSelection);focusMenu=false;}}
        string SharpeningPreview()
        {
            const float step=.1f;
            float current=step/PeelInteractionController.PeelAmount(Vector2.zero,step,toolLevel,false);
            if(toolLevel>=3)
                return string.Format(T(PeelText.ShopSharpenMaxTime),current);

            float next=step/PeelInteractionController.PeelAmount(Vector2.zero,step,toolLevel+1,false);
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
        internal void Draw()
        {
            if(!Settings)return;
            Styles();GUI.color=Color.white;GUI.contentColor=Color.white;
            var guiEvent=Event.current;
            if(guiEvent.type==EventType.KeyDown && (guiEvent.keyCode==KeyCode.Return || guiEvent.keyCode==KeyCode.KeypadEnter || guiEvent.keyCode==KeyCode.Tab || guiEvent.keyCode==KeyCode.UpArrow || guiEvent.keyCode==KeyCode.DownArrow || guiEvent.keyCode==KeyCode.LeftArrow || guiEvent.keyCode==KeyCode.RightArrow))guiEvent.Use();
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            if(CurrentScreen==PeelScreen.Settings){DrawSettings(w,h);FocusMenu();return;}
            if(CurrentScreen==PeelScreen.ReadingArchiveNote){DrawArchiveNote(w,h);FocusMenu();return;}
            if(CurrentScreen==PeelScreen.Intro)
            {
                float extra=CanCopyArchive?52f:0f;
                float introBottom=h-extra;
                Panel(new Rect(38,introBottom-326,625,288+extra));
                GUI.Label(new Rect(60,introBottom-310,580,45),"Just One More Peel",title);
                bool ru=Settings.Language==PeelLanguage.Russian;
                string intro=archiveIssueSlice
                    ? (ru ? "Домовой архив. Найди адрес дела №17 о стуке.\nОн под старым листом на левом планшете.\nWASD — ходьба, ЛКМ — снять лист.\n1 — карта, 2 — шпатель, 3 — пар."
                          : "Building records. File 17 is about the knocking.\nPeel the top sheet on the left board to find its shelf.\nWASD to move, LMB to peel.\n1 — Card, 2 — Scraper, 3 — Steam.")
                    : routePrototype ? (ru
                    ? "Подойди к заклеенной двери (WASD).\nНаведи взгляд на лист и удерживай ЛКМ, чтобы снять его.\nПосмотри на тележку вблизи и нажми E для покупки."
                    : "Walk up to the paper-covered door (WASD).\nAim at a notice and hold LMB to peel it.\nTo buy a tool, look at the cart up close and press E.") : T(PeelText.Intro);
                GUI.Label(new Rect(60,introBottom-252,580,100),intro,text);
                string startLabel=T(PeelText.Start);
                if(hasSavedShift)
                {
                    var saveEntry=save.InspectIfPathChanged();
                    string status="";
                    switch(saveEntry)
                    {
                        case SaveEntry.Continue:
                            startLabel=ru?"Продолжить":"Continue";
                            var saved=save.InspectedSave;
                            if(saved!=null)
                            {
                                string location=ru ? "Выдача" : "Issue counter";
                                if(routePrototype)
                                {
                                    int chapter=routePrototype.AreaAt(saved.position.z);
                                    location=chapter==0 ? (ru ? "Двор" : "Courtyard") : T(chapter==1 ? PeelText.EnterBasement : PeelText.EnterArchive);
                                }
                                string progress=save.InspectedShiftFinished ? (ru ? "Смена завершена · " : "Shift complete · ") : "";
                                status=$"{progress}{location} · ${saved.money}";
                            }
                            break;
                        case SaveEntry.Backup:
                            startLabel=ru?"Из резерва":"Load backup";
                            status=ru?"Основной файл недоступен. Можно продолжить с резервной копии."
                                :"Main save unavailable. You can continue from the backup.";
                            break;
                        case SaveEntry.Unreadable:
                        case SaveEntry.Incompatible:
                            startLabel=ru?"Сбросить сейв…":"Reset save…";
                            status=saveEntry==SaveEntry.Incompatible
                                ? (ru?"Версия сохранения не поддерживается.":"This save version is unsupported.")
                                : (ru?"Сохранение не читается.":"The save could not be read.");
                            status+=ru?" Для новой смены можно сбросить сейв.":" Reset the save to start a new shift.";
                            break;
                    }
                    GUI.Label(new Rect(60,introBottom-140,580,30),status,small);
                }
                if(HasAlternateStart)
                {
                    if(MenuButton(new Rect(60,introBottom-102,180,42),startLabel,0))session.BeginShift();
                    if(MenuButton(new Rect(250,introBottom-102,180,42),T(PeelText.Settings),1))session.OpenSettings();
                    string alternate=routePrototype ? (ru?"Осмотр маршрута":"Review route") : T(paperSave.InspectIfPathChanged() switch
                    {
                        SaveEntry.Continue => PeelText.PaperCollectionContinue,
                        SaveEntry.Backup => PeelText.PaperCollectionBackup,
                        SaveEntry.Unreadable or SaveEntry.Incompatible => PeelText.PaperCollectionReset,
                        _ => PeelText.PaperCollection
                    });
                    if(MenuButton(new Rect(440,introBottom-102,195,42),alternate,2))
                    {
                        if(routePrototype){routePrototype.OpenForReview();session.BeginShift();}
                        else session.BeginPaperCollection();
                    }
                    if(extra>0 && MenuButton(new Rect(60,h-102,575,42),T(PeelText.PaperCollectionCopy),3))
                        session.CopyArchiveToPaperCollection();
                }
                else
                {
                    if(MenuButton(new Rect(60,h-102,280,42),startLabel,0))session.BeginShift();
                    if(MenuButton(new Rect(355,h-102,280,42),T(PeelText.Settings),1))session.OpenSettings();
                }
                FocusMenu();return;
            }
            bool playing = CurrentScreen == PeelScreen.Playing;
            bool hasObjective = playing && (areaExperiment || hasSavedShift);
            float noticeY = hasObjective ? 106f : 22f;
            bool saveWarning = save.Failed;
            if (playing && (saveWarning || save.CheckpointBackupPending || Time.unscaledTime < save.RecoveryNoticeUntil))
            {
                bool russian = Settings.Language == PeelLanguage.Russian;
                string message = saveWarning && notice==PeelText.PaperDropOffSaveFailed && Time.time<noticeUntil
                    ? T(PeelText.PaperDropOffSaveFailed)
                    : saveWarning
                    ? (russian ? "Прогресс не записан. Проверь доступ к папке сохранений."
                        : "Progress was not saved. Check access to the save folder.")
                    : save.CheckpointBackupPending
                    ? (russian ? "Прогресс записан. Резервная копия ещё не обновлена."
                        : "Progress saved. The backup has not been updated yet.")
                    : (russian ? "Загружена резервная копия. Последние изменения могли потеряться."
                        : "Backup loaded. Recent progress may have been lost.");
                Panel(new Rect(w/2-360,noticeY,720,50));
                GUI.Label(new Rect(w/2-350,noticeY,700,50),message,center);
                noticeY += 55f;
            }
            if(playing)DrawPlayingHud(w,h);
            if(CurrentScreen==PeelScreen.Shop)
            {
                string tool=activeTool==1 ? T(PeelText.Steamer) : toolLevel>0 ? T(PeelText.Scraper) : T(PeelText.Card);
                bool showPurchase=Time.time<noticeUntil && IsPurchaseNotice(notice);
                bool showSaveStatus=saveWarning || save.CheckpointBackupPending;
                float purchaseY=paperCollection?389:493;
                float statusY=showPurchase ? purchaseY+38 : purchaseY;
                float bottom=showSaveStatus ? statusY+40 : purchaseY+(showPurchase?52:12);
                Panel(new Rect(38,70,500,bottom-70));
                GUI.Label(new Rect(58,77,290,42),T(paperCollection?PeelText.PaperShop:PeelText.Tools),title);
                GUI.Label(new Rect(350,94,155,30),"$"+money,shopBalance);
                GUI.Label(new Rect(58,116,460,22),string.Format(T(PeelText.EquippedTool),tool),small);
                bool scraperOwned=paperCollection ? session.TrialScraperOwned : toolLevel>=3;
                string upgrade=scraperOwned ? T(paperCollection?PeelText.PaperShopScraperOwned:PeelText.MaxUpgrade)
                    : string.Format(T(toolLevel==0?PeelText.BuyScraper:PeelText.Upgrade),UpgradeCost);
                string scraperEffect=paperCollection ? T(PeelText.PaperShopScraperEffect)
                    : toolLevel==0 ? T(PeelText.ShopScraperEffect) : SharpeningPreview();
                if(ShopOffer(145,0,upgrade,scraperEffect,scraperOwned,UpgradeCost))session.TryUpgrade();
                if(paperCollection)
                {
                    bool max=session.PaperCapacityPrice==0;
                    string label=max ? string.Format(T(PeelText.PaperCapacityMax),session.PaperCapacity)
                        : string.Format(T(PeelText.PaperCapacityOffer),session.NextPaperCapacity,session.PaperCapacityPrice);
                    string bundle=string.Format(T(PeelText.PaperCollectionCount),session.CollectedItems,session.PaperCapacity);
                    if(ShopOffer(240,1,label,bundle,max,session.PaperCapacityPrice))session.TryUpgradePaperCapacity();
                }
                else
                {
                    string steam=steamUnlocked?T(PeelText.SteamOwned):string.Format(T(PeelText.BuySteam),SteamCost);
                    if(ShopOffer(240,1,steam,T(PeelText.SteamDescription),steamUnlocked,SteamCost,toolLevel==0?T(PeelText.SteamLocked):null))session.TryBuySteam();
                    string expansion=steamExpanded?T(PeelText.SteamExpandedOwned):string.Format(T(PeelText.BuySteamExpansion),SteamExpansionCost);
                    if(ShopOffer(335,2,expansion,T(PeelText.ShopWiderSteamEffect),steamExpanded,SteamExpansionCost,!steamUnlocked?T(PeelText.ShopRequiresSteam):null))session.TryExpandSteam();
                }
                if(MenuButton(new Rect(58,paperCollection?344:448,460,40),T(PeelText.Close),ShopOptionCount-1))session.Back();
                if(showPurchase)GUI.Label(new Rect(58,purchaseY,460,38),T(notice),small);
                if(showSaveStatus)
                {
                    string status=purchaseSaveFailed && saveWarning
                        ? (Settings.Language==PeelLanguage.Russian ? "Покупка отменена. Проверь доступ к папке сохранений."
                            : "Purchase cancelled. Check access to the save folder.")
                        : PauseSaveStatus();
                    GUI.Label(new Rect(58,statusY,460,38),status,small);
                }
                FocusMenu();
            }
            if(CurrentScreen==PeelScreen.Paused)
            {
                bool reviewing=routePrototype && routePrototype.ReviewMode;
                bool showChapters=routePrototype && !reviewing;
                float extra=showChapters ? 54f : 0f;
                const float width=480,padding=25,contentWidth=width-2*padding;
                float x=w/2-width/2,y=h/2-(300f+extra)*.5f;
                float left=x+padding;
                Panel(new Rect(x,y,width,300f+extra));GUI.Label(new Rect(left,y+15,contentWidth,45),T(PeelText.Pause),title);
                GUI.Label(new Rect(x+width-200,y+29,175,28),T(PeelText.PlayTime)+" "+FormatPlayTime(),small);
                GUI.Label(new Rect(left,y+55,contentWidth,24),PauseSaveStatus(),small);
                if(showChapters)
                {
                    bool ru=Settings.Language==PeelLanguage.Russian;
                    GUI.Label(new Rect(left,y+80,contentWidth,20),ru ? "Обязательных листов осталось" : "Required notices left",small);
                    string chapters=$"{(ru ? "Двор" : "Courtyard")} {routePrototype.RemainingInArea(0)} · {T(PeelText.EnterBasement)} {routePrototype.RemainingInArea(1)} · {T(PeelText.EnterArchive)} {routePrototype.RemainingInArea(2)}";
                    GUI.Label(new Rect(left,y+101,contentWidth,26),chapters,text);
                }
                y+=extra;
                if(MenuButton(new Rect(left,y+82,contentWidth,44),T(PeelText.Resume),0))session.Back();
                if(MenuButton(new Rect(left,y+145,contentWidth,44),T(PeelText.Settings),1))session.OpenSettings();
                string restartLabel=reviewing
                    ? (Settings.Language==PeelLanguage.Russian?"Завершить осмотр":"End route review")
                    : T(PeelText.Restart);
                if(MenuButton(new Rect(left,y+192,contentWidth,40),restartLabel,2))session.RequestRestart();
                if(hasSavedShift && MenuButton(new Rect(left,y+241,contentWidth,40),
                    Settings.Language==PeelLanguage.Russian?"В главное меню":"Main menu",3))session.ReturnToMainMenu();
                FocusMenu();
            }
            if(CurrentScreen==PeelScreen.ConfirmRestart)
            {
                bool ru=Settings.Language==PeelLanguage.Russian;
                bool review=routePrototype && routePrototype.ReviewMode;
                bool fromIntro=restartReturn==PeelScreen.Intro && hasSavedShift;
                float x=w/2-250,y=h/2-160;
                Panel(new Rect(x,y,500,320));
                string heading=review ? (ru?"Завершить осмотр?":"End route review?")
                    : fromIntro ? (ru?"Сбросить сохранение?":"Reset the save?")
                    : (ru?"Начать заново?":"Start over?");
                GUI.Label(new Rect(x+25,y+18,450,45),heading,title);
                string explanation=review
                    ? (ru ? "Осмотр завершится, и откроется главное меню. Сохранённая смена останется." : "The review will end and return to the main menu. Your saved shift will remain.")
                    : paperCollection
                        ? (ru ? "Прогресс, деньги и резерв этой пробы будут сброшены." : "This trial’s progress, cash and backup will be reset.")
                    : fromIntro
                        ? (ru?"Текущее сохранение и его резервная копия будут удалены.":"The current save and its backup will be deleted.")
                        : (ru?"Прогресс, деньги и инструменты этой смены будут сброшены.":"Progress, money and tools from this shift will be reset.");
                GUI.Label(new Rect(x+25,y+75,450,55),explanation,text);
                if(resetFailed)GUI.Label(new Rect(x+25,y+130,450,26),
                    ru?"Не удалось сбросить сейв. Можно отменить.":"Could not reset the save. You can cancel.",small);
                if(MenuButton(new Rect(x+25,y+160,450,44),ru ? "Отмена" : "Cancel",0))session.Back();
                string confirm=review ? (ru?"Завершить осмотр":"End review")
                    : fromIntro ? (ru?"Сбросить сохранение":"Reset save")
                    : (ru?"Начать заново":"Start over");
                if(MenuButton(new Rect(x+25,y+222,450,44),confirm,1))session.RestartShift();
                FocusMenu();
            }
            if(!playing)return;
            if(routePrototype && routePrototype.Completed)
            {
                if(Time.unscaledTime<completionNoticeUntil)
                {
                    Panel(new Rect(w/2-260,h-145,520,86));
                    GUI.Label(new Rect(w/2-245,h-140,490,76),routePrototype.FinishText(FormatPlayTime()),center);
                }
                return;
            }
            if(!archiveIssueSlice && ClearedPosters==TotalPosters)
            {
                GUI.Label(new Rect(w/2-360,noticeY,720,50),T(PeelText.Finished),center);
                noticeY += 55f;
            }
            else if(Time.time<noticeUntil && notice!=PeelText.PaperDropOffSaveFailed && (notice!=PeelText.Welcome || (!routePrototype && !archiveIssueSlice)))
            {
                bool upgradeNotice=notice==PeelText.ScraperAvailable || notice==PeelText.SharpenAvailable;
                bool steamNotice=notice==PeelText.SteamAvailable || notice==PeelText.WiderSteamAvailable;
                int cost=upgradeNotice?UpgradeCost:notice==PeelText.SteamAvailable?SteamCost:SteamExpansionCost;
                string message=upgradeNotice || steamNotice ? string.Format(T(notice),cost) : T(notice);
                float width=Mathf.Min(720f,center.CalcSize(new GUIContent(message)).x+36f);
                DrawHint(new Rect((w-width)*.5f,noticeY,width,42),message);
                noticeY += 55f;
            }
        }

        void DrawPlayingHud(float w,float h)
        {
            bool ru=Settings.Language==PeelLanguage.Russian;
            bool complete=routePrototype && routePrototype.Completed && !routePrototype.ReviewMode;
            bool review=routePrototype && routePrototype.ReviewMode;
            bool door=routePrototype && routePrototype.UsesLocalCourtyardGate && routePrototype.CurrentArea==0;
            int remaining=routePrototype ? routePrototype.RemainingHere
                : areaExperiment ? areaExperiment.RemainingHere : TotalPosters-ClearedPosters;
            string objective=routePrototype ? routePrototype.Objective : areaExperiment ? areaExperiment.Objective : "";
            bool bundleFull=paperCollection && session.CollectedItems>=session.PaperCapacity;
            if(paperCollection)
                objective=bundleFull ? T(PeelText.PaperDropOffObjective)
                    : session.CollectedItems>0 ? T(PeelText.PaperDropOffOptional)
                    : (ru ? "Собери бумагу" : "Collect paper");
            else if(archiveIssueSlice)
            {
                objective=session.ArchiveIssueAddressFound
                    ? (ru ? "Полка Б за столом" : "Shelf B is past the desk")
                    : (ru ? "Сними верхний лист слева" : "Peel the top sheet on the left");
            }
            if(!complete && basementLight && basementLight.IsDark)
                objective=basementLight.SwitchIsOn
                    ? (ru ? "Лампа зажигается…" : "Lamp warming up…")
                    : (ru ? "Найди выключатель слева от входа" : "Find the switch left of the entrance");
            if(objective.Length>0)
            {
                string label=paperCollection ? T(PeelText.PaperCollection)
                    : archiveIssueSlice ? (ru ? "Дело №17" : "File 17")
                    : complete ? T(PeelText.PlayTime)+" "+FormatPlayTime()
                    : review ? (ru ? "Осмотр маршрута" : "Route review")
                    : (door ? (ru ? "На двери" : "On the door") : T(PeelText.RemainingPosters))+"  "+remaining;
                float width=Mathf.Clamp(hudObjective.CalcSize(new GUIContent(objective)).x+38f,268f,490f);
                float height=Mathf.Max(20f,hudObjective.CalcHeight(new GUIContent(objective),width-56f));
                Panel(new Rect(22,22,width,height+47f));
                Fill(new Rect(34,34,3,height+21f),Amber,1.5f);
                GUI.Label(new Rect(45,29,width-52,21),label,hudLabel);
                GUI.Label(new Rect(45,51,width-56,height+4f),objective,hudObjective);
            }

            if(!complete)
            {
                Panel(new Rect(w-140,22,118,50));
                GUI.Label(new Rect(w-128,28,94,35),"$"+money,hudValue);
                if(paperCollection)
                {
                    string bundle=string.Format(T(bundleFull ? PeelText.PaperCollectionFull : PeelText.PaperCollectionCount),
                        session.CollectedItems,session.PaperCapacity);
                    float width=center.CalcSize(new GUIContent(bundle)).x+24f;
                    DrawHint(new Rect(w-22f-width,80f,width,29f),bundle);
                    if(Time.unscaledTime<payoutUntil)
                    {
                        string receipt=string.Format(T(PeelText.PaperDropOffReceipt),paperReceiptItems,payoutAmount);
                        float receiptWidth=center.CalcSize(new GUIContent(receipt)).x+24f;
                        DrawHint(new Rect(w-22f-receiptWidth,117f,receiptWidth,29f),receipt);
                    }
                }
                else if(Time.unscaledTime<payoutUntil)
                    DrawHint(new Rect(w-140,80,118,29),"+$"+payoutAmount);
                if(toolLevel==0 && !review && !archiveIssueSlice)
                {
                    string offer=money>=UpgradeCost ? (ru ? "Шпатель на тележке" : "Scraper at the cart")
                        : T(PeelText.Scraper)+"  $"+money+" / $"+UpgradeCost;
                    float width=center.CalcSize(new GUIContent(offer)).x+24f;
                    DrawHint(new Rect(w-22f-width,Time.unscaledTime<payoutUntil ? 117f : 80f,width,29),offer);
                }

                bool hasSecondary=steamUnlocked;
                bool archiveTools=archiveIssueSlice && hasSecondary;
                bool secondarySelected=archiveIssueSlice ? toolLevel>0 && activeTool==0 : activeTool==1;
                string primary=T(toolLevel>0 && !archiveIssueSlice ? PeelText.Scraper : PeelText.Card);
                string secondary=T(archiveIssueSlice ? PeelText.Scraper : PeelText.Steamer);
                float primaryWidth=hudObjective.CalcSize(new GUIContent(primary)).x+(hasSecondary ? 65f : 34f);
                float secondaryWidth=hasSecondary ? hudObjective.CalcSize(new GUIContent(secondary)).x+65f : 0f;
                string steam=T(PeelText.Steamer);
                float steamWidth=archiveTools ? hudObjective.CalcSize(new GUIContent(steam)).x+65f : 0f;
                float toolX=w-22f-primaryWidth-(hasSecondary ? secondaryWidth+6f : 0f)
                    -(archiveTools ? steamWidth+6f : 0f);
                DrawToolSlot(new Rect(toolX,h-72,primaryWidth,50),primary,
                    hasSecondary ? "1" : "",archiveIssueSlice ? toolLevel==0 : !secondarySelected);
                if(hasSecondary)DrawToolSlot(new Rect(toolX+primaryWidth+6f,h-72,secondaryWidth,50),secondary,"2",secondarySelected);
                if(archiveTools)DrawToolSlot(new Rect(w-22f-steamWidth,h-72,steamWidth,50),steam,"3",activeTool==1);
            }
            DrawReticle(w,h);
            if(IsPeeling)return;

            string hint=bundleFull ? "" : AwaitingRelease ? (complete ? "" : T(PeelText.ReleaseGrip)) : TargetHint();
            if(!AwaitingRelease && target && routePrototype && door && routePrototype.IsOptionalCourtyardNotice(target.Stack))
                hint=(ru ? "Дополнительно" : "Optional")+" · "+hint;
            if(session.CanUseWorkbench())
                hint=paperCollection
                    ? T(session.CollectedItems>0 ? PeelText.PaperDropOffInteract : PeelText.PaperDropOffEmpty)
                    : WorkbenchHint();
            if(routePrototype && routePrototype.CanOpenDoor())hint=ru ? "E   Открыть дверь" : "E   Open door";
            if(basementLight && basementLight.CanInteract())
                hint=basementLight.SwitchIsOn ? (ru ? "E   Выключить свет" : "E   Switch off light")
                    : (ru ? "E   Включить свет" : "E   Switch on light");
            if(routePrototype && routePrototype.CanReadArchiveNote())hint=T(PeelText.ReadArchiveNote);
            if(hint.Length==0)return;

            float hintWidth=Mathf.Min(680f,center.CalcSize(new GUIContent(hint)).x+32f);
            float hintHeight=Mathf.Max(36f,center.CalcHeight(new GUIContent(hint),hintWidth-24f)+12f);
            DrawHint(new Rect((w-hintWidth)*.5f,h-83f-hintHeight,hintWidth,hintHeight),hint);
        }

        void DrawHint(Rect rect,string label)
        {
            Panel(rect);
            GUI.Label(new Rect(rect.x+8f,rect.y,rect.width-16f,rect.height),label,center);
        }

        void DrawToolSlot(Rect rect,string label,string key,bool active)
        {
            Panel(rect);
            if(active)Fill(new Rect(rect.x+9,rect.yMax-6,rect.width-18,2),Amber,1f);
            float labelX=rect.x+12f;
            if(key.Length>0)
            {
                Fill(new Rect(labelX,rect.y+13,23,22),new Color(.20f,.25f,.21f),4f);
                GUI.Label(new Rect(labelX+6,rect.y+13,17,21),key,hudLabel);
                labelX+=32f;
            }
            GUI.Label(new Rect(labelX,rect.y+13,rect.xMax-labelX-6f,25),label,hudObjective);
        }
        string WorkbenchHint()
        {
            string scraperOffer=CanUpgrade(money,toolLevel) ? string.Format(T(toolLevel==0 ? PeelText.BuyScraper : PeelText.Upgrade),UpgradeCost) : "";
            string steamOffer=CanBuySteam(money,toolLevel,steamUnlocked) ? string.Format(T(PeelText.BuySteam),SteamCost) :
                CanExpandSteam(money,steamUnlocked,steamExpanded) ? string.Format(T(PeelText.BuySteamExpansion),SteamExpansionCost) : "";
            if(scraperOffer.Length==0 && steamOffer.Length==0)return T(PeelText.Workbench);
            if(scraperOffer.Length==0)return "E · "+steamOffer;
            return steamOffer.Length==0 ? "E · "+scraperOffer : $"E · {scraperOffer} / {steamOffer}";
        }
        void DrawReticle(float width,float height)
        {
            bool pulling=IsPeeling;
            float x=width*.5f,y=height*.5f;
            Fill(new Rect(x-3f,y-3f,6f,6f),Ink,3f);
            Fill(new Rect(x-1.5f,y-1.5f,3f,3f),pulling ? Amber : Paper,1.5f);
            if(!pulling)return;

            Fill(new Rect(x-20f,y+12f,40f,5f),Ink,2.5f);
            float filled=38f*Mathf.Clamp01(interaction.GripCompletion);
            if(filled>0f)Fill(new Rect(x-19f,y+13f,filled,3f),Amber,1.5f);
        }
        void DrawArchiveNote(float width,float height)
        {
            Panel(new Rect(0,0,width,height));
            var note=routePrototype ? routePrototype.ArchiveNoteTexture : null;
            if(note)
            {
                float pageHeight=Mathf.Min(height-86f,620f);
                float pageWidth=pageHeight*note.width/note.height;
                GUI.DrawTexture(new Rect((width-pageWidth)*.5f,(height-pageHeight-54f)*.5f,pageWidth,pageHeight),note);
            }
            if(MenuButton(new Rect(width*.5f-90f,height-52f,180f,36f),T(PeelText.Back),0))session.Back();
        }
        string TargetHint()
        {
            if(!target || !target.IsExposed)return "";
            if(toolLevel==0)return T(PeelText.HoldCard);

            bool usingSteam=activeTool==1 && steamUnlocked;
            bool precision=target.Stack && target.Stack.PrecisionWork;
            if(usingSteam)
            {
                var hint=precision ? PeelText.PrecisionSwitchToScraper : steamExpanded ? PeelText.HoldExpandedSteam : PeelText.HoldSteam;
                return string.Format(T(hint),interaction.PreviewTargetCount);
            }

            return T(precision ? PeelText.PrecisionScraper : PeelText.DragScraper);
        }
        string FormatPlayTime()
        {
            var elapsed = System.TimeSpan.FromSeconds(ElapsedPlayTime);
            return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        void DrawSettings(float w,float h)
        {
            float x=w/2-285,y=h/2-230;Panel(new Rect(x,y,570,460));
            GUI.Label(new Rect(x+25,y+16,520,45),T(PeelText.Settings),title);
            GUI.Label(new Rect(x+25,y+83,180,32),T(PeelText.Language),text);
            if(MenuButton(new Rect(x+205,y+78,155,40),Settings.Language==PeelLanguage.English?"• English":"English",0))Settings.SetLanguage(PeelLanguage.English);
            if(GUI.Button(new Rect(x+373,y+78,170,40),Settings.Language==PeelLanguage.Russian?"• Русский":"Русский",button))
            {menuSelection=0;Settings.SetLanguage(PeelLanguage.Russian);}
            GUI.Label(new Rect(x+25,y+144,510,32),T(PeelText.Sound)+"   "+Mathf.RoundToInt(Settings.SoundVolume*100)+"%",text);
            float volume=SettingsSlider(new Rect(x+28,y+187,510,20),Settings.SoundVolume,0,1,1);
            if(!Mathf.Approximately(volume,Settings.SoundVolume))Settings.SetSoundVolume(volume);
            GUI.Label(new Rect(x+25,y+224,510,32),T(PeelText.Sensitivity)+"   "+Settings.LookSensitivity.ToString("0.00")+"×",text);
            float sensitivity=SettingsSlider(new Rect(x+28,y+267,510,20),Settings.LookSensitivity,.25f,2,2);
            if(!Mathf.Approximately(sensitivity,Settings.LookSensitivity))Settings.SetLookSensitivity(sensitivity);
            string controls=T(PeelText.SettingsHelp);
            if(paperCollection)controls=T(PeelText.PaperCollectionControls);
            else if(archiveIssueSlice)
                controls=Settings.Language==PeelLanguage.Russian
                    ? "WASD — движение · Мышь — обзор · ЛКМ — снять лист\n1 — карта · 2 — шпатель · 3 — пар · Esc — пауза/назад\nМеню: Tab / ↑↓ — выбор · ←→ — изменить · Enter — подтвердить"
                    : "WASD — move · Mouse — look · LMB — peel\n1 — Card · 2 — Scraper · 3 — Steam · Esc — pause/back\nMenus: Tab / ↑↓ — navigate · ←→ — adjust · Enter — select";
            GUI.Label(new Rect(x+25,y+300,520,83),controls,small);
            if(MenuButton(new Rect(x+25,y+391,520,42),T(PeelText.Back),3))session.Back();
        }
        float SettingsSlider(Rect rect,float value,float min,float max,int index)
        {
            if(Event.current.type==EventType.MouseDown && rect.Contains(Event.current.mousePosition))menuSelection=index;
            if(menuSelection==index)
            {
                var old=GUI.color;
                GUI.color=new Color(.97f,.78f,.42f);
                GUI.DrawTexture(new Rect(rect.x-8,rect.y-39,3,51),Texture2D.whiteTexture);
                GUI.color=old;
            }
            GUI.SetNextControlName("peel-menu-"+index);
            return GUI.HorizontalSlider(rect,value,min,max);
        }
    }
}
