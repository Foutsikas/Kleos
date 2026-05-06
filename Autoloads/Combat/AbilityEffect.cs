// AbilityEffect.cs
// Location: res://Resources/Combat/AbilityEffect.cs
//
// One effect within a CombatAbility. An ability can have multiple effects
// (e.g. deal damage AND apply a debuff in the same action).
//
// This is a Godot Resource so it can be edited in the Inspector as part
// of a CombatAbility's Effects array.

using Godot;

[GlobalClass]
public partial class AbilityEffect : Resource
{
    // -------------------------------------------------------------------------
    // Core
    // -------------------------------------------------------------------------

    [ExportGroup("Effect")]
    [Export] public AbilityEffectType EffectType { get; set; } = AbilityEffectType.DealDamage;
    [Export] public AbilityTargetType Target { get; set; } = AbilityTargetType.Enemy;
    [Export] public float Value { get; set; } = 0f;

    // -------------------------------------------------------------------------
    // Status Effect Fields (used when EffectType is ApplyStatus or ApplySelfStatus)
    // -------------------------------------------------------------------------

    [ExportGroup("Status Effect")]
    [Export] public StatusEffectType StatusType { get; set; } = StatusEffectType.AttackDamageUp;
    [Export] public string StatusName { get; set; } = "";
    [Export] public float StatusValue { get; set; } = 0f;
    [Export] public int StatusDuration { get; set; } = 3;
    [Export] public bool StatusIsDebuff { get; set; } = true;
    [Export] public StatusEffectMode StatusMode { get; set; } = StatusEffectMode.Flat;
    [Export] public int StatusMaxStacks { get; set; } = 1;

    // -------------------------------------------------------------------------
    // Status Flavor Text
    // -------------------------------------------------------------------------

    [ExportGroup("Status Flavor Text")]
    [Export] public string StatusApplyText { get; set; } = "";
    [Export] public string StatusTickText { get; set; } = "";
    [Export] public string StatusExpireText { get; set; } = "";
}
