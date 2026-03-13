using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gera terreno estilo Liurnia of the Lakes (Elden Ring):
/// Lago central raso, colinas nas bordas, ruínas na água e em terra,
/// vegetação esparsa, trilhas de pedra.
/// Otimizado para performance: menos objetos, meshes combinados.
/// </summary>
public class LiurniaMapGenerator : MonoBehaviour
{
    [Header("Terreno")]
    public int terrainSize = 512;
    public int terrainHeight = 35;
    public int heightmapResolution = 513;

    [Header("Água")]
    public float waterLevel = 5.5f;

    [Header("Populacao")]
    public int treeCount = 40;
    public int rockFormationCount = 25;
    public int ruinCount = 15;

    [Header("Seed")]
    public int seed = 0;
    public bool useRandomSeed = true;

    // references
    private Terrain terrain;
    private TerrainData terrainData;
    private GameObject mapParent;

    // Materials
    private Material rockMat;
    private Material deadBarkMat;
    private Material sparseLeaveMat;

    // Spawn info
    public Vector3 PlayerSpawnPosition { get; private set; }
    public Vector3 BonfirePosition { get; private set; }
    public Vector3 BossArenaCenter { get; private set; }
    public List<Vector3> EnemySpawnPositions { get; private set; } = new List<Vector3>();
    public List<Vector3> RuinPositions { get; private set; } = new List<Vector3>();

    [HideInInspector] public bool generatedInEditor = false;

    private void Awake()
    {
        if (!generatedInEditor)
        {
            if (useRandomSeed) seed = Random.Range(0, 99999);
        }
        Random.InitState(seed);
    }

    private void Start()
    {
        if (!generatedInEditor) GenerateMap();
    }

    public void GenerateMap()
    {
        ClearMap();
        CreateMaterials();
        CreateTerrain();
        PaintTerrain();
        PlaceTrees();
        PlaceRockFormations();
        PlaceRuins();
        PlaceBonfire();
        CalculateSpawnPositions();

        Debug.Log($"[Liurnia] Mapa gerado! Seed: {seed}, Terrain: {terrainSize}x{terrainSize}");
    }

    public void ClearMap()
    {
        if (mapParent != null)
        {
            if (Application.isPlaying) Destroy(mapParent);
            else DestroyImmediate(mapParent);
        }

        Terrain existing = FindFirstObjectByType<Terrain>();
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        EnemySpawnPositions.Clear();
        RuinPositions.Clear();
    }

    #region Materials

    private void CreateMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        rockMat = new Material(urpLit);
        rockMat.name = "M_LiurniaRock";
        rockMat.color = new Color(0.4f, 0.42f, 0.45f);
        rockMat.SetFloat("_Smoothness", 0.25f);
        rockMat.enableInstancing = true;

        deadBarkMat = new Material(urpLit);
        deadBarkMat.name = "M_DeadBark";
        deadBarkMat.color = new Color(0.3f, 0.28f, 0.25f);
        deadBarkMat.SetFloat("_Smoothness", 0.1f);
        deadBarkMat.enableInstancing = true;

        sparseLeaveMat = new Material(urpLit);
        sparseLeaveMat.name = "M_SparseLeaves";
        sparseLeaveMat.color = new Color(0.18f, 0.28f, 0.2f, 0.85f);
        sparseLeaveMat.SetFloat("_Smoothness", 0.1f);
        sparseLeaveMat.enableInstancing = true;
    }

    #endregion

    #region Terrain

    private void CreateTerrain()
    {
        terrainData = new TerrainData();
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);

        float[,] heights = GenerateHeightmap();

        // Achatar áreas de spawn e boss
        FlattenArea(heights, 0.5f, 0.12f, 0.05f);  // Sul - spawn
        FlattenArea(heights, 0.5f, 0.88f, 0.06f);  // Norte - boss

        terrainData.SetHeights(0, 0, heights);

        CreateTerrainLayers();

        GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        terrainObj.name = "LiurniaTerrain";
        terrainObj.transform.position = new Vector3(-terrainSize * 0.5f, 0, -terrainSize * 0.5f);

        terrain = terrainObj.GetComponent<Terrain>();
        terrain.treeBillboardDistance = 250f;
        terrain.detailObjectDistance = 80f;
        terrain.heightmapPixelError = 5;
        terrain.materialTemplate = CreateTerrainMaterial();

        TerrainCollider terrCol = terrainObj.GetComponent<TerrainCollider>();
        terrCol.terrainData = terrainData;

        mapParent = new GameObject("=== LIURNIA MAP ===");
        mapParent.isStatic = true;
    }

    private float[,] GenerateHeightmap()
    {
        int res = heightmapResolution;
        float[,] heights = new float[res, res];

        float offsetX = seed * 1.3f;
        float offsetZ = seed * 2.7f;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / res;
                float nz = (float)z / res;

                // Base noise (suave, poucas colinas)
                float h = 0f;
                float amplitude = 1f;
                float frequency = 0.003f;

                for (int o = 0; o < 4; o++)
                {
                    float sx = (nx + offsetX) * frequency * res;
                    float sz = (nz + offsetZ) * frequency * res;
                    h += Mathf.PerlinNoise(sx, sz) * amplitude;
                    amplitude *= 0.4f;
                    frequency *= 2.2f;
                }
                h /= 1.6f;

                // === LAGO CENTRAL ===
                // Depressão radial do centro (centro-sul onde o lago fica)
                float cx = nx - 0.5f;
                float cz = nz - 0.45f; // Lago ligeiramente ao sul
                float distFromCenter = Mathf.Sqrt(cx * cx + cz * cz);

                // Zona do lago: depressão forte no centro
                float lakeRadius = 0.32f;
                if (distFromCenter < lakeRadius)
                {
                    float t = distFromCenter / lakeRadius;
                    float depression = (1f - t * t) * 0.15f;
                    h -= depression;
                }

                // === BORDAS ELEVADAS (cliffs) ===
                float edgeDist = Mathf.Min(nx, 1f - nx, nz, 1f - nz);
                if (edgeDist < 0.12f)
                {
                    float edgeElev = (0.12f - edgeDist) / 0.12f;
                    h += edgeElev * edgeElev * 0.2f;
                }
                // Falloff nas bordas extremas
                if (edgeDist < 0.05f)
                {
                    h *= edgeDist / 0.05f;
                }

                // === PLATÔ NORTE (boss area) ===
                float bossDistZ = Mathf.Abs(nz - 0.88f);
                float bossDistX = Mathf.Abs(nx - 0.5f);
                float bossDist = Mathf.Sqrt(bossDistX * bossDistX + bossDistZ * bossDistZ);
                if (bossDist < 0.1f)
                {
                    float t = bossDist / 0.1f;
                    h = Mathf.Lerp(0.22f, h, t * t);
                }

                // Normalizar e escalar
                h = Mathf.Clamp(h * 0.35f + 0.1f, 0.02f, 0.95f);
                heights[z, x] = h;
            }
        }

        return heights;
    }

    private void FlattenArea(float[,] heights, float cx, float cz, float radius)
    {
        int res = heights.GetLength(0);
        int centerX = (int)(cx * res), centerZ = (int)(cz * res);
        int rad = (int)(radius * res);

        float avgH = 0f; int count = 0;
        for (int z = centerZ - rad; z <= centerZ + rad; z++)
            for (int x = centerX - rad; x <= centerX + rad; x++)
                if (x >= 0 && x < res && z >= 0 && z < res) { avgH += heights[z, x]; count++; }
        if (count > 0) avgH /= count;

        for (int z = centerZ - rad * 2; z <= centerZ + rad * 2; z++)
            for (int x = centerX - rad * 2; x <= centerX + rad * 2; x++)
                if (x >= 0 && x < res && z >= 0 && z < res)
                {
                    float dist = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
                    float t = Mathf.Clamp01(dist / (rad * 2));
                    heights[z, x] = Mathf.Lerp(avgH, heights[z, x], t * t);
                }
    }

    private void CreateTerrainLayers()
    {
        Texture2D grassTex = ProceduralTextureGenerator.GenerateMoonlitGrassTexture();
        Texture2D grassNorm = ProceduralTextureGenerator.GenerateNormalMap(grassTex, 1.5f);

        Texture2D stoneTex = ProceduralTextureGenerator.GenerateAncientStoneTexture();
        Texture2D stoneNorm = ProceduralTextureGenerator.GenerateNormalMap(stoneTex, 2.5f);

        Texture2D sandTex = ProceduralTextureGenerator.GeneratePaleSandTexture();
        Texture2D sandNorm = ProceduralTextureGenerator.GenerateNormalMap(sandTex, 1.5f);

        Texture2D lakeTex = ProceduralTextureGenerator.GenerateLakeBedTexture();
        Texture2D lakeNorm = ProceduralTextureGenerator.GenerateNormalMap(lakeTex, 2f);

        TerrainLayer grassLayer = new TerrainLayer();
        grassLayer.diffuseTexture = grassTex;
        grassLayer.normalMapTexture = grassNorm;
        grassLayer.tileSize = new Vector2(12, 12);
        grassLayer.name = "MoonlitGrass";

        TerrainLayer stoneLayer = new TerrainLayer();
        stoneLayer.diffuseTexture = stoneTex;
        stoneLayer.normalMapTexture = stoneNorm;
        stoneLayer.tileSize = new Vector2(10, 10);
        stoneLayer.name = "AncientStone";

        TerrainLayer sandLayer = new TerrainLayer();
        sandLayer.diffuseTexture = sandTex;
        sandLayer.normalMapTexture = sandNorm;
        sandLayer.tileSize = new Vector2(8, 8);
        sandLayer.name = "PaleSand";

        TerrainLayer lakeLayer = new TerrainLayer();
        lakeLayer.diffuseTexture = lakeTex;
        lakeLayer.normalMapTexture = lakeNorm;
        lakeLayer.tileSize = new Vector2(10, 10);
        lakeLayer.name = "LakeBed";

        terrainData.terrainLayers = new TerrainLayer[] { grassLayer, stoneLayer, sandLayer, lakeLayer };
    }

    private Material CreateTerrainMaterial()
    {
        Shader ts = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (ts == null) ts = Shader.Find("Nature/Terrain/Standard");
        return new Material(ts);
    }

    #endregion

    #region Paint Terrain

    private void PaintTerrain()
    {
        int alphaRes = terrainData.alphamapResolution;
        float[,,] alphaMap = new float[alphaRes, alphaRes, 4];
        // 0=MoonlitGrass, 1=AncientStone, 2=PaleSand, 3=LakeBed

        float waterNorm = waterLevel / terrainHeight;

        for (int z = 0; z < alphaRes; z++)
        {
            for (int x = 0; x < alphaRes; x++)
            {
                float nx = (float)x / alphaRes;
                float nz = (float)z / alphaRes;

                float h = terrainData.GetHeight(
                    (int)(nx * terrainData.heightmapResolution),
                    (int)(nz * terrainData.heightmapResolution)) / terrainHeight;

                float steepness = terrainData.GetSteepness(nx, nz) / 90f;

                float grass = 1f, stone = 0f, sand = 0f, lakeBed = 0f;

                // Underwater areas = lake bed
                if (h < waterNorm + 0.02f)
                {
                    float depth = (waterNorm + 0.02f - h) / 0.08f;
                    lakeBed = Mathf.Clamp01(depth);
                    sand = Mathf.Clamp01(1f - depth) * 0.7f;
                    grass = Mathf.Max(0, 1f - lakeBed - sand);
                }

                // Shoreline = sand
                if (h >= waterNorm - 0.01f && h < waterNorm + 0.05f)
                {
                    sand = Mathf.Max(sand, 0.6f);
                    grass = Mathf.Max(0, 1f - sand - lakeBed);
                }

                // Steep slopes = stone
                if (steepness > 0.2f)
                {
                    float stoneBlend = (steepness - 0.2f) / 0.2f;
                    stone = Mathf.Clamp01(stoneBlend);
                    grass -= stone * 0.6f;
                }

                // High areas = more stone
                if (h > 0.3f)
                {
                    float hBlend = (h - 0.3f) / 0.15f;
                    stone = Mathf.Max(stone, hBlend * 0.5f);
                    grass -= hBlend * 0.3f;
                }

                // Noise variation
                float noiseS = Mathf.PerlinNoise(nx * 18 + seed, nz * 18) * 0.2f;
                stone += noiseS;
                grass -= noiseS * 0.5f;

                // Normalize
                grass = Mathf.Max(0, grass);
                stone = Mathf.Max(0, stone);
                sand = Mathf.Max(0, sand);
                lakeBed = Mathf.Max(0, lakeBed);
                float total = grass + stone + sand + lakeBed;
                if (total > 0)
                {
                    alphaMap[z, x, 0] = grass / total;
                    alphaMap[z, x, 1] = stone / total;
                    alphaMap[z, x, 2] = sand / total;
                    alphaMap[z, x, 3] = lakeBed / total;
                }
                else
                {
                    alphaMap[z, x, 0] = 1f;
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, alphaMap);
    }

    #endregion

    #region Trees

    private void PlaceTrees()
    {
        GameObject treesParent = new GameObject("Trees");
        treesParent.transform.SetParent(mapParent.transform);
        treesParent.isStatic = true;

        List<Vector3> placed = new List<Vector3>();
        int attempts = 0, maxAttempts = treeCount * 8;

        while (placed.Count < treeCount && attempts < maxAttempts)
        {
            attempts++;
            float nx = Random.Range(0.08f, 0.92f);
            float nz = Random.Range(0.08f, 0.92f);

            Vector3 wp = TerrainNormToWorld(nx, nz);
            wp.y = GetTerrainHeight(wp);

            // Não colocar em água profunda
            if (wp.y < waterLevel - 0.5f) continue;

            // Não muito íngreme
            float steep = terrainData.GetSteepness(nx, nz);
            if (steep > 25f) continue;

            // Distância mínima
            bool tooClose = false;
            foreach (var p in placed)
                if (Vector3.Distance(p, wp) < 8f) { tooClose = true; break; }
            if (tooClose) continue;

            // Criar árvore simples (morta/esparsa para Liurnia)
            GameObject tree = CreateSimpleTree(wp, Random.Range(0, 3));
            tree.transform.SetParent(treesParent.transform);
            tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), Random.Range(-3, 3));
            placed.Add(wp);
        }

        Debug.Log($"[Liurnia] {placed.Count} árvores colocadas.");
    }

    private GameObject CreateSimpleTree(Vector3 pos, int style)
    {
        GameObject tree = new GameObject("Tree_Liurnia");
        tree.transform.position = pos;
        tree.isStatic = true;

        float h = Random.Range(5f, 10f);
        float r = Random.Range(0.15f, 0.3f);

        // Tronco (cilindro simples — 1 objeto)
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localPosition = new Vector3(0, h * 0.5f, 0);
        trunk.transform.localScale = new Vector3(r * 2, h * 0.5f, r * 2);
        trunk.GetComponent<Renderer>().sharedMaterial = deadBarkMat;
        trunk.isStatic = true;

        if (style == 0) // Árvore morta - só tronco + galhos
        {
            for (int i = 0; i < 3; i++)
            {
                float angle = Random.Range(0, 360);
                float by = h * (0.5f + Random.value * 0.4f);
                float bLen = Random.Range(1.5f, 3.5f);
                Vector3 dir = Quaternion.Euler(Random.Range(10, 50), angle, 0) * Vector3.up;

                GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = $"Branch_{i}";
                branch.transform.SetParent(tree.transform);
                branch.transform.localPosition = new Vector3(0, by, 0) + dir * bLen * 0.5f;
                branch.transform.localScale = new Vector3(0.08f, bLen * 0.5f, 0.08f);
                branch.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90, 0, 0);
                branch.GetComponent<Renderer>().sharedMaterial = deadBarkMat;
                branch.isStatic = true;
            }
        }
        else if (style == 1) // Árvore com copa esparsa
        {
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(tree.transform);
            canopy.transform.localPosition = new Vector3(0, h * 0.85f, 0);
            float cs = Random.Range(2f, 4f);
            canopy.transform.localScale = new Vector3(cs, cs * 0.6f, cs);
            canopy.GetComponent<Renderer>().sharedMaterial = sparseLeaveMat;
            canopy.isStatic = true;
        }
        else // Árvore fina e alta (silver birch style)
        {
            // Copa estreita
            for (int i = 0; i < 2; i++)
            {
                float cy = h * (0.6f + i * 0.2f);
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.name = $"Canopy_{i}";
                leaf.transform.SetParent(tree.transform);
                leaf.transform.localPosition = new Vector3(Random.Range(-0.3f, 0.3f), cy, Random.Range(-0.3f, 0.3f));
                float ls = Random.Range(1.2f, 2.5f);
                leaf.transform.localScale = new Vector3(ls, ls * 0.5f, ls);
                leaf.GetComponent<Renderer>().sharedMaterial = sparseLeaveMat;
                leaf.isStatic = true;
            }
        }

        return tree;
    }

    #endregion

    #region Rocks

    private void PlaceRockFormations()
    {
        GameObject rocksParent = new GameObject("RockFormations");
        rocksParent.transform.SetParent(mapParent.transform);
        rocksParent.isStatic = true;

        for (int i = 0; i < rockFormationCount; i++)
        {
            float nx = Random.Range(0.06f, 0.94f);
            float nz = Random.Range(0.06f, 0.94f);

            Vector3 wp = TerrainNormToWorld(nx, nz);
            wp.y = GetTerrainHeight(wp);

            // Preferir bordas e áreas elevadas
            float edgeDist = Mathf.Min(nx, 1f - nx, nz, 1f - nz);
            if (edgeDist > 0.25f && Random.value > 0.3f) continue;

            if (Random.value < 0.4f)
                CreateBoulder(wp, rocksParent.transform);
            else
                CreateRockCluster(wp, rocksParent.transform);
        }
    }

    private void CreateBoulder(Vector3 pos, Transform parent)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Boulder";
        rock.transform.SetParent(parent);
        rock.transform.position = pos;
        float sx = Random.Range(1f, 4f);
        float sy = Random.Range(0.8f, 2.5f);
        float sz = Random.Range(1f, 4f);
        rock.transform.localScale = new Vector3(sx, sy, sz);
        rock.transform.rotation = Quaternion.Euler(Random.Range(-8, 8), Random.Range(0, 360), Random.Range(-8, 8));
        rock.GetComponent<Renderer>().sharedMaterial = rockMat;
        rock.isStatic = true;
    }

    private void CreateRockCluster(Vector3 pos, Transform parent)
    {
        GameObject cluster = new GameObject("RockCluster");
        cluster.transform.SetParent(parent);
        cluster.transform.position = pos;
        cluster.isStatic = true;

        int count = Random.Range(2, 5);
        for (int i = 0; i < count; i++)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = $"Rock_{i}";
            rock.transform.SetParent(cluster.transform);
            rock.transform.localPosition = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-0.2f, 0.3f), Random.Range(-1.5f, 1.5f));
            float s = Random.Range(0.5f, 1.8f);
            rock.transform.localScale = new Vector3(s, s * Random.Range(0.5f, 1f), s);
            rock.GetComponent<Renderer>().sharedMaterial = rockMat;
            rock.isStatic = true;
        }
    }

    #endregion

    #region Ruins

    private void PlaceRuins()
    {
        ProceduralRuinGenerator.InitMaterials();

        GameObject ruinsParent = new GameObject("Ruins");
        ruinsParent.transform.SetParent(mapParent.transform);
        ruinsParent.isStatic = true;

        List<Vector3> ruinPositions = new List<Vector3>();

        for (int i = 0; i < ruinCount; i++)
        {
            float nx = Random.Range(0.12f, 0.88f);
            float nz = Random.Range(0.15f, 0.82f);

            Vector3 wp = TerrainNormToWorld(nx, nz);
            wp.y = GetTerrainHeight(wp);

            // Algumas ruínas na água (parcialmente submersas)
            bool inWater = wp.y < waterLevel;
            if (inWater) wp.y = waterLevel - Random.Range(0.3f, 1.2f);

            // Distância mínima entre ruínas
            bool tooClose = false;
            foreach (var p in ruinPositions)
                if (Vector3.Distance(p, wp) < 15f) { tooClose = true; break; }
            if (tooClose) continue;

            int ruinSeed = seed + i * 137;
            GameObject ruin;

            float roll = Random.value;
            if (roll < 0.3f)
                ruin = ProceduralRuinGenerator.CreateColonnade(wp, ruinSeed);
            else if (roll < 0.55f)
                ruin = ProceduralRuinGenerator.CreateArch(wp, ruinSeed);
            else if (roll < 0.8f)
                ruin = ProceduralRuinGenerator.CreateRuinWall(wp, ruinSeed);
            else
                ruin = ProceduralRuinGenerator.CreateGlyphPedestal(wp, ruinSeed);

            ruin.transform.SetParent(ruinsParent.transform);
            ruinPositions.Add(wp);
        }

        RuinPositions = ruinPositions;
        Debug.Log($"[Liurnia] {ruinPositions.Count} ruínas colocadas.");
    }

    #endregion

    #region Bonfire & Spawns

    private void PlaceBonfire()
    {
        Vector3 spawnArea = TerrainNormToWorld(0.5f, 0.13f);
        spawnArea.y = GetTerrainHeight(spawnArea);

        // Garantir que não está na água
        if (spawnArea.y < waterLevel + 0.5f)
            spawnArea.y = waterLevel + 0.5f;

        BonfirePosition = spawnArea;
        PlayerSpawnPosition = spawnArea + new Vector3(3f, 0.5f, 0f);

        // Criar bonfire (mesmo sistema da floresta)
        GameObject bonfireObj = new GameObject("Bonfire");
        bonfireObj.transform.SetParent(mapParent.transform);
        bonfireObj.transform.position = BonfirePosition;

        // Base de pedra
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward * 0.7f;
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stone.name = $"BonfireStone_{i}";
            stone.transform.SetParent(bonfireObj.transform);
            stone.transform.localPosition = dir + Vector3.up * 0.1f;
            stone.transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
            stone.GetComponent<Renderer>().sharedMaterial = rockMat;
            stone.isStatic = true;
        }

        // Espada
        GameObject sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sword.name = "CoiledSword";
        sword.transform.SetParent(bonfireObj.transform);
        sword.transform.localPosition = Vector3.up * 0.7f;
        sword.transform.localScale = new Vector3(0.07f, 1.2f, 0.07f);
        sword.transform.localRotation = Quaternion.Euler(0, 0, 5f);
        Material swordMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        swordMat.color = new Color(0.5f, 0.5f, 0.55f);
        swordMat.SetFloat("_Smoothness", 0.7f);
        sword.GetComponent<Renderer>().material = swordMat;
        Collider col = sword.GetComponent<Collider>();
        if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }

        // Fogo
        GameObject fireObj = new GameObject("BonfireFire");
        fireObj.transform.SetParent(bonfireObj.transform);
        fireObj.transform.localPosition = Vector3.up * 0.3f;

        Light fireLight = fireObj.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(0.8f, 0.6f, 0.3f);
        fireLight.intensity = 3f;
        fireLight.range = 12f;
        fireLight.shadows = LightShadows.Soft;

        fireObj.AddComponent<TorchFlicker>();

        // Orbes de fogo
        for (int i = 0; i < 4; i++)
        {
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = $"FireOrb_{i}";
            orb.transform.SetParent(fireObj.transform);
            orb.transform.localPosition = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0f, 0.5f), Random.Range(-0.2f, 0.2f));
            orb.transform.localScale = Vector3.one * Random.Range(0.08f, 0.18f);
            Material fm = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            fm.color = new Color(1f, Random.Range(0.3f, 0.6f), 0.05f);
            fm.EnableKeyword("_EMISSION");
            fm.SetColor("_EmissionColor", fm.color * 5f);
            orb.GetComponent<Renderer>().material = fm;
            Collider oc = orb.GetComponent<Collider>();
            if (oc != null) { if (Application.isPlaying) Destroy(oc); else DestroyImmediate(oc); }
        }

        bonfireObj.AddComponent<Bonfire>();
    }

    private void CalculateSpawnPositions()
    {
        EnemySpawnPositions.Clear();

        Vector3 bossPos = TerrainNormToWorld(0.5f, 0.88f);
        bossPos.y = GetTerrainHeight(bossPos) + 1f;
        BossArenaCenter = bossPos;

        // Inimigos perto das ruínas
        foreach (var ruinPos in RuinPositions)
        {
            if (ruinPos.y >= waterLevel - 0.3f) // Não em água profunda
            {
                Vector3 enemyPos = ruinPos + new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
                enemyPos.y = GetTerrainHeight(enemyPos);
                if (enemyPos.y > waterLevel - 0.2f)
                    EnemySpawnPositions.Add(enemyPos);
            }
        }

        // Alguns extras na costa
        for (int i = 0; i < 4; i++)
        {
            float nx = Random.Range(0.2f, 0.8f);
            float nz = Random.Range(0.2f, 0.75f);
            Vector3 pos = TerrainNormToWorld(nx, nz);
            pos.y = GetTerrainHeight(pos);
            if (pos.y > waterLevel && pos.y < waterLevel + 5f)
                EnemySpawnPositions.Add(pos);
        }
    }

    #endregion

    #region Helpers

    private Vector3 TerrainNormToWorld(float nx, float nz)
    {
        if (terrain == null) return Vector3.zero;
        Vector3 tPos = terrain.transform.position;
        return new Vector3(tPos.x + nx * terrainData.size.x, 0, tPos.z + nz * terrainData.size.z);
    }

    private float GetTerrainHeight(Vector3 wp)
    {
        if (terrain == null) return 0;
        return terrain.SampleHeight(wp) + terrain.transform.position.y;
    }

    #endregion
}
