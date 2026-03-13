using UnityEngine;

/// <summary>
/// Gera texturas procedurais de alta qualidade para terreno, árvores e rochas.
/// Usa ruído Perlin multicamada, Worley noise simulado e blending avançado.
/// Texturas 1024x1024 com detalhes realistas.
/// </summary>
public static class ProceduralTextureGenerator
{
    private const int TEX_SIZE = 1024;

    // =================== TERRAIN TEXTURES ====================

    /// <summary>
    /// Grama densa com variações de verde e detalhes de folhas.
    /// </summary>
    public static Texture2D GenerateGrassTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_Grass_Procedural";
        Color baseGreen = new Color(0.18f, 0.32f, 0.08f);
        Color darkGreen = new Color(0.1f, 0.2f, 0.04f);
        Color lightGreen = new Color(0.25f, 0.42f, 0.12f);
        Color yellowGreen = new Color(0.3f, 0.35f, 0.1f);

        float offsetX = Random.Range(0f, 1000f);
        float offsetY = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE;
                float ny = (float)y / TEX_SIZE;

                // Multi-octave noise para variação geral
                float n1 = Mathf.PerlinNoise((nx + offsetX) * 8f, (ny + offsetY) * 8f);
                float n2 = Mathf.PerlinNoise((nx + offsetX) * 24f, (ny + offsetY) * 24f) * 0.3f;
                float n3 = Mathf.PerlinNoise((nx + offsetX) * 64f, (ny + offsetY) * 64f) * 0.15f;
                float noise = n1 + n2 + n3;

                // Detalhes finos (simulando folhas individuais)
                float bladeNoise = Mathf.PerlinNoise((nx + offsetX) * 200f, (ny + offsetY) * 200f);
                float bladeDetail = Mathf.PerlinNoise((nx + offsetX) * 400f, (ny + offsetY) * 400f) * 0.5f;

                Color col = Color.Lerp(darkGreen, lightGreen, noise);
                col = Color.Lerp(col, yellowGreen, Mathf.Clamp01(n2 * 2f - 0.2f) * 0.3f);
                col = Color.Lerp(col, baseGreen, bladeNoise * 0.3f);

                // Micro variação
                float microVar = (bladeDetail - 0.25f) * 0.08f;
                col.r += microVar;
                col.g += microVar * 1.5f;
                col.b += microVar * 0.5f;

                col.a = 1f;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    /// <summary>
    /// Terra/lama com detalhes de pedregulhos e rachaduras.
    /// </summary>
    public static Texture2D GenerateDirtTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_Dirt_Procedural";
        Color baseDirt = new Color(0.28f, 0.2f, 0.12f);
        Color darkDirt = new Color(0.15f, 0.1f, 0.06f);
        Color lightDirt = new Color(0.4f, 0.3f, 0.18f);
        Color pebble = new Color(0.35f, 0.32f, 0.28f);

        float offsetX = Random.Range(0f, 1000f);
        float offsetY = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE;
                float ny = (float)y / TEX_SIZE;

                float n1 = Mathf.PerlinNoise((nx + offsetX) * 6f, (ny + offsetY) * 6f);
                float n2 = Mathf.PerlinNoise((nx + offsetX) * 20f, (ny + offsetY) * 20f) * 0.4f;
                float n3 = Mathf.PerlinNoise((nx + offsetX) * 60f, (ny + offsetY) * 60f) * 0.2f;

                // Worley-like noise (pedregulhos)
                float worley = SimulateWorley(nx * 30f + offsetX, ny * 30f + offsetY);

                float noise = n1 + n2 + n3;
                Color col = Color.Lerp(darkDirt, lightDirt, noise);
                col = Color.Lerp(col, pebble, Mathf.Clamp01(worley * 0.4f));

                // Rachaduras
                float crack = Mathf.PerlinNoise((nx + offsetX) * 100f, (ny + offsetY) * 100f);
                if (crack > 0.75f)
                    col = Color.Lerp(col, darkDirt, (crack - 0.75f) * 3f);

                col.a = 1f;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    /// <summary>
    /// Rocha com veios, musgo e variação mineral.
    /// </summary>
    public static Texture2D GenerateRockTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_Rock_Procedural";
        Color baseRock = new Color(0.35f, 0.33f, 0.3f);
        Color darkRock = new Color(0.18f, 0.17f, 0.15f);
        Color lightRock = new Color(0.5f, 0.48f, 0.44f);
        Color moss = new Color(0.12f, 0.22f, 0.06f);

        float offsetX = Random.Range(0f, 1000f);
        float offsetY = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE;
                float ny = (float)y / TEX_SIZE;

                float n1 = Mathf.PerlinNoise((nx + offsetX) * 4f, (ny + offsetY) * 4f);
                float n2 = Mathf.PerlinNoise((nx + offsetX) * 12f, (ny + offsetY) * 12f) * 0.35f;
                float n3 = Mathf.PerlinNoise((nx + offsetX) * 40f, (ny + offsetY) * 40f) * 0.2f;
                float n4 = Mathf.PerlinNoise((nx + offsetX) * 100f, (ny + offsetY) * 100f) * 0.1f;

                float noise = n1 + n2 + n3 + n4;
                Color col = Color.Lerp(darkRock, lightRock, noise);

                // Veios minerais
                float vein = Mathf.PerlinNoise((nx + offsetX) * 50f, (ny + offsetY) * 8f);
                float vein2 = Mathf.PerlinNoise((nx + offsetX) * 8f, (ny + offsetY) * 50f);
                if (vein > 0.7f)
                    col = Color.Lerp(col, lightRock, (vein - 0.7f) * 2f);
                if (vein2 > 0.72f)
                    col = Color.Lerp(col, darkRock, (vein2 - 0.72f) * 2.5f);

                // Musgo (mais no topo)
                float mossNoise = Mathf.PerlinNoise((nx + offsetX) * 15f, (ny + offsetY) * 15f);
                float mossAmount = Mathf.Clamp01((ny - 0.5f) * 2f) * mossNoise;
                col = Color.Lerp(col, moss, mossAmount * 0.4f);

                col.a = 1f;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    /// <summary>
    /// Musgo/chão florestal com folhas decompostas.
    /// </summary>
    public static Texture2D GenerateForestFloorTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_ForestFloor_Procedural";
        Color baseMoss = new Color(0.12f, 0.18f, 0.05f);
        Color darkMoss = new Color(0.06f, 0.1f, 0.02f);
        Color leaves = new Color(0.22f, 0.15f, 0.05f);
        Color dryLeaf = new Color(0.35f, 0.2f, 0.08f);
        Color twig = new Color(0.25f, 0.18f, 0.1f);

        float offsetX = Random.Range(0f, 1000f);
        float offsetY = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE;
                float ny = (float)y / TEX_SIZE;

                float n1 = Mathf.PerlinNoise((nx + offsetX) * 5f, (ny + offsetY) * 5f);
                float n2 = Mathf.PerlinNoise((nx + offsetX) * 18f, (ny + offsetY) * 18f) * 0.35f;
                float n3 = Mathf.PerlinNoise((nx + offsetX) * 50f, (ny + offsetY) * 50f) * 0.15f;

                float noise = n1 + n2 + n3;
                Color col = Color.Lerp(darkMoss, baseMoss, noise);

                // Folhas espalhadas
                float leafNoise = Mathf.PerlinNoise((nx + offsetX) * 80f, (ny + offsetY) * 80f);
                if (leafNoise > 0.6f)
                {
                    float leafType = Mathf.PerlinNoise((nx + offsetX) * 120f, (ny + offsetY) * 120f);
                    col = Color.Lerp(col, leafType > 0.5f ? dryLeaf : leaves, (leafNoise - 0.6f) * 2f);
                }

                // Galhos 
                float twigNoise = Mathf.PerlinNoise((nx + offsetX) * 200f, (ny + offsetY) * 30f);
                if (twigNoise > 0.85f)
                    col = Color.Lerp(col, twig, (twigNoise - 0.85f) * 4f);

                col.a = 1f;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    // =================== NORMAL MAPS ====================

    /// <summary>
    /// Gera normal map a partir de uma textura de cor (derivada de height).
    /// </summary>
    public static Texture2D GenerateNormalMap(Texture2D source, float strength = 2f)
    {
        int w = source.width;
        int h = source.height;
        Texture2D normal = new Texture2D(w, h, TextureFormat.RGBA32, true);
        normal.name = source.name + "_Normal";

        Color[] pixels = source.GetPixels();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float left = GetGray(pixels, x - 1, y, w, h);
                float right = GetGray(pixels, x + 1, y, w, h);
                float down = GetGray(pixels, x, y - 1, w, h);
                float up = GetGray(pixels, x, y + 1, w, h);

                float dx = (left - right) * strength;
                float dy = (down - up) * strength;

                Vector3 n = new Vector3(dx, dy, 1f).normalized;

                // Encode to color
                normal.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f));
            }
        }

        normal.Apply(true);
        normal.filterMode = FilterMode.Bilinear;
        normal.wrapMode = TextureWrapMode.Repeat;
        return normal;
    }

    // =================== BARK / WOOD ====================

    /// <summary>
    /// Textura de casca de árvore com sulcos verticais.
    /// </summary>
    public static Texture2D GenerateBarkTexture()
    {
        Texture2D tex = new Texture2D(512, 512, TextureFormat.RGBA32, true);
        tex.name = "T_Bark_Procedural";
        Color darkBark = new Color(0.15f, 0.1f, 0.06f);
        Color midBark = new Color(0.25f, 0.18f, 0.1f);
        Color lightBark = new Color(0.35f, 0.25f, 0.15f);

        float offsetX = Random.Range(0f, 1000f);
        float offsetY = Random.Range(0f, 1000f);

        for (int y = 0; y < 512; y++)
        {
            for (int x = 0; x < 512; x++)
            {
                float nx = (float)x / 512f;
                float ny = (float)y / 512f;

                // Sulcos verticais
                float verticalGroove = Mathf.PerlinNoise((nx + offsetX) * 40f, (ny + offsetY) * 3f);
                float detail = Mathf.PerlinNoise((nx + offsetX) * 80f, (ny + offsetY) * 12f) * 0.3f;
                float micro = Mathf.PerlinNoise((nx + offsetX) * 150f, (ny + offsetY) * 25f) * 0.15f;

                float combined = verticalGroove + detail + micro;
                Color col = Color.Lerp(darkBark, lightBark, combined);
                col = Color.Lerp(col, midBark, Mathf.Abs(Mathf.Sin(nx * 30f + verticalGroove * 10f)) * 0.2f);

                col.a = 1f;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    /// <summary>
    /// Textura de folhagem (canopy).
    /// </summary>
    public static Texture2D GenerateLeafTexture()
    {
        Texture2D tex = new Texture2D(512, 512, TextureFormat.RGBA32, true);
        tex.name = "T_Leaves_Procedural";
        Color darkLeaf = new Color(0.05f, 0.15f, 0.02f);
        Color midLeaf = new Color(0.1f, 0.28f, 0.05f);
        Color lightLeaf = new Color(0.2f, 0.4f, 0.1f);
        Color yellowLeaf = new Color(0.3f, 0.35f, 0.05f);

        float offsetX = Random.Range(0f, 1000f);
        float offsetY = Random.Range(0f, 1000f);

        for (int y = 0; y < 512; y++)
        {
            for (int x = 0; x < 512; x++)
            {
                float nx = (float)x / 512f;
                float ny = (float)y / 512f;

                // Cluster de folhas
                float cluster = Mathf.PerlinNoise((nx + offsetX) * 10f, (ny + offsetY) * 10f);
                float leafShape = Mathf.PerlinNoise((nx + offsetX) * 40f, (ny + offsetY) * 40f);
                float vein = Mathf.PerlinNoise((nx + offsetX) * 100f, (ny + offsetY) * 60f) * 0.2f;

                float combined = cluster + leafShape * 0.3f + vein;
                Color col = Color.Lerp(darkLeaf, lightLeaf, combined);

                // Variação sazonal
                float seasonal = Mathf.PerlinNoise((nx + offsetX) * 5f, (ny + offsetY) * 5f);
                col = Color.Lerp(col, yellowLeaf, Mathf.Clamp01(seasonal - 0.7f) * 2f);

                // Transparência pras bordas das folhas
                float alpha = Mathf.Clamp01(leafShape * 1.5f + 0.3f);
                col.a = alpha;

                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    // =================== HELPERS ====================

    private static float SimulateWorley(float x, float y)
    {
        // Worley noise simplificado
        float minDist = 1f;
        int cellX = Mathf.FloorToInt(x);
        int cellY = Mathf.FloorToInt(y);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int cx = cellX + dx;
                int cy = cellY + dy;
                // Ponto pseudo-aleatório na célula
                float px = cx + Frac(Mathf.Sin(cx * 127.1f + cy * 311.7f) * 43758.5453f);
                float py = cy + Frac(Mathf.Sin(cx * 269.5f + cy * 183.3f) * 43758.5453f);
                float dist = (x - px) * (x - px) + (y - py) * (y - py);
                if (dist < minDist) minDist = dist;
            }
        }

        return Mathf.Sqrt(minDist);
    }

    private static float Frac(float x) => x - Mathf.Floor(x);

    private static float GetGray(Color[] pixels, int x, int y, int w, int h)
    {
        x = (x + w) % w;
        y = (y + h) % h;
        Color c = pixels[y * w + x];
        return c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
    }

    // =================== RDR2 STYLE TEXTURES ====================

    /// <summary>
    /// Terra seca com pedras — estilo fazenda Red Dead Redemption 2.
    /// Cor predominante marrom/bege com pedregulhos cinza.
    /// </summary>
    public static Texture2D GenerateDryDirtWithRocksTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_DryDirt_RDR2";
        Color baseDirt = new Color(0.42f, 0.34f, 0.22f);  // Marrom seco
        Color lightSand = new Color(0.55f, 0.48f, 0.35f);  // Areia clara
        Color darkDirt = new Color(0.25f, 0.18f, 0.1f);   // Terra escura
        Color rockGray = new Color(0.45f, 0.43f, 0.38f);  // Pedra
        Color rockDark = new Color(0.3f, 0.28f, 0.25f);

        float ox = Random.Range(0f, 1000f);
        float oy = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE;
                float ny = (float)y / TEX_SIZE;

                // Base: dirt with variation
                float n1 = Mathf.PerlinNoise((nx + ox) * 5f, (ny + oy) * 5f);
                float n2 = Mathf.PerlinNoise((nx + ox) * 15f, (ny + oy) * 15f) * 0.35f;
                float n3 = Mathf.PerlinNoise((nx + ox) * 40f, (ny + oy) * 40f) * 0.15f;
                float noise = n1 + n2 + n3;

                Color col = Color.Lerp(darkDirt, lightSand, noise);
                col = Color.Lerp(col, baseDirt, 0.4f);

                // Rocks embedded in dirt
                float worley = SimulateWorley(nx * 25f + ox, ny * 25f + oy);
                if (worley < 0.15f)
                {
                    float rockBlend = 1f - (worley / 0.15f);
                    col = Color.Lerp(col, rockGray, rockBlend * 0.7f);
                    // Rock surface detail
                    float rockDetail = Mathf.PerlinNoise((nx + ox) * 150f, (ny + oy) * 150f);
                    col = Color.Lerp(col, rockDark, rockDetail * rockBlend * 0.3f);
                }

                // Cracks in dry dirt
                float crack1 = Mathf.PerlinNoise((nx + ox) * 80f, (ny + oy) * 3f);
                float crack2 = Mathf.PerlinNoise((nx + ox) * 3f, (ny + oy) * 80f);
                if (crack1 > 0.78f || crack2 > 0.8f)
                    col = Color.Lerp(col, darkDirt, 0.4f);

                // Fine grain
                float grain = Mathf.PerlinNoise((nx + ox) * 300f, (ny + oy) * 300f) * 0.06f - 0.03f;
                col.r += grain; col.g += grain * 0.8f; col.b += grain * 0.5f;

                col.a = 1f;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    /// <summary>
    /// Grama seca/morta estilo fazenda — amarela/marrom.
    /// </summary>
    public static Texture2D GenerateDriedGrassTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_DriedGrass_RDR2";
        Color baseYellow = new Color(0.48f, 0.42f, 0.2f);
        Color dryBrown = new Color(0.35f, 0.28f, 0.12f);
        Color lightStraw = new Color(0.6f, 0.55f, 0.3f);
        Color greenHint = new Color(0.3f, 0.35f, 0.15f);

        float ox = Random.Range(0f, 1000f);
        float oy = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE;
                float ny = (float)y / TEX_SIZE;

                float n1 = Mathf.PerlinNoise((nx + ox) * 10f, (ny + oy) * 10f);
                float n2 = Mathf.PerlinNoise((nx + ox) * 30f, (ny + oy) * 30f) * 0.3f;

                // Blades direction feel
                float blade = Mathf.PerlinNoise((nx + ox) * 150f, (ny + oy) * 8f);
                float bladeV = Mathf.PerlinNoise((nx + ox) * 8f, (ny + oy) * 150f) * 0.5f;

                float noise = n1 + n2;
                Color col = Color.Lerp(dryBrown, lightStraw, noise);
                col = Color.Lerp(col, baseYellow, 0.3f);

                // Occasional green patches
                float greenPatch = Mathf.PerlinNoise((nx + ox) * 4f, (ny + oy) * 4f);
                if (greenPatch > 0.7f)
                    col = Color.Lerp(col, greenHint, (greenPatch - 0.7f) * 1.5f);

                // Blade detail
                float bladeDetail = (blade + bladeV) * 0.5f;
                col = Color.Lerp(col, dryBrown, bladeDetail * 0.15f);

                col.a = 1f;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    /// <summary>
    /// Cascalho / pedregulho — terreno rochoso estilo RDR2.
    /// </summary>
    public static Texture2D GenerateGravelTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_Gravel_RDR2";
        Color baseGravel = new Color(0.38f, 0.35f, 0.3f);
        Color lightGravel = new Color(0.52f, 0.48f, 0.42f);
        Color darkGravel = new Color(0.22f, 0.2f, 0.17f);
        Color dirtBetween = new Color(0.32f, 0.25f, 0.15f);

        float ox = Random.Range(0f, 1000f);
        float oy = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE;
                float ny = (float)y / TEX_SIZE;

                // Worley for pebble pattern
                float worley1 = SimulateWorley(nx * 40f + ox, ny * 40f + oy);
                float worley2 = SimulateWorley(nx * 20f + ox * 1.3f, ny * 20f + oy * 1.3f);

                Color col;
                if (worley1 < 0.08f)
                {
                    // Pebble surface
                    float pebbleDetail = Mathf.PerlinNoise((nx + ox) * 100f, (ny + oy) * 100f);
                    col = Color.Lerp(baseGravel, lightGravel, pebbleDetail);
                }
                else if (worley1 < 0.15f)
                {
                    // Pebble edge
                    col = Color.Lerp(darkGravel, baseGravel, (worley1 - 0.08f) / 0.07f);
                }
                else
                {
                    // Dirt between pebbles
                    float dirtNoise = Mathf.PerlinNoise((nx + ox) * 30f, (ny + oy) * 30f);
                    col = Color.Lerp(dirtBetween, darkGravel, dirtNoise * 0.3f + worley2 * 0.2f);
                }

                // Grain
                float grain = Mathf.PerlinNoise((nx + ox) * 250f, (ny + oy) * 250f) * 0.04f - 0.02f;
                col.r += grain; col.g += grain; col.b += grain;

                col.a = 1f;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    /// <summary>
    /// Sandy path — caminho de areia/poeira.
    /// </summary>
    public static Texture2D GenerateSandyPathTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_SandyPath_RDR2";
        Color baseSand = new Color(0.52f, 0.45f, 0.32f);
        Color lightSand = new Color(0.62f, 0.55f, 0.4f);
        Color darkSand = new Color(0.38f, 0.32f, 0.2f);

        float ox = Random.Range(0f, 1000f);
        float oy = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE;
                float ny = (float)y / TEX_SIZE;

                float n1 = Mathf.PerlinNoise((nx + ox) * 8f, (ny + oy) * 8f);
                float n2 = Mathf.PerlinNoise((nx + ox) * 25f, (ny + oy) * 25f) * 0.3f;
                float n3 = Mathf.PerlinNoise((nx + ox) * 80f, (ny + oy) * 80f) * 0.1f;

                float noise = n1 + n2 + n3;
                Color col = Color.Lerp(darkSand, lightSand, noise);
                col = Color.Lerp(col, baseSand, 0.3f);

                // Wheel tracks / footprints feel
                float track = Mathf.PerlinNoise((nx + ox) * 4f, (ny + oy) * 60f);
                if (track > 0.6f)
                    col = Color.Lerp(col, darkSand, (track - 0.6f) * 0.5f);

                // Fine particles
                float fine = Mathf.PerlinNoise((nx + ox) * 400f, (ny + oy) * 400f) * 0.03f - 0.015f;
                col.r += fine; col.g += fine * 0.9f; col.b += fine * 0.6f;

                col.a = 1f;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    // =================== LIURNIA TEXTURES ====================

    /// <summary>
    /// Grama pálida azul-esverdeada para Liurnia.
    /// </summary>
    public static Texture2D GenerateMoonlitGrassTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_MoonlitGrass";
        Color baseCol = new Color(0.25f, 0.35f, 0.22f);
        Color paleCol = new Color(0.35f, 0.42f, 0.32f);
        Color blueCol = new Color(0.2f, 0.3f, 0.35f);
        float ox = Random.Range(0f, 1000f), oy = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE, ny = (float)y / TEX_SIZE;
                float n1 = Mathf.PerlinNoise((nx + ox) * 10f, (ny + oy) * 10f);
                float n2 = Mathf.PerlinNoise((nx + ox) * 30f, (ny + oy) * 30f) * 0.3f;
                float n3 = Mathf.PerlinNoise((nx + ox) * 80f, (ny + oy) * 80f) * 0.15f;
                float n = n1 + n2 + n3;
                Color col = Color.Lerp(baseCol, paleCol, n);
                col = Color.Lerp(col, blueCol, Mathf.Clamp01(n2 * 2f) * 0.25f);
                float micro = Mathf.PerlinNoise((nx + ox) * 200f, (ny + oy) * 200f) * 0.06f;
                col.r += micro; col.g += micro; col.b += micro * 1.5f;
                col.a = 1f;
                tex.SetPixel(x, y, col);
            }

        tex.Apply(true); tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    /// <summary>
    /// Pedra antiga cinza-prateada para Liurnia.
    /// </summary>
    public static Texture2D GenerateAncientStoneTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_AncientStone";
        Color baseGrey = new Color(0.45f, 0.45f, 0.48f);
        Color darkGrey = new Color(0.3f, 0.3f, 0.33f);
        Color lightGrey = new Color(0.6f, 0.58f, 0.55f);
        float ox = Random.Range(0f, 1000f), oy = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE, ny = (float)y / TEX_SIZE;
                float n1 = Mathf.PerlinNoise((nx + ox) * 6f, (ny + oy) * 6f);
                float n2 = Mathf.PerlinNoise((nx + ox) * 25f, (ny + oy) * 25f) * 0.35f;
                float veins = Mathf.PerlinNoise((nx + ox) * 50f, (ny + oy) * 15f) * 0.2f;
                float n = n1 + n2 + veins;
                Color col = Color.Lerp(darkGrey, lightGrey, n);
                col = Color.Lerp(col, baseGrey, 0.3f);
                float crack = Mathf.PerlinNoise((nx + ox) * 120f, (ny + oy) * 120f);
                if (crack > 0.78f) col = Color.Lerp(col, darkGrey, (crack - 0.78f) * 4f);
                col.a = 1f;
                tex.SetPixel(x, y, col);
            }

        tex.Apply(true); tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    /// <summary>
    /// Areia pálida/creme para margens do lago.
    /// </summary>
    public static Texture2D GeneratePaleSandTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_PaleSand";
        Color baseSand = new Color(0.6f, 0.55f, 0.45f);
        Color lightSand = new Color(0.7f, 0.65f, 0.55f);
        Color darkSand = new Color(0.48f, 0.43f, 0.35f);
        float ox = Random.Range(0f, 1000f), oy = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE, ny = (float)y / TEX_SIZE;
                float n1 = Mathf.PerlinNoise((nx + ox) * 12f, (ny + oy) * 12f);
                float n2 = Mathf.PerlinNoise((nx + ox) * 40f, (ny + oy) * 40f) * 0.25f;
                float n3 = Mathf.PerlinNoise((nx + ox) * 100f, (ny + oy) * 100f) * 0.1f;
                float n = n1 + n2 + n3;
                Color col = Color.Lerp(darkSand, lightSand, n);
                col = Color.Lerp(col, baseSand, 0.3f);
                col.a = 1f;
                tex.SetPixel(x, y, col);
            }

        tex.Apply(true); tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    /// <summary>
    /// Fundo do lago — lama azul-acinzentada.
    /// </summary>
    public static Texture2D GenerateLakeBedTexture()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, true);
        tex.name = "T_LakeBed";
        Color baseMud = new Color(0.18f, 0.2f, 0.28f);
        Color darkMud = new Color(0.1f, 0.12f, 0.18f);
        Color lightMud = new Color(0.25f, 0.27f, 0.32f);
        float ox = Random.Range(0f, 1000f), oy = Random.Range(0f, 1000f);

        for (int y = 0; y < TEX_SIZE; y++)
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float nx = (float)x / TEX_SIZE, ny = (float)y / TEX_SIZE;
                float n1 = Mathf.PerlinNoise((nx + ox) * 8f, (ny + oy) * 8f);
                float n2 = Mathf.PerlinNoise((nx + ox) * 20f, (ny + oy) * 20f) * 0.3f;
                float peb = SimulateWorley(nx * 25f + ox, ny * 25f + oy) * 0.15f;
                float n = n1 + n2 + peb;
                Color col = Color.Lerp(darkMud, lightMud, n);
                col = Color.Lerp(col, baseMud, 0.3f);
                col.a = 1f;
                tex.SetPixel(x, y, col);
            }

        tex.Apply(true); tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }
}
