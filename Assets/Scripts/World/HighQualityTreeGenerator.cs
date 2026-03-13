using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gera meshes reais de árvores com geometria de tronco cilíndrico, galhos
/// e canopias com faces verdadeiras em vez de primitivas empilhadas.
/// Qualidade muito superior ao ProceduralTreeFactory.
/// </summary>
public static class HighQualityTreeGenerator
{
    private static Material barkMaterial;
    private static Material leafMaterial;
    private static Material deadBarkMaterial;
    private static Material pineLeafMaterial;

    public static void InitMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // Casca com textura procedural
        Texture2D barkTex = ProceduralTextureGenerator.GenerateBarkTexture();
        Texture2D barkNormal = ProceduralTextureGenerator.GenerateNormalMap(barkTex, 3f);

        barkMaterial = new Material(urpLit);
        barkMaterial.name = "M_Bark_HQ";
        barkMaterial.mainTexture = barkTex;
        barkMaterial.SetTexture("_BumpMap", barkNormal);
        barkMaterial.SetFloat("_BumpScale", 1.5f);
        barkMaterial.EnableKeyword("_NORMALMAP");
        barkMaterial.SetFloat("_Smoothness", 0.15f);
        barkMaterial.SetFloat("_Metallic", 0f);

        // Folhagem com textura e alpha
        Texture2D leafTex = ProceduralTextureGenerator.GenerateLeafTexture();
        leafMaterial = new Material(urpLit);
        leafMaterial.name = "M_Leaves_HQ";
        leafMaterial.mainTexture = leafTex;
        leafMaterial.color = new Color(0.15f, 0.3f, 0.06f, 0.9f);
        leafMaterial.SetFloat("_Smoothness", 0.1f);
        // Subsurface scattering fake via emissão sutil
        leafMaterial.EnableKeyword("_EMISSION");
        leafMaterial.SetColor("_EmissionColor", new Color(0.02f, 0.04f, 0.01f));

        // Casca morta
        deadBarkMaterial = new Material(urpLit);
        deadBarkMaterial.name = "M_DeadBark_HQ";
        deadBarkMaterial.mainTexture = barkTex;
        deadBarkMaterial.color = new Color(0.25f, 0.2f, 0.15f);
        deadBarkMaterial.SetTexture("_BumpMap", barkNormal);
        deadBarkMaterial.EnableKeyword("_NORMALMAP");
        deadBarkMaterial.SetFloat("_Smoothness", 0.08f);

        // Folhagem de pinheiro
        pineLeafMaterial = new Material(urpLit);
        pineLeafMaterial.name = "M_PineLeaves_HQ";
        pineLeafMaterial.color = new Color(0.06f, 0.15f, 0.04f);
        pineLeafMaterial.SetFloat("_Smoothness", 0.05f);
    }

    /// <summary>
    /// Gera uma árvore completa (tronco + galhos + copa) com mesh real.
    /// </summary>
    public static GameObject CreateTree(Vector3 position, int style = 0, int seed = 0)
    {
        if (barkMaterial == null) InitMaterials();

        System.Random rng = new System.Random(seed);
        float scale = 0.8f + (float)rng.NextDouble() * 0.6f;

        switch (style % 5)
        {
            case 0: return CreateOakTree(position, scale, rng);
            case 1: return CreatePineTree(position, scale, rng);
            case 2: return CreateTallPine(position, scale, rng);
            case 3: return CreateDeadTree(position, scale, rng);
            case 4: return CreateWillowTree(position, scale, rng);
            default: return CreateOakTree(position, scale, rng);
        }
    }

    // =================== OAK TREE ====================
    static GameObject CreateOakTree(Vector3 pos, float scale, System.Random rng)
    {
        GameObject tree = new GameObject("Oak_HQ");
        tree.transform.position = pos;

        float trunkH = (4f + (float)rng.NextDouble() * 3f) * scale;
        float trunkR = (0.2f + (float)rng.NextDouble() * 0.15f) * scale;

        // Tronco
        GameObject trunk = CreateCylinderMesh("Trunk", tree.transform,
            Vector3.up * trunkH * 0.5f, trunkR, trunkR * 0.6f, trunkH, 10, barkMaterial);

        // 3-5 galhos principais
        int branchCount = 3 + rng.Next(3);
        for (int i = 0; i < branchCount; i++)
        {
            float angle = (360f / branchCount) * i + (float)rng.NextDouble() * 30f;
            float heightOnTrunk = trunkH * (0.5f + (float)rng.NextDouble() * 0.35f);
            float branchLength = (1.5f + (float)rng.NextDouble() * 2f) * scale;
            float branchR = trunkR * 0.3f;

            Vector3 dir = Quaternion.Euler(-30 - (float)rng.NextDouble() * 30f, angle, 0) * Vector3.up;
            Vector3 branchPos = Vector3.up * heightOnTrunk + dir * branchLength * 0.5f;

            GameObject branch = CreateCylinderMesh($"Branch_{i}", tree.transform,
                branchPos, branchR, branchR * 0.4f, branchLength, 6, barkMaterial);
            branch.transform.localRotation = Quaternion.LookRotation(Vector3.up, dir) *
                Quaternion.Euler(90, 0, 0);

            // Copa no final do galho
            Vector3 canopyPos = Vector3.up * heightOnTrunk + dir * branchLength;
            float canopySize = (1.5f + (float)rng.NextDouble() * 1.5f) * scale;
            CreateIcosphereMesh($"Canopy_{i}", tree.transform, canopyPos,
                canopySize, 1, leafMaterial);
        }

        // Copa central grande
        float mainCanopyY = trunkH * 0.85f;
        float mainCanopySize = (2.5f + (float)rng.NextDouble() * 2f) * scale;
        CreateIcosphereMesh("MainCanopy", tree.transform,
            Vector3.up * mainCanopyY, mainCanopySize, 1, leafMaterial);

        // Segunda camada de copa
        CreateIcosphereMesh("TopCanopy", tree.transform,
            Vector3.up * (mainCanopyY + mainCanopySize * 0.5f),
            mainCanopySize * 0.7f, 1, leafMaterial);

        AddTreeCollider(tree, trunkH, trunkR);
        return tree;
    }

    // =================== PINE TREE ====================
    static GameObject CreatePineTree(Vector3 pos, float scale, System.Random rng)
    {
        GameObject tree = new GameObject("Pine_HQ");
        tree.transform.position = pos;

        float trunkH = (6f + (float)rng.NextDouble() * 4f) * scale;
        float trunkR = (0.15f + (float)rng.NextDouble() * 0.1f) * scale;

        CreateCylinderMesh("Trunk", tree.transform,
            Vector3.up * trunkH * 0.5f, trunkR, trunkR * 0.5f, trunkH, 8, barkMaterial);

        // Camadas cônicas de folhagem
        int layers = 4 + rng.Next(3);
        for (int i = 0; i < layers; i++)
        {
            float t = (float)i / layers;
            float y = trunkH * (0.3f + t * 0.65f);
            float coneRadius = (2.5f - t * 1.8f) * scale;
            float coneHeight = (1.5f - t * 0.5f) * scale;

            CreateConeMesh($"Layer_{i}", tree.transform,
                Vector3.up * y, coneRadius, coneHeight, 8, pineLeafMaterial);
        }

        AddTreeCollider(tree, trunkH, trunkR);
        return tree;
    }

    // =================== TALL PINE ====================
    static GameObject CreateTallPine(Vector3 pos, float scale, System.Random rng)
    {
        GameObject tree = new GameObject("TallPine_HQ");
        tree.transform.position = pos;

        float trunkH = (10f + (float)rng.NextDouble() * 5f) * scale;
        float trunkR = (0.12f + (float)rng.NextDouble() * 0.08f) * scale;

        CreateCylinderMesh("Trunk", tree.transform,
            Vector3.up * trunkH * 0.5f, trunkR, trunkR * 0.4f, trunkH, 8, barkMaterial);

        // Copa só no topo
        int layers = 3 + rng.Next(2);
        for (int i = 0; i < layers; i++)
        {
            float t = (float)i / layers;
            float y = trunkH * (0.6f + t * 0.35f);
            float coneRadius = (1.5f - t * 1f) * scale;
            float coneHeight = (1.8f - t * 0.6f) * scale;

            CreateConeMesh($"Layer_{i}", tree.transform,
                Vector3.up * y, coneRadius, coneHeight, 8, pineLeafMaterial);
        }

        AddTreeCollider(tree, trunkH, trunkR);
        return tree;
    }

    // =================== DEAD TREE ====================
    static GameObject CreateDeadTree(Vector3 pos, float scale, System.Random rng)
    {
        GameObject tree = new GameObject("Dead_HQ");
        tree.transform.position = pos;

        float trunkH = (3f + (float)rng.NextDouble() * 4f) * scale;
        float trunkR = (0.15f + (float)rng.NextDouble() * 0.12f) * scale;

        CreateCylinderMesh("Trunk", tree.transform,
            Vector3.up * trunkH * 0.5f, trunkR, trunkR * 0.5f, trunkH, 8, deadBarkMaterial);

        // Galhos tortos sem folha
        int branchCount = 3 + rng.Next(4);
        for (int i = 0; i < branchCount; i++)
        {
            float angle = (float)rng.NextDouble() * 360f;
            float heightOnTrunk = trunkH * (0.35f + (float)rng.NextDouble() * 0.55f);
            float branchLen = (1f + (float)rng.NextDouble() * 2.5f) * scale;
            float branchR = trunkR * 0.25f;

            Vector3 dir = Quaternion.Euler(-20 - (float)rng.NextDouble() * 40f, angle, 0) * Vector3.up;
            Vector3 bPos = Vector3.up * heightOnTrunk + dir * branchLen * 0.5f;

            GameObject branch = CreateCylinderMesh($"Branch_{i}", tree.transform,
                bPos, branchR, branchR * 0.2f, branchLen, 5, deadBarkMaterial);
            branch.transform.localRotation = Quaternion.LookRotation(Vector3.up, dir) *
                Quaternion.Euler(90, 0, 0);
        }

        AddTreeCollider(tree, trunkH, trunkR);
        return tree;
    }

    // =================== WILLOW TREE ====================
    static GameObject CreateWillowTree(Vector3 pos, float scale, System.Random rng)
    {
        GameObject tree = new GameObject("Willow_HQ");
        tree.transform.position = pos;

        float trunkH = (5f + (float)rng.NextDouble() * 3f) * scale;
        float trunkR = (0.2f + (float)rng.NextDouble() * 0.15f) * scale;

        CreateCylinderMesh("Trunk", tree.transform,
            Vector3.up * trunkH * 0.5f, trunkR, trunkR * 0.55f, trunkH, 10, barkMaterial);

        // Copa ampla
        float canopyY = trunkH * 0.8f;
        float canopySize = (3f + (float)rng.NextDouble() * 2f) * scale;
        CreateIcosphereMesh("MainCanopy", tree.transform,
            Vector3.up * canopyY, canopySize, 1, leafMaterial);

        // Ramos pendentes (cilindros finos verticais)
        int vineCount = 8 + rng.Next(6);
        for (int i = 0; i < vineCount; i++)
        {
            float angle = (float)rng.NextDouble() * 360f;
            float dist = canopySize * (0.4f + (float)rng.NextDouble() * 0.5f);
            float vineLen = (2f + (float)rng.NextDouble() * 3f) * scale;

            Vector3 vineBase = Vector3.up * canopyY +
                Quaternion.Euler(0, angle, 0) * Vector3.forward * dist;

            CreateCylinderMesh($"Vine_{i}", tree.transform,
                vineBase - Vector3.up * vineLen * 0.5f, 0.02f * scale, 0.01f * scale,
                vineLen, 4, leafMaterial);
        }

        AddTreeCollider(tree, trunkH, trunkR);
        return tree;
    }

    // =================== MESH GENERATORS ====================

    static GameObject CreateCylinderMesh(string name, Transform parent,
        Vector3 localPos, float radiusBottom, float radiusTop, float height,
        int segments, Material mat)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        Mesh mesh = new Mesh();
        mesh.name = name + "_Mesh";

        int vertCount = (segments + 1) * 2;
        Vector3[] verts = new Vector3[vertCount + 2]; // +2 for caps
        Vector2[] uvs = new Vector2[vertCount + 2];
        int[] tris = new int[segments * 6 + segments * 6];

        float halfH = height * 0.5f;

        // Verts do cilindro
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // Bottom ring
            verts[i] = new Vector3(cos * radiusBottom, -halfH, sin * radiusBottom);
            uvs[i] = new Vector2((float)i / segments, 0f);

            // Top ring
            verts[i + segments + 1] = new Vector3(cos * radiusTop, halfH, sin * radiusTop);
            uvs[i + segments + 1] = new Vector2((float)i / segments, 1f);
        }

        // Triangles do corpo
        int triIdx = 0;
        for (int i = 0; i < segments; i++)
        {
            int bl = i;
            int br = i + 1;
            int tl = i + segments + 1;
            int tr = i + segments + 2;

            tris[triIdx++] = bl; tris[triIdx++] = tl; tris[triIdx++] = br;
            tris[triIdx++] = br; tris[triIdx++] = tl; tris[triIdx++] = tr;
        }

        // Caps (bottom e top)
        int capStart = vertCount;
        verts[capStart] = new Vector3(0, -halfH, 0); // center bottom
        uvs[capStart] = new Vector2(0.5f, 0f);
        verts[capStart + 1] = new Vector3(0, halfH, 0); // center top
        uvs[capStart + 1] = new Vector2(0.5f, 1f);

        // Bottom cap triangles
        for (int i = 0; i < segments; i++)
        {
            tris[triIdx++] = capStart;
            tris[triIdx++] = (i + 1) % (segments + 1);
            tris[triIdx++] = i;
        }

        // Top cap triangles
        for (int i = 0; i < segments; i++)
        {
            tris[triIdx++] = capStart + 1;
            tris[triIdx++] = i + segments + 1;
            tris[triIdx++] = ((i + 1) % (segments + 1)) + segments + 1;
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;

        return obj;
    }

    static GameObject CreateConeMesh(string name, Transform parent,
        Vector3 localPos, float radius, float height, int segments, Material mat)
    {
        return CreateCylinderMesh(name, parent, localPos + Vector3.up * height * 0.5f,
            radius, 0.05f, height, segments, mat);
    }

    static GameObject CreateIcosphereMesh(string name, Transform parent,
        Vector3 localPos, float radius, int subdivisions, Material mat)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        Mesh mesh = GenerateIcosphere(radius, subdivisions);
        mesh.name = name + "_Mesh";
        mf.sharedMesh = mesh;

        return obj;
    }

    static Mesh GenerateIcosphere(float radius, int subdivisions)
    {
        // Icosaedro base
        float t = (1f + Mathf.Sqrt(5f)) / 2f;

        List<Vector3> verts = new List<Vector3>
        {
            new Vector3(-1, t, 0).normalized * radius,
            new Vector3(1, t, 0).normalized * radius,
            new Vector3(-1, -t, 0).normalized * radius,
            new Vector3(1, -t, 0).normalized * radius,
            new Vector3(0, -1, t).normalized * radius,
            new Vector3(0, 1, t).normalized * radius,
            new Vector3(0, -1, -t).normalized * radius,
            new Vector3(0, 1, -t).normalized * radius,
            new Vector3(t, 0, -1).normalized * radius,
            new Vector3(t, 0, 1).normalized * radius,
            new Vector3(-t, 0, -1).normalized * radius,
            new Vector3(-t, 0, 1).normalized * radius
        };

        List<int> tris = new List<int>
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
            1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
            4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };

        // Subdividir
        Dictionary<long, int> midPointCache = new Dictionary<long, int>();
        for (int s = 0; s < subdivisions; s++)
        {
            List<int> newTris = new List<int>();
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = GetMidPoint(tris[i], tris[i + 1], verts, midPointCache, radius);
                int b = GetMidPoint(tris[i + 1], tris[i + 2], verts, midPointCache, radius);
                int c = GetMidPoint(tris[i + 2], tris[i], verts, midPointCache, radius);

                newTris.AddRange(new[] { tris[i], a, c });
                newTris.AddRange(new[] { tris[i + 1], b, a });
                newTris.AddRange(new[] { tris[i + 2], c, b });
                newTris.AddRange(new[] { a, b, c });
            }
            tris = newTris;
        }

        // Deformar ligeiramente para parecer natural
        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 v = verts[i];
            float noise = Mathf.PerlinNoise(v.x * 3f + 100f, v.z * 3f + 100f) * 0.3f;
            verts[i] = v * (1f + noise - 0.15f);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);

        // UVs
        List<Vector2> uvs = new List<Vector2>();
        foreach (var v in verts)
        {
            Vector3 n = v.normalized;
            uvs.Add(new Vector2(
                0.5f + Mathf.Atan2(n.z, n.x) / (2f * Mathf.PI),
                0.5f + Mathf.Asin(n.y) / Mathf.PI
            ));
        }
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static int GetMidPoint(int p1, int p2, List<Vector3> verts,
        Dictionary<long, int> cache, float radius)
    {
        long key = ((long)Mathf.Min(p1, p2) << 32) + Mathf.Max(p1, p2);
        if (cache.TryGetValue(key, out int idx)) return idx;

        Vector3 mid = ((verts[p1] + verts[p2]) / 2f).normalized * radius;
        verts.Add(mid);
        idx = verts.Count - 1;
        cache[key] = idx;
        return idx;
    }

    static void AddTreeCollider(GameObject tree, float height, float radius)
    {
        CapsuleCollider col = tree.AddComponent<CapsuleCollider>();
        col.height = height;
        col.radius = radius * 1.5f;
        col.center = Vector3.up * height * 0.5f;
    }
}
