// StatusEffectManager.cs
// Location: res://Autoloads/Combat/StatusEffectManager.cs
//
// Manages all active status effects on a single combatant.
// BattleSystem creates two instances: one for the hero, one for the enemy.
// Both are cleared when the battle ends.
//
// Plain C# class -- not a Node, not an Autoload. No scene tree dependency.

using System;
using System.Collections.Generic;
using Godot;

public class StatusEffectManager
{
    // -------------------------------------------------------------------------
    // Active Effects
    // -------------------------------------------------------------------------

    private readonly List<StatusEffect> activeEffects = new();

    // -------------------------------------------------------------------------
    // Callbacks (wired by BattleSystem for logging and HP updates)
    // -------------------------------------------------------------------------

    public Action<float> OnDamageTaken;
    public Action<float> OnHealingReceived;
    public Action<string> OnEffectApplied;
    public Action<string> OnEffectTicked;
    public Action<string> OnEffectExpired;

    // -------------------------------------------------------------------------
    // Apply / Remove
    // -------------------------------------------------------------------------

    public void ApplyEffect(StatusEffect effect, string sourceId)
    {
        effect.SourceId = sourceId;

        // Check for existing effect of same type
        StatusEffect existing = activeEffects.Find(
            e => e.Type == effect.Type);

        if (existing != null)
        {
            // Same source: refresh duration, no new stacks
            if (existing.SourceId == sourceId)
            {
                existing.Duration = Mathf.Max(existing.Duration, effect.Duration);
            }
            // Different source: stack up to max
            else if (existing.CurrentStacks < existing.MaxStacks)
            {
                existing.CurrentStacks++;
                existing.Duration = Mathf.Max(existing.Duration, effect.Duration);
            }
            // At max stacks: just refresh duration
            else
            {
                existing.Duration = Mathf.Max(existing.Duration, effect.Duration);
            }
        }
        else
        {
            activeEffects.Add(effect);
        }

        if (!string.IsNullOrEmpty(effect.ApplyFlavorText))
        {
            OnEffectApplied?.Invoke(effect.ApplyFlavorText);
        }
    }

    public void RemoveEffect(StatusEffectType type)
    {
        activeEffects.RemoveAll(e => e.Type == type);
    }

    // -------------------------------------------------------------------------
    // Round Processing
    // -------------------------------------------------------------------------

    // Called at the start of each round. Ticks DoTs and HoTs, decrements
    // durations, expires effects at zero. Returns log messages.
    public List<string> ProcessStartOfRound()
    {
        var logMessages = new List<string>();

        // Process in reverse so removal during iteration is safe
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeEffects[i];

            // Tick damage-over-time effects
            if (effect.IsDamageOverTime())
            {
                float tickDamage = effect.Value * effect.CurrentStacks;
                OnDamageTaken?.Invoke(tickDamage);

                string tickMsg = !string.IsNullOrEmpty(effect.TickFlavorText)
                    ? $"{effect.TickFlavorText} {tickDamage:F0} damage."
                    : $"{effect.EffectName} deals {tickDamage:F0} damage.";
                logMessages.Add(tickMsg);
                OnEffectTicked?.Invoke(tickMsg);
            }

            // Tick healing-over-time effects
            if (effect.IsHealOverTime())
            {
                float tickHeal = effect.Value * effect.CurrentStacks;
                OnHealingReceived?.Invoke(tickHeal);

                string healMsg = !string.IsNullOrEmpty(effect.TickFlavorText)
                    ? $"{effect.TickFlavorText} {tickHeal:F0} healed."
                    : $"{effect.EffectName} heals {tickHeal:F0}.";
                logMessages.Add(healMsg);
                OnEffectTicked?.Invoke(healMsg);
            }

            // Decrement duration
            effect.Duration--;

            // Expire if duration is zero
            if (effect.Duration <= 0)
            {
                string expireMsg = !string.IsNullOrEmpty(effect.ExpireFlavorText)
                    ? effect.ExpireFlavorText
                    : $"{effect.EffectName} fades.";
                logMessages.Add(expireMsg);
                OnEffectExpired?.Invoke(expireMsg);

                activeEffects.RemoveAt(i);
            }
        }

        return logMessages;
    }

    // Called at the end of each round. Reserved for future end-of-round triggers.
    public void ProcessEndOfRound()
    {
        // Currently empty. Phase 3 may add end-of-round triggers here.
    }

    // -------------------------------------------------------------------------
    // Stat Queries (Phase 1 core -- used by BattleSystem damage pipeline)
    // -------------------------------------------------------------------------

    // Applies all active damage modifiers to a base damage value.
    // AttackDamageUp increases, AttackDamageDown and WeaponSteal decrease.
    // Result is never less than 1.
    public float GetModifiedDamage(float baseDamage)
    {
        float result = baseDamage;

        foreach (var effect in activeEffects)
        {
            if (effect.Type == StatusEffectType.AttackDamageUp)
            {
                if (effect.Mode == StatusEffectMode.Flat)
                    result += effect.Value * effect.CurrentStacks;
                else
                    result += baseDamage * (effect.Value * effect.CurrentStacks);
            }
            else if (effect.Type == StatusEffectType.AttackDamageDown)
            {
                if (effect.Mode == StatusEffectMode.Flat)
                    result -= effect.Value * effect.CurrentStacks;
                else
                    result -= baseDamage * (effect.Value * effect.CurrentStacks);
            }
            else if (effect.Type == StatusEffectType.WeaponSteal)
            {
                if (effect.Mode == StatusEffectMode.Flat)
                    result -= effect.Value * effect.CurrentStacks;
                else
                    result -= baseDamage * (effect.Value * effect.CurrentStacks);
            }
        }

        return Mathf.Max(1f, result);
    }

    // Applies dodge modifiers to base dodge chance. Returns clamped 0-1.
    public float GetModifiedDodgeChance(float baseChance)
    {
        float result = baseChance;

        foreach (var effect in activeEffects)
        {
            if (effect.Type == StatusEffectType.DodgeUp)
            {
                if (effect.Mode == StatusEffectMode.Flat)
                    result += effect.Value * effect.CurrentStacks;
                else
                    result += baseChance * (effect.Value * effect.CurrentStacks);
            }
            else if (effect.Type == StatusEffectType.DodgeDown)
            {
                if (effect.Mode == StatusEffectMode.Flat)
                    result -= effect.Value * effect.CurrentStacks;
                else
                    result -= baseChance * (effect.Value * effect.CurrentStacks);
            }
        }

        return Mathf.Clamp(result, 0f, 1f);
    }

    // Applies crit chance modifiers. Returns clamped 0-1.
    public float GetModifiedCritChance(float baseChance)
    {
        float result = baseChance;

        foreach (var effect in activeEffects)
        {
            if (effect.Type == StatusEffectType.CritChanceUp)
            {
                if (effect.Mode == StatusEffectMode.Flat)
                    result += effect.Value * effect.CurrentStacks;
                else
                    result += baseChance * (effect.Value * effect.CurrentStacks);
            }
        }

        return Mathf.Clamp(result, 0f, 1f);
    }

    // -------------------------------------------------------------------------
    // Effect Checks
    // -------------------------------------------------------------------------

    public bool HasEffect(StatusEffectType type)
    {
        return activeEffects.Exists(e => e.Type == type);
    }

    public bool HasCritImmunity()
    {
        return HasEffect(StatusEffectType.CritImmunity);
    }

    public bool IsStunned()
    {
        return HasEffect(StatusEffectType.Stun);
    }

    // -------------------------------------------------------------------------
    // Shield and Reflect (Phase 3 -- methods exist now, activate later)
    // -------------------------------------------------------------------------

    // Returns total remaining shield absorption. 0 if no shield active.
    public float GetShieldAmount()
    {
        float total = 0f;
        foreach (var effect in activeEffects)
        {
            if (effect.Type == StatusEffectType.Shield)
                total += effect.Value * effect.CurrentStacks;
        }
        return total;
    }

    // Processes incoming damage through shield. Returns the leftover
    // damage that passes through to HP. Depletes shield value.
    public float AbsorbDamage(float incomingDamage)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].Type != StatusEffectType.Shield)
                continue;

            StatusEffect shield = activeEffects[i];
            float absorbed = Mathf.Min(incomingDamage, shield.Value);
            shield.Value -= absorbed;
            incomingDamage -= absorbed;

            if (shield.Value <= 0f)
            {
                string expireMsg = !string.IsNullOrEmpty(shield.ExpireFlavorText)
                    ? shield.ExpireFlavorText
                    : $"{shield.EffectName} shatters.";
                OnEffectExpired?.Invoke(expireMsg);
                activeEffects.RemoveAt(i);
            }

            if (incomingDamage <= 0f)
                return 0f;
        }

        return incomingDamage;
    }

    // Returns the percentage of damage that should be reflected back.
    // 0 if no DamageReflect is active.
    public float GetReflectPercent()
    {
        float total = 0f;
        foreach (var effect in activeEffects)
        {
            if (effect.Type == StatusEffectType.DamageReflect)
                total += effect.Value * effect.CurrentStacks;
        }
        return Mathf.Clamp(total, 0f, 1f);
    }

    // -------------------------------------------------------------------------
    // UI Access
    // -------------------------------------------------------------------------

    // Returns a read-only snapshot for StatusEffectDisplay to render.
    public IReadOnlyList<StatusEffect> GetActiveEffects()
    {
        return activeEffects.AsReadOnly();
    }

    // -------------------------------------------------------------------------
    // Cleanup
    // -------------------------------------------------------------------------

    public void ClearAll()
    {
        activeEffects.Clear();
    }
}
