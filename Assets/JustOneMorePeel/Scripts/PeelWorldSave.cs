using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JustOneMorePeel
{
    [Serializable] public sealed class PeelStackSave
    {
        public string id;
        public int cleared;
        public float[] strips;
        public int[] prints;
    }

    [Serializable] public struct PeelPaperSave
    {
        public const int StartingCapacity=5;
        public int capacity, items, value;
        public bool scraperOwned;
    }

    [Serializable] public sealed class PeelWorldSave
    {
        public const int CurrentVersion = 5;
        internal const int AuthoredLayoutVersion = 3;
        internal const int PavilionHubLayoutVersion = 4;
        public const string RouteScene = "three-area";
        public const string ArchiveIssueScene = "main-archive-issue";
        public const string PaperCollectionScene = "main-archive-paper";
        public const string MainRouteScene = "main-route";
        public const string PavilionScene = "pavilion";
        public const string PavilionHubScene = "pavilion-hub";
        public const string LegacyRouteScene = "Assets/JustOneMorePeel/Scenes/ThreeAreaPrototype.unity";
        public int version;
        public string scene;
        public int money, toolLevel, activeTool;
        public bool steamUnlocked, steamExpanded, firstDoorOpen, lightOn;
        public bool basementGlassSeen;
        public float elapsed;
        public bool hasLookPitch;
        public float lookPitch;
        public Vector3 position;
        public Quaternion rotation;
        public PeelStackSave[] stacks;
        public string[] noticePalette;
        public PeelPaperSave paper;

        public static string DefaultPath => Path.Combine(Application.persistentDataPath,"three-area-save-v1.json");

        internal static bool Validate(PeelWorldSave save, string scene,
            Dictionary<string,PeelPosterStack> index, int[] printMap, out string error)
        {
            error="Invalid or incompatible save";
            bool paperTrial=scene==PaperCollectionScene;
            bool archive=scene==ArchiveIssueScene || paperTrial;
            bool pavilion=scene==PavilionScene || scene==PavilionHubScene;
            bool carriesPaper=paperTrial || scene==PavilionHubScene;
            if(save==null || (save.version!=CurrentVersion && save.version!=AuthoredLayoutVersion &&
                !(scene==PavilionHubScene && save.version==PavilionHubLayoutVersion)) || save.scene!=scene || save.stacks==null ||
                save.money<0 || save.toolLevel<0 || save.toolLevel>3 || save.activeTool<0 || save.activeTool>PeelGame.CardToolSlot ||
                (save.activeTool==PeelGame.CardToolSlot && ((scene!=MainRouteScene && !pavilion) || save.toolLevel==0)) ||
                (save.steamUnlocked && save.toolLevel==0) || (save.steamExpanded && !save.steamUnlocked) ||
                (archive && (save.toolLevel>1 || save.steamUnlocked)) ||
                (save.activeTool==1 && (archive ? save.toolLevel!=1 : !save.steamUnlocked)) ||
                (carriesPaper && (!ValidPaper(save.paper) || (save.toolLevel>0 && !save.paper.scraperOwned))) ||
                !Finite(save.elapsed) || save.elapsed<0 ||
                !Finite(save.position.x) || !Finite(save.position.y) || !Finite(save.position.z) ||
                !Finite(save.rotation.x) || !Finite(save.rotation.y) || !Finite(save.rotation.z) || !Finite(save.rotation.w) ||
                (save.hasLookPitch && (!Finite(save.lookPitch) || save.lookPitch < -180f || save.lookPitch > 180f)))return false;
            float q=save.rotation.x*save.rotation.x+save.rotation.y*save.rotation.y+save.rotation.z*save.rotation.z+save.rotation.w*save.rotation.w;
            if(q<.9f || q>1.1f)return false;
            var seen=new HashSet<string>();
            int removed=0;
            foreach(var item in save.stacks)
            {
                if(item==null || string.IsNullOrEmpty(item.id) || !seen.Add(item.id) || !index.TryGetValue(item.id,out var stack) ||
                    item.cleared<0 || item.cleared>stack.PosterCount || !ValidStrips(item.strips,item.cleared==stack.PosterCount))return false;
                if(save.version==CurrentVersion && !ValidPrints(item.prints,stack.PosterCount,printMap))return false;
                if(pavilion && save.lightOn && item.cleared!=stack.PosterCount)
                {error="Pavilion light is on with paper still attached";return false;}
                if(carriesPaper)
                {
                    removed+=item.cleared*4;
                    foreach(float p in item.strips)if(p==1f)removed++;
                }
            }
            if(carriesPaper && (seen.Count!=index.Count || save.paper.value>removed*PeelProgressionController.StripValue))return false;
            if(seen.Count!=index.Count)
            {
                // Older archive shifts ended at issue (2 stacks) or sorting (5 stacks).
                if(save.version!=AuthoredLayoutVersion || !archive || !seen.Contains("archive.issue.precision") || !seen.Contains("archive.issue.wide"))return false;
                if(seen.Count!=2 && !(seen.Count==5 && seen.Contains("archive.sorting.rounds") &&
                    seen.Contains("archive.sorting.parcels") && seen.Contains("archive.sorting.warning")))return false;
            }
            error=null;return true;
        }
        static bool ValidPrints(int[] prints,int posterCount,int[] printMap)
        {
            if(prints==null || prints.Length!=posterCount || printMap==null)return false;
            foreach(int print in prints)
                if(print<0 || print>=printMap.Length || printMap[print]<0)return false;
            return true;
        }
        static bool ValidPaper(PeelPaperSave paper)
        {
            const int rate=PeelProgressionController.StripValue;
            return paper.capacity is 5 or 10 or 15 or 20 && paper.items>=0 && paper.items<=paper.capacity
                && paper.value>=paper.items*rate && paper.value<=paper.items*4*rate && paper.value%rate==0;
        }
        static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        internal static bool ValidStrips(float[] strips,bool completed)
        {
            if(strips==null || strips.Length!=4)return false;
            int finished=0;
            foreach(float progress in strips)
            {
                if(!Finite(progress) || progress<0 || progress>1 || (completed && progress!=0))return false;
                if(progress==1)finished++;
            }
            return finished!=4;
        }

        public static void Write(string path, PeelWorldSave save)
        {
            string temporary=path+".tmp";
            File.WriteAllText(temporary,JsonUtility.ToJson(save));
            if(File.Exists(path))File.Replace(temporary,path,path+".bak");
            else File.Move(temporary,path);
        }

        internal static void CheckpointBackup(string path)
        {
            string backup=path+".bak";
            string temporary=path+".tmp";
            File.Copy(path,temporary,true);
            if(File.Exists(backup))File.Replace(temporary,backup,null);
            else File.Move(temporary,backup);
        }

        public static void Reset(string path)
        {
            string backup=path+".bak";
            string staged=backup+".reset-"+Guid.NewGuid().ToString("N");
            bool hasBackup=File.Exists(backup);
            if(hasBackup)File.Move(backup,staged);
            try
            {
                File.Delete(path);
            }
            catch
            {
                if(hasBackup)File.Move(staged,backup);
                throw;
            }
            if(hasBackup)
            {
                try { File.Delete(staged); }
                catch(IOException e) { Debug.LogWarning("Could not remove staged save: "+e.Message); }
                catch(UnauthorizedAccessException e) { Debug.LogWarning("Could not remove staged save: "+e.Message); }
            }
        }
        // Keep a damaged original before atomically replacing it with the verified backup.
        public static void Recover(string path)
        {
            string temporary=path+".tmp";
            File.Copy(path+".bak",temporary,true);
            if(File.Exists(path))File.Replace(temporary,path,path+".damaged-"+Guid.NewGuid().ToString("N"));
            else File.Move(temporary,path);
        }
        public static PeelWorldSave Read(string path)
        {
            var file=new FileInfo(path);
            if(file.Length>2*1024*1024)throw new InvalidDataException("Save is too large");
            return JsonUtility.FromJson<PeelWorldSave>(File.ReadAllText(path));
        }
    }
}
