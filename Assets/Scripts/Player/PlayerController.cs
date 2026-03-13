using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador do jogador estilo Soulslike — terceira pessoa com 
/// movimento, dodge roll e gravidade.
/// Usa o novo Input System.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;
    public float rotationSpeed = 12f;
    public float gravity = -20f;
    public float jumpHeight = 1.2f;

    [Header("Dodge / Roll")]
    public float dodgeSpeed = 8f;
    public float dodgeDuration = 0.5f;
    public float dodgeStaminaCost = 20f;
    public float dodgeInvulnerabilityTime = 0.3f;

    [Header("Referências")]
    public Transform cameraTransform;

    // Componentes
    private CharacterController characterController;
    private PlayerStats playerStats;
    private PlayerCombat playerCombat;

    // Estado interno
    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isRunning;
    private bool isDodging;
    private float dodgeTimer;
    private Vector3 dodgeDirection;
    private bool isGrounded;

    // Lock-on
    public Transform lockOnTarget;
    public bool IsLockedOn => lockOnTarget != null;

    // Propriedades públicas
    public bool IsDodging => isDodging;
    public Vector3 MoveDirection { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerStats = GetComponent<PlayerStats>();
        playerCombat = GetComponent<PlayerCombat>();
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (playerStats != null && playerStats.IsDead) return;

        isGrounded = characterController.isGrounded;

        if (isDodging)
        {
            HandleDodge();
        }
        else
        {
            HandleMovement();
        }

        ApplyGravity();
        characterController.Move(velocity * Time.deltaTime);
    }

    #region Input Callbacks (conectar via PlayerInput component ou Input Actions)

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        isRunning = context.performed;
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.started && !isDodging)
        {
            StartDodge();
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void OnLockOn(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            ToggleLockOn();
        }
    }

    #endregion

    #region Direct Input Methods (chamados por DirectInputHandler)

    public void SetMoveInput(Vector2 input) => moveInput = input;
    public void SetRunning(bool running) => isRunning = running;

    public void TryDodge()
    {
        if (!isDodging) StartDodge();
    }

    public void TryJump()
    {
        if (isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    public void TryToggleLockOn() => ToggleLockOn();

    #endregion

    #region Movement

    private void HandleMovement()
    {
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            // Calcular direção relativa à câmera
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
            if (cameraTransform != null)
                targetAngle += cameraTransform.eulerAngles.y;

            MoveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // Rotação suave
            if (!IsLockedOn)
            {
                Quaternion targetRotation = Quaternion.LookRotation(MoveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    rotationSpeed * Time.deltaTime);
            }
            else if (lockOnTarget != null)
            {
                Vector3 dirToTarget = lockOnTarget.position - transform.position;
                dirToTarget.y = 0;
                if (dirToTarget.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(dirToTarget), rotationSpeed * Time.deltaTime);
                }
            }

            // Velocidade
            float speed = isRunning ? runSpeed : walkSpeed;

            // Consumo de stamina ao correr
            if (isRunning && playerStats != null)
            {
                if (!playerStats.HasStamina(5f * Time.deltaTime))
                    speed = walkSpeed;
                else
                    playerStats.ConsumeStamina(5f * Time.deltaTime);
            }

            velocity.x = MoveDirection.x * speed;
            velocity.z = MoveDirection.z * speed;
        }
        else
        {
            velocity.x = 0f;
            velocity.z = 0f;
            MoveDirection = Vector3.zero;
        }
    }

    #endregion

    #region Dodge

    private void StartDodge()
    {
        if (playerStats != null && !playerStats.HasStamina(dodgeStaminaCost)) return;

        isDodging = true;
        dodgeTimer = dodgeDuration;

        // Direção do dodge: se movendo, usa direção do movimento; senão, para trás
        if (moveInput.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg;
            if (cameraTransform != null) targetAngle += cameraTransform.eulerAngles.y;
            dodgeDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
        }
        else
        {
            dodgeDirection = -transform.forward;
        }

        playerStats?.ConsumeStamina(dodgeStaminaCost);

        // Iframes
        if (playerStats != null)
            playerStats.IsInvulnerable = true;

        Invoke(nameof(EndInvulnerability), dodgeInvulnerabilityTime);
    }

    private void HandleDodge()
    {
        dodgeTimer -= Time.deltaTime;

        velocity.x = dodgeDirection.x * dodgeSpeed;
        velocity.z = dodgeDirection.z * dodgeSpeed;

        if (dodgeTimer <= 0f)
        {
            isDodging = false;
        }
    }

    private void EndInvulnerability()
    {
        if (playerStats != null)
            playerStats.IsInvulnerable = false;
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

        // Achar inimigo mais próximo
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

    #region Gravity

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    #endregion
}
