using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gera uma floresta sombria estilo Dark Souls com Terrain, árvores procedurais,
/// rochas, ruínas e iluminação atmosférica.
/// </summary>
public class ForestMapGenerator : MonoBehaviour
{
    [Header("Terreno")]
    public int terrainSize = 512;
    public int terrainHeight = 80;
    public int heightmapResolution = 513;
    public float baseFrequency = 0.005f;
    public int octaves = 5;
    public float persistence = 0.45f;
    public float lacunarity = 2.2f;

    [Header("Trilha / Caminho")]
    public float pathWidth = 6f;
    public float pathSmoothing = 0.02f;

    [Header("Árvores")]
    public int treeCount = 600;
    public float treeMinDistance = 3f;
    public float treeLineMax = 0.85f;   // não colocar no topo das montanhas
    public float treeLineMin = 0.08f;   // não colocar no fundo dos vales

    [Header("Rochas")]
    public int rockCount = 120;
    public int ruinCount = 8;

    [Header("Iluminação")]
    public int forestLightCount = 20;

    [Header("Seed")]
    public int seed = 0;
    public bool useRandomSeed = true;

    // Materiais (criados automaticamente)
    private Material trunkMaterial;
    private Material leavesMaterial;
    private Material darkLeavesMaterial;
    private Material deadLeavesMaterial;
    private Material rockMaterial;
    private Material groundMaterial;
    private Material ruinMaterial;
    private Material mossyRockMaterial;

    // Referências
    private Terrain terrain;
    private TerrainData terrainData;
    private GameObject mapParent;

    // Pontos de interesse
    public Vector3 PlayerSpawnPosition { get; private set; }
    public Vector3 BonfirePosition { get; private set; }
    public Vector3 BossArenaCenter { get; private set; }
    public List<Vector3> EnemySpawnPositions { get; private set; } = new List<Vector3>();
    public List<Vector3> PathPoints { get; private set; } = new List<Vector3>();

    [HideInInspector]
    public bool generatedInEditor = false;

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
        // Não regenerar se já foi gerado pelo editor
        if (!generatedInEditor)
        {
            GenerateForest();
        }
    }

    public void GenerateForest()
    {
        ClearMap();
        CreateMaterials();
        CreateTerrain();
        PaintTerrain();
        GeneratePath();
        PlaceTrees();
        PlaceRocks();
        PlaceRuins();
        PlaceForestLights();
        PlaceBonfire();
        CalculateSpawnPositions();

        Debug.Log($"[ForestGenerator] Floresta gerada! Seed: {seed}, " +
                  $"Árvores: {treeCount}, Rochas: {rockCount}");
    }

    public void ClearMap()
    {
        if (mapParent != null)
        {
            if (Application.isPlaying) Destroy(mapParent);
            else DestroyImmediate(mapParent);
        }

        // Limpar terreno existente
        Terrain existingTerrain = FindFirstObjectByType<Terrain>();
        if (existingTerrain != null)
        {
            if (Application.isPlaying) Destroy(existingTerrain.gameObject);
            else DestroyImmediate(existingTerrain.gameObject);
        }

        EnemySpawnPositions.Clear();
        PathPoints.Clear();
    }

    #region Materiais

    private void CreateMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // Tronco
        trunkMaterial = new Material(urpLit);
        trunkMaterial.name = "M_Trunk";
        trunkMaterial.color = new Color(0.22f, 0.15f, 0.08f);
        trunkMaterial.SetFloat("_Smoothness", 0.15f);

        // Folhas verdes escuras
        leavesMaterial = new Material(urpLit);
        leavesMaterial.name = "M_Leaves_Green";
        leavesMaterial.color = new Color(0.08f, 0.22f, 0.06f);
        leavesMaterial.SetFloat("_Smoothness", 0.1f);

        // Folhas muito escuras (profundidade)
        darkLeavesMaterial = new Material(urpLit);
        darkLeavesMaterial.name = "M_Leaves_Dark";
        darkLeavesMaterial.color = new Color(0.04f, 0.12f, 0.03f);
        darkLeavesMaterial.SetFloat("_Smoothness", 0.08f);

        // Folhas secas/mortas
        deadLeavesMaterial = new Material(urpLit);
        deadLeavesMaterial.name = "M_Leaves_Dead";
        deadLeavesMaterial.color = new Color(0.18f, 0.12f, 0.06f);
        deadLeavesMaterial.SetFloat("_Smoothness", 0.05f);

        // Rocha
        rockMaterial = new Material(urpLit);
        rockMaterial.name = "M_Rock";
        rockMaterial.color = new Color(0.3f, 0.28f, 0.25f);
        rockMaterial.SetFloat("_Smoothness", 0.2f);

        // Rocha com musgo
        mossyRockMaterial = new Material(urpLit);
        mossyRockMaterial.name = "M_MossyRock";
        mossyRockMaterial.color = new Color(0.18f, 0.25f, 0.12f);
        mossyRockMaterial.SetFloat("_Smoothness", 0.15f);

        // Ruína
        ruinMaterial = new Material(urpLit);
        ruinMaterial.name = "M_Ruin";
        ruinMaterial.color = new Color(0.35f, 0.32f, 0.28f);
        ruinMaterial.SetFloat("_Smoothness", 0.25f);

        // Chão (para referência)
        groundMaterial = new Material(urpLit);
        groundMaterial.name = "M_Ground";
        groundMaterial.color = new Color(0.12f, 0.1f, 0.06f);
        groundMaterial.SetFloat("_Smoothness", 0.05f);
    }

    #endregion

    #region Terreno

    private void CreateTerrain()
    {
        terrainData = new TerrainData();
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);

        // Gerar heightmap com Perlin noise multi-octave
        float[,] heights = GenerateHeightmap();

        // Achatar áreas para o player spawn e boss arena
        FlattenArea(heights, 0.5f, 0.1f, 0.06f);   // Centro — spawn
        FlattenArea(heights, 0.5f, 0.85f, 0.08f);   // Norte — boss arena
        FlattenArea(heights, 0.3f, 0.5f, 0.04f);    // Ponto intermediário

        terrainData.SetHeights(0, 0, heights);

        // Criar terrain layers (texturas do chão)
        CreateTerrainLayers();

        // Instanciar objeto
        GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        terrainObj.name = "ForestTerrain";
        terrainObj.transform.position = new Vector3(-terrainSize * 0.5f, 0, -terrainSize * 0.5f);

        terrain = terrainObj.GetComponent<Terrain>();
        terrain.treeBillboardDistance = 200f;
        terrain.detailObjectDistance = 100f;
        terrain.heightmapPixelError = 5;
        terrain.materialTemplate = CreateTerrainMaterial();

        // Collider
        TerrainCollider terrCol = terrainObj.GetComponent<TerrainCollider>();
        terrCol.terrainData = terrainData;

        mapParent = new GameObject("=== FOREST MAP ===");
        mapParent.isStatic = true;
    }

    private float[,] GenerateHeightmap()
    {
        int res = heightmapResolution;
        float[,] heights = new float[res, res];

        float offsetX = seed * 1.7f;
        float offsetZ = seed * 3.1f;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / res;
                float nz = (float)z / res;

                float height = 0f;
                float amplitude = 1f;
                float frequency = baseFrequency;

                for (int o = 0; o < octaves; o++)
                {
                    float sampleX = (nx + offsetX) * frequency * res;
                    float sampleZ = (nz + offsetZ) * frequency * res;
                    height += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Normalizar para 0..1
                height /= (1f - Mathf.Pow(persistence, octaves)) / (1f - persistence);

                // Bordas caem suavemente (para evitar cortes bruscos)
                float edgeFalloff = 1f;
                float edgeDist = Mathf.Min(nx, 1f - nx, nz, 1f - nz);
                if (edgeDist < 0.1f)
                    edgeFalloff = edgeDist / 0.1f;

                heights[z, x] = height * edgeFalloff * 0.3f + 0.05f;
            }
        }

        return heights;
    }

    private void FlattenArea(float[,] heights, float cx, float cz, float radius)
    {
        int res = heights.GetLength(0);
        int centerX = (int)(cx * res);
        int centerZ = (int)(cz * res);
        int rad = (int)(radius * res);

        // Calcular altura média na região
        float avgHeight = 0f;
        int count = 0;
        for (int z = centerZ - rad; z <= centerZ + rad; z++)
        {
            for (int x = centerX - rad; x <= centerX + rad; x++)
            {
                if (x >= 0 && x < res && z >= 0 && z < res)
                {
                    avgHeight += heights[z, x];
                    count++;
                }
            }
        }
        if (count > 0) avgHeight /= count;

        // Achatar com falloff suave
        for (int z = centerZ - rad * 2; z <= centerZ + rad * 2; z++)
        {
            for (int x = centerX - rad * 2; x <= centerX + rad * 2; x++)
            {
                if (x >= 0 && x < res && z >= 0 && z < res)
                {
                    float dist = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
                    float t = Mathf.Clamp01(dist / (rad * 2));
                    t = t * t; // quadrática = transição suave
                    heights[z, x] = Mathf.Lerp(avgHeight, heights[z, x], t);
                }
            }
        }
    }

    private void CreateTerrainLayers()
    {
        // Usar texturas procedurais de alta qualidade (1024x1024)
        Texture2D grassTex = ProceduralTextureGenerator.GenerateGrassTexture();
        Texture2D grassNormal = ProceduralTextureGenerator.GenerateNormalMap(grassTex, 2f);

        Texture2D dirtTex = ProceduralTextureGenerator.GenerateDirtTexture();
        Texture2D dirtNormal = ProceduralTextureGenerator.GenerateNormalMap(dirtTex, 2.5f);

        Texture2D rockTex = ProceduralTextureGenerator.GenerateRockTexture();
        Texture2D rockNormal = ProceduralTextureGenerator.GenerateNormalMap(rockTex, 3f);

        Texture2D mossTex = ProceduralTextureGenerator.GenerateForestFloorTexture();
        Texture2D mossNormal = ProceduralTextureGenerator.GenerateNormalMap(mossTex, 1.5f);

        TerrainLayer grassLayer = new TerrainLayer();
        grassLayer.diffuseTexture = grassTex;
        grassLayer.normalMapTexture = grassNormal;
        grassLayer.tileSize = new Vector2(10, 10);
        grassLayer.name = "Grass";

        TerrainLayer dirtLayer = new TerrainLayer();
        dirtLayer.diffuseTexture = dirtTex;
        dirtLayer.normalMapTexture = dirtNormal;
        dirtLayer.tileSize = new Vector2(8, 8);
        dirtLayer.name = "Dirt";

        TerrainLayer rockLayer = new TerrainLayer();
        rockLayer.diffuseTexture = rockTex;
        rockLayer.normalMapTexture = rockNormal;
        rockLayer.tileSize = new Vector2(12, 12);
        rockLayer.name = "Rock";

        TerrainLayer mossLayer = new TerrainLayer();
        mossLayer.diffuseTexture = mossTex;
        mossLayer.normalMapTexture = mossNormal;
        mossLayer.tileSize = new Vector2(6, 6);
        mossLayer.name = "Moss";

        terrainData.terrainLayers = new TerrainLayer[] { grassLayer, dirtLayer, rockLayer, mossLayer };
    }

    private Material CreateTerrainMaterial()
    {
        // Usar URP terrain shader
        Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (terrainShader == null)
            terrainShader = Shader.Find("Nature/Terrain/Standard");

        Material mat = new Material(terrainShader);
        return mat;
    }

    #endregion

    #region Pintura do Terreno

    private void PaintTerrain()
    {
        int alphaRes = terrainData.alphamapResolution;
        float[,,] alphaMap = new float[alphaRes, alphaRes, 4]; // 4 layers

        for (int z = 0; z < alphaRes; z++)
        {
            for (int x = 0; x < alphaRes; x++)
            {
                float nx = (float)x / alphaRes;
                float nz = (float)z / alphaRes;

                // Pegar hauteur normalizada nesse ponto
                float h = terrainData.GetHeight(
                    (int)(nx * terrainData.heightmapResolution),
                    (int)(nz * terrainData.heightmapResolution)) / terrainHeight;

                // Pegar steepness (inclinação)
                float steepness = terrainData.GetSteepness(nx, nz) / 90f;

                // Calcular pesos das texturas
                float grass = 1f;
                float dirt = 0f;
                float rock = 0f;
                float moss = 0f;

                // Partes íngremes = rocha
                if (steepness > 0.4f)
                {
                    rock = (steepness - 0.4f) / 0.3f;
                    grass -= rock;
                }

                // Altitudes altas = mais rocha
                if (h > 0.25f)
                {
                    float rockBlend = (h - 0.25f) / 0.15f;
                    rock = Mathf.Max(rock, rockBlend);
                    grass = Mathf.Max(0, grass - rockBlend * 0.7f);
                }

                // Baixadas = dirt + moss
                if (h < 0.1f)
                {
                    dirt = (0.1f - h) / 0.08f;
                    moss = dirt * 0.5f;
                    grass -= (dirt + moss) * 0.8f;
                }

                // Noise para variação natural
                float noiseMoss = Mathf.PerlinNoise(nx * 30 + seed, nz * 30) * 0.3f;
                moss += noiseMoss;
                grass -= noiseMoss * 0.5f;

                float noiseDirt = Mathf.PerlinNoise(nx * 20 + seed * 2, nz * 20) * 0.2f;
                dirt += noiseDirt;
                grass -= noiseDirt * 0.5f;

                // Normalizar
                grass = Mathf.Max(0, grass);
                float total = grass + dirt + rock + moss;
                if (total > 0)
                {
                    alphaMap[z, x, 0] = grass / total;
                    alphaMap[z, x, 1] = dirt / total;
                    alphaMap[z, x, 2] = rock / total;
                    alphaMap[z, x, 3] = moss / total;
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

    #region Caminho/Trilha

    private void GeneratePath()
    {
        PathPoints.Clear();

        // Criar um caminho sinuoso do spawn ao boss arena
        float startX = 0.5f;
        float startZ = 0.1f;
        float endX = 0.5f;
        float endZ = 0.85f;

        int steps = 40;
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            float x = Mathf.Lerp(startX, endX, t);
            float z = Mathf.Lerp(startZ, endZ, t);

            // Adicionar curvas
            x += Mathf.Sin(t * Mathf.PI * 3) * 0.08f;
            x += Mathf.PerlinNoise(t * 5 + seed, 0) * 0.05f;

            Vector3 worldPos = TerrainNormToWorld(x, z);
            worldPos.y = GetTerrainHeight(worldPos);
            PathPoints.Add(worldPos);
        }

        // "Pintar" o caminho no terreno (dirt layer)
        PaintPath();
    }

    private void PaintPath()
    {
        if (PathPoints.Count == 0 || terrainData == null) return;

        int alphaRes = terrainData.alphamapResolution;
        float[,,] alphaMap = terrainData.GetAlphamaps(0, 0, alphaRes, alphaRes);

        foreach (var point in PathPoints)
        {
            Vector3 terrainLocal = point - terrain.transform.position;
            float normX = terrainLocal.x / terrainData.size.x;
            float normZ = terrainLocal.z / terrainData.size.z;

            int cx = (int)(normX * alphaRes);
            int cz = (int)(normZ * alphaRes);
            int radius = (int)(pathWidth * alphaRes / terrainSize);

            for (int z = cz - radius; z <= cz + radius; z++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x >= 0 && x < alphaRes && z >= 0 && z < alphaRes)
                    {
                        float dist = Vector2.Distance(new Vector2(x, z), new Vector2(cx, cz));
                        if (dist <= radius)
                        {
                            float blend = 1f - (dist / radius);
                            blend = blend * blend; // Quadrático para borda suave

                            // Misturar com dirt
                            alphaMap[z, x, 0] = Mathf.Lerp(alphaMap[z, x, 0], 0.1f, blend * 0.7f);
                            alphaMap[z, x, 1] = Mathf.Lerp(alphaMap[z, x, 1], 0.8f, blend * 0.7f);
                            alphaMap[z, x, 2] = Mathf.Lerp(alphaMap[z, x, 2], 0f, blend * 0.5f);
                            alphaMap[z, x, 3] = Mathf.Lerp(alphaMap[z, x, 3], 0.1f, blend * 0.3f);
                        }
                    }
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, alphaMap);
    }

    #endregion

    #region Árvores

    private void PlaceTrees()
    {
        GameObject treesParent = new GameObject("Trees");
        treesParent.transform.SetParent(mapParent.transform);
        treesParent.isStatic = true;

        // Inicializar materiais HQ
        HighQualityTreeGenerator.InitMaterials();

        List<Vector3> placedPositions = new List<Vector3>();
        int attempts = 0;
        int maxAttempts = treeCount * 5;

        while (placedPositions.Count < treeCount && attempts < maxAttempts)
        {
            attempts++;

            // Posição aleatória no terreno
            float nx = Random.Range(0.05f, 0.95f);
            float nz = Random.Range(0.05f, 0.95f);

            Vector3 worldPos = TerrainNormToWorld(nx, nz);
            worldPos.y = GetTerrainHeight(worldPos);

            // Verificar se não está muito alto ou muito baixo
            float normalizedHeight = worldPos.y / terrainHeight;
            if (normalizedHeight > treeLineMax || normalizedHeight < treeLineMin)
                continue;

            // Verificar inclinação
            float steepness = terrainData.GetSteepness(nx, nz);
            if (steepness > 30f) continue;

            // Verificar distância mínima de outras árvores
            bool tooClose = false;
            foreach (var pos in placedPositions)
            {
                if (Vector3.Distance(pos, worldPos) < treeMinDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Verificar distância do caminho (menos árvores perto do caminho)
            bool nearPath = false;
            foreach (var pathPt in PathPoints)
            {
                if (Vector2.Distance(
                    new Vector2(worldPos.x, worldPos.z),
                    new Vector2(pathPt.x, pathPt.z)) < pathWidth * 1.5f)
                {
                    nearPath = true;
                    break;
                }
            }
            if (nearPath && Random.value > 0.15f) continue;

            // Escolher estilo de árvore (0-4)
            float styleRoll = Random.value;
            int style;
            if (styleRoll < 0.35f) style = 0;       // Oak
            else if (styleRoll < 0.55f) style = 1;  // Pine
            else if (styleRoll < 0.7f) style = 2;   // TallPine
            else if (styleRoll < 0.85f) style = 4;  // Willow
            else style = 3;                          // DeadTree

            // Criar árvore com mesh real HQ
            int treeSeed = Random.Range(0, 99999);
            GameObject tree = HighQualityTreeGenerator.CreateTree(worldPos, style, treeSeed);
            tree.transform.SetParent(treesParent.transform);

            // Rotação aleatória
            tree.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            placedPositions.Add(worldPos);
        }

        Debug.Log($"[Forest] {placedPositions.Count} árvores HQ colocadas.");
    }

    #endregion

    #region Rochas

    private void PlaceRocks()
    {
        GameObject rocksParent = new GameObject("Rocks");
        rocksParent.transform.SetParent(mapParent.transform);
        rocksParent.isStatic = true;

        for (int i = 0; i < rockCount; i++)
        {
            float nx = Random.Range(0.05f, 0.95f);
            float nz = Random.Range(0.05f, 0.95f);

            Vector3 worldPos = TerrainNormToWorld(nx, nz);
            worldPos.y = GetTerrainHeight(worldPos);

            // Escolher tipo de rocha
            if (Random.value < 0.3f)
            {
                CreateBoulder(worldPos, rocksParent.transform);
            }
            else if (Random.value < 0.6f)
            {
                CreateRockCluster(worldPos, rocksParent.transform);
            }
            else
            {
                CreateFlatRock(worldPos, rocksParent.transform);
            }
        }
    }

    private void CreateBoulder(Vector3 pos, Transform parent)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Boulder";
        rock.transform.SetParent(parent);
        rock.transform.position = pos;

        float scaleX = Random.Range(0.8f, 3f);
        float scaleY = Random.Range(0.6f, 2f);
        float scaleZ = Random.Range(0.8f, 3f);
        rock.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        rock.transform.rotation = Quaternion.Euler(
            Random.Range(-10, 10), Random.Range(0, 360), Random.Range(-10, 10));

        Material mat = Random.value < 0.4f ? mossyRockMaterial : rockMaterial;
        rock.GetComponent<Renderer>().sharedMaterial = mat;
        rock.isStatic = true;
    }

    private void CreateRockCluster(Vector3 pos, Transform parent)
    {
        GameObject cluster = new GameObject("RockCluster");
        cluster.transform.SetParent(parent);
        cluster.transform.position = pos;
        cluster.isStatic = true;

        int count = Random.Range(3, 6);
        for (int i = 0; i < count; i++)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = $"Rock_{i}";
            rock.transform.SetParent(cluster.transform);
            rock.transform.localPosition = new Vector3(
                Random.Range(-1f, 1f), Random.Range(-0.2f, 0.3f), Random.Range(-1f, 1f));

            float s = Random.Range(0.3f, 1.2f);
            rock.transform.localScale = new Vector3(s, s * Random.Range(0.5f, 1f), s);
            rock.transform.rotation = Quaternion.Euler(
                Random.Range(0, 30), Random.Range(0, 360), Random.Range(0, 20));

            rock.GetComponent<Renderer>().sharedMaterial = Random.value < 0.3f ? mossyRockMaterial : rockMaterial;
            rock.isStatic = true;
        }
    }

    private void CreateFlatRock(Vector3 pos, Transform parent)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rock.name = "FlatRock";
        rock.transform.SetParent(parent);
        rock.transform.position = pos + Vector3.up * 0.1f;

        float w = Random.Range(1f, 3f);
        float h = Random.Range(0.2f, 0.5f);
        float d = Random.Range(1f, 3f);
        rock.transform.localScale = new Vector3(w, h, d);
        rock.transform.rotation = Quaternion.Euler(
            Random.Range(-5, 5), Random.Range(0, 360), Random.Range(-5, 5));

        rock.GetComponent<Renderer>().sharedMaterial = mossyRockMaterial;
        rock.isStatic = true;
    }

    #endregion

    #region Ruínas

    private void PlaceRuins()
    {
        GameObject ruinsParent = new GameObject("Ruins");
        ruinsParent.transform.SetParent(mapParent.transform);
        ruinsParent.isStatic = true;

        for (int i = 0; i < ruinCount; i++)
        {
            float nx = Random.Range(0.1f, 0.9f);
            float nz = Random.Range(0.15f, 0.8f);
            Vector3 pos = TerrainNormToWorld(nx, nz);
            pos.y = GetTerrainHeight(pos);

            if (Random.value < 0.5f)
                CreateRuinArch(pos, ruinsParent.transform, i);
            else
                CreateRuinWall(pos, ruinsParent.transform, i);
        }
    }

    private void CreateRuinArch(Vector3 pos, Transform parent, int index)
    {
        GameObject archObj = new GameObject($"RuinArch_{index}");
        archObj.transform.SetParent(parent);
        archObj.transform.position = pos;
        archObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        archObj.isStatic = true;

        float h = Random.Range(3f, 5f);
        float w = Random.Range(3f, 5f);

        // Pilar esquerdo
        GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.name = "PillarLeft";
        left.transform.SetParent(archObj.transform);
        left.transform.localPosition = new Vector3(-w / 2, h / 2, 0);
        left.transform.localScale = new Vector3(0.6f, h, 0.6f);
        left.GetComponent<Renderer>().sharedMaterial = ruinMaterial;
        left.isStatic = true;

        // Pilar direito
        GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
        right.name = "PillarRight";
        right.transform.SetParent(archObj.transform);
        right.transform.localPosition = new Vector3(w / 2, h / 2, 0);
        right.transform.localScale = new Vector3(0.6f, h, 0.6f);
        right.GetComponent<Renderer>().sharedMaterial = ruinMaterial;
        right.isStatic = true;

        // Viga horizontal (parcialmente quebrada)
        bool broken = Random.value < 0.4f;
        if (!broken)
        {
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "Beam";
            beam.transform.SetParent(archObj.transform);
            beam.transform.localPosition = new Vector3(0, h, 0);
            beam.transform.localScale = new Vector3(w + 0.6f, 0.4f, 0.5f);
            beam.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-3, 3));
            beam.GetComponent<Renderer>().sharedMaterial = ruinMaterial;
            beam.isStatic = true;
        }
    }

    private void CreateRuinWall(Vector3 pos, Transform parent, int index)
    {
        GameObject wallObj = new GameObject($"RuinWall_{index}");
        wallObj.transform.SetParent(parent);
        wallObj.transform.position = pos;
        wallObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), Random.Range(-5, 5));
        wallObj.isStatic = true;

        float w = Random.Range(3f, 8f);
        float h = Random.Range(2f, 5f);

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(wallObj.transform);
        wall.transform.localPosition = new Vector3(0, h / 2, 0);
        wall.transform.localScale = new Vector3(w, h, 0.5f);
        wall.GetComponent<Renderer>().sharedMaterial = ruinMaterial;
        wall.isStatic = true;

        // Blocos caídos ao lado
        int debrisCount = Random.Range(1, 4);
        for (int d = 0; d < debrisCount; d++)
        {
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.name = $"Debris_{d}";
            debris.transform.SetParent(wallObj.transform);
            debris.transform.localPosition = new Vector3(
                Random.Range(-w / 2 - 1, w / 2 + 1), Random.Range(0.1f, 0.5f), Random.Range(-2, 2));
            float ds = Random.Range(0.3f, 1f);
            debris.transform.localScale = new Vector3(ds, ds, ds);
            debris.transform.localRotation = Quaternion.Euler(
                Random.Range(0, 30), Random.Range(0, 90), Random.Range(0, 30));
            debris.GetComponent<Renderer>().sharedMaterial = ruinMaterial;
            debris.isStatic = true;
        }
    }

    #endregion

    #region Iluminação da Floresta

    private void PlaceForestLights()
    {
        GameObject lightsParent = new GameObject("ForestLights");
        lightsParent.transform.SetParent(mapParent.transform);

        // Luzes em pontos do caminho
        for (int i = 0; i < PathPoints.Count; i += PathPoints.Count / forestLightCount)
        {
            Vector3 pos = PathPoints[Mathf.Min(i, PathPoints.Count - 1)];

            // Tocha/lanterna no chão do caminho
            GameObject lightObj = new GameObject($"PathLight_{i}");
            lightObj.transform.SetParent(lightsParent.transform);
            lightObj.transform.position = pos + Vector3.up * 2.5f;

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.65f, 0.25f);
            light.intensity = 1.8f;
            light.range = 12f;
            light.shadows = LightShadows.Soft;

            // Base visual (poste de pedra)
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "LightPost";
            post.transform.SetParent(lightObj.transform);
            post.transform.localPosition = Vector3.down * 1.2f;
            post.transform.localScale = new Vector3(0.15f, 1.2f, 0.15f);
            post.GetComponent<Renderer>().sharedMaterial = rockMaterial;
            post.isStatic = true;
            DestroyCollider(post);

            // Chama
            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Flame";
            flame.transform.SetParent(lightObj.transform);
            flame.transform.localPosition = Vector3.zero;
            flame.transform.localScale = Vector3.one * 0.2f;
            Renderer flRend = flame.GetComponent<Renderer>();
            Material flameMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            flameMat.color = new Color(1f, 0.5f, 0.1f);
            flameMat.EnableKeyword("_EMISSION");
            flameMat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f) * 4f);
            flRend.material = flameMat;
            DestroyCollider(flame);

            lightObj.AddComponent<TorchFlicker>();
        }

        // Luzes ambiente difusas na floresta (luz suave filtrada pelas copas)
        for (int i = 0; i < 8; i++)
        {
            float nx = Random.Range(0.1f, 0.9f);
            float nz = Random.Range(0.1f, 0.9f);
            Vector3 pos = TerrainNormToWorld(nx, nz);
            pos.y = GetTerrainHeight(pos) + 15f;

            GameObject ambLight = new GameObject($"AmbientForestLight_{i}");
            ambLight.transform.SetParent(lightsParent.transform);
            ambLight.transform.position = pos;

            Light light = ambLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.15f, 0.2f, 0.1f); // Verde escuro
            light.intensity = 0.3f;
            light.range = 40f;
            light.shadows = LightShadows.None;
        }
    }

    #endregion

    #region Bonfire

    private void PlaceBonfire()
    {
        Vector3 spawnArea = TerrainNormToWorld(0.5f, 0.12f);
        spawnArea.y = GetTerrainHeight(spawnArea);
        BonfirePosition = spawnArea;

        PlayerSpawnPosition = spawnArea + new Vector3(3f, 0.5f, 0f);

        GameObject bonfireObj = new GameObject("Bonfire");
        bonfireObj.transform.SetParent(mapParent.transform);
        bonfireObj.transform.position = BonfirePosition;

        // Base circular de pedra
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward * 0.7f;
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stone.name = $"BonfireStone_{i}";
            stone.transform.SetParent(bonfireObj.transform);
            stone.transform.localPosition = dir + Vector3.up * 0.1f;
            stone.transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
            stone.GetComponent<Renderer>().sharedMaterial = rockMaterial;
            stone.isStatic = true;
        }

        // Espada coiled
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
        DestroyCollider(sword);

        // Fogo da fogueira
        GameObject fireObj = new GameObject("BonfireFire");
        fireObj.transform.SetParent(bonfireObj.transform);
        fireObj.transform.localPosition = Vector3.up * 0.3f;

        Light fireLight = fireObj.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f, 0.55f, 0.15f);
        fireLight.intensity = 4f;
        fireLight.range = 15f;
        fireLight.shadows = LightShadows.Soft;

        fireObj.AddComponent<TorchFlicker>();

        // Esferas emissivas de fogo
        for (int i = 0; i < 5; i++)
        {
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = $"FireOrb_{i}";
            orb.transform.SetParent(fireObj.transform);
            orb.transform.localPosition = new Vector3(
                Random.Range(-0.2f, 0.2f),
                Random.Range(0f, 0.6f),
                Random.Range(-0.2f, 0.2f));
            orb.transform.localScale = Vector3.one * Random.Range(0.08f, 0.2f);

            Material fireMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            fireMat.color = new Color(1f, Random.Range(0.3f, 0.6f), 0.05f);
            fireMat.EnableKeyword("_EMISSION");
            fireMat.SetColor("_EmissionColor", fireMat.color * 6f);
            orb.GetComponent<Renderer>().material = fireMat;
            DestroyCollider(orb);
        }

        bonfireObj.AddComponent<Bonfire>();
    }

    #endregion

    #region Spawn Positions

    private void CalculateSpawnPositions()
    {
        EnemySpawnPositions.Clear();

        // Boss arena
        Vector3 bossPos = TerrainNormToWorld(0.5f, 0.85f);
        bossPos.y = GetTerrainHeight(bossPos);
        BossArenaCenter = bossPos + Vector3.up;

        // Inimigos ao longo do caminho e nas laterais
        for (int i = 0; i < PathPoints.Count; i += 3)
        {
            if (Random.value < 0.3f)
            {
                Vector3 pathPt = PathPoints[i];
                // Offset lateral
                Vector3 offset = new Vector3(Random.Range(-8, 8), 0, Random.Range(-8, 8));
                Vector3 spawnPos = pathPt + offset;
                spawnPos.y = GetTerrainHeight(spawnPos) + 1f;
                EnemySpawnPositions.Add(spawnPos);
            }
        }

        // Alguns perto das ruínas
        for (int i = 0; i < 5; i++)
        {
            float nx = Random.Range(0.15f, 0.85f);
            float nz = Random.Range(0.2f, 0.75f);
            Vector3 pos = TerrainNormToWorld(nx, nz);
            pos.y = GetTerrainHeight(pos) + 1f;
            EnemySpawnPositions.Add(pos);
        }
    }

    #endregion

    #region Helpers

    private Vector3 TerrainNormToWorld(float nx, float nz)
    {
        if (terrain == null) return Vector3.zero;
        Vector3 tPos = terrain.transform.position;
        return new Vector3(
            tPos.x + nx * terrainData.size.x,
            0,
            tPos.z + nz * terrainData.size.z);
    }

    private float GetTerrainHeight(Vector3 worldPos)
    {
        if (terrain == null) return 0;
        return terrain.SampleHeight(worldPos);
    }

    private Texture2D GenerateNoiseTexture(int size, Color color1, Color color2)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        float offset = seed * 0.1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise = Mathf.PerlinNoise(
                    (float)x / size * 8f + offset,
                    (float)y / size * 8f + offset);

                // Adicionar detalhes finos
                noise += Mathf.PerlinNoise(
                    (float)x / size * 32f + offset,
                    (float)y / size * 32f + offset) * 0.3f;

                noise = Mathf.Clamp01(noise);
                Color c = Color.Lerp(color1, color2, noise);
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    private static void DestroyCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        // Caminho
        if (PathPoints != null && PathPoints.Count > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < PathPoints.Count - 1; i++)
                Gizmos.DrawLine(PathPoints[i], PathPoints[i + 1]);
        }

        // Spawn enemy
        Gizmos.color = Color.red;
        if (EnemySpawnPositions != null)
            foreach (var pos in EnemySpawnPositions)
                Gizmos.DrawSphere(pos, 0.5f);

        // Player spawn
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(PlayerSpawnPosition, 1f);

        // Boss
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(BossArenaCenter, 5f);
    }
}
