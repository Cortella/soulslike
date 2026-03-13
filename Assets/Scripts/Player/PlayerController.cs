using UnityEngine;

/// <summary>
/// Controlador do jogador com física Rigidbody realista.
/// Gravidade real do mundo, colisão física, momentum, slope handling.
/// Funciona com WASD via DirectInputHandler.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float walkSpeed = 4.5f;
    public float runSpeed = 7.5f;
    public float rotationSpeed = 14f;
    public float acceleration = 65f;
    public float deceleration = 50f;

    [Header("Pulo")]
    public float jumpForce = 7.5f;
    public float airControlFactor = 0.4f;
    public float fallGravityMultiplier = 2.5f;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.28f;
    public float groundCheckOffset = 0.1f;
    public LayerMask groundLayers = ~0;

    [Header("Slopes")]
    public float maxSlopeAngle = 50f;

    [Header("Dodge / Roll")]
    public float dodgeSpeed = 11f;
    public float dodgeDuration = 0.4f;
    public float dodgeStaminaCost = 20f;
    public float dodgeInvulnerabilityTime = 0.3f;

    [Header("Referências")]
    public Transform cameraTransform;

    // Components
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private PlayerStats playerStats;
    private PlayerCombat playerCombat;

    // Input state
    private Vector2 moveInput;
    private bool isRunning;

    // Physics state
    private bool isGrounded;
    private bool isDodging;
    private float dodgeTimer;
    private Vector3 dodgeDirection;
    private Vector3 groundNormal = Vector3.up;
    private float groundCheckDist;

    // Lock-on
    public Transform lockOnTarget;
    public bool IsLockedOn => lockOnTarget != null;

    // Public state
    public bool IsDodging => isDodging;
    public bool IsGrounded => isGrounded;
    public Vector3 MoveDirection { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        playerStats = GetComponent<PlayerStats>();
        playerCombat = GetComponent<PlayerCombat>();

        // Rigidbody para controle de personagem
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.mass = 70f; // 70kg — peso realista

        // Sem fricção nas paredes (evita grudar)
        PhysicsMaterial noFriction = new PhysicsMaterial("PlayerNoFriction");
        noFriction.dynamicFriction = 0f;
        noFriction.staticFriction = 0f;
        noFriction.bounciness = 0f;
        noFriction.frictionCombine = PhysicsMaterialCombine.Minimum;
        capsule.material = noFriction;

        groundCheckDist = capsule.height * 0.5f - capsule.radius + groundCheckOffset;
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        if (playerStats != null && playerStats.IsDead) return;

        PerformGroundCheck();

        if (isDodging)
            HandleDodge();
        else
            HandleMovement();

        ApplyExtraGravity();
    }

    #region Ground Check

    private void PerformGroundCheck()
    {
        Vector3 origin = transform.position + capsule.center;
        isGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
            out RaycastHit hit, groundCheckDist, groundLayers, QueryTriggerInteraction.Ignore);

        groundNormal = isGrounded ? hit.normal : Vector3.up;
    }

    #endregion

    #region Movement

    private void HandleMovement()
    {
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            // Direção relativa à câmera
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
            if (cameraTransform != null)
                targetAngle += cameraTransform.eulerAngles.y;

            MoveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // Rotação
            if (!IsLockedOn)
            {
                Quaternion targetRot = Quaternion.LookRotation(MoveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                    rotationSpeed * Time.fixedDeltaTime);
            }
            else if (lockOnTarget != null)
            {
                Vector3 toLock = lockOnTarget.position - transform.position;
                toLock.y = 0;
                if (toLock.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(toLock), rotationSpeed * Time.fixedDeltaTime);
            }

            // Velocidade
            float speed = isRunning ? runSpeed : walkSpeed;
            if (isRunning && playerStats != null)
            {
                if (!playerStats.HasStamina(5f * Time.fixedDeltaTime))
                    speed = walkSpeed;
                else
                    playerStats.ConsumeStamina(5f * Time.fixedDeltaTime);
            }

            // Controle reduzido no ar
            float controlFactor = isGrounded ? 1f : airControlFactor;

            // Projetar na inclinação quando no chão
            Vector3 targetVel = MoveDirection * speed;
            if (isGrounded)
            {
                float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
                if (slopeAngle < maxSlopeAngle && slopeAngle > 1f)
                    targetVel = Vector3.ProjectOnPlane(targetVel, groundNormal).normalized * speed;
            }

            // Aceleração suave (momentum realista)
            Vector3 currentH = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 targetH = new Vector3(targetVel.x, 0, targetVel.z);
            Vector3 newH = Vector3.MoveTowards(currentH, targetH,
                acceleration * controlFactor * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector3(newH.x, rb.linearVelocity.y, newH.z);
        }
        else
        {
            // Desaceleração para parar (fricção)
            Vector3 h = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 decel = Vector3.MoveTowards(h, Vector3.zero,
                deceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(decel.x, rb.linearVelocity.y, decel.z);
            MoveDirection = Vector3.zero;
        }
    }

    #endregion

    #region Dodge

    private void HandleDodge()
    {
        dodgeTimer -= Time.fixedDeltaTime;
        rb.linearVelocity = new Vector3(
            dodgeDirection.x * dodgeSpeed,
            rb.linearVelocity.y,
            dodgeDirection.z * dodgeSpeed);

        if (dodgeTimer <= 0f)
            isDodging = false;
    }

    private void StartDodge()
    {
        if (playerStats != null && !playerStats.HasStamina(dodgeStaminaCost)) return;

        isDodging = true;
        dodgeTimer = dodgeDuration;

        if (moveInput.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg;
            if (cameraTransform != null) angle += cameraTransform.eulerAngles.y;
            dodgeDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        }
        else
        {
            dodgeDirection = -transform.forward;
        }

        playerStats?.ConsumeStamina(dodgeStaminaCost);
        if (playerStats != null) playerStats.IsInvulnerable = true;
        Invoke(nameof(EndInvulnerability), dodgeInvulnerabilityTime);
    }

    private void EndInvulnerability()
    {
        if (playerStats != null) playerStats.IsInvulnerable = false;
    }

    #endregion

    #region Gravity

    private void ApplyExtraGravity()
    {
        // Gravidade extra na queda para sensação mais responsiva
        if (!isGrounded && rb.linearVelocity.y < 0)
        {
            rb.AddForce(Vector3.down * (fallGravityMultiplier - 1f) *
                Mathf.Abs(Physics.gravity.y), ForceMode.Acceleration);
        }

        // Evitar deslizar em slopes quando parado
        if (isGrounded && MoveDirection == Vector3.zero && !isDodging)
        {
            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
            if (slopeAngle > 2f && slopeAngle < maxSlopeAngle)
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    Mathf.Max(rb.linearVelocity.y, -0.5f),
                    rb.linearVelocity.z);
            }
        }
    }

    #endregion

    #region Lock-On

    private void ToggleLockOn()
    {
        if (lockOnTarget != null)
        {
            lockOnTarget = null;
            return;
        }

        float maxRange = 25f;
        Collider[] hits = Physics.OverlapSphere(transform.position, maxRange);
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = hit.transform;
                }
            }
        }

        lockOnTarget = closest;
    }

    #endregion

    #region Direct Input Methods (chamados por DirectInputHandler)

    public void SetMoveInput(Vector2 input) => moveInput = input;
    public void SetRunning(bool running) => isRunning = running;
    public void TryDodge() { if (!isDodging) StartDodge(); }

    public void TryJump()
    {
        if (isGrounded)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    public void TryToggleLockOn() => ToggleLockOn();

    #endregion
}
