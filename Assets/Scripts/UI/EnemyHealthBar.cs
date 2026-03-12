using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Barra de HP flutuante sobre inimigos (mundo 3D).
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    public EnemyStats enemyStats;
    public Vector3 offset = new Vector3(0, 2.5f, 0);

    private Camera mainCamera;
    private Image fillImage;
    private Canvas worldCanvas;
    private GameObject barObject;

    private void Start()
    {
        mainCamera = Camera.main;

        if (enemyStats == null)
            enemyStats = GetComponentInParent<EnemyStats>();

        CreateWorldSpaceBar();

        if (enemyStats != null)
        {
            enemyStats.OnHealthChanged += UpdateBar;
            enemyStats.OnEnemyDeath += HideBar;
        }
    }

    private void CreateWorldSpaceBar()
    {
        barObject = new GameObject("EnemyHPBar_Canvas");
        barObject.transform.SetParent(transform);
        barObject.transform.localPosition = offset;

        worldCanvas = barObject.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1.2f, 0.15f);

        // Background
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(barObject.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (barObject != null && mainCamera != null)
        {
            barObject.transform.LookAt(mainCamera.transform);
        }
    }

    private void UpdateBar(float current, float max)
    {
        if (fillImage != null)
            fillImage.fillAmount = current / max;
    }

    private void HideBar()
    {
        if (barObject != null)
            barObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (enemyStats != null)
        {
            enemyStats.OnHealthChanged -= UpdateBar;
            enemyStats.OnEnemyDeath -= HideBar;
        }
    }
}
