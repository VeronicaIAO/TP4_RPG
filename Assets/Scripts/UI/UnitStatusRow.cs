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

            if (atbBar != null)
            {
                if (!u.IsAlive) atbBar.Set(0f, Color.gray);
                else if (u.atb < 0f) atbBar.Set(-u.atb / BattleUnit.MaxATB, new Color(0.85f, 0.35f, 0.25f));
                else if (u.atb >= BattleUnit.MaxATB) atbBar.Set(1f, Color.Lerp(new Color(1f, 0.85f, 0.2f), Color.white, Mathf.PingPong(Time.time * 3f, 1f)));
                else atbBar.Set(u.atb / BattleUnit.MaxATB, new Color(0.3f, 0.8f, 0.95f));
            }
            if (atbText != null)
                atbText.text = !u.IsAlive ? "" : (u.atb < 0f ? "RECOVER" : (u.atb >= BattleUnit.MaxATB ? "READY!" : ""));

            if (statusText != null) statusText.text = u.StatusSummary();
        }
    }
}
