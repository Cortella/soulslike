using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Boss enemy com fases, padrões de ataque e mecânicas especiais.
/// Estilo Dark Souls: 2 fases, ataques telegrafados, arena fechada.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BossEnemy : MonoBehaviour, IDamageable
{
    public enum BossPhase { Phase1, Phase2 }
    public enum BossState { Idle, Walking, Attacking, Charging, Stunned, Dead }

    [Header("Status")]
    public float maxHealth = 500f;
    public float currentHealth;
    public float phase2HealthThreshold = 0.5f; // 50% HP

    [Header("Fase 1 - Ataques")]
    public float phase1Damage = 25f;
    public float phase1AttackCooldown = 2f;
    public float phase1Speed = 3f;

    [Header("Fase 2 - Ataques")]
    public float phase2Damage = 40f;
    public float phase2AttackCooldown = 1.2f;
    public float phase2Speed = 5f;
    public Color phase2AuraColor = new Color(0.8f, 0.2f, 0.1f);

    [Header("Recompensas")]
    public int soulsReward = 1000;
    public string bossName = "Knight of the Abyss";

    // Componentes
    private NavMeshAgent agent;
    private Transform playerTransform;
    private Renderer meshRenderer;

    // Estado
    public BossPhase currentPhase = BossPhase.Phase1;
    public BossState currentState = BossState.Idle;
    public bool IsDead { get; private set; }

    private float attackTimer;
    private bool isActivated;

    // Eventos
    public System.Action<float, float> OnBossHealthChanged;
    public System.Action<BossPhase> OnPhaseChanged;
    public System.Action OnBossDeath;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        agent.speed = phase1Speed;
        IsDead = false;

        // Iniciar inativo (esperar fog gate trigger)
        agent.isStopped = true;
        currentState = BossState.Idle;
    }

    /// <summary>
    /// Chamado quando o player passa pelo Fog Gate.
    /// </summary>
    public void ActivateBoss()
    {
        isActivated = true;
        currentState = BossState.Walking;
        agent.isStopped = false;

        // Encontrar player
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
            playerTransform = pc.transform;

        Debug.Log($"[Boss] {bossName} ativado!");
    }

    private void Update()
    {
        if (IsDead || !isActivated) return;

        attackTimer -= Time.deltaTime;

        switch (currentState)
        {
            case BossState.Walking:
                UpdateWalking();
                break;
            case BossState.Attacking:
                UpdateAttacking();
                break;
            case BossState.Stunned:
                // Aguardar fim do stun
                break;
        }
    }

    private void UpdateWalking()
    {
        if (playerTransform == null) return;

        agent.SetDestination(playerTransform.position);

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        float attackRange = currentPhase == BossPhase.Phase1 ? 3f : 4f;

        if (dist <= attackRange && attackTimer <= 0f)
        {
            currentState = BossState.Attacking;
            agent.isStopped = true;
            PerformAttack();
        }
    }

    private void UpdateAttacking()
    {
        // Após o ataque, voltar a andar
        // (simplificado — na prática teria uma animação)
    }

    private void PerformAttack()
    {
        float damage = currentPhase == BossPhase.Phase1 ? phase1Damage : phase2Damage;
        float cooldown = currentPhase == BossPhase.Phase1 ? phase1AttackCooldown : phase2AttackCooldown;

        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= 4f)
            {
                IDamageable target = playerTransform.GetComponent<IDamageable>();
                if (target != null)
                {
                    PlayerCombat combat = playerTransform.GetComponent<PlayerCombat>();
                    if (combat != null && combat.IsBlocking)
                    {
                        float reduced = combat.ProcessBlockedDamage(damage);
                        if (reduced > 0) target.TakeDamage(reduced);
                    }
                    else
                    {
                        DamageSystem.ApplyDamage(target, damage);
                    }
                }
            }
        }

        attackTimer = cooldown;

        // Voltar ao estado de walking após breve delay
        Invoke(nameof(ReturnToWalking), 0.8f);
    }

    private void ReturnToWalking()
    {
        if (IsDead) return;
        currentState = BossState.Walking;
        agent.isStopped = false;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnBossHealthChanged?.Invoke(currentHealth, maxHealth);

        // Flash
        StartCoroutine(DamageFlash());

        // Checar mudança de fase
        if (currentPhase == BossPhase.Phase1 &&
            currentHealth / maxHealth <= phase2HealthThreshold)
        {
            EnterPhase2();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void EnterPhase2()
    {
        currentPhase = BossPhase.Phase2;
        agent.speed = phase2Speed;

        // Efeito visual: aura vermelha
        if (meshRenderer != null)
        {
            Material mat = meshRenderer.material;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", phase2AuraColor * 2f);
        }

        // Breve stun durante transição
        currentState = BossState.Stunned;
        agent.isStopped = true;
        Invoke(nameof(ReturnToWalking), 1.5f);

        OnPhaseChanged?.Invoke(BossPhase.Phase2);
        Debug.Log($"[Boss] {bossName} entrou na FASE 2!");
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        currentState = BossState.Dead;
        agent.isStopped = true;

        // Dar souls
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player != null)
            player.AddSouls(soulsReward);

        OnBossDeath?.Invoke();
        Debug.Log($"[Boss] {bossName} DERROTADO! +{soulsReward} Souls");

        Destroy(gameObject, 5f);
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        if (meshRenderer != null)
        {
            Color original = meshRenderer.material.color;
            meshRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            meshRenderer.material.color = original;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 3f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 4f);
    }
}
