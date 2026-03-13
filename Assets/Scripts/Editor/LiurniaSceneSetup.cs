using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Configura cena estilo Liurnia of the Lakes (Elden Ring).
/// Lago místico com ruínas, luz de lua, partículas etéreas.
/// Menu: Soulslike > Configurar Liurnia (Lago Místico)
/// </summary>
public class LiurniaSceneSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Soulslike/Configurar Liurnia (Lago Mistico)")]
    public static void SetupLiurniaScene()
    {
        if (!EditorUtility.DisplayDialog("Soulslike - Liurnia (Lago Místico)",
            "Criar mapa estilo Liurnia of the Lakes:\n\n" +
            "■ Terreno 512m com lago central\n" +
            "■ Água transparente com ondulação\n" +
            "■ 15 ruínas (colunas, arcos, pedestais)\n" +
            "■ 40 árvores (mortas/esparsas)\n" +
            "■ 25 formações rochosas\n" +
            "■ Iluminação lunar azul/prata\n" +
            "■ Partículas etéreas (luzes flutuantes)\n" +
            "■ Física Rigidbody realista\n" +
            "■ 8 Skeletons + 4 Hollows\n" +
            "■ GPU Instancing + Static Batching\n\n" +
            "A cena atual será limpa. Continuar?", "Gerar Liurnia", "Cancelar"))
            return;

        float startTime = (float)EditorApplication.timeSinceStartup;

        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Limpando cena...", 0f);
        ClearScene();

        // === 0. URP QUALITY ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Configurando URP...", 0.03f);
        URPQualityMaximizer.MaximizeURPQuality();

        // === 1. RENDER SETTINGS ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Render settings Liurnia...", 0.05f);
        SetupLiurniaRenderSettings();

        // === 2. TERRENO ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Gerando terreno e lago...", 0.1f);
        GameObject mapObj = new GameObject("LiurniaMapGenerator");
        LiurniaMapGenerator map = mapObj.AddComponent<LiurniaMapGenerator>();
        map.terrainSize = 512;
        map.terrainHeight = 35;
        map.waterLevel = 5.5f;
        map.treeCount = 40;
        map.rockFormationCount = 25;
        map.ruinCount = 15;
        map.useRandomSeed = true;
        map.GenerateMap();
        map.generatedInEditor = true;

        // === 3. ÁGUA ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Criando lago...", 0.35f);
        Terrain terrain = FindFirstObjectByType<Terrain>();
        Vector3 terrainCenter = Vector3.zero;
        if (terrain != null)
            terrainCenter = terrain.transform.position + new Vector3(
                terrain.terrainData.size.x * 0.5f, 0, terrain.terrainData.size.z * 0.45f);

        GameObject water = WaterPlaneGenerator.CreateWaterPlane(
            terrainCenter, 380f, map.waterLevel,
            new Color(0.06f, 0.12f, 0.28f), 0.6f);

        // === 4. NAVMESH ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Baking NavMesh...", 0.45f);
        SetupNavMesh();

        // === 5. PLAYER ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Criando player...", 0.55f);
        Vector3 playerSpawn = map.PlayerSpawnPosition;
        if (terrain != null)
        {
            float terrainY = terrain.SampleHeight(playerSpawn) + terrain.transform.position.y;
            playerSpawn.y = Mathf.Max(terrainY, map.waterLevel) + 0.5f;
        }
        GameObject player = CreatePlayer(playerSpawn);

        // === 6. CAMERA ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Configurando câmera...", 0.6f);
        SetupCamera(player);

        // === 7. ENEMIES ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Spawning enemies...", 0.7f);
        SpawnEnemies(map, terrain);

        // === 8. ATMOSPHERE ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Atmosfera mística...", 0.8f);
        AtmosphereSetup.CreateAtmosphereVolume(AtmosphereSetup.AtmospherePreset.Liurnia);
        SetupMoonlight();

        // === 9. PARTICLES ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Partículas etéreas...", 0.85f);
        CreateEtherealParticles(terrainCenter, map.waterLevel);

        // === 10. HUD & SYSTEMS ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "HUD e sistemas...", 0.9f);
        CreateHUD();
        CreateEventSystem();
        CreateGameManager();

        // === 11. GPU OPTIMIZATION ===
        EditorUtility.DisplayProgressBar("Gerando Liurnia...", "Otimizando GPU...", 0.95f);
        int gpuMats = GPURenderOptimizer.EnableGPUInstancingAll();
        int staticObjs = GPURenderOptimizer.MarkStaticsAll();
        GPURenderOptimizer.ConfigureQualityForPerformance();
        GPURenderOptimizer.OptimizeLighting();
        GPURenderOptimizer.OptimizeCamera();

        GameObject optObj = new GameObject("GPURenderOptimizer");
        optObj.AddComponent<GPURenderOptimizer>();

        EditorUtility.ClearProgressBar();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        float elapsed = (float)EditorApplication.timeSinceStartup - startTime;

        Debug.Log($"=== LIURNIA GERADA EM {elapsed:F1}s ===");

        EditorUtility.DisplayDialog("Liurnia - Lago Místico Criado!",
            $"Mapa gerado em {elapsed:F1}s!\n\n" +
            "★ Pressione PLAY para explorar ★\n\n" +
            $"GPU: {gpuMats} materiais, {staticObjs} objetos static\n\n" +
            "Controles:\n" +
            "  WASD = Mover (física real)\n" +
            "  Shift = Correr\n" +
            "  Mouse = Câmera\n" +
            "  Espaço = Dodge Roll\n" +
            "  F = Pular\n" +
            "  Click Esq = Ataque\n" +
            "  Click Dir = Bloquear\n" +
            "  Tab/Q = Lock-On\n" +
            "  E = Interagir (Fogueira)\n\n" +
            "Inspirado em Liurnia of the Lakes!", "Explorar!");

        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.pivot = playerSpawn + Vector3.up * 8f;
            sv.size = 20f;
            sv.Repaint();
        }
    }

    // =================== SCENE SETUP ====================

    private static void ClearScene()
    {
        GameObject[] allObjs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjs)
            if (obj != null && obj.transform.parent == null)
                DestroyImmediate(obj);
    }

    private static void SetupLiurniaRenderSettings()
    {
        // Atmosfera lunar azul/prata
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.15f, 0.2f, 0.35f);
        RenderSettings.ambientEquatorColor = new Color(0.1f, 0.15f, 0.25f);
        RenderSettings.ambientGroundColor = new Color(0.06f, 0.08f, 0.12f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.12f, 0.15f, 0.22f, 1f);
        RenderSettings.fogDensity = 0.003f;

        RenderSettings.skybox = null;
        RenderSettings.subtractiveShadowColor = new Color(0.1f, 0.12f, 0.2f);

        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowDistance = 150f;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowCascades = 4;
    }

    private static void SetupNavMesh()
    {
        GameObject navObj = new GameObject("NavMeshSurface");
        var surface = navObj.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.BuildNavMesh();
    }

    private static GameObject CreatePlayer(Vector3 spawnPos)
    {
        GameObject player = new GameObject("Player");
        player.transform.position = spawnPos;
        player.tag = "Player";

        // Rigidbody (física real)
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.mass = 70f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Capsule Collider
        CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
        capsule.height = 1.8f;
        capsule.radius = 0.35f;
        capsule.center = new Vector3(0, 0.9f, 0);

        // Sem fricção
        PhysicsMaterial noFriction = new PhysicsMaterial("PlayerPhysMat");
        noFriction.dynamicFriction = 0f;
        noFriction.staticFriction = 0f;
        noFriction.bounciness = 0f;
        noFriction.frictionCombine = PhysicsMaterialCombine.Minimum;
        capsule.material = noFriction;

        // Modelo visual
        HighQualityKnightGenerator.InitMaterials();
        GameObject model = HighQualityKnightGenerator.CreateKnight(Vector3.zero);
        model.transform.SetParent(player.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        // Scripts
        player.AddComponent<PlayerStats>();
        PlayerController controller = player.AddComponent<PlayerController>();
        PlayerCombat combat = player.AddComponent<PlayerCombat>();
        player.AddComponent<DirectInputHandler>();

        // Attack point
        GameObject attackPoint = new GameObject("AttackPoint");
        attackPoint.transform.SetParent(player.transform);
        attackPoint.transform.localPosition = new Vector3(0, 1f, 1.2f);
        combat.attackPoint = attackPoint.transform;
        combat.enemyLayers = LayerMask.GetMask("Default");

        // Lanterna
        GameObject playerLight = new GameObject("PlayerLight");
        playerLight.transform.SetParent(player.transform);
        playerLight.transform.localPosition = new Vector3(0, 1.5f, 0.3f);
        Light pl = playerLight.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(0.6f, 0.7f, 1f); // Luz fria/azulada para Liurnia
        pl.intensity = 0.5f;
        pl.range = 6f;
        pl.shadows = LightShadows.None;

        Debug.Log("[Liurnia] Player criado com física Rigidbody.");
        return player;
    }

    private static void SetupCamera(GameObject player)
    {
        GameObject camObj = new GameObject("MainCamera");
        Camera cam = camObj.AddComponent<Camera>();
        camObj.tag = "MainCamera";
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 600f;
        cam.fieldOfView = 55f;
        cam.backgroundColor = new Color(0.05f, 0.08f, 0.15f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        camObj.AddComponent<AudioListener>();
        camObj.transform.position = player.transform.position + new Vector3(0, 3, -5);
        camObj.transform.LookAt(player.transform.position + Vector3.up);

        ThirdPersonCamera tpc = camObj.AddComponent<ThirdPersonCamera>();
        tpc.target = player.transform;
        tpc.playerController = player.GetComponent<PlayerController>();
        tpc.offset = new Vector3(0, 1.8f, 0);
        tpc.defaultDistance = 6f;
        tpc.collisionLayers = LayerMask.GetMask("Default");

        player.GetComponent<PlayerController>().cameraTransform = camObj.transform;

        var urpCam = camObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        urpCam.renderPostProcessing = true;

        Debug.Log("[Liurnia] Câmera configurada.");
    }

    private static void SpawnEnemies(LiurniaMapGenerator map, Terrain terrain)
    {
        GameObject enemiesParent = new GameObject("=== ENEMIES ===");
        SkeletonGenerator.InitMaterials();
        HighQualityKnightGenerator.InitMaterials();

        int spawned = 0;
        var spawnPositions = map.EnemySpawnPositions;

        for (int i = 0; i < Mathf.Min(spawnPositions.Count, 12); i++)
        {
            Vector3 pos = spawnPositions[i];

            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 15f, NavMesh.AllAreas))
            {
                bool isSkeleton = (i < 8);
                string prefix = isSkeleton ? "Skeleton" : "Hollow";

                GameObject enemy = new GameObject($"Enemy_{prefix}_{spawned}");
                enemy.transform.position = hit.position;
                enemy.transform.SetParent(enemiesParent.transform);
                enemy.tag = "Enemy";

                CapsuleCollider col = enemy.AddComponent<CapsuleCollider>();
                col.height = 1.8f;
                col.radius = 0.35f;
                col.center = new Vector3(0, 0.9f, 0);

                GameObject model = isSkeleton
                    ? SkeletonGenerator.CreateSkeleton(Vector3.zero)
                    : HighQualityKnightGenerator.CreateHollow(Vector3.zero);
                model.transform.SetParent(enemy.transform);
                model.transform.localPosition = Vector3.zero;

                NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
                agent.speed = 3f;
                agent.stoppingDistance = 1.8f;
                agent.radius = 0.35f;
                agent.height = 1.8f;

                float difficulty = (float)i / 12f;
                EnemyStats stats = enemy.AddComponent<EnemyStats>();
                if (isSkeleton)
                {
                    stats.maxHealth = Mathf.Lerp(30f, 60f, difficulty);
                    stats.attackDamage = Mathf.Lerp(8f, 15f, difficulty);
                    stats.soulsReward = Mathf.RoundToInt(Mathf.Lerp(20, 60, difficulty));
                }
                else
                {
                    stats.maxHealth = Mathf.Lerp(80f, 120f, difficulty);
                    stats.attackDamage = Mathf.Lerp(15f, 25f, difficulty);
                    stats.soulsReward = Mathf.RoundToInt(Mathf.Lerp(60, 120, difficulty));
                }

                EnemyAI ai = enemy.AddComponent<EnemyAI>();
                ai.detectionRange = Mathf.Lerp(10f, 16f, difficulty);
                ai.attackRange = 2f;
                ai.patrolSpeed = 2f;
                ai.chaseSpeed = Mathf.Lerp(3.5f, 5f, difficulty);

                enemy.AddComponent<EnemyHealthBar>();
                spawned++;
            }
        }

        Debug.Log($"[Liurnia] {spawned} inimigos spawnados.");
    }

    private static void SetupMoonlight()
    {
        // Luz direcional lunar
        GameObject moonObj = new GameObject("Moonlight");
        Light moon = moonObj.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.6f, 0.7f, 0.95f); // Azul prateado
        moon.intensity = 1.8f;
        moon.shadows = LightShadows.Soft;
        moon.shadowStrength = 0.7f;
        moonObj.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

        // Fill light suave (de baixo/frente)
        GameObject fillObj = new GameObject("FillLight");
        Light fill = fillObj.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.15f, 0.2f, 0.35f);
        fill.intensity = 0.4f;
        fill.shadows = LightShadows.None;
        fillObj.transform.rotation = Quaternion.Euler(-10f, 150f, 0f);
    }

    private static void CreateEtherealParticles(Vector3 center, float waterLevel)
    {
        GameObject particlesParent = new GameObject("=== ETHEREAL EFFECTS ===");

        // Luzes flutuantes azuis (estilo Liurnia)
        GameObject floatingLights = new GameObject("FloatingLights");
        floatingLights.transform.SetParent(particlesParent.transform);
        floatingLights.transform.position = new Vector3(center.x, waterLevel + 5f, center.z);

        ParticleSystem ps = floatingLights.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.4f, 0.6f, 1f, 0.7f),
            new Color(0.6f, 0.8f, 1f, 0.9f));
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.01f; // Float upward slightly

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(200f, 15f, 200f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.1f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f);
        sizeCurve.AddKey(0.15f, 1f);
        sizeCurve.AddKey(0.85f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.3f, 0.5f, 1f), 0f),
                new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.5f, 1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.8f, 0.2f),
                new GradientAlphaKey(0.8f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var renderer = floatingLights.GetComponent<ParticleSystemRenderer>();
        Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (particleMat.shader == null)
            particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
        particleMat.color = new Color(0.5f, 0.7f, 1f, 0.8f);
        particleMat.EnableKeyword("_EMISSION");
        particleMat.SetColor("_EmissionColor", new Color(0.3f, 0.5f, 1f) * 2f);
        renderer.material = particleMat;

        // Fog leve sobre a água
        GameObject fogObj = new GameObject("LakeFog");
        fogObj.transform.SetParent(particlesParent.transform);
        fogObj.transform.position = new Vector3(center.x, waterLevel + 1f, center.z);

        ParticleSystem fogPS = fogObj.AddComponent<ParticleSystem>();
        var fogMain = fogPS.main;
        fogMain.loop = true;
        fogMain.startLifetime = new ParticleSystem.MinMaxCurve(8f, 15f);
        fogMain.startSpeed = 0.05f;
        fogMain.startSize = new ParticleSystem.MinMaxCurve(8f, 20f);
        fogMain.startColor = new Color(0.2f, 0.25f, 0.35f, 0.15f);
        fogMain.maxParticles = 40;
        fogMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var fogEmission = fogPS.emission;
        fogEmission.rateOverTime = 3f;

        var fogShape = fogPS.shape;
        fogShape.shapeType = ParticleSystemShapeType.Box;
        fogShape.scale = new Vector3(250f, 3f, 250f);

        var fogNoise = fogPS.noise;
        fogNoise.enabled = true;
        fogNoise.strength = 0.2f;
        fogNoise.frequency = 0.1f;

        var fogRenderer = fogObj.GetComponent<ParticleSystemRenderer>();
        Material fogMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (fogMat.shader == null)
            fogMat = new Material(Shader.Find("Particles/Standard Unlit"));
        fogMat.color = new Color(0.2f, 0.25f, 0.35f, 0.12f);
        fogRenderer.material = fogMat;
    }

    private static void CreateHUD()
    {
        new GameObject("PlayerHUD").AddComponent<PlayerHUD>();
    }

    private static void CreateEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    private static void CreateGameManager()
    {
        if (FindFirstObjectByType<GameManager>() == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }
#endif
}
