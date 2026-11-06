using UnityEngine;
namespace JustOneMorePeel
{
    public class PeelToolView : MonoBehaviour
    {
        PeelGame game;
        Transform tool;
        GameObject card, scraper, steamer;
        ParticleSystem steam, wallSteam;
        Material steamMaterial;
        Texture2D steamTexture;
        float wallSteamBudget;
        float gripBlend, gripPull;
        public void Initialize(PeelGame owner, GameObject cardPrefab, GameObject scraperPrefab, GameObject steamerPrefab)
        {
            game=owner;
            transform.SetParent(game.view.transform,false);
            if (!cardPrefab || !scraperPrefab || !steamerPrefab)
            {
                Debug.LogError("PeelToolView: assign card, scraper and steamer prefabs on PeelGame.", this);
                enabled = false;
                return;
            }
            tool=new GameObject("Held tool pose").transform;tool.SetParent(transform,false);
            card=Instantiate(cardPrefab,tool,false);
            scraper=Instantiate(scraperPrefab,tool,false);
            steamer=Instantiate(steamerPrefab,tool,false);steamer.SetActive(false);
            var emitter=new GameObject("Steam nozzle");emitter.transform.SetParent(tool,false);
            emitter.transform.localPosition=new Vector3(0,.215f,-.015f);
            emitter.transform.localRotation=Quaternion.Euler(-90,0,0);
            steam=emitter.AddComponent<ParticleSystem>();
            var main=steam.main;main.startLifetime=.35f;main.startSpeed=.6f;main.startSize=.055f;
            main.startColor=new Color(.85f,.94f,1,.2f);main.maxParticles=40;main.playOnAwake=false;
            var emission=steam.emission;emission.rateOverTime=35;
            var shape=steam.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=14;shape.radius=.02f;
            steamMaterial=new Material(Resources.Load<Shader>("PeelSteam"));
            ConfigureSteamRenderer(steam);
            steam.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            CreateWallSteam();
        }

        void CreateWallSteam()
        {
            const int size=32;
            steamTexture=new Texture2D(size,size,TextureFormat.RGBA32,false)
            { name="Soft steam particle",wrapMode=TextureWrapMode.Clamp };
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)
            for(int x=0;x<size;x++)
            {
                float u=(x+.5f)*2f/size-1f,v=(y+.5f)*2f/size-1f;
                float alpha=Mathf.Clamp01(1f-u*u-v*v);
                pixels[y*size+x]=new Color32(255,255,255,(byte)(255f*alpha*alpha));
            }
            steamTexture.SetPixels32(pixels);
            steamTexture.Apply(false,true);
            steamMaterial.SetTexture("_BaseMap",steamTexture);

            var cloud=new GameObject("Steam at poster surface");
            cloud.transform.SetParent(transform,false);
            wallSteam=cloud.AddComponent<ParticleSystem>();
            var main=wallSteam.main;
            main.playOnAwake=false;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startLifetime=.48f;
            main.startSize=new ParticleSystem.MinMaxCurve(.19f,.31f);
            main.startColor=new Color(.87f,.95f,1f,.58f);
            main.maxParticles=96;
            var emission=wallSteam.emission;emission.enabled=false;
            var shape=wallSteam.shape;shape.enabled=false;
            var color=wallSteam.colorOverLifetime;color.enabled=true;
            var fade=new Gradient();
            fade.SetKeys(new[]{new GradientColorKey(Color.white,0f),new GradientColorKey(Color.white,1f)},
                new[]{new GradientAlphaKey(.6f,0f),new GradientAlphaKey(1f,.2f),new GradientAlphaKey(0f,1f)});
            color.color=fade;
            var growth=wallSteam.sizeOverLifetime;growth.enabled=true;
            growth.size=new ParticleSystem.MinMaxCurve(1f,AnimationCurve.Linear(0f,.65f,1f,1f));
            ConfigureSteamRenderer(wallSteam);
            wallSteam.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            wallSteam.Play();
        }

        void ConfigureSteamRenderer(ParticleSystem particles)
        {
            var renderer=particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial=steamMaterial;
            renderer.SetActiveVertexStreams(new System.Collections.Generic.List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Color,
                ParticleSystemVertexStream.UV
            });
        }

        void LateUpdate()
        {
            if(!wallSteam || !game)return;
            if(!game.IsPeeling || game.activeTool!=1 || !game.steamUnlocked)
            {
                wallSteamBudget=0f;
                return;
            }

            wallSteamBudget+=Time.deltaTime*18f;
            int count=Mathf.Min(2,(int)wallSteamBudget);
            if(count==0)return;
            wallSteamBudget-=count;
            foreach(var stack in game.WorkingStacks)
            {
                var strips=stack.CurrentStrips;
                var strip=strips[0];
                var center=(strip.transform.position+strips[strips.Length-1].transform.position)*.5f;
                var outward=-strip.transform.forward;
                for(int i=0;i<count;i++)
                {
                    var position=center+outward*.06f
                        +strip.transform.right*(Random.Range(-1f,1f)*strip.width)
                        +strip.transform.up*(Random.Range(-.25f,.2f)*strip.height);
                    wallSteam.Emit(new ParticleSystem.EmitParams
                    {
                        position=position,
                        velocity=Vector3.up*Random.Range(.24f,.4f)+outward*.12f
                    },1);
                }
            }
        }
        void Update()
        {
            if(!game||!tool)return;
            bool working=game.IsPeeling;
            tool.gameObject.SetActive(Cursor.lockState==CursorLockMode.Locked);
            bool useSteam=game.toolLevel>0 && game.activeTool==1 && game.steamUnlocked;
            bool useCard=game.toolLevel==0;
            if(steamer.activeSelf!=useSteam)steamer.SetActive(useSteam);
            if(scraper.activeSelf!=(!useSteam && !useCard))scraper.SetActive(!useSteam && !useCard);
            if(card.activeSelf!=useCard)card.SetActive(useCard);
            gripBlend=Mathf.MoveTowards(gripBlend,working?1f:0f,Time.deltaTime/.12f);
            float eased=gripBlend*gripBlend*(3f-2f*gripBlend);
            if(working)gripPull=game.GripProgress;
            var rest=useCard?new Vector3(.19f,-.13f,.31f):new Vector3(.32f,-.28f,.52f);
            float tremble=working?Mathf.Sin(Time.time*19)*.004f:0f;
            tool.localPosition=rest+new Vector3(0,(-.025f*gripPull+tremble)*eased,.018f*eased);
            tool.localRotation=Quaternion.Euler(Mathf.Lerp(-22f,-12f,eased),-15,-12);
            bool spraying=working&&useSteam;
            if(spraying&&!steam.isPlaying)steam.Play();
            if(!spraying&&steam.isPlaying)steam.Stop();
        }
        void OnDestroy()
        {
            if(steamMaterial)Destroy(steamMaterial);
            if(steamTexture)Destroy(steamTexture);
        }
    }
}
