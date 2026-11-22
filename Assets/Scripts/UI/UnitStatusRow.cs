using UnityEngine;
using UnityEngine.UI;

namespace TPRPG
{
    public class UnitStatusRow : MonoBehaviour
    {
        public Image background;
        public Text nameText;
        public Text statusText;
        public UIBar healthBar;
        public Text healthText;
        public UIBar manaBar;
        public Text manaText;
        public UIBar atbBar;
        public Text atbText;

        public BattleUnit Unit { get; private set; }

        public Color normalColor = new Color(0.98f, 0.97f, 0.9f);
        public Color activeColor = new Color(1f, 0.93f, 0.6f);
        public Color knockedOutColor = new Color(0.8f, 0.7f, 0.7f);
        public bool tintName = true;
        public Color atbColor = new Color(0.3f, 0.8f, 0.95f);

        public void Bind(BattleUnit u)
        {
            Unit = u;
            if (nameText != null)
            {
                nameText.text = u.DisplayName.ToUpper();
                if (tintName) nameText.color = u.data.mainColor;
            }
            if (manaBar != null) manaBar.gameObject.SetActive(u.MaxMana > 0);
        }

        public void Refresh(bool active)
        {
            var u = Unit;
            if (u == null) return;

            if (background != null) background.color = !u.IsAlive ? knockedOutColor : (active ? activeColor : normalColor);

            float hp = u.HealthPercent;
            Color hpColor = hp > 0.5f ? new Color(0.3f, 0.85f, 0.35f) : (hp > 0.25f ? new Color(0.95f, 0.75f, 0.2f) : new Color(0.9f, 0.25f, 0.2f));
            if (healthBar != null) healthBar.Set(hp, hpColor);
            if (healthText != null) healthText.text = $"{u.currentHealth}/ {u.MaxHealth}";

            if (manaBar != null && u.MaxMana > 0) manaBar.Set((float)u.currentMana / u.MaxMana, new Color(0.3f, 0.55f, 1f));
            if (manaText != null) manaText.text = u.MaxMana > 0 ? $"{u.currentMana}/ {u.MaxMana}" : "";

            if (atbBar != null) atbBar.Set(u.IsAlive ? Mathf.Max(0f, u.atb) / BattleUnit.MaxATB : 0f, atbColor);

            if (statusText != null) statusText.text = u.StatusSummary();
        }
    }
}
