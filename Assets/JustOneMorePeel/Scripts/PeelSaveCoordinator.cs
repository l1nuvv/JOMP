using System;
using System.Collections.Generic;
using System.IO;
using StarterAssets;
using UnityEngine;

namespace JustOneMorePeel
{
    internal enum SaveEntry { New, Continue, Backup, Unreadable, Incompatible }

    internal sealed class PeelSaveCoordinator
    {
        readonly UnityEngine.Object context;
        readonly Func<string> path;
        readonly string sceneId;
        readonly List<PeelPosterStack> stacks;
        readonly PeelRoutePrototype routePrototype;
        readonly PeelBasementLight basementLight;
        readonly PeelPavilion pavilion;
        readonly PeelPavilionHub hub;
        readonly string[] localPalette;
        int saveVersion=PeelWorldSave.CurrentVersion;
        readonly int TotalPosters;
        string WorldSavePath => path();
        string inspectedSavePath;
        SaveEntry saveEntry;
        bool saveBlocked, saveWriteFailed, saveSucceeded, recoveryPending;
        internal bool Failed => saveBlocked || saveWriteFailed;
        internal bool Succeeded => saveSucceeded;
        internal bool WriteFailed => saveWriteFailed;
        internal bool CheckpointBackupPending { get; private set; }
        internal float RecoveryNoticeUntil { get; private set; }
        internal PeelWorldSave InspectedSave { get; private set; }
        internal bool InspectedShiftFinished { get; private set; }

        internal PeelSaveCoordinator(UnityEngine.Object context, Func<string> path, string sceneId, List<PeelPosterStack> stacks,
            int totalPosters, PeelSettings settings, PeelRoutePrototype route, PeelBasementLight light, PeelPavilion pavilion=null, PeelPavilionHub hub=null)
        {
            this.context=context;this.path=path;this.sceneId=sceneId;this.stacks=stacks;TotalPosters=totalPosters;
            routePrototype=route;basementLight=light;this.pavilion=pavilion;this.hub=hub;
            localPalette=new string[settings.PaperCount];
            for(int i=0;i<localPalette.Length;i++)localPalette[i]=settings.PaperSaveId(i);
        }

        internal SaveEntry InspectIfPathChanged()
        {
            if(inspectedSavePath!=WorldSavePath)
            {
                inspectedSavePath=WorldSavePath;
                saveEntry=InspectWorld(out var data,out _);
                InspectedSave=data;
                InspectedShiftFinished=IsFinished(data);
            }
            return saveEntry;
        }

        internal void Reset() => PeelWorldSave.Reset(WorldSavePath);
        bool IsFinished(PeelWorldSave data) => data!=null &&
            (pavilion ? data.lightOn : routePrototype && routePrototype.IsSavedArchiveCleared(data));

        bool SaveIndex(out Dictionary<string,PeelPosterStack> index)
        {
            if(PeelPersistenceIds.TryIndex(stacks,out index,out var error))
            {
                int posters=0;
                foreach(var stack in index.Values)posters+=stack.PosterCount;
                if(index.Count>0 && TotalPosters>0 && posters==TotalPosters)return true;
                error="Poster stack cache is incomplete; restart the play session before saving.";
            }
            Debug.LogError("World save disabled: "+error,context);saveBlocked=true;return false;
        }
        internal bool Write(int money,int toolLevel,int activeTool,bool steamUnlocked,bool steamExpanded,float elapsed,
            GameObject player,FirstPersonController controller,bool checkpointBackup,PeelPaperSave paper)
        {
            if((routePrototype && routePrototype.ReviewMode) || !SaveIndex(out var index))return false;
            saveSucceeded=false;
            var data=new PeelWorldSave {version=saveVersion,scene=sceneId,money=money,toolLevel=toolLevel,activeTool=activeTool,
                steamUnlocked=steamUnlocked,steamExpanded=steamExpanded,elapsed=elapsed,position=player.transform.position,
                rotation=player.transform.rotation,firstDoorOpen=routePrototype && routePrototype.CourtyardDoorOpen,
                lightOn=pavilion ? pavilion.IsLit : basementLight && basementLight.SwitchIsOn,
                basementGlassSeen=routePrototype && routePrototype.BasementGlassSeen,
                stacks=new PeelStackSave[index.Count],paper=paper,
                noticePalette=saveVersion==PeelWorldSave.CurrentVersion ? localPalette : null};
            if(controller && controller.CinemachineCameraTarget)
            {
                data.hasLookPitch=true;
                data.lookPitch=Mathf.DeltaAngle(0,controller.CinemachineCameraTarget.transform.localEulerAngles.x);
            }
            int n=0;
            foreach(var pair in index)
            {
                var stack=pair.Value;var item=new PeelStackSave {id=pair.Key,cleared=stack.ClearedPosters,strips=new float[4]};
                if(data.noticePalette!=null)item.prints=stack.ShiftDesigns;
                if(stack.RemainingPosters>0)
                {
                    int done=0;
                    for(int i=0;i<4;i++)
                    {
                        var strip=stack.CurrentStrips[i];
                        if(strip.IsRemoved){item.strips[i]=1f;done++;}
                        // A fully lifted part can still be attached while the rest of its sheet catches up.
                        else item.strips[i]=Mathf.Min(strip.progress,.9999999f);
                    }
                    if(done==4){item.cleared++;item.strips=new float[4];}
                }
                data.stacks[n++]=item;
            }
            if(!PeelWorldSave.Validate(data,sceneId,index,MapPalette(data.noticePalette),out var error))
            {Debug.LogError("World save rejected: "+error,context);saveBlocked=true;return false;}
            try
            {
                // Do not rotate a damaged primary over the only readable backup.
                if(recoveryPending){PeelWorldSave.Recover(WorldSavePath);recoveryPending=false;}
                PeelWorldSave.Write(WorldSavePath,data);
            }
            catch(System.Exception e){saveWriteFailed=true;Debug.LogError("Could not write world save: "+e.Message,context);return false;}

            saveWriteFailed=false;saveSucceeded=true;
            // Publish the primary first. A failed first move must never leave a new state in the backup.
            CheckpointBackupPending=false;
            if(checkpointBackup || !File.Exists(WorldSavePath+".bak"))
            {
                try { PeelWorldSave.CheckpointBackup(WorldSavePath); }
                catch(System.Exception e)
                {
                    CheckpointBackupPending=true;
                    Debug.LogWarning("Could not update the checkpoint backup: "+e.Message,context);
                }
            }
            return true;
        }
        SaveEntry InspectWorld(out PeelWorldSave data, out Dictionary<string,PeelPosterStack> index)
        {
            data=null;index=null;
            if(!File.Exists(WorldSavePath) && !File.Exists(WorldSavePath+".bak"))return SaveEntry.New;
            if(!SaveIndex(out index))return SaveEntry.Unreadable;
            try { data=ReadValidatedWorld(WorldSavePath,index);return SaveEntry.Continue; }
            catch(System.NotSupportedException) { return SaveEntry.Incompatible; }
            catch(System.Exception)
            {
                try { data=ReadValidatedWorld(WorldSavePath+".bak",index);return SaveEntry.Backup; }
                catch(System.NotSupportedException) { return SaveEntry.Incompatible; }
                catch(System.Exception) { return SaveEntry.Unreadable; }
            }
        }
        internal bool Load(Action requestRestart, Action<PeelWorldSave,int,int> applyProgress, Action<PeelWorldSave> restorePlayer)
        {
            inspectedSavePath=WorldSavePath;
            saveEntry=InspectWorld(out var data,out var index);
            InspectedSave=data;
            InspectedShiftFinished=IsFinished(data);
            if(saveEntry==SaveEntry.New)return true;
            if(saveEntry==SaveEntry.Unreadable || saveEntry==SaveEntry.Incompatible){requestRestart();return false;}
            if(saveEntry==SaveEntry.Backup)
            {
                recoveryPending=true;
                try { PeelWorldSave.Recover(WorldSavePath);recoveryPending=false; }
                catch(System.Exception e){saveWriteFailed=true;Debug.LogError("Could not restore backup to disk: "+e.Message,context);}
                RecoveryNoticeUntil=Time.unscaledTime+10f;
            }
            return Restore(data,index,applyProgress,restorePlayer);
        }

        internal bool LoadArchiveCopy(PeelSaveCoordinator archive, Action<PeelWorldSave,int,int> applyProgress, Action<PeelWorldSave> restorePlayer)
        {
            if(File.Exists(WorldSavePath) || File.Exists(WorldSavePath+".bak"))return false;
            var entry=archive.InspectWorld(out var data,out var index);
            if(entry!=SaveEntry.Continue && entry!=SaveEntry.Backup)return false;
            data.paper=new PeelPaperSave {capacity=PeelPaperSave.StartingCapacity,scraperOwned=data.toolLevel>0};
            return Restore(data,index,applyProgress,restorePlayer);
        }

        bool Restore(PeelWorldSave data,Dictionary<string,PeelPosterStack> index,
            Action<PeelWorldSave,int,int> applyProgress,Action<PeelWorldSave> restorePlayer)
        {
            try
            {
                var printMap=MapPalette(data.noticePalette);
                foreach(var stack in stacks)
                    stack.ShiftDesigns=data.version==PeelWorldSave.PavilionHubLayoutVersion
                        ? hub.LegacyPrints(stack,localPalette.Length) : null;
                int savedRemoved=0,savedCleared=0;
                foreach(var item in data.stacks)
                {
                    var stack=index[item.id];
                    if(data.version==PeelWorldSave.CurrentVersion)
                        stack.ShiftDesigns=Array.ConvertAll(item.prints,print => printMap[print]);
                    stack.RestoreProgress(item.cleared,item.strips);
                    if(basementLight)basementLight.RefreshPosterMaterials(stack);
                    savedCleared+=item.cleared;savedRemoved+=item.cleared*4;
                    foreach(float p in item.strips)if(p>=1)savedRemoved++;
                }
                if(data.stacks.Length<index.Count)
                {
                    var restored=new HashSet<string>(Array.ConvertAll(data.stacks,item => item.id));
                    foreach(var pair in index)
                        if(!restored.Contains(pair.Key))pair.Value.RestoreProgress(0,new float[4]);
                }
                applyProgress(data,savedRemoved,savedCleared);
                if(routePrototype)routePrototype.RestoreProgress(data.firstDoorOpen);
                if(basementLight)basementLight.RestoreProgress(data.lightOn);
                if(routePrototype)routePrototype.RestoreBasementGlass(data.basementGlassSeen);
                if(pavilion)pavilion.SetLight(data.lightOn);
                Physics.SyncTransforms();
                restorePlayer(data);
                saveVersion=data.version;
                return true;
            }
            catch(System.Exception e)
            {
                saveBlocked=true;Debug.LogError("World save could not be loaded; the file was preserved: "+e.Message,context);
                saveEntry=SaveEntry.Unreadable;return false;
            }
        }
        PeelWorldSave ReadValidatedWorld(string path,Dictionary<string,PeelPosterStack> index)
        {
            var data=PeelWorldSave.Read(path);
            if(sceneId==PeelWorldSave.RouteScene && routePrototype && data!=null && data.version==1 && data.scene==PeelWorldSave.LegacyRouteScene)
            {
                routePrototype.MigrateCourtyardSave(data);
                data.version=2;data.scene=PeelWorldSave.RouteScene;
            }
            if(sceneId==PeelWorldSave.RouteScene && routePrototype && data!=null && data.version==2 && data.scene==PeelWorldSave.RouteScene)
                routePrototype.MigrateInteriorSave(data);
            if(data!=null && (data.version>PeelWorldSave.CurrentVersion || (data.version>0 && data.scene!=sceneId) ||
                (data.version==PeelWorldSave.PavilionHubLayoutVersion && sceneId!=PeelWorldSave.PavilionHubScene)))
                throw new System.NotSupportedException("Unsupported save version or route.");
            if(!PeelWorldSave.Validate(data,sceneId,index,MapPalette(data?.noticePalette),out var error))
                throw new System.IO.InvalidDataException(error);
            if(routePrototype && !routePrototype.CanRestoreProgress(data))throw new System.IO.InvalidDataException("Open door with uncleared wall");
            return data;
        }

        int[] MapPalette(string[] palette) => palette==null ? null :
            Array.ConvertAll(palette,id => Array.IndexOf(localPalette,id));
    }
}
