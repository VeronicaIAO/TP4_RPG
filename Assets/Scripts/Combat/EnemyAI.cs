using System.Collections.Generic;
using UnityEngine;

namespace TPRPG
{
    public class EnemyAI : MonoBehaviour
    {
        [Range(0f, 1f)] public float healThreshold = 0.35f;

        public struct Decision
        {
            public ActionData action;
            public BattleUnit target;
        }

        static bool IsHeal(ActionData a)
        {
            foreach (var e in a.effects)
                if (e.type == EffectType.Heal) return true;
            return false;
        }

        public Decision Decide(BattleUnit self, List<BattleUnit> team, List<BattleUnit> rivals)
        {
            var usable = new List<SkillData>();
            foreach (var s in self.data.skills)
                if (s != null && self.currentMana >= s.manaCost) usable.Add(s);

            BattleUnit wounded = null;
            foreach (var c in team)
                if (c.IsAlive && c.HealthPercent < healThreshold && (wounded == null || c.HealthPercent < wounded.HealthPercent))
                    wounded = c;
            if (wounded != null)
            {
                foreach (var s in usable)
                {
                    if (!IsHeal(s)) continue;
                    var t = ChooseTarget(s, self, team, rivals);
                    if (t != null || !ActionResolver.RequiresSelection(s.target))
                        return new Decision { action = s, target = s.target == TargetType.SingleAlly ? wounded : t };
                }
            }

            usable.RemoveAll(s => IsHeal(s));
            if (usable.Count > 0 && Random.value < self.data.skillChance)
            {
                var s = usable[Random.Range(0, usable.Count)];
                var t = ChooseTarget(s, self, team, rivals);
                if (t != null || !ActionResolver.RequiresSelection(s.target))
                    return new Decision { action = s, target = t };
            }

            var basic = self.data.basicAttack;
            return new Decision { action = basic, target = basic != null ? ChooseTarget(basic, self, team, rivals) : null };
        }

        BattleUnit ChooseTarget(ActionData a, BattleUnit self, List<BattleUnit> team, List<BattleUnit> rivals)
        {
            var candidates = ActionResolver.Candidates(a.target, self, team, rivals);
            if (candidates.Count == 0) return null;
            if (a.target == TargetType.Self) return self;

            if (!ActionResolver.TargetsRivals(a.target))
            {
                BattleUnit lowest = candidates[0];
                foreach (var c in candidates) if (c.HealthPercent < lowest.HealthPercent) lowest = c;
                return lowest;
            }

            switch (self.data.aiProfile)
            {
                case AIProfile.TargetWeakest:
                {
                    BattleUnit m = candidates[0];
                    foreach (var c in candidates) if (c.currentHealth < m.currentHealth) m = c;
                    return m;
                }
                case AIProfile.TargetStrongest:
                {
                    BattleUnit m = candidates[0];
                    foreach (var c in candidates)
                        if (c.GetStat(StatType.Attack) + c.GetStat(StatType.Wit) >
                            m.GetStat(StatType.Attack) + m.GetStat(StatType.Wit)) m = c;
                    return m;
                }
                default:
                    return candidates[Random.Range(0, candidates.Count)];
            }
        }
    }
}
