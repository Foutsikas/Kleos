// AbilityEnums.cs
// Location: res://Resources/Combat/AbilityEnums.cs
//
// All enums used by the ability framework.

public enum AbilityEffectType
{
    DealDamage,
    ApplyStatus,
    ApplySelfStatus,
    HealSelf,
    HealTarget,
    RemoveDebuff,
    RemoveBuff
}

public enum AbilityTargetType
{
    Self,
    Enemy
}

public enum AbilityTrigger
{
    OnCooldown,
    WhenHPBelow,
    WhenHPAbove,
    WhenTargetHPBelow,
    FirstRound,
    EveryNRounds
}
