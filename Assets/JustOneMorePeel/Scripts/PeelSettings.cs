using System;
using UnityEngine;

namespace JustOneMorePeel
{
    public enum PeelLanguage { English, Russian }
    public enum PeelText
    {
        Intro, Start, Settings, Language, Back, Sound, Sensitivity, SettingsHelp, Pause, Resume,
        Restart, Tools, MaxUpgrade, BuyScraper, Upgrade, SteamOwned, BuySteam, SteamDescription,
        Close, Card, Scraper, Steamer, Wall, SwitchTools, HoldCard, DragScraper, ReleaseGrip,
        Workbench, Finished, Welcome, Upgraded, SteamBought, Secret, SteamLocked,
        Posters, CardHint, ScraperHint, SteamHint, RemainingPosters, PlayTime,
        BuySteamExpansion, SteamExpandedOwned, SteamExpanded, SteamExpandedHint, HoldSteam, HoldExpandedSteam,
        ShopScraperEffect, ShopSharpenEffect, ShopWiderSteamEffect, ShopRequiresSteam, ShopCashShortfall,
        ShopSharpenMaxTime, ScraperSharpened, ScraperAvailable, SharpenAvailable, SteamAvailable, WiderSteamAvailable,
        PrecisionScraper, PrecisionSwitchToScraper, ReadArchiveNote, EnterBasement, EnterArchive, EquippedTool,
        PaperCollection, PaperCollectionCount, PaperCollectionFull, PaperDropOffSaveFailed, PaperCollectionControls,
        PaperDropOffSign, PaperDropOffObjective, PaperDropOffOptional, PaperDropOffInteract, PaperDropOffEmpty, PaperDropOffReceipt,
        PaperShop, PaperShopScraperOwned, PaperShopScraperEffect, PaperCapacityOffer, PaperCapacityMax,
        PaperCollectionContinue, PaperCollectionBackup, PaperCollectionReset, PaperCollectionCopy,
        ShiftComplete, RequiredNoticesResult, OptionalCourtyardResult, CourtyardCleared
    }

    public sealed class PeelSettings : MonoBehaviour
    {
        public const string LanguageKey="JustOneMorePeel.Language";
        public const string VolumeKey="JustOneMorePeel.SoundVolume";
        public const string SensitivityKey="JustOneMorePeel.LookSensitivity";
        [SerializeField] private Material[] russianPapers;
        [SerializeField] private Material[] englishPapers;
        [SerializeField] private bool[] opaquePapers;
        [SerializeField] private float[] paperAspects;
        [SerializeField] private TextMesh wallWriting;
        public PeelLanguage Language { get; private set; }
        public float SoundVolume { get; private set; }
        public float LookSensitivity { get; private set; }
        public int PaperCount => russianPapers.Length;
        // Print proportions are independent of GPU power-of-two texture resizing.
        public float PaperAspect(int design) => paperAspects!=null && design<paperAspects.Length ? paperAspects[design] : (float)Paper(design).mainTexture.height/Paper(design).mainTexture.width;
        // Authoring validates both PNG alpha channels before allowing an occluder.
        public bool IsPaperOpaque(int design) => opaquePapers!=null && design<opaquePapers.Length ? opaquePapers[design] : design==1;
        public event Action Changed;

        static readonly string[,] Words = {
            {"Courtyard 17. Start with an old plastic card.\nHold LMB to keep peeling.\nBuy a scraper to remove whole posters.","Двор №17. Начни со старой пластиковой карты.\nДержи ЛКМ, чтобы снимать бумагу.\nКупи шпатель, чтобы снимать плакаты целиком."},
            {"Start shift","Начать смену"},{"Settings","Настройки"},{"Language","Язык"},
            {"Back","Назад"},{"Sound volume","Громкость звука"},{"Mouse sensitivity","Чувствительность мыши"},
            {"Keyboard and mouse\nWASD — move · Mouse — look · Hold LMB — peel\nE — interact · 1/2 or wheel — tools · Esc — pause/back\nMenus: Tab / ↑↓ — navigate · ←→ — adjust · Enter — select",
             "Клавиатура и мышь\nWASD — движение · Мышь — обзор · Держи ЛКМ, чтобы снять лист\nE — действие · 1/2 или колесо — инструмент · Esc — пауза/назад\nМеню: Tab / ↑↓ — выбор · ←→ — изменить · Enter — подтвердить"},
            {"Paused","Пауза"},{"Resume","Продолжить"},{"Restart shift","Начать смену заново"},
            {"Tools","Инструменты"},{"Scraper fully upgraded","Шпатель полностью улучшен"},
            {"Scraper — ${0}","Шпатель — ${0}"},{"Sharpen scraper — ${0}","Заточить шпатель — ${0}"},
            {"Steamer owned · key 2","Отпариватель куплен · клавиша 2"},{"Steamer — ${0}","Отпариватель — ${0}"},
            {"Up to 3 posters. Clockwise motion speeds up steam.","До 3 листов. Круг по часовой ускоряет пар."},
            {"Close","Закрыть"},{"Plastic card","Пластиковая карта"},{"Scraper","Шпатель"},{"Steamer","Отпариватель"},
            {"of the wall","стены"},{"1/2 or wheel — switch tools","1/2 или колесо — сменить инструмент"},
            {"Hold LMB to peel a strip","Держи ЛКМ, чтобы снять полосу"},
            {"Hold LMB · drag sideways to speed up","Держи ЛКМ · веди вбок для ускорения"},
            {"Release LMB to grip again","Отпусти ЛКМ для нового захвата"},
            {"E — open the tool shop","E — открыть магазин инструментов"},
            {"Shift complete. The wall is clear.","Смена завершена. Стена очищена."},
            {"Welcome to your first shift","Добро пожаловать на первую смену"},
            {"Scraper purchased and equipped · hold LMB and move sideways","Шпатель куплен и активен · держи ЛКМ и двигай в стороны"},
            {"Steamer equipped · hold LMB and move clockwise to speed up","Отпариватель в руке · держи ЛКМ и веди мышь по часовой стрелке для ускорения"},
            {"OUR SUMMER\nWAS HERE","ЗДЕСЬ БЫЛО\nНАШЕ ЛЕТО"},{"Buy a scraper first","Сначала купи шпатель"},
            {"posters","плакатов"},{"The card peels slowly. A scraper unlocks whole posters.","Карта снимает медленно. Шпатель откроет целые плакаты."},
            {"Whole posters · move LEFT or RIGHT for speed","Целые плакаты · двигай ВЛЕВО или ВПРАВО для ускорения"},
            {"Steam softens glue · up to 3 nearby posters at a time","Пар размягчает клей · до 3 соседних плакатов одновременно"},
            {"Posters left","Осталось плакатов"},{"Time","Время"},
            {"Wider steam — ${0}","Широкий пар — ${0}"},
            {"Wider steam owned · up to 5 posters","Широкий пар куплен · до 5 плакатов"},
            {"Wider steam purchased","Широкий пар куплен"},
            {"Steam softens glue · up to 5 nearby posters at a time","Пар размягчает клей · до 5 соседних плакатов одновременно"},
            {"Steam: {0} · hold LMB · clockwise circles speed up","Пар: {0} · держи ЛКМ · круг по часовой ускоряет отрыв"},
            {"Wide steam: {0} · hold LMB · clockwise circles speed up","Широкий пар: {0} · держи ЛКМ · круг по часовой ускоряет отрыв"},
            {"Peel a whole poster per grip instead of one strip.","Снимает целый плакат за захват вместо одной полосы."},
            {"Scraper: {0:0.0} → {1:0.0} s per poster at base speed.","Шпатель: {0:0.0} → {1:0.0} с на лист при базовой скорости."},
            {"Up to 3 → 5 posters · radius 1.5 → 2.5 m","До 3 → 5 листов · радиус 1,5 → 2,5 м"},
            {"Buy a steamer first.","Сначала купи отпариватель."},
            {"Need ${0} more.","Не хватает ${0}."},
            {"Scraper: {0:0.0} s per poster at base speed.","Шпатель: {0:0.0} с на лист при базовой скорости."},
            {"Scraper sharpened · removes posters faster","Шпатель заточен · снимает листы быстрее"},
            {"Scraper available at the cart · ${0}","Шпатель доступен на тележке · ${0}"},
            {"Scraper sharpening available at the cart · ${0}","Заточка шпателя доступна на тележке · ${0}"},
            {"Steamer available at the cart · ${0}","Отпариватель доступен на тележке · ${0}"},
            {"Wider steam available at the cart · ${0}","Широкий пар доступен на тележке · ${0}"},
            {"Tough paper · hold LMB and drag sideways to peel faster","Плотная бумага · держи ЛКМ и веди мышь вбок для ускорения"},
            {"Tough paper · {0} — switch to scraper","Плотная бумага · {0} — сменить пар на шпатель"},
            {"E — read the note","E — прочитать записку"},
            {"Basement","Подвал"},{"Archive","Архив"},
            {"Equipped — {0}","В руке — {0}"},
            {"Paper collection trial","Проба сбора бумаги"},
            {"Paper bundle · {0}/{1}","Пачка · {0}/{1}"},
            {"Bundle full · {0}/{1}","Пачка полна · {0}/{1}"},
            {"Could not save the drop-off. Your paper is still in the bundle.","Сдача не записана. Бумага осталась в пачке."},
            {"WASD — move · Mouse — look · Hold LMB to peel\n1 — Card · 2/3 — Scraper/Steam after purchase\nE — drop off paper · F — shop · Esc — pause/back\nMenus: Tab / ↑↓ — navigate · ←→ — adjust · Enter — select",
             "WASD — движение · Мышь — обзор · ЛКМ — снять бумагу\n1 — карта · 2/3 — шпатель/пар после покупки\nE — сдача · F — магазин · Esc — пауза/назад\nМеню: Tab / ↑↓ — выбор · ←→ — изменить · Enter — подтвердить"},
            {"PAPER DROP-OFF","МАКУЛАТУРА"},
            {"Drop off the bundle at the cart","Сдай пачку у тележки"},
            {"Collect more paper or drop off at the cart","Продолжай сбор или сдай пачку у тележки"},
            {"E — Drop off paper · F — Shop","E — Сдать бумагу · F — Магазин"},
            {"Bundle empty · F — Shop","Пачка пуста · F — Магазин"},
            {"Items accepted · {0} · +${1}","Сдано {0} шт. · +${1}"},
            {"Cart shop","Магазин"},
            {"Scraper owned","Шпатель куплен"},
            {"Whole sheets · Steam included","Целые листы · пар в комплекте"},
            {"Bundle capacity {0} · ${1}","Вместимость {0} · ${1}"},
            {"Bundle capacity {0} · maximum","Вместимость {0} · максимум"},
            {"Continue trial","Продолжить пробу"},
            {"Load trial backup","Проба из резерва"},
            {"Reset trial save…","Сбросить пробу…"},
            {"Copy archive shift into trial","Скопировать архивную смену в пробу"},
            {"Shift complete","Смена завершена"},
            {"Required notices cleared · {0}/{1}","Снято обязательных листов · {0}/{1}"},
            {"Optional notices left in the courtyard · {0}","Необязательные листы во дворе · осталось {0}"},
            {"Courtyard fully cleared","Двор полностью очищен"}
        };

        void Awake() { Load(); }
        public void Load()
        {
            Language=(PeelLanguage)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey,0),0,1);
            SoundVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey,.7f));
            LookSensitivity=Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey,1),.25f,2);
        }
        public string Text(PeelText key) { return Words[(int)key,(int)Language]; }
        public Material Paper(int design)
        {
            var materials=Language==PeelLanguage.English ? englishPapers : russianPapers;
            if(materials==null || design<0 || design>=materials.Length || !materials[design])
                throw new InvalidOperationException("Missing localized paper material: "+Language+" / "+design);
            return materials[design];
        }
        internal string PaperVariantId(int design)
        {
            var texture = russianPapers[design].mainTexture;
            return texture ? texture.name : null;
        }
        internal string PaperSaveId(int design) => russianPapers[design].name;
        public void SetLanguage(PeelLanguage language)
        {
            if(language!=PeelLanguage.English && language!=PeelLanguage.Russian)return;
            Language=language;ApplyWorldText();Changed?.Invoke();Save();
        }
        public void SetSoundVolume(float value) { value=Mathf.Clamp01(value);if(Mathf.Approximately(value,SoundVolume))return;SoundVolume=value;Changed?.Invoke(); }
        public void SetLookSensitivity(float value) { value=Mathf.Clamp(value,.25f,2);if(Mathf.Approximately(value,LookSensitivity))return;LookSensitivity=value;Changed?.Invoke(); }
        public void ApplyWorldText() { if(wallWriting)wallWriting.text=Text(PeelText.Secret); }
        public void Save()
        {
#if UNITY_EDITOR
            if(!string.IsNullOrEmpty(PeelGame.WorldSavePathOverride))return;
            if(TryGetComponent<PeelGame>(out var game) && game.ArchiveIssueSlice)return;
#endif
            PlayerPrefs.SetInt(LanguageKey,(int)Language);
            PlayerPrefs.SetFloat(VolumeKey,SoundVolume);PlayerPrefs.SetFloat(SensitivityKey,LookSensitivity);
            PlayerPrefs.Save();
        }
        void OnApplicationQuit() { Save(); }
    }
}
