using System.Collections.Generic;
using UnityEngine;

namespace TPRPG
{
    [CreateAssetMenu(menuName = "TP RPG/Character", fileName = "NewCharacter")]
    public class CharacterData : ScriptableObject
    {
        public string displayName = "Character";
        public Color mainColor = Color.white;

        [Min(1)] public int maxHealth = 100;
        [Min(0)] public int maxMana = 30;

        [Min(1)] public int attack = 10;
        [Min(1)] public int defense = 10;
        [Min(1)] public int wit = 10;
        [Min(0.1f)] public float speed = 10f;

        [Range(0f, 100f)] public float startATBMin = 0f;
        [Range(0f, 100f)] public float startATBMax = 40f;

        public SkillData basicAttack;
        public SkillData guardAction;
        public List<SkillData> skills = new List<SkillData>();

        public AIProfile aiProfile = AIProfile.Random;
        [Range(0f, 1f)] public float skillChance = 0.4f;

        public CharacterVoice voice;
    }
}
