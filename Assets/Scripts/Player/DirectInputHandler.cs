using UnityEngine;

/// <summary>
/// Input direto via teclado e mouse — funciona IMEDIATAMENTE sem 
/// configuração de PlayerInput/InputActions.
/// Lê teclas diretamente (WASD, Shift, Espaço, Mouse) e repassa 
/// para PlayerController e PlayerCombat.
/// 
/// SUBSTITUI o InputHandler antigo que dependia de PlayerInput asset.
/// </summary>
public class DirectInputHandler : MonoBehaviour
{
    private PlayerController playerController;
    private PlayerCombat playerCombat;

    // Input state
    private Vector2 moveInput;
    private bool isRunning;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
    }

    private void Update()
    {
        if (playerController == null) return;

        // === MOVIMENTO (WASD / Arrow Keys) ===
        float h = 0f, v = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;

        moveInput = new Vector2(h, v).normalized;
        playerController.SetMoveInput(moveInput);

        // === CORRER (Left Shift) ===
        playerController.SetRunning(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        // === DODGE / ROLL (Espaço) ===
        if (Input.GetKeyDown(KeyCode.Space))
            playerController.TryDodge();

        // === PULO (F) ===
        if (Input.GetKeyDown(KeyCode.F))
            playerController.TryJump();

        // === LOCK-ON (Tab ou Q) ===
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Q))
            playerController.TryToggleLockOn();

        // === COMBATE ===
        if (playerCombat != null)
        {
            // Ataque leve (Click esquerdo)
            if (Input.GetMouseButtonDown(0))
                playerCombat.TryLightAttack();

            // Ataque pesado (Click do meio ou Ctrl+Click)
            if (Input.GetMouseButtonDown(2) ||
                (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0)))
                playerCombat.TryHeavyAttack();

            // Bloquear (Click direito - segurar)
            if (Input.GetMouseButton(1))
                playerCombat.SetBlocking(true);
            else
                playerCombat.SetBlocking(false);
        }

        // === INTERAGIR (E) ===
        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    private void TryInteract()
    {
        // Procurar bonfire perto
        Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
        foreach (var hit in hits)
        {
            Bonfire bonfire = hit.GetComponent<Bonfire>();
            if (bonfire != null)
            {
                bonfire.Interact(gameObject);
                return;
            }
        }
    }
}
