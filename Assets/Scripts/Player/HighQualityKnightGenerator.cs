using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gera um modelo de cavaleiro com meshes reais (não primitivas).
/// Armadura detalhada, espada, escudo - tudo com geometria mesh,
/// texturas procedurais e normal maps.
/// </summary>
public static class HighQualityKnightGenerator
{
    private static Material armorMat;
    private static Material chainmailMat;
    private static Material leatherMat;
    private static Material swordBladeMat;
    private static Material capeMat;
    private static Material helmetMat;
    private static Material skinMat;
    private static Material eyeGlowMat; // para inimigos

    public static void InitMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Texture2D metalTex = GenerateMetalTexture();
        Texture2D metalNormal = ProceduralTextureGenerator.GenerateNormalMap(metalTex, 2f);

        Texture2D leatherTex = GenerateLeatherTexture();
        Texture2D leatherNormal = ProceduralTextureGenerator.GenerateNormalMap(leatherTex, 2.5f);

        // Armadura polida
        armorMat = new Material(urpLit);
        armorMat.name = "M_Armor_HQ";
        armorMat.mainTexture = metalTex;
        armorMat.color = new Color(0.35f, 0.35f, 0.38f);
        armorMat.SetTexture("_BumpMap", metalNormal);
        armorMat.EnableKeyword("_NORMALMAP");
        armorMat.SetFloat("_BumpScale", 1.2f);
        armorMat.SetFloat("_Smoothness", 0.65f);
        armorMat.SetFloat("_Metallic", 0.85f);

        // Cota de malha
        chainmailMat = new Material(urpLit);
        chainmailMat.name = "M_Chainmail_HQ";
        chainmailMat.mainTexture = metalTex;
        chainmailMat.color = new Color(0.3f, 0.3f, 0.32f);
        chainmailMat.SetTexture("_BumpMap", metalNormal);
        chainmailMat.EnableKeyword("_NORMALMAP");
        chainmailMat.SetFloat("_Smoothness", 0.4f);
        chainmailMat.SetFloat("_Metallic", 0.6f);

        // Couro
        leatherMat = new Material(urpLit);
        leatherMat.name = "M_Leather_HQ";
        leatherMat.mainTexture = leatherTex;
        leatherMat.color = new Color(0.22f, 0.15f, 0.08f);
        leatherMat.SetTexture("_BumpMap", leatherNormal);
        leatherMat.EnableKeyword("_NORMALMAP");
        leatherMat.SetFloat("_Smoothness", 0.2f);

        // Lâmina de espada
        swordBladeMat = new Material(urpLit);
        swordBladeMat.name = "M_SwordBlade_HQ";
        swordBladeMat.color = new Color(0.75f, 0.75f, 0.8f);
        swordBladeMat.SetFloat("_Smoothness", 0.92f);
        swordBladeMat.SetFloat("_Metallic", 1f);

        // Capa
        capeMat = new Material(urpLit);
        capeMat.name = "M_Cape_HQ";
        capeMat.mainTexture = leatherTex;
        capeMat.color = new Color(0.15f, 0.06f, 0.06f);
        capeMat.SetFloat("_Smoothness", 0.08f);

        // Elmo
        helmetMat = new Material(urpLit);
        helmetMat.name = "M_Helmet_HQ";
        helmetMat.mainTexture = metalTex;
        helmetMat.color = new Color(0.3f, 0.28f, 0.25f);
        helmetMat.SetTexture("_BumpMap", metalNormal);
        helmetMat.EnableKeyword("_NORMALMAP");
        helmetMat.SetFloat("_Smoothness", 0.5f);
        helmetMat.SetFloat("_Metallic", 0.7f);

        // Pele (para hollow)
        skinMat = new Material(urpLit);
        skinMat.name = "M_HollowSkin_HQ";
        skinMat.color = new Color(0.22f, 0.18f, 0.14f);
        skinMat.SetFloat("_Smoothness", 0.15f);

        // Olhos emissivos
        eyeGlowMat = new Material(urpLit);
        eyeGlowMat.name = "M_EyeGlow";
        eyeGlowMat.color = Color.red;
        eyeGlowMat.EnableKeyword("_EMISSION");
        eyeGlowMat.SetColor("_EmissionColor", new Color(1f, 0.2f, 0f) * 3f);
    }

    /// <summary>
    /// Cria o cavaleiro jogável com mesh real.
    /// </summary>
    public static GameObject CreateKnight(Vector3 position)
    {
        if (armorMat == null) InitMaterials();

        GameObject knight = new GameObject("KnightModel_HQ");
        knight.transform.position = position;

        // === TORSO ===
        CreateMeshPart("Torso", knight.transform, Vector3.up * 1.15f,
            new Vector3(0.5f, 0.42f, 0.28f), armorMat, MeshShape.RoundedBox);

        // Ombreiras
        CreateMeshPart("Shoulder_L", knight.transform, new Vector3(-0.3f, 1.35f, 0),
            new Vector3(0.16f, 0.14f, 0.2f), armorMat, MeshShape.Sphere);
        CreateMeshPart("Shoulder_R", knight.transform, new Vector3(0.3f, 1.35f, 0),
            new Vector3(0.16f, 0.14f, 0.2f), armorMat, MeshShape.Sphere);

        // === ABDÔMEN ===
        CreateMeshPart("Abdomen", knight.transform, Vector3.up * 0.88f,
            new Vector3(0.42f, 0.18f, 0.24f), chainmailMat, MeshShape.RoundedBox);

        // Cinto
        CreateMeshPart("Belt", knight.transform, Vector3.up * 0.76f,
            new Vector3(0.46f, 0.06f, 0.26f), leatherMat, MeshShape.Box);

        // === CABEÇA / ELMO ===
        CreateMeshPart("Helmet", knight.transform, Vector3.up * 1.58f,
            new Vector3(0.2f, 0.22f, 0.2f), helmetMat, MeshShape.Sphere);

        // Visor
        CreateMeshPart("Visor", knight.transform, new Vector3(0, 1.55f, 0.1f),
            new Vector3(0.14f, 0.04f, 0.04f), new Material(armorMat) { color = new Color(0.03f, 0.03f, 0.03f) },
            MeshShape.Box);

        // Crista
        CreateMeshPart("Crest", knight.transform, new Vector3(0, 1.72f, -0.02f),
            new Vector3(0.03f, 0.08f, 0.14f), armorMat, MeshShape.Box);

        // Pescoço
        CreateCylinderPart("Neck", knight.transform, Vector3.up * 1.42f,
            0.08f, 0.08f, 0.08f, 8, chainmailMat);

        // === BRAÇOS ===
        // Esquerdo
        CreateCylinderPart("UpperArm_L", knight.transform, new Vector3(-0.34f, 1.12f, 0),
            0.08f, 0.07f, 0.2f, 8, chainmailMat);
        CreateCylinderPart("ForeArm_L", knight.transform, new Vector3(-0.36f, 0.85f, 0.04f),
            0.07f, 0.06f, 0.18f, 8, armorMat);
        CreateMeshPart("Hand_L", knight.transform, new Vector3(-0.36f, 0.7f, 0.06f),
            new Vector3(0.07f, 0.05f, 0.08f), leatherMat, MeshShape.RoundedBox);

        // Direito
        CreateCylinderPart("UpperArm_R", knight.transform, new Vector3(0.34f, 1.12f, 0),
            0.08f, 0.07f, 0.2f, 8, chainmailMat);
        CreateCylinderPart("ForeArm_R", knight.transform, new Vector3(0.36f, 0.85f, 0.04f),
            0.07f, 0.06f, 0.18f, 8, armorMat);
        CreateMeshPart("Hand_R", knight.transform, new Vector3(0.36f, 0.7f, 0.06f),
            new Vector3(0.07f, 0.05f, 0.08f), leatherMat, MeshShape.RoundedBox);

        // === PERNAS ===
        // Esquerda
        CreateCylinderPart("Thigh_L", knight.transform, new Vector3(-0.14f, 0.56f, 0),
            0.09f, 0.08f, 0.22f, 8, chainmailMat);
        CreateCylinderPart("Shin_L", knight.transform, new Vector3(-0.14f, 0.28f, 0),
            0.075f, 0.065f, 0.22f, 8, armorMat);
        CreateMeshPart("Boot_L", knight.transform, new Vector3(-0.14f, 0.06f, 0.02f),
            new Vector3(0.09f, 0.08f, 0.15f), leatherMat, MeshShape.RoundedBox);

        // Direita
        CreateCylinderPart("Thigh_R", knight.transform, new Vector3(0.14f, 0.56f, 0),
            0.09f, 0.08f, 0.22f, 8, chainmailMat);
        CreateCylinderPart("Shin_R", knight.transform, new Vector3(0.14f, 0.28f, 0),
            0.075f, 0.065f, 0.22f, 8, armorMat);
        CreateMeshPart("Boot_R", knight.transform, new Vector3(0.14f, 0.06f, 0.02f),
            new Vector3(0.09f, 0.08f, 0.15f), leatherMat, MeshShape.RoundedBox);

        // === ESPADA ===
        GameObject sword = new GameObject("Sword_HQ");
        sword.transform.SetParent(knight.transform);
        sword.transform.localPosition = new Vector3(0.42f, 0.95f, 0.22f);
        sword.transform.localRotation = Quaternion.Euler(-10, 0, -15);

        CreateMeshPart("Blade", sword.transform, new Vector3(0, 0.42f, 0),
            new Vector3(0.035f, 0.48f, 0.012f), swordBladeMat, MeshShape.Box);
        CreateMeshPart("Guard", sword.transform, new Vector3(0, 0.14f, 0),
            new Vector3(0.13f, 0.02f, 0.025f), armorMat, MeshShape.Box);
        CreateCylinderPart("Grip", sword.transform, new Vector3(0, 0.05f, 0),
            0.02f, 0.018f, 0.1f, 6, leatherMat);
        CreateMeshPart("Pommel", sword.transform, Vector3.zero,
            Vector3.one * 0.03f, armorMat, MeshShape.Sphere);

        // === ESCUDO ===
        GameObject shield = new GameObject("Shield_HQ");
        shield.transform.SetParent(knight.transform);
        shield.transform.localPosition = new Vector3(-0.46f, 0.88f, 0.12f);
        shield.transform.localRotation = Quaternion.Euler(0, 10, 5);

        CreateMeshPart("ShieldBody", shield.transform, Vector3.zero,
            new Vector3(0.055f, 0.36f, 0.28f), leatherMat, MeshShape.RoundedBox);
        CreateMeshPart("ShieldBoss", shield.transform, new Vector3(0.03f, 0, 0),
            Vector3.one * 0.06f, armorMat, MeshShape.Sphere);

        // === CAPA ===
        CreateMeshPart("Cape_Upper", knight.transform, new Vector3(0, 1.15f, -0.16f),
            new Vector3(0.42f, 0.32f, 0.035f), capeMat, MeshShape.Box);
        CreateMeshPart("Cape_Lower", knight.transform, new Vector3(0, 0.68f, -0.18f),
            new Vector3(0.38f, 0.46f, 0.03f), capeMat, MeshShape.Box);

        return knight;
    }

    /// <summary>
    /// Cria modelo Hollow (inimigo undead) com mesh real.
    /// </summary>
    public static GameObject CreateHollow(Vector3 position)
    {
        if (skinMat == null) InitMaterials();

        Material hollowCloth = new Material(chainmailMat);
        hollowCloth.color = new Color(0.1f, 0.08f, 0.06f);
        hollowCloth.SetFloat("_Metallic", 0f);

        Material hollowWeapon = new Material(armorMat);
        hollowWeapon.color = new Color(0.28f, 0.22f, 0.18f);

        GameObject hollow = new GameObject("Hollow_HQ");
        hollow.transform.position = position;

        // Corpo magro
        CreateMeshPart("Torso", hollow.transform, new Vector3(0, 1f, 0.02f),
            new Vector3(0.3f, 0.32f, 0.18f), hollowCloth, MeshShape.RoundedBox);

        CreateMeshPart("Abdomen", hollow.transform, Vector3.up * 0.75f,
            new Vector3(0.25f, 0.12f, 0.15f), skinMat, MeshShape.RoundedBox);

        // Cabeça
        CreateMeshPart("Head", hollow.transform, new Vector3(0.01f, 1.38f, 0.01f),
            new Vector3(0.14f, 0.17f, 0.14f), skinMat, MeshShape.Sphere);

        // Olhos emissivos
        CreateMeshPart("Eye_L", hollow.transform, new Vector3(-0.04f, 1.4f, 0.07f),
            Vector3.one * 0.02f, eyeGlowMat, MeshShape.Sphere);
        CreateMeshPart("Eye_R", hollow.transform, new Vector3(0.04f, 1.4f, 0.07f),
            Vector3.one * 0.02f, eyeGlowMat, MeshShape.Sphere);

        // Braços finos
        CreateCylinderPart("Arm_L", hollow.transform, new Vector3(-0.22f, 0.9f, 0.04f),
            0.04f, 0.03f, 0.32f, 6, skinMat);
        CreateCylinderPart("Arm_R", hollow.transform, new Vector3(0.22f, 0.9f, 0.04f),
            0.04f, 0.03f, 0.32f, 6, skinMat);

        // Pernas finas
        CreateCylinderPart("Leg_L", hollow.transform, new Vector3(-0.09f, 0.35f, 0),
            0.05f, 0.04f, 0.36f, 6, hollowCloth);
        CreateCylinderPart("Leg_R", hollow.transform, new Vector3(0.09f, 0.35f, 0),
            0.05f, 0.04f, 0.36f, 6, hollowCloth);

        // Espada quebrada
        GameObject weapon = new GameObject("BrokenSword");
        weapon.transform.SetParent(hollow.transform);
        weapon.transform.localPosition = new Vector3(0.28f, 0.8f, 0.14f);
        weapon.transform.localRotation = Quaternion.Euler(-20, 0, -25);

        CreateMeshPart("Blade", weapon.transform, new Vector3(0, 0.25f, 0),
            new Vector3(0.03f, 0.3f, 0.01f), hollowWeapon, MeshShape.Box);
        CreateCylinderPart("Grip", weapon.transform, Vector3.zero,
            0.018f, 0.015f, 0.08f, 5, hollowCloth);

        return hollow;
    }

    // =================== MESH GENERATORS ====================

    enum MeshShape { Box, RoundedBox, Sphere }

    static GameObject CreateMeshPart(string name, Transform parent,
        Vector3 localPos, Vector3 scale, Material mat, MeshShape shape)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        Mesh mesh;
        switch (shape)
        {
            case MeshShape.Sphere:
                mesh = CreateSphereWithUVs(scale, 12, 8);
                break;
            case MeshShape.RoundedBox:
                mesh = CreateRoundedBoxMesh(scale, 0.02f);
                break;
            default:
                mesh = CreateBoxMesh(scale);
                break;
        }
        mesh.name = name + "_Mesh";
        mf.sharedMesh = mesh;

        return obj;
    }

    static GameObject CreateCylinderPart(string name, Transform parent,
        Vector3 localPos, float rBot, float rTop, float height, int segments, Material mat)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        Mesh mesh = CreateCylinderMesh(rBot, rTop, height, segments);
        mesh.name = name + "_Mesh";
        mf.sharedMesh = mesh;

        return obj;
    }

    static Mesh CreateBoxMesh(Vector3 size)
    {
        Vector3 h = size * 0.5f;
        Mesh mesh = new Mesh();

        Vector3[] verts = {
            // Front
            new Vector3(-h.x, -h.y, h.z), new Vector3(h.x, -h.y, h.z),
            new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z),
            // Back
            new Vector3(h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, -h.z),
            new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z),
            // Top
            new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z),
            new Vector3(h.x, h.y, -h.z), new Vector3(-h.x, h.y, -h.z),
            // Bottom
            new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z),
            new Vector3(h.x, -h.y, h.z), new Vector3(-h.x, -h.y, h.z),
            // Left
            new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, h.z),
            new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, h.y, -h.z),
            // Right
            new Vector3(h.x, -h.y, h.z), new Vector3(h.x, -h.y, -h.z),
            new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z)
        };

        Vector2[] uvs = new Vector2[24];
        for (int i = 0; i < 6; i++)
        {
            uvs[i * 4] = new Vector2(0, 0);
            uvs[i * 4 + 1] = new Vector2(1, 0);
            uvs[i * 4 + 2] = new Vector2(1, 1);
            uvs[i * 4 + 3] = new Vector2(0, 1);
        }

        int[] tris = {
            0,2,1, 0,3,2,
            4,6,5, 4,7,6,
            8,10,9, 8,11,10,
            12,14,13, 12,15,14,
            16,18,17, 16,19,18,
            20,22,21, 20,23,22
        };

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh CreateRoundedBoxMesh(Vector3 size, float bevel)
    {
        // Box simplificado com normais suaves para parecer arredondado
        Mesh box = CreateBoxMesh(size);
        // Smooth normals por vertex averaging
        Vector3[] normals = box.normals;
        Vector3[] verts = box.vertices;
        Dictionary<Vector3, Vector3> normalAccum = new Dictionary<Vector3, Vector3>();

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 key = new Vector3(
                Mathf.Round(verts[i].x * 1000f) / 1000f,
                Mathf.Round(verts[i].y * 1000f) / 1000f,
                Mathf.Round(verts[i].z * 1000f) / 1000f);
            if (normalAccum.ContainsKey(key))
                normalAccum[key] += normals[i];
            else
                normalAccum[key] = normals[i];
        }

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 key = new Vector3(
                Mathf.Round(verts[i].x * 1000f) / 1000f,
                Mathf.Round(verts[i].y * 1000f) / 1000f,
                Mathf.Round(verts[i].z * 1000f) / 1000f);
            normals[i] = normalAccum[key].normalized;
        }
        box.normals = normals;
        return box;
    }

    static Mesh CreateSphereWithUVs(Vector3 scale, int longSegments, int latSegments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int lat = 0; lat <= latSegments; lat++)
        {
            float theta = Mathf.PI * lat / latSegments;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lon = 0; lon <= longSegments; lon++)
            {
                float phi = 2f * Mathf.PI * lon / longSegments;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                Vector3 v = new Vector3(
                    cosPhi * sinTheta * scale.x * 0.5f,
                    cosTheta * scale.y * 0.5f,
                    sinPhi * sinTheta * scale.z * 0.5f);

                verts.Add(v);
                uvs.Add(new Vector2((float)lon / longSegments, (float)lat / latSegments));
            }
        }

        for (int lat = 0; lat < latSegments; lat++)
        {
            for (int lon = 0; lon < longSegments; lon++)
            {
                int first = lat * (longSegments + 1) + lon;
                int second = first + longSegments + 1;

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

    static Mesh CreateCylinderMesh(float rBot, float rTop, float height, int segments)
    {
        Mesh mesh = new Mesh();
        float halfH = height * 0.5f;
        int vertCount = (segments + 1) * 2 + 2;
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

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

        int capIdx = (segments + 1) * 2;
        verts[capIdx] = new Vector3(0, -halfH, 0);
        uvs[capIdx] = new Vector2(0.5f, 0);
        verts[capIdx + 1] = new Vector3(0, halfH, 0);
        uvs[capIdx + 1] = new Vector2(0.5f, 1);

        List<int> tris = new List<int>();

        for (int i = 0; i < segments; i++)
        {
            int bl = i, br = i + 1;
            int tl = i + segments + 1, tr = i + segments + 2;
            tris.Add(bl); tris.Add(tl); tris.Add(br);
            tris.Add(br); tris.Add(tl); tris.Add(tr);
        }

        for (int i = 0; i < segments; i++)
        {
            tris.Add(capIdx); tris.Add((i + 1) % (segments + 1)); tris.Add(i);
            tris.Add(capIdx + 1); tris.Add(i + segments + 1); tris.Add(((i + 1) % (segments + 1)) + segments + 1);
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // =================== TEXTURE GENERATORS ====================

    static Texture2D GenerateMetalTexture()
    {
        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, true);
        tex.name = "T_Metal_Procedural";
        float ox = Random.Range(0f, 1000f), oy = Random.Range(0f, 1000f);

        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float nx = (float)x / 256f;
                float ny = (float)y / 256f;
                float n1 = Mathf.PerlinNoise((nx + ox) * 30f, (ny + oy) * 30f);
                float n2 = Mathf.PerlinNoise((nx + ox) * 80f, (ny + oy) * 80f) * 0.3f;
                float brushed = Mathf.PerlinNoise((nx + ox) * 200f, (ny + oy) * 5f) * 0.15f;
                float v = 0.35f + (n1 + n2 + brushed) * 0.25f;
                tex.SetPixel(x, y, new Color(v, v, v * 1.02f, 1f));
            }
        }
        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    static Texture2D GenerateLeatherTexture()
    {
        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, true);
        tex.name = "T_Leather_Procedural";
        float ox = Random.Range(0f, 1000f), oy = Random.Range(0f, 1000f);

        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float nx = (float)x / 256f;
                float ny = (float)y / 256f;
                float n1 = Mathf.PerlinNoise((nx + ox) * 20f, (ny + oy) * 20f);
                float n2 = Mathf.PerlinNoise((nx + ox) * 60f, (ny + oy) * 60f) * 0.25f;
                float grain = Mathf.PerlinNoise((nx + ox) * 150f, (ny + oy) * 150f) * 0.1f;
                float v = 0.2f + (n1 + n2 + grain) * 0.2f;
                tex.SetPixel(x, y, new Color(v * 1.2f, v, v * 0.7f, 1f));
            }
        }
        tex.Apply(true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }
}
