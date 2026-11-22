using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JustOneMorePeel
{
    [InitializeOnLoad]
    static class PeelTimingPlaytest
    {
        const string MenuPath = "Just One More Peel/Start isolated timing playtest";
        const string RouteScene = "Assets/JustOneMorePeel/Scenes/ThreeAreaPrototype.unity";
        const string SavePathKey = "JustOneMorePeel.TimingPlaytest.SavePath";
        const string TimingLogName = "timing.log";

        static PeelTimingPlaytest()
        {
            // Entering Play Mode can reload the domain before the game's Awake reads the save path.
            string path = SessionState.GetString(SavePathKey, string.Empty);
            if (!string.IsNullOrEmpty(path))
                ApplyTimingSession(path);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Application.logMessageReceived += RecordMilestone;
        }

        [MenuItem(MenuPath, true)]
        static bool CanStart()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling
                && SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().path == RouteScene
                && string.IsNullOrEmpty(PeelGame.WorldSavePathOverride);
        }

        [MenuItem(MenuPath)]
        static void Start()
        {
            if (!CanStart())
                return;

            var run = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
            var directory = Directory.CreateDirectory(Path.Combine("Logs", "PR-06", run));
            var path = Path.Combine(directory.FullName, "world.json");
            File.WriteAllText(Path.Combine(directory.FullName, TimingLogName), string.Empty);
            SessionState.SetString(SavePathKey, path);
            ApplyTimingSession(path);
            EditorApplication.EnterPlaymode();
        }

        static void ApplyTimingSession(string savePath)
        {
            PeelGame.WorldSavePathOverride = savePath;
            PeelGame.IsTimingPlaytest = true;
        }

        static void RecordMilestone(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Log || !PeelGame.IsTimingPlaytest || !message.StartsWith("[QA-08] ", StringComparison.Ordinal))
                return;

            string path = SessionState.GetString(SavePathKey, string.Empty);
            if (string.IsNullOrEmpty(path) || PeelGame.WorldSavePathOverride != path)
                return;

            try
            {
                File.AppendAllText(Path.Combine(Path.GetDirectoryName(path), TimingLogName), message + Environment.NewLine);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[PR-06] Could not write {TimingLogName}: {error.Message}");
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            string path = SessionState.GetString(SavePathKey, string.Empty);
            if (string.IsNullOrEmpty(path))
                return;

            if (state == PlayModeStateChange.EnteredPlayMode)
                Debug.Log($"[PR-06] Isolated timing playtest. Save: {path}. Milestones: {Path.Combine(Path.GetDirectoryName(path), TimingLogName)}");

            // Keep the override through shutdown so the final autosave stays isolated.
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (PeelGame.WorldSavePathOverride == path)
                    PeelGame.WorldSavePathOverride = null;
                PeelGame.IsTimingPlaytest = false;
                SessionState.EraseString(SavePathKey);
            }
        }
    }
}
