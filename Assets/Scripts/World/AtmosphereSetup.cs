using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Configura pós-processamento atmosférico estilo soulslike (sombrio, neblina, bloom).
/// Cria Volume Profile via código com todos os overrides de URP.
/// </summary>
public class AtmosphereSetup : MonoBehaviour
{
    [Header("Atmosphere Presets")]
    public AtmospherePreset preset = AtmospherePreset.DarkForest;

    public enum AtmospherePreset
    {
        DarkForest,
        BossArena,
        SafeZone,
        Nighttime,
        Liurnia
    }

    /// <summary>
    /// Cria um Volume de pós-processamento global com configurações atmosféricas.
    /// </summary>
    public static GameObject CreateAtmosphereVolume(AtmospherePreset preset = AtmospherePreset.DarkForest)
    {
        GameObject volumeObj = new GameObject("PostProcessVolume_Atmosphere");
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        // ====== BLOOM ======
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.value = 0.9f;
        bloom.intensity.value = 0.8f;
        bloom.scatter.value = 0.65f;
        bloom.tint.value = new Color(1f, 0.95f, 0.85f);
        bloom.threshold.overrideState = true;
        bloom.intensity.overrideState = true;
        bloom.scatter.overrideState = true;
        bloom.tint.overrideState = true;

        // ====== VIGNETTE ======
        Vignette vignette = profile.Add<Vignette>(true);
        vignette.color.value = Color.black;
        vignette.center.value = new Vector2(0.5f, 0.5f);
        vignette.color.overrideState = true;
        vignette.center.overrideState = true;
        vignette.intensity.overrideState = true;
        vignette.smoothness.overrideState = true;

        // ====== COLOR ADJUSTMENTS ======
        ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.postExposure.overrideState = true;
        colorAdj.contrast.overrideState = true;
        colorAdj.saturation.overrideState = true;
        colorAdj.colorFilter.overrideState = true;

        // ====== LIFT GAMMA GAIN ======
        LiftGammaGain lgg = profile.Add<LiftGammaGain>(true);
        lgg.lift.overrideState = true;
        lgg.gamma.overrideState = true;
        lgg.gain.overrideState = true;

        // ====== TONEMAPPING ======
        Tonemapping tonemap = profile.Add<Tonemapping>(true);
        tonemap.mode.value = TonemappingMode.ACES;
        tonemap.mode.overrideState = true;

        // ====== FILM GRAIN ======
        FilmGrain filmGrain = profile.Add<FilmGrain>(true);
        filmGrain.type.overrideState = true;
        filmGrain.intensity.overrideState = true;

        // ====== CHROMATIC ABERRATION ======
        ChromaticAberration chrAb = profile.Add<ChromaticAberration>(true);
        chrAb.intensity.overrideState = true;

        // ====== APPLY PRESET ======
        ApplyPreset(preset, bloom, vignette, colorAdj, lgg, filmGrain, chrAb);

        return volumeObj;
    }

    static void ApplyPreset(AtmospherePreset preset,
        Bloom bloom, Vignette vignette, ColorAdjustments colorAdj,
        LiftGammaGain lgg, FilmGrain filmGrain, ChromaticAberration chrAb)
    {
        switch (preset)
        {
            case AtmospherePreset.DarkForest:
                // Atmosfera florestal - visível mas atmosférica
                vignette.intensity.value = 0.2f;
                vignette.smoothness.value = 0.4f;
                colorAdj.postExposure.value = 0.5f;
                colorAdj.contrast.value = 15f;
                colorAdj.saturation.value = -5f;
                colorAdj.colorFilter.value = new Color(0.9f, 0.95f, 0.85f);
                lgg.lift.value = new Vector4(0f, 0.01f, 0.005f, 0f);
                lgg.gamma.value = new Vector4(0f, 0f, 0f, 0f);
                lgg.gain.value = new Vector4(0.01f, 0f, -0.01f, 0f);
                bloom.intensity.value = 0.8f;
                filmGrain.type.value = FilmGrainLookup.Thin1;
                filmGrain.intensity.value = 0.1f;
                chrAb.intensity.value = 0.03f;
                break;

            case AtmospherePreset.BossArena:
                // Atmosfera tensa de arena de boss
                vignette.intensity.value = 0.55f;
                vignette.smoothness.value = 0.6f;
                colorAdj.postExposure.value = -0.5f;
                colorAdj.contrast.value = 30f;
                colorAdj.saturation.value = -25f;
                colorAdj.colorFilter.value = new Color(0.9f, 0.8f, 0.7f); // Tom alaranjado
                lgg.lift.value = new Vector4(0.02f, 0f, 0f, -0.08f); // Sombras vermelhas
                lgg.gamma.value = new Vector4(0.01f, 0f, -0.01f, -0.05f);
                lgg.gain.value = new Vector4(0.03f, 0.01f, -0.02f, 0f);
                bloom.intensity.value = 1.2f;
                bloom.tint.value = new Color(1f, 0.7f, 0.4f); // Bloom alaranjado
                filmGrain.type.value = FilmGrainLookup.Medium3;
                filmGrain.intensity.value = 0.35f;
                chrAb.intensity.value = 0.1f;
                break;

            case AtmospherePreset.SafeZone:
                // Atmosfera mais leve na fogueira
                vignette.intensity.value = 0.25f;
                vignette.smoothness.value = 0.4f;
                colorAdj.postExposure.value = 0.2f;
                colorAdj.contrast.value = 10f;
                colorAdj.saturation.value = -5f;
                colorAdj.colorFilter.value = new Color(1f, 0.95f, 0.88f); // Quente
                lgg.lift.value = new Vector4(0f, 0f, 0f, 0f);
                lgg.gamma.value = new Vector4(0.02f, 0.01f, 0f, 0f);
                lgg.gain.value = new Vector4(0.01f, 0f, 0f, 0f);
                bloom.intensity.value = 1f;
                bloom.tint.value = new Color(1f, 0.9f, 0.7f); // Bloom quente
                filmGrain.type.value = FilmGrainLookup.Thin2;
                filmGrain.intensity.value = 0.1f;
                chrAb.intensity.value = 0f;
                break;

            case AtmospherePreset.Nighttime:
                // Noite escura
                vignette.intensity.value = 0.5f;
                vignette.smoothness.value = 0.55f;
                colorAdj.postExposure.value = -1f;
                colorAdj.contrast.value = 25f;
                colorAdj.saturation.value = -40f;
                colorAdj.colorFilter.value = new Color(0.7f, 0.75f, 0.9f); // Tom azulado
                lgg.lift.value = new Vector4(-0.02f, 0f, 0.05f, -0.1f); // Sombras azuis
                lgg.gamma.value = new Vector4(0f, 0f, 0.02f, -0.08f);
                lgg.gain.value = new Vector4(0f, 0f, 0.02f, -0.05f);
                bloom.intensity.value = 0.4f;
                bloom.tint.value = new Color(0.7f, 0.8f, 1f); // Bloom azulado
                filmGrain.type.value = FilmGrainLookup.Medium6;
                filmGrain.intensity.value = 0.4f;
                chrAb.intensity.value = 0.08f;
                break;

            case AtmospherePreset.Liurnia:
                // Lago místico — azul prateado, etéreo
                vignette.intensity.value = 0.15f;
                vignette.smoothness.value = 0.35f;
                colorAdj.postExposure.value = 0.3f;
                colorAdj.contrast.value = 12f;
                colorAdj.saturation.value = -15f;
                colorAdj.colorFilter.value = new Color(0.75f, 0.8f, 0.95f); // Azul prateado
                lgg.lift.value = new Vector4(-0.01f, 0f, 0.04f, 0f); // Sombras azuis
                lgg.gamma.value = new Vector4(0f, 0f, 0.015f, 0f);
                lgg.gain.value = new Vector4(-0.01f, 0f, 0.02f, 0f);
                bloom.intensity.value = 1.5f; // Bloom forte para brilho etéreo
                bloom.tint.value = new Color(0.7f, 0.8f, 1f);
                bloom.threshold.value = 0.7f; // Limiar baixo para mais glow
                filmGrain.type.value = FilmGrainLookup.Thin1;
                filmGrain.intensity.value = 0.08f;
                chrAb.intensity.value = 0.02f;
                break;
        }
    }

    /// <summary>
    /// Configura a luz direcional principal com estilo floresta.
    /// </summary>
    public static Light SetupDirectionalLight()
    {
        // Procura luz existente
        Light mainLight = null;
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                mainLight = l;
                break;
            }
        }

        if (mainLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light - Forest");
            mainLight = lightObj.AddComponent<Light>();
            mainLight.type = LightType.Directional;
        }

        mainLight.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        mainLight.color = new Color(1f, 0.95f, 0.85f);
        mainLight.intensity = 2.8f;
        mainLight.shadows = LightShadows.Soft;
        mainLight.shadowStrength = 0.7f;
        mainLight.shadowBias = 0.02f;
        mainLight.shadowNormalBias = 0.3f;
        mainLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;

        // URP Additional Light Data
        var urpLight = mainLight.GetComponent<UniversalAdditionalLightData>();
        if (urpLight == null)
            urpLight = mainLight.gameObject.AddComponent<UniversalAdditionalLightData>();

        // Adicionar luz de preenchimento (fill light) para reduzir áreas totalmente escuras
        GameObject fillLightObj = new GameObject("FillLight_Sky");
        Light fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.transform.rotation = Quaternion.Euler(70f, 150f, 0f);
        fillLight.color = new Color(0.55f, 0.65f, 0.75f);
        fillLight.intensity = 0.5f;
        fillLight.shadows = LightShadows.None;

        return mainLight;
    }

    /// <summary>
    /// Cria sistema de neblina volumétrica simples usando partículas.
    /// </summary>
    public static GameObject CreateFogParticles(Vector3 center, float radius = 100f)
    {
        GameObject fogObj = new GameObject("FogParticles");
        fogObj.transform.position = center + Vector3.up * 1f;

        var ps = fogObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 12f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(8f, 18f);
        main.startColor = new Color(0.5f, 0.55f, 0.5f, 0.05f);
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.01f;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(radius * 2, 2f, radius * 2);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.06f, 0.3f),
                new GradientAlphaKey(0.06f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);

        var renderer = fogObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateFogMaterial();

        return fogObj;
    }

    /// <summary>
    /// Cria partículas de vagalumes para atmosfera noturna.
    /// </summary>
    public static GameObject CreateFireflies(Vector3 center, float radius = 80f)
    {
        GameObject ffObj = new GameObject("Fireflies");
        ffObj.transform.position = center + Vector3.up * 2f;

        var ps = ffObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f);
        main.startSpeed = 0.15f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = new Color(0.8f, 1f, 0.4f, 0.8f); // Verde-amarelado
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(radius * 2, 4f, radius * 2);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.1f;
        noise.damping = true;

        // Piscar dos vagalumes
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.6f, 1f, 0.3f), 0f),
                new GradientColorKey(new Color(0.9f, 1f, 0.5f), 0.5f),
                new GradientColorKey(new Color(0.6f, 1f, 0.3f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.8f, 0.15f),
                new GradientAlphaKey(0.3f, 0.4f),
                new GradientAlphaKey(0.9f, 0.6f),
                new GradientAlphaKey(0.2f, 0.85f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = ffObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // Material emissivo para vagalumes
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit == null) urpUnlit = Shader.Find("Unlit/Color");
        Material ffMat = new Material(urpUnlit);
        ffMat.name = "M_Firefly";
        ffMat.color = new Color(0.8f, 1f, 0.4f);
        // Tentar habilitar emissão
        ffMat.EnableKeyword("_EMISSION");
        renderer.material = ffMat;

        // Ponto de luz suave que acompanha o vagalume (light module)
        var lights = ps.lights;
        lights.enabled = true;
        lights.ratio = 0.15f;
        lights.maxLights = 15;
        lights.intensityMultiplier = 0.3f;
        lights.rangeMultiplier = 1.5f;

        // Criar prefab de luz
        GameObject lightPrefab = new GameObject("FireflyLight");
        Light fl = lightPrefab.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(0.7f, 1f, 0.3f);
        fl.intensity = 0.2f;
        fl.range = 1.5f;
        fl.shadows = LightShadows.None;
        lights.light = fl;
        lightPrefab.SetActive(false);
        lightPrefab.transform.SetParent(ffObj.transform);

        return ffObj;
    }

    /// <summary>
    /// Cria partículas de folhas caindo.
    /// </summary>
    public static GameObject CreateFallingLeaves(Vector3 center, float radius = 100f)
    {
        GameObject leavesObj = new GameObject("FallingLeaves");
        leavesObj.transform.position = center + Vector3.up * 20f;

        var ps = leavesObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 15f);
        main.startSpeed = 0.2f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.05f;

        // Cor das folhas (varia do verde ao marrom)
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.2f, 0.35f, 0.05f), // Verde escuro
            new Color(0.4f, 0.25f, 0.05f)  // Marrom
        );

        var emission = ps.emission;
        emission.rateOverTime = 5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(radius * 2, 1f, radius * 2);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 0.3f;
        noise.damping = true;

        var rotOverLifetime = ps.rotationOverLifetime;
        rotOverLifetime.enabled = true;
        rotOverLifetime.x = new ParticleSystem.MinMaxCurve(-45f, 45f);
        rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-90f, 90f);

        var renderer = leavesObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit == null) urpUnlit = Shader.Find("Unlit/Color");
        Material leafMat = new Material(urpUnlit);
        leafMat.name = "M_Leaf";
        leafMat.color = new Color(0.3f, 0.3f, 0.05f, 0.8f);
        renderer.material = leafMat;

        return leavesObj;
    }

    static Material CreateFogMaterial()
    {
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlit == null) urpUnlit = Shader.Find("Particles/Standard Unlit");
        Material fogMat = new Material(urpUnlit);
        fogMat.name = "M_Fog";
        fogMat.color = new Color(0.5f, 0.55f, 0.5f, 0.03f);
        return fogMat;
    }
}
