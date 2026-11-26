using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TPRPG
{
    [RequireComponent(typeof(UnitVisual))]
    public class BattleUnit : MonoBehaviour, IPointerClickHandler
    {
        public const float MaxATB = 100f;

        public CharacterData data;

        public bool isPlayerSide;
        public int currentHealth;
        public int currentMana;
        public float atb;
        public bool guarding;
        public float guardMultiplier = 1f;
        public List<StatModifier> modifiers = new List<StatModifier>();

        public UnitVisual Visual { get; private set; }
        public UnitVoice Voice { get; private set; }
        BattleManager manager;

        public string DisplayName => data != null ? data.displayName : name;
        public bool IsAlive => currentHealth > 0;
        public bool ReadyToAct => IsAlive && atb >= MaxATB;
        public int MaxHealth => data.maxHealth;
        public int MaxMana => data.maxMana;
        public float HealthPercent => (float)currentHealth / Mathf.Max(1, data.maxHealth);

        void Awake()
        {
            Visual = GetComponent<UnitVisual>();
            Voice = GetComponent<UnitVoice>();
        }

        public void Initialize(BattleManager m, bool playerSide)
        {
            manager = m;
            isPlayerSide = playerSide;
            if (data == null)
            {
                Debug.LogError($"[{name}] BattleUnit has no CharacterData assigned.", this);
                return;
            }
            currentHealth = data.maxHealth;
            currentMana = data.maxMana;
            atb = Random.Range(data.startATBMin, Mathf.Max(data.startATBMin, data.startATBMax));
            modifiers.Clear();
            guarding = false;
            if (Voice != null) Voice.voice = data.voice;
        }

        int BaseStat(StatType s)
        {
            switch (s)
            {
                case StatType.Attack: return data.attack;
                case StatType.Defense: return data.defense;
                case StatType.Wit: return data.wit;
                default: return Mathf.RoundToInt(data.speed);
            }
        }

        public float Multiplier(StatType s)
        {
            float m = 1f;
            foreach (var mod in modifiers) if (mod.stat == s) m *= mod.multiplier;
            return m;
        }

        public int GetStat(StatType s) => Mathf.Max(1, Mathf.RoundToInt(BaseStat(s) * Multiplier(s)));

        public float EffectiveSpeed => data.speed * Multiplier(StatType.Speed);

        public void AdvanceATB(float dt, float scale)
        {
            if (!IsAlive) return;
            atb = Mathf.Min(MaxATB, atb + EffectiveSpeed * scale * dt);
        }

        public void AlterATB(float amount)
        {
            if (!IsAlive) return;
            atb = Mathf.Clamp(atb + amount, -MaxATB, MaxATB);
        }

        public void BeginTurn()
        {
            guarding = false;
            guardMultiplier = 1f;
        }

        public void EndTurn(float recoveryCost)
        {
            if (!IsAlive) return;
            atb = Mathf.Clamp(-recoveryCost, -MaxATB, MaxATB - 1f);
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                modifiers[i].turnsLeft--;
                if (modifiers[i].turnsLeft <= 0) modifiers.RemoveAt(i);
            }
        }

        public int TakeDamage(int amount)
        {
            if (!IsAlive) return 0;
            if (guarding) amount = Mathf.Max(1, Mathf.RoundToInt(amount * guardMultiplier));
            int dealt = Mathf.Min(amount, currentHealth);
            currentHealth -= dealt;
            if (currentHealth <= 0) OnKnockedOut();
            return dealt;
        }

        public int Heal(int amount)
        {
            if (!IsAlive) return 0;
            int real = Mathf.Min(amount, data.maxHealth - currentHealth);
            currentHealth += real;
            return real;
        }

        public int RestoreMana(int amount)
        {
            if (!IsAlive) return 0;
            int real = Mathf.Min(amount, data.maxMana - currentMana);
            currentMana += real;
            return real;
        }

        public bool SpendMana(int amount)
        {
            if (currentMana < amount) return false;
            currentMana -= amount;
            return true;
        }

        public void Revive(float percent)
        {
            if (IsAlive) return;
            currentHealth = Mathf.Max(1, Mathf.RoundToInt(data.maxHealth * Mathf.Clamp01(percent)));
            atb = 0f;
        }

        void OnKnockedOut()
        {
            currentHealth = 0;
            atb = 0f;
            modifiers.Clear();
            guarding = false;
        }

        public void AddModifier(StatType s, float mult, int turns, string source)
        {
            foreach (var m in modifiers)
            {
                if (m.stat == s && m.source == source)
                {
                    m.multiplier = mult;
                    m.turnsLeft = Mathf.Max(m.turnsLeft, turns);
                    return;
                }
            }
            modifiers.Add(new StatModifier { stat = s, multiplier = mult, turnsLeft = turns, source = source });
        }

        public void StartGuard(float mult)
        {
            guarding = true;
            guardMultiplier = mult;
        }

        public string StatusSummary()
        {
            if (!IsAlive) return "K.O.";
            var sb = new StringBuilder();
            if (guarding) sb.Append("<color=#7fd3ff>GUARD</color>  ");
            foreach (var m in modifiers)
            {
                string sign = m.IsBuff ? "+" : "-";
                string color = m.IsBuff ? "#ffb347" : "#c78bff";
                sb.Append($"<color={color}>{StatModifier.Abbreviation(m.stat)}{sign}({m.turnsLeft})</color> ");
            }
            return sb.ToString();
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (manager != null && manager.ui != null) manager.ui.UnitClicked(this);
        }
    }
}