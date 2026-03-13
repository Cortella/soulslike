using UnityEngine;

/// <summary>
/// Gerenciador de estado do jogo: controles de pausa, game over, etc.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        Paused,
        Dead,
        Loading,
        Resting // na bonfire
    }

    [Header("Estado")]
    public GameState currentState = GameState.Playing;

    [Header("Configurações")]
    public float respawnDelay = 3f;

    // Eventos
    public System.Action<GameState> OnGameStateChanged;

    private Vector3 lastBonfirePosition;
    private PlayerStats playerStats;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath += HandlePlayerDeath;
        }

        // Salvar posição inicial como bonfire
        DungeonGenerator dungeon = FindFirstObjectByType<DungeonGenerator>();
        if (dungeon != null)
        {
            lastBonfirePosition = dungeon.BonfirePosition;
        }
    }

    private void Update()
    {
        // Pause com ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Playing)
                PauseGame();
            else if (currentState == GameState.Paused)
                ResumeGame();
        }
    }

    public void SetBonfireCheckpoint(Vector3 position)
    {
        lastBonfirePosition = position;
        Debug.Log("[GameManager] Checkpoint atualizado: " + position);
    }

    public void PauseGame()
    {
        currentState = GameState.Paused;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OnGameStateChanged?.Invoke(currentState);
    }

    public void ResumeGame()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        OnGameStateChanged?.Invoke(currentState);
    }

    private void HandlePlayerDeath()
    {
        currentState = GameState.Dead;
        OnGameStateChanged?.Invoke(currentState);

        // Respawn após delay
        Invoke(nameof(RespawnPlayer), respawnDelay);
    }

    private void RespawnPlayer()
    {
        // Reposicionar player na última bonfire
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            Rigidbody rb = pc.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            pc.transform.position = lastBonfirePosition;
            if (rb != null) rb.isKinematic = false;
        }

        // Restaurar vida
        if (playerStats != null)
        {
            playerStats.Heal(playerStats.maxHealth);
            // Reset death flag via reflection ou new start
            // (na prática, precisaria de um método Reset no PlayerStats)
        }

        currentState = GameState.Playing;
        OnGameStateChanged?.Invoke(currentState);
        Debug.Log("[GameManager] Player respawnado na bonfire.");
    }
}
