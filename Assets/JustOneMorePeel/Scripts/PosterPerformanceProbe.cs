#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using UnityEngine;

namespace JustOneMorePeel
{
    /// <summary>Opt-in Editor experiment. Never attach to the saved scene. Exit Play afterwards.</summary>
    public sealed class PosterPerformanceProbe : MonoBehaviour
    {
        public string ReportName="poster-audit/stress";
        public int[] Totals={100,500,1000,2000,2432,5000};
        public string[] Scenarios={"close","away","occluded","moving","rotating","active_12_scraps"};
        public int WarmupFrames=30, SampleFrames=120;
        public string Status { get; private set; }
        static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
        static string F(double v) => v.ToString("F4",CultureInfo.InvariantCulture);
        static double Percentile(List<double> values,double p)
        {values.Sort();return values[Mathf.Clamp((int)Math.Ceiling(values.Count*p)-1,0,values.Count-1)];}

        IEnumerator Start()
        {
            var game=FindAnyObjectByType<PeelGame>();
            var camera=game.view;
            var stacks=FindObjectsByType<PeelPosterStack>().OrderBy(s=>s.name).ToArray();
            var counts=stacks.Select(s=>s.PosterCount).ToArray();
            var countField=typeof(PeelPosterStack).GetField("posterCount",Fields);
            var brain=camera.GetComponents<Behaviour>().First(b=>b.GetType().Name=="CinemachineBrain");
            var controller=game.player.GetComponent<StarterAssets.FirstPersonController>();
            var position=camera.transform.position;var rotation=camera.transform.rotation;
            bool brainEnabled=brain.enabled,controllerEnabled=controller && controller.enabled;
            var oldScreen=game.CurrentScreen;
            var data=new List<string>{"logical,scenario,frame,dt_ms,cpu_main_ms,cpu_render_ms,gpu_ms,timing_available,gc_editor_frame_bytes,draws,setpass,triangles,vertices,focused,visible_patches"};
            var report=new List<string>{"Editor Play; depth stress across existing 19 patches, NOT thousands of independent world patches.",
                SystemInfo.graphicsDeviceName+" / "+SystemInfo.processorType+" / "+SystemInfo.graphicsDeviceType,
                "resolution="+Screen.width+"x"+Screen.height+" quality="+QualitySettings.names[QualitySettings.GetQualityLevel()]+" vsync="+QualitySettings.vSyncCount+" warmup="+WarmupFrames+" samples="+SampleFrames,
                "FrameTiming zeros are unavailable. GC recorder includes Editor/harness allocations; not poster-only GC. Scripted effects, not physical held input."};
            var frameTiming=new FrameTiming[1];
            var visibility=game.GetComponent<PeelWallVisibility>();
            var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            var block=new GameObject("Audit occluder");
            var filter=block.AddComponent<MeshFilter>();
            var mesh=new Mesh {name="Audit occluder quad"};
            mesh.vertices=new[]{new Vector3(-20,-20,0),new Vector3(20,-20,0),new Vector3(-20,20,0),new Vector3(20,20,0)};
            mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();filter.sharedMesh=mesh;
            var renderer=block.AddComponent<MeshRenderer>();
            var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetFloat("_Cull",0);renderer.sharedMaterial=mat;
            block.transform.position=new Vector3(0,0,2.9f);block.SetActive(false);
            try
            {
                game.BeginShift();brain.enabled=false;if(controller)controller.enabled=false;
                foreach(int total in Totals)
                {
                    if(total<stacks.Length*2)throw new InvalidOperationException("At least two posters per patch required");
                    for(int i=0;i<stacks.Length;i++)
                    {countField.SetValue(stacks[i],total/stacks.Length+(i<total%stacks.Length?1:0));stacks[i].Initialize(game);}
                    game.total=total*4;game.removed=0;game.money=0;
                    foreach(string scenario in Scenarios)
                    {
                        Status=total+" / "+scenario;
                        block.SetActive(scenario=="occluded");
                        camera.transform.SetPositionAndRotation(new Vector3(.79f,1.46f,1.58f),Quaternion.Euler(337.41f,18.41f,359.96f));
                        if(scenario=="away")camera.transform.rotation=Quaternion.Euler(0,180,0);
                        var active=stacks.Take(3).SelectMany(s=>s.CurrentStrips).ToArray();
                        if(scenario=="active_12_scraps")
                        {
                            foreach(var strip in active){strip.Begin();strip.Pull(.3f);}
                            for(int i=0;i<PeelScrapPool.Capacity;i++)game.Scraps.Emit(active[i%active.Length]);
                        }
                        var dt=new List<double>();var main=new List<double>();var gpu=new List<double>();int focused=0;
                        for(int frame=-WarmupFrames;frame<SampleFrames;frame++)
                        {
                            int route=frame+WarmupFrames;
                            if(scenario=="moving")camera.transform.position=new Vector3(.79f+Mathf.Sin(route*.06f)*.35f,1.46f,1.58f);
                            if(scenario=="rotating")camera.transform.rotation=Quaternion.Euler(0,route*7,0);
                            if(scenario=="active_12_scraps")
                            {
                                // Keep partial progress below detach; exercise twelve real deformable views.
                                foreach(var strip in active){strip.progress=.3f+Mathf.Sin(route*.03f)*.1f;strip.Pull(.0001f);}
                                if(route%3==0)game.Scraps.Emit(active[route%active.Length]);
                            }
                            FrameTimingManager.CaptureFrameTimings();
                            yield return null;
                            if(frame<0)continue;
                            uint available=FrameTimingManager.GetLatestTimings(1,frameTiming);
                            double ms=Time.unscaledDeltaTime*1000;dt.Add(ms);
                            if(available>0 && frameTiming[0].cpuMainThreadFrameTime>0)main.Add(frameTiming[0].cpuMainThreadFrameTime);
                            if(available>0 && frameTiming[0].gpuFrameTime>0)gpu.Add(frameTiming[0].gpuFrameTime);
                            if(Application.isFocused)focused++;
                            data.Add(total+","+scenario+","+frame+","+F(ms)+","+F(available>0?frameTiming[0].cpuMainThreadFrameTime:0)+","+F(available>0?frameTiming[0].cpuRenderThreadFrameTime:0)+","+F(available>0?frameTiming[0].gpuFrameTime:0)+","+available+","+(gc.Valid?gc.LastValue:-1)+","+UnityEditor.UnityStats.drawCalls+","+UnityEditor.UnityStats.setPassCalls+","+UnityEditor.UnityStats.triangles+","+UnityEditor.UnityStats.vertices+","+(Application.isFocused?1:0)+","+visibility.VisiblePatches);
                        }
                        foreach(var strip in active)strip.End();
                        report.Add(total+" "+scenario+" median="+F(Percentile(dt,.5))+" p95="+F(Percentile(dt,.95))+" p99="+F(Percentile(dt,.99))+" mean="+F(dt.Average())+" main_median="+(main.Count>0?F(Percentile(main,.5)):"unavailable")+" gpu_median="+(gpu.Count>0?F(Percentile(gpu,.5)):"unavailable")+" focused="+focused+"/"+SampleFrames);
                        File.WriteAllLines("Logs/"+ReportName+".txt",report);
                        File.WriteAllLines("Logs/"+ReportName+".csv",data);
                    }
                }
                Status="complete";
            }
            finally
            {
                gc.Dispose();Destroy(block);Destroy(mesh);Destroy(mat);
                for(int i=0;i<stacks.Length;i++){countField.SetValue(stacks[i],counts[i]);stacks[i].Initialize(game);}
                camera.transform.SetPositionAndRotation(position,rotation);brain.enabled=brainEnabled;if(controller)controller.enabled=controllerEnabled;
                // Experiments deliberately change runtime progress. Discard the entire session.
                report.Add("Finished; original depths/camera restored. Exit Play to discard test progress. Original screen="+oldScreen);
                File.WriteAllLines("Logs/"+ReportName+".txt",report);File.WriteAllLines("Logs/"+ReportName+".csv",data);
            }
            Destroy(gameObject);
        }
    }
}
#endif
