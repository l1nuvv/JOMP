using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace JustOneMorePeel
{
    public sealed class PeelPosterOutline : MonoBehaviour
    {
        public PeelPosterStack Selected { get; private set; }
        LineRenderer line;
        readonly List<LineRenderer> lines=new List<LineRenderer>();
        readonly List<PeelPosterStack> targets=new List<PeelPosterStack>();
        Material material;
        readonly Vector3[] corners=new Vector3[4];
        void Awake()
        {
            line=gameObject.AddComponent<LineRenderer>();
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor",new Color(.96f,.79f,.43f));line.sharedMaterial=material;
            line.useWorldSpace=true;line.loop=true;line.positionCount=4;line.widthMultiplier=.009f;
            line.numCornerVertices=3;line.numCapVertices=2;
            line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.enabled=false;
            lines.Add(line);
        }
        public void SetTarget(PeelPosterStack stack)
        { targets.Clear();if(stack)targets.Add(stack);RefreshLines(); }
        public void SetTargets(List<PeelPosterStack> stacks)
        { targets.Clear();targets.AddRange(stacks);RefreshLines(); }
        void RefreshLines()
        {
            Selected=targets.Count>0?targets[0]:null;
            while(lines.Count<targets.Count)
            {
                var child=new GameObject("Additional poster outline");child.transform.SetParent(transform,false);
                var extra=child.AddComponent<LineRenderer>();extra.sharedMaterial=material;extra.useWorldSpace=true;extra.loop=true;
                extra.positionCount=4;extra.widthMultiplier=line.widthMultiplier;extra.numCornerVertices=3;extra.numCapVertices=2;
                extra.shadowCastingMode=ShadowCastingMode.Off;extra.receiveShadows=false;lines.Add(extra);
            }
            for(int i=0;i<lines.Count;i++)lines[i].enabled=i<targets.Count && targets[i] && targets[i].RemainingPosters>0;
            UpdateCorners();
        }
        void LateUpdate() { UpdateCorners(); }
        void UpdateCorners()
        {
            for(int i=0;i<targets.Count;i++)
            {
            var selected=targets[i];var current=lines[i];
            if(!selected || selected.RemainingPosters==0){current.enabled=false;continue;}
            var strip=selected.CurrentStrips[0];float w=strip.width*2,h=strip.height*.5f;
            float x=strip.transform.localPosition.x+strip.width*1.5f;
            float z=strip.transform.localPosition.z-.003f;
            corners[0]=selected.transform.TransformPoint(new Vector3(x-w,-h,z));
            corners[1]=selected.transform.TransformPoint(new Vector3(x-w,h,z));
            corners[2]=selected.transform.TransformPoint(new Vector3(x+w,h,z));
            corners[3]=selected.transform.TransformPoint(new Vector3(x+w,-h,z));current.SetPositions(corners);
            }
        }
        void OnDestroy() {if(material)Destroy(material);}
    }
}
