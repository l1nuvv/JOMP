using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JustOneMorePeel
{
    // Run only in a disposable, freshly entered Play Mode session.
    public static class PeelProgressionChecks
    {
        const string Scene = "Assets/JustOneMorePeel/Scenes/ThreeAreaPrototype.unity";
        public static string Run()
        {
            if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != Scene)
                throw new InvalidOperationException("Fresh ThreeAreaPrototype Play Mode is required.");
            var game = Object.FindAnyObjectByType<PeelGame>();
            var route = Object.FindAnyObjectByType<PeelRoutePrototype>();
            if (!game || !route || route.ReviewMode || game.CurrentScreen != PeelScreen.Intro || game.removed != 0)
                throw new InvalidOperationException("Fresh normal Intro is required; no existing session is modified.");
            if (!string.IsNullOrEmpty(PeelGame.WorldSavePathOverride))
                throw new InvalidOperationException("A save override is already active.");

            var cacheField = typeof(PeelGame).GetField("stacks", BindingFlags.NonPublic | BindingFlags.Instance);
            var cachedStacks = cacheField?.GetValue(game) as System.Collections.IList;
            var sceneStacks = Object.FindObjectsByType<PeelPosterStack>();
            if (cachedStacks == null || cachedStacks.Count == 0 || cachedStacks.Count != sceneStacks.Length ||
                sceneStacks.Sum(s => s.PosterCount) != game.TotalPosters)
                throw new InvalidOperationException("Poster cache is incomplete; stop and restart Play Mode before the smoke.");

            var results = new List<string>();
            Action<string, bool> check = (name, ok) => results.Add((ok ? "PASS " : "FAIL ") + name);
            var sessionField = typeof(PeelGame).GetField("worldSession", BindingFlags.NonPublic | BindingFlags.Instance);
            var saveMethod = typeof(PeelGame).GetMethod("SaveWorld", BindingFlags.NonPublic | BindingFlags.Instance);
            if (sessionField == null || saveMethod == null) throw new InvalidOperationException("Save API changed.");
            string dir = Path.Combine(Path.GetTempPath(), "JOMP-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "world.json");
            PeelGame.WorldSavePathOverride = path;
            try
            {
                var stacks = Object.FindObjectsByType<PeelPosterStack>();
                check("Route scene and initial clean state", stacks.Length > 0 && game.TotalPosters == 700 && game.money == 0);
                game.BeginShift();
                check("Clean isolated session begins", game.CurrentScreen == PeelScreen.Playing && game.removed == 0 && game.money == 0 && !File.Exists(path));
                var stack = stacks.First(s => s.RemainingPosters > 1 && s.CurrentStrips.All(x => x.IsExposed));
                var strips = (PeelStrip[])stack.CurrentStrips.Clone();
                int beforeMoney = game.money, beforeRemoved = game.removed, beforeCleared = stack.ClearedPosters;
                strips[0].Pull(1f);
                check("One strip pays $2 once", game.money == beforeMoney + 2 && game.removed == beforeRemoved + 1);
                strips[0].Pull(1f);
                check("Removed strip cannot pay twice", game.money == beforeMoney + 2 && game.removed == beforeRemoved + 1);
                for (int i = 1; i < strips.Length; i++) strips[i].Pull(1f);
                check("Four strips pay $8", game.money == beforeMoney + 8 && game.removed == beforeRemoved + 4);
                stack.AdvanceIfComplete();
                check("Layer advances and exposes next sheet", stack.ClearedPosters == beforeCleared + 1 && stack.CurrentStrips.All(x => x.IsExposed));
                saveMethod.Invoke(game, null);
                check("Isolated save and initial backup created", File.Exists(path) && File.Exists(path + ".bak"));
                if (File.Exists(path))
                {
                    var saved = PeelWorldSave.Read(path);
                    var entry = saved.stacks.FirstOrDefault(s => s.id == stack.PersistentId);
                    check("Save contains payout and layer", saved.scene == PeelWorldSave.RouteScene && saved.version == PeelWorldSave.CurrentVersion && saved.money == beforeMoney + 8 && entry != null && entry.cleared == beforeCleared + 1);
                }
                else check("Save contains payout and layer", false);
                saveMethod.Invoke(game, null);
                check("Separate backup remains readable", File.Exists(path + ".bak") && PeelWorldSave.Read(path + ".bak").scene == PeelWorldSave.RouteScene);
                CheckSaveReliability(game, route, stack, path, saveMethod, check);
            }
            catch (Exception e) { results.Add("FAIL Exception: " + e); }
            finally
            {
                // Prevent subsequent focus/autosave callbacks from ever using the player's path.
                sessionField.SetValue(game, false);
                PeelGame.WorldSavePathOverride = null;
            }
            Directory.CreateDirectory("Logs");
            results.Add("Isolated files: " + dir);
            File.WriteAllLines("Logs/Peel-progression-checks.txt", results);
            return string.Join("\n", results) + "\nTOTAL: " + results.Count(s => s.StartsWith("PASS ")) + " PASS, " + results.Count(s => s.StartsWith("FAIL ")) + " FAIL";
        }

        static void CheckSaveReliability(PeelGame game, PeelRoutePrototype route, PeelPosterStack stack,
            string path, MethodInfo save, Action<string, bool> check)
        {
            const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
            var load = typeof(PeelGame).GetMethod("LoadWorld", Private);
            var screen = typeof(PeelGame).GetMethod("SetScreen", Private);
            var controller = game.player.GetComponentInChildren<StarterAssets.FirstPersonController>();
            var light = Object.FindAnyObjectByType<PeelBasementLight>();
            var spawn = game.player.transform.position;

            stack.CurrentStrips[0].Pull(.25f);
            var partial = stack.CurrentStrips[0].progress;
            controller.RestoreLookPitch(-17);
            game.player.transform.rotation = Quaternion.Euler(0, 35, 0);
            save.Invoke(game, null);
            save.Invoke(game, null);
            string good = File.ReadAllText(path);
            check("Partial strip saved", partial > 0 && partial < 1 &&
                PeelWorldSave.Read(path).stacks.Single(s => s.id == stack.PersistentId).strips[0] == partial);

            Directory.CreateDirectory(path + ".tmp");
            save.Invoke(game, null);
            check("Blocked temporary write leaves both copies intact",
                File.ReadAllText(path) == good && File.ReadAllText(path + ".bak") == good);
            Directory.Delete(path + ".tmp");

            screen.Invoke(game, new object[] { PeelScreen.Paused });
            game.RequestRestart();
            using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                game.RestartShift();
            check("Reset failure keeps confirmation and both copies", game.CurrentScreen == PeelScreen.ConfirmRestart &&
                File.ReadAllText(path) == good && File.ReadAllText(path + ".bak") == good);
            game.Back();
            check("Cancel returns to pause and preserves progress", game.CurrentScreen == PeelScreen.Paused &&
                File.ReadAllText(path) == good);

            var unsupported = PeelWorldSave.Read(path);
            unsupported.version++;
            string future = JsonUtility.ToJson(unsupported);
            File.WriteAllText(path, future);
            check("Unsupported primary preserved even with valid backup",
                !(bool)load.Invoke(game, null) && File.ReadAllText(path) == future && File.ReadAllText(path + ".bak") == good);
            File.WriteAllText(path, "{broken");
            Directory.CreateDirectory(path + ".tmp");
            game.money = 0;
            game.player.transform.SetPositionAndRotation(spawn + Vector3.up, Quaternion.identity);
            controller.RestoreLookPitch(0);
            check("Readable backup loads even when recovery cannot write",
                (bool)load.Invoke(game, null) && game.money == PeelWorldSave.Read(path + ".bak").money);
            save.Invoke(game, null);
            check("Failed recovery does not rotate away the good backup", File.ReadAllText(path + ".bak") == good);
            Directory.Delete(path + ".tmp");
            check("Corrupt primary recovered and copied aside", (bool)load.Invoke(game, null) &&
                File.ReadAllText(path) == good && Directory.GetFiles(Path.GetDirectoryName(path), "world.json.damaged-*")
                    .Any(file => File.ReadAllText(file) == "{broken"));
            check("Partial progress, position, yaw and pitch restored",
                stack.CurrentStrips[0].progress == partial && Vector3.Distance(game.player.transform.position, spawn) < .01f &&
                Mathf.Abs(Mathf.DeltaAngle(game.player.transform.eulerAngles.y, 35)) < .1f &&
                Mathf.Abs(Mathf.DeltaAngle(controller.CinemachineCameraTarget.transform.localEulerAngles.x, -17)) < .1f);

            var legacy = PeelWorldSave.Read(path);
            legacy.version = 1;
            legacy.scene = Scene;
            var door = (PeelPosterStack[])typeof(PeelRoutePrototype).GetField("doorPosters", Private).GetValue(route);
            var omitted = new HashSet<string>(door.Skip(1).Take(9).Select(s => s.PersistentId));
            legacy.stacks = legacy.stacks.Where(s => !omitted.Contains(s.id)).ToArray();
            var first = legacy.stacks.Single(s => s.id == door[0].PersistentId);
            first.cleared = 6;
            first.strips = new[] { .35f, 0, 0, 0 };
            File.WriteAllText(path, JsonUtility.ToJson(legacy));
            check("Legacy grouped door IDs migrate with the partial strip",
                (bool)load.Invoke(game, null) && door[0].ClearedPosters == 5 &&
                door[1].ClearedPosters == 1 && door[1].CurrentStrips[0].progress == .35f);

            var opened = JsonUtility.FromJson<PeelWorldSave>(good);
            opened.version = 2;
            var basement = StacksFor(route, "basement").ToDictionary(s => s.PersistentId);
            var goal = door.Concat(basement.Values).ToDictionary(s => s.PersistentId);
            foreach (var entry in opened.stacks)
            {
                if (!goal.TryGetValue(entry.id, out var target)) continue;
                entry.cleared = basement.ContainsKey(entry.id) ? target.PosterCount / 2 : target.PosterCount;
                entry.strips = new float[4];
            }
            opened.firstDoorOpen = true;
            opened.lightOn = true;
            opened.position = spawn + Vector3.up * 1000;
            File.WriteAllText(path, JsonUtility.ToJson(opened));
            check("V2 doors and light restored with fallback from unsupported position",
                (bool)load.Invoke(game, null) && route.CourtyardDoorOpen && route.BasementDoorOpen &&
                light.IsOn && Vector3.Distance(game.player.transform.position, spawn) < .01f);
            PeelWorldSave.Reset(path);
            check("Successful reset removes both live copies", !File.Exists(path) && !File.Exists(path + ".bak"));
        }

        static PeelPosterStack[] StacksFor(PeelRoutePrototype route, string field) =>
            (PeelPosterStack[])typeof(PeelRoutePrototype).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(route);
    }
}
