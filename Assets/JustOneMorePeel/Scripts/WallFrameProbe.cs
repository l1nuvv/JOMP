#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace JustOneMorePeel
{
    // Temporary diagnostic: never added to the saved scene or to a Player.
    public sealed class WallFrameProbe : MonoBehaviour
    {
        public bool OwnerView;
        public string ReportName="wall-frame-probe";
        IEnumerator Start()
        {
            var game=FindAnyObjectByType<PeelGame>();
            game.BeginShift();
            var camera=game.view;
            var brain=camera.GetComponents<Behaviour>().First(b=>b.GetType().Name=="CinemachineBrain");
            bool wasEnabled=brain.enabled;brain.enabled=false;
            var position=camera.transform.position;var rotation=camera.transform.rotation;
            var stacks=FindObjectsByType<PeelPosterStack>(FindObjectsSortMode.None);
            var renderers=stacks.Select(s=>s.transform.Find("Underlying poster layers").GetComponent<Renderer>()).ToArray();
            var colliders=stacks.Select(s=>s.transform.Find("Underlying poster layers").GetComponent<Collider>()).ToArray();
            var data=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.ScriptableRendererData>("Assets/Settings/PC_Renderer.asset");
            var ao=data.rendererFeatures.First(f=>f.name=="ScreenSpaceAmbientOcclusion");bool aoEnabled=ao.isActive;
            var report=new List<string>{SystemInfo.graphicsDeviceName+" / "+SystemInfo.processorType,"screen="+Screen.width+"x"+Screen.height+" quality="+QualitySettings.names[QualitySettings.GetQualityLevel()]+" vsync="+QualitySettings.vSyncCount};
            var raw=new List<string>{"scenario,dt_ms,gpu_ms,main_ms,render_ms,presentwait_ms,focused"};
            var timing=new FrameTiming[1];
            try
            {
                foreach(string mode in OwnerView?new[]{"close","close_no_deep_render","close_repeat","moving_close"}:new[]{"close","away","far","close_no_deep_render","close_no_deep_collision","close_no_ssao","close_repeat","moving_close"})
                {
                    foreach(var r in renderers)r.enabled=mode!="close_no_deep_render";
                    foreach(var c in colliders)c.enabled=mode!="close_no_deep_collision";
                    ao.SetActive(aoEnabled && mode!="close_no_ssao");
                    camera.transform.SetPositionAndRotation(mode=="far"?new Vector3(.31f,1.45f,-3.61f):new Vector3(.31f,1.45f,2.52f),Quaternion.Euler(0,mode=="away"?180:4.88f,0));
                    if(OwnerView)camera.transform.SetPositionAndRotation(new Vector3(.79f,1.46f,1.58f),Quaternion.Euler(337.41f,18.41f,359.96f));
                    for(int i=0;i<40;i++)yield return null;
                    var dt=new List<double>();int focused=0;
                    for(int i=0;i<150;i++)
                    {
                        if(mode=="moving_close")camera.transform.position=OwnerView?new Vector3(.79f+Mathf.Sin(i*.06f)*.35f,1.46f,1.58f):new Vector3(.31f+Mathf.Sin(i*.06f)*.35f,1.45f,2.52f);
                        yield return null;
                        FrameTimingManager.CaptureFrameTimings();
                        FrameTimingManager.GetLatestTimings(1,timing);
                        double ms=Time.unscaledDeltaTime*1000;dt.Add(ms);if(Application.isFocused)focused++;
                        raw.Add(string.Join(",",new[]{mode,ms.ToString("F4",CultureInfo.InvariantCulture),timing[0].gpuFrameTime.ToString("F4",CultureInfo.InvariantCulture),timing[0].cpuMainThreadFrameTime.ToString("F4",CultureInfo.InvariantCulture),timing[0].cpuRenderThreadFrameTime.ToString("F4",CultureInfo.InvariantCulture),timing[0].cpuMainThreadPresentWaitTime.ToString("F4",CultureInfo.InvariantCulture),Application.isFocused?"1":"0"}));
                    }
                    dt.Sort();report.Add(mode+" median="+dt[75].ToString("F2")+" p95="+dt[142].ToString("F2")+" worst="+dt[149].ToString("F2")+" focused="+focused+"/150 draws="+UnityEditor.UnityStats.drawCalls);
                }
            }
            finally
            {
                foreach(var r in renderers)if(r)r.enabled=true;
                foreach(var c in colliders)if(c)c.enabled=true;
                ao.SetActive(aoEnabled);camera.transform.SetPositionAndRotation(position,rotation);brain.enabled=wasEnabled;
                File.WriteAllLines("Logs/"+ReportName+".csv",raw);File.WriteAllLines("Logs/"+ReportName+".txt",report);
                Debug.Log("Wall frame probe finished; state restored");
            }
            Destroy(this);
        }
    }
}
#endif
