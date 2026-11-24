using System.Collections.Generic;
using UnityEngine;

namespace JustOneMorePeel
{
    public sealed class PeelPavilionHub : MonoBehaviour
    {
        [SerializeField] private PeelWorkbench localDropOff;
        [SerializeField] private Collider cabinet;
        [SerializeField] private TextMesh entranceSign;
        [SerializeField] private TextMesh shopSign;
        [SerializeField] private TextMesh cabinetSign;

        // Frozen mapping for saves written by the original PavilionHub layout.
        // Rows are bays 1–4 and entries are paper palette indices for print01 onward.
        static readonly Dictionary<string, int> LowerPrints = BuildLowerPrints();

        internal PeelWorkbench LocalDropOff => localDropOff;
        internal PeelNoticeCollection Collection { get; private set; }

        static Dictionary<string, int> BuildLowerPrints()
        {
            int[][] byBay =
            {
                new[] { 22, 5, 24, 14, 26, 15, 9, 2, 24, 13, 6, 29, 5 },
                new[] { 31, 13, 7, 29, 22, 26, 14, 24, 15, 31 },
                new[] { 24, 31, 28, 13, 21, 9, 7, 15, 11, 16, 22, 14 },
                new[] { 9, 22, 2, 19, 24, 15, 6, 16, 13, 16, 0, 7 }
            };
            var prints = new Dictionary<string, int>();
            for (int bay = 0; bay < byBay.Length; bay++)
                for (int print = 0; print < byBay[bay].Length; print++)
                    prints.Add($"pavilion.bay{bay + 1}.print{print + 1:00}", byBay[bay][print]);
            return prints;
        }

        internal int[] LegacyPrints(PeelPosterStack stack, int paperCount) =>
            stack.PosterCount == 2 && LowerPrints.TryGetValue(stack.PersistentId, out int design)
                ? new[] { stack.AuthoredDesignAt(0, paperCount), design }
                : null;

        internal void InitializeCollection(PeelSettings settings, IReadOnlyList<PeelPosterStack> stacks)
        {
            Collection = new PeelNoticeCollection(settings, stacks);
            Collection.Restore(stacks);
        }

        internal bool CanReadCabinet(Camera view) =>
            Physics.Raycast(view.transform.position, view.transform.forward, out var hit, 2.2f) &&
            hit.collider == cabinet;

        internal static string CabinetTitle(bool ru) => ru ? "Объявления нашего двора" : "Our courtyard notices";

        internal void ApplyLanguage(PeelSettings settings)
        {
            bool ru = settings.Language == PeelLanguage.Russian;
            entranceSign.text = ru ? "ПРИЁМНАЯ" : "RECEPTION";
            shopSign.text = ru ? "ИНСТРУМЕНТЫ" : "TOOLS";
            cabinetSign.text = ru ? "Объявления\nнашего двора" : "Our courtyard\nnotices";
            localDropOff.RefreshPaperDropOffLabel(settings, ru ? "СДАЧА" : "DROP-OFF");
        }
    }
}
