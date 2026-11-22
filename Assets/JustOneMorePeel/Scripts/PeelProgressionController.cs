using System;

namespace JustOneMorePeel
{
    internal sealed class PeelProgressionController
    {
        internal const int SteamCost=480;
        internal const int SteamExpansionCost=800;
        internal const int StripValue=2;
        internal int PaperCapacity { get; private set; }=5;
        internal int NextPaperCapacity => Math.Min(PaperCapacity+5,20);
        internal int PaperCapacityPrice => PaperCapacity switch { 5=>10, 10=>20, 15=>30, _=>0 };
        internal int CollectedItems { get; private set; }
        internal int FreePaperSlots => PaperCapacity-CollectedItems;
        int collectedPaperValue;
        internal static int UpgradeCost(int toolLevel) => toolLevel==0?25:toolLevel==1?160:640;
        internal static bool CanUpgrade(int money,int toolLevel) => toolLevel<3 && money>=UpgradeCost(toolLevel);
        internal static bool CanBuySteam(int money,int toolLevel,bool steamUnlocked) => toolLevel>0 && !steamUnlocked && money>=SteamCost;
        internal static bool CanExpandSteam(int money,bool steamUnlocked,bool steamExpanded) => steamUnlocked && !steamExpanded && money>=SteamExpansionCost;
        internal static bool IsPurchaseNotice(PeelText notice) => notice is
            PeelText.Upgraded or PeelText.ScraperSharpened or PeelText.SteamBought or PeelText.SteamExpanded;
        readonly Action Release;
        readonly Func<bool> SavePurchase;
        readonly Action<PeelText> Notify;
        readonly Action ShowPayout;
#if UNITY_EDITOR
        readonly Action<string> LogRoutePlaytest;
#endif

        internal PeelProgressionController(Action release,Func<bool> savePurchase,Action<PeelText> notify,Action showPayout
#if UNITY_EDITOR
            , Action<string> logRoutePlaytest
#endif
            )
        {
            Release=release;SavePurchase=savePurchase;Notify=notify;ShowPayout=showPayout;
#if UNITY_EDITOR
            LogRoutePlaytest=logRoutePlaytest;
#endif
        }

        internal bool TryUpgrade(ref int money,ref int toolLevel)
        {
            if(!CanUpgrade(money,toolLevel))return false;
            int cost=UpgradeCost(toolLevel);
            Release();money-=cost;toolLevel++;
            if(!SavePurchase()){money+=cost;toolLevel--;return false;}
#if UNITY_EDITOR
            LogRoutePlaytest(toolLevel == 1 ? "Scraper purchased" : $"Scraper sharpened to level {toolLevel}");
#endif
            Notify(toolLevel==1?PeelText.Upgraded:PeelText.ScraperSharpened);return true;
        }
        internal bool TryBuySteam(ref int money,int toolLevel,ref int activeTool,ref bool steamUnlocked)
        {
            if(!CanBuySteam(money,toolLevel,steamUnlocked))return false;
            int previousTool=activeTool;
            Release();money-=SteamCost;steamUnlocked=true;activeTool=1;
            if(!SavePurchase()){money+=SteamCost;steamUnlocked=false;activeTool=previousTool;return false;}
            Notify(PeelText.SteamBought);
#if UNITY_EDITOR
            LogRoutePlaytest("Steam purchased");
#endif
            return true;
        }
        internal bool TryExpandSteam(ref int money,bool steamUnlocked,ref bool steamExpanded)
        {
            if(!CanExpandSteam(money,steamUnlocked,steamExpanded))return false;
            Release();money-=SteamExpansionCost;steamExpanded=true;
            if(!SavePurchase()){money+=SteamExpansionCost;steamExpanded=false;return false;}
            Notify(PeelText.SteamExpanded);
#if UNITY_EDITOR
            LogRoutePlaytest("Wide steam purchased");
#endif
            return true;
        }

        internal void RecordStripDetached(ref int money,ref int removed,int total,int toolLevel,bool steamUnlocked,bool steamExpanded,
            bool collectPaper,bool partOfSheet)
        {
            removed++;
            if(collectPaper)
            {
                if(!partOfSheet)CollectPaper(1);
                return;
            }

            int previousMoney=money;
            money+=StripValue;
            ShowPayout();
            if(removed==total)Notify(PeelText.Finished);
            else if(CanUpgrade(money,toolLevel) && !CanUpgrade(previousMoney,toolLevel))Notify(toolLevel==0?PeelText.ScraperAvailable:PeelText.SharpenAvailable);
            else if(CanBuySteam(money,toolLevel,steamUnlocked) && !CanBuySteam(previousMoney,toolLevel,steamUnlocked))Notify(PeelText.SteamAvailable);
            else if(CanExpandSteam(money,steamUnlocked,steamExpanded) && !CanExpandSteam(previousMoney,steamUnlocked,steamExpanded))Notify(PeelText.WiderSteamAvailable);
        }

        internal void CollectPaper(int stripCount)
        {
            CollectedItems++;
            collectedPaperValue+=stripCount*StripValue;
        }

        internal bool TryUpgradePaperCapacity(ref int money)
        {
            int price=PaperCapacityPrice;
            if(price==0 || money<price)return false;
            money-=price;
            PaperCapacity=NextPaperCapacity;
            return true;
        }

        internal (int Items,int Payout) DeliverPaper(ref int money)
        {
            int items=CollectedItems;
            int payout=collectedPaperValue;
            CollectedItems=0;
            collectedPaperValue=0;
            money+=payout;
            return (items,payout);
        }
    }
}
