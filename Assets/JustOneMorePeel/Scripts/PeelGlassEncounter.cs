using UnityEngine;

namespace JustOneMorePeel
{
    [DefaultExecutionOrder(150)]
    public sealed class PeelGlassEncounter : MonoBehaviour
    {
        const float MaxLookDistance = 3.6f;
        const float Duration = 3.35f;
        static readonly Vector3 AdheredPosition = new Vector3(.15f, .04f, -.059f);
        [SerializeField] PeelGame game;
        [SerializeField] PeelBasementLight basementLight;
        [SerializeField] BoxCollider pane;
        [SerializeField] GameObject alcove;
        [SerializeField] Transform figure;
        [SerializeField] Transform sheet;
        [SerializeField] Transform upperArm, forearm, hand;

        Quaternion upperRest, forearmRest;
        bool started;
        float elapsed;
        internal bool Seen { get; private set; }

        void Awake()
        {
            if (!game || !basementLight || !pane || !alcove || !figure || !sheet || !upperArm || !forearm || !hand)
            {
                Debug.LogError("Basement glass encounter needs its scene references.", this);
                enabled = false;
                return;
            }
            upperRest = upperArm.localRotation;
            forearmRest = forearm.localRotation;
            ShowState();
        }

        void Update()
        {
            if (alcove.activeSelf != basementLight.IsOn) alcove.SetActive(basementLight.IsOn);
            if (Seen || !basementLight.IsOn || game.CurrentScreen != PeelScreen.Playing) return;
            if (!started)
            {
                if (!CanSeeGlass()) return;
                started = true;
                ShowState();
            }
            elapsed += Time.deltaTime;
            if (elapsed < Duration)
            {
                Pose(elapsed);
                return;
            }
            Restore(true);
            game.SaveWorld();
        }

        bool CanSeeGlass()
        {
            var view = game.view;
            if (!view || Vector3.Dot(view.transform.position - pane.transform.position, transform.forward) <= 0)
                return false;
            var direction = (pane.bounds.center - view.transform.position).normalized;
            if (Vector3.Dot(view.transform.forward, direction) < .94f) return false;
            return Physics.Raycast(view.transform.position, direction, out var hit, MaxLookDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider == pane;
        }

        void Pose(float time)
        {
            float approach = Smooth(0f, .9f, time);
            float withdraw = Smooth(2.36f, Duration, time);
            figure.localPosition = new Vector3(Mathf.Lerp(.98f, -.09f, approach) + withdraw * 1.06f,
                -1.37f + Mathf.Sin(approach * Mathf.PI) * .06f, Mathf.Lerp(-.82f, -.48f, approach) - withdraw * .28f);
            float press = Smooth(.78f, 1.62f, time);
            float release = Smooth(2.03f, 2.42f, time);
            var carried = figure.localPosition + new Vector3(.26f, 1.28f, .23f);
            sheet.localPosition = time < 1.62f ? Vector3.Lerp(carried, AdheredPosition, press) : AdheredPosition;
            float crease = Smooth(1.54f, 2.02f, time);
            sheet.localRotation = Quaternion.Euler(Mathf.Lerp(14f, 0f, press), 0f, Mathf.Lerp(-9f, -3f, press));
            sheet.localScale = new Vector3(.255f, .36f * Mathf.Lerp(.86f, 1f, crease), 1f);
            var palm = sheet.localPosition + new Vector3(.13f, Mathf.Lerp(.095f, -.10f, crease), -.17f);
            PoseArm(transform.TransformPoint(Vector3.Lerp(palm, carried - Vector3.forward * .10f, release)),
                approach * (1f - release));
        }

        static float Smooth(float start, float end, float time) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, time));

        void PoseArm(Vector3 target, float weight)
        {
            upperArm.localRotation = upperRest;
            forearm.localRotation = forearmRest;
            if (weight <= 0f) return;
            var shoulder = upperArm.position;
            float upperLength = Vector3.Distance(shoulder, forearm.position);
            float lowerLength = Vector3.Distance(forearm.position, hand.position);
            var offset = Vector3.Lerp(hand.position, target, weight) - shoulder;
            var direction = offset.normalized;
            float distance = Mathf.Clamp(offset.magnitude, .065f, upperLength + lowerLength - .01f);
            float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
            float bend = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            var down = Vector3.ProjectOnPlane(-transform.up + transform.right * .35f, direction).normalized;
            var elbow = shoulder + direction * along + down * bend;
            upperArm.rotation = Quaternion.FromToRotation(forearm.position - shoulder, elbow - shoulder) * upperArm.rotation;
            forearm.rotation = Quaternion.FromToRotation(hand.position - forearm.position,
                shoulder + direction * distance - forearm.position) * forearm.rotation;
        }

        internal void Restore(bool seen)
        {
            Seen = seen;
            started = false;
            elapsed = 0f;
            if (enabled) ShowState();
        }

        void ShowState()
        {
            figure.gameObject.SetActive(started && !Seen);
            sheet.gameObject.SetActive(started || Seen);
            if (Seen)
            {
                sheet.localPosition = AdheredPosition;
                sheet.localRotation = Quaternion.Euler(0f, 0f, -3f);
                sheet.localScale = new Vector3(.255f, .36f, 1f);
            }
        }
    }
}
