using System.Collections.Generic;
using UnityEngine;

namespace TPRPG
{
    public struct EffectResult
    {
        public BattleUnit target;
        public string text;
        public Color color;
        public FeedbackSound sound;
        public bool isDamage;
        public bool critical;
        public string log;
    }

    public static class ActionResolver
    {
        public static readonly Color DamageColor = Color.white;
        public static readonly Color CriticalColor = new Color(1f, 0.85f, 0.2f);
        public static readonly Color HealColor = new Color(0.4f, 1f, 0.45f);
        public static readonly Color ManaColor = new Color(0.45f, 0.7f, 1f);
        public static readonly Color BuffColor = new Color(1f, 0.7f, 0.28f);
        public static readonly Color DebuffColor = new Color(0.78f, 0.55f, 1f);

        public static bool RequiresSelection(TargetType t) =>
            t == TargetType.SingleEnemy || t == TargetType.SingleAlly || t == TargetType.FallenAlly;

        public static bool TargetsRivals(TargetType t) =>
            t == TargetType.SingleEnemy || t == TargetType.AllEnemies;

        public static List<BattleUnit> Candidates(TargetType t, BattleUnit user, List<BattleUnit> team, List<BattleUnit> rivals)
        {
            var r = new List<BattleUnit>();
            switch (t)
            {
                case TargetType.SingleEnemy:
                case TargetType.AllEnemies:
                    foreach (var u in rivals) if (u.IsAlive) r.Add(u);
                    break;
                case TargetType.SingleAlly:
                case TargetType.AllAllies:
                    foreach (var u in team) if (u.IsAlive) r.Add(u);
                    break;
                case TargetType.Self:
                    if (user.IsAlive) r.Add(user);
                    break;
                case TargetType.FallenAlly:
                    foreach (var u in team) if (!u.IsAlive) r.Add(u);
                    break;
            }
            return r;
        }

        public static List<BattleUnit> ResolveTargets(ActionData a, BattleUnit user, BattleUnit chosen,
                                                      List<BattleUnit> team, List<BattleUnit> rivals)
        {
            var candidates = Candidates(a.target, user, team, rivals);
            if (!RequiresSelection(a.target)) return candidates;

            var r = new List<BattleUnit>();
            if (chosen != null && candidates.Contains(chosen)) r.Add(chosen);
            else if (candidates.Count > 0 && a.target != TargetType.FallenAlly)
                r.Add(candidates[Random.Range(0, candidates.Count)]);
            return r;
        }

        static int ScaledStat(ScalingStat s, BattleUnit u)
        {
            switch (s)
            {
                case ScalingStat.Attack: return u.GetStat(StatType.Attack);
                case ScalingStat.Wit: return u.GetStat(StatType.Wit);
                default: return 0;
            }
        }

        public static int CalculateDamage(EffectEntry e, BattleUnit u, BattleUnit t, BattleConfig cfg, out bool critical)
        {
            critical = false;
            float damage;
            if (e.scaling == ScalingStat.None)
            {
                damage = e.power;
            }
            else
            {
                float atk = ScaledStat(e.scaling, u);
                float def = e.scaling == ScalingStat.Wit
                    ? (t.GetStat(StatType.Defense) + t.GetStat(StatType.Wit)) * 0.5f
                    : t.GetStat(StatType.Defense);
                damage = e.power * 2f * atk / Mathf.Max(1f, atk + def);
                if (cfg != null && Random.value < cfg.criticalChance)
                {
                    critical = true;
                    damage *= cfg.criticalMultiplier;
                }
            }
            float variance = cfg != null ? cfg.damageVariance : 0.1f;
            damage *= Random.Range(1f - variance, 1f + variance);
            return Mathf.Max(1, Mathf.RoundToInt(damage));
        }

        public static int CalculateHeal(EffectEntry e, BattleUnit u)
        {
            if (e.scaling == ScalingStat.None) return Mathf.RoundToInt(e.power);
            return Mathf.RoundToInt(e.power * (1f + ScaledStat(e.scaling, u) / 50f));
        }

        public static void Apply(EffectEntry e, ActionData a, BattleUnit u, BattleUnit t, BattleConfig cfg, List<EffectResult> results)
        {
            switch (e.type)
            {
                case EffectType.Damage:
                case EffectType.Drain:
                {
                    if (!t.IsAlive) return;
                    int d = CalculateDamage(e, u, t, cfg, out bool crit);
                    int dealt = t.TakeDamage(d);
                    results.Add(new EffectResult
                    {
                        target = t, isDamage = true, critical = crit,
                        text = crit ? dealt + "!" : dealt.ToString(),
                        color = crit ? CriticalColor : DamageColor,
                        sound = crit ? FeedbackSound.Critical : (a.sound == FeedbackSound.None ? FeedbackSound.Hit : a.sound),
                        log = $"{t.DisplayName} takes {dealt} damage" + (crit ? " (CRITICAL!)" : "") + (t.guarding ? " (guarding)" : "")
                    });
                    if (e.type == EffectType.Drain)
                    {
                        int heal = u.Heal(Mathf.RoundToInt(dealt * e.drainPercent));
                        results.Add(new EffectResult
                        {
                            target = u, text = "+" + heal, color = HealColor, sound = FeedbackSound.Heal,
                            log = $"{u.DisplayName} drains {heal} HP"
                        });
                    }
                    break;
                }
                case EffectType.Heal:
                {
                    if (!t.IsAlive) return;
                    int h = t.Heal(CalculateHeal(e, u));
                    results.Add(new EffectResult
                    {
                        target = t, text = "+" + h, color = HealColor, sound = FeedbackSound.Heal,
                        log = $"{t.DisplayName} recovers {h} HP"
                    });
                    break;
                }
                case EffectType.RestoreMana:
                {
                    if (!t.IsAlive) return;
                    int m = t.RestoreMana(CalculateHeal(e, u));
                    results.Add(new EffectResult
                    {
                        target = t, text = "+" + m + " MP", color = ManaColor, sound = FeedbackSound.Heal,
                        log = $"{t.DisplayName} recovers {m} MP"
                    });
                    break;
                }
                case EffectType.ModifyStat:
                {
                    if (!t.IsAlive) return;
                    t.AddModifier(e.stat, e.multiplier, e.durationTurns, a.displayName);
                    bool buff = e.multiplier >= 1f;
                    int pct = Mathf.RoundToInt((e.multiplier - 1f) * 100f);
                    string abv = StatModifier.Abbreviation(e.stat);
                    results.Add(new EffectResult
                    {
                        target = t,
                        text = $"{abv} {(pct >= 0 ? "+" : "")}{pct}%",
                        color = buff ? BuffColor : DebuffColor,
                        sound = buff ? FeedbackSound.Buff : FeedbackSound.Debuff,
                        log = $"{t.DisplayName}'s {abv} {(buff ? "rises" : "drops")} {Mathf.Abs(pct)}% for {e.durationTurns} turns"
                    });
                    break;
                }
                case EffectType.AlterATB:
                {
                    if (!t.IsAlive) return;
                    t.AlterATB(e.atbAmount);
                    bool faster = e.atbAmount >= 0f;
                    results.Add(new EffectResult
                    {
                        target = t,
                        text = faster ? "Hurry!" : "Stunned!",
                        color = faster ? new Color(1f, 1f, 0.45f) : DebuffColor,
                        sound = faster ? FeedbackSound.Buff : FeedbackSound.Debuff,
                        log = $"{t.DisplayName}'s ATB bar {(faster ? "moves forward" : "is pushed back")} {Mathf.Abs(e.atbAmount):0}"
                    });
                    break;
                }
                case EffectType.Guard:
                {
                    if (!t.IsAlive) return;
                    t.StartGuard(e.multiplier);
                    results.Add(new EffectResult
                    {
                        target = t, text = "Guard", color = new Color(0.5f, 0.85f, 1f), sound = FeedbackSound.Buff,
                        log = $"{t.DisplayName} covers up"
                    });
                    break;
                }
                case EffectType.Revive:
                {
                    if (t.IsAlive) return;
                    t.Revive(e.power / 100f);
                    results.Add(new EffectResult
                    {
                        target = t, text = "Back up!", color = HealColor, sound = FeedbackSound.Heal,
                        log = $"{t.DisplayName} gets back up!"
                    });
                    break;
                }
            }
        }
    }
}
