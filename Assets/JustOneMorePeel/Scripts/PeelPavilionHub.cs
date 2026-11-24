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

        internal PeelWorkbench LocalDropOff => localDropOff;

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
            localDropOff.RefreshPaperDropOffLabel(settings);
        }
    }
}
