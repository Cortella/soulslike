using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bridge entre o Input System e os scripts do Player.
/// Encaminha os callbacks do PlayerInput para os componentes corretos.
/// Adicione este script ao GameObject do Player junto com PlayerInput.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class InputHandler : MonoBehaviour
{
    private PlayerController playerController;
    private PlayerCombat playerCombat;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        playerController?.SetMoveInput(context.ReadValue<Vector2>());
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        playerController?.SetRunning(context.performed);
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed) playerController?.TryDodge();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) playerController?.TryJump();
    }

    public void OnLockOn(InputAction.CallbackContext context)
    {
        if (context.performed) playerController?.TryToggleLockOn();
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        playerCombat?.OnLightAttack(context);
    }

    public void OnHeavyAttack(InputAction.CallbackContext context)
    {
        playerCombat?.OnHeavyAttack(context);
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        playerCombat?.OnBlock(context);
    }
}
