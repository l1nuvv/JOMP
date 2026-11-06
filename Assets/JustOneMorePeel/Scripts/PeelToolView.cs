using UnityEngine;
namespace JustOneMorePeel
{
    public class PeelToolView : MonoBehaviour
    {
        PeelGame game;
        Transform tool;
        Transform hand;
        Vector3 wristInModel;
        Quaternion modelGripFrame;
        static readonly string[] DigitNames={"Index","Middle","Ring","Pinky","Thumb"};
        readonly Transform[,] fingers=new Transform[5,4];
        readonly Quaternion[,] fingerRest=new Quaternion[5,3];
        int handPose=-1;
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
            hand.localScale = Vector3.one * .86f;
            Transform wrist = null;
            foreach (var bone in hand.GetComponentsInChildren<Transform>())
            {
                if (bone.name == "Wrist") wrist = bone;
                for (int digit = 0; digit < DigitNames.Length; digit++)
                for (int joint = 0; joint < 3; joint++)
                {
                    if (bone.name != DigitNames[digit] + "_0" + (joint + 1)) continue;
                    fingers[digit, joint] = bone;
                    fingerRest[digit, joint] = bone.localRotation;
                }
            }

            for (int digit = 0; digit < DigitNames.Length; digit++)
            {
                var middle = fingers[digit, 1];
                var distal = fingers[digit, 2];
                if (!fingers[digit, 0] || !middle || !distal || !wrist)
                {
                    Debug.LogError("PeelToolView: FPS_Hand skeleton is incomplete.", this);
                    Destroy(hand.gameObject);
                    hand = null;
                    return;
                }
                var tip = new GameObject("Grip fingertip").transform;
                tip.SetParent(distal, false);
                float length = Vector3.Distance(middle.position, distal.position);
                tip.position = distal.position + distal.up * length * (digit == 4 ? .96f : .80f);
                fingers[digit, 3] = tip;
            }

            wristInModel = hand.InverseTransformPoint(wrist.position);
            Vector3 forward = hand.InverseTransformVector(fingers[1, 0].position - wrist.position);
            Vector3 thumbSide = hand.InverseTransformVector(fingers[0, 0].position - fingers[3, 0].position);
            modelGripFrame = Quaternion.LookRotation(forward, thumbSide);
            foreach (var renderer in hand.GetComponentsInChildren<SkinnedMeshRenderer>())
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void SetHandGrip(int pose)
        {
            if (!hand || handPose == pose) return;
            handPose = pose;

            bool pinching = pose == 0;
            float handleDepth = pose == 2 ? .065f : 0f;
            Vector3 across = pinching ? new Vector3(-1f, .30f, .55f) : new Vector3(-1f, .38f, .90f);
            Quaternion rotation = Quaternion.LookRotation(across, Vector3.up) * Quaternion.Inverse(modelGripFrame);
            Vector3 wristTarget = pinching
                ? new Vector3(.100f, -.025f, -.030f)
                : new Vector3(.055f, pose == 2 ? -.125f : -.124f, handleDepth - .050f);
            hand.localRotation = rotation;
            hand.localPosition = wristTarget - rotation * Vector3.Scale(wristInModel, hand.localScale);

            for (int digit = 0; digit < DigitNames.Length; digit++)
            {
                for (int joint = 0; joint < 3; joint++)
                    fingers[digit, joint].localRotation = fingerRest[digit, joint];

                if (pinching)
                {
                    if (digit == 0 || digit == 4)
                        AimFingertip(digit, new Vector3(.033f, .032f, digit == 0 ? .002f : -.002f));
                    else
                        for (int joint = 0; joint < 3; joint++)
                            fingers[digit, joint].localRotation *= Quaternion.Euler(joint == 0 ? -24f : -39f, 0, 0);
                }
                else if (digit == 4)
                    AimFingertip(digit, new Vector3(.024f, wristTarget.y + .044f, handleDepth + .017f));
                else
                    WrapHandle(digit, handleDepth, pose == 2 ? .032f : .030f);
            }
        }

        void WrapHandle(int digit, float depth, float radius)
        {
            // The handle runs along tool-local Y. The radial offset includes glove thickness.
            Vector3 center = new Vector3(0f, 0f, depth);
            for (int joint = 0; joint < 3; joint++)
            {
                Transform bone = fingers[digit, joint];
                Transform end = fingers[digit, joint + 1];
                Vector3 start = tool.InverseTransformPoint(bone.position);
                Vector3 radial = start - center;
                radial.y = 0;
                if (radial.sqrMagnitude < .000001f) continue;
                float length = Vector3.Distance(start, tool.InverseTransformPoint(end.position));
                float arc = 2f * Mathf.Asin(Mathf.Clamp01(length / (2f * radius))) * Mathf.Rad2Deg;
                Vector3 target = center + Quaternion.AngleAxis(arc, Vector3.up) * radial.normalized * radius;
                target.y = start.y;
                AimBone(bone, end, tool.TransformPoint(target), 85f);
            }
        }
        void AimFingertip(int digit,Vector3 point)
        {
            Vector3 target=tool.TransformPoint(point);
            Transform tip=fingers[digit,3];
            for(int iteration=0;iteration<8;iteration++)
                for(int joint=2;joint>=0;joint--)
                    AimBone(fingers[digit,joint],tip,target,26f);
        }

        static void AimBone(Transform bone,Transform end,Vector3 target,float maxAngle)
        {
            Vector3 current=bone.parent.InverseTransformDirection(end.position-bone.position);
            Vector3 wanted=bone.parent.InverseTransformDirection(target-bone.position);
            if(current.sqrMagnitude<.0000001f || wanted.sqrMagnitude<.0000001f)return;
            Quaternion correction=Quaternion.FromToRotation(current,wanted);
            bone.localRotation=Quaternion.RotateTowards(Quaternion.identity,correction,maxAngle)*bone.localRotation;
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
            var rest=useCard?new Vector3(.19f,-.13f,.31f):new Vector3(.32f,-.15f,.52f);
            if(useSteam){rest.x+=.025f;rest.y-=.040f;}
            SetHandGrip(useCard?0:useSteam?2:1);
            float tremble=working?Mathf.Sin(Time.time*19)*.004f:0f;
            tool.localPosition=rest+new Vector3(0,(-.025f*gripPull+tremble)*eased,.042f*eased);
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
