using UnityEngine;

/// <summary>
/// Gera um plano de água com material semitransparente URP.
/// Suporta profundidade visual, reflexo sutil e cor configurável.
/// </summary>
public static class WaterPlaneGenerator
{
    /// <summary>
    /// Cria um plano de água na altura especificada com o tamanho dado.
    /// </summary>
    public static GameObject CreateWaterPlane(Vector3 center, float size, float waterLevel,
        Color waterColor = default, float alpha = 0.65f)
    {
        if (waterColor == default)
            waterColor = new Color(0.08f, 0.18f, 0.38f); // Azul profundo estilo Liurnia

        GameObject waterObj = new GameObject("WaterPlane");
        waterObj.transform.position = new Vector3(center.x, waterLevel, center.z);
        waterObj.layer = LayerMask.NameToLayer("Water");
        if (waterObj.layer == -1) waterObj.layer = 4; // Water layer padrão

        // Criar mesh do plano de água (subdividido para ondulação)
        MeshFilter mf = waterObj.AddComponent<MeshFilter>();
        MeshRenderer mr = waterObj.AddComponent<MeshRenderer>();

        int subdivisions = 32;
        Mesh waterMesh = CreateSubdividedPlane(size, subdivisions);
        waterMesh.name = "WaterPlaneMesh";
        mf.sharedMesh = waterMesh;

        // Material de água transparente URP
        Material waterMat = CreateWaterMaterial(waterColor, alpha);
        mr.sharedMaterial = waterMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Collider trigger para detectar "em água"
        BoxCollider waterCol = waterObj.AddComponent<BoxCollider>();
        waterCol.size = new Vector3(size, 0.5f, size);
        waterCol.center = new Vector3(0, -0.25f, 0);
        waterCol.isTrigger = true;

        // Efeito visual: leve ondulação via script
        waterObj.AddComponent<WaterWaveEffect>();

        return waterObj;
    }

    private static Material CreateWaterMaterial(Color baseColor, float alpha)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Material mat = new Material(urpLit);
        mat.name = "M_Water_Liurnia";

        // Configurar transparência URP
        mat.SetFloat("_Surface", 1f); // Transparent
        mat.SetFloat("_Blend", 0f);   // Alpha
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // Cor da água com alpha
        mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        mat.SetFloat("_Smoothness", 0.92f);
        mat.SetFloat("_Metallic", 0.15f);

        // Emissão sutil (reflexo de lua)
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.03f, 0.05f, 0.08f));

        // Normal map para ripples
        Texture2D normalMap = GenerateWaterNormalMap(512);
        mat.SetTexture("_BumpMap", normalMap);
        mat.SetFloat("_BumpScale", 0.3f);
        mat.EnableKeyword("_NORMALMAP");

        mat.enableInstancing = true;
        return mat;
    }

    private static Mesh CreateSubdividedPlane(float size, int subdivisions)
    {
        int vertCount = (subdivisions + 1) * (subdivisions + 1);
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        int[] triangles = new int[subdivisions * subdivisions * 6];

        float halfSize = size * 0.5f;
        float step = size / subdivisions;

        int v = 0;
        for (int z = 0; z <= subdivisions; z++)
        {
            for (int x = 0; x <= subdivisions; x++)
            {
                vertices[v] = new Vector3(-halfSize + x * step, 0, -halfSize + z * step);
                uvs[v] = new Vector2((float)x / subdivisions, (float)z / subdivisions);
                normals[v] = Vector3.up;
                v++;
            }
        }

        int t = 0;
        for (int z = 0; z < subdivisions; z++)
        {
            for (int x = 0; x < subdivisions; x++)
            {
                int bl = z * (subdivisions + 1) + x;
                int br = bl + 1;
                int tl = bl + subdivisions + 1;
                int tr = tl + 1;

                triangles[t++] = bl;
                triangles[t++] = tl;
                triangles[t++] = br;
                triangles[t++] = br;
                triangles[t++] = tl;
                triangles[t++] = tr;
            }
        }

        Mesh mesh = new Mesh();
        if (vertCount > 65000)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Texture2D GenerateWaterNormalMap(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
        float scale1 = 8f, scale2 = 20f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (float)x / size;
                float ny = (float)y / size;

                // Duas camadas de noise para ripples
                float h1 = Mathf.PerlinNoise(nx * scale1, ny * scale1);
                float h2 = Mathf.PerlinNoise(nx * scale2 + 100, ny * scale2 + 100) * 0.5f;
                float h = h1 + h2;

                // Calcular normal via diferenças finitas
                float hR = Mathf.PerlinNoise((nx + 1f / size) * scale1, ny * scale1) +
                           Mathf.PerlinNoise((nx + 1f / size) * scale2 + 100, ny * scale2 + 100) * 0.5f;
                float hU = Mathf.PerlinNoise(nx * scale1, (ny + 1f / size) * scale1) +
                           Mathf.PerlinNoise(nx * scale2 + 100, (ny + 1f / size) * scale2 + 100) * 0.5f;

                float dx = (h - hR) * 2f;
                float dy = (h - hU) * 2f;

                // Encode normal em RGB (tangent space)
                Vector3 normal = new Vector3(dx, dy, 1f).normalized;
                tex.SetPixel(x, y, new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f));
            }
        }

        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        return tex;
    }
}

/// <summary>
/// Efeito de ondulação sutil na água (vertex displacement leve).
/// </summary>
public class WaterWaveEffect : MonoBehaviour
{
    public float waveSpeed = 0.3f;
    public float waveHeight = 0.08f;
    public float waveFrequency = 3f;

    private MeshFilter meshFilter;
    private Vector3[] originalVerts;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
            originalVerts = meshFilter.mesh.vertices;
    }

    private void Update()
    {
        if (originalVerts == null || meshFilter == null) return;

        Mesh mesh = meshFilter.mesh;
        Vector3[] verts = new Vector3[originalVerts.Length];
        float time = Time.time * waveSpeed;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = originalVerts[i];
            v.y += Mathf.Sin(v.x * waveFrequency + time) * waveHeight * 0.5f;
            v.y += Mathf.Sin(v.z * waveFrequency * 0.7f + time * 1.3f) * waveHeight * 0.5f;
            verts[i] = v;
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
    }
}
