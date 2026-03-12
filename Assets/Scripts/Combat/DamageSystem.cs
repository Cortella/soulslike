using UnityEngine;

/// <summary>
/// Sistema de dano centralizado — calcula dano base, defesa, 
/// e dispara eventos de hit/morte.
/// </summary>
public static class DamageSystem
{
    /// <summary>
    /// Aplica dano a um alvo IDamageable, levando em conta defesa.
    /// </summary>
    public static void ApplyDamage(IDamageable target, float rawDamage, float defenseReduction = 0f)
    {
        if (target == null || target.IsDead) return;

        float finalDamage = Mathf.Max(rawDamage - defenseReduction, 1f);
        target.TakeDamage(finalDamage);
    }

    /// <summary>
    /// Calcula dano crítico (backstab / riposte).
    /// </summary>
    public static float CalculateCriticalDamage(float baseDamage, float critMultiplier = 2.5f)
    {
        return baseDamage * critMultiplier;
    }
}
