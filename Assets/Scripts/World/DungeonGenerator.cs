using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gerador procedural de mazmorra/dungeon estilo Dark Souls.
/// Cria geometria visual usando primitivos do Unity (cubes, planes).
/// Gera: salão principal (hub), corredores, salas, escadarias, arena de boss.
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    [Header("Configurações do Mapa")]
    public int gridWidth = 30;
    public int gridHeight = 30;
    public float cellSize = 4f;
    public float wallHeight = 5f;
    public float corridorWidth = 4f;

    [Header("Materiais (auto-criados se null)")]
    public Material floorMaterial;
    public Material wallMaterial;
    public Material ceilingMaterial;
    public Material pillarMaterial;
    public Material bossDoorMaterial;

    [Header("Iluminação")]
    public float torchIntensity = 1.5f;
    public float torchRange = 8f;
    public Color torchColor = new Color(1f, 0.6f, 0.2f, 1f);
    public int torchSpacing = 3; // a cada N células

    [Header("Seed")]
    public int seed = 0;
    public bool useRandomSeed = true;

    // Dados do mapa
    private int[,] map; // 0=vazio, 1=chão, 2=hub, 3=boss arena, 4=corredor, 5=sala
    private GameObject mapParent;
    private List<Vector2Int> roomCenters = new List<Vector2Int>();

    // Posições importantes (acessíveis publicamente)
    public Vector3 PlayerSpawnPosition { get; private set; }
    public Vector3 BossArenaCenter { get; private set; }
    public Vector3 BonfirePosition { get; private set; }
    public List<Vector3> EnemySpawnPositions { get; private set; } = new List<Vector3>();

    private void Awake()
    {
        if (useRandomSeed)
            seed = Random.Range(0, 100000);
        Random.InitState(seed);
    }

    /// <summary>
    /// Gera o mapa completo. Pode ser chamado do Start() ou de um Editor Script.
    /// </summary>
    public void GenerateMap()
    {
        ClearMap();
        CreateMaterials();
        InitializeGrid();
        CarveHub();
        CarveCorridors();
        CarveRooms();
        CarveBossArena();
        ConnectRooms();
        BuildGeometry();
        PlaceTorches();
        PlaceBonfire();
        CalculateSpawnPositions();

        Debug.Log($"[DungeonGenerator] Mapa gerado com seed {seed}. " +
                  $"Salas: {roomCenters.Count}, Inimigos: {EnemySpawnPositions.Count}");
    }

    private void Start()
    {
        GenerateMap();
    }

    #region Limpeza

    public void ClearMap()
    {
        if (mapParent != null)
        {
            if (Application.isPlaying)
                Destroy(mapParent);
            else
                DestroyImmediate(mapParent);
        }

        roomCenters.Clear();
        EnemySpawnPositions.Clear();
    }

    #endregion

    #region Criação de Materiais

    private void CreateMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        if (floorMaterial == null)
        {
            floorMaterial = new Material(urpLit);
            floorMaterial.name = "Floor_DarkStone";
            floorMaterial.color = new Color(0.15f, 0.13f, 0.12f, 1f);
            floorMaterial.SetFloat("_Smoothness", 0.2f);
        }

        if (wallMaterial == null)
        {
            wallMaterial = new Material(urpLit);
            wallMaterial.name = "Wall_Stone";
            wallMaterial.color = new Color(0.22f, 0.2f, 0.18f, 1f);
            wallMaterial.SetFloat("_Smoothness", 0.15f);
        }

        if (ceilingMaterial == null)
        {
            ceilingMaterial = new Material(urpLit);
            ceilingMaterial.name = "Ceiling_DarkStone";
            ceilingMaterial.color = new Color(0.1f, 0.09f, 0.08f, 1f);
            ceilingMaterial.SetFloat("_Smoothness", 0.1f);
        }

        if (pillarMaterial == null)
        {
            pillarMaterial = new Material(urpLit);
            pillarMaterial.name = "Pillar_Stone";
            pillarMaterial.color = new Color(0.25f, 0.22f, 0.2f, 1f);
            pillarMaterial.SetFloat("_Smoothness", 0.3f);
        }

        if (bossDoorMaterial == null)
        {
            bossDoorMaterial = new Material(urpLit);
            bossDoorMaterial.name = "BossDoor_FogGate";
            bossDoorMaterial.color = new Color(0.5f, 0.4f, 0.8f, 0.6f);
            // Tornar semi-transparente
            bossDoorMaterial.SetFloat("_Surface", 1); // Transparent
            bossDoorMaterial.SetFloat("_Blend", 0);
            bossDoorMaterial.SetOverrideTag("RenderType", "Transparent");
            bossDoorMaterial.renderQueue = 3000;
            bossDoorMaterial.SetFloat("_Smoothness", 0.8f);
        }
    }

    #endregion

    #region Grid / Layout

    private void InitializeGrid()
    {
        map = new int[gridWidth, gridHeight];
        // Tudo começa como vazio (0)
    }

    /// <summary>
    /// Hub central — salão inicial onde o player spawna.
    /// </summary>
    private void CarveHub()
    {
        int hubSize = 5;
        int cx = gridWidth / 2;
        int cy = 3; // perto do início

        for (int x = cx - hubSize; x <= cx + hubSize; x++)
        {
            for (int y = cy - hubSize / 2; y <= cy + hubSize / 2; y++)
            {
                if (InBounds(x, y))
                    map[x, y] = 2; // hub
            }
        }

        roomCenters.Add(new Vector2Int(cx, cy));
        PlayerSpawnPosition = GridToWorld(cx, cy) + Vector3.up * 0.5f;
    }

    /// <summary>
    /// Corredores principais conectando áreas do mapa.
    /// </summary>
    private void CarveCorridors()
    {
        int cx = gridWidth / 2;

        // Corredor central (norte-sul)
        for (int y = 1; y < gridHeight - 2; y++)
        {
            for (int w = -1; w <= 1; w++)
            {
                if (InBounds(cx + w, y))
                    if (map[cx + w, y] == 0)
                        map[cx + w, y] = 4;
            }
        }

        // Corredores laterais
        int[] lateralYs = { gridHeight / 3, gridHeight * 2 / 3 };
        foreach (int ly in lateralYs)
        {
            for (int x = 3; x < gridWidth - 3; x++)
            {
                if (InBounds(x, ly))
                    if (map[x, ly] == 0)
                        map[x, ly] = 4;
            }
        }
    }

    /// <summary>
    /// Salas conectadas aos corredores.
    /// </summary>
    private void CarveRooms()
    {
        int numRooms = Random.Range(4, 7);

        for (int i = 0; i < numRooms; i++)
        {
            int roomW = Random.Range(3, 6);
            int roomH = Random.Range(3, 6);
            int rx = Random.Range(4, gridWidth - roomW - 4);
            int ry = Random.Range(4, gridHeight - roomH - 4);

            // Verificar se há espaço
            bool canPlace = true;
            for (int x = rx; x < rx + roomW && canPlace; x++)
                for (int y = ry; y < ry + roomH && canPlace; y++)
                    if (!InBounds(x, y) || map[x, y] == 2 || map[x, y] == 3)
                        canPlace = false;

            if (!canPlace) continue;

            for (int x = rx; x < rx + roomW; x++)
                for (int y = ry; y < ry + roomH; y++)
                    if (InBounds(x, y))
                        map[x, y] = 5;

            Vector2Int center = new Vector2Int(rx + roomW / 2, ry + roomH / 2);
            roomCenters.Add(center);
        }
    }

    /// <summary>
    /// Arena do Boss no final do mapa.
    /// </summary>
    private void CarveBossArena()
    {
        int arenaSize = 6;
        int cx = gridWidth / 2;
        int cy = gridHeight - arenaSize - 2;

        for (int x = cx - arenaSize; x <= cx + arenaSize; x++)
        {
            for (int y = cy - arenaSize; y <= cy + arenaSize; y++)
            {
                if (InBounds(x, y))
                {
                    // Forma circular
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist <= arenaSize)
                        map[x, y] = 3;
                }
            }
        }

        roomCenters.Add(new Vector2Int(cx, cy));
        BossArenaCenter = GridToWorld(cx, cy) + Vector3.up * 0.5f;
    }

    /// <summary>
    /// Conecta salas ao corredor mais próximo.
    /// </summary>
    private void ConnectRooms()
    {
        int cx = gridWidth / 2;

        foreach (var room in roomCenters)
        {
            // Conectar ao corredor central
            int startX = Mathf.Min(room.x, cx);
            int endX = Mathf.Max(room.x, cx);

            for (int x = startX; x <= endX; x++)
            {
                if (InBounds(x, room.y) && map[x, room.y] == 0)
                    map[x, room.y] = 4;
            }
        }
    }

    #endregion

    #region Construção de Geometria

    private void BuildGeometry()
    {
        mapParent = new GameObject("=== DUNGEON MAP ===");
        mapParent.transform.position = Vector3.zero;
        mapParent.isStatic = true;

        GameObject floorsParent = CreateChild(mapParent, "Floors");
        GameObject wallsParent = CreateChild(mapParent, "Walls");
        GameObject ceilingsParent = CreateChild(mapParent, "Ceilings");
        GameObject pillarsParent = CreateChild(mapParent, "Pillars");
        GameObject fogGatesParent = CreateChild(mapParent, "FogGates");

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (map[x, y] == 0) continue;

                Vector3 worldPos = GridToWorld(x, y);

                // === CHÃO ===
                GameObject floor = CreatePrimitive($"Floor_{x}_{y}", PrimitiveType.Cube,
                    worldPos, new Vector3(cellSize, 0.3f, cellSize), floorMaterial);
                floor.transform.SetParent(floorsParent.transform);
                floor.isStatic = true;
                floor.layer = LayerMask.NameToLayer("Default");

                // === TETO ===
                float ceilHeight = map[x, y] == 3 ? wallHeight * 1.5f : wallHeight;
                GameObject ceiling = CreatePrimitive($"Ceiling_{x}_{y}", PrimitiveType.Cube,
                    worldPos + Vector3.up * ceilHeight,
                    new Vector3(cellSize, 0.2f, cellSize), ceilingMaterial);
                ceiling.transform.SetParent(ceilingsParent.transform);
                ceiling.isStatic = true;

                // === PAREDES (nas bordas) ===
                // Parede se a célula vizinha é vazia ou fora do mapa
                if (!IsFloor(x - 1, y))
                    CreateWall(wallsParent, worldPos, Vector3.left, ceilHeight);
                if (!IsFloor(x + 1, y))
                    CreateWall(wallsParent, worldPos, Vector3.right, ceilHeight);
                if (!IsFloor(x, y - 1))
                    CreateWall(wallsParent, worldPos, Vector3.back, ceilHeight);
                if (!IsFloor(x, y + 1))
                    CreateWall(wallsParent, worldPos, Vector3.forward, ceilHeight);

                // === PILARES (cantos de salas grandes) ===
                if (map[x, y] == 2 || map[x, y] == 3 || map[x, y] == 5)
                {
                    if (x % 3 == 0 && y % 3 == 0)
                    {
                        // Verificar se não está na borda
                        if (IsFloor(x - 1, y) && IsFloor(x + 1, y) &&
                            IsFloor(x, y - 1) && IsFloor(x, y + 1))
                        {
                            GameObject pillar = CreatePrimitive($"Pillar_{x}_{y}",
                                PrimitiveType.Cylinder,
                                worldPos + Vector3.up * (ceilHeight * 0.5f),
                                new Vector3(0.6f, ceilHeight * 0.5f, 0.6f),
                                pillarMaterial);
                            pillar.transform.SetParent(pillarsParent.transform);
                            pillar.isStatic = true;
                        }
                    }
                }
            }
        }

        // === FOG GATE (entrada da boss arena) ===
        int bx = gridWidth / 2;
        int by = gridHeight - 14;
        Vector3 fogPos = GridToWorld(bx, by) + Vector3.up * (wallHeight * 0.5f);
        GameObject fogGate = CreatePrimitive("FogGate_Boss", PrimitiveType.Cube,
            fogPos, new Vector3(cellSize * 2, wallHeight, 0.2f), bossDoorMaterial);
        fogGate.transform.SetParent(fogGatesParent.transform);
        fogGate.tag = "Untagged";
        // Adicionar trigger
        BoxCollider fogTrigger = fogGate.AddComponent<BoxCollider>();
        fogTrigger.isTrigger = true;
        fogTrigger.size = new Vector3(1, 1, 5);

        // Paredes decorativas na entrada do boss
        CreatePrimitive("BossEntrance_Left", PrimitiveType.Cube,
            fogPos + Vector3.left * cellSize + Vector3.down * wallHeight * 0.2f,
            new Vector3(1f, wallHeight * 1.2f, 1f), pillarMaterial)
            .transform.SetParent(fogGatesParent.transform);

        CreatePrimitive("BossEntrance_Right", PrimitiveType.Cube,
            fogPos + Vector3.right * cellSize + Vector3.down * wallHeight * 0.2f,
            new Vector3(1f, wallHeight * 1.2f, 1f), pillarMaterial)
            .transform.SetParent(fogGatesParent.transform);
    }

    private void CreateWall(GameObject parent, Vector3 cellCenter, Vector3 direction, float height)
    {
        Vector3 wallPos = cellCenter + direction * (cellSize * 0.5f) + Vector3.up * (height * 0.5f);
        Vector3 wallScale;

        if (direction == Vector3.left || direction == Vector3.right)
            wallScale = new Vector3(0.4f, height, cellSize);
        else
            wallScale = new Vector3(cellSize, height, 0.4f);

        GameObject wall = CreatePrimitive($"Wall", PrimitiveType.Cube,
            wallPos, wallScale, wallMaterial);
        wall.transform.SetParent(parent.transform);
        wall.isStatic = true;
    }

    #endregion

    #region Iluminação

    private void PlaceTorches()
    {
        GameObject torchParent = CreateChild(mapParent, "Torches");

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (map[x, y] == 0) continue;
                if (x % torchSpacing != 0 || y % torchSpacing != 0) continue;

                // Colocar tocha apenas perto de paredes
                bool nearWall = !IsFloor(x - 1, y) || !IsFloor(x + 1, y) ||
                                !IsFloor(x, y - 1) || !IsFloor(x, y + 1);

                if (!nearWall) continue;

                Vector3 worldPos = GridToWorld(x, y);

                // Luz
                GameObject torchObj = new GameObject($"Torch_{x}_{y}");
                torchObj.transform.SetParent(torchParent.transform);
                torchObj.transform.position = worldPos + Vector3.up * (wallHeight * 0.65f);

                Light torchLight = torchObj.AddComponent<Light>();
                torchLight.type = LightType.Point;
                torchLight.color = torchColor;
                torchLight.intensity = torchIntensity;
                torchLight.range = torchRange;
                torchLight.shadows = LightShadows.Soft;

                // Base visual da tocha
                GameObject torchVisual = CreatePrimitive($"TorchBase_{x}_{y}",
                    PrimitiveType.Cylinder,
                    worldPos + Vector3.up * (wallHeight * 0.5f),
                    new Vector3(0.1f, 0.3f, 0.1f), pillarMaterial);
                torchVisual.transform.SetParent(torchObj.transform);

                // "Chama" (esfera emissiva)
                GameObject flame = CreatePrimitive($"Flame_{x}_{y}",
                    PrimitiveType.Sphere,
                    worldPos + Vector3.up * (wallHeight * 0.65f),
                    new Vector3(0.15f, 0.2f, 0.15f), null);

                Renderer flameRend = flame.GetComponent<Renderer>();
                Material flameMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                flameMat.color = torchColor;
                flameMat.EnableKeyword("_EMISSION");
                flameMat.SetColor("_EmissionColor", torchColor * 3f);
                flameRend.material = flameMat;
                flame.transform.SetParent(torchObj.transform);

                // Destruir collider da chama
                Collider flameCol = flame.GetComponent<Collider>();
                if (flameCol != null)
                {
                    if (Application.isPlaying) Destroy(flameCol);
                    else DestroyImmediate(flameCol);
                }
            }
        }

        // Luz ambiente na boss arena
        if (BossArenaCenter != Vector3.zero)
        {
            GameObject bossLight = new GameObject("BossArenaLight");
            bossLight.transform.SetParent(torchParent.transform);
            bossLight.transform.position = BossArenaCenter + Vector3.up * (wallHeight * 1.2f);
            Light bl = bossLight.AddComponent<Light>();
            bl.type = LightType.Point;
            bl.color = new Color(0.4f, 0.2f, 0.6f); // roxo sombrio
            bl.intensity = 2f;
            bl.range = 25f;
            bl.shadows = LightShadows.Soft;
        }
    }

    #endregion

    #region Pontos de interesse

    private void PlaceBonfire()
    {
        int cx = gridWidth / 2;
        int cy = 3;
        BonfirePosition = GridToWorld(cx, cy + 2) + Vector3.up * 0.3f;

        GameObject bonfireObj = new GameObject("Bonfire");
        bonfireObj.transform.SetParent(mapParent.transform);
        bonfireObj.transform.position = BonfirePosition;

        // Base de pedra
        GameObject base1 = CreatePrimitive("BonfireBase", PrimitiveType.Cylinder,
            BonfirePosition, new Vector3(1f, 0.2f, 1f), pillarMaterial);
        base1.transform.SetParent(bonfireObj.transform);

        // "Espada" central
        GameObject sword = CreatePrimitive("BonfireSword", PrimitiveType.Cube,
            BonfirePosition + Vector3.up * 0.7f,
            new Vector3(0.08f, 1.2f, 0.08f), wallMaterial);
        sword.transform.SetParent(bonfireObj.transform);

        // Fogo
        GameObject fire = new GameObject("BonfireFire");
        fire.transform.SetParent(bonfireObj.transform);
        fire.transform.position = BonfirePosition + Vector3.up * 0.4f;

        Light fireLight = fire.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f, 0.5f, 0.1f);
        fireLight.intensity = 3f;
        fireLight.range = 12f;
        fireLight.shadows = LightShadows.Soft;

        // Esferas de "fogo" emissivo
        for (int i = 0; i < 3; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-0.15f, 0.15f),
                Random.Range(0.2f, 0.5f),
                Random.Range(-0.15f, 0.15f));

            GameObject fireSphere = CreatePrimitive($"FireOrb_{i}", PrimitiveType.Sphere,
                BonfirePosition + Vector3.up * 0.3f + offset,
                Vector3.one * Random.Range(0.1f, 0.2f), null);

            Renderer r = fireSphere.GetComponent<Renderer>();
            Material fireMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            fireMat.color = new Color(1f, 0.4f, 0.05f);
            fireMat.EnableKeyword("_EMISSION");
            fireMat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.05f) * 5f);
            r.material = fireMat;
            fireSphere.transform.SetParent(bonfireObj.transform);

            Collider c = fireSphere.GetComponent<Collider>();
            if (c != null) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
        }

        // Componente de bonfire (interatividade)
        bonfireObj.AddComponent<Bonfire>();
    }

    private void CalculateSpawnPositions()
    {
        EnemySpawnPositions.Clear();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Colocar inimigos em corredores e salas (não no hub ou boss arena)
                if (map[x, y] == 4 || map[x, y] == 5)
                {
                    // Chance de 8% por célula
                    if (Random.value < 0.08f)
                    {
                        Vector3 pos = GridToWorld(x, y) + Vector3.up * 1f;
                        EnemySpawnPositions.Add(pos);
                    }
                }
            }
        }
    }

    #endregion

    #region Helpers

    private Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(
            (x - gridWidth * 0.5f) * cellSize,
            0f,
            y * cellSize
        );
    }

    private bool InBounds(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    private bool IsFloor(int x, int y)
    {
        return InBounds(x, y) && map[x, y] != 0;
    }

    private GameObject CreatePrimitive(string name, PrimitiveType type,
        Vector3 position, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;

        if (mat != null)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            rend.material = mat;
        }

        return obj;
    }

    private GameObject CreateChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        return child;
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (map == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (map[x, y] == 0) continue;

                switch (map[x, y])
                {
                    case 2: Gizmos.color = Color.green; break;   // Hub
                    case 3: Gizmos.color = Color.red; break;     // Boss
                    case 4: Gizmos.color = Color.gray; break;    // Corredor
                    case 5: Gizmos.color = Color.cyan; break;    // Sala
                    default: Gizmos.color = Color.white; break;
                }

                Gizmos.DrawCube(GridToWorld(x, y), Vector3.one * cellSize * 0.9f);
            }
        }

        // Spawn positions
        Gizmos.color = Color.yellow;
        foreach (var pos in EnemySpawnPositions)
        {
            Gizmos.DrawSphere(pos, 0.5f);
        }
    }
}
