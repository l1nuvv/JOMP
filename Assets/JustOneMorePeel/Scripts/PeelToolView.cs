using UnityEngine;
namespace JustOneMorePeel
{
    public class PeelToolView : MonoBehaviour
    {
        PeelGame game;
        Transform tool;
        Transform hand;
        Transform wrist, forearm;
        Vector3 forearmRestPosition;
        Quaternion forearmRestRotation, wristRestRotation;
        Vector3 gripInModel;
        Quaternion modelGripFrame;
        static readonly string[] DigitNames={"Index","Middle","Ring","Pinky","Thumb"};
        readonly Transform[,] fingers=new Transform[5,4];
        readonly Quaternion[,] fingerRest=new Quaternion[5,3];
        readonly Vector3[,] fingerFlexAxis=new Vector3[5,3];
        readonly Vector3[] fingerSpreadAxis=new Vector3[5];
        int handPose=-1;
        int shownTool=-1;
        float toolDrawStarted=float.NegativeInfinity;
        const float ToolDrawDuration=.18f;
        GameObject card, scraper, steamer;
        ParticleSystem steam, wallSteam;
        Material steamMaterial;
        Texture2D steamTexture;
        float wallSteamBudget;
        float gripBlend;
        Vector3 contactInView;
        bool wasWorkingWithCard;
        float cardContactStarted = float.NegativeInfinity;
        const float CardContactDuration = .09f;
        float detachTime=float.NegativeInfinity, detachStrength;
        const float DetachRise=.035f, DetachSettle=.16f;
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
            CreateHand();
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
            game.PaperDetached+=OnPaperDetached;
        }

        void CreateHand()
        {
            var model = Resources.Load<GameObject>("FPS_Hand");
            if (!model)
            {
                Debug.LogError("PeelToolView: missing FPS_Hand model in Resources.", this);
                return;
            }

            hand = Instantiate(model, tool, false).transform;
            hand.name = "Right hand";
            hand.localScale=Vector3.one*.91f;
            Transform gripSocket = null;
            foreach (var bone in hand.GetComponentsInChildren<Transform>())
            {
                if (bone.name == "Wrist") wrist = bone;
                if (bone.name == "Forearm") forearm = bone;
                if (bone.name == "GripSocket") gripSocket = bone;
                for (int digit = 0; digit < DigitNames.Length; digit++)
                {
                    if(bone.name==DigitNames[digit]+"Tip")fingers[digit,3]=bone;
                    for(int joint=0;joint<3;joint++)
                    {
                        if(bone.name!=DigitNames[digit]+"_0"+(joint+1))continue;
                        fingers[digit,joint]=bone;
                        fingerRest[digit,joint]=bone.localRotation;
                    }
                }
            }

            for (int digit = 0; digit < DigitNames.Length; digit++)
            {
                if(!fingers[digit,0] || !fingers[digit,1] || !fingers[digit,2] || !fingers[digit,3] || !wrist || !forearm || !gripSocket)
                {
                    Debug.LogError("PeelToolView: FPS_Hand skeleton is incomplete.", this);
                    Destroy(hand.gameObject);
                    hand = null;
                    return;
                }
            }

            forearmRestPosition=forearm.localPosition;
            forearmRestRotation=forearm.localRotation;
            wristRestRotation=wrist.localRotation;
            Vector3 forward=(fingers[1,0].position-wrist.position).normalized;
            Vector3 palm=Vector3.ProjectOnPlane(gripSocket.position-wrist.position,forward).normalized;
            Vector3 thumbOpposition=(fingers[1,0].position-fingers[4,0].position).normalized;
            gripInModel=hand.InverseTransformPoint(gripSocket.position);
            modelGripFrame=Quaternion.LookRotation(hand.InverseTransformDirection(forward),
                hand.InverseTransformDirection(palm));
            for(int digit=0;digit<DigitNames.Length;digit++)
            for(int joint=0;joint<3;joint++)
            {
                var bone=fingers[digit,joint];
                Vector3 flexToward=digit==4 ? palm+thumbOpposition*.85f : palm;
                fingerFlexAxis[digit,joint]=bone.InverseTransformDirection(
                    Vector3.Cross(fingers[digit,joint+1].position-bone.position,flexToward).normalized);
                if(joint==0)fingerSpreadAxis[digit]=bone.InverseTransformDirection(palm);
            }
            foreach (var renderer in hand.GetComponentsInChildren<SkinnedMeshRenderer>())
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void SetHandGrip(int pose)
        {
            if (!hand || handPose == pose) return;
            handPose = pose;
            ResetForearm();

            bool pinching=pose==0, steaming=pose==2;
            Vector3 across=pinching ? new Vector3(-1f,1.10f,.18f)
                : steaming ? new Vector3(.91f,.33f,.32f) : new Vector3(-1f,.32f,.16f);
            Vector3 palm=steaming ? new Vector3(.22f,0f,-1f) : new Vector3(-.16f,0f,-1f);
            hand.localRotation=Quaternion.LookRotation(across,palm)*Quaternion.Inverse(modelGripFrame);
            Vector3 grip;
            Vector3 anchor=gripInModel;
            switch(pose)
            {
                case 0:
                    // The thumb and index pads straddle the outer corner of the card.
                    FlexFinger(0,new Vector3(46,21,3),13f);
                    FlexFinger(1,new Vector3(42,53,24));
                    FlexFinger(2,new Vector3(49,60,29));
                    FlexFinger(3,new Vector3(55,64,34));
                    FlexFinger(4,new Vector3(13,8,16));
                    anchor=hand.InverseTransformPoint((fingers[0,3].position+fingers[4,3].position)*.5f);
                    grip=new Vector3(.034f,.010f,0f);
                    break;
                case 1:
                    FlexFinger(0,new Vector3(20,34,26));
                    FlexFinger(1,new Vector3(31,43,30));
                    FlexFinger(2,new Vector3(22,44,32));
                    FlexFinger(3,new Vector3(20,34,24));
                    FlexFinger(4,new Vector3(4,35,44),45f);
                    grip=new Vector3(0f,-.111f,0f);
                    break;
                default:
                    FlexFinger(0,new Vector3(30,43,29));
                    FlexFinger(1,new Vector3(26,42,31));
                    FlexFinger(2,new Vector3(19,31,21),3f);
                    FlexFinger(3,new Vector3(28,75,38),-1f);
                    FlexFinger(4,new Vector3(4,2,44),-15f);
                    grip=new Vector3(0f,-.100f,.085f);
                    break;
            }
            hand.localPosition=grip-hand.localRotation*Vector3.Scale(anchor,hand.localScale);
        }

        void FlexFinger(int digit,Vector3 angles,float spread=0f)
        {
            for(int joint=0;joint<3;joint++)
            {
                Quaternion hinge=Quaternion.AngleAxis(angles[joint],fingerFlexAxis[digit,joint]);
                if(joint==0)hinge=Quaternion.AngleAxis(spread,fingerSpreadAxis[digit])*hinge;
                fingers[digit,joint].localRotation=fingerRest[digit,joint]*hinge;
            }
        }

        void ResetForearm()
        {
            forearm.SetLocalPositionAndRotation(forearmRestPosition,forearmRestRotation);
            wrist.localRotation=wristRestRotation;
        }

        void AlignForearm(bool usingScraper)
        {
            if(!hand)return;
            ResetForearm();
            Vector3 wristPoint=wrist.position;
            Quaternion wristRotation=wrist.rotation;
            // Guide the sleeve back toward the player's elbow while keeping the grip fixed.
            Vector3 elbowInView = usingScraper ? new Vector3(.28f, -.50f, .07f) : new Vector3(.48f, -.40f, .16f);
            Vector3 elbow=transform.TransformPoint(elbowInView);
            Quaternion bend=Quaternion.FromToRotation(wristPoint-forearm.position,wristPoint-elbow);
            forearm.rotation=Quaternion.RotateTowards(Quaternion.identity,bend,usingScraper ? 55f : 24f)*forearm.rotation;
            forearm.position+=wristPoint-wrist.position;
            wrist.rotation=wristRotation;
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
            main.startLifetime=.4f;
            main.startSize=new ParticleSystem.MinMaxCurve(.13f,.23f);
            main.startColor=new Color(.87f,.95f,1f,.38f);
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
            UpdateToolPose();
            if(!wallSteam || !game)return;
            if(!game.IsPeeling || game.activeTool!=1 || !game.steamUnlocked)
            {
                wallSteamBudget=0f;
                return;
            }

            wallSteamBudget+=Time.deltaTime*12f;
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
                    // Steam rises from the upper edge, leaving the working face readable.
                    var position=center+outward*.06f
                        +strip.transform.right*(Random.Range(-1f,1f)*strip.width)
                        +strip.transform.up*(Random.Range(.35f,.48f)*strip.height);
                    wallSteam.Emit(new ParticleSystem.EmitParams
                    {
                        position=position,
                        velocity=Vector3.up*Random.Range(.24f,.4f)+outward*.12f
                    },1);
                }
            }
        }
        void OnPaperDetached(int count)
        {
            bool useSteam=game.activeTool==1 && game.steamUnlocked;
            detachStrength=useSteam ? .32f : game.toolLevel==0 ? .65f : 1f;
            detachStrength*=Mathf.Lerp(1f,1.2f,Mathf.Clamp01((count-1)/7f));
            detachTime=Time.time;
        }

        void UpdateToolPose()
        {
            if(!game||!tool)return;
            bool working=game.IsPeeling;
            tool.gameObject.SetActive(Cursor.lockState==CursorLockMode.Locked);
            if(!tool.gameObject.activeSelf)detachTime=float.NegativeInfinity;
            bool useSteam=game.toolLevel>0 && game.activeTool==1 && game.steamUnlocked;
            bool useCard=game.toolLevel==0;
            if(steamer.activeSelf!=useSteam)steamer.SetActive(useSteam);
            if(scraper.activeSelf!=(!useSteam && !useCard))scraper.SetActive(!useSteam && !useCard);
            if(card.activeSelf!=useCard)card.SetActive(useCard);
            bool workingWithCard = working && useCard;
            if (workingWithCard && !wasWorkingWithCard)
            {
                gripBlend = 1f;
                cardContactStarted = Time.time;
            }
            wasWorkingWithCard = workingWithCard;
            gripBlend=Mathf.MoveTowards(gripBlend,working?1f:0f,Time.deltaTime/.12f);
            float eased=gripBlend*gripBlend*(3f-2f*gripBlend);
            if(working)
                contactInView=transform.InverseTransformPoint(game.ToolContactPoint);
            var rest=useCard?new Vector3(.19f,-.163f,.49f):new Vector3(.30f,-.147f,.56f);
            if(useSteam)
                rest+=new Vector3(-.02f,0f,.04f);
            int selectedTool=useCard ? 0 : useSteam ? 2 : 1;
            if(tool.gameObject.activeSelf && shownTool!=selectedTool)
            {
                shownTool=selectedTool;
                toolDrawStarted=Time.time;
                detachTime=float.NegativeInfinity;
            }
            SetHandGrip(selectedTool);
            float drawOffset=1f-Mathf.SmoothStep(0f,1f,(Time.time-toolDrawStarted)/ToolDrawDuration);
            if (useCard) drawOffset *= 1f - eased;
            float tremble=working?Mathf.Sin(Time.time*19)*.0012f:0f;
            float age=Time.time-detachTime;
            float recoil=age<DetachRise ? Mathf.SmoothStep(0f,1f,age/DetachRise)
                : Mathf.SmoothStep(1f,0f,(age-DetachRise)/DetachSettle);
            recoil*=detachStrength;
            var idleRotation=useSteam
                ? Quaternion.Euler(51f,-5f,26f)*Quaternion.AngleAxis(123f,Vector3.up)
                : Quaternion.Euler(19f,9f,23f);
            var pressRotation=useSteam
                ? Quaternion.Euler(63f,-3f,19f)*Quaternion.AngleAxis(123f,Vector3.up)
                : Quaternion.Euler(useCard ? 36f : 22f,12f,24f);
            if (useCard)
            {
                float contactRoll = Mathf.SmoothStep(20f, 0f, (Time.time - cardContactStarted) / CardContactDuration);
                pressRotation = Quaternion.AngleAxis(contactRoll, Vector3.forward) * pressRotation;
            }
            var position=rest;
            if(eased>0f && contactInView.z>.01f)
            {
                // Register the near-camera working edge with the exact point on the curled sheet.
                Vector3 tip=useCard ? new Vector3(-.040f,.056f,0f)
                    : useSteam ? new Vector3(0f,.212f,-.015f) : new Vector3(0f,.198f,0f);
                float depth=Mathf.Min(useCard ? .41f : useSteam ? .82f : .72f,contactInView.z-.012f);
                var contactPose=contactInView*(depth/contactInView.z)-pressRotation*tip;
                position=Vector3.Lerp(rest,contactPose,eased);
                // Clear the curled edge on the way between contact and rest.
                float clearance = 4f * eased * (1f - eased);
                position += new Vector3(0f, -.04f, -.03f) * clearance;
            }
            position+=new Vector3(0,tremble*eased-.012f*recoil-.105f*drawOffset,-.026f*recoil);
            tool.localPosition=position;
            tool.localRotation=Quaternion.Slerp(idleRotation,pressRotation,eased)
                *Quaternion.Euler(-7f*recoil,0,3f*recoil);
            AlignForearm(!useCard && !useSteam);

            bool spraying=working&&useSteam;
            if(spraying&&!steam.isPlaying)steam.Play();
            if(!spraying&&steam.isPlaying)steam.Stop();
        }
        void OnDestroy()
        {
            if(game)game.PaperDetached-=OnPaperDetached;
            if(steamMaterial)Destroy(steamMaterial);
            if(steamTexture)Destroy(steamTexture);
        }
    }
}
