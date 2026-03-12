using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// IA de inimigo estilo Soulslike com estados: Idle, Patrol, Chase, Attack, Stagger.
/// Usa NavMeshAgent para navegação.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Stagger,
        Dead
    }

    [Header("Detecção")]
    public float detectionRange = 12f;
    public float loseAggroRange = 18f;
    public float attackRange = 2f;
    public float fieldOfView = 120f;

    [Header("Patrulha")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;

    [Header("Ataque")]
    public float attackCooldown = 2f;
    public float attackDamage = 15f;
    public float staggerDuration = 0.5f;

    [Header("Velocidades")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4.5f;

    // Componentes
    private NavMeshAgent agent;
    private EnemyStats stats;
    private Transform playerTransform;

    // Estado
    private EnemyState currentState = EnemyState.Idle;
    private int currentPatrolIndex;
    private float waitTimer;
    private float attackTimer;
    private float staggerTimer;

    public EnemyState CurrentState => currentState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();
    }

    private void Start()
    {
        // Encontrar o player
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            playerTransform = player.transform;

        agent.speed = patrolSpeed;
        currentState = patrolPoints != null && patrolPoints.Length > 0
            ? EnemyState.Patrol
            : EnemyState.Idle;

        // Escutar evento de morte
        stats.OnEnemyDeath += () => { currentState = EnemyState.Dead; agent.isStopped = true; };
    }

    private void Update()
    {
        if (currentState == EnemyState.Dead) return;

        attackTimer -= Time.deltaTime;

        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Stagger:
                UpdateStagger();
                break;
        }
    }

    #region State Updates

    private void UpdateIdle()
    {
        if (CanSeePlayer())
        {
            TransitionTo(EnemyState.Chase);
        }
    }

    private void UpdatePatrol()
    {
        if (CanSeePlayer())
        {
            TransitionTo(EnemyState.Chase);
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= patrolWaitTime)
            {
                waitTimer = 0f;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
        }
    }

    private void UpdateChase()
    {
        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Perder aggro
        if (distToPlayer > loseAggroRange)
        {
            TransitionTo(patrolPoints != null && patrolPoints.Length > 0
                ? EnemyState.Patrol
                : EnemyState.Idle);
            return;
        }

        // Entrar no alcance de ataque
        if (distToPlayer <= attackRange)
        {
            TransitionTo(EnemyState.Attack);
            return;
        }

        agent.SetDestination(playerTransform.position);
    }

    private void UpdateAttack()
    {
        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Olhar para o player
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction), 8f * Time.deltaTime);

        // Se player saiu do alcance, voltar a perseguir
        if (distToPlayer > attackRange * 1.5f)
        {
            TransitionTo(EnemyState.Chase);
            return;
        }

        // Atacar
        if (attackTimer <= 0f)
        {
            PerformAttack();
            attackTimer = attackCooldown;
        }
    }

    private void UpdateStagger()
    {
        staggerTimer -= Time.deltaTime;
        if (staggerTimer <= 0f)
        {
            TransitionTo(EnemyState.Chase);
        }
    }

    #endregion

    #region Transitions

    private void TransitionTo(EnemyState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case EnemyState.Idle:
                agent.isStopped = true;
                break;
            case EnemyState.Patrol:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                if (patrolPoints != null && patrolPoints.Length > 0)
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                break;
            case EnemyState.Chase:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                break;
            case EnemyState.Attack:
                agent.isStopped = true;
                break;
            case EnemyState.Stagger:
                agent.isStopped = true;
                staggerTimer = staggerDuration;
                break;
        }
    }

    #endregion

    #region Actions

    private void PerformAttack()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= attackRange)
        {
            IDamageable target = playerTransform.GetComponent<IDamageable>();
            if (target != null)
            {
                // Verificar se o player está bloqueando
                PlayerCombat combat = playerTransform.GetComponent<PlayerCombat>();
                if (combat != null && combat.IsBlocking)
                {
                    float reducedDamage = combat.ProcessBlockedDamage(attackDamage);
                    if (reducedDamage > 0f)
                        target.TakeDamage(reducedDamage);
                }
                else
                {
                    DamageSystem.ApplyDamage(target, attackDamage);
                }
            }
        }
    }

    public void TriggerStagger()
    {
        TransitionTo(EnemyState.Stagger);
    }

    #endregion

    #region Detection

    private bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist > detectionRange) return false;

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (angle < fieldOfView * 0.5f)
        {
            // Raycast para checar se não há obstáculo
            if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, out RaycastHit hit, detectionRange))
            {
                if (hit.transform == playerTransform || hit.transform.root == playerTransform)
                    return true;
            }
        }

        return false;
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
