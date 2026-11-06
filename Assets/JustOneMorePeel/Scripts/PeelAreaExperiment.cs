using UnityEngine;

namespace JustOneMorePeel
{
    // Temporary A03 blockout. World persistence and a final ending belong to later milestones.
    [DefaultExecutionOrder(50)]
    public sealed class PeelAreaExperiment : MonoBehaviour
    {
        [SerializeField] PeelGame game;
        [SerializeField] PeelPosterStack passagePaper;
        [SerializeField] PeelPosterStack[] secondArea;
        [SerializeField] GameObject barrier;
        [SerializeField] TextMesh passageSign;
        [SerializeField] TextMesh revealWriting;
        [SerializeField] float areaBoundaryZ = -6.5f;
        bool opened;

        public int RemainingHere
        {
            get
            {
                int second = 0;
                foreach (var stack in secondArea) if (stack) second += stack.RemainingPosters;
                return game.player.transform.position.z < areaBoundaryZ ? second : game.TotalPosters - game.ClearedPosters - second;
            }
        }
        public string Objective
        {
            get
            {
                bool ru = game.Settings.Language == PeelLanguage.Russian;
                if (game.player.transform.position.z < areaBoundaryZ)
                    return ru ? "Новый участок: очисти старые объявления" : "New area: clear the old notices";
                if (opened) return ru ? "Проход напротив рабочей стены открыт" : "The passage opposite the work wall is open";
                return ru ? "Напротив рабочей стены: очисти объявление у прохода" : "Opposite the work wall: clear the notice at the passage";
            }
        }
        void Start()
        {
            if (!game || !passagePaper || !barrier || secondArea == null || !game.Settings)
            { Debug.LogError("A03: missing passage references", this); enabled = false; return; }
            game.PosterWasCleared += OnPosterCleared;
            game.Settings.Changed += RefreshSign;
            RefreshSign();
        }
        void OnDestroy()
        {
            if (game) { game.PosterWasCleared -= OnPosterCleared; if (game.Settings) game.Settings.Changed -= RefreshSign; }
        }
        void OnPosterCleared(PeelPosterStack stack)
        {
            if (opened || stack != passagePaper || stack.RemainingPosters != 0) return;
            opened = true;
            barrier.SetActive(false);
            Physics.SyncTransforms();
            RefreshSign();
        }
        void RefreshSign()
        {
            if (!game || !game.Settings) return;
            bool ru = game.Settings.Language == PeelLanguage.Russian;
            if (passageSign) passageSign.text = opened ? (ru ? "ПРОХОД ОТКРЫТ" : "PASSAGE OPEN") : (ru ? "ОЧИСТИ ОБЪЯВЛЕНИЕ" : "CLEAR THE NOTICE");
            if (revealWriting) revealWriting.text = game.Settings.Text(PeelText.Secret);
        }
    }
}
