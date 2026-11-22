using UnityEngine;

namespace TPRPG
{
    [CreateAssetMenu(menuName = "TP RPG/Battle Config", fileName = "BattleConfig")]
    public class BattleConfig : ScriptableObject
    {
        public ATBMode atbMode = ATBMode.Wait;
        [Min(0.1f)] public float globalATBSpeed = 3f;
        public bool pauseATBDuringActions = true;

        [Range(0f, 1f)] public float criticalChance = 0.08f;
        [Min(1f)] public float criticalMultiplier = 1.5f;
        [Range(0f, 0.5f)] public float damageVariance = 0.1f;

        [Min(0f)] public float delayBetweenActions = 0.25f;
        [Min(0.1f)] public float animationSpeed = 1f;
    }
}
