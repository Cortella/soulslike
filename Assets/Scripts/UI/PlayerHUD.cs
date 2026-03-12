using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD do jogador: barras de vida, stamina e contador de souls.
/// Cria toda a UI via código caso não haja referências.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Referências UI")]
    public Image healthBarFill;
    public Image staminaBarFill;
    public TextMeshProUGUI soulsText;
    public GameObject deathScreen;

    [Header("Cores")]
    public Color healthColor = new Color(0.7f, 0.1f, 0.1f, 1f);
    public Color staminaColor = new Color(0.2f, 0.6f, 0.2f, 1f);
    public Color barBackground = new Color(0.15f, 0.15f, 0.15f, 0.8f);

    private PlayerStats playerStats;
    private Canvas canvas;

    private void Awake()
    {
        // Apenas criar a UI se não estiverem referenciadas
        if (healthBarFill == null || staminaBarFill == null || soulsText == null)
        {
            CreateUI();
        }
    }

    private void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealthBar;
            playerStats.OnStaminaChanged += UpdateStaminaBar;
            playerStats.OnSoulsChanged += UpdateSoulsText;
            playerStats.OnPlayerDeath += ShowDeathScreen;

            // Inicializar
            UpdateHealthBar(playerStats.currentHealth, playerStats.maxHealth);
            UpdateStaminaBar(playerStats.currentStamina, playerStats.maxStamina);
            UpdateSoulsText(playerStats.souls);
        }

        if (deathScreen != null)
            deathScreen.SetActive(false);
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = current / max;
    }

    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaBarFill != null)
            staminaBarFill.fillAmount = current / max;
    }

    private void UpdateSoulsText(int amount)
    {
        if (soulsText != null)
            soulsText.text = amount.ToString("N0");
    }

    private void ShowDeathScreen()
    {
        if (deathScreen != null)
            deathScreen.SetActive(true);
    }

    #region Criação de UI via código

    private void CreateUI()
    {
        // Canvas
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }

        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // === BARRA DE VIDA ===
        healthBarFill = CreateBar("HealthBar", new Vector2(300, 25),
            new Vector2(170, -40), healthColor);

        // === BARRA DE STAMINA ===
        staminaBarFill = CreateBar("StaminaBar", new Vector2(250, 18),
            new Vector2(145, -70), staminaColor);

        // === SOULS TEXT ===
        GameObject soulsObj = new GameObject("SoulsText");
        soulsObj.transform.SetParent(transform, false);
        soulsText = soulsObj.AddComponent<TextMeshProUGUI>();
        soulsText.text = "0";
        soulsText.fontSize = 24;
        soulsText.color = Color.white;
        soulsText.alignment = TextAlignmentOptions.BottomRight;
        RectTransform soulsRect = soulsObj.GetComponent<RectTransform>();
        soulsRect.anchorMin = new Vector2(1, 0);
        soulsRect.anchorMax = new Vector2(1, 0);
        soulsRect.pivot = new Vector2(1, 0);
        soulsRect.anchoredPosition = new Vector2(-30, 30);
        soulsRect.sizeDelta = new Vector2(200, 40);

        // Ícone de souls (texto)
        GameObject soulsLabel = new GameObject("SoulsLabel");
        soulsLabel.transform.SetParent(transform, false);
        TextMeshProUGUI label = soulsLabel.AddComponent<TextMeshProUGUI>();
        label.text = "SOULS";
        label.fontSize = 14;
        label.color = new Color(0.8f, 0.7f, 0.3f, 1f);
        label.alignment = TextAlignmentOptions.BottomRight;
        RectTransform labelRect = soulsLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(1, 0);
        labelRect.anchorMax = new Vector2(1, 0);
        labelRect.pivot = new Vector2(1, 0);
        labelRect.anchoredPosition = new Vector2(-30, 65);
        labelRect.sizeDelta = new Vector2(200, 25);

        // === DEATH SCREEN ===
        deathScreen = new GameObject("DeathScreen");
        deathScreen.transform.SetParent(transform, false);
        Image deathBg = deathScreen.AddComponent<Image>();
        deathBg.color = new Color(0, 0, 0, 0.85f);
        RectTransform deathRect = deathScreen.GetComponent<RectTransform>();
        deathRect.anchorMin = Vector2.zero;
        deathRect.anchorMax = Vector2.one;
        deathRect.sizeDelta = Vector2.zero;

        GameObject deathTextObj = new GameObject("DeathText");
        deathTextObj.transform.SetParent(deathScreen.transform, false);
        TextMeshProUGUI deathText = deathTextObj.AddComponent<TextMeshProUGUI>();
        deathText.text = "YOU DIED";
        deathText.fontSize = 72;
        deathText.color = new Color(0.7f, 0.1f, 0.1f, 1f);
        deathText.alignment = TextAlignmentOptions.Center;
        RectTransform dtRect = deathTextObj.GetComponent<RectTransform>();
        dtRect.anchorMin = new Vector2(0.5f, 0.5f);
        dtRect.anchorMax = new Vector2(0.5f, 0.5f);
        dtRect.sizeDelta = new Vector2(600, 100);
    }

    private Image CreateBar(string barName, Vector2 size, Vector2 position, Color fillColor)
    {
        // Background
        GameObject bgObj = new GameObject(barName + "_BG");
        bgObj.transform.SetParent(transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = barBackground;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(0, 1);
        bgRect.pivot = new Vector2(0, 1);
        bgRect.sizeDelta = size;
        bgRect.anchoredPosition = position;

        // Fill
        GameObject fillObj = new GameObject(barName + "_Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        return fillImage;
    }

    #endregion

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthBar;
            playerStats.OnStaminaChanged -= UpdateStaminaBar;
            playerStats.OnSoulsChanged -= UpdateSoulsText;
            playerStats.OnPlayerDeath -= ShowDeathScreen;
        }
    }
}
