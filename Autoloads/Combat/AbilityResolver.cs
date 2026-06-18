using System.Collections.Generic;
using Godot;
using Godot.Collections;

public class AbilityResolver
{
    // -------------------------------------------------------------------------
    // Per-Battle Tracking
    // -------------------------------------------------------------------------

    private readonly System.Collections.Generic.Dictionary<CombatAbility, int> cooldowns = new();
    private readonly HashSet<CombatAbility> usedOneTimeAbilities = new();
    private int currentRound = 0;

    // -------------------------------------------------------------------------
    // Round Management
    // -------------------------------------------------------------------------

    // Call at the start of each round before resolving abilities.
    public void AdvanceRound()
    {
        currentRound++;
        TickCooldowns();
    }

    // -------------------------------------------------------------------------
    // Core Resolution
    // -------------------------------------------------------------------------

    // Returns the ability to use this turn, or null for a normal attack.
    public CombatAbility ResolveAbility(
        Array<CombatAbility> abilities,
        float selfHPPercent,
        float targetHPPercent,
        StatusEffectManager selfEffects,
        StatusEffectManager targetEffects)
    {
        if (abilities == null || abilities.Count == 0)
            return null;

        // Gather valid candidates
        var candidates = new List<CombatAbility>();

        for (int i = 0; i < abilities.Count; i++)
        {
            CombatAbility ability = abilities[i];

            if (ability == null) continue;
            if (!IsOffCooldown(ability)) continue;
            if (usedOneTimeAbilities.Contains(ability)) continue;
            if (!IsTriggerMet(ability, selfHPPercent, targetHPPercent)) continue;
            if (!AreUseConditionsMet(ability, selfEffects, targetEffects)) continue;

            candidates.Add(ability);
        }

        if (candidates.Count == 0)
            return null;

        // Sort by priority descending (higher number = checked first)
        candidates.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        // Try highest priority first, roll UseChance
        for (int i = 0; i < candidates.Count; i++)
        {
            CombatAbility candidate = candidates[i];

            if (GD.Randf() <= candidate.UseChance)
            {
                OnAbilityUsed(candidate);
                return candidate;
            }
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // Reset (call on battle end)
    // -------------------------------------------------------------------------

    public void Reset()
    {
        cooldowns.Clear();
        usedOneTimeAbilities.Clear();
        currentRound = 0;
    }

    // -------------------------------------------------------------------------
    // Cooldown Management
    // -------------------------------------------------------------------------

    private bool IsOffCooldown(CombatAbility ability)
    {
        if (!cooldowns.ContainsKey(ability))
            return true;
        return cooldowns[ability] <= 0;
    }

    private void TickCooldowns()
    {
        var keys = new List<CombatAbility>(cooldowns.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            if (cooldowns[keys[i]] > 0)
                cooldowns[keys[i]]--;
        }
    }

    private void OnAbilityUsed(CombatAbility ability)
    {
        cooldowns[ability] = ability.CooldownRounds;

        if (ability.OneTimeUse)
            usedOneTimeAbilities.Add(ability);
    }

    // -------------------------------------------------------------------------
    // Trigger Evaluation
    // -------------------------------------------------------------------------

    private bool IsTriggerMet(
        CombatAbility ability,
        float selfHPPercent,
        float targetHPPercent)
    {
        switch (ability.Trigger)
        {
            case AbilityTrigger.OnCooldown:
                return true;

            case AbilityTrigger.WhenHPBelow:
                return selfHPPercent < ability.TriggerValue;

            case AbilityTrigger.WhenHPAbove:
                return selfHPPercent > ability.TriggerValue;

            case AbilityTrigger.WhenTargetHPBelow:
                return targetHPPercent < ability.TriggerValue;

            case AbilityTrigger.FirstRound:
                return currentRound == 1;

            case AbilityTrigger.EveryNRounds:
                int interval = Mathf.Max(1, (int)ability.TriggerValue);
                return currentRound % interval == 0;

            default:
                return false;
        }
    }

    // -------------------------------------------------------------------------
    // Use Condition Checks
    // -------------------------------------------------------------------------

    private bool AreUseConditionsMet(
        CombatAbility ability,
        StatusEffectManager selfEffects,
        StatusEffectManager targetEffects)
    {
        // Skip if target already has the effect we would apply
        if (ability.CheckTargetHasNoEffect && targetEffects != null)
        {
            if (targetEffects.HasEffect(ability.RequiresTargetNoEffect))
                return false;
        }

        // Skip if self does NOT have a required effect
        if (ability.CheckSelfHasEffect && selfEffects != null)
        {
            if (!selfEffects.HasEffect(ability.RequiresSelfEffect))
                return false;
        }

        return true;
    }
}
