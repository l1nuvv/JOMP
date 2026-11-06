using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JustOneMorePeel
{
    public static class PeelRouteAuthoringCheck
    {
        [MenuItem("Just One More Peel/Validate route authoring")]
        public static void Report()
        {
            var issues = Inspect();
            if (issues.Length == 0)
                Debug.Log("Peel route authoring: no issues found.");
            else
                Debug.LogError(string.Join("\n", issues));
        }

        public static string[] Inspect()
        {
            var scene = SceneManager.GetActiveScene();
            var routes = Components<PeelRoutePrototype>(scene);
            if (routes.Length != 1)
                return new[] { "Expected one peel route in " + scene.name };

            var issues = new List<string>();
            var route = new SerializedObject(routes[0]);
            var game = route.FindProperty("game").objectReferenceValue as PeelGame;
            var settings = game ? game.GetComponent<PeelSettings>() : null;
            int paperCount = CheckMaterials(settings, issues);
            var courtyard = ReadGroup(route, "courtyard", issues);
            var door = ReadGroup(route, "doorPosters", issues);
            var basement = ReadGroup(route, "basement", issues);
            var hall = ReadGroup(route, "hall", issues);
            foreach (var gate in new[] { "courtyardDoor", "basementDoor", "finalReveal" })
            {
                var target = route.FindProperty(gate).objectReferenceValue as GameObject;
                if (!target || target.scene != scene)
                    issues.Add("Missing or external route object: " + gate);
            }

            if (door.Except(courtyard).Any())
                issues.Add("Every door stack must also belong to the courtyard.");
            var registered = new HashSet<PeelPosterStack>(courtyard);
            foreach (var stack in basement.Concat(hall))
                if (!registered.Add(stack))
                    issues.Add(stack.name + ": belongs to more than one chapter.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var stack in Components<PeelPosterStack>(scene))
            {
                if (!stack.gameObject.activeInHierarchy && !registered.Contains(stack))
                    continue;
                if (string.IsNullOrWhiteSpace(stack.PersistentId) || !ids.Add(stack.PersistentId))
                    issues.Add(stack.name + ": missing or duplicate persistent ID.");
                if (!registered.Contains(stack))
                    issues.Add(stack.name + ": not registered with a route chapter.");
                if (!stack.isActiveAndEnabled)
                    issues.Add(stack.name + ": disabled in the scene.");

                var config = new SerializedObject(stack);
                foreach (var design in new[] { "firstDesign", "secondDesign" })
                {
                    int index = config.FindProperty(design).intValue;
                    if (index < 0 || index >= paperCount)
                        issues.Add(stack.name + ": invalid " + design + " material index.");
                }
                if (config.FindProperty("sequenceOffset").intValue < 0)
                    issues.Add(stack.name + ": negative sequence offset.");
                var strips = new HashSet<PeelStrip>();
                CheckBuffer(stack, config.FindProperty("front"), strips, issues);
                CheckBuffer(stack, config.FindProperty("back"), strips, issues);
            }
            return issues.ToArray();
        }

        static T[] Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        static int CheckMaterials(PeelSettings settings, List<string> issues)
        {
            if (!settings)
            {
                issues.Add("Missing route game or paper settings.");
                return 0;
            }
            var config = new SerializedObject(settings);
            var russian = config.FindProperty("russianPapers");
            var english = config.FindProperty("englishPapers");
            if (russian.arraySize == 0 || russian.arraySize != english.arraySize)
                issues.Add("Expected equally sized, nonempty RU/EN paper catalogues.");
            foreach (var catalogue in new[] { russian, english })
                for (int i = 0; i < catalogue.arraySize; i++)
                {
                    var material = catalogue.GetArrayElementAtIndex(i).objectReferenceValue as Material;
                    if (!material || !material.mainTexture)
                        issues.Add(catalogue.name + "[" + i + "]: missing material or texture.");
                }
            return Math.Min(russian.arraySize, english.arraySize);
        }

        static HashSet<PeelPosterStack> ReadGroup(SerializedObject route, string field, List<string> issues)
        {
            var group = route.FindProperty(field);
            var stacks = new HashSet<PeelPosterStack>();
            if (group.arraySize == 0)
                issues.Add(field + ": no stacks assigned.");
            for (int i = 0; i < group.arraySize; i++)
            {
                var stack = group.GetArrayElementAtIndex(i).objectReferenceValue as PeelPosterStack;
                if (!stack || stack.gameObject.scene != ((Component)route.targetObject).gameObject.scene || !stacks.Add(stack))
                    issues.Add(field + "[" + i + "]: missing, external or duplicate stack.");
            }
            return stacks;
        }

        static void CheckBuffer(PeelPosterStack stack, SerializedProperty buffer,
            HashSet<PeelStrip> strips, List<string> issues)
        {
            if (buffer.arraySize != 4)
                issues.Add(stack.name + ": " + buffer.name + " must contain four strips.");
            for (int i = 0; i < buffer.arraySize; i++)
            {
                var strip = buffer.GetArrayElementAtIndex(i).objectReferenceValue as PeelStrip;
                if (!strip || strip.GetComponentInParent<PeelPosterStack>() != stack || !strips.Add(strip))
                {
                    issues.Add(stack.name + ": missing, external or duplicate " + buffer.name + " strip " + i);
                    continue;
                }
                var collider = strip.GetComponent<BoxCollider>();
                if (!collider || collider.isTrigger)
                    issues.Add(stack.name + ": " + strip.name + " needs a non-trigger box collider.");
            }
        }
    }
}
