using UnityEngine;

namespace JustOneMorePeel
{
    public sealed class PeelPavilion : MonoBehaviour
    {
        [SerializeField] private Collider lightSwitch;
        [SerializeField] private GameObject backlight;
        [SerializeField] private TextMesh pavilionSign;
        [SerializeField] private TextMesh workOrder;

        internal bool IsLit => backlight.activeSelf;

        internal bool CanTurnOn(Camera view, int remaining) =>
            remaining == 0 && !IsLit &&
            Physics.Raycast(view.transform.position, view.transform.forward, out var hit, 2.2f) &&
            hit.collider == lightSwitch;

        internal void SetLight(bool on) => backlight.SetActive(on);

        internal void ApplyLanguage(PeelLanguage language)
        {
            bool ru = language == PeelLanguage.Russian;
            pavilionSign.text = ru ? "ДВОРОВЫЙ ПАВИЛЬОН" : "COURTYARD PAVILION";
            workOrder.text = ru ? "ЗАЯВКА №17\nОЧИСТИТЬ ВИТРИНУ" : "WORK ORDER 17\nCLEAR THE DISPLAY";
        }
    }
}
