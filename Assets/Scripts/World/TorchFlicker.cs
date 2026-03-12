using UnityEngine;

/// <summary>
/// Componente que faz a "chama" da tocha oscilar suavemente,
/// simulando fogo vivo.
/// </summary>
public class TorchFlicker : MonoBehaviour
{
    public Light torchLight;
    public float minIntensity = 1f;
    public float maxIntensity = 2f;
    public float flickerSpeed = 3f;

    private float baseIntensity;
    private float randomOffset;

    private void Start()
    {
        if (torchLight == null)
            torchLight = GetComponent<Light>();

        if (torchLight != null)
            baseIntensity = torchLight.intensity;

        randomOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (torchLight == null) return;

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + randomOffset, 0f);
        torchLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
