using System.Collections.Generic;
using UnityEngine;

namespace TPRPG
{
    public abstract class ActionData : ScriptableObject
    {
        public string displayName = "Action";
        [TextArea(2, 4)] public string description = "";
        public TargetType target = TargetType.SingleEnemy;
        [Range(-60f, 100f)] public float recoveryCost = 0f;
        public List<EffectEntry> effects = new List<EffectEntry>();

        public ActionAnimation actionAnimation = ActionAnimation.Punch;
        public HitReaction reaction = HitReaction.Hurt;
        public Color effectColor = Color.white;
        public FeedbackSound sound = FeedbackSound.Hit;

        public virtual string CostLabel => "";
    }
}
