using UnityEngine;

namespace JustOneMorePeel
{
    public class PeelWorkbench : MonoBehaviour
    {
        [SerializeField] private TextMesh paperDropOffLabel;
        [SerializeField] private bool singleToolDisplay;
        PeelGame game;
        PeelRoutePrototype route;
        sealed class ToolHighlight
        {
            public GameObject model;
            public LineRenderer ring;
            public bool highlighted, confirming;
            public float purchaseUntil;
        }
        ToolHighlight scraper, steamer;
        Material markerMaterial, purchaseMaterial;
        const float PurchasePulseDuration=.8f, MarkerWidth=.025f;

        void Start()
        {
            if(paperDropOffLabel)return;
            game=FindAnyObjectByType<PeelGame>();
            if(game)route=game.GetComponent<PeelRoutePrototype>();
            markerMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            markerMaterial.SetColor("_BaseColor",new Color(1f,.88f,.48f));
            purchaseMaterial=new Material(markerMaterial);
            purchaseMaterial.SetColor("_BaseColor",new Color(.64f,.95f,.78f));
            scraper=FindTool("Scraper on tray");
            steamer=FindTool("Steamer on tray");
        }

        internal void RefreshPaperDropOffLabel(PeelSettings settings, string label=null)
        {
            if(paperDropOffLabel)paperDropOffLabel.text=label ?? settings.Text(PeelText.PaperDropOffSign);
        }

        ToolHighlight FindTool(string childName)
        {
            var child=transform.Find(childName);
            if(!child)return null;
            var parts=child.GetComponentsInChildren<Renderer>(true);
            if(parts.Length==0)return null;
            var bounds=parts[0].bounds;
            foreach(var part in parts)bounds.Encapsulate(part.bounds);
            var marker=new GameObject("Available tool marker");marker.transform.SetParent(transform,false);
            var ring=marker.AddComponent<LineRenderer>();ring.sharedMaterial=markerMaterial;
            ring.useWorldSpace=false;ring.loop=true;ring.positionCount=48;ring.widthMultiplier=MarkerWidth;
            ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.receiveShadows=false;ring.enabled=false;
            float y=bounds.min.y+.04f;
            for(int i=0;i<ring.positionCount;i++)
            {
                float angle=2f*Mathf.PI*i/ring.positionCount;
                var point=new Vector3(bounds.center.x+Mathf.Cos(angle)*(bounds.extents.x+.08f),y,bounds.center.z+Mathf.Sin(angle)*(bounds.extents.z+.08f));
                ring.SetPosition(i,transform.InverseTransformPoint(point));
            }
            return new ToolHighlight { model=child.gameObject, ring=ring };
        }

        void Update()
        {
            bool playing=game && game.CurrentScreen==PeelScreen.Playing && !(route && route.ReviewMode);
            bool feedbackVisible=game && (game.CurrentScreen==PeelScreen.Playing || game.CurrentScreen==PeelScreen.Shop);
            bool scraperReady=playing && PeelProgressionController.CanUpgrade(game.money,game.toolLevel);
            bool steamReady=game &&
                (PeelProgressionController.CanBuySteam(game.money,game.toolLevel,game.steamUnlocked,game.SteamPrice) ||
                 PeelProgressionController.CanExpandSteam(game.money,game.steamUnlocked,game.steamExpanded));
            if(singleToolDisplay && scraper!=null && steamer!=null)
            {
                bool showSteam=steamer.purchaseUntil>Time.unscaledTime ||
                    (steamReady && scraper.purchaseUntil<=Time.unscaledTime);
                if(steamer.model.activeSelf!=showSteam)steamer.model.SetActive(showSteam);
                if(scraper.model.activeSelf==showSteam)scraper.model.SetActive(!showSteam);
                scraperReady &= !showSteam;
                steamReady &= showSteam;
            }
            SetHighlight(scraper,scraperReady,feedbackVisible);
            SetHighlight(steamer,playing && steamReady,feedbackVisible);
        }

        internal void ShowPurchase(bool steam)
        {
            var other=steam ? scraper : steamer;
            if(singleToolDisplay && other!=null)other.purchaseUntil=0f;
            var tool=steam ? steamer : scraper;
            if(tool!=null)tool.purchaseUntil=Time.unscaledTime+PurchasePulseDuration;
        }

        void SetHighlight(ToolHighlight tool,bool show,bool feedbackVisible)
        {
            if(tool==null || !tool.ring)return;
            float remaining=tool.purchaseUntil-Time.unscaledTime;
            bool confirming=feedbackVisible && remaining>0f;
            bool visible=show || confirming;
            if(tool.highlighted!=visible)
            {
                tool.highlighted=visible;
                tool.ring.enabled=visible;
            }
            if(tool.confirming!=confirming)
            {
                tool.confirming=confirming;
                tool.ring.sharedMaterial=confirming ? purchaseMaterial : markerMaterial;
                tool.ring.widthMultiplier=MarkerWidth;
            }
            if(confirming)
                tool.ring.widthMultiplier=MarkerWidth+.018f*Mathf.Sin(Mathf.Clamp01(remaining/PurchasePulseDuration)*Mathf.PI);
        }
        void OnDestroy()
        {
            if(markerMaterial)Destroy(markerMaterial);
            if(purchaseMaterial)Destroy(purchaseMaterial);
        }
    }
}
