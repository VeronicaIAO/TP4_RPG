namespace TPRPG
{
    [System.Serializable]
    public class StatModifier
    {
        public StatType stat;
        public float multiplier;
        public int turnsLeft;
        public string source;

        public bool IsBuff => multiplier >= 1f;

        public static string Abbreviation(StatType s)
        {
            switch (s)
            {
                case StatType.Attack: return "ATK";
                case StatType.Defense: return "DEF";
                case StatType.Wit: return "WIT";
                default: return "SPD";
            }
        }
    }
}
