namespace TPRPG
{
    public enum StatType { Attack, Defense, Wit, Speed }

    public enum TargetType
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        AllAllies,
        Self,
        FallenAlly
    }

    public enum EffectType
    {
        Damage,
        Heal,
        RestoreMana,
        ModifyStat,
        AlterATB,
        Guard,
        Revive,
        Drain
    }

    public enum ScalingStat { None, Attack, Wit }

    public enum AIProfile { Random, TargetWeakest, TargetStrongest }

    public enum ActionAnimation
    {
        None,
        Punch,
        Insult,
        HairPull,
        Shove,
        Callout,
        Defamation,
        Spit,
        Throw,
        Guard,
        Consume
    }

    public enum HitReaction
    {
        None,
        Hurt,
        Pain,
        Surprise,
        Outrage,
        Disgust
    }

    public enum ATBMode { Active, Wait }

    public enum FeedbackSound
    {
        None, Hit, Critical, Verbal, Shutter, Splat, Whoosh, Heal, Buff, Debuff,
        KO, Victory, Defeat, Select, Turn
    }
}
