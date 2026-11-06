using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace JustOneMorePeel
{
    public static class PeelProgressionChecks
    {
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static List<string> results;
        static void Check(string name,bool ok) { results.Add((ok?"PASS ":"FAIL ")+name); }
        static void Invoke(object owner,string method,params object[] args)
        {owner.GetType().GetMethod(method,Private).Invoke(owner,args);}
        static void Tap(PeelGame game,Keyboard keyboard,Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();Invoke(game,"Update");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
        }
        static void Aim(PeelGame game,PeelStrip strip)
        {
            game.view.transform.position=new Vector3(strip.transform.position.x,1.6f,1.1f);
            game.view.transform.LookAt(strip.transform.position);Physics.SyncTransforms();
        }
        static int GroupCount(PeelGame game,string field)
        {return ((System.Collections.IList)typeof(PeelGame).GetField(field,Private).GetValue(game)).Count;}
        static void FinishGrip(PeelGame game,int limit=80)
        {for(int tick=0;tick<limit && !game.AwaitingRelease;tick++)game.StepPeeling(true,new Vector2(0,-120),.1f);}

        public static string Run()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required.");
            results=new List<string>();
            var game=Object.FindAnyObjectByType<PeelGame>();
            var stacks=Object.FindObjectsByType<PeelPosterStack>().OrderBy(s=>s.transform.position.z).ToArray();
            var language=game.Settings.Language;float volume=game.Settings.SoundVolume,sensitivity=game.Settings.LookSensitivity;
            var audioField=typeof(PeelGame).GetField("audioSource",Private);var audio=audioField.GetValue(game);
            var cameraPosition=game.view.transform.position;var cameraRotation=game.view.transform.rotation;
            var brain=game.view.GetComponents<Behaviour>().FirstOrDefault(c=>c.GetType().Name=="CinemachineBrain");
            bool brainEnabled=brain && brain.enabled;if(brain)brain.enabled=false;
            var keyboard=InputSystem.AddDevice<Keyboard>("Peel verification keyboard");
            try
            {
                audioField.SetValue(game,null); // Suppress thousands of one-shot voices during accelerated simulation only.
                Check("Play Mode and fresh intro",EditorApplication.isPlaying && game.CurrentScreen==PeelScreen.Intro && game.removed==0);
                Check("Bench removed",!GameObject.Find("Worn courtyard bench"));
                Check("2432 logical posters",stacks.Length==19 && game.TotalPosters==2432 && game.total==9728);
                Check("152 display strips and fixed 32 scrap objects",Object.FindObjectsByType<PeelStrip>().Length==152 && game.Scraps.transform.childCount==32);
                Check("Intro cursor free",Cursor.lockState==CursorLockMode.None && Cursor.visible);
                Invoke(game,"OnApplicationFocus",true);
                Check("Intro focus return stays free",Cursor.lockState==CursorLockMode.None && !game.player.GetComponentInChildren<StarterAssets.StarterAssetsInputs>().cursorLocked);
                game.OpenSettings();Check("Settings available before starting",game.CurrentScreen==PeelScreen.Settings && Cursor.visible);
                var words=(string[,])typeof(PeelSettings).GetField("Words",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
                Check("Every UI key has two translations",words.GetLength(0)==Enum.GetValues(typeof(PeelText)).Length && words.GetLength(1)==2);
                game.Settings.SetLanguage(PeelLanguage.Russian);
                Check("Russian interface and printed poster",game.Settings.Text(PeelText.Settings)=="Настройки" && !AssetDatabase.GetAssetPath(stacks[0].CurrentStrips[0].GetComponent<Renderer>().sharedMaterial.mainTexture).Contains("/English/"));
                game.Settings.SetLanguage(PeelLanguage.English);
                Check("English interface and printed poster",game.Settings.Text(PeelText.Settings)=="Settings" && AssetDatabase.GetAssetPath(stacks[0].CurrentStrips[0].GetComponent<Renderer>().sharedMaterial.mainTexture).Contains("/English/"));
                Check("Language preference persisted",PlayerPrefs.GetInt(PeelSettings.LanguageKey,-1)==0);
                Tap(game,keyboard,Key.RightArrow);Check("Keyboard changes language to Russian",game.Settings.Language==PeelLanguage.Russian);
                Tap(game,keyboard,Key.LeftArrow);Check("Keyboard changes language to English",game.Settings.Language==PeelLanguage.English);
                float previous=game.Settings.SoundVolume;Tap(game,keyboard,Key.Tab);Tap(game,keyboard,Key.LeftArrow);
                Check("Keyboard adjusts sound volume",game.Settings.SoundVolume<previous);
                previous=game.Settings.LookSensitivity;Tap(game,keyboard,Key.Tab);Tap(game,keyboard,Key.RightArrow);
                Check("Keyboard adjusts mouse sensitivity",game.Settings.LookSensitivity>previous);
                Tap(game,keyboard,Key.Escape);Check("Settings back returns to intro",game.CurrentScreen==PeelScreen.Intro && Cursor.visible);
                Tap(game,keyboard,Key.Enter);Check("Enter starts game and locks cursor",game.CurrentScreen==PeelScreen.Playing && Cursor.lockState==CursorLockMode.Locked);
                Tap(game,keyboard,Key.Escape);Check("Escape pauses and frees cursor",game.CurrentScreen==PeelScreen.Paused && Cursor.visible);
                game.OpenSettings();Invoke(game,"OnApplicationFocus",true);
                Check("Pause settings retains free cursor on focus",game.CurrentScreen==PeelScreen.Settings && Cursor.visible);
                Tap(game,keyboard,Key.Escape);Check("Settings back returns to pause",game.CurrentScreen==PeelScreen.Paused);
                Tap(game,keyboard,Key.Escape);Check("Pause closes to gameplay",game.CurrentScreen==PeelScreen.Playing && !Cursor.visible);
                var lookInput=game.player.GetComponentInChildren<StarterAssets.StarterAssetsInputs>();
                lookInput.LookInput(new Vector2(12,-7));game.StepPeeling(false,Vector2.zero,0);
                Check("Idle grip release preserves controller look input",lookInput.look==new Vector2(12,-7) && lookInput.cursorInputForLook);
                lookInput.LookInput(Vector2.zero);

                var cart=GameObject.Find("Tool workbench");var point=cart.GetComponent<Collider>().bounds.center;
                game.view.transform.position=new Vector3(-4.35f,1.6f,-.6f);game.view.transform.LookAt(point);
                Check("Cart in range and facing",game.CanUseWorkbench());Tap(game,keyboard,Key.E);
                Check("E opens shop with free cursor",game.CurrentScreen==PeelScreen.Shop && Cursor.visible);
                Tap(game,keyboard,Key.Escape);Check("Shop closes with locked cursor",game.CurrentScreen==PeelScreen.Playing && !Cursor.visible);
                game.view.transform.rotation=Quaternion.LookRotation(Vector3.back);Check("Cart rejects looking away",!game.CanUseWorkbench());
                game.view.transform.position=new Vector3(-4.35f,1.6f,-3);game.view.transform.LookAt(point);Check("Cart rejects distance",!game.CanUseWorkbench());

                float idle=PeelGame.PeelAmount(Vector2.zero,.1f,0,false);
                Check("Card has no downward-drag bonus",Mathf.Approximately(idle,PeelGame.PeelAmount(new Vector2(0,-100000),.1f,0,false)));
                Check("Scraper unlocks downward bonus",PeelGame.PeelAmount(new Vector2(0,-120),.1f,1,false)>PeelGame.PeelAmount(Vector2.zero,.1f,1,false));
                Check("Upward motion has no bonus",Mathf.Approximately(PeelGame.PeelAmount(new Vector2(0,120),.1f,1,false),PeelGame.PeelAmount(Vector2.zero,.1f,1,false)));
                Check("Physical drag speed is frame-rate independent",Mathf.Approximately(PeelGame.PeelAmount(new Vector2(0,-10),1f/60,1,false),2*PeelGame.PeelAmount(new Vector2(0,-5),1f/120,1,false)));
                Check("Huge mouse delta cannot instantly peel",PeelGame.PeelAmount(new Vector2(0,-100000),1f/60,1,false)<.02f);
                Check("Long frame progress capped",PeelGame.PeelAmount(new Vector2(0,-100000),10,1,false)<.1f);
                game.money=10000;Check("Steam requires a scraper",!game.TryBuySteam());game.money=0;

                var first=stacks[0];var cardTarget=first.CurrentStrips[1];Aim(game,cardTarget);game.StepPeeling(false,Vector2.zero,0);
                Check("Outline selects poster under crosshair",game.Outline.Selected==first && game.Outline.GetComponent<LineRenderer>().enabled);
                var outline=game.Outline.GetComponent<LineRenderer>();
                Check("Outline spans entire poster width",Mathf.Abs(Vector3.Distance(outline.GetPosition(1),outline.GetPosition(2))-cardTarget.width*4)<.001f);
                game.StepPeeling(true,new Vector2(0,-100000),.1f);
                Check("Card works on one strip",GroupCount(game,"workingStrips")==1 && game.removed==0);
                Check("Active grip freezes look without releasing cursor",!lookInput.cursorInputForLook && lookInput.look==Vector2.zero && Cursor.lockState==CursorLockMode.Locked);
                FinishGrip(game);Check("Card grip removes exactly one strip",game.removed==1 && game.ClearedPosters==0 && game.AwaitingRelease);
                cardTarget.Pull(10);Check("Removed strip cannot pay twice",game.money==2 && game.removed==1);
                for(int i=0;i<100;i++)game.StepPeeling(true,new Vector2(0,-100000),.1f);
                Check("Holding does not auto-peel another strip or layer",game.removed==1);
                Check("Underlying sheet stays covered until entire top removed",!first.GetComponentsInChildren<PeelStrip>().First(s=>s!=cardTarget && s.BufferIndex==cardTarget.BufferIndex).IsExposed);
                game.StepPeeling(false,Vector2.zero,0);Aim(game,first.CurrentStrips[2]);game.StepPeeling(true,Vector2.zero,.1f);FinishGrip(game);
                Check("Release and fresh grip removes next strip",game.removed==2 && game.money==4);
                game.money=game.UpgradeCost-1;Check("Purchase rejects insufficient funds",!game.TryUpgrade());
                game.money=game.UpgradeCost;Check("First upgrade buys scraper",game.TryUpgrade() && game.toolLevel==1 && game.money==0);
                game.StepPeeling(false,Vector2.zero,0);Aim(game,first.CurrentStrips[0]);game.StepPeeling(true,Vector2.zero,.01f);
                Check("Scraper captures remaining two strips of partly peeled poster",GroupCount(game,"workingStrips")==2);
                FinishGrip(game);Check("Scraper finishes whole poster once",game.ClearedPosters==1 && game.removed==4 && game.money==4);
                for(int i=0;i<100;i++)game.StepPeeling(true,Vector2.zero,.1f);
                Check("Scraper does not advance through layers while held",game.ClearedPosters==1);
                int emissions=game.Scraps.TotalEmissions;
                game.StepPeeling(false,Vector2.zero,0);Aim(game,first.CurrentStrips[1]);game.StepPeeling(true,Vector2.zero,.01f);
                Check("Fresh scraper grip captures all four poster parts",GroupCount(game,"workingStrips")==4);
                FinishGrip(game);Check("One complete poster pays eight",game.ClearedPosters==2 && game.removed==8 && game.money==12);
                Check("Intact poster falls as one sheet",game.Scraps.TotalEmissions==emissions+1);

                game.money=PeelGame.SteamCost;Check("Steam purchase succeeds after scraper",game.TryBuySteam() && game.steamUnlocked && game.activeTool==1 && game.money==0);
                PeelPosterStack best=null;int maximum=0;
                foreach(var stack in stacks)
                {
                    Aim(game,stack.CurrentStrips[1]);game.StepPeeling(false,Vector2.zero,0);game.StepPeeling(true,Vector2.zero,0);
                    int count=GroupCount(game,"workingStacks");if(count>maximum){maximum=count;best=stack;}
                }
                Check("Steam can select three nearby visible posters",maximum==3);
                game.StepPeeling(false,Vector2.zero,0);Aim(game,best.CurrentStrips[1]);game.StepPeeling(true,Vector2.zero,.01f);
                int selected=GroupCount(game,"workingStacks"),before=game.ClearedPosters;
                Check("Steam group bounded at three posters and twelve parts",selected<=3 && GroupCount(game,"workingStrips")<=12);
                Invoke(Object.FindAnyObjectByType<PeelToolView>(),"Update");Check("Steam effect plays while working",Object.FindAnyObjectByType<PeelToolView>().GetComponentInChildren<ParticleSystem>().isPlaying);
                FinishGrip(game);Check("Steam finishes selected posters",game.ClearedPosters==before+selected);
                int removed=game.removed;for(int i=0;i<80;i++)game.StepPeeling(true,Vector2.zero,.1f);
                Check("Steam requires new grip for next posters",game.removed==removed);
                game.StepPeeling(false,Vector2.zero,0);Invoke(Object.FindAnyObjectByType<PeelToolView>(),"Update");
                Check("Steam stops on release",!Object.FindAnyObjectByType<PeelToolView>().GetComponentInChildren<ParticleSystem>().isPlaying);
                game.activeTool=0;game.toolLevel=3;

                int transformCount=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Length;
                int meshCount=Resources.FindObjectsOfTypeAll<Mesh>().Length;
                int expectedMoney=game.money+(game.total-game.removed)*2;
                bool reachable=true;
                foreach(var stack in stacks)
                    while(stack.RemainingPosters>0)
                    {
                        var strip=stack.CurrentStrips.First(s=>s.IsExposed);
                        Aim(game,strip);game.StepPeeling(false,Vector2.zero,0);int cleared=stack.ClearedPosters;
                        game.StepPeeling(true,new Vector2(0,-120),.1f);FinishGrip(game);
                        if(stack.ClearedPosters==cleared){reachable=false;break;}
                    }
                Check("Every remaining poster reachable through gameplay rays",reachable);
                Check("All 2432 posters can be cleared",game.ClearedPosters==2432 && stacks.All(s=>s.RemainingPosters==0) && game.removed==9728);
                Check("Total payout exact across buffer reuse",game.money==expectedMoney && Mathf.Approximately(game.Progress,1));
                Check("Object count constant across thousands of posters",Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Length==transformCount);
                Check("Mesh count constant across thousands of posters",Resources.FindObjectsOfTypeAll<Mesh>().Length==meshCount);
                Check("Only original display buffers and fixed scraps remain",Object.FindObjectsByType<PeelStrip>().Length==152 && game.Scraps.transform.childCount==32);
                Check("All poster colliders disabled at completion",stacks.SelectMany(s=>s.GetComponentsInChildren<PeelStrip>()).All(s=>!s.GetComponent<Collider>().enabled));
            }
            catch(Exception error){results.Add("FAIL Exception: "+error);}
            finally
            {
                InputSystem.RemoveDevice(keyboard);audioField.SetValue(game,audio);
                game.Settings.SetLanguage(language);game.Settings.SetSoundVolume(volume);game.Settings.SetLookSensitivity(sensitivity);game.Settings.Save();
                game.view.transform.SetPositionAndRotation(cameraPosition,cameraRotation);if(brain)brain.enabled=brainEnabled;
                Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/Peel-progression-checks.txt",results);
            }
            return string.Join("\n",results)+"\nTOTAL: "+results.Count(s=>s.StartsWith("PASS"))+" PASS, "+results.Count(s=>s.StartsWith("FAIL"))+" FAIL";
        }
    }
}
