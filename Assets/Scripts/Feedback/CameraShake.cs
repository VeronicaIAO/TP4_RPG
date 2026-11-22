using System.Collections;
using UnityEngine;

namespace TPRPG
{
    public class CameraShake : MonoBehaviour
    {
        Vector3 basePosition;
        Coroutine routine;

        void Awake() => basePosition = transform.localPosition;

        public void Shake(float intensity, float duration)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Routine(intensity, duration));
        }

        IEnumerator Routine(float intensity, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - t / duration;
                transform.localPosition = basePosition + Random.insideUnitSphere * intensity * k;
                yield return null;
            }
            transform.localPosition = basePosition;
        }
    }
}
