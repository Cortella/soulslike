using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de combate do jogador: light attack, heavy attack, block, parry.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Ataque Leve")]
    public float lightAttackDamage = 20f;
    public float lightAttackStaminaCost = 15f;
    public float lightAttackCooldown = 0.6f;
    public float lightAttackRange = 2f;

    [Header("Ataque Pesado")]
    public float heavyAttackDamage = 45f;
    public float heavyAttackStaminaCost = 30f;
    public float heavyAttackCooldown = 1.2f;
    public float heavyAttackRange = 2.5f;

    [Header("Bloqueio")]
    public float blockDamageReduction = 0.7f; // 70% redução
    public float blockStaminaCost = 10f;
    public float parryWindow = 0.2f;

    [Header("Referências")]
    public Transform attackPoint;
    public LayerMask enemyLayers;

    // Componentes
    private PlayerStats playerStats;
    private PlayerController playerController;

    // Estado
    private float attackCooldownTimer;
    private bool isBlocking;
    private bool isAttacking;
    private float parryTimer;
    private bool canParry;

    public bool IsBlocking => isBlocking;
    public bool IsAttacking => isAttacking;

    // Eventos
    public System.Action<string> OnAttackPerformed; // "light", "heavy"
    public System.Action OnBlockStart;
    public System.Action OnBlockEnd;
    public System.Action OnParrySuccess;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        if (attackPoint == null)
            attackPoint = transform;
    }

    private void Update()
    {
        if (playerStats != null && playerStats.IsDead) return;

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        if (canParry)
        {
            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0f) canParry = false;
        }
    }

    #region Input Callbacks

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (context.started)
            PerformLightAttack();
    }

    public void OnHeavyAttack(InputAction.CallbackContext context)
    {
        if (context.started)
            PerformHeavyAttack();
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            StartBlock();
        }
        else if (context.canceled)
        {
            EndBlock();
        }
    }

    #endregion

    #region Light Attack

    private void PerformLightAttack()
    {
        if (attackCooldownTimer > 0f || isBlocking || isAttacking) return;
        if (playerController != null && playerController.IsDodging) return;
        if (playerStats != null && !playerStats.HasStamina(lightAttackStaminaCost)) return;

        isAttacking = true;
        playerStats?.ConsumeStamina(lightAttackStaminaCost);
        attackCooldownTimer = lightAttackCooldown;

        // Detectar inimigos no alcance
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, lightAttackRange, enemyLayers);
        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target == null) target = hit.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                DamageSystem.ApplyDamage(target, lightAttackDamage);
            }
        }

        OnAttackPerformed?.Invoke("light");
        Invoke(nameof(EndAttack), lightAttackCooldown * 0.8f);
    }

    #endregion

    #region Heavy Attack

    private void PerformHeavyAttack()
    {
        if (attackCooldownTimer > 0f || isBlocking || isAttacking) return;
        if (playerController != null && playerController.IsDodging) return;
        if (playerStats != null && !playerStats.HasStamina(heavyAttackStaminaCost)) return;

        isAttacking = true;
        playerStats?.ConsumeStamina(heavyAttackStaminaCost);
        attackCooldownTimer = heavyAttackCooldown;

        Collider[] hits = Physics.OverlapSphere(attackPoint.position, heavyAttackRange, enemyLayers);
        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target == null) target = hit.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                DamageSystem.ApplyDamage(target, heavyAttackDamage);
            }
        }

        OnAttackPerformed?.Invoke("heavy");
        Invoke(nameof(EndAttack), heavyAttackCooldown * 0.8f);
    }

    #endregion

    #region Block / Parry

    private void StartBlock()
    {
        if (playerStats != null && !playerStats.HasStamina(blockStaminaCost)) return;

        isBlocking = true;
        canParry = true;
        parryTimer = parryWindow;

        OnBlockStart?.Invoke();
    }

    private void EndBlock()
    {
        isBlocking = false;
        canParry = false;
        OnBlockEnd?.Invoke();
    }

    /// <summary>
    /// Chamado quando o player recebe dano enquanto bloqueia.
    /// Retorna o dano restante após o bloqueio.
    /// </summary>
    public float ProcessBlockedDamage(float incomingDamage)
    {
        if (canParry)
        {
            OnParrySuccess?.Invoke();
            return 0f; // Parry bem sucedido: sem dano
        }

        playerStats?.ConsumeStamina(blockStaminaCost);
        return incomingDamage * (1f - blockDamageReduction);
    }

    #endregion

    private void EndAttack()
    {
        isAttacking = false;
    }

    #region Direct Input Methods (chamados por DirectInputHandler)

    public void TryLightAttack() => PerformLightAttack();
    public void TryHeavyAttack() => PerformHeavyAttack();

    public void SetBlocking(bool blocking)
    {
        if (blocking && !isBlocking) StartBlock();
        else if (!blocking && isBlocking) EndBlock();
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, lightAttackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, heavyAttackRange);
    }
}
