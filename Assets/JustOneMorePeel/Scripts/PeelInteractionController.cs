using System;
using System.Collections.Generic;
using UnityEngine;

namespace JustOneMorePeel
{
    internal sealed class PeelInteractionController
    {
        const float SteamRate=1.08f;
        const float SteamTurnSampleDistance=3f;
        const float SteamTurnIdleLimit=.15f;
        readonly List<PeelStrip> workingStrips=new List<PeelStrip>(12);
        readonly List<PeelPosterStack> workingStacks=new List<PeelPosterStack>(3);
        readonly List<PeelPosterStack> wholePosterVisuals=new List<PeelPosterStack>(3);
        readonly List<PeelPosterStack> nearbyStacks=new List<PeelPosterStack>();
        readonly List<PeelPosterStack> previewStacks=new List<PeelPosterStack>(5);
        Vector3 gripPoint;
        PeelStrip contactStrip;
        Vector2 contactOnSheet;
        bool liftingWithScraper;
        internal Vector3 ToolContactPoint => contactStrip && !contactStrip.IsRemoved
            ? contactStrip.SurfacePoint(liftingWithScraper
                ? new Vector2(contactOnSheet.x, (contactStrip.progress-.5f)*contactStrip.height)
                : contactOnSheet) : gripPoint;
        Vector2 steamTurnSample, previousSteamDirection;
        float steamTurnDegrees, steamTurnIdleTime;
        PeelStrip target;
        PeelAudioController audio;
        PeelWallVisibility wallVisibility;
        MillionPosterField posterField;
        PeelPosterOutline Outline;
        PeelScrapPool Scraps;
        Action<bool> setLookFrozen;
        Action<int> paperDetached;

        internal bool AwaitingRelease { get; set; }
        internal bool IsPeeling => workingStrips.Count>0;
        internal IReadOnlyList<PeelPosterStack> WorkingStacks => workingStacks;
        internal int PreviewTargetCount => previewStacks.Count;
        internal float GripProgress => workingStrips.Count>0 && workingStrips[0] ? workingStrips[0].progress : 0f;
        internal float GripCompletion
        {
            get
            {
                if(!IsPeeling)return 0f;

                float completion=1f;
                foreach(var strip in workingStrips)
                    if(strip)completion=Mathf.Min(completion,strip.progress);
                return completion;
            }
        }
        internal PeelStrip Target => target;
#if UNITY_EDITOR
        internal bool testFastPeeling;
#endif

        internal void Initialize(PeelAudioController audio, PeelWallVisibility visibility, MillionPosterField field,
            PeelPosterOutline outline, PeelScrapPool scraps, Action<bool> onLookFreeze, Action<int> onPaperDetached)
        {
            this.audio=audio;wallVisibility=visibility;posterField=field;Outline=outline;Scraps=scraps;setLookFrozen=onLookFreeze;
            paperDetached=onPaperDetached;
        }

        internal void StepPeeling(bool pressed,Vector2 delta,float seconds, Camera view,float reach,
            int toolLevel,int activeTool,bool steamUnlocked,bool steamExpanded,int? freePaperSlots,ref int removed)
        {
            bool canStartGrip=freePaperSlots is null or >0;
            bool collectWholeSheets=freePaperSlots.HasValue && toolLevel>0;
            bool usingSteam=toolLevel>0 && activeTool==1 && steamUnlocked;
            target=null;
            if(Physics.Raycast(view.transform.position,view.transform.forward,out var hit,reach))
            {
                target=hit.collider.GetComponent<PeelStrip>();
                if(pressed && !AwaitingRelease && posterField && workingStrips.Count==0)
                    target=posterField.ResolveTarget(hit,target,view.transform.position,view.transform.forward);
            }
            previewStacks.Clear();
            if(IsPeeling)
            {
                foreach(var stack in workingStacks)
                    if(!collectWholeSheets || !stack.NeedsAdvance)previewStacks.Add(stack);
            }
            else if(canStartGrip && target && target.IsExposed)
            {
                if(usingSteam)
                    SelectSteamTargets(target.Stack,previewStacks,view,reach,steamExpanded,Mathf.Min(steamExpanded?5:3,freePaperSlots??5));
                else previewStacks.Add(target.Stack);
            }
            if(Outline)Outline.SetTargets(previewStacks,toolLevel>0,usingSteam);
            if(!pressed){Release();setLookFrozen(false);AwaitingRelease=false;return;}
            if(AwaitingRelease)return;
            if(!canStartGrip && !IsPeeling){setLookFrozen(false);return;}
            if(workingStrips.Count>0 && !GripReachable(view,reach))
            {
                CancelGrip();
                AwaitingRelease=false;
                if(Outline)Outline.SetTarget(null);
                return;
            }
            if(workingStrips.Count==0 && target && target.IsExposed)
            {
                gripPoint=hit.point;
                contactStrip=target;
                contactOnSheet=target.transform.InverseTransformPoint(hit.point);
                liftingWithScraper=toolLevel>0 && !usingSteam;
                if(toolLevel==0)workingStrips.Add(target);
                else
                {
                    if(usingSteam)
                    {
                        foreach(var stack in previewStacks)AddPoster(stack);
                    }
                    else AddPoster(target.Stack);
                }
                if(target.Stack && !workingStacks.Contains(target.Stack))workingStacks.Add(target.Stack);
                foreach(var strip in workingStrips)strip.Begin(liftingWithScraper);
                audio.BeginPeel(target,toolLevel,activeTool,steamUnlocked);
                setLookFrozen(true);
            }
            if(workingStrips.Count==0)
            {
                SteamTurnBonus(Vector2.zero,seconds);
                return;
            }
            float amount=PeelAmount(delta,seconds,toolLevel,usingSteam);
            if(usingSteam)amount*=1f+.8f*SteamTurnBonus(delta,seconds);
            if(toolLevel>0 && !usingSteam && workingStacks.Count==1 && workingStacks[0].PrecisionWork)
                amount*=Mathf.Lerp(1f,3f,Mathf.Clamp01(Mathf.Abs(delta.x)/(Mathf.Max(seconds,.001f)*1200f)));
#if UNITY_EDITOR
            if(testFastPeeling)amount*=10f;
#endif
            int removedBefore=removed;
            bool finished=true;
            bool completedSheet=false;
            if(collectWholeSheets)
            {
                foreach(var stack in workingStacks)
                {
                    if(stack.NeedsAdvance)continue;
                    if(stack.PullAsSheet(amount))completedSheet=true;
                    else finished=false;
                }
                if(completedSheet)
                {
                    previewStacks.RemoveAll(stack=>stack.NeedsAdvance);
                    if(Outline)Outline.SetTargets(previewStacks,true,usingSteam);
                }
            }
            else
            {
                foreach(var strip in workingStrips)
                {
                    if(strip.IsRemoved)continue;
                    strip.Pull(amount);
                    if(!strip.IsRemoved)finished=false;
                    else if(strip.Stack && strip.Stack.NeedsAdvance)completedSheet=true;
                }
            }
            int detached=removed-removedBefore;
            audio.PlayDetach(detached,completedSheet);
            if(detached>0)paperDetached(detached);
            if(finished)
            {
                foreach(var strip in workingStrips)strip.End();
                workingStrips.Clear();audio.StopPeelSound();AdvanceStacks();
                // The same held gesture can continue on the next layer. Return mouse look on release.
            }
        }
        bool GripReachable(Camera view,float reach)
        {
            Vector3 offset=gripPoint-view.transform.position;
            float distance=offset.magnitude;
            if(distance>reach)return false;
            if(posterField || !Physics.Raycast(view.transform.position,offset,out var obstruction,distance-.01f))return true;
            return obstruction.collider.TryGetComponent<PeelStrip>(out var strip) && workingStrips.Contains(strip);
        }
        void AddPoster(PeelPosterStack stack)
        {
            if(!stack)return;
            workingStacks.Add(stack);
            int intact=0;foreach(var strip in stack.CurrentStrips)if(strip.IsExposed)intact++;
            if(intact==4)wholePosterVisuals.Add(stack);
            foreach(var strip in stack.CurrentStrips)if(strip.IsExposed)workingStrips.Add(strip);
        }
        internal bool NeedsStripScrap(PeelStrip strip) { return !wholePosterVisuals.Contains(strip.Stack); }
        void SelectSteamTargets(PeelPosterStack origin,List<PeelPosterStack> result, Camera view,float reach,bool steamExpanded,int maxTargets)
        {
            result.Add(origin);
            if(maxTargets==1)return;
            float radius=steamExpanded?2.5f:1.5f;
            wallVisibility.FindNearby(origin.transform.position,radius,nearbyStacks);
            while(result.Count<maxTargets)
            {
                PeelPosterStack closest=null;float distance=radius*radius;
                foreach(var candidate in nearbyStacks)
                {
                    if(candidate.RemainingPosters==0 || result.Contains(candidate))continue;
                    float d=(candidate.transform.position-origin.transform.position).sqrMagnitude;
                    if(d>=distance || !VisiblePoster(candidate,view,reach))continue;
                    closest=candidate;distance=d;
                }
                if(!closest)break;
                result.Add(closest);
            }
        }
        bool VisiblePoster(PeelPosterStack stack,Camera view,float reach)
        {
            foreach(var strip in stack.CurrentStrips)
            {
                if(!strip.IsExposed)continue;
                Vector3 direction=strip.transform.position-view.transform.position;
                if(direction.magnitude<=reach && Vector3.Dot(view.transform.forward,direction.normalized)>.65f
                    && Physics.Raycast(view.transform.position,direction.normalized,out var hit,Mathf.Min(reach,direction.magnitude+.08f))
                    && hit.collider.TryGetComponent<PeelStrip>(out var exposed) && exposed.Stack==stack && exposed.IsExposed)
                    return true;
            }
            return false;
        }
        internal static float PeelAmount(Vector2 delta,float seconds,int level,bool steam)
        {
            float dt=Mathf.Clamp(seconds,0,.1f);
            if(steam && level>0)return dt*SteamRate;
            float rate=level==0?.4f:.35f+Mathf.Clamp(level,1,3)*.1f;
            float horizontalSpeed=seconds>0?Mathf.Abs(delta.x)/seconds:0;
            float drag=level>0?Mathf.Clamp01(horizontalSpeed/1200f)*.8f:0;
            return dt*rate*(1+drag);
        }

        float SteamTurnBonus(Vector2 delta,float seconds)
        {
            if(seconds<=0)return 0f;
            steamTurnDegrees=Mathf.MoveTowards(steamTurnDegrees,0f,180f*seconds);
            steamTurnIdleTime+=seconds;
            if(steamTurnIdleTime>=SteamTurnIdleLimit)ResetSteamTurn();

            // Accumulate pixels before measuring heading to filter small, quantized mouse deltas.
            steamTurnSample+=delta;
            if(steamTurnSample.sqrMagnitude>=SteamTurnSampleDistance*SteamTurnSampleDistance)
            {
                var direction=steamTurnSample.normalized;
                // Positive Y is up in Input System mouse deltas, so clockwise turns have a negative cross product.
                float turn=-Vector2.SignedAngle(previousSteamDirection,direction);
                if(Mathf.Abs(turn)>120f)steamTurnDegrees=0f;
                else steamTurnDegrees=Mathf.Clamp(steamTurnDegrees+turn,0f,225f);
                previousSteamDirection=direction;
                steamTurnSample=Vector2.zero;
                steamTurnIdleTime=0f;
            }
            return delta==Vector2.zero?0f:Mathf.InverseLerp(45f,180f,steamTurnDegrees);
        }

        void ResetSteamTurn()
        {
            steamTurnSample=previousSteamDirection=Vector2.zero;
            steamTurnDegrees=steamTurnIdleTime=0f;
        }
        void AdvanceStacks()
        {
            bool changed=false;
            foreach(var stack in workingStacks)if(stack.NeedsAdvance)
            {if(wholePosterVisuals.Contains(stack))Scraps.EmitPoster(stack);stack.AdvanceIfComplete();changed=true;}
            workingStacks.Clear();wholePosterVisuals.Clear();if(changed)Physics.SyncTransforms();
        }

        internal void Release()
        {
            CancelGrip();
            AwaitingRelease=true;
        }

        void CancelGrip()
        {
            ResetSteamTurn();
            foreach(var strip in workingStrips)if(strip)strip.End();
            workingStrips.Clear();audio?.StopPeelSound();AdvanceStacks();
        }
    }
}
