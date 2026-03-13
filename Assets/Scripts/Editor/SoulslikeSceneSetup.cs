using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Editor Script para configurar a cena Soulslike completa com um clique.
/// Menu: Soulslike > Setup Scene
/// Cria: Dungeon, Player, Camera, Inimigos, UI, NavMesh, Iluminação.
/// </summary>
public class SoulslikeSceneSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Soulslike/Dungeon/Configurar Cena Completa")]
    public static void SetupScene()
    {
        // Confirmar
        if (!EditorUtility.DisplayDialog("Soulslike Setup",
            "Isso vai configurar a cena com:\n" +
            "- Dungeon Procedural\n" +
            "- Player + Camera\n" +
            "- Inimigos\n" +
            "- UI (HUD)\n" +
            "- Iluminação\n\n" +
            "Continuar?", "Sim", "Cancelar"))
            return;

        // Limpar cena atual
        ClearScene();

        // === 1. CONFIGURAÇÃO DE RENDER ===
        SetupRenderSettings();

        // === 2. DIRECTIONAL LIGHT ===
        SetupDirectionalLight();

        // === 3. DUNGEON GENERATOR ===
        GameObject dungeonObj = new GameObject("DungeonGenerator");
        DungeonGenerator dungeon = dungeonObj.AddComponent<DungeonGenerator>();
        dungeon.gridWidth = 30;
        dungeon.gridHeight = 30;
        dungeon.cellSize = 4f;
        dungeon.wallHeight = 5f;
        dungeon.torchSpacing = 4;
        // A dungeon se gera no Start(), mas vamos gerar preview no editor
        dungeon.GenerateMap();

        // === 4. NAVMESH SURFACE ===
        SetupNavMesh(dungeonObj);

        // === 5. PLAYER ===
        GameObject player = CreatePlayer(dungeon.PlayerSpawnPosition);

        // === 6. CAMERA ===
        SetupCamera(player);

        // === 7. INIMIGOS ===
        SpawnEnemies(dungeon);

        // === 8. HUD ===
        CreateHUD();

        // === 9. EVENT SYSTEM (para UI) ===
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Marcar cena como modificada
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("=== CENA SOULSLIKE CONFIGURADA COM SUCESSO ===");
        EditorUtility.DisplayDialog("Sucesso!", 
            "Cena Soulslike configurada!\n\n" +
            "Pressione Play para testar.\n" +
            "WASD = Mover\n" +
            "Shift = Correr\n" +
            "Mouse = Câmera\n" +
            "Click Esq = Ataque Leve\n" +
            "Click Dir = Bloquear\n" +
            "Espaço = Dodge Roll\n" +
            "Tab = Lock-On\n" +
            "E = Interagir (Bonfire)", "OK");
    }

    private static void ClearScene()
    {
        // Remover objetos existentes (manter a câmera e luz padrão se possível)
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.transform.parent == null && obj.name != "=== DUNGEON MAP ===")
            {
                DestroyImmediate(obj);
            }
        }
    }

    private static void SetupRenderSettings()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.04f, 0.06f, 1f); // muito escuro
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.04f, 1f);
        RenderSettings.fogDensity = 0.02f;

        // Skybox escuro (remover skybox para escuridão)
        RenderSettings.skybox = null;
        RenderSettings.ambientSkyColor = new Color(0.03f, 0.03f, 0.05f);
    }

    private static void SetupDirectionalLight()
    {
        GameObject lightObj = new GameObject("DirectionalLight_Moon");
        Light dirLight = lightObj.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.color = new Color(0.15f, 0.15f, 0.25f); // Lua fria e fraca
        dirLight.intensity = 0.3f;
        dirLight.shadows = LightShadows.Soft;
        dirLight.shadowStrength = 0.6f;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void SetupNavMesh(GameObject dungeonObj)
    {
        // Adicionar NavMeshSurface para que inimigos possam navegar
        var surface = dungeonObj.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = dungeonObj.AddComponent<NavMeshSurface>();

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

        // Bake do NavMesh
        surface.BuildNavMesh();
        Debug.Log("[Setup] NavMesh baked.");
    }

    private static GameObject CreatePlayer(Vector3 spawnPos)
    {
        // Criar player como cápsula
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = spawnPos;
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");

        // Visual
        Renderer rend = player.GetComponent<Renderer>();
        Material playerMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        playerMat.color = new Color(0.3f, 0.3f, 0.35f);
        playerMat.SetFloat("_Smoothness", 0.5f);
        rend.material = playerMat;

        // Remover collider da primitiva e adicionar CharacterController
        DestroyImmediate(player.GetComponent<CapsuleCollider>());

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = Vector3.up * 1f;
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.5f;

        // Scripts
        player.AddComponent<PlayerStats>();
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerCombat>();

        // Attack Point (filho)
        GameObject attackPoint = new GameObject("AttackPoint");
        attackPoint.transform.SetParent(player.transform);
        attackPoint.transform.localPosition = new Vector3(0, 1f, 1f);

        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        combat.attackPoint = attackPoint.transform;
        // Configurar a layer de inimigos
        combat.enemyLayers = LayerMask.GetMask("Default"); // TODO: trocar para "Enemy" layer

        // "Espada" visual no player
        GameObject sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sword.name = "Sword";
        sword.transform.SetParent(player.transform);
        sword.transform.localPosition = new Vector3(0.5f, 1f, 0.3f);
        sword.transform.localScale = new Vector3(0.08f, 0.8f, 0.08f);
        sword.transform.localRotation = Quaternion.Euler(0, 0, -20f);
        Renderer swordRend = sword.GetComponent<Renderer>();
        Material swordMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        swordMat.color = new Color(0.6f, 0.6f, 0.65f);
        swordMat.SetFloat("_Smoothness", 0.8f);
        swordRend.material = swordMat;
        DestroyImmediate(sword.GetComponent<BoxCollider>());

        // "Escudo" visual
        GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shield.name = "Shield";
        shield.transform.SetParent(player.transform);
        shield.transform.localPosition = new Vector3(-0.55f, 1f, 0.2f);
        shield.transform.localScale = new Vector3(0.1f, 0.6f, 0.5f);
        Renderer shieldRend = shield.GetComponent<Renderer>();
        Material shieldMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        shieldMat.color = new Color(0.35f, 0.25f, 0.15f);
        shieldRend.material = shieldMat;
        DestroyImmediate(shield.GetComponent<BoxCollider>());

        Debug.Log("[Setup] Player criado em " + spawnPos);
        return player;
    }

    private static void SetupCamera(GameObject player)
    {
        GameObject camObj = new GameObject("MainCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.fieldOfView = 60f;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        // Adicionar Audio Listener
        camObj.AddComponent<AudioListener>();

        // Posicionar atrás do player
        camObj.transform.position = player.transform.position + new Vector3(0, 3, -5);
        camObj.transform.LookAt(player.transform.position + Vector3.up);

        // Script de câmera
        ThirdPersonCamera tpc = camObj.AddComponent<ThirdPersonCamera>();
        tpc.target = player.transform;
        tpc.playerController = player.GetComponent<PlayerController>();
        tpc.offset = new Vector3(0, 1.8f, 0);
        tpc.defaultDistance = 5f;
        tpc.collisionLayers = LayerMask.GetMask("Default");

        // Atualizar referência no PlayerController
        PlayerController pc = player.GetComponent<PlayerController>();
        pc.cameraTransform = camObj.transform;

        // URP Camera Data
        var urpCamData = camObj.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (urpCamData == null)
            urpCamData = camObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        urpCamData.renderPostProcessing = true;

        Debug.Log("[Setup] Camera configurada.");
    }

    private static void SpawnEnemies(DungeonGenerator dungeon)
    {
        if (dungeon.EnemySpawnPositions == null || dungeon.EnemySpawnPositions.Count == 0)
        {
            Debug.LogWarning("[Setup] Nenhuma posição de spawn de inimigo.");
            return;
        }

        GameObject enemiesParent = new GameObject("=== ENEMIES ===");
        int count = 0;

        foreach (var pos in dungeon.EnemySpawnPositions)
        {
            // Verificar se posição está no NavMesh
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemy.name = $"Enemy_Hollow_{count}";
                enemy.transform.position = hit.position;
                enemy.transform.SetParent(enemiesParent.transform);
                enemy.tag = "Enemy";

                // Visual escuro/sombrio
                Renderer rend = enemy.GetComponent<Renderer>();
                Material enemyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                float shade = Random.Range(0.1f, 0.2f);
                enemyMat.color = new Color(shade, shade * 0.8f, shade * 0.7f);
                rend.material = enemyMat;

                // Ajustar collider para CapsuleCollider (já é por padrão da primitive)

                // NavMeshAgent
                NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
                agent.speed = 3.5f;
                agent.stoppingDistance = 1.5f;
                agent.radius = 0.4f;
                agent.height = 2f;

                // Scripts
                EnemyStats stats = enemy.AddComponent<EnemyStats>();
                stats.maxHealth = Random.Range(50f, 100f);
                stats.attackDamage = Random.Range(10f, 20f);
                stats.soulsReward = Random.Range(30, 80);

                EnemyAI ai = enemy.AddComponent<EnemyAI>();
                ai.detectionRange = 10f;
                ai.attackRange = 2f;
                ai.patrolSpeed = 2f;
                ai.chaseSpeed = 4f;

                // HP Bar
                enemy.AddComponent<EnemyHealthBar>();

                // "Arma" visual
                GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                weapon.name = "Weapon";
                weapon.transform.SetParent(enemy.transform);
                weapon.transform.localPosition = new Vector3(0.5f, 0.8f, 0.3f);
                weapon.transform.localScale = new Vector3(0.06f, 0.7f, 0.06f);
                weapon.transform.localRotation = Quaternion.Euler(0, 0, -30f);
                Renderer wRend = weapon.GetComponent<Renderer>();
                Material wMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                wMat.color = new Color(0.4f, 0.35f, 0.3f);
                wRend.material = wMat;
                DestroyImmediate(weapon.GetComponent<BoxCollider>());

                count++;
            }
        }

        Debug.Log($"[Setup] {count} inimigos spawnados.");
    }

    private static void CreateHUD()
    {
        GameObject hudObj = new GameObject("PlayerHUD");
        hudObj.AddComponent<PlayerHUD>();
        Debug.Log("[Setup] HUD criado.");
    }

    // ========== UTILIDADES EXTRAS ==========

    [MenuItem("Soulslike/Dungeon/Regenerar Dungeon (Nova Seed)")]
    public static void RegenerateDungeon()
    {
        DungeonGenerator gen = FindFirstObjectByType<DungeonGenerator>();
        if (gen != null)
        {
            gen.useRandomSeed = true;
            gen.GenerateMap();

            // Rebuild NavMesh
            NavMeshSurface surface = gen.GetComponent<NavMeshSurface>();
            if (surface != null)
                surface.BuildNavMesh();

            Debug.Log("Dungeon regenerada com nova seed!");
        }
        else
        {
            Debug.LogWarning("Nenhum DungeonGenerator encontrado. Use 'Soulslike > Configurar Cena Completa' primeiro.");
        }
    }

    [MenuItem("Soulslike/Dungeon/Limpar Cena Dungeon")]
    public static void CleanScene()
    {
        ClearScene();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Cena limpa.");
    }
#endif
}
