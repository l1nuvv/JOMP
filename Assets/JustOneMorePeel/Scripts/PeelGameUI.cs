using System.Collections.Generic;
using System.Globalization;
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
        int SteamPrice { get; }
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
        float CompletionSummaryUntil { get; }
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
        bool CanDeliverPaper();
        bool CanReadHubCabinet();
    }

    internal sealed class PeelGameUI
    {
        readonly IPeelGameUIState session;
        readonly PeelInteractionController interaction;
        readonly PeelSaveCoordinator shiftSave, paperSave;
        readonly PeelRoutePrototype routePrototype;
        readonly PeelAreaExperiment areaExperiment;
        readonly PeelBasementLight basementLight;
        readonly PeelPavilion pavilion;
        readonly PeelPavilionHub hub;
        readonly Camera view;
        bool focusMenu=true;
        int menuSelection;
        const int CabinetColumns=5;
        const float CabinetRow=148, CabinetViewport=480, CabinetHeading=34;
        readonly List<PeelNoticeCollection.Card> cabinetCards=new();
        int cabinetFoundCount, cabinetCard=-1;
        Vector2 cabinetScroll;
        PeelNoticeCollection.Card cabinetDiscovery;
        int discoveryFrame=-1, discoveriesInFrame;
        float discoveryUntil;
        int payoutAmount, payoutFrame=-1;
        int paperReceiptItems;
        float payoutUntil;
        PeelText notice=PeelText.Welcome;
        PeelText? shopPurchase;
        bool capacityPurchased;
        bool purchaseSaveFailed;
        GUIStyle title, text, small, center, button, primaryButton, shopBalance, shopMutedBalance, hudLabel, hudPrimary, hudHint;
        GUIStyle collectionTitle, cabinetTileTitle;
        static readonly Color Ink=new Color(.070f,.094f,.088f,.97f);
        static readonly Color Paper=new Color(.95f,.91f,.80f);
        static readonly Color Amber=new Color(.96f,.75f,.42f);
        const float HudInset=22f, HudPadding=14f;
        float noticeUntil;
        PeelText? purchaseOffer;
        float purchaseOfferUntil;

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
        bool paperTrial => paperCollection && paperSave!=null;
        PeelSaveCoordinator save => paperTrial ? paperSave : shiftSave;
        bool HasAlternateStart => routePrototype || paperSave!=null;
        bool CanCopyArchive => paperSave!=null && paperSave.InspectIfPathChanged()==SaveEntry.New
            && shiftSave.InspectIfPathChanged() is SaveEntry.Continue or SaveEntry.Backup;
        int ShopOptionCount => paperTrial ? 3 : hub ? 5 : 4;
        bool hasSavedShift => archiveIssueSlice || routePrototype || pavilion;
        PeelScreen restartReturn => session.RestartReturn;
        bool resetFailed => session.ResetFailed;
        bool IsPeeling => interaction.IsPeeling;
        bool AwaitingRelease => interaction.AwaitingRelease;
        PeelStrip target => interaction.Target;
        bool ShowsFirstScraperOffer => toolLevel==0 && !archiveIssueSlice && !(routePrototype && routePrototype.ReviewMode);
        bool FirstScraperSignalActive => ShowsFirstScraperOffer && !paperCollection && CanUpgrade(money,toolLevel)
            && notice==PeelText.ScraperAvailable && Time.time<noticeUntil;

        internal PeelGameUI(IPeelGameUIState session,PeelInteractionController interaction,PeelSaveCoordinator save,
            PeelSaveCoordinator paperSave,PeelRoutePrototype route,PeelAreaExperiment area,PeelBasementLight light,Camera view,
            PeelPavilion pavilion,PeelPavilionHub hub)
        {
            this.session=session;this.interaction=interaction;shiftSave=save;this.paperSave=paperSave;
            routePrototype=route;areaExperiment=area;basementLight=light;
            this.view=view;this.pavilion=pavilion;this.hub=hub;
        }

        internal void ResetMenu(int selection=0)
        {
            menuSelection=selection;focusMenu=true;
            if(CurrentScreen==PeelScreen.ReadingHubCabinet)PrepareCabinet();
            shopPurchase=null;capacityPurchased=false;purchaseSaveFailed=false;
        }
        internal void ShowWelcome() { noticeUntil=Time.time+5; }
        internal void ShowCabinetDiscovery(PeelNoticeCollection.Card card)
        {
            discoveriesInFrame=discoveryFrame==Time.frameCount ? discoveriesInFrame+1 : 1;
            discoveryFrame=Time.frameCount;
            cabinetDiscovery=card;
            discoveryUntil=Time.unscaledTime+2.8f;
        }
        internal void Notify(PeelText key)
        {
            if((pavilion || (routePrototype && routePrototype.IsMainRoute)) &&
                key is (PeelText.ScraperAvailable or PeelText.SharpenAvailable or PeelText.SteamAvailable or PeelText.WiderSteamAvailable))
            {
                purchaseOffer=key;purchaseOfferUntil=Time.time+6f;
                return;
            }
            notice=key;noticeUntil=Time.time+4;
            if(CurrentScreen==PeelScreen.Shop && IsPurchaseNotice(key))
            {shopPurchase=key;capacityPurchased=false;purchaseSaveFailed=false;}
        }
        internal void NotifyPurchaseFailed()
        {noticeUntil=0;shopPurchase=null;capacityPurchased=false;purchaseSaveFailed=true;}

        void BuyPaperCapacity()
        {
            if(!session.TryUpgradePaperCapacity())return;
            capacityPurchased=true;shopPurchase=null;purchaseSaveFailed=false;
        }
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
            if(CurrentScreen==PeelScreen.ReadingHubCabinet){NavigateCabinet(keyboard);return;}
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
                    if(menuSelection==0)ChangeLanguage(direction>0?PeelLanguage.Russian:PeelLanguage.English);
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
            {if(menuSelection==0)ChangeLanguage(Settings.Language==PeelLanguage.English?PeelLanguage.Russian:PeelLanguage.English);else if(menuSelection==3)session.Back();}
            else if(CurrentScreen==PeelScreen.Shop)
            {
                if(menuSelection==0)session.TryUpgrade();
                else if(menuSelection==ShopOptionCount-1)session.Back();
                else if(paperTrial || (hub && menuSelection==3))BuyPaperCapacity();
                else if(menuSelection==1)session.TryBuySteam();
                else session.TryExpandSteam();
            }
        }

        string T(PeelText key) => Settings.Text(key);
        void Styles()
        {
            if(title!=null && title.fontSize==32 && text!=null && text.fontSize==19)return;
            title=new GUIStyle(GUI.skin.label){fontSize=32,fontStyle=FontStyle.Bold};title.normal.textColor=Paper;
            collectionTitle=new GUIStyle(title){fontSize=27,wordWrap=true};
            text=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};text.normal.textColor=Paper;
            small=new GUIStyle(text){fontSize=15};small.normal.textColor=new Color(.78f,.85f,.83f);
            cabinetTileTitle=new GUIStyle(small){fontSize=16,alignment=TextAnchor.UpperCenter};
            center=new GUIStyle(text){alignment=TextAnchor.MiddleCenter};
            shopBalance=new GUIStyle(text){alignment=TextAnchor.MiddleRight,fontStyle=FontStyle.Bold};
            shopBalance.normal.textColor=new Color(.97f,.87f,.63f);
            shopMutedBalance=new GUIStyle(shopBalance);
            shopMutedBalance.normal.textColor=small.normal.textColor;
            button=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true,alignment=TextAnchor.MiddleCenter};
            button.normal.textColor=button.hover.textColor=button.active.textColor=button.focused.textColor=Paper;
            hudLabel=new GUIStyle(small){padding=new RectOffset()};
            hudPrimary=new GUIStyle(hudLabel){fontSize=18,fontStyle=FontStyle.Bold};
            hudPrimary.normal.textColor=Paper;
            hudHint=new GUIStyle(hudPrimary){alignment=TextAnchor.UpperCenter,fontStyle=FontStyle.Normal};
            primaryButton=new GUIStyle(button){fontSize=hudPrimary.fontSize,fontStyle=hudPrimary.fontStyle};
            primaryButton.normal.textColor=primaryButton.hover.textColor=primaryButton.active.textColor=primaryButton.focused.textColor=Ink;
        }
        static void Fill(Rect rect,Color color,float radius=0f) =>
            GUI.DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0f,color,0f,radius);
        void Panel(Rect rect)
        { Fill(rect,Ink,9f); }
        bool MenuButton(Rect rect,string label,int index,bool primary=false)
        {
            GUI.SetNextControlName("peel-menu-"+index);
            bool selected=menuSelection==index;
            bool hovered=GUI.enabled && rect.Contains(Event.current.mousePosition);
            if(primary)
            {
                if(selected)Fill(new Rect(rect.x-4f,rect.y-4f,rect.width+8f,rect.height+8f),Paper,9f);
                Fill(rect,hovered ? Paper : Amber,6f);
            }
            else
            {
                if(selected || hovered)Fill(rect,selected ? Amber : Paper,7f);
                Fill(new Rect(rect.x+1f,rect.y+1f,rect.width-2f,rect.height-2f),
                    selected || hovered ? new Color(.21f,.28f,.23f) : new Color(.13f,.17f,.15f),6f);
            }
            bool clicked=GUI.Button(rect,label,primary ? primaryButton : button);
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
        bool ShopOffer(float y,int index,string label,string effect,int cost,string unavailable=null,bool owned=false)
        {
            bool ru=Settings.Language==PeelLanguage.Russian;
            string status=unavailable;
            if(owned)status=ru ? "Уже куплено" : "Owned";
            else if(status==null && money<cost)status=string.Format(T(PeelText.ShopCashShortfall),cost-money);
            bool available=status==null;
            var rect=new Rect(58,y,474,87);
            bool selected=menuSelection==index;
            bool hovered=available && rect.Contains(Event.current.mousePosition);
            if(selected || hovered)Fill(rect,selected ? Amber : Paper,8f);
            Fill(new Rect(rect.x+1f,y+1f,rect.width-2f,rect.height-2f),
                selected || hovered ? new Color(.20f,.27f,.24f) : new Color(.10f,.15f,.15f),7f);
            GUI.SetNextControlName("peel-menu-"+index);
            bool wasEnabled=GUI.enabled;
            GUI.enabled=wasEnabled && available;
            bool clicked=GUI.Button(rect,GUIContent.none,button);
            GUI.enabled=wasEnabled;
            if(clicked)menuSelection=index;

            GUI.Label(new Rect(72,y+9,345,24),label,hudPrimary);
            if(!owned)GUI.Label(new Rect(408,y+7,111,25),"$"+cost,available ? shopBalance : shopMutedBalance);
            GUI.Label(new Rect(72,y+35,446,22),effect,hudLabel);
            GUI.Label(new Rect(72,y+59,446,22),status ?? (ru ? "Можно купить" : "Available to buy"),hudLabel);
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
            if(CurrentScreen==PeelScreen.ReadingHubCabinet){DrawHubCabinet(w,h);FocusMenu();return;}
            if(CurrentScreen==PeelScreen.Intro)
            {
                bool ru=Settings.Language==PeelLanguage.Russian;
                string details=hub
                    ? (ru ? "Очисти витрину. Сдай бумагу в приёмной слева или у помоста справа.\nE — сдать пачку, открыть магазин или шкаф.\nWASD — ходьба, ЛКМ — снять бумагу."
                          : "Clear the display. Drop off paper at reception on the left or by the platform on the right.\nE — drop off paper, open the shop or inspect the cabinet.\nWASD to move, LMB to peel.")
                    : pavilion
                    ? (ru ? "Очисти витрину и включи подсветку росписи.\nWASD — ходьба, ЛКМ — снять бумагу.\nПодойди к тележке и нажми E для покупки."
                          : "Clear the display and turn on the mural lights.\nWASD to move, LMB to peel.\nLook at the cart up close and press E to shop.")
                    : archiveIssueSlice
                    ? (ru ? "Домовой архив. Найди адрес дела №17 о стуке.\nОн под старым листом на левом планшете.\nWASD — ходьба, ЛКМ — снять лист.\n1 — карта, 2 — шпатель, 3 — пар."
                          : "Building records. File 17 is about the knocking.\nPeel the top sheet on the left board to find its shelf.\nWASD to move, LMB to peel.\n1 — Card, 2 — Scraper, 3 — Steam.")
                    : routePrototype ? (ru
                    ? "Подойди к заклеенной двери (WASD).\nНаведи взгляд на лист и удерживай ЛКМ, чтобы снять его.\nПосмотри на тележку вблизи и нажми E для покупки."
                    : "Walk up to the paper-covered door (WASD).\nAim at a notice and hold LMB to peel it.\nTo buy a tool, look at the cart up close and press E.") : T(PeelText.Intro);
                string startLabel=T(PeelText.Start);
                string state=ru ? "Новая смена" : "New shift";
                if(hasSavedShift)
                {
                    var saveEntry=save.InspectIfPathChanged();
                    switch(saveEntry)
                    {
                        case SaveEntry.Continue:
                            startLabel=ru?"Продолжить":"Continue";
                            var saved=save.InspectedSave;
                            if(saved!=null)
                            {
                                string location=pavilion ? (ru ? "Павильон" : "Pavilion") : (ru ? "Выдача" : "Issue counter");
                                if(routePrototype)
                                {
                                    int chapter=routePrototype.AreaAt(saved.position.z);
                                    location=chapter==0 ? (ru ? "Двор" : "Courtyard") : T(chapter==1 ? PeelText.EnterBasement : PeelText.EnterArchive);
                                }
                                state=save.InspectedShiftFinished ? (ru ? "Смена завершена" : "Shift complete")
                                    : (ru ? "Смена сохранена" : "Saved shift");
                                details=$"{location} · ${saved.money}";
                            }
                            break;
                        case SaveEntry.Backup:
                            startLabel=ru?"Загрузить резерв":"Load backup";
                            state=ru ? "Доступна резервная копия" : "Backup available";
                            details=ru?"Основной файл недоступен. Можно продолжить с резервной копии."
                                :"Main save unavailable. You can continue from the backup.";
                            break;
                        case SaveEntry.Unreadable:
                        case SaveEntry.Incompatible:
                            startLabel=ru?"Сбросить сохранение…":"Reset save…";
                            state=saveEntry==SaveEntry.Incompatible
                                ? (ru?"Версия сохранения не поддерживается.":"This save version is unsupported.")
                                : (ru?"Сохранение не читается.":"The save could not be read.");
                            details=ru ? "Для новой смены потребуется подтвердить сброс." : "Confirm a reset to start a new shift.";
                            break;
                    }
                }
                DrawIntro(h,ru,startLabel,state,details);
                FocusMenu();return;
            }
            if(CurrentScreen==PeelScreen.Playing)
            {
                if(guiEvent.type==EventType.Repaint)DrawPlayingHud(w,h);
                return;
            }
            if(CurrentScreen==PeelScreen.Shop)
            {
                bool ru=Settings.Language==PeelLanguage.Russian;
                float resultY=paperTrial ? 332 : hub ? 522 : 427;
                float closeY=resultY+72;
                Panel(new Rect(38,61,514,closeY+58-61));
                GUI.Label(new Rect(58,71,330,42),T(paperCollection?PeelText.PaperShop:PeelText.Tools),title);
                GUI.Label(new Rect(365,77,157,32),"$"+money,shopBalance);
                GUI.Label(new Rect(58,111,474,24),string.Format(T(PeelText.EquippedTool),EquippedTool()),hudPrimary);
                bool scraperOwned=paperTrial ? session.TrialScraperOwned : toolLevel>=3;
                string upgrade=scraperOwned ? T(paperTrial?PeelText.PaperShopScraperOwned:PeelText.MaxUpgrade)
                    : toolLevel==0 ? T(PeelText.Scraper) : (ru ? "Заточка шпателя" : "Sharpen scraper");
                string scraperEffect=paperTrial ? T(PeelText.PaperShopScraperEffect)
                    : toolLevel==0 ? T(PeelText.ShopScraperEffect) : SharpeningPreview();
                if(ShopOffer(140,0,upgrade,scraperEffect,UpgradeCost,owned:scraperOwned))session.TryUpgrade();
                if(paperTrial)DrawCapacityOffer(235,1,ru);
                else
                {
                    if(ShopOffer(235,1,T(PeelText.Steamer),T(PeelText.SteamDescription),session.SteamPrice,toolLevel==0?T(PeelText.SteamLocked):null,steamUnlocked))session.TryBuySteam();
                    string expansion=ru ? "Широкий пар" : "Wider steam";
                    if(ShopOffer(330,2,expansion,T(PeelText.ShopWiderSteamEffect),SteamExpansionCost,!steamUnlocked?T(PeelText.ShopRequiresSteam):null,steamExpanded))session.TryExpandSteam();
                    if(hub)DrawCapacityOffer(425,3,ru);
                }
                string result=ShopPurchaseResult(ru);
                if(result.Length>0)
                {
                    Fill(new Rect(58,resultY,474,62),new Color(.16f,.22f,.18f),7f);
                    GUI.Label(new Rect(70,resultY+8,450,45),result,hudPrimary);
                }
                if(MenuButton(new Rect(58,closeY,474,40),T(PeelText.Close),ShopOptionCount-1))session.Back();
                FocusMenu();
            }
            if(CurrentScreen==PeelScreen.Paused){DrawPause(w,h);FocusMenu();return;}
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
                if(MenuButton(new Rect(x+25,y+160,450,44),ru ? "Отмена" : "Cancel",0,primary:true))session.Back();
                string confirm=review ? (ru?"Завершить осмотр":"End review")
                    : fromIntro ? (ru?"Сбросить сохранение":"Reset save")
                    : (ru?"Начать заново":"Start over");
                if(MenuButton(new Rect(x+25,y+222,450,44),confirm,1))session.RestartShift();
                FocusMenu();
            }
        }

        void DrawPause(float w,float h)
        {
            bool ru=Settings.Language==PeelLanguage.Russian;
            bool reviewing=routePrototype && routePrototype.ReviewMode;
            bool showChapters=routePrototype && !reviewing;
            string saveStatus=PauseSaveStatus();
            const float width=540f, padding=25f, innerWidth=width-2*padding, row=42f, step=52f;
            float height=237f+(hasSavedShift ? step : 0f)+(showChapters ? 50f : 0f)+(saveStatus.Length>0 ? 25f : 0f);
            float x=(w-width)*.5f, y=(h-height)*.5f, left=x+padding;
            Panel(new Rect(x,y,width,height));
            GUI.Label(new Rect(left,y+17,300,43),T(PeelText.Pause),title);
            GUI.Label(new Rect(x+width-190,y+23,165,30),T(PeelText.PlayTime)+" "+FormatPlayTime(),shopMutedBalance);
            float next=y+69f;
            if(saveStatus.Length>0)
            {
                GUI.Label(new Rect(left,next,innerWidth,22),saveStatus,hudLabel);
                next+=25f;
            }
            if(showChapters)
            {
                GUI.Label(new Rect(left,next,innerWidth,20),ru ? "Обязательных листов осталось" : "Required notices left",hudLabel);
                string chapters=$"{(ru ? "Двор" : "Courtyard")} {routePrototype.RemainingInArea(0)} · {T(PeelText.EnterBasement)} {routePrototype.RemainingInArea(1)} · {T(PeelText.EnterArchive)} {routePrototype.RemainingInArea(2)}";
                GUI.Label(new Rect(left,next+20f,innerWidth,23),chapters,hudPrimary);
                next+=50f;
            }
            if(MenuButton(new Rect(left,next,innerWidth,row),T(PeelText.Resume),0,primary:true))session.Back();
            if(MenuButton(new Rect(left,next+step,innerWidth,row),T(PeelText.Settings),1))session.OpenSettings();
            string restart=reviewing ? (ru ? "Завершить осмотр" : "End route review") : T(PeelText.Restart);
            if(MenuButton(new Rect(left,next+2*step,innerWidth,row),restart,2))session.RequestRestart();
            if(hasSavedShift && MenuButton(new Rect(left,next+3*step,innerWidth,row),
                ru ? "В главное меню" : "Main menu",3))session.ReturnToMainMenu();
        }

        void DrawIntro(float h,bool ru,string action,string state,string details)
        {
            bool alternate=HasAlternateStart, canCopy=CanCopyArchive;
            const float x=60f, width=575f, gap=10f, row=42f;
            float footerY=h-69f;
            float secondaryY=footerY-gap-row-(canCopy ? row+gap : 0f);
            float actionY=secondaryY-gap-48f;
            float detailHeight=hudLabel.CalcHeight(new GUIContent(details),width);
            float detailY=actionY-16f-detailHeight;
            float stateY=detailY-25f;
            float titleY=stateY-54f;
            Panel(new Rect(x-22f,titleY-16f,width+44f,h-titleY-22f));
            GUI.Label(new Rect(x,titleY,width,44f),"Just One More Peel",title);
            GUI.Label(new Rect(x,stateY,width,22f),state,hudPrimary);
            GUI.Label(new Rect(x,detailY,width,detailHeight),details,hudLabel);
            if(MenuButton(new Rect(x,actionY,width,48f),action,0,primary:true))session.BeginShift();

            float secondaryWidth=alternate ? (width-gap)*.5f : width;
            if(MenuButton(new Rect(x,secondaryY,secondaryWidth,row),T(PeelText.Settings),1))session.OpenSettings();
            if(alternate)
            {
                string label=routePrototype ? (ru?"Осмотр маршрута":"Review route") : T(paperSave.InspectIfPathChanged() switch
                {
                    SaveEntry.Continue => PeelText.PaperCollectionContinue,
                    SaveEntry.Backup => PeelText.PaperCollectionBackup,
                    SaveEntry.Unreadable or SaveEntry.Incompatible => PeelText.PaperCollectionReset,
                    _ => PeelText.PaperCollection
                });
                if(MenuButton(new Rect(x+secondaryWidth+gap,secondaryY,secondaryWidth,row),label,2))
                {
                    if(routePrototype){routePrototype.OpenForReview();session.BeginShift();}
                    else session.BeginPaperCollection();
                }
            }
            if(canCopy && MenuButton(new Rect(x,secondaryY+row+gap,width,row),T(PeelText.PaperCollectionCopy),3))
                session.CopyArchiveToPaperCollection();
            GUI.Label(new Rect(x,footerY,width,22f),
                ru ? "Tab / ↑↓ — выбор · Enter — открыть" : "Tab / ↑↓ — select · Enter — open",hudLabel);
        }

        void DrawCapacityOffer(float y,int index,bool ru)
        {
            bool max=session.PaperCapacityPrice==0;
            string label=max ? string.Format(T(PeelText.PaperCapacityMax),session.PaperCapacity)
                : (ru ? "Вместимость до " : "Capacity to ")+session.NextPaperCapacity;
            string bundle=string.Format(T(PeelText.PaperCollectionCount),session.CollectedItems,session.PaperCapacity);
            if(ShopOffer(y,index,label,bundle,session.PaperCapacityPrice,owned:max))BuyPaperCapacity();
        }

        void DrawPlayingHud(float w,float h)
        {
            bool ru=Settings.Language==PeelLanguage.Russian;
            bool complete=(pavilion && pavilion.IsLit) || (routePrototype && routePrototype.Completed && !routePrototype.ReviewMode);
            bool showSummary=complete && Time.unscaledTime<session.CompletionSummaryUntil;
            bool review=routePrototype && routePrototype.ReviewMode;
            bool door=routePrototype && routePrototype.UsesLocalCourtyardGate && routePrototype.CurrentArea==0;
            int remaining=routePrototype ? routePrototype.RemainingHere
                : areaExperiment ? areaExperiment.RemainingHere : TotalPosters-ClearedPosters;
            string objective=routePrototype ? routePrototype.Objective : areaExperiment ? areaExperiment.Objective : "";
            if(pavilion)
                objective=complete ? T(PeelText.ShiftComplete) : remaining>0
                    ? (ru ? "Очисти витрину" : "Clear the display")
                    : (ru ? "Включи подсветку слева" : "Turn on the lights at the left end");
            bool bundleFull=paperCollection && session.CollectedItems>=session.PaperCapacity;
            if(paperTrial)
                objective=bundleFull ? T(PeelText.PaperDropOffObjective)
                    : session.CollectedItems>0 ? T(PeelText.PaperDropOffOptional)
                    : (ru ? "Собери бумагу" : "Collect paper");
            else if(hub && bundleFull && remaining>0)
                objective=ru ? "Сдай полную пачку" : "Drop off the full bundle";
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
            if(showSummary)objective=T(PeelText.ShiftComplete);
            float warningY=HudInset;
            if(objective.Length>0)
            {
                string label=showSummary ? ShiftSummaryDetails()
                    : paperTrial ? T(PeelText.PaperCollection)
                    : archiveIssueSlice ? (ru ? "Дело №17" : "File 17")
                    : complete && !pavilion ? T(PeelText.PlayTime)+" "+FormatPlayTime()
                    : review ? (ru ? "Осмотр маршрута" : "Route review")
                    : (door ? (ru ? "На двери" : "On the door") : T(PeelText.RemainingPosters))+"  "+remaining;
                float contentWidth=hudPrimary.CalcSize(new GUIContent(objective)).x;
                if(showSummary)contentWidth=Mathf.Max(contentWidth,hudLabel.CalcSize(new GUIContent(label)).x);
                float width=Mathf.Clamp(contentWidth+2*HudPadding,228f,464f);
                float innerWidth=width-2*HudPadding;
                float height=hudPrimary.CalcHeight(new GUIContent(objective),innerWidth);
                float labelHeight=hudLabel.CalcHeight(new GUIContent(label),innerWidth);
                Rect panel=new Rect(HudInset,HudInset,width,2*HudPadding+height+labelHeight+4f);
                Panel(panel);
                if(showSummary)Fill(new Rect(panel.x+6f,panel.y+HudPadding,3f,height),Amber,1.5f);
                float x=panel.x+HudPadding, y=panel.y+HudPadding;
                GUI.Label(new Rect(x,y,innerWidth,height),objective,hudPrimary);
                GUI.Label(new Rect(x,y+height+4f,innerWidth,labelHeight),label,hudLabel);
                warningY=panel.yMax+8f;
            }

            DrawSaveWarning(warningY,ru);
            if(!pavilion || !complete || (hub && (session.CollectedItems>0 || Time.unscaledTime<payoutUntil)))
                DrawToolStatus(w,ru,bundleFull);
            DrawRouteGuidance(w,h,ru);
            if(!pavilion || !complete)DrawReticle(w,h);
            DrawCabinetDiscovery(w,h,ru);
            if(IsPeeling)return;

            string hint=PlayingHint(ru,complete,door,bundleFull);
            if(hint.Length==0)return;

            var content=new GUIContent(hint);
            float widthWithPadding=Mathf.Min(620f,hudHint.CalcSize(content).x+2*HudPadding);
            float hintHeight=hudHint.CalcHeight(content,widthWithPadding-2*HudPadding);
            float hintX=(w-widthWithPadding)*.5f, hintY=h-HudInset-hintHeight-20f;
            Panel(new Rect(hintX,hintY,widthWithPadding,hintHeight+20f));
            GUI.Label(new Rect(hintX+HudPadding,hintY+10f,widthWithPadding-2*HudPadding,hintHeight),content,hudHint);
        }

        void DrawRouteGuidance(float w,float h,bool ru)
        {
            bool mainRoute=routePrototype && routePrototype.IsMainRoute && !routePrototype.ReviewMode && !routePrototype.Completed;
            if(!mainRoute && (!pavilion || pavilion.IsLit))return;

            string offer=Time.time<purchaseOfferUntil ? purchaseOffer switch
            {
                PeelText.ScraperAvailable when toolLevel==0 && CanUpgrade(money,toolLevel) => string.Format(T(PeelText.ScraperAvailable),UpgradeCost),
                PeelText.SharpenAvailable when toolLevel>0 && CanUpgrade(money,toolLevel) => string.Format(T(PeelText.SharpenAvailable),UpgradeCost),
                PeelText.SteamAvailable when CanBuySteam(money,toolLevel,steamUnlocked,session.SteamPrice) => string.Format(T(PeelText.SteamAvailable),session.SteamPrice),
                PeelText.WiderSteamAvailable when CanExpandSteam(money,steamUnlocked,steamExpanded) => string.Format(T(PeelText.WiderSteamAvailable),SteamExpansionCost),
                _ => ""
            } : "";
            if(offer.Length>0)DrawGuidanceLabel(offer,w*.5f,h*.18f,w*.6f);
            if(!mainRoute || IsPeeling)return;

            if(basementLight && basementLight.IsDark && !basementLight.SwitchIsOn)
                DrawGoalMarker(basementLight.SwitchPosition+Vector3.up*.3f,ru ? "Включи свет" : "Switch on the light",w,h);
            else if(routePrototype.CurrentArea==0)
            {
                if(!routePrototype.CourtyardDoorOpen)
                {
                    string action=routePrototype.RemainingHere>0 ? (ru ? "Сними листы с двери" : "Peel the notices off the door")
                        : (ru ? "Открой дверь" : "Open the door");
                    DrawGoalMarker(routePrototype.CourtyardEntry,action,w,h);
                }
                if(routePrototype.CourtyardCart && (CanUpgrade(money,toolLevel) || CanBuySteam(money,toolLevel,steamUnlocked,session.SteamPrice) ||
                    CanExpandSteam(money,steamUnlocked,steamExpanded)))
                    DrawGoalMarker(routePrototype.CourtyardCart.position+Vector3.up*1.25f,ru ? "Есть покупка" : "Upgrade available",w,h);
            }
        }

        void DrawGoalMarker(Vector3 point,string label,float w,float h)
        {
            Vector3 screen=view.WorldToViewportPoint(point);
            if(screen.z<=0f || screen.x<.08f || screen.x>.92f || screen.y<.15f || screen.y>.84f)return;
            float x=screen.x*w,y=(1f-screen.y)*h;
            DrawGuidanceLabel(label,x,y-15f,w*.6f);
            Fill(new Rect(x-2f,y+3f,4f,9f),Amber,2f);
        }

        void DrawGuidanceLabel(string label,float x,float y,float maxWidth,float opacity=1f)
        {
            var content=new GUIContent(label);
            float width=Mathf.Min(maxWidth,hudPrimary.CalcSize(content).x+2*HudPadding);
            float height=hudPrimary.CalcHeight(content,width-2*HudPadding);
            Rect box=new Rect(x-width*.5f,y-height*.5f-6f,width,height+12f);
            Fill(box,new Color(Ink.r,Ink.g,Ink.b,Ink.a*opacity),9f);
            Fill(new Rect(box.x+4f,box.y+5f,3f,box.height-10f),
                new Color(Amber.r,Amber.g,Amber.b,Amber.a*opacity),1.5f);
            var color=GUI.color;
            GUI.color=new Color(color.r,color.g,color.b,color.a*opacity);
            GUI.Label(new Rect(box.x+HudPadding,box.y+6f,width-2*HudPadding,height),content,hudPrimary);
            GUI.color=color;
        }

        void DrawCabinetDiscovery(float w,float h,bool ru)
        {
            float remaining=discoveryUntil-Time.unscaledTime;
            if(cabinetDiscovery==null || remaining<=0)return;
            string label=discoveriesInFrame==1
                ? (ru ? "В шкафу · " : "Added to the cabinet · ")+cabinetDiscovery.Title(ru)
                : (ru ? "Новых оттисков в шкафу · " : "New prints in the cabinet · ")+discoveriesInFrame;
            DrawGuidanceLabel(label,w*.5f,h*.26f,w*.65f,Mathf.Clamp01(remaining/.35f));
        }

        void DrawSaveWarning(float y,bool ru)
        {
            string message;
            if(save.Failed)
                message=notice==PeelText.PaperDropOffSaveFailed && Time.time<noticeUntil
                    ? T(PeelText.PaperDropOffSaveFailed)
                    : ru ? "Прогресс не записан. Проверь доступ к папке сохранений."
                        : "Progress was not saved. Check access to the save folder.";
            else if(save.CheckpointBackupPending)
                message=ru ? "Резервная копия ещё не обновлена." : "Backup update pending.";
            else if(Time.unscaledTime<save.RecoveryNoticeUntil)
                message=ru ? "Загружен резерв. Последние изменения могли потеряться."
                    : "Backup loaded. Recent progress may have been lost.";
            else return;

            var content=new GUIContent(message);
            float width=Mathf.Min(464f,hudLabel.CalcSize(content).x+2*HudPadding);
            float height=hudLabel.CalcHeight(content,width-2*HudPadding);
            Panel(new Rect(HudInset,y,width,height+20f));
            GUI.Label(new Rect(HudInset+HudPadding,y+10f,width-2*HudPadding,height),content,hudLabel);
        }

        bool CardEquipped => toolLevel==0 || activeTool==PeelGame.CardToolSlot;
        string EquippedTool() => T(activeTool==1 && steamUnlocked ? PeelText.Steamer : CardEquipped ? PeelText.Card : PeelText.Scraper);

        string ShopPurchaseResult(bool ru)
        {
            if(purchaseSaveFailed)
                return ru ? "Покупка отменена. Проверь доступ к папке сохранений."
                    : "Purchase cancelled. Check access to the save folder.";
            string result=shopPurchase switch
            {
                PeelText.Upgraded => ru ? "Шпатель куплен" : "Scraper purchased",
                PeelText.ScraperSharpened => ru ? "Шпатель заточен" : "Scraper sharpened",
                PeelText.SteamBought => ru ? "Отпариватель куплен" : "Steamer purchased",
                PeelText.SteamExpanded => ru ? "Широкий пар куплен" : "Wider steam purchased",
                _ => capacityPurchased ? (ru ? "Вместимость увеличена" : "Capacity upgraded") : ""
            };
            if(!save.Failed && !save.CheckpointBackupPending)return result;
            return result.Length>0 ? result+"\n"+PauseSaveStatus() : PauseSaveStatus();
        }

        string SelectedToolLabel()
        {
            bool steam=activeTool==1 && steamUnlocked;
            string tool=EquippedTool();
            if(activeTool==PeelGame.CardToolSlot)return "3  ·  "+tool;
            if(!steamUnlocked)return tool;
            int key=steam ? (archiveIssueSlice ? 3 : 2) : archiveIssueSlice && toolLevel>0 ? 2 : 1;
            return key+"  ·  "+tool;
        }

        void DrawToolStatus(float w,bool ru,bool bundleFull)
        {
            string label=SelectedToolLabel();
            string balance="$"+money;
            bool payout=Time.unscaledTime<payoutUntil;
            if(payout && !paperCollection)balance+="   +$"+payoutAmount;

            string detail="";
            if(paperCollection)
            {
                detail=string.Format(T(bundleFull ? PeelText.PaperCollectionFull : PeelText.PaperCollectionCount),
                    session.CollectedItems,session.PaperCapacity);
                if(payout)detail+="\n"+string.Format(T(PeelText.PaperDropOffReceipt),paperReceiptItems,payoutAmount);
                else if(hub && toolLevel==0 && CanUpgrade(money,toolLevel))
                    detail+="\n"+(ru ? "Шпатель в приёмной" : "Scraper at reception");
            }
            else if(ShowsFirstScraperOffer)
                detail=CanUpgrade(money,toolLevel) ? (ru ? "Шпатель на тележке" : "Scraper at the cart")
                    : T(PeelText.Scraper)+"  $"+UpgradeCost;
            else if((pavilion || (routePrototype && routePrototype.IsMainRoute)) && toolLevel>0)
                detail=ru ? "1 — Шпатель   3 — Карта" : "1 — Scraper   3 — Card";

            float statusWidth=Mathf.Max(hudLabel.CalcSize(new GUIContent(balance)).x,hudLabel.CalcSize(new GUIContent(detail)).x);
            float contentWidth=Mathf.Max(hudPrimary.CalcSize(new GUIContent(label)).x,statusWidth);
            float width=Mathf.Clamp(contentWidth+2*HudPadding,148f,254f), innerWidth=width-2*HudPadding;
            float x=w-HudInset-width, y=HudInset;
            float labelHeight=hudPrimary.CalcHeight(new GUIContent(label),innerWidth);
            float balanceHeight=hudLabel.CalcHeight(new GUIContent(balance),innerWidth);
            float detailHeight=detail.Length>0 ? hudLabel.CalcHeight(new GUIContent(detail),innerWidth) : 0f;
            float balanceY=y+HudPadding+labelHeight+4f;
            float detailY=balanceY+balanceHeight+4f;
            Panel(new Rect(x,y,width,(detailHeight>0f ? detailY+detailHeight : balanceY+balanceHeight)-y+HudPadding));
            GUI.Label(new Rect(x+HudPadding,y+HudPadding,innerWidth,labelHeight),label,hudPrimary);
            GUI.Label(new Rect(x+HudPadding,balanceY,innerWidth,balanceHeight),balance,hudLabel);
            if(detailHeight==0f)return;
            if(FirstScraperSignalActive)Fill(new Rect(x+6f,detailY+2f,3f,detailHeight-4f),Amber,1.5f);
            GUI.Label(new Rect(x+HudPadding,detailY,innerWidth,detailHeight),detail,hudLabel);
        }

        string PlayingHint(bool ru,bool complete,bool door,bool bundleFull)
        {
            // Match the order of E interactions in PeelGame.Update.
            if(routePrototype && routePrototype.CanReadArchiveNote())return T(PeelText.ReadArchiveNote);
            if(routePrototype && routePrototype.CanOpenDoor())return ru ? "E — Открыть дверь" : "E — Open door";
            if(basementLight && basementLight.CanInteract())
                return basementLight.SwitchIsOn ? (ru ? "E — Выключить свет" : "E — Switch off light")
                    : (ru ? "E — Включить свет" : "E — Switch on light");
            if(pavilion && pavilion.CanTurnOn(view,TotalPosters-ClearedPosters))
                return ru ? "E — Включить подсветку" : "E — Turn on the lights";
            if(session.CanReadHubCabinet())return ru ? "E — Осмотреть шкаф" : "E — Inspect the cabinet";
            if(session.CanDeliverPaper())
            {
                if(paperTrial)return T(session.CollectedItems>0 ? PeelText.PaperDropOffInteract : PeelText.PaperDropOffEmpty);
                if(session.CollectedItems>0)return ru ? "E — Сдать бумагу" : "E — Drop off paper";
                return ru ? "Пачка пуста" : "Bundle empty";
            }
            if(session.CanUseWorkbench())
                return T(PeelText.Workbench);

            if(AwaitingRelease && !bundleFull)return T(PeelText.ReleaseGrip);
            string hint=bundleFull ? "" : TargetHint();
            if(hint.Length>0)
                return door && routePrototype.IsOptionalCourtyardNotice(target.Stack)
                    ? (ru ? "Дополнительно" : "Optional")+" · "+hint : hint;

            if(complete)return "";
            if(!pavilion && !archiveIssueSlice && !routePrototype && ClearedPosters==TotalPosters)return T(PeelText.Finished);
            if(Time.time>=noticeUntil || notice==PeelText.PaperDropOffSaveFailed || FirstScraperSignalActive
                || (notice==PeelText.Welcome && (routePrototype || archiveIssueSlice)))return "";

            return notice switch
            {
                PeelText.ScraperAvailable or PeelText.SharpenAvailable => string.Format(T(notice),UpgradeCost),
                PeelText.SteamAvailable => string.Format(T(notice),session.SteamPrice),
                PeelText.WiderSteamAvailable => string.Format(T(notice),SteamExpansionCost),
                _ => T(notice)
            };
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
            if(MenuButton(new Rect(width*.5f-90f,height-52f,180f,36f),T(PeelText.Back),0,primary:true))session.Back();
        }

        void PrepareCabinet()
        {
            cabinetCards.Clear();
            cabinetFoundCount=0;
            cabinetCard=-1;
            cabinetScroll=Vector2.zero;
            foreach(var card in hub.Collection.Cards)
            {
                if(card.AvailableBays==0)continue;
                cabinetCards.Add(card);
                if(card.FoundBays!=0)cabinetFoundCount++;
            }

            bool ru=Settings.Language==PeelLanguage.Russian;
            var names=System.StringComparer.Create(CultureInfo.GetCultureInfo(ru ? "ru-RU" : "en-US"),true);
            cabinetCards.Sort((a,b)=>
            {
                int comparison=(b.FoundBays!=0).CompareTo(a.FoundBays!=0);
                if(comparison!=0)return comparison;
                comparison=a.Series.CompareTo(b.Series);
                return comparison!=0 ? comparison : names.Compare(a.Title(ru),b.Title(ru));
            });
        }

        void NavigateCabinet(Keyboard keyboard)
        {
            int tab=keyboard.tabKey.wasPressedThisFrame
                ? (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? -1 : 1) : 0;
            int vertical=keyboard.downArrowKey.wasPressedThisFrame ? 1 : keyboard.upArrowKey.wasPressedThisFrame ? -1 : 0;
            int horizontal=keyboard.rightArrowKey.wasPressedThisFrame ? 1 : keyboard.leftArrowKey.wasPressedThisFrame ? -1 : 0;
            NavigateCabinetGrid(tab,vertical,horizontal,keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
        }

        void NavigateCabinetGrid(int tab,int vertical,int horizontal,bool confirm)
        {
            int next=menuSelection;
            int back=cabinetCards.Count;
            if(tab!=0)
                next=(next+back+1+tab)%(back+1);
            else if(vertical>0 && next<back)
            {
                int end=next<cabinetFoundCount ? cabinetFoundCount : back;
                int start=next<cabinetFoundCount ? 0 : cabinetFoundCount;
                int cell=next-start;
                next=cell/CabinetColumns==(end-start-1)/CabinetColumns
                    ? Mathf.Min(end+cell%CabinetColumns,end==back ? back : back-1)
                    : Mathf.Min(next+CabinetColumns,end-1);
            }
            else if(vertical<0 && back>0)
            {
                if(next==back)next=back-1;
                else if(next>=cabinetFoundCount && next<cabinetFoundCount+CabinetColumns)
                    next=cabinetFoundCount==0 ? next : Mathf.Min(cabinetFoundCount-1,
                        (cabinetFoundCount-1)/CabinetColumns*CabinetColumns+next-cabinetFoundCount);
                else if(next>=CabinetColumns)next-=CabinetColumns;
            }
            else if(horizontal<0 && next<back && CabinetColumn(next)!=0)
                next--;
            else if(horizontal>0 && next+1<back && next+1!=cabinetFoundCount && CabinetColumn(next)<CabinetColumns-1)
                next++;

            if(next!=menuSelection)
            {
                menuSelection=next;focusMenu=true;
                if(next<back)
                {
                    float y=CabinetTileY(next);
                    cabinetScroll.y=Mathf.Clamp(cabinetScroll.y,Mathf.Max(0,y+CabinetRow-CabinetViewport),y);
                }
            }
            if(confirm)ActivateCabinetOption(menuSelection);
        }

        void ActivateCabinetOption(int option)
        {
            if(option==cabinetCards.Count)session.Back();
            else
            {
                menuSelection=cabinetCard=option;
                focusMenu=true;
            }
        }

        static string CabinetBayList(int mask)
        {
            string list="";
            for(int i=0;i<4;i++)
                if((mask&(1<<i))!=0)list+=(list.Length==0 ? "" : ", ")+(i+1);
            return list;
        }

        static string CabinetSeriesName(int series,bool ru) => series==0
            ? (ru ? "Двор и соседи" : "Courtyard life")
            : (ru ? "Свободный вечер" : "Free evenings");

        static float CabinetGridHeight(int count) => (count+CabinetColumns-1)/CabinetColumns*CabinetRow;
        float CabinetFoundHeight => cabinetFoundCount==0 ? CabinetHeading : CabinetGridHeight(cabinetFoundCount);
        int CabinetColumn(int index) => (index<cabinetFoundCount ? index : index-cabinetFoundCount)%CabinetColumns;
        float CabinetTileY(int index) => index<cabinetFoundCount
            ? CabinetHeading+index/CabinetColumns*CabinetRow
            : CabinetHeading*2+CabinetFoundHeight+(index-cabinetFoundCount)/CabinetColumns*CabinetRow;

        void DrawHubCabinet(float width,float height)
        {
            bool ru=Settings.Language==PeelLanguage.Russian;
            var panel=new Rect((width-1216f)*.5f,(height-656f)*.5f,1216f,656f);
            Panel(panel);
            float x=panel.x+26, y=panel.y+19;
            GUI.Label(new Rect(x,y,680,46),PeelPavilionHub.CabinetTitle(ru),title);
            float contentHeight=CabinetHeading+CabinetFoundHeight;
            if(cabinetFoundCount<cabinetCards.Count)
                contentHeight+=CabinetHeading+CabinetGridHeight(cabinetCards.Count-cabinetFoundCount);
            cabinetScroll=GUI.BeginScrollView(new Rect(x,y+61,692,CabinetViewport),cabinetScroll,
                new Rect(0,0,674,contentHeight));
            x=1;y=0;
            GUI.Label(new Rect(x,y,610,28),$"{(ru ? "Найдено" : "Found")}  {cabinetFoundCount} / {cabinetCards.Count}",hudPrimary);
            y+=CabinetHeading;
            if(cabinetFoundCount==0)
            {
                GUI.Label(new Rect(x,y,610,30),ru ? "Здесь появятся объявления, снятые целиком." : "Fully removed notices will appear here.",small);
            }
            else y=DrawCabinetGrid(x,y,0,cabinetFoundCount,ru);

            if(cabinetFoundCount<cabinetCards.Count)
            {
                GUI.Label(new Rect(x,CabinetHeading+CabinetFoundHeight,610,27),ru ? "Не найдено" : "Undiscovered",hudPrimary);
                DrawCabinetGrid(x,CabinetHeading*2+CabinetFoundHeight,cabinetFoundCount,cabinetCards.Count,ru);
            }
            GUI.EndScrollView();
            x=panel.x+26;

            var detail=new Rect(panel.x+736,panel.y+80,454,516);
            Fill(detail,new Color(.12f,.17f,.15f),8f);
            if(cabinetCard>=cabinetFoundCount)
            {
                GUI.Label(new Rect(detail.x+18,detail.y+30,detail.width-36,60),
                    ru ? "Объявление пока во дворе" : "Still out in the courtyard",collectionTitle);
                GUI.Label(new Rect(detail.x+18,detail.y+104,detail.width-36,100),
                    ru ? "Снимите этот лист целиком, чтобы добавить его оттиск в шкаф."
                       : "Peel the whole notice to add its print to the cabinet.",text);
            }
            else if(cabinetCard>=0)DrawCabinetCard(detail,cabinetCards[cabinetCard],ru);
            else if(cabinetCards.Count>0)
                GUI.Label(new Rect(detail.x+18,detail.center.y-36,detail.width-36,72),
                    ru ? "Выберите ячейку." : "Select a tile.",center);

            int searchBays=0;
            for(int series=0;series<PeelNoticeCollection.SeriesCount;series++)
                searchBays|=hub.Collection.SeriesProgress(series).searchBays;
            if(searchBays!=0)
                GUI.Label(new Rect(x+187,panel.yMax-50,440,32),
                    (ru ? "Где искать · пролёты " : "Where to look · bays ")+CabinetBayList(searchBays),small);
            if(MenuButton(new Rect(x,panel.yMax-54,164,36),T(PeelText.Back),cabinetCards.Count,primary:true))
                ActivateCabinetOption(cabinetCards.Count);
        }

        float DrawCabinetGrid(float x,float y,int start,int end,bool ru)
        {
            const float cellWidth=128, stepX=134;
            for(int index=start;index<end;index++)
            {
                int cell=index-start;
                var rect=new Rect(x+cell%CabinetColumns*stepX,y+cell/CabinetColumns*CabinetRow,cellWidth,CabinetRow-8);
                if(MenuButton(rect,index>=cabinetFoundCount ? "?" : "",index))ActivateCabinetOption(index);
                if(index==cabinetCard)Fill(new Rect(rect.x+1,rect.yMax-4,rect.width-2,3),Amber,1);
                if(index>=cabinetFoundCount)
                    continue;
                var card=cabinetCards[index];
                DrawCabinetPrint(new Rect(rect.x+5,rect.y+6,rect.width-10,84),card.Design);
                GUI.Label(new Rect(rect.x+4,rect.y+93,rect.width-8,42),card.Title(ru),cabinetTileTitle);
            }
            return y+CabinetGridHeight(end-start);
        }

        void DrawCabinetCard(Rect rect,PeelNoticeCollection.Card card,bool ru)
        {
            float x=rect.x+16, width=rect.width-32;
            GUI.Label(new Rect(x,rect.y+9,width,23),CabinetSeriesName(card.Series,ru),small);
            GUI.Label(new Rect(x,rect.y+33,width,62),card.Title(ru),collectionTitle);
            DrawCabinetPrint(new Rect(x,rect.y+102,width,296),card.Design);
            GUI.Label(new Rect(x,rect.y+410,width,23),
                (ru ? "Найдено · пролёты " : "Found in · bays ")+CabinetBayList(card.FoundBays),small);
            GUI.Label(new Rect(x,rect.y+437,width,70),card.Note(ru),text);
        }

        void DrawCabinetPrint(Rect area,int design)
        {
            float aspect=Settings.PaperAspect(design);
            float width=Mathf.Min(area.width,area.height/aspect), height=width*aspect;
            GUI.DrawTexture(new Rect(area.center.x-width*.5f,area.center.y-height*.5f,width,height),
                Settings.Paper(design).mainTexture);
        }

        string ShiftSummaryDetails()
        {
            int total=routePrototype.RequiredNoticesTotal;
            int optionalLeft=routePrototype.OptionalCourtyardNoticesLeft;
            string required=string.Format(T(PeelText.RequiredNoticesResult),total-routePrototype.RequiredNoticesLeft,total);
            string courtyard=optionalLeft>0 ? string.Format(T(PeelText.OptionalCourtyardResult),optionalLeft) : T(PeelText.CourtyardCleared);
            return required+"\n"+courtyard+"\n"+T(PeelText.PlayTime)+" "+FormatPlayTime();
        }
        string TargetHint()
        {
            if(!target || !target.IsExposed)return "";
            if(CardEquipped)return T(PeelText.HoldCard);

            bool usingSteam=activeTool==1 && steamUnlocked;
            bool precision=target.Stack && target.Stack.PrecisionWork;
            if(usingSteam)
            {
                if(precision)return string.Format(T(PeelText.PrecisionSwitchToScraper),archiveIssueSlice ? 2 : 1);
                return string.Format(T(steamExpanded ? PeelText.HoldExpandedSteam : PeelText.HoldSteam),interaction.PreviewTargetCount);
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
            string controls=T(PeelText.SettingsHelp);
            if(paperTrial)controls=T(PeelText.PaperCollectionControls);
            else if(hub)
                controls=Settings.Language==PeelLanguage.Russian
                    ? "WASD — движение · Мышь — обзор · ЛКМ — снять бумагу\n1 — шпатель · 2 — пар · 3 — карта\nE — действие · Esc — пауза/назад"
                    : "WASD — move · Mouse — look · LMB — peel\n1 — Scraper · 2 — Steam · 3 — Card\nE — interact · Esc — pause/back";
            else if(archiveIssueSlice)
                controls=Settings.Language==PeelLanguage.Russian
                    ? "WASD — движение · Мышь — обзор · ЛКМ — снять лист\n1 — карта · 2 — шпатель · 3 — пар · Esc — пауза/назад\nМеню: Tab / ↑↓ — выбор · ←→ — изменить · Enter — подтвердить"
                    : "WASD — move · Mouse — look · LMB — peel\n1 — Card · 2 — Scraper · 3 — Steam · Esc — pause/back\nMenus: Tab / ↑↓ — navigate · ←→ — adjust · Enter — select";
            const float width=620f, innerWidth=570f, row=56f;
            float helpHeight=hudLabel.CalcHeight(new GUIContent(controls),innerWidth);
            float height=351f+helpHeight;
            float x=(w-width)*.5f,y=(h-height)*.5f, left=x+25f;
            Panel(new Rect(x,y,width,height));
            GUI.Label(new Rect(left,y+17,innerWidth,43),T(PeelText.Settings),title);
            var languageRow=new Rect(left,y+69,innerWidth,row);
            SettingsRow(languageRow,0);
            GUI.Label(new Rect(left+14,y+87,200,23),T(PeelText.Language),hudPrimary);
            LanguageButton(new Rect(left+234,y+79,150,36),"English",PeelLanguage.English);
            LanguageButton(new Rect(left+396,y+79,160,36),"Русский",PeelLanguage.Russian);

            float volume=SettingsSlider(new Rect(left,y+135,innerWidth,row),T(PeelText.Sound),
                Mathf.RoundToInt(Settings.SoundVolume*100)+"%",Settings.SoundVolume,0,1,1);
            if(!Mathf.Approximately(volume,Settings.SoundVolume))Settings.SetSoundVolume(volume);
            float sensitivity=SettingsSlider(new Rect(left,y+201,innerWidth,row),T(PeelText.Sensitivity),
                Settings.LookSensitivity.ToString("0.00")+"×",Settings.LookSensitivity,.25f,2,2);
            if(!Mathf.Approximately(sensitivity,Settings.LookSensitivity))Settings.SetLookSensitivity(sensitivity);

            GUI.Label(new Rect(left,y+271,innerWidth,helpHeight),controls,hudLabel);
            if(MenuButton(new Rect(left,y+helpHeight+285,innerWidth,42),T(PeelText.Back),3,primary:true))session.Back();
        }

        void ChangeLanguage(PeelLanguage language)
        {
            menuSelection=0;
            focusMenu=true;
            Settings.SetLanguage(language);
        }

        void LanguageButton(Rect rect,string label,PeelLanguage language)
        {
            bool selected=Settings.Language==language;
            if(selected)GUI.SetNextControlName("peel-menu-0");
            bool hovered=rect.Contains(Event.current.mousePosition);
            Fill(rect,selected ? Amber : hovered ? new Color(.21f,.28f,.23f) : Ink,6f);
            if(GUI.Button(rect,label,selected ? primaryButton : button))ChangeLanguage(language);
        }

        void SettingsRow(Rect rect,int index)
        {
            if(Event.current.type==EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {menuSelection=index;focusMenu=true;}
            if(menuSelection==index)Fill(rect,Amber,8f);
            Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),new Color(.13f,.17f,.15f),7f);
        }

        float SettingsSlider(Rect rect,string label,string reading,float value,float min,float max,int index)
        {
            SettingsRow(rect,index);
            GUI.Label(new Rect(rect.x+14,rect.y+8,rect.width-110,22),label,hudPrimary);
            GUI.Label(new Rect(rect.xMax-90,rect.y+4,76,26),reading,shopMutedBalance);
            GUI.SetNextControlName("peel-menu-"+index);
            return GUI.HorizontalSlider(new Rect(rect.x+14,rect.y+35,rect.width-28,20),value,min,max);
        }
    }
}
