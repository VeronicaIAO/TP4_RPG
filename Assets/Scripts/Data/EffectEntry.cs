using UnityEngine;

namespace TPRPG
{
    [System.Serializable]
    public class EffectEntry
    {
        public EffectType type = EffectType.Damage;
        public float power = 10f;
        public ScalingStat scaling = ScalingStat.Attack;
        public StatType stat = StatType.Attack;
        public float multiplier = 1.5f;
        [Min(1)] public int durationTurns = 3;
        [Range(-100f, 100f)] public float atbAmount = 0f;
        [Range(0f, 1f)] public float drainPercent = 0.5f;
    }
}
