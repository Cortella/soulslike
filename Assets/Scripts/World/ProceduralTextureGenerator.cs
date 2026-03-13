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
}
