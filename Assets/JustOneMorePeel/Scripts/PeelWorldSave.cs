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
    }

    [Serializable] public sealed class PeelWorldSave
    {
        public const int CurrentVersion = 3;
        public const string RouteScene = "three-area";
        public const string LegacyRouteScene = "Assets/JustOneMorePeel/Scenes/ThreeAreaPrototype.unity";
        public int version;
        public string scene;
        public int money, toolLevel, activeTool;
        public bool steamUnlocked, steamExpanded, firstDoorOpen, lightOn;
        public float elapsed;
        public bool hasLookPitch;
        public float lookPitch;
        public Vector3 position;
        public Quaternion rotation;
        public PeelStackSave[] stacks;

        public static string DefaultPath => Path.Combine(Application.persistentDataPath,"three-area-save-v1.json");

        public static bool Validate(PeelWorldSave save, string scene,
            Dictionary<string,PeelPosterStack> index, out string error)
        {
            error="Invalid or incompatible save";
            if(save==null || save.version!=CurrentVersion || save.scene!=scene || save.stacks==null || save.stacks.Length!=index.Count ||
                save.money<0 || save.toolLevel<0 || save.toolLevel>3 || save.activeTool<0 || save.activeTool>1 ||
                (save.steamUnlocked && save.toolLevel==0) || (save.steamExpanded && !save.steamUnlocked) ||
                (save.activeTool==1 && !save.steamUnlocked) || !Finite(save.elapsed) || save.elapsed<0 ||
                !Finite(save.position.x) || !Finite(save.position.y) || !Finite(save.position.z) ||
                !Finite(save.rotation.x) || !Finite(save.rotation.y) || !Finite(save.rotation.z) || !Finite(save.rotation.w) ||
                (save.hasLookPitch && (!Finite(save.lookPitch) || save.lookPitch < -180f || save.lookPitch > 180f)))return false;
            float q=save.rotation.x*save.rotation.x+save.rotation.y*save.rotation.y+save.rotation.z*save.rotation.z+save.rotation.w*save.rotation.w;
            if(q<.9f || q>1.1f)return false;
            var seen=new HashSet<string>();
            foreach(var item in save.stacks)
            {
                if(item==null || string.IsNullOrEmpty(item.id) || !seen.Add(item.id) || !index.TryGetValue(item.id,out var stack) ||
                    item.cleared<0 || item.cleared>stack.PosterCount || !ValidStrips(item.strips,item.cleared==stack.PosterCount))return false;
            }
            error=null;return true;
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
            else
            {
                if(!File.Exists(path+".bak"))File.Copy(temporary,path+".bak");
                File.Move(temporary,path);
            }
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
