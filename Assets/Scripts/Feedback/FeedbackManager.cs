using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TPRPG
{
    public class FeedbackManager : MonoBehaviour
    {
        public Camera cam;
        public CameraShake cameraShake;
        public ProceduralAudio sfx;
        public Image screenFlash;

        [Range(3, 30)] public int burstCount = 10;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (screenFlash != null)
            {
                screenFlash.color = new Color(1f, 1f, 1f, 0f);
                screenFlash.raycastTarget = false;
            }
        }

        public void Sound(FeedbackSound s)
        {
            if (sfx != null) sfx.Play(s);
        }

        public void ShakeCamera(float intensity, float duration)
        {
            if (cameraShake != null) cameraShake.Shake(intensity, duration);
        }

        public void ShowResult(EffectResult r, ActionData action, float delay)
        {
            if (r.target == null) return;
            StartCoroutine(ResultRoutine(r, delay));
        }

        IEnumerator ResultRoutine(EffectResult r, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            var v = r.target.Visual;
            if (r.isDamage)
            {
                v.Flash(new Color(1f, 0.2f, 0.2f), 0.35f);
                v.Shake(r.critical ? 0.22f : 0.12f, 0.3f);
                Burst(v.ImpactPoint, r.critical ? ActionResolver.CriticalColor : new Color(1f, 0.4f, 0.3f));
                if (r.critical) ShakeCamera(0.25f, 0.3f);
            }
            else
            {
                v.Flash(r.color, 0.5f);
            }
            Sound(r.sound);
        }

        public void CameraFlash(Vector3 worldPosition)
        {
            StartCoroutine(FlashSphere(worldPosition));
            if (screenFlash != null) StartCoroutine(ScreenFlashRoutine(0.25f));
        }

        IEnumerator FlashSphere(Vector3 pos)
        {
            var go = Primitive(PrimitiveType.Sphere, Color.white, 0.1f);
            go.transform.position = pos;
            float d = 0.25f, e = 0f;
            while (e < d)
            {
                e += Time.deltaTime;
                float k = e / d;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 0.9f, k) * (1f - k * 0.7f);
                yield return null;
            }
            Destroy(go);
        }

        IEnumerator ScreenFlashRoutine(float d)
        {
            float e = 0f;
            while (e < d)
            {
                e += Time.deltaTime;
                screenFlash.color = new Color(1f, 1f, 1f, 0.75f * (1f - e / d));
                yield return null;
            }
            screenFlash.color = new Color(1f, 1f, 1f, 0f);
        }

        static GameObject Primitive(PrimitiveType type, Color c, float scale)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.localScale = Vector3.one * scale;
            var r = go.GetComponent<Renderer>();
            r.material.color = c;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        public IEnumerator Projectile(Vector3 from, Vector3 to, Color c, float duration, float size = 0.3f, float arc = 0.8f)
        {
            var go = Primitive(PrimitiveType.Sphere, c, size);
            float e = 0f;
            while (e < duration)
            {
                e += Time.deltaTime;
                float k = Mathf.Clamp01(e / duration);
                go.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * arc;
                go.transform.localScale = Vector3.one * size * (1f + Mathf.Sin(Time.time * 30f) * 0.15f);
                yield return null;
            }
            Destroy(go);
            Burst(to, c);
        }

        public void Burst(Vector3 pos, Color c) => StartCoroutine(BurstRoutine(pos, c));

        IEnumerator BurstRoutine(Vector3 pos, Color c)
        {
            int n = burstCount;
            var pieces = new GameObject[n];
            var dirs = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                pieces[i] = Primitive(PrimitiveType.Cube, c, 0.12f);
                pieces[i].transform.position = pos;
                pieces[i].transform.rotation = Random.rotation;
                dirs[i] = (Random.onUnitSphere + Vector3.up * 0.5f).normalized * Random.Range(2f, 4f);
            }
            float d = 0.5f, e = 0f;
            while (e < d)
            {
                e += Time.deltaTime;
                float k = e / d;
                for (int i = 0; i < n; i++)
                {
                    dirs[i] += Vector3.down * 9f * Time.deltaTime;
                    pieces[i].transform.position += dirs[i] * Time.deltaTime;
                    pieces[i].transform.localScale = Vector3.one * 0.12f * (1f - k);
                }
                yield return null;
            }
            foreach (var p in pieces) Destroy(p);
        }

        public void Aura(Vector3 pos, Color c) => StartCoroutine(AuraRoutine(pos, c));

        IEnumerator AuraRoutine(Vector3 pos, Color c)
        {
            const int n = 8;
            var pieces = new GameObject[n];
            for (int i = 0; i < n; i++) pieces[i] = Primitive(PrimitiveType.Sphere, c, 0.15f);
            float d = 0.8f, e = 0f;
            while (e < d)
            {
                e += Time.deltaTime;
                float k = e / d;
                for (int i = 0; i < n; i++)
                {
                    float ang = (i / (float)n) * Mathf.PI * 2f + k * 6f;
                    float radius = 0.7f * (1f - k * 0.5f);
                    pieces[i].transform.position = pos + new Vector3(Mathf.Cos(ang) * radius, k * 2f, Mathf.Sin(ang) * radius);
                    pieces[i].transform.localScale = Vector3.one * 0.18f * (1f - k);
                }
                yield return null;
            }
            foreach (var p in pieces) Destroy(p);
        }
    }
}
