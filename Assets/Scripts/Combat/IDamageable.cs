using UnityEngine;

/// <summary>
/// Interface para qualquer entidade que pode receber dano.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
    void Die();
    bool IsDead { get; }
}
