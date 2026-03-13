using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Editor Script para configurar a cena de Floresta Soulslike completa.
/// Menu: Soulslike > Configurar Floresta Sombria
/// Cria: Terreno, Árvores, Player Cavaleiro, Inimigos, Atmosfera, Neblina, Vagalumes, UI.
/// </summary>
public class ForestSceneSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Soulslike/Configurar Floresta Sombria")]
    public static void SetupForestScene()
    {
        if (!EditorUtility.DisplayDialog("Soulslike - Floresta Sombria",
            "Isso vai criar a cena completa da floresta com:\n\n" +
            "■ Terreno com heightmap procedural\n" +
            "■ 600+ árvores (5 espécies)\n" +
            "■ 120+ rochas e 8 ruínas\n" +
            "■ Player Cavaleiro com espada e escudo\n" +
            "■ 12 inimigos Hollow\n" +
            "■ Fogueira (bonfire)\n" +
            "■ Pós-processamento atmosférico\n" +
            "■ Neblina, vagalumes, folhas caindo\n" +
            "■ Iluminação florestal\n" +
            "■ NavMesh para IA\n" +
            "■ HUD completa\n\n" +
            "A cena atual será limpa. Continuar?", "Gerar Floresta", "Cancelar"))
            return;

        float startTime = (float)EditorApplication.timeSinceStartup;
        
        EditorUtility.DisplayProgressBar("Gerando Floresta...", "Limpando cena...", 0f);
        ClearScene();

        // === 1. RENDER SETTINGS ===
        EditorUtility.DisplayProgressBar("Gerando Floresta...", "Configurando render...", 0.05f);
        SetupForestRenderSettings();

        // === 2. TERRENO + ÁRVORES + ROCHAS ===
        EditorUtility.DisplayProgressBar("Gerando Floresta...", "Gerando terreno e árvores (pode levar um momento)...", 0.1f);
        GameObject forestObj = new GameObject("ForestMapGenerator");
        ForestMapGenerator forest = forestObj.AddComponent<ForestMapGenerator>();

        // Configurar parâmetros
        forest.terrainWidth = 512;
        forest.terrainHeight = 80;
        forest.terrainLength = 512;
        forest.treeCount = 600;
        forest.rockCount = 120;
        forest.ruinCount = 8;
        forest.useRandomSeed = true;

        // Gerar o mapa
        forest.GenerateForest();

        // === 3. NAVMESH ===
        EditorUtility.DisplayProgressBar("Gerando Floresta...", "Baking NavMesh...", 0.4f);
        SetupNavMesh(forestObj);

        // === 4. PLAYER CAVALEIRO ===
        EditorUtility.DisplayProgressBar("Gerando Floresta...", "Criando player cavaleiro...", 0.5f);
        Vector3 playerSpawn = forest.PlayerSpawnPosition;
        // Garantir posição acima do terreno
        Terrain terrain = FindFirstObjectByType<Terrain>();
        if (terrain != null)
        {
            float terrainY = terrain.SampleHeight(playerSpawn) + terrain.transform.position.y;
            playerSpawn.y = terrainY + 0.1f;
        }
        GameObject player = CreateKnightPlayer(playerSpawn);

        // === 5. CAMERA ===
        EditorUtility.DisplayProgressBar("Gerando Floresta...", "Configurando câmera...", 0.6f);
        SetupCamera(player);

        // === 6. INIMIGOS HOLLOW ===
        EditorUtility.DisplayProgressBar("Gerando Floresta...", "Spawnando inimigos...", 0.7f);
        SpawnForestEnemies(forest, terrain);

        // === 7. ATMOSFERA (Pós-Processamento) ===
        EditorUtility.DisplayProgressBar("Gerando Floresta...", "Criando atmosfera...", 0.8f);
        AtmosphereSetup.CreateAtmosphereVolume(AtmosphereSetup.AtmospherePreset.DarkForest);
        AtmosphereSetup.SetupDirectionalLight();

        // === 8. PARTÍCULAS AMBIENTAIS ===
        EditorUtility.DisplayProgressBar("Gerando Floresta...", "Adicionando efeitos ambientais...", 0.85f);
        Vector3 forestCenter = new Vector3(256, 0, 256);
        if (terrain != null)
            forestCenter.y = terrain.SampleHeight(forestCenter) + terrain.transform.position.y;

        GameObject ambientParent = new GameObject("=== AMBIENT EFFECTS ===");
        
        GameObject fog = AtmosphereSetup.CreateFogParticles(forestCenter, 180f);
        fog.transform.SetParent(ambientParent.transform);

        GameObject fireflies = AtmosphereSetup.CreateFireflies(forestCenter, 150f);
        fireflies.transform.SetParent(ambientParent.transform);

        GameObject leaves = AtmosphereSetup.CreateFallingLeaves(forestCenter, 180f);
        leaves.transform.SetParent(ambientParent.transform);

        // === 9. HUD ===
        EditorUtility.DisplayProgressBar("Gerando Floresta...", "Criando HUD...", 0.9f);
        CreateHUD();

        // === 10. EVENT SYSTEM ===
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // === 11. GAME MANAGER ===
        if (FindFirstObjectByType<GameManager>() == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        EditorUtility.ClearProgressBar();

        // === FINALIZAR ===
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        float elapsed = (float)EditorApplication.timeSinceStartup - startTime;
        
        Debug.Log($"=== FLORESTA SOMBRIA GERADA EM {elapsed:F1}s ===");
        Debug.Log($"    Terreno: {forest.terrainWidth}x{forest.terrainLength}");
        Debug.Log($"    Árvores: ~{forest.treeCount}");
        Debug.Log($"    Rochas: ~{forest.rockCount}");
        Debug.Log($"    Ruínas: {forest.ruinCount}");
        Debug.Log($"    Inimigos: ~12");

        EditorUtility.DisplayDialog("Floresta Sombria Criada!",
            $"A floresta foi gerada com sucesso em {elapsed:F1}s!\n\n" +
            "★ Pressione PLAY para explorar ★\n\n" +
            "Controles:\n" +
            "  WASD = Mover\n" +
            "  Shift = Correr\n" +
            "  Mouse = Câmera\n" +
            "  Click Esq = Ataque Leve\n" +
            "  Click Dir = Bloquear\n" +
            "  Espaço = Dodge Roll\n" +
            "  Tab = Lock-On\n" +
            "  E = Interagir (Fogueira)\n\n" +
            "Dica: Navegue pela Scene View para ver o mapa completo!", "Explorar!");

        // Focar na posição do player na Scene View
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.pivot = playerSpawn + Vector3.up * 5f;
            sv.size = 15f;
            sv.Repaint();
        }
    }

    private static void ClearScene()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.transform.parent == null)
            {
                DestroyImmediate(obj);
            }
        }
    }

    private static void SetupForestRenderSettings()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.15f, 0.18f, 0.12f);
        RenderSettings.ambientEquatorColor = new Color(0.08f, 0.1f, 0.06f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.05f, 0.03f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.18f, 0.2f, 0.14f, 1f); // Neblina esverdeada
        RenderSettings.fogDensity = 0.008f;

        // Skybox - usar gradiente se disponível, senão cor sólida
        RenderSettings.skybox = null; // Sem skybox material por enquanto
        RenderSettings.subtractiveShadowColor = new Color(0.2f, 0.18f, 0.15f);
    }

    private static void SetupNavMesh(GameObject parent)
    {
        // NavMesh em objeto separado para pegar terreno
        GameObject navObj = new GameObject("NavMeshSurface");
        var surface = navObj.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        
        // Bake
        surface.BuildNavMesh();
        Debug.Log("[Forest Setup] NavMesh baked.");
    }

    private static GameObject CreateKnightPlayer(Vector3 spawnPos)
    {
        // Root do player
        GameObject player = new GameObject("Player");
        player.transform.position = spawnPos;
        player.tag = "Player";

        // CharacterController
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.4f;

        // Modelo visual do cavaleiro
        GameObject knightModel = ProceduralKnightFactory.CreateKnight(Vector3.zero);
        knightModel.transform.SetParent(player.transform);
        knightModel.transform.localPosition = Vector3.zero;
        knightModel.transform.localRotation = Quaternion.identity;

        // Scripts de gameplay
        PlayerStats stats = player.AddComponent<PlayerStats>();
        PlayerController controller = player.AddComponent<PlayerController>();
        PlayerCombat combat = player.AddComponent<PlayerCombat>();

        // Attack Point
        GameObject attackPoint = new GameObject("AttackPoint");
        attackPoint.transform.SetParent(player.transform);
        attackPoint.transform.localPosition = new Vector3(0, 1f, 1.2f);
        combat.attackPoint = attackPoint.transform;
        combat.enemyLayers = LayerMask.GetMask("Default");

        // Ponto de luz sutil no player (lanterna)
        GameObject playerLight = new GameObject("PlayerAmbientLight");
        playerLight.transform.SetParent(player.transform);
        playerLight.transform.localPosition = new Vector3(0, 1.5f, 0.3f);
        Light pl = playerLight.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(1f, 0.85f, 0.6f);
        pl.intensity = 0.4f;
        pl.range = 6f;
        pl.shadows = LightShadows.Soft;

        Debug.Log("[Forest Setup] Player cavaleiro criado em " + spawnPos);
        return player;
    }

    private static void SetupCamera(GameObject player)
    {
        GameObject camObj = new GameObject("MainCamera");
        Camera cam = camObj.AddComponent<Camera>();
        camObj.tag = "MainCamera";
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;
        cam.fieldOfView = 60f;
        cam.backgroundColor = new Color(0.1f, 0.12f, 0.08f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        camObj.AddComponent<AudioListener>();

        camObj.transform.position = player.transform.position + new Vector3(0, 3, -5);
        camObj.transform.LookAt(player.transform.position + Vector3.up);

        // Third person camera script
        ThirdPersonCamera tpc = camObj.AddComponent<ThirdPersonCamera>();
        tpc.target = player.transform;
        tpc.playerController = player.GetComponent<PlayerController>();
        tpc.offset = new Vector3(0, 1.8f, 0);
        tpc.defaultDistance = 5f;
        tpc.collisionLayers = LayerMask.GetMask("Default");

        // Atualizar PlayerController
        PlayerController pc = player.GetComponent<PlayerController>();
        pc.cameraTransform = camObj.transform;

        // URP Camera com pós-processamento
        var urpCam = camObj.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (urpCam == null)
            urpCam = camObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        urpCam.renderPostProcessing = true;

        Debug.Log("[Forest Setup] Câmera configurada com post-processing.");
    }

    private static void SpawnForestEnemies(ForestMapGenerator forest, Terrain terrain)
    {
        GameObject enemiesParent = new GameObject("=== ENEMIES ===");
        int spawned = 0;

        // Posições dos inimigos ao longo do caminho 
        // (entre spawn e boss arena para que o jogador os encontre naturalmente)
        Vector3 spawnPos = forest.PlayerSpawnPosition;
        Vector3 bossPos = forest.BossArenaCenter;
        
        if (bossPos == Vector3.zero)
            bossPos = new Vector3(400, 0, 400);

        float pathLength = Vector3.Distance(spawnPos, bossPos);

        // Spawn 12 inimigos em posições estratégicas
        for (int i = 0; i < 12; i++)
        {
            float t = (i + 1f) / 13f; // distribuir ao longo do caminho
            Vector3 basePos = Vector3.Lerp(spawnPos, bossPos, t);

            // Offset lateral aleatório (para não ficar em fila)
            float lateralOffset = Random.Range(-20f, 20f);
            Vector3 perpDir = Vector3.Cross((bossPos - spawnPos).normalized, Vector3.up);
            Vector3 enemyPos = basePos + perpDir * lateralOffset;

            // Ajustar Y ao terreno
            if (terrain != null)
            {
                enemyPos.x = Mathf.Clamp(enemyPos.x, 30f, 480f);
                enemyPos.z = Mathf.Clamp(enemyPos.z, 30f, 480f);
                enemyPos.y = terrain.SampleHeight(enemyPos) + terrain.transform.position.y + 0.1f;
            }

            // Verificar NavMesh
            if (NavMesh.SamplePosition(enemyPos, out NavMeshHit hit, 15f, NavMesh.AllAreas))
            {
                // Criar inimigo com modelo Hollow
                GameObject enemy = new GameObject($"Enemy_Hollow_{spawned}");
                enemy.transform.position = hit.position;
                enemy.transform.SetParent(enemiesParent.transform);
                enemy.tag = "Enemy";

                // Capsule collider para físicas
                CapsuleCollider col = enemy.AddComponent<CapsuleCollider>();
                col.height = 1.8f;
                col.radius = 0.35f;
                col.center = new Vector3(0, 0.9f, 0);

                // Modelo visual Hollow
                GameObject hollowModel = ProceduralKnightFactory.CreateHollow(Vector3.zero);
                hollowModel.transform.SetParent(enemy.transform);
                hollowModel.transform.localPosition = Vector3.zero;

                // NavMeshAgent
                NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
                agent.speed = 3f;
                agent.stoppingDistance = 1.8f;
                agent.radius = 0.35f;
                agent.height = 1.8f;

                // Stats (variam conforme posição - mais fortes mais longe)
                EnemyStats stats = enemy.AddComponent<EnemyStats>();
                float difficulty = t; // 0 a 1
                stats.maxHealth = Mathf.Lerp(50f, 120f, difficulty);
                stats.attackDamage = Mathf.Lerp(10f, 25f, difficulty);
                stats.soulsReward = Mathf.RoundToInt(Mathf.Lerp(30, 120, difficulty));

                // IA
                EnemyAI ai = enemy.AddComponent<EnemyAI>();
                ai.detectionRange = Mathf.Lerp(8f, 14f, difficulty);
                ai.attackRange = 2f;
                ai.patrolSpeed = 2f;
                ai.chaseSpeed = Mathf.Lerp(3.5f, 5f, difficulty);

                // HP bar
                enemy.AddComponent<EnemyHealthBar>();

                spawned++;
            }
        }

        Debug.Log($"[Forest Setup] {spawned} Hollows spawnados na floresta.");
    }

    private static void CreateHUD()
    {
        GameObject hudObj = new GameObject("PlayerHUD");
        hudObj.AddComponent<PlayerHUD>();
        Debug.Log("[Forest Setup] HUD criada.");
    }

    // ========== MENU EXTRAS ==========

    [MenuItem("Soulslike/Regenerar Floresta (Nova Seed)")]
    public static void RegenerateForest()
    {
        ForestMapGenerator gen = FindFirstObjectByType<ForestMapGenerator>();
        if (gen != null)
        {
            gen.useRandomSeed = true;
            gen.GenerateForest();

            // Rebuild NavMesh
            NavMeshSurface surface = FindFirstObjectByType<NavMeshSurface>();
            if (surface != null)
                surface.BuildNavMesh();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Floresta regenerada com nova seed!");
        }
        else
        {
            EditorUtility.DisplayDialog("Erro",
                "Nenhum ForestMapGenerator encontrado.\n" +
                "Use 'Soulslike > Configurar Floresta Sombria' primeiro.", "OK");
        }
    }

    [MenuItem("Soulslike/Mudar Atmosfera/Floresta Sombria")]
    public static void SetAtmosphereDarkForest()
    {
        ReplaceAtmosphere(AtmosphereSetup.AtmospherePreset.DarkForest);
    }

    [MenuItem("Soulslike/Mudar Atmosfera/Arena de Boss")]
    public static void SetAtmosphereBossArena()
    {
        ReplaceAtmosphere(AtmosphereSetup.AtmospherePreset.BossArena);
    }

    [MenuItem("Soulslike/Mudar Atmosfera/Zona Segura")]
    public static void SetAtmosphereSafeZone()
    {
        ReplaceAtmosphere(AtmosphereSetup.AtmospherePreset.SafeZone);
    }

    [MenuItem("Soulslike/Mudar Atmosfera/Noturno")]
    public static void SetAtmosphereNighttime()
    {
        ReplaceAtmosphere(AtmosphereSetup.AtmospherePreset.Nighttime);
    }

    private static void ReplaceAtmosphere(AtmosphereSetup.AtmospherePreset preset)
    {
        // Remover volume existente
        var existingVolumes = FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
        foreach (var v in existingVolumes)
        {
            if (v.gameObject.name.Contains("Atmosphere"))
                DestroyImmediate(v.gameObject);
        }

        AtmosphereSetup.CreateAtmosphereVolume(preset);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Atmosfera alterada para: {preset}");
    }

    [MenuItem("Soulslike/Limpar Cena")]
    public static void CleanScene()
    {
        if (EditorUtility.DisplayDialog("Limpar Cena",
            "Isso vai remover TODOS os objetos da cena.\nContinuar?", "Limpar", "Cancelar"))
        {
            ClearScene();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Cena limpa.");
        }
    }
#endif
}
