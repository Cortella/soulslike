using UnityEngine;

/// <summary>
/// Gerencia os atributos do jogador: HP, Stamina, Souls, Level.
/// </summary>
public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 25f;
    public float staminaRegenDelay = 1f;

    [Header("Almas (Souls)")]
    public int souls = 0;
    public int soulLevel = 1;

    [Header("Defesa")]
    public float baseDefense = 5f;

    // Flags
    public bool IsDead { get; private set; }
    public bool IsInvulnerable { get; set; }

    private float staminaRegenTimer;

    // Eventos
    public System.Action<float, float> OnHealthChanged;   // current, max
    public System.Action<float, float> OnStaminaChanged;   // current, max
    public System.Action<int> OnSoulsChanged;
    public System.Action OnPlayerDeath;

    private void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        IsDead = false;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        OnSoulsChanged?.Invoke(souls);
    }

    private void Update()
    {
        if (IsDead) return;
        RegenerateStamina();
    }

    #region Health

    public void TakeDamage(float amount)
    {
        if (IsDead || IsInvulnerable) return;

        float finalDamage = Mathf.Max(amount - baseDefense, 1f);
        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Die()
    {
        IsDead = true;
        OnPlayerDeath?.Invoke();
        Debug.Log("YOU DIED");
    }

    #endregion

    #region Stamina

    public bool HasStamina(float amount)
    {
        return currentStamina >= amount;
    }

    public void ConsumeStamina(float amount)
    {
        currentStamina -= amount;
        currentStamina = Mathf.Max(currentStamina, 0f);
        staminaRegenTimer = staminaRegenDelay;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private void RegenerateStamina()
    {
        if (staminaRegenTimer > 0f)
        {
            staminaRegenTimer -= Time.deltaTime;
            return;
        }

        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    #endregion

    #region Souls

    public void AddSouls(int amount)
    {
        souls += amount;
        OnSoulsChanged?.Invoke(souls);
    }

    public bool SpendSouls(int amount)
    {
        if (souls >= amount)
        {
            souls -= amount;
            OnSoulsChanged?.Invoke(souls);
            return true;
        }
        return false;
    }

    #endregion
}
