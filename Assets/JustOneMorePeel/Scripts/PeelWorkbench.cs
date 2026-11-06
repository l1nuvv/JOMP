using UnityEngine;

namespace JustOneMorePeel
{
    public class PeelWorkbench : MonoBehaviour
    {
        PeelGame game;
        PeelRoutePrototype route;
        sealed class ToolHighlight
        {
            public Renderer[] parts;
            public MaterialPropertyBlock[] originalProperties;
            public bool highlighted;
        }
        ToolHighlight scraper, steamer;
        MaterialPropertyBlock highlight;
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor");

        void Start()
        {
            game=FindAnyObjectByType<PeelGame>();
            if(game)route=game.GetComponent<PeelRoutePrototype>();
            scraper=FindTool("Scraper on tray");
            steamer=FindTool("Steamer on tray");
            highlight=new MaterialPropertyBlock();
        }

        ToolHighlight FindTool(string childName)
        {
            var child=transform.Find(childName);
            if(!child)return null;
            var result=new ToolHighlight { parts=child.GetComponentsInChildren<Renderer>() };
            result.originalProperties=new MaterialPropertyBlock[result.parts.Length];
            for(int i=0;i<result.parts.Length;i++)
            {
                result.originalProperties[i]=new MaterialPropertyBlock();
                result.parts[i].GetPropertyBlock(result.originalProperties[i]);
            }
            return result;
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
            for(int i=0;i<tool.parts.Length;i++)
            {
                var part=tool.parts[i];
                if(!part)continue;
                if(!show){part.SetPropertyBlock(tool.originalProperties[i]);continue;}
                part.GetPropertyBlock(highlight);
                var material=part.sharedMaterial;
                if(material && material.HasProperty(BaseColor))
                    highlight.SetColor(BaseColor,Color.Lerp(material.GetColor(BaseColor),new Color(1f,.77f,.3f),.65f));
                part.SetPropertyBlock(highlight);
            }
        }
    }
}
