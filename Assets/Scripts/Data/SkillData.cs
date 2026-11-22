using UnityEngine;

namespace TPRPG
{
    [CreateAssetMenu(menuName = "TP RPG/Skill", fileName = "NewSkill")]
    public class SkillData : ActionData
    {
        [Min(0)] public int manaCost = 0;

        public override string CostLabel => manaCost > 0 ? manaCost + " MP" : "";
    }
}
