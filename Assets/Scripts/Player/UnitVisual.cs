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

        static readonly Vector3 ArmForward = V(0f, -90f, 8f);
        static readonly Vector3 ArmToMouth = V(0f, -118f, 32f);
        static readonly Vector3 ArmPhoneUp = V(0f, -75f, 55f);

        public static readonly BodyPose Idle = P(Z, Z);

        public static readonly BodyPose PunchWindup = P(V(-4f, 25f, 0f), V(4f, -18f, 0f));
        public static readonly BodyPose PunchStrike = P(V(12f, -28f, 0f), V(6f, 22f, 0f));

        public static readonly BodyPose ShoveWindup = PB(V(-10f, 0f, 0f), V(-6f, 0f, 0f), V(0f, -60f, -25f));
        public static readonly BodyPose ShoveStrike = PB(V(22f, 0f, 0f), V(10f, 0f, 0f), V(0f, -90f, 4f));

        public static readonly BodyPose HairReach = PR(V(14f, -10f, 0f), V(6f, 0f, 0f), V(0f, -90f, 40f));
        public static readonly BodyPose HairYank = PR(V(-20f, 20f, 0f), V(-10f, 0f, 0f), V(0f, -65f, 15f));

        public static readonly BodyPose InsultPoint = PR(V(12f, -8f, 0f), V(8f, 0f, 0f), ArmForward);
        public static readonly BodyPose InsultJab = PR(V(18f, -12f, 0f), V(14f, 0f, 0f), V(0f, -95f, 0f));

        public static readonly BodyPose CalloutPhoneUp = PR(V(-6f, 0f, 0f), V(-12f, 0f, 0f), ArmPhoneUp);
        public static readonly BodyPose CalloutPoint = PLR(V(8f, 10f, 0f), V(6f, 0f, 0f), Mirror(ArmForward), ArmPhoneUp);

        public static BodyPose Whisper(float audienceSign) =>
            PR(V(6f, 60f * audienceSign, 0f), V(14f, 18f * audienceSign, 16f * audienceSign), ArmToMouth);
        public static readonly BodyPose DefamePoint = PR(V(10f, -14f, 0f), V(4f, 10f, -8f), ArmForward);

        public static readonly BodyPose SpitWindup = P(V(-14f, 0f, 0f), V(-32f, 0f, 0f));
        public static readonly BodyPose SpitRelease = P(V(18f, 0f, 0f), V(28f, 0f, 0f));
    }

    public class UnitVisual : MonoBehaviour
    {
        public Transform model;
        public CharacterAnimator animator;
        public Transform torso;
        public Transform head;
        public Transform armLeft;
        public Transform armRight;
        public string idleClip = "idle";

        Renderer[] renderers;
        Color[] baseColors;
        string[] colorProperties;
        bool knockedOut;
        BodyPose pose = Poses.Idle;
        Coroutine poseRoutine, flashRoutine;

        readonly Transform[] bones = new Transform[4];
        readonly Quaternion[] lastAnimated = new Quaternion[4];
        readonly Quaternion[] lastWritten = new Quaternion[4];

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
            bones[0] = torso;
            bones[1] = head;
            bones[2] = armLeft;
            bones[3] = armRight;
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) lastAnimated[i] = lastWritten[i] = bones[i].localRotation;
        }

        void Start()
        {
            DiagnoseIdle();
            PlayIdle();
        }

        void DiagnoseIdle()
        {
            if (animator == null) { Debug.LogWarning($"[{name}] UnitVisual has no CharacterAnimator assigned: no idle animation.", this); return; }
            if (!animator.HasClips) { Debug.LogWarning($"[{name}] CharacterAnimator has no clips: no idle animation.", this); return; }
            if (animator.FindClip(idleClip) == null)
                Debug.LogWarning($"[{name}] idle clip '{idleClip}' not found. Available: {string.Join(", ", animator.ClipNames)}", this);
        }

        public void PlayClip(string clipName, bool loop = false, float fade = -1f, float speed = 1f)
        {
            if (animator != null) animator.Play(clipName, loop, fade, speed);
        }

        public void PlayIdle()
        {
            if (knockedOut || animator == null) return;
            string clip = animator.FindClip(idleClip);
            if (clip != null) animator.Play(clip, true);
        }

        void LateUpdate()
        {
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
                    case 1: r = Quaternion.Euler(pose.head) * r; break;
                    case 2: r = Quaternion.Slerp(r, Quaternion.Euler(pose.armLeft), Mathf.Clamp01(pose.armLeftWeight)); break;
                    case 3: r = Quaternion.Slerp(r, Quaternion.Euler(pose.armRight), Mathf.Clamp01(pose.armRightWeight)); break;
                }
                b.localRotation = r;
                lastWritten[i] = r;
            }
        }

        public IEnumerator PoseTo(BodyPose target, float duration)
        {
            if (poseRoutine != null) StopCoroutine(poseRoutine);
            poseRoutine = null;
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

        Color CurrentColor(int i) => knockedOut ? Color.Lerp(baseColors[i], new Color(0.3f, 0.3f, 0.3f), 0.6f) : baseColors[i];

        void ApplyColors(Color tint, float k)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null && colorProperties[i] != null)
                    renderers[i].material.SetColor(colorProperties[i], Color.Lerp(CurrentColor(i), tint, k));
        }

        public void Flash(Color c, float duration = 0.3f)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
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

        public void SetKnockedOut(bool value)
        {
            knockedOut = value;
            ApplyColors(Color.white, 0f);
        }
    }
}