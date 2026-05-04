// StatusEffectType.cs
// Location: res://Autoloads/Combat/StatusEffectType.cs
//
// All status effect types and the flat/percentage mode enum.
// Every type is defined now even though Phase 1 only uses stat
// modifiers. Later phases activate the rest without touching this file.

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
