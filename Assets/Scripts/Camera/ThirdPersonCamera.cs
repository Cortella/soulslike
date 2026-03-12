using UnityEngine;

/// <summary>
/// Câmera em terceira pessoa estilo Soulslike com suporte a Lock-On.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Alvo")]
    public Transform target;           // Player transform
    public PlayerController playerController;

    [Header("Offset e Distância")]
    public Vector3 offset = new Vector3(0f, 2f, 0f);
    public float defaultDistance = 4f;
    public float minDistance = 1.5f;
    public float maxDistance = 8f;

    [Header("Sensibilidade")]
    public float mouseSensitivityX = 3f;
    public float mouseSensitivityY = 2f;
    public float smoothSpeed = 10f;

    [Header("Limites Verticais")]
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 70f;

    [Header("Colisão")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.3f;

    [Header("Lock-On")]
    public float lockOnSmoothSpeed = 8f;

    // Estado interno
    private float currentX;
    private float currentY = 15f;
    private float currentDistance;

    private void Start()
    {
        currentDistance = defaultDistance;

        if (target == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                target = pc.transform;
                playerController = pc;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (playerController != null && playerController.IsLockedOn && playerController.lockOnTarget != null)
        {
            HandleLockOnCamera();
        }
        else
        {
            HandleFreeCamera();
        }
    }

    private void HandleFreeCamera()
    {
        // Input do mouse
        currentX += Input.GetAxis("Mouse X") * mouseSensitivityX;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivityY;
        currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);

        // Calcular posição desejada
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0f);
        Vector3 targetPosition = target.position + offset;
        Vector3 desiredPosition = targetPosition - rotation * Vector3.forward * currentDistance;

        // Checagem de colisão
        desiredPosition = CheckCollision(targetPosition, desiredPosition);

        // Aplicar
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(targetPosition);
    }

    private void HandleLockOnCamera()
    {
        Transform lockTarget = playerController.lockOnTarget;
        Vector3 targetPos = target.position + offset;

        // Posicionar câmera atrás do player olhando para o inimigo
        Vector3 midPoint = (target.position + lockTarget.position) * 0.5f + Vector3.up * 2f;
        Vector3 dirFromMid = (target.position - lockTarget.position).normalized;
        Vector3 desiredPos = target.position + dirFromMid * currentDistance + Vector3.up * 2.5f;

        desiredPos = CheckCollision(targetPos, desiredPos);

        transform.position = Vector3.Lerp(transform.position, desiredPos, lockOnSmoothSpeed * Time.deltaTime);
        transform.LookAt(midPoint);
    }

    private Vector3 CheckCollision(Vector3 from, Vector3 desired)
    {
        Vector3 direction = desired - from;
        float distance = direction.magnitude;

        if (Physics.SphereCast(from, collisionRadius, direction.normalized, out RaycastHit hit, distance, collisionLayers))
        {
            return hit.point + hit.normal * collisionRadius;
        }

        return desired;
    }

    /// <summary>
    /// Scroll do mouse para zoom.
    /// </summary>
    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentDistance -= scroll * 2f;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }
}
