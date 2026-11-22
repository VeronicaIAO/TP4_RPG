using System.Collections;
using UnityEngine;

namespace TPRPG
{
    public class FeedbackManager : MonoBehaviour
    {
        public ProceduralAudio sfx;
        public Color damageColor = new Color(1f, 0.2f, 0.2f);

        public void Sound(FeedbackSound s)
        {
            if (sfx != null) sfx.Play(s);
        }

        public void ShowResult(EffectResult r, float delay)
        {
            if (r.target == null) return;
            StartCoroutine(ResultRoutine(r, delay));
        }

        IEnumerator ResultRoutine(EffectResult r, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (r.isDamage) r.target.Visual.Flash(damageColor, 0.35f);
            Sound(r.sound);
        }
    }
}
