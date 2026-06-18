using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CombatAbility : Resource
{
    // -------------------------------------------------------------------------
    // Identity
    // -------------------------------------------------------------------------

    [ExportGroup("Identity")]
    [Export] public string AbilityId { get; set; } = "";
    [Export] public string AbilityName { get; set; } = "";
    [Export(PropertyHint.MultilineText)]
    public string AbilityDescription { get; set; } = "";
    [Export] public Color AbilityColor { get; set; } = new Color("9A7ABF");

    // -------------------------------------------------------------------------
    // Trigger and Timing
    // -------------------------------------------------------------------------

    [ExportGroup("Trigger")]
    [Export] public AbilityTrigger Trigger { get; set; } = AbilityTrigger.OnCooldown;
    [Export] public float TriggerValue { get; set; } = 0f;
    [Export] public int CooldownRounds { get; set; } = 0;
    [Export] public bool ReplacesAttack { get; set; } = true;
    [Export] public bool OneTimeUse { get; set; } = false;

    // -------------------------------------------------------------------------
    // AI Priority
    // -------------------------------------------------------------------------

    [ExportGroup("AI")]
    [Export] public int Priority { get; set; } = 0;
    [Export(PropertyHint.Range, "0,1,0.05")]
    public float UseChance { get; set; } = 1.0f;

    // -------------------------------------------------------------------------
    // Use Conditions (smart AI)
    // -------------------------------------------------------------------------

    [ExportGroup("Use Conditions")]
    [Export] public bool CheckTargetHasNoEffect { get; set; } = false;
    [Export] public StatusEffectType RequiresTargetNoEffect { get; set; }
    [Export] public bool CheckSelfHasEffect { get; set; } = false;
    [Export] public StatusEffectType RequiresSelfEffect { get; set; }

    // -------------------------------------------------------------------------
    // Hero Unlock (Phase 4 -- fields defined now, logic added later)
    // -------------------------------------------------------------------------

    [ExportGroup("Hero Unlock")]
    [Export] public int UnlockAtLevel { get; set; } = 0;
    [Export] public float KleosPurchaseCost { get; set; } = 0f;
    [Export] public string UnlockFromDungeonId { get; set; } = "";

    // -------------------------------------------------------------------------
    // Flavor Text
    // -------------------------------------------------------------------------

    [ExportGroup("Flavor")]
    [Export(PropertyHint.MultilineText)]
    public string CastFlavorText { get; set; } = "";

    // -------------------------------------------------------------------------
    // Effects
    // -------------------------------------------------------------------------

    [ExportGroup("Effects")]
    [Export] public Array<AbilityEffect> Effects { get; set; } = new();
}
