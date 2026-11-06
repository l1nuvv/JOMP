using UnityEngine;

namespace JustOneMorePeel
{
    public class PeelWorkbench : MonoBehaviour
    {
        PeelGame game;
        PeelRoutePrototype route;
        sealed class ToolHighlight
        {
            public LineRenderer ring;
            public bool highlighted;
        }
        ToolHighlight scraper, steamer;
        Material markerMaterial;

        void Start()
        {
            game=FindAnyObjectByType<PeelGame>();
            if(game)route=game.GetComponent<PeelRoutePrototype>();
            markerMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            markerMaterial.SetColor("_BaseColor",new Color(1f,.88f,.48f));
            scraper=FindTool("Scraper on tray");
            steamer=FindTool("Steamer on tray");
        }

        ToolHighlight FindTool(string childName)
        {
            var child=transform.Find(childName);
            if(!child)return null;
            var parts=child.GetComponentsInChildren<Renderer>();
            if(parts.Length==0)return null;
            var bounds=parts[0].bounds;
            foreach(var part in parts)bounds.Encapsulate(part.bounds);
            var marker=new GameObject("Available tool marker");marker.transform.SetParent(transform,false);
            var ring=marker.AddComponent<LineRenderer>();ring.sharedMaterial=markerMaterial;
            ring.useWorldSpace=false;ring.loop=true;ring.positionCount=48;ring.widthMultiplier=.025f;
            ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.receiveShadows=false;ring.enabled=false;
            float y=bounds.min.y+.04f;
            for(int i=0;i<ring.positionCount;i++)
            {
                float angle=2f*Mathf.PI*i/ring.positionCount;
                var point=new Vector3(bounds.center.x+Mathf.Cos(angle)*(bounds.extents.x+.08f),y,bounds.center.z+Mathf.Sin(angle)*(bounds.extents.z+.08f));
                ring.SetPosition(i,transform.InverseTransformPoint(point));
            }
            return new ToolHighlight { ring=ring };
        }

        void Update()
        {
            bool playing=game && game.CurrentScreen==PeelScreen.Playing && !(route && route.ReviewMode);
            bool scraperReady=playing && game.toolLevel<3 && game.money>=game.UpgradeCost;
            bool steamReady=playing && game.toolLevel>0 &&
                ((!game.steamUnlocked && game.money>=PeelGame.SteamCost) ||
                 (game.steamUnlocked && !game.steamExpanded && game.money>=PeelGame.SteamExpansionCost));
            SetHighlight(scraper,scraperReady);
            SetHighlight(steamer,steamReady);
        }

        void SetHighlight(ToolHighlight tool,bool show)
        {
            if(tool==null || tool.highlighted==show)return;
            tool.highlighted=show;
            if(tool.ring)tool.ring.enabled=show;
        }
        void OnDestroy(){if(markerMaterial)Destroy(markerMaterial);}
    }
}
