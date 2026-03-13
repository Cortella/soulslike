using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gera detalhes ambientais de alta qualidade para a floresta:
/// Grass Patches com billboards, raízes no chão, cogumelos, pedras detalhadas,
/// ground cover (folhas caídas, musgo).
/// Renderizado com meshes reais e texturas procedurais.
/// </summary>
public static class EnvironmentDetailsGenerator
{
    private static Material grassMat;
    private static Material mossMat;
    private static Material mushroomMat;
    private static Material rootsMat;
    private static Material detailRockMat;
    private static Material groundLeavesMat;

    public static void InitMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Texture2D grassTex = ProceduralTextureGenerator.GenerateGrassTexture();
        Texture2D grassNormal = ProceduralTextureGenerator.GenerateNormalMap(grassTex, 1.5f);

        grassMat = new Material(urpLit);
        grassMat.name = "M_GrassDetail";
        grassMat.mainTexture = grassTex;
        grassMat.color = new Color(0.25f, 0.4f, 0.12f);
        grassMat.SetTexture("_BumpMap", grassNormal);
        grassMat.EnableKeyword("_NORMALMAP");
        grassMat.SetFloat("_Smoothness", 0.15f);
        // Double-sided rendering
        grassMat.SetFloat("_Cull", 0);

        Texture2D barkTex = ProceduralTextureGenerator.GenerateBarkTexture();
        Texture2D barkNormal = ProceduralTextureGenerator.GenerateNormalMap(barkTex, 2.5f);

        mossMat = new Material(urpLit);
        mossMat.name = "M_Moss";
        mossMat.color = new Color(0.1f, 0.25f, 0.08f);
        mossMat.SetFloat("_Smoothness", 0.3f);

        mushroomMat = new Material(urpLit);
        mushroomMat.name = "M_Mushroom";
        mushroomMat.color = new Color(0.55f, 0.18f, 0.08f);
        mushroomMat.SetFloat("_Smoothness", 0.45f);

        rootsMat = new Material(urpLit);
        rootsMat.name = "M_Roots";
        rootsMat.mainTexture = barkTex;
        rootsMat.color = new Color(0.18f, 0.12f, 0.07f);
        rootsMat.SetTexture("_BumpMap", barkNormal);
        rootsMat.EnableKeyword("_NORMALMAP");
        rootsMat.SetFloat("_Smoothness", 0.15f);

        Texture2D rockTex = ProceduralTextureGenerator.GenerateRockTexture();
        Texture2D rockNormal = ProceduralTextureGenerator.GenerateNormalMap(rockTex, 3f);

        detailRockMat = new Material(urpLit);
        detailRockMat.name = "M_DetailRock";
        detailRockMat.mainTexture = rockTex;
        detailRockMat.color = new Color(0.35f, 0.33f, 0.3f);
        detailRockMat.SetTexture("_BumpMap", rockNormal);
        detailRockMat.EnableKeyword("_NORMALMAP");
        detailRockMat.SetFloat("_Smoothness", 0.2f);
        detailRockMat.SetFloat("_Metallic", 0.05f);

        Texture2D leavesTex = ProceduralTextureGenerator.GenerateLeavesTexture();
        groundLeavesMat = new Material(urpLit);
        groundLeavesMat.name = "M_GroundLeaves";
        groundLeavesMat.mainTexture = leavesTex;
        groundLeavesMat.color = new Color(0.22f, 0.15f, 0.06f);
        groundLeavesMat.SetFloat("_Smoothness", 0.05f);
    }

    /// <summary>
    /// Gera detalhes no terreno: grama, cogumelos, raízes, pedras pequenas, folhas.
    /// </summary>
    public static GameObject GenerateDetails(Terrain terrain, int grassPatchCount = 400,
        int mushroomCount = 80, int smallRockCount = 150, int rootClusterCount = 60)
    {
        if (grassMat == null) InitMaterials();

        GameObject root = new GameObject("=== ENVIRONMENT DETAILS ===");
        float terrainSize = terrain.terrainData.size.x;

        // === GRASS PATCHES ===
        GameObject grassParent = new GameObject("GrassPatches");
        grassParent.transform.SetParent(root.transform);

        for (int i = 0; i < grassPatchCount; i++)
        {
            Vector3 pos = GetRandomTerrainPos(terrain, terrainSize, 20f);
            if (pos.y < 0) continue;

            // Cluster de 3-7 grass blades
            int blades = Random.Range(3, 8);
            GameObject cluster = new GameObject($"GrassCluster_{i}");
            cluster.transform.SetParent(grassParent.transform);
            cluster.transform.position = pos;

            for (int b = 0; b < blades; b++)
            {
                GameObject blade = CreateGrassBlade(
                    new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f)),
                    Random.Range(0.3f, 0.8f),
                    Random.Range(0.04f, 0.08f));
                blade.transform.SetParent(cluster.transform);
                blade.transform.localRotation = Quaternion.Euler(
                    Random.Range(-5f, 5f), Random.Range(0, 360f), Random.Range(-8f, 8f));
            }
        }

        // === MUSHROOMS ===
        GameObject mushroomParent = new GameObject("Mushrooms");
        mushroomParent.transform.SetParent(root.transform);

        for (int i = 0; i < mushroomCount; i++)
        {
            Vector3 pos = GetRandomTerrainPos(terrain, terrainSize, 25f);
            if (pos.y < 0) continue;

            // Mini cluster de 1 a 3 cogumelos
            int count = Random.Range(1, 4);
            for (int m = 0; m < count; m++)
            {
                Vector3 offset = new Vector3(Random.Range(-0.2f, 0.2f), 0, Random.Range(-0.2f, 0.2f));
                GameObject mushroom = CreateMushroom(pos + offset, Random.Range(0.03f, 0.08f));
                mushroom.transform.SetParent(mushroomParent.transform);
            }
        }

        // === SMALL ROCKS ===
        GameObject rockParent = new GameObject("SmallRocks");
        rockParent.transform.SetParent(root.transform);

        for (int i = 0; i < smallRockCount; i++)
        {
            Vector3 pos = GetRandomTerrainPos(terrain, terrainSize, 15f);
            if (pos.y < 0) continue;

            GameObject rock = CreateSmallRock(pos, Random.Range(0.1f, 0.35f));
            rock.transform.SetParent(rockParent.transform);
        }

        // === ROOT CLUSTERS ===
        GameObject rootParent = new GameObject("Roots");
        rootParent.transform.SetParent(root.transform);

        for (int i = 0; i < rootClusterCount; i++)
        {
            Vector3 pos = GetRandomTerrainPos(terrain, terrainSize, 30f);
            if (pos.y < 0) continue;

            GameObject roots = CreateRootCluster(pos);
            roots.transform.SetParent(rootParent.transform);
        }

        // === GROUND COVER (Folhas caídas espalhadas) ===
        GameObject coverParent = new GameObject("GroundCover");
        coverParent.transform.SetParent(root.transform);

        for (int i = 0; i < 200; i++)
        {
            Vector3 pos = GetRandomTerrainPos(terrain, terrainSize, 20f);
            if (pos.y < 0) continue;

            GameObject cover = CreateGroundLeafPatch(pos, Random.Range(0.5f, 1.5f));
            cover.transform.SetParent(coverParent.transform);
        }

        return root;
    }

    // ==================== FACTORY METHODS ====================

    static GameObject CreateGrassBlade(Vector3 localPos, float height, float width)
    {
        GameObject blade = new GameObject("GrassBlade");
        blade.transform.localPosition = localPos;

        MeshFilter mf = blade.AddComponent<MeshFilter>();
        MeshRenderer mr = blade.AddComponent<MeshRenderer>();
        mr.sharedMaterial = grassMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;

        // Blade mesh: flat quad curvado
        Mesh mesh = new Mesh();
        int segments = 4;
        Vector3[] verts = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[(segments + 1) * 2];

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float curveX = Mathf.Sin(t * Mathf.PI * 0.3f) * width * 1.5f;
            float w = width * (1f - t * 0.6f); // afina pra cima

            verts[i * 2] = new Vector3(-w + curveX, t * height, 0);
            verts[i * 2 + 1] = new Vector3(w + curveX, t * height, 0);

            uvs[i * 2] = new Vector2(0, t);
            uvs[i * 2 + 1] = new Vector2(1, t);
        }

        List<int> tris = new List<int>();
        for (int i = 0; i < segments; i++)
        {
            int bl = i * 2, br = i * 2 + 1;
            int tl = (i + 1) * 2, tr = (i + 1) * 2 + 1;
            tris.Add(bl); tris.Add(tl); tris.Add(br);
            tris.Add(br); tris.Add(tl); tris.Add(tr);
            // Back face
            tris.Add(bl); tris.Add(br); tris.Add(tl);
            tris.Add(br); tris.Add(tr); tris.Add(tl);
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;

        return blade;
    }

    static GameObject CreateMushroom(Vector3 pos, float scale)
    {
        GameObject mushroomObj = new GameObject("Mushroom");
        mushroomObj.transform.position = pos;

        // Stem - thin cylinder
        GameObject stem = new GameObject("Stem");
        stem.transform.SetParent(mushroomObj.transform);
        stem.transform.localPosition = new Vector3(0, scale * 0.5f, 0);
        MeshFilter mfS = stem.AddComponent<MeshFilter>();
        MeshRenderer mrS = stem.AddComponent<MeshRenderer>();
        Mesh stemMesh = CreateCylinderMeshSimple(scale * 0.15f, scale * 0.1f, scale, 6);
        mfS.sharedMesh = stemMesh;
        mrS.sharedMaterial = mossMat;
        mrS.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        // Cap - flattened sphere
        GameObject cap = new GameObject("Cap");
        cap.transform.SetParent(mushroomObj.transform);
        cap.transform.localPosition = new Vector3(0, scale * 1.05f, 0);
        MeshFilter mfC = cap.AddComponent<MeshFilter>();
        MeshRenderer mrC = cap.AddComponent<MeshRenderer>();
        Mesh capMesh = CreateHalfSphereMesh(scale * 0.6f, 8, 4);
        mfC.sharedMesh = capMesh;
        mrC.sharedMaterial = mushroomMat;
        mrC.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        // Emissão sutil para cogumelos bioluminescentes (1 em 4)
        if (Random.value < 0.25f)
        {
            Material glowMush = new Material(mushroomMat);
            glowMush.EnableKeyword("_EMISSION");
            Color emColor = new Color(0.1f, 0.4f, 0.15f) * 1.5f;
            glowMush.SetColor("_EmissionColor", emColor);
            mrC.sharedMaterial = glowMush;

            // Small light
            Light l = cap.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = emColor;
            l.intensity = 0.3f;
            l.range = 1.5f;
            l.shadows = LightShadows.None;
        }

        return mushroomObj;
    }

    static GameObject CreateSmallRock(Vector3 pos, float scale)
    {
        GameObject rock = new GameObject("SmallRock");
        rock.transform.position = pos;
        rock.transform.localRotation = Quaternion.Euler(
            Random.Range(0, 30f), Random.Range(0, 360f), Random.Range(0, 20f));

        MeshFilter mf = rock.AddComponent<MeshFilter>();
        MeshRenderer mr = rock.AddComponent<MeshRenderer>();
        mr.sharedMaterial = detailRockMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        // Deformed icosphere
        Mesh mesh = CreateDeformedSphereMesh(scale, 6, 4, 0.3f);
        mf.sharedMesh = mesh;

        return rock;
    }

    static GameObject CreateRootCluster(Vector3 pos)
    {
        GameObject rootCluster = new GameObject("RootCluster");
        rootCluster.transform.position = pos;

        int rootCount = Random.Range(2, 5);
        for (int i = 0; i < rootCount; i++)
        {
            GameObject rootPiece = new GameObject($"Root_{i}");
            rootPiece.transform.SetParent(rootCluster.transform);

            float angle = Random.Range(0, 360f);
            float length = Random.Range(0.5f, 1.5f);
            float radius = Random.Range(0.03f, 0.06f);

            rootPiece.transform.localPosition = Vector3.zero;
            rootPiece.transform.localRotation = Quaternion.Euler(
                Random.Range(60f, 85f), angle, 0);

            MeshFilter mf = rootPiece.AddComponent<MeshFilter>();
            MeshRenderer mr = rootPiece.AddComponent<MeshRenderer>();
            mr.sharedMaterial = rootsMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            Mesh mesh = CreateCylinderMeshSimple(radius, radius * 0.4f, length, 5);
            mf.sharedMesh = mesh;
        }

        return rootCluster;
    }

    static GameObject CreateGroundLeafPatch(Vector3 pos, float patchSize)
    {
        GameObject patch = new GameObject("LeafPatch");
        patch.transform.position = pos + Vector3.up * 0.02f;
        patch.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

        MeshFilter mf = patch.AddComponent<MeshFilter>();
        MeshRenderer mr = patch.AddComponent<MeshRenderer>();
        mr.sharedMaterial = groundLeavesMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;

        // Flat quad no chão
        Mesh mesh = new Mesh();
        float h = patchSize * 0.5f;
        mesh.vertices = new Vector3[] {
            new Vector3(-h, 0, -h), new Vector3(h, 0, -h),
            new Vector3(h, 0, h), new Vector3(-h, 0, h)
        };
        mesh.uv = new Vector2[] {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        mf.sharedMesh = mesh;

        return patch;
    }

    // ==================== MESH HELPERS ====================

    static Vector3 GetRandomTerrainPos(Terrain terrain, float terrainSize, float margin)
    {
        float x = Random.Range(margin, terrainSize - margin);
        float z = Random.Range(margin, terrainSize - margin);
        float y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y;

        // Evitar spawn em terreno muito íngreme
        Vector3 normal = terrain.terrainData.GetInterpolatedNormal(x / terrainSize, z / terrainSize);
        if (normal.y < 0.7f) return Vector3.down; // Sinaliza skip

        return new Vector3(x, y + 0.01f, z);
    }

    static Mesh CreateCylinderMeshSimple(float rBot, float rTop, float height, int segments)
    {
        Mesh mesh = new Mesh();
        float halfH = height * 0.5f;

        Vector3[] verts = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[(segments + 1) * 2];

        for (int i = 0; i <= segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);

            verts[i] = new Vector3(c * rBot, -halfH, s * rBot);
            uvs[i] = new Vector2((float)i / segments, 0f);

            verts[i + segments + 1] = new Vector3(c * rTop, halfH, s * rTop);
            uvs[i + segments + 1] = new Vector2((float)i / segments, 1f);
        }

        List<int> tris = new List<int>();
        for (int i = 0; i < segments; i++)
        {
            int bl = i, br = i + 1;
            int tl = i + segments + 1, tr = i + segments + 2;
            tris.Add(bl); tris.Add(tl); tris.Add(br);
            tris.Add(br); tris.Add(tl); tris.Add(tr);
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh CreateHalfSphereMesh(float radius, int longSeg, int latSeg)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int lat = 0; lat <= latSeg; lat++)
        {
            float theta = Mathf.PI * 0.5f * lat / latSeg; // Apenas metade superior
            float sinT = Mathf.Sin(theta);
            float cosT = Mathf.Cos(theta);

            for (int lon = 0; lon <= longSeg; lon++)
            {
                float phi = 2f * Mathf.PI * lon / longSeg;
                verts.Add(new Vector3(
                    Mathf.Cos(phi) * sinT * radius,
                    cosT * radius * 0.5f,
                    Mathf.Sin(phi) * sinT * radius));
                uvs.Add(new Vector2((float)lon / longSeg, (float)lat / latSeg));
            }
        }

        for (int lat = 0; lat < latSeg; lat++)
        {
            for (int lon = 0; lon < longSeg; lon++)
            {
                int first = lat * (longSeg + 1) + lon;
                int second = first + longSeg + 1;
                tris.Add(first); tris.Add(second); tris.Add(first + 1);
                tris.Add(second); tris.Add(second + 1); tris.Add(first + 1);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh CreateDeformedSphereMesh(float radius, int longSeg, int latSeg, float deformAmount)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        float seed = Random.Range(0f, 1000f);

        for (int lat = 0; lat <= latSeg; lat++)
        {
            float theta = Mathf.PI * lat / latSeg;
            float sinT = Mathf.Sin(theta);
            float cosT = Mathf.Cos(theta);

            for (int lon = 0; lon <= longSeg; lon++)
            {
                float phi = 2f * Mathf.PI * lon / longSeg;
                float noise = Mathf.PerlinNoise(
                    seed + (float)lon / longSeg * 3f,
                    seed + (float)lat / latSeg * 3f);
                float r = radius * (1f + (noise - 0.5f) * deformAmount);

                verts.Add(new Vector3(
                    Mathf.Cos(phi) * sinT * r,
                    cosT * r * 0.6f, // Achatar Y
                    Mathf.Sin(phi) * sinT * r));
                uvs.Add(new Vector2((float)lon / longSeg, (float)lat / latSeg));
            }
        }

        for (int lat = 0; lat < latSeg; lat++)
        {
            for (int lon = 0; lon < longSeg; lon++)
            {
                int first = lat * (longSeg + 1) + lon;
                int second = first + longSeg + 1;
                tris.Add(first); tris.Add(second); tris.Add(first + 1);
                tris.Add(second); tris.Add(second + 1); tris.Add(first + 1);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
