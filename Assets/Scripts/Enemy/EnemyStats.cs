using UnityEngine;

/// <summary>
/// Atributos base de um inimigo — HP, dano, defesa, souls drop.
/// </summary>
public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    public float maxHealth = 80f;
    public float currentHealth;

    [Header("Combate")]
    public float attackDamage = 15f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public float defense = 3f;

    [Header("Recompensas")]
    public int soulsReward = 50;

    public bool IsDead { get; private set; }

    // Eventos
    public System.Action<float, float> OnHealthChanged;
    public System.Action OnEnemyDeath;

    private void Start()
    {
        currentHealth = maxHealth;
        IsDead = false;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Flash vermelho (visual feedback)
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        OnEnemyDeath?.Invoke();

        // Dar souls ao player
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player != null)
        {
            player.AddSouls(soulsReward);
        }

        // Desabilitar após morte (pode ser substituído por animação)
        Destroy(gameObject, 2f);
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Color original = rend.material.color;
            rend.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            rend.material.color = original;
        }
    }
}
