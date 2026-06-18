public enum StatusEffectType
{
    // Stat Modifiers
    AttackDamageUp,
    AttackDamageDown,
    AttackSpeedUp,
    AttackSpeedDown,
    CritChanceUp,
    CritImmunity,
    DodgeUp,
    DodgeDown,

    // Damage Over Time
    Bleed,
    Poison,
    Burn,

    // Healing Over Time
    Regeneration,

    // Defensive
    Shield,
    DamageReflect,
    DamageAbsorb,

    // Special
    Stun,
    WeaponSteal
}

public enum StatusEffectMode
{
    Flat,
    Percentage
}
