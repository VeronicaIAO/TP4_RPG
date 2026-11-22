using UnityEngine;
using UnityEngine.UI;

namespace TPRPG
{
    public class UIBar : MonoBehaviour
    {
        public RectTransform fill;
        public Image fillImage;
        public RectTransform ghost;
        public float ghostSpeed = 0.8f;

        float ghostValue = 1f;

        public void Set(float fraction, Color color)
        {
            fraction = Mathf.Clamp01(fraction);
            if (fill != null) fill.anchorMax = new Vector2(fraction, 1f);
            if (fillImage != null) fillImage.color = color;

            if (ghost != null)
            {
                if (fraction > ghostValue) ghostValue = fraction;
                else ghostValue = Mathf.MoveTowards(ghostValue, fraction, ghostSpeed * Time.deltaTime);
                ghost.anchorMax = new Vector2(ghostValue, 1f);
            }
        }
    }
}
