// StatusEffect.cs
// Location: res://Autoloads/Combat/StatusEffect.cs
//
// A single status effect instance on a combatant. Created when an
// ability applies an effect, destroyed when duration reaches zero.
// Plain C# class -- not a Node, not a Resource.

using Godot;

public class StatusEffect
{
    // -------------------------------------------------------------------------
    // Core Fields
    // -------------------------------------------------------------------------

    public StatusEffectType Type;
    public string EffectName;
    public float Value;
    public int Duration;
    public int MaxStacks;
    public int CurrentStacks;
    public bool IsDebuff;
    public StatusEffectMode Mode;

    // Tracks which source applied this effect (for stacking rules)
    public string SourceId;

    // -------------------------------------------------------------------------
    // Battle Log Text
    // -------------------------------------------------------------------------

    public string ApplyFlavorText;
    public string TickFlavorText;
    public string ExpireFlavorText;

    // -------------------------------------------------------------------------
    // Constructors
    // -------------------------------------------------------------------------

    public StatusEffect() { }

    public StatusEffect(
        StatusEffectType type,
        string name,
        float value,
        int duration,
        bool isDebuff,
        StatusEffectMode mode = StatusEffectMode.Flat,
        int maxStacks = 1)
    {
        Type = type;
        EffectName = name;
        Value = value;
        Duration = duration;
        IsDebuff = isDebuff;
        Mode = mode;
        MaxStacks = Mathf.Max(1, maxStacks);
        CurrentStacks = 1;
        SourceId = "";
        ApplyFlavorText = "";
        TickFlavorText = "";
        ExpireFlavorText = "";
    }

    // -------------------------------------------------------------------------
    // Clone
    // -------------------------------------------------------------------------

    // Creates an independent copy. Used when applying effects from ability
    // templates so the template data is never mutated.
    public StatusEffect Clone()
    {
        return new StatusEffect
        {
            Type = this.Type,
            EffectName = this.EffectName,
            Value = this.Value,
            Duration = this.Duration,
            MaxStacks = this.MaxStacks,
            CurrentStacks = this.CurrentStacks,
            IsDebuff = this.IsDebuff,
            Mode = this.Mode,
            SourceId = this.SourceId,
            ApplyFlavorText = this.ApplyFlavorText,
            TickFlavorText = this.TickFlavorText,
            ExpireFlavorText = this.ExpireFlavorText
        };
    }

    // -------------------------------------------------------------------------
    // Type Helpers
    // -------------------------------------------------------------------------

    public bool IsStatModifier()
    {
        return Type == StatusEffectType.AttackDamageUp
            || Type == StatusEffectType.AttackDamageDown
            || Type == StatusEffectType.AttackSpeedUp
            || Type == StatusEffectType.AttackSpeedDown
            || Type == StatusEffectType.CritChanceUp
            || Type == StatusEffectType.CritImmunity
            || Type == StatusEffectType.DodgeUp
            || Type == StatusEffectType.DodgeDown
            || Type == StatusEffectType.WeaponSteal;
    }

    public bool IsDamageOverTime()
    {
        return Type == StatusEffectType.Bleed
            || Type == StatusEffectType.Poison
            || Type == StatusEffectType.Burn;
    }

    public bool IsHealOverTime()
    {
        return Type == StatusEffectType.Regeneration;
    }

    public bool IsDefensive()
    {
        return Type == StatusEffectType.Shield
            || Type == StatusEffectType.DamageReflect
            || Type == StatusEffectType.DamageAbsorb;
    }
}
