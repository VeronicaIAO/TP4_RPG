using System.Collections;
using UnityEngine;

namespace TPRPG
{
    [System.Serializable]
    public struct BodyPose
    {
        public Vector3 torso;
        public Vector3 head;
        public Vector3 armLeft;
        public float armLeftWeight;
        public Vector3 armRight;
        public float armRightWeight;

        public BodyPose(Vector3 torso, Vector3 head, Vector3 armLeft, float armLeftWeight, Vector3 armRight, float armRightWeight)
        {
            this.torso = torso;
            this.head = head;
            this.armLeft = armLeft;
            this.armLeftWeight = armLeftWeight;
            this.armRight = armRight;
            this.armRightWeight = armRightWeight;
        }

        public static BodyPose Lerp(BodyPose a, BodyPose b, float t) => new BodyPose(
            Vector3.LerpUnclamped(a.torso, b.torso, t),
            Vector3.LerpUnclamped(a.head, b.head, t),
            Vector3.LerpUnclamped(a.armLeft, b.armLeft, t),
            Mathf.LerpUnclamped(a.armLeftWeight, b.armLeftWeight, t),
            Vector3.LerpUnclamped(a.armRight, b.armRight, t),
            Mathf.LerpUnclamped(a.armRightWeight, b.armRightWeight, t));
    }

    public static class Poses
    {
        static readonly Vector3 Z = Vector3.zero;

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Vector3 Mirror(Vector3 v) => new Vector3(v.x, -v.y, -v.z);

        static BodyPose P(Vector3 torso, Vector3 head) => new BodyPose(torso, head, Z, 0f, Z, 0f);
        static BodyPose PR(Vector3 torso, Vector3 head, Vector3 right) => new BodyPose(torso, head, Z, 0f, right, 1f);
        static BodyPose PB(Vector3 torso, Vector3 head, Vector3 right) => new BodyPose(torso, head, Mirror(right), 1f, right, 1f);
        static BodyPose PLR(Vector3 torso, Vector3 head, Vector3 left, Vector3 right) => new BodyPose(torso, head, left, 1f, right, 1f);

        public static readonly Vector3 ArmForward = V(0f, -90f, 8f);
        public static readonly Vector3 ArmToHead = V(0f, -55f, 72f);
        public static readonly Vector3 ArmToMouth = V(0f, -118f, 32f);
        public static readonly Vector3 ArmPhoneUp = V(0f, -75f, 55f);
        public static readonly Vector3 ArmUp = V(0f, -20f, 78f);

        public static readonly BodyPose Idle = P(Z, Z);
        public static readonly BodyPose Guard = PB(V(12f, 0f, 0f), V(10f, 0f, 0f), V(0f, -112f, 28f));
        public static readonly BodyPose Celebrate = PB(V(-6f, 0f, 0f), V(-15f, 0f, 0f), ArmUp);

        public static readonly BodyPose PunchWindup = P(V(-4f, 25f, 0f), V(4f, -18f, 0f));
        public static readonly BodyPose PunchStrike = P(V(12f, -28f, 0f), V(6f, 22f, 0f));

        public static readonly BodyPose ShoveWindup = PB(V(-10f, 0f, 0f), V(-6f, 0f, 0f), V(0f, -60f, -25f));
        public static readonly BodyPose ShoveStrike = PB(V(22f, 0f, 0f), V(10f, 0f, 0f), V(0f, -90f, 4f));

        public static readonly BodyPose HairReach = PR(V(14f, -10f, 0f), V(6f, 0f, 0f), V(0f, -90f, 40f));
        public static readonly BodyPose HairYank = PR(V(-20f, 20f, 0f), V(-10f, 0f, 0f), V(0f, -65f, 15f));
        public static readonly BodyPose HairHold = PR(V(-6f, 8f, 0f), V(-4f, 0f, 0f), V(0f, -80f, 30f));
        public static readonly BodyPose HairGrabbed = PB(V(22f, 0f, 0f), V(28f, 0f, 14f), ArmToHead);

        public static readonly BodyPose InsultPoint = PR(V(12f, -8f, 0f), V(8f, 0f, 0f), ArmForward);
        public static readonly BodyPose InsultJab = PR(V(18f, -12f, 0f), V(14f, 0f, 0f), V(0f, -95f, 0f));

        public static readonly BodyPose CalloutPhoneUp = PR(V(-6f, 0f, 0f), V(-12f, 0f, 0f), ArmPhoneUp);
        public static readonly BodyPose CalloutPoint = PLR(V(8f, 10f, 0f), V(6f, 0f, 0f), Mirror(ArmForward), ArmPhoneUp);

        public static BodyPose Whisper(float audienceSign) =>
            PR(V(6f, 60f * audienceSign, 0f), V(14f, 18f * audienceSign, 16f * audienceSign), ArmToMouth);
        public static readonly BodyPose DefamePoint = PR(V(10f, -14f, 0f), V(4f, 10f, -8f), ArmForward);

        public static readonly BodyPose SpitWindup = P(V(-14f, 0f, 0f), V(-32f, 0f, 0f));
        public static readonly BodyPose SpitRelease = P(V(18f, 0f, 0f), V(28f, 0f, 0f));

        public static readonly BodyPose ThrowWindup = PR(V(-12f, 22f, 0f), V(-6f, -12f, 0f), V(0f, 10f, 70f));
        public static readonly BodyPose ThrowRelease = PR(V(16f, -22f, 0f), V(8f, 16f, 0f), V(0f, -90f, -10f));

        public static readonly BodyPose Consume = PR(V(-4f, 0f, 0f), V(-22f, 0f, 0f), ArmToMouth);

        public static readonly BodyPose Hurt = P(V(-24f, 10f, 0f), V(-26f, -10f, 0f));
        public static readonly BodyPose Pain = PB(V(18f, 0f, 0f), V(22f, 0f, 0f), ArmToHead);
        public static readonly BodyPose Surprise = PB(V(-16f, 0f, 0f), V(-20f, 0f, 0f), V(0f, 0f, 45f));
        public static readonly BodyPose Outrage = P(V(-10f, 0f, 0f), V(-12f, 0f, 0f));
        public static readonly BodyPose Disgust = PR(V(16f, 22f, 0f), V(8f, 48f, 10f), ArmToMouth);
    }

    public class UnitVisual : MonoBehaviour
    {
        public Transform model;
        public CharacterAnimator animator;
        public Transform torso;
        public Transform head;
        public Transform armLeft;
        public Transform armRight;
        public GameObject prop;
        public GameObject turnMarker;
        public GameObject targetMarker;

        public float impactHeight = 0.6f;
        public float handReach = 0.13f;
        public float approachDistance = 0.85f;
        public float moveSpeed = 4.5f;
        public float turnDuration = 0.15f;

        public string idleClip = "idle";
        public string walkClip = "walk";
        public string runClip = "sprint";
        public string dieClip = "die";
        public string victoryClip = "emote-yes";

        Renderer[] renderers;
        Color[] baseColors;
        string[] colorProperties;
        Vector3 modelBasePosition;
        Vector3 homePosition;
        Quaternion homeRotation;
        Vector3 shakeOffset;
        Vector3 pullOffset;
        float phase;
        float talk;
        bool knockedOut;
        bool guardPose;
        bool celebrating;
        BodyPose pose = Poses.Idle;
        Coroutine flashRoutine, shakeRoutine, poseRoutine, reactRoutine, pullRoutine;
        Vector3 turnMarkerScale;
        Vector3 targetMarkerPosition;

        readonly Transform[] bones = new Transform[4];
        readonly Quaternion[] lastAnimated = new Quaternion[4];
        readonly Quaternion[] lastWritten = new Quaternion[4];

        public Vector3 ImpactPoint => transform.position + Vector3.up * impactHeight;
        public Vector3 HeadPoint => head != null ? head.position + Vector3.up * 0.22f * ModelScale :ImpactPoint + Vector3.up * 0.4f;
        public Vector3 MouthPoint => head != null ? head.position + Vector3.up * 0.1f * ModelScale + transform.forward * 0.12f * ModelScale : ImpactPoint + Vector3.up * 0.3f;
        public Vector3 RightHandPoint => armRight != null ? armRight.TransformPoint(Vector3.right * handReach) : ImpactPoint;
        public Vector3 HomePosition => homePosition;
        public BodyPose RestPose => guardPose ? Poses.Guard : Poses.Idle;
        public bool IsKnockedOut => knockedOut;
        float ModelScale => model != null ? model.localScale.y : 1f;

        public float AudienceSign
        {
            get
            {
                var cam = Camera.main;
                if (cam == null) return 1f;
                Vector3 toCam = cam.transform.position - transform.position;
                toCam.y = 0f;
                return Vector3.Dot(transform.right, toCam) >= 0f ? 1f : -1f;
            }
        }

        void Awake()
        {
            if (model == null) model = transform;
            renderers = model.GetComponentsInChildren<Renderer>(true);
            baseColors = new Color[renderers.Length];
            colorProperties = new string[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                var m = renderers[i].material;
                colorProperties[i] = m.HasProperty("_BaseColor") ? "_BaseColor" : m.HasProperty("baseColorFactor") ? "baseColorFactor" : m.HasProperty("_Color") ? "_Color" : null;
                baseColors[i] = colorProperties[i] != null ? m.GetColor(colorProperties[i]) : Color.white;
            }
            modelBasePosition = model.localPosition;
            homePosition = transform.position;
            homeRotation = transform.rotation;
            phase = Random.value * 10f;
            bones[0] = torso;
            bones[1] = head;
            bones[2] = armLeft;
            bones[3] = armRight;
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) lastAnimated[i] = lastWritten[i] = bones[i].localRotation;
            if (turnMarker != null) turnMarkerScale = turnMarker.transform.localScale;
            if (targetMarker != null) targetMarkerPosition = targetMarker.transform.localPosition;
            if (prop != null) prop.SetActive(false);
            SetTurnMarker(false);
            SetTargetMarker(false);
        }

        void Start()
        {
            PlayClip(idleClip, true, 0f);
        }

        public void PlayClip(string clipName, bool loop = false, float fade = -1f, float speed = 1f)
        {
            if (animator != null) animator.Play(clipName, loop, fade, speed);
        }

        public float ClipLength(string clipName) => animator != null ? animator.Length(clipName) : 0f;
        public bool HasClip(string clipName) => animator != null && animator.Has(clipName);

        public void PlayIdle() { if (!knockedOut) PlayClip(idleClip, true); }

        void LateUpdate()
        {
            float time = Time.time + phase;
            if (model != transform)
                model.localPosition = modelBasePosition + shakeOffset + transform.InverseTransformDirection(pullOffset);

            float talkWave = Mathf.Sin(time * 15f) * talk;
            for (int i = 0; i < bones.Length; i++)
            {
                var b = bones[i];
                if (b == null) continue;
                if (b.localRotation == lastWritten[i]) b.localRotation = lastAnimated[i];
                lastAnimated[i] = b.localRotation;
                Quaternion r = b.localRotation;
                switch (i)
                {
                    case 0: r = Quaternion.Euler(pose.torso) * r; break;
                    case 1: r = Quaternion.Euler(pose.head + new Vector3(talkWave * 10f, 0f, 0f)) * r; break;
                    case 2: r = Quaternion.Slerp(r, Quaternion.Euler(pose.armLeft), Mathf.Clamp01(pose.armLeftWeight)); break;
                    case 3: r = Quaternion.Slerp(r, Quaternion.Euler(pose.armRight), Mathf.Clamp01(pose.armRightWeight)); break;
                }
                b.localRotation = r;
                lastWritten[i] = r;
            }

            if (turnMarker != null && turnMarker.activeSelf)
                turnMarker.transform.localScale = turnMarkerScale * (1f + Mathf.Sin(Time.time * 6f) * 0.08f);

            if (targetMarker != null && targetMarker.activeSelf)
            {
                targetMarker.transform.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
                targetMarker.transform.localPosition = targetMarkerPosition + Vector3.up * Mathf.Sin(Time.time * 5f) * 0.1f;
            }
        }

        public void SetTurnMarker(bool v) { if (turnMarker != null) turnMarker.SetActive(v); }
        public void SetTargetMarker(bool v) { if (targetMarker != null) targetMarker.SetActive(v); }
        public void SetTalking(bool v) => talk = v ? 1f : 0f;
        public void ShowProp(bool v) { if (prop != null) prop.SetActive(v); }

        public void SetGuard(bool v)
        {
            if (guardPose == v) return;
            guardPose = v;
            if (knockedOut) return;
            StopInternal(ref poseRoutine);
            poseRoutine = StartCoroutine(Tween(RestPose, 0.2f));
        }

        void StopInternal(ref Coroutine c)
        {
            if (c != null) StopCoroutine(c);
            c = null;
        }

        public IEnumerator PoseTo(BodyPose target, float duration)
        {
            StopInternal(ref poseRoutine);
            return Tween(target, duration);
        }

        IEnumerator Tween(BodyPose target, float duration)
        {
            BodyPose from = pose;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                pose = BodyPose.Lerp(from, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            pose = target;
        }

        Color CurrentColor(int i) => knockedOut ? Color.Lerp(baseColors[i], new Color(0.3f, 0.3f, 0.3f), 0.5f) : baseColors[i];

        void ApplyColors(Color tint, float k)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null && colorProperties[i] != null)
                    renderers[i].material.SetColor(colorProperties[i], Color.Lerp(CurrentColor(i), tint, k));
        }

        public void Flash(Color c, float duration = 0.3f)
        {
            StopInternal(ref flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(c, duration));
        }

        IEnumerator FlashRoutine(Color c, float d)
        {
            float t = 0f;
            while (t < d)
            {
                t += Time.deltaTime;
                ApplyColors(c, 1f - t / d);
                yield return null;
            }
            ApplyColors(Color.white, 0f);
        }

        public void Shake(float intensity = 0.12f, float duration = 0.3f)
        {
            StopInternal(ref shakeRoutine);
            shakeRoutine = StartCoroutine(ShakeRoutine(intensity, duration));
        }

        IEnumerator ShakeRoutine(float i, float d)
        {
            float t = 0f;
            while (t < d)
            {
                t += Time.deltaTime;
                float k = 1f - t / d;
                shakeOffset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * i * k;
                yield return null;
            }
            shakeOffset = Vector3.zero;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }

        IEnumerator TurnTo(Quaternion target, float duration)
        {
            Quaternion from = transform.rotation;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(from, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            transform.rotation = target;
        }

        public IEnumerator MoveTo(Vector3 destination, float duration, bool animate = true)
        {
            Vector3 from = transform.position;
            destination.y = from.y;
            if (animate) PlayClip(HasClip(runClip) && (destination - from).magnitude > 2f ? runClip : walkClip, true);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(from, destination, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            transform.position = destination;
            if (animate) PlayIdle();
        }

        float TravelTime(Vector3 destination, float minimum)
        {
            return Mathf.Max(minimum, Vector3.Distance(transform.position, destination) / Mathf.Max(0.1f, moveSpeed));
        }

        public IEnumerator Approach(Vector3 targetPoint, float stopDistance, float speedFactor)
        {
            Vector3 dir = Flat(targetPoint - homePosition);
            Vector3 destination = targetPoint - dir * stopDistance;
            return MoveTo(destination, TravelTime(destination, 0.2f) / Mathf.Max(0.1f, speedFactor));
        }

        public IEnumerator StepForward(float distance, float duration)
        {
            return MoveTo(transform.position + Flat(transform.forward) * distance, duration);
        }

        public IEnumerator Lunge(float distance, float duration)
        {
            return MoveTo(transform.position + Flat(transform.forward) * distance, duration, false);
        }

        public IEnumerator ReturnHome(float speedFactor)
        {
            if ((transform.position - homePosition).sqrMagnitude < 0.0004f)
            {
                transform.rotation = homeRotation;
                yield break;
            }
            float s = Mathf.Max(0.1f, speedFactor);
            Vector3 back = Flat(homePosition - transform.position);
            yield return TurnTo(Quaternion.LookRotation(back, Vector3.up), turnDuration / s);
            yield return MoveTo(homePosition, TravelTime(homePosition, 0.2f) / s);
            yield return TurnTo(homeRotation, turnDuration / s);
        }

        public void React(HitReaction reaction, Vector3 from, float knockback, float speed)
        {
            if (knockedOut || reaction == HitReaction.None) return;
            StopInternal(ref reactRoutine);
            StopInternal(ref poseRoutine);
            reactRoutine = StartCoroutine(ReactRoutine(reaction, from, knockback, Mathf.Max(0.1f, speed)));
        }

        IEnumerator ReactRoutine(HitReaction reaction, Vector3 from, float knockback, float s)
        {
            Vector3 away = Flat(transform.position - from);
            Coroutine slide = null;
            if (knockback > 0.01f)
                slide = StartCoroutine(MoveTo(transform.position + away * knockback, 0.2f / s, false));

            switch (reaction)
            {
                case HitReaction.Hurt:
                    yield return Tween(Poses.Hurt, 0.07f / s);
                    yield return new WaitForSeconds(0.3f / s);
                    break;
                case HitReaction.Pain:
                    PlayClip("crouch", false);
                    yield return Tween(Poses.Pain, 0.1f / s);
                    Shake(0.05f, 0.5f / s);
                    yield return new WaitForSeconds(0.55f / s);
                    break;
                case HitReaction.Surprise:
                    PlayClip("fall", false);
                    StartCoroutine(Hop(0.25f, 0.3f / s));
                    yield return Tween(Poses.Surprise, 0.08f / s);
                    yield return new WaitForSeconds(0.4f / s);
                    break;
                case HitReaction.Outrage:
                    PlayClip("emote-no", false);
                    yield return Tween(Poses.Outrage, 0.12f / s);
                    yield return new WaitForSeconds(Mathf.Max(0.5f, ClipLength("emote-no")) / s);
                    break;
                case HitReaction.Disgust:
                    PlayClip("emote-no", false);
                    yield return Tween(Poses.Disgust, 0.1f / s);
                    var wipe = Poses.Disgust;
                    for (int i = 0; i < 2; i++)
                    {
                        wipe.armRight = new Vector3(0f, -140f, 32f);
                        yield return Tween(wipe, 0.12f / s);
                        wipe.armRight = Poses.ArmToMouth;
                        yield return Tween(wipe, 0.12f / s);
                    }
                    break;
            }

            if (slide != null) yield return slide;
            PlayIdle();
            yield return Tween(RestPose, 0.2f / s);
            if ((transform.position - homePosition).sqrMagnitude > 0.0004f)
            {
                PlayClip(walkClip, true);
                float back = 0.35f / s;
                Vector3 start = transform.position;
                float t = 0f;
                while (t < back)
                {
                    t += Time.deltaTime;
                    transform.position = Vector3.Lerp(start, homePosition, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / back)));
                    yield return null;
                }
                transform.position = homePosition;
                PlayIdle();
            }
            reactRoutine = null;
        }

        IEnumerator Hop(float height, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                pullOffset = Vector3.up * Mathf.Sin(k * Mathf.PI) * height;
                yield return null;
            }
            pullOffset = Vector3.zero;
        }

        public void SetHeldByHair(bool held, float speed)
        {
            if (knockedOut) return;
            StopInternal(ref reactRoutine);
            StopInternal(ref poseRoutine);
            if (held) PlayClip(idleClip, true);
            poseRoutine = StartCoroutine(Tween(held ? Poses.HairGrabbed : RestPose, 0.1f / Mathf.Max(0.1f, speed)));
        }

        public void HairJerk(Vector3 towards, float strength, float duration)
        {
            if (knockedOut) return;
            StopInternal(ref pullRoutine);
            pullRoutine = StartCoroutine(PullRoutine(Flat(towards - transform.position) * strength, duration));
        }

        IEnumerator PullRoutine(Vector3 offset, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                pullOffset = offset * Mathf.Sin(k * Mathf.PI) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.04f;
                yield return null;
            }
            pullOffset = Vector3.zero;
        }

        public void Die()
        {
            knockedOut = true;
            talk = 0f;
            celebrating = false;
            SetTurnMarker(false);
            SetTargetMarker(false);
            ShowProp(false);
            StopInternal(ref reactRoutine);
            StopInternal(ref poseRoutine);
            poseRoutine = StartCoroutine(Tween(Poses.Idle, 0.2f));
            PlayClip(dieClip, false, 0.08f);
            ApplyColors(Color.white, 0f);
        }

        public void Revive()
        {
            knockedOut = false;
            PlayIdle();
            StopInternal(ref poseRoutine);
            poseRoutine = StartCoroutine(Tween(RestPose, 0.3f));
            Flash(new Color(1f, 0.95f, 0.6f), 0.6f);
        }

        public void StartCelebration()
        {
            if (knockedOut || celebrating) return;
            celebrating = true;
            StopInternal(ref reactRoutine);
            StopInternal(ref poseRoutine);
            poseRoutine = StartCoroutine(CelebrationRoutine());
        }

        IEnumerator CelebrationRoutine()
        {
            if ((transform.position - homePosition).sqrMagnitude > 0.0004f) yield return ReturnHome(1f);
            PlayClip(victoryClip, true);
            while (celebrating)
            {
                yield return Tween(Poses.Celebrate, 0.18f);
                yield return Hop(0.25f, 0.35f);
                var down = Poses.Celebrate;
                down.armLeftWeight = 0.4f;
                down.armRightWeight = 0.4f;
                yield return Tween(down, 0.18f);
                yield return new WaitForSeconds(0.15f);
            }
        }
    }
}
