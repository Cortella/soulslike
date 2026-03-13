using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gera ruínas procedurais estilo Liurnia (Elden Ring): colunas de mármore,
/// arcos antigos, paredes quebradas, pedestais com glifos emissivos.
/// Cada ruína é um ÚNICO mesh combinado para performance.
/// </summary>
public static class ProceduralRuinGenerator
{
    private static Material marbleMat;
    private static Material darkStoneMat;
    private static Material glyphMat; // Emissivo azul

    public static void InitMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // Mármore/pedra antiga — cinza claro
        Texture2D marbleTex = ProceduralTextureGenerator.GenerateAncientStoneTexture();
        Texture2D marbleNormal = ProceduralTextureGenerator.GenerateNormalMap(marbleTex, 2f);
        marbleMat = new Material(urpLit);
        marbleMat.name = "M_AncientMarble";
        marbleMat.mainTexture = marbleTex;
        marbleMat.SetTexture("_BumpMap", marbleNormal);
        marbleMat.EnableKeyword("_NORMALMAP");
        marbleMat.SetFloat("_BumpScale", 1.2f);
        marbleMat.SetFloat("_Smoothness", 0.35f);
        marbleMat.SetFloat("_Metallic", 0.05f);
        marbleMat.enableInstancing = true;

        // Pedra escura (base, pedestal)
        darkStoneMat = new Material(urpLit);
        darkStoneMat.name = "M_DarkStone";
        darkStoneMat.color = new Color(0.25f, 0.25f, 0.28f);
        darkStoneMat.SetFloat("_Smoothness", 0.2f);
        darkStoneMat.enableInstancing = true;

        // Glifos emissivos azuis
        glyphMat = new Material(urpLit);
        glyphMat.name = "M_Glyph_Blue";
        glyphMat.color = new Color(0.3f, 0.5f, 0.9f);
        glyphMat.EnableKeyword("_EMISSION");
        glyphMat.SetColor("_EmissionColor", new Color(0.15f, 0.3f, 0.8f) * 3f);
        glyphMat.SetFloat("_Smoothness", 0.8f);
        glyphMat.enableInstancing = true;
    }

    /// <summary>
    /// Cria uma ruína de colunata (2-4 colunas com lintel opcional).
    /// Retorna GameObject pai contendo mesh combinado.
    /// </summary>
    public static GameObject CreateColonnade(Vector3 position, int seed = 0)
    {
        if (marbleMat == null) InitMaterials();
        System.Random rng = new System.Random(seed);

        GameObject ruin = new GameObject("Ruin_Colonnade");
        ruin.transform.position = position;
        ruin.isStatic = true;

        int columns = 2 + rng.Next(3); // 2-4 colunas
        float spacing = 2.5f + (float)rng.NextDouble() * 1.5f;
        float height = 4f + (float)rng.NextDouble() * 3f;
        float radius = 0.25f + (float)rng.NextDouble() * 0.15f;
        bool hasLintel = rng.NextDouble() > 0.35;
        bool isBroken = rng.NextDouble() > 0.5;

        for (int i = 0; i < columns; i++)
        {
            float x = (i - (columns - 1) * 0.5f) * spacing;
            float colH = height;
            if (isBroken && rng.NextDouble() > 0.5)
                colH *= 0.3f + (float)rng.NextDouble() * 0.5f;

            // Coluna (cilindro)
            CreateCylinderPart($"Column_{i}", ruin.transform,
                new Vector3(x, colH * 0.5f, 0), radius, radius * 0.85f, colH, 10, marbleMat);

            // Base da coluna
            CreateBoxPart($"Base_{i}", ruin.transform,
                new Vector3(x, 0.15f, 0), new Vector3(radius * 3f, 0.3f, radius * 3f), darkStoneMat);

            // Capitel (topo decorado)
            if (colH > height * 0.7f)
            {
                CreateBoxPart($"Capital_{i}", ruin.transform,
                    new Vector3(x, colH - 0.1f, 0), new Vector3(radius * 2.5f, 0.2f, radius * 2.5f), marbleMat);
            }
        }

        // Lintel (viga horizontal conectando colunas)
        if (hasLintel && !isBroken)
        {
            float lintelWidth = (columns - 1) * spacing + radius * 4f;
            CreateBoxPart("Lintel", ruin.transform,
                new Vector3(0, height + 0.1f, 0), new Vector3(lintelWidth, 0.35f, radius * 3f), marbleMat);
        }

        // Glifo emissivo em uma coluna aleatória
        if (rng.NextDouble() > 0.4)
        {
            int glyphCol = rng.Next(columns);
            float gx = (glyphCol - (columns - 1) * 0.5f) * spacing;
            float gy = 1.5f + (float)rng.NextDouble() * 1.5f;
            CreateBoxPart("Glyph", ruin.transform,
                new Vector3(gx, gy, radius + 0.01f), new Vector3(0.15f, 0.3f, 0.02f), glyphMat);

            // Luz do glifo
            GameObject light = new GameObject("GlyphLight");
            light.transform.SetParent(ruin.transform);
            light.transform.localPosition = new Vector3(gx, gy, radius + 0.3f);
            Light gl = light.AddComponent<Light>();
            gl.type = LightType.Point;
            gl.color = new Color(0.3f, 0.5f, 1f);
            gl.intensity = 0.8f;
            gl.range = 4f;
            gl.shadows = LightShadows.None;
        }

        // Rotação aleatória
        ruin.transform.rotation = Quaternion.Euler(0, rng.Next(360), 0);
        return ruin;
    }

    /// <summary>
    /// Cria um arco (dois pilares + arco semicircular).
    /// </summary>
    public static GameObject CreateArch(Vector3 position, int seed = 0)
    {
        if (marbleMat == null) InitMaterials();
        System.Random rng = new System.Random(seed);

        GameObject ruin = new GameObject("Ruin_Arch");
        ruin.transform.position = position;
        ruin.isStatic = true;

        float width = 3f + (float)rng.NextDouble() * 2f;
        float height = 4.5f + (float)rng.NextDouble() * 2f;
        float pillarR = 0.3f;
        bool intact = rng.NextDouble() > 0.3;

        // Pilares
        float lx = -width * 0.5f, rx = width * 0.5f;
        CreateCylinderPart("Pillar_L", ruin.transform,
            new Vector3(lx, height * 0.5f, 0), pillarR, pillarR * 0.9f, height, 8, marbleMat);
        CreateCylinderPart("Pillar_R", ruin.transform,
            new Vector3(rx, height * 0.5f, 0), pillarR, pillarR * 0.9f, height, 8, marbleMat);

        // Bases
        CreateBoxPart("Base_L", ruin.transform,
            new Vector3(lx, 0.15f, 0), new Vector3(0.8f, 0.3f, 0.8f), darkStoneMat);
        CreateBoxPart("Base_R", ruin.transform,
            new Vector3(rx, 0.15f, 0), new Vector3(0.8f, 0.3f, 0.8f), darkStoneMat);

        // Arco semicircular (keystone pieces)
        if (intact)
        {
            int archSegments = 7;
            for (int i = 0; i <= archSegments; i++)
            {
                float t = (float)i / archSegments;
                float angle = Mathf.PI * t;
                float ax = Mathf.Cos(angle) * width * 0.5f;
                float ay = Mathf.Sin(angle) * width * 0.35f + height;

                CreateBoxPart($"ArchStone_{i}", ruin.transform,
                    new Vector3(ax, ay, 0),
                    new Vector3(0.4f, 0.25f, 0.5f), marbleMat);
            }
        }
        else
        {
            // Arco quebrado — só metade
            for (int i = 0; i <= 3; i++)
            {
                float t = (float)i / 7;
                float angle = Mathf.PI * t;
                float ax = Mathf.Cos(angle) * width * 0.5f;
                float ay = Mathf.Sin(angle) * width * 0.35f + height;

                CreateBoxPart($"ArchStone_{i}", ruin.transform,
                    new Vector3(ax, ay, 0), new Vector3(0.4f, 0.25f, 0.5f), marbleMat);
            }

            // Debris no chão
            for (int d = 0; d < 3; d++)
            {
                float dx = (float)(rng.NextDouble() - 0.5) * width;
                float ds = 0.2f + (float)rng.NextDouble() * 0.4f;
                CreateBoxPart($"Debris_{d}", ruin.transform,
                    new Vector3(dx, ds * 0.5f, (float)(rng.NextDouble() - 0.5) * 2f),
                    new Vector3(ds, ds, ds), marbleMat);
            }
        }

        // Glifo no keystone
        if (intact && rng.NextDouble() > 0.3)
        {
            CreateBoxPart("Glyph", ruin.transform,
                new Vector3(0, height + width * 0.35f, 0.26f),
                new Vector3(0.2f, 0.2f, 0.02f), glyphMat);

            GameObject light = new GameObject("GlyphLight");
            light.transform.SetParent(ruin.transform);
            light.transform.localPosition = new Vector3(0, height + width * 0.35f, 0.5f);
            Light gl = light.AddComponent<Light>();
            gl.type = LightType.Point;
            gl.color = new Color(0.3f, 0.5f, 1f);
            gl.intensity = 0.6f;
            gl.range = 3f;
            gl.shadows = LightShadows.None;
        }

        ruin.transform.rotation = Quaternion.Euler(0, rng.Next(360), 0);
        return ruin;
    }

    /// <summary>
    /// Cria uma parede arruinada com possível janela/abertura.
    /// </summary>
    public static GameObject CreateRuinWall(Vector3 position, int seed = 0)
    {
        if (marbleMat == null) InitMaterials();
        System.Random rng = new System.Random(seed);

        GameObject ruin = new GameObject("Ruin_Wall");
        ruin.transform.position = position;
        ruin.isStatic = true;

        float w = 4f + (float)rng.NextDouble() * 4f;
        float h = 2.5f + (float)rng.NextDouble() * 3f;
        float thickness = 0.4f;

        // Parede principal
        CreateBoxPart("Wall", ruin.transform,
            new Vector3(0, h * 0.5f, 0), new Vector3(w, h, thickness), marbleMat);

        // Base
        CreateBoxPart("WallBase", ruin.transform,
            new Vector3(0, 0.1f, 0), new Vector3(w + 0.3f, 0.2f, thickness + 0.2f), darkStoneMat);

        // Debris
        int debrisCount = 1 + rng.Next(3);
        for (int d = 0; d < debrisCount; d++)
        {
            float ds = 0.2f + (float)rng.NextDouble() * 0.6f;
            float dx = (float)(rng.NextDouble() - 0.5) * (w + 2f);
            float dz = (float)(rng.NextDouble() - 0.5) * 2f;
            CreateBoxPart($"Debris_{d}", ruin.transform,
                new Vector3(dx, ds * 0.5f, dz), new Vector3(ds, ds * 0.7f, ds), marbleMat);
        }

        ruin.transform.rotation = Quaternion.Euler(0, rng.Next(360), (float)(rng.NextDouble() - 0.5) * 8f);
        return ruin;
    }

    /// <summary>
    /// Cria um pedestal com glifo emissivo e luz azul.
    /// </summary>
    public static GameObject CreateGlyphPedestal(Vector3 position, int seed = 0)
    {
        if (marbleMat == null) InitMaterials();
        System.Random rng = new System.Random(seed);

        GameObject ruin = new GameObject("Ruin_Pedestal");
        ruin.transform.position = position;
        ruin.isStatic = true;

        float baseSize = 1f + (float)rng.NextDouble() * 0.5f;
        float height = 0.8f + (float)rng.NextDouble() * 0.4f;

        // Base piramidal
        CreateBoxPart("PedestalBase", ruin.transform,
            new Vector3(0, 0.1f, 0), new Vector3(baseSize * 1.3f, 0.2f, baseSize * 1.3f), darkStoneMat);
        CreateBoxPart("PedestalBody", ruin.transform,
            new Vector3(0, height * 0.5f + 0.1f, 0), new Vector3(baseSize, height, baseSize), marbleMat);

        // Glifo emissivo no topo
        CreateBoxPart("GlyphPlate", ruin.transform,
            new Vector3(0, height + 0.15f, 0), new Vector3(baseSize * 0.6f, 0.08f, baseSize * 0.6f), glyphMat);

        // Luz azul
        GameObject light = new GameObject("PedestalLight");
        light.transform.SetParent(ruin.transform);
        light.transform.localPosition = new Vector3(0, height + 0.5f, 0);
        Light gl = light.AddComponent<Light>();
        gl.type = LightType.Point;
        gl.color = new Color(0.25f, 0.4f, 1f);
        gl.intensity = 1.2f;
        gl.range = 6f;
        gl.shadows = LightShadows.None;

        return ruin;
    }

    // =================== MESH HELPERS ====================

    static GameObject CreateBoxPart(string name, Transform parent,
        Vector3 localPos, Vector3 size, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        obj.isStatic = true;
        return obj;
    }

    static GameObject CreateCylinderPart(string name, Transform parent,
        Vector3 localPos, float radiusBottom, float radiusTop, float height, int segments, Material mat)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.isStatic = true;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        Mesh mesh = new Mesh();
        float halfH = height * 0.5f;
        Vector3[] verts = new Vector3[(segments + 1) * 2 + 2];
        Vector2[] uvs = new Vector2[verts.Length];

        // Side vertices
        for (int i = 0; i <= segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            verts[i] = new Vector3(c * radiusBottom, -halfH, s * radiusBottom);
            uvs[i] = new Vector2((float)i / segments, 0);
            verts[i + segments + 1] = new Vector3(c * radiusTop, halfH, s * radiusTop);
            uvs[i + segments + 1] = new Vector2((float)i / segments, 1);
        }

        // Center verts for caps
        int botCenter = verts.Length - 2;
        int topCenter = verts.Length - 1;
        verts[botCenter] = new Vector3(0, -halfH, 0);
        uvs[botCenter] = new Vector2(0.5f, 0);
        verts[topCenter] = new Vector3(0, halfH, 0);
        uvs[topCenter] = new Vector2(0.5f, 1);

        List<int> tris = new List<int>();
        for (int i = 0; i < segments; i++)
        {
            // Side
            int bl = i, br = i + 1, tl = i + segments + 1, tr = i + segments + 2;
            tris.Add(bl); tris.Add(tl); tris.Add(br);
            tris.Add(br); tris.Add(tl); tris.Add(tr);
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.name = name + "_Mesh";
        mf.sharedMesh = mesh;

        // Collider
        obj.AddComponent<MeshCollider>().sharedMesh = mesh;
        return obj;
    }
}
