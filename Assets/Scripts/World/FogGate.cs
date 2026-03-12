using UnityEngine;

/// <summary>
/// Fog Gate — portão de névoa que bloqueia passagem até ser atravessado.
/// Estilo Dark Souls: ao passar, fecha atrás do jogador.
/// </summary>
public class FogGate : MonoBehaviour
{
    [Header("Configurações")]
    public bool isOneWay = true;     // fecha após entrar
    public bool isBossGate = true;

    private bool isOpen = true;
    private bool playerPassed;
    private Renderer gateRenderer;

    private void Start()
    {
        gateRenderer = GetComponent<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isOpen) return;

        if (other.GetComponent<PlayerController>() != null)
        {
            playerPassed = true;

            if (isOneWay)
            {
                // Fechar atrás do player após um delay
                Invoke(nameof(CloseGate), 0.5f);
            }

            Debug.Log("[FogGate] Player passou pelo fog gate.");
        }
    }

    private void CloseGate()
    {
        isOpen = false;

        // Visual: tornar mais opaco
        if (gateRenderer != null)
        {
            Color c = gateRenderer.material.color;
            c.a = 0.9f;
            gateRenderer.material.color = c;
        }

        // Ativar boss, se houver
        if (isBossGate)
        {
            // TODO: Ativar boss na arena
            Debug.Log("[FogGate] Boss ativado!");
        }
    }

    public void OpenGate()
    {
        isOpen = true;
        if (gateRenderer != null)
        {
            Color c = gateRenderer.material.color;
            c.a = 0.4f;
            gateRenderer.material.color = c;
        }
    }
}
