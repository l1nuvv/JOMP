using System;
using System.Collections.Generic;
using UnityEngine;

namespace JustOneMorePeel
{
    internal static class PeelNoticeLayout
    {
        const float NearbyDistance = .9f;

        internal static void Generate(PeelSettings settings, IReadOnlyList<PeelPosterStack> stacks,
            PeelPosterStack archiveAddress, PeelPosterStack[] fixedStacks)
        {
            var slots = new List<(PeelPosterStack stack, int layer, int format)>();
            var usedAsNotice = new bool[settings.PaperCount];
            var counts = new int[settings.PaperCount];
            var formats = new int[settings.PaperCount];
            var plain = new bool[settings.PaperCount];
            for (int i = 0; i < formats.Length; i++)
            {
                formats[i] = settings.PaperAspect(i) < .9f ? 0 : 1;
                plain[i] = settings.PaperVariantId(i) == null || settings.PaperSaveId(i) == "Old paper";
            }
            var fixedPrints = fixedStacks == null ? new HashSet<PeelPosterStack>() : new HashSet<PeelPosterStack>(fixedStacks);
            if (archiveAddress) fixedPrints.Add(archiveAddress);

            var cells = new Dictionary<Vector3Int, List<PeelPosterStack>>();
            foreach (var stack in stacks)
            {
                var cell = CellAt(stack.transform.position);
                if (!cells.TryGetValue(cell, out var patches)) cells.Add(cell, patches = new List<PeelPosterStack>());
                patches.Add(stack);
                stack.ShiftDesigns = new int[stack.PosterCount];
                bool keepPrints = fixedPrints.Contains(stack);
                for (int layer = 0; layer < stack.PosterCount; layer++)
                {
                    int design = stack.AuthoredDesignAt(layer, settings.PaperCount);
                    if (keepPrints || plain[design])
                    {
                        stack.ShiftDesigns[layer] = design;
                        counts[design]++;
                        continue;
                    }
                    usedAsNotice[design] = true;
                    stack.ShiftDesigns[layer] = -1;
                    slots.Add((stack, layer, formats[design]));
                }
            }

            var pools = new[] { new List<int>(), new List<int>() };
            for (int i = 0; i < formats.Length; i++)
            {
                string variant = settings.PaperVariantId(i);
                if (plain[i] ||
                    (!usedAsNotice[i] && (variant == "31_RoundsLog" || variant == "32_ArchiveLoan"))) continue;
                pools[formats[i]].Add(i);
            }

            var neighbors = FindNeighbors(stacks, cells);
            var random = new System.Random();
            var nearbyPrints = new HashSet<int>();
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                int other = random.Next(i + 1);
                (slots[i], slots[other]) = (slots[other], slots[i]);
                var (stack, layer, format) = slots[i];
                nearbyPrints.Clear();
                if (layer > 0) nearbyPrints.Add(stack.ShiftDesigns[layer - 1]);
                if (layer + 1 < stack.PosterCount) nearbyPrints.Add(stack.ShiftDesigns[layer + 1]);
                foreach (var neighbor in neighbors[stack])
                    if (layer < neighbor.PosterCount) nearbyPrints.Add(neighbor.ShiftDesigns[layer]);

                int design = Choose(pools[format], counts, nearbyPrints, random);
                stack.ShiftDesigns[layer] = design;
                counts[design]++;
            }
        }

        static int Choose(List<int> pool, int[] counts, HashSet<int> nearby, System.Random random)
        {
            int best = int.MaxValue, selected = -1, ties = 0;
            foreach (int design in pool)
            {
                // Avoid a repeat if another print is at most one copy ahead.
                int score = counts[design] * 2 + (nearby.Contains(design) ? 3 : 0);
                if (score > best) continue;
                if (score < best) { best = score; ties = 0; }
                if (random.Next(++ties) == 0) selected = design;
            }
            return selected;
        }

        static Vector3Int CellAt(Vector3 position) => Vector3Int.FloorToInt(position / NearbyDistance);

        static Dictionary<PeelPosterStack, List<PeelPosterStack>> FindNeighbors(
            IReadOnlyList<PeelPosterStack> stacks, Dictionary<Vector3Int, List<PeelPosterStack>> cells)
        {
            var result = new Dictionary<PeelPosterStack, List<PeelPosterStack>>(stacks.Count);
            foreach (var stack in stacks)
            {
                var position = stack.transform.position;
                var cell = CellAt(position);
                var neighbors = new List<PeelPosterStack>();
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        for (int z = -1; z <= 1; z++)
                            if (cells.TryGetValue(cell + new Vector3Int(x, y, z), out var candidates))
                                foreach (var candidate in candidates)
                                    if (candidate != stack && (position - candidate.transform.position).sqrMagnitude <= NearbyDistance * NearbyDistance)
                                        neighbors.Add(candidate);
                result.Add(stack, neighbors);
            }
            return result;
        }
    }
}
