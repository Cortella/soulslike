using UnityEngine;

/// <summary>
/// Bonfire (fogueira) — ponto de descanso.
/// Restaura vida, reseta stamina, funciona como checkpoint.
/// </summary>
public class Bonfire : MonoBehaviour
{
    [Header("Configurações")]
    public float interactionRange = 3f;
    public bool isLit = true;

    private bool playerInRange;
    private PlayerStats playerStats;

    private void Update()
    {
        if (!isLit) return;

        // Verificar se player está perto
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        playerInRange = dist <= interactionRange;

        // Input de interação (E)
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            RestAtBonfire(player);
        }
    }

    private void RestAtBonfire(PlayerController player)
    {
        playerStats = player.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.Heal(playerStats.maxHealth);
            Debug.Log("[Bonfire] Descansou na fogueira. HP restaurado.");
        }

        // TODO: Respawnar inimigos, salvar progresso, etc.
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, interactionRange);
    }
}
