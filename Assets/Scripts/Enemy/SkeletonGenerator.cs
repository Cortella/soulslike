using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gera um modelo de Caveira (Skeleton) Nível 1 com mesh real.
/// Corpo esquelético com ossos, crânio, olhos emissivos vermelhos,
/// espada enferrujada. Primeiro mob do jogo.
/// </summary>
public static class SkeletonGenerator
{
    private static Material boneMat;
    private static Material boneJointMat;
    private static Material skullMat;
    private static Material eyeGlowMat;
    private static Material rustyWeaponMat;
    private static Material ragMat; // trapos de roupa

    public static void InitMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // Osso — bege/amarelado
        boneMat = new Material(urpLit);
        boneMat.name = "M_Bone";
        boneMat.color = new Color(0.72f, 0.65f, 0.5f);
        boneMat.SetFloat("_Smoothness", 0.3f);
        boneMat.SetFloat("_Metallic", 0.05f);
        boneMat.enableInstancing = true;

        // Juntas — mais escuras
        boneJointMat = new Material(urpLit);
        boneJointMat.name = "M_BoneJoint";
        boneJointMat.color = new Color(0.55f, 0.48f, 0.35f);
        boneJointMat.SetFloat("_Smoothness", 0.2f);
        boneJointMat.enableInstancing = true;

        // Crânio — ligeiramente diferente
        skullMat = new Material(urpLit);
        skullMat.name = "M_Skull";
        skullMat.color = new Color(0.78f, 0.7f, 0.55f);
        skullMat.SetFloat("_Smoothness", 0.25f);
        skullMat.enableInstancing = true;

        // Olhos emissivos vermelhos
        eyeGlowMat = new Material(urpLit);
        eyeGlowMat.name = "M_SkeletonEyes";
        eyeGlowMat.color = new Color(1f, 0.1f, 0f);
        eyeGlowMat.EnableKeyword("_EMISSION");
        eyeGlowMat.SetColor("_EmissionColor", new Color(1f, 0.15f, 0f) * 4f);
        eyeGlowMat.enableInstancing = true;

        // Espada enferrujada
        rustyWeaponMat = new Material(urpLit);
        rustyWeaponMat.name = "M_RustyBlade";
        rustyWeaponMat.color = new Color(0.35f, 0.25f, 0.15f);
        rustyWeaponMat.SetFloat("_Smoothness", 0.15f);
        rustyWeaponMat.SetFloat("_Metallic", 0.5f);
        rustyWeaponMat.enableInstancing = true;

        // Trapos
        ragMat = new Material(urpLit);
        ragMat.name = "M_Rag";
        ragMat.color = new Color(0.18f, 0.15f, 0.1f);
        ragMat.SetFloat("_Smoothness", 0.05f);
        ragMat.enableInstancing = true;
    }

    /// <summary>
    /// Cria um Skeleton Level 1 completo na posição dada.
    /// Retorna o GameObject root.
    /// </summary>
    public static GameObject CreateSkeleton(Vector3 position)
    {
        if (boneMat == null) InitMaterials();

        GameObject skeleton = new GameObject("Skeleton_Lv1");
        skeleton.transform.position = position;

        // === CRÂNIO ===
        CreatePart("Skull", skeleton.transform, new Vector3(0, 1.55f, 0),
            new Vector3(0.16f, 0.18f, 0.16f), skullMat, PShape.Sphere);

        // Mandíbula
        CreatePart("Jaw", skeleton.transform, new Vector3(0, 1.46f, 0.04f),
            new Vector3(0.1f, 0.05f, 0.08f), skullMat, PShape.Box);

        // Olhos emissivos
        CreatePart("Eye_L", skeleton.transform, new Vector3(-0.04f, 1.57f, 0.07f),
            Vector3.one * 0.025f, eyeGlowMat, PShape.Sphere);
        CreatePart("Eye_R", skeleton.transform, new Vector3(0.04f, 1.57f, 0.07f),
            Vector3.one * 0.025f, eyeGlowMat, PShape.Sphere);

        // Light nos olhos
        GameObject eyeLight = new GameObject("EyeLight");
        eyeLight.transform.SetParent(skeleton.transform);
        eyeLight.transform.localPosition = new Vector3(0, 1.57f, 0.1f);
        Light el = eyeLight.AddComponent<Light>();
        el.type = LightType.Point;
        el.color = new Color(1f, 0.15f, 0f);
        el.intensity = 0.6f;
        el.range = 1.5f;
        el.shadows = LightShadows.None;

        // === PESCOÇO (3 vértebras) ===
        for (int i = 0; i < 3; i++)
        {
            float y = 1.42f - i * 0.04f;
            float scale = 0.03f - i * 0.003f;
            CreateCyl("Neck_" + i, skeleton.transform, new Vector3(0, y, 0),
                scale, scale * 0.9f, 0.035f, 6, boneJointMat);
        }

        // === COLUNA VERTEBRAL (vértebras) ===
        for (int i = 0; i < 8; i++)
        {
            float y = 1.28f - i * 0.05f;
            float s = 0.035f + Mathf.Sin(i * 0.4f) * 0.005f;
            CreateCyl("Spine_" + i, skeleton.transform, new Vector3(0, y, 0),
                s, s * 0.85f, 0.04f, 6, boneMat);
        }

        // === COSTELAS ===
        for (int i = 0; i < 5; i++)
        {
            float y = 1.25f - i * 0.06f;
            float ribWidth = 0.12f + i * 0.008f;
            float ribDepth = 0.08f + i * 0.005f;

            // Rib left
            CreatePart($"Rib_L_{i}", skeleton.transform, new Vector3(-ribWidth * 0.5f, y, 0),
                new Vector3(ribWidth, 0.015f, ribDepth), boneMat, PShape.RoundedBox);
            // Rib right
            CreatePart($"Rib_R_{i}", skeleton.transform, new Vector3(ribWidth * 0.5f, y, 0),
                new Vector3(ribWidth, 0.015f, ribDepth), boneMat, PShape.RoundedBox);
        }

        // === PELVIS ===
        CreatePart("Pelvis", skeleton.transform, new Vector3(0, 0.83f, 0),
            new Vector3(0.2f, 0.08f, 0.12f), boneMat, PShape.RoundedBox);

        // === BRAÇO ESQUERDO ===
        // Clavícula
        CreateCyl("CollarBone_L", skeleton.transform, new Vector3(-0.12f, 1.28f, 0),
            0.02f, 0.015f, 0.12f, 5, boneMat);
        // Úmero
        CreateCyl("Humerus_L", skeleton.transform, new Vector3(-0.22f, 1.1f, 0),
            0.025f, 0.02f, 0.22f, 6, boneMat);
        // Cotovelo
        CreatePart("Elbow_L", skeleton.transform, new Vector3(-0.22f, 0.98f, 0),
            Vector3.one * 0.025f, boneJointMat, PShape.Sphere);
        // Rádio/Ulna
        CreateCyl("Forearm_L", skeleton.transform, new Vector3(-0.24f, 0.82f, 0.02f),
            0.02f, 0.015f, 0.2f, 5, boneMat);
        // Mão esquelética
        CreatePart("Hand_L", skeleton.transform, new Vector3(-0.24f, 0.7f, 0.04f),
            new Vector3(0.04f, 0.02f, 0.06f), boneJointMat, PShape.Box);

        // === BRAÇO DIREITO ===
        CreateCyl("CollarBone_R", skeleton.transform, new Vector3(0.12f, 1.28f, 0),
            0.02f, 0.015f, 0.12f, 5, boneMat);
        CreateCyl("Humerus_R", skeleton.transform, new Vector3(0.22f, 1.1f, 0),
            0.025f, 0.02f, 0.22f, 6, boneMat);
        CreatePart("Elbow_R", skeleton.transform, new Vector3(0.22f, 0.98f, 0),
            Vector3.one * 0.025f, boneJointMat, PShape.Sphere);
        CreateCyl("Forearm_R", skeleton.transform, new Vector3(0.24f, 0.82f, 0.02f),
            0.02f, 0.015f, 0.2f, 5, boneMat);
        CreatePart("Hand_R", skeleton.transform, new Vector3(0.24f, 0.7f, 0.04f),
            new Vector3(0.04f, 0.02f, 0.06f), boneJointMat, PShape.Box);

        // === PERNA ESQUERDA ===
        // Fêmur
        CreateCyl("Femur_L", skeleton.transform, new Vector3(-0.1f, 0.62f, 0),
            0.03f, 0.025f, 0.25f, 6, boneMat);
        // Joelho
        CreatePart("Knee_L", skeleton.transform, new Vector3(-0.1f, 0.48f, 0.01f),
            Vector3.one * 0.03f, boneJointMat, PShape.Sphere);
        // Tíbia/Fíbula
        CreateCyl("Shin_L", skeleton.transform, new Vector3(-0.1f, 0.28f, 0),
            0.025f, 0.02f, 0.25f, 6, boneMat);
        // Pé esquelético
        CreatePart("Foot_L", skeleton.transform, new Vector3(-0.1f, 0.04f, 0.03f),
            new Vector3(0.04f, 0.025f, 0.1f), boneJointMat, PShape.Box);

        // === PERNA DIREITA ===
        CreateCyl("Femur_R", skeleton.transform, new Vector3(0.1f, 0.62f, 0),
            0.03f, 0.025f, 0.25f, 6, boneMat);
        CreatePart("Knee_R", skeleton.transform, new Vector3(0.1f, 0.48f, 0.01f),
            Vector3.one * 0.03f, boneJointMat, PShape.Sphere);
        CreateCyl("Shin_R", skeleton.transform, new Vector3(0.1f, 0.28f, 0),
            0.025f, 0.02f, 0.25f, 6, boneMat);
        CreatePart("Foot_R", skeleton.transform, new Vector3(0.1f, 0.04f, 0.03f),
            new Vector3(0.04f, 0.025f, 0.1f), boneJointMat, PShape.Box);

        // === TRAPOS DE ROUPA ===
        // Tanga/saia rasgada
        CreatePart("Rag_Skirt", skeleton.transform, new Vector3(0, 0.72f, 0),
            new Vector3(0.22f, 0.2f, 0.14f), ragMat, PShape.Box);

        // Tiras no torso
        CreatePart("Rag_Chest", skeleton.transform, new Vector3(-0.05f, 1.15f, 0.03f),
            new Vector3(0.18f, 0.08f, 0.02f), ragMat, PShape.Box);

        // === ESPADA ENFERRUJADA ===
        GameObject sword = new GameObject("RustySword");
        sword.transform.SetParent(skeleton.transform);
        sword.transform.localPosition = new Vector3(0.3f, 0.85f, 0.15f);
        sword.transform.localRotation = Quaternion.Euler(-15, 0, -20);

        // Lâmina
        CreatePart("Blade", sword.transform, new Vector3(0, 0.3f, 0),
            new Vector3(0.03f, 0.38f, 0.008f), rustyWeaponMat, PShape.Box);
        // Guarda
        CreatePart("Guard", sword.transform, new Vector3(0, 0.08f, 0),
            new Vector3(0.08f, 0.015f, 0.02f), rustyWeaponMat, PShape.Box);
        // Empunhadura
        CreateCyl("Grip", sword.transform, Vector3.zero,
            0.015f, 0.012f, 0.08f, 5, ragMat);

        return skeleton;
    }

    // =================== MESH HELPERS ====================

    enum PShape { Box, RoundedBox, Sphere }

    static GameObject CreatePart(string name, Transform parent,
        Vector3 localPos, Vector3 scale, Material mat, PShape shape)
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
            case PShape.Sphere:
                mesh = CreateSphereMesh(scale, 8, 6);
                break;
            case PShape.RoundedBox:
                mesh = CreateBoxMesh(scale, true);
                break;
            default:
                mesh = CreateBoxMesh(scale, false);
                break;
        }
        mesh.name = name + "_Mesh";
        mf.sharedMesh = mesh;
        return obj;
    }

    static GameObject CreateCyl(string name, Transform parent,
        Vector3 localPos, float rBot, float rTop, float height, int segs, Material mat)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        Mesh mesh = new Mesh();
        float halfH = height * 0.5f;
        Vector3[] verts = new Vector3[(segs + 1) * 2];
        Vector2[] uvs = new Vector2[(segs + 1) * 2];

        for (int i = 0; i <= segs; i++)
        {
            float a = (float)i / segs * Mathf.PI * 2f;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            verts[i] = new Vector3(c * rBot, -halfH, s * rBot);
            uvs[i] = new Vector2((float)i / segs, 0);
            verts[i + segs + 1] = new Vector3(c * rTop, halfH, s * rTop);
            uvs[i + segs + 1] = new Vector2((float)i / segs, 1);
        }

        List<int> tris = new List<int>();
        for (int i = 0; i < segs; i++)
        {
            int bl = i, br = i + 1, tl = i + segs + 1, tr = i + segs + 2;
            tris.Add(bl); tris.Add(tl); tris.Add(br);
            tris.Add(br); tris.Add(tl); tris.Add(tr);
        }

        mesh.vertices = verts; mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.name = name + "_Mesh";
        mf.sharedMesh = mesh;
        return obj;
    }

    static Mesh CreateBoxMesh(Vector3 size, bool smoothNormals)
    {
        Vector3 h = size * 0.5f;
        Mesh mesh = new Mesh();

        Vector3[] verts = {
            new Vector3(-h.x,-h.y, h.z), new Vector3( h.x,-h.y, h.z),
            new Vector3( h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z),
            new Vector3( h.x,-h.y,-h.z), new Vector3(-h.x,-h.y,-h.z),
            new Vector3(-h.x, h.y,-h.z), new Vector3( h.x, h.y,-h.z),
            new Vector3(-h.x, h.y, h.z), new Vector3( h.x, h.y, h.z),
            new Vector3( h.x, h.y,-h.z), new Vector3(-h.x, h.y,-h.z),
            new Vector3(-h.x,-h.y,-h.z), new Vector3( h.x,-h.y,-h.z),
            new Vector3( h.x,-h.y, h.z), new Vector3(-h.x,-h.y, h.z),
            new Vector3(-h.x,-h.y,-h.z), new Vector3(-h.x,-h.y, h.z),
            new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, h.y,-h.z),
            new Vector3( h.x,-h.y, h.z), new Vector3( h.x,-h.y,-h.z),
            new Vector3( h.x, h.y,-h.z), new Vector3( h.x, h.y, h.z)
        };
        Vector2[] uvs = new Vector2[24];
        for (int i = 0; i < 6; i++)
        {
            uvs[i * 4] = new Vector2(0, 0); uvs[i * 4 + 1] = new Vector2(1, 0);
            uvs[i * 4 + 2] = new Vector2(1, 1); uvs[i * 4 + 3] = new Vector2(0, 1);
        }
        int[] tris = {
            0,2,1, 0,3,2, 4,6,5, 4,7,6, 8,10,9, 8,11,10,
            12,14,13, 12,15,14, 16,18,17, 16,19,18, 20,22,21, 20,23,22
        };

        mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
        mesh.RecalculateNormals();

        if (smoothNormals)
        {
            Vector3[] normals = mesh.normals;
            Dictionary<Vector3, Vector3> acc = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 k = new Vector3(
                    Mathf.Round(verts[i].x * 1000f) / 1000f,
                    Mathf.Round(verts[i].y * 1000f) / 1000f,
                    Mathf.Round(verts[i].z * 1000f) / 1000f);
                if (acc.ContainsKey(k)) acc[k] += normals[i]; else acc[k] = normals[i];
            }
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 k = new Vector3(
                    Mathf.Round(verts[i].x * 1000f) / 1000f,
                    Mathf.Round(verts[i].y * 1000f) / 1000f,
                    Mathf.Round(verts[i].z * 1000f) / 1000f);
                normals[i] = acc[k].normalized;
            }
            mesh.normals = normals;
        }

        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh CreateSphereMesh(Vector3 scale, int lon, int lat)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int la = 0; la <= lat; la++)
        {
            float theta = Mathf.PI * la / lat;
            float sinT = Mathf.Sin(theta), cosT = Mathf.Cos(theta);
            for (int lo = 0; lo <= lon; lo++)
            {
                float phi = 2f * Mathf.PI * lo / lon;
                verts.Add(new Vector3(
                    Mathf.Cos(phi) * sinT * scale.x * 0.5f,
                    cosT * scale.y * 0.5f,
                    Mathf.Sin(phi) * sinT * scale.z * 0.5f));
                uvs.Add(new Vector2((float)lo / lon, (float)la / lat));
            }
        }

        for (int la = 0; la < lat; la++)
            for (int lo = 0; lo < lon; lo++)
            {
                int f = la * (lon + 1) + lo, s = f + lon + 1;
                tris.Add(f); tris.Add(s); tris.Add(f + 1);
                tris.Add(s); tris.Add(s + 1); tris.Add(f + 1);
            }

        mesh.SetVertices(verts); mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }
}
