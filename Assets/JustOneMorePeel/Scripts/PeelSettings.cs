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
        Workbench, Finished, Welcome, Upgraded, SteamBought, Milestone, Secret, SteamLocked,
        Posters, CardHint, ScraperHint, SteamHint, RemainingPosters, PlayTime,
        BuySteamExpansion, SteamExpandedOwned, SteamExpanded, SteamExpandedHint, HoldSteam, HoldExpandedSteam,
        ShopScraperEffect, ShopSharpenEffect, ShopWiderSteamEffect, ShopRequiresSteam, ShopCashShortfall,
        ShopSharpenMaxTime, ScraperSharpened, ScraperAvailable, SharpenAvailable, SteamAvailable, WiderSteamAvailable
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
            {"Start shift · Enter","Начать смену · Enter"},{"Settings","Настройки"},{"Language","Язык"},
            {"Back","Назад"},{"Sound volume","Громкость звука"},{"Mouse sensitivity","Чувствительность мыши"},
            {"Applies to menus, hints and printed notices.\nTab / arrows to navigate. Enter to choose. Esc to go back.","Меняет меню, подсказки и тексты объявлений.\nTab / стрелки — выбор. Enter — применить. Esc — назад."},
            {"Paused","Пауза"},{"Resume","Продолжить"},{"Restart shift","Начать смену заново"},
            {"Tools","Инструменты"},{"Scraper fully upgraded","Шпатель полностью улучшен"},
            {"Scraper — ${0}","Шпатель — ${0}"},{"Sharpen scraper — ${0}","Заточить шпатель — ${0}"},
            {"Steamer owned · key 2","Отпариватель куплен · клавиша 2"},{"Steamer — ${0}","Отпариватель — ${0}"},
            {"Softens glue on up to 3 nearby posters at once.","Размягчает клей. До 3 соседних плакатов за раз."},
            {"Close","Закрыть"},{"Plastic card","Пластиковая карта"},{"Scraper","Шпатель"},{"Steamer","Отпариватель"},
            {"of the wall","стены"},{"1 / 2 — switch tools","1 / 2 — сменить инструмент"},
            {"Hold LMB · one strip at a time","Держи ЛКМ · по одной полосе"},
            {"Hold LMB · move LEFT or RIGHT to peel faster","Держи ЛКМ · двигай ВЛЕВО или ВПРАВО для ускорения"},
            {"Release LMB before the next grip","Отпусти ЛКМ перед следующим захватом"},
            {"E — tools on the cart","E — инструменты на тележке"},
            {"Shift complete. The wall is clear.","Смена завершена. Стена очищена."},
            {"Welcome to your first shift","Добро пожаловать на первую смену"},
            {"Scraper purchased and equipped · hold LMB and move sideways","Шпатель куплен и активен · держи ЛКМ и двигай в стороны"},
            {"Steamer purchased","Отпариватель куплен"},{"Good work. Upgrades are at the cart.","Чистая работа. Улучшения — на тележке."},
            {"OUR SUMMER\nWAS HERE","ЗДЕСЬ БЫЛО\nНАШЕ ЛЕТО"},{"Buy a scraper first","Сначала купи шпатель"},
            {"posters","плакатов"},{"The card peels slowly. A scraper unlocks whole posters.","Карта снимает медленно. Шпатель откроет целые плакаты."},
            {"Whole posters · move LEFT or RIGHT for speed","Целые плакаты · двигай ВЛЕВО или ВПРАВО для ускорения"},
            {"Steam softens glue · up to 3 nearby posters at a time","Пар размягчает клей · до 3 соседних плакатов одновременно"},
            {"Posters left","Осталось плакатов"},{"Time","Время"},
            {"Wider steam — ${0}","Широкий пар — ${0}"},
            {"Wider steam owned · up to 5 posters","Широкий пар куплен · до 5 плакатов"},
            {"Wider steam purchased","Широкий пар куплен"},
            {"Steam softens glue · up to 5 nearby posters at a time","Пар размягчает клей · до 5 соседних плакатов одновременно"},
            {"Hold LMB · steam up to 3 nearby posters","Держи ЛКМ · пар для 3 соседних плакатов"},
            {"Hold LMB · steam up to 5 nearby posters","Держи ЛКМ · пар для 5 соседних плакатов"},
            {"Peel a whole poster per grip instead of one strip.","Снимает целый плакат за захват вместо одной полосы."},
            {"Scraper: {0:0.0} → {1:0.0} s per poster. Steam speeds up too.","Шпатель: {0:0.0} → {1:0.0} с на лист. Пар тоже быстрее."},
            {"Up to 3 → 5 posters · radius 1.5 → 2.5 m","До 3 → 5 листов · радиус 1,5 → 2,5 м"},
            {"Buy a steamer first.","Сначала купи отпариватель."},
            {"Need ${0} more.","Не хватает ${0}."},
            {"Scraper: {0:0.0} s per poster at base speed.","Шпатель: {0:0.0} с на лист при базовой скорости."},
            {"Scraper sharpened · scraper and steam peel faster","Шпатель заточен · шпатель и пар снимают быстрее"},
            {"Scraper available at the cart · ${0}","Шпатель доступен на тележке · ${0}"},
            {"Scraper sharpening available at the cart · ${0}","Заточка шпателя доступна на тележке · ${0}"},
            {"Steamer available at the cart · ${0}","Отпариватель доступен на тележке · ${0}"},
            {"Wider steam available at the cart · ${0}","Широкий пар доступен на тележке · ${0}"}
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
#endif
            PlayerPrefs.SetInt(LanguageKey,(int)Language);
            PlayerPrefs.SetFloat(VolumeKey,SoundVolume);PlayerPrefs.SetFloat(SensitivityKey,LookSensitivity);
            PlayerPrefs.Save();
        }
        void OnApplicationQuit() { Save(); }
    }
}
