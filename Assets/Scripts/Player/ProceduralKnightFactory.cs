using UnityEngine;

/// <summary>
/// Gera um modelo de cavaleiro/guerreiro procedural usando primitivas.
/// Corpo articulado: cabeça, torso, braços, pernas, espada, escudo, capa.
/// Muito mais detalhado que uma simples cápsula.
/// </summary>
public static class ProceduralKnightFactory
{
    public static GameObject CreateKnight(Vector3 position)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // Materiais
        Material armorMat = new Material(urpLit);
        armorMat.name = "M_Armor";
        armorMat.color = new Color(0.25f, 0.25f, 0.28f);
        armorMat.SetFloat("_Smoothness", 0.65f);
        armorMat.SetFloat("_Metallic", 0.8f);

        Material chainmailMat = new Material(urpLit);
        chainmailMat.name = "M_Chainmail";
        chainmailMat.color = new Color(0.3f, 0.3f, 0.32f);
        chainmailMat.SetFloat("_Smoothness", 0.4f);
        chainmailMat.SetFloat("_Metallic", 0.6f);

        Material leatherMat = new Material(urpLit);
        leatherMat.name = "M_Leather";
        leatherMat.color = new Color(0.2f, 0.13f, 0.07f);
        leatherMat.SetFloat("_Smoothness", 0.2f);

        Material skinMat = new Material(urpLit);
        skinMat.name = "M_Skin";
        skinMat.color = new Color(0.6f, 0.45f, 0.35f);
        skinMat.SetFloat("_Smoothness", 0.3f);

        Material swordMat = new Material(urpLit);
        swordMat.name = "M_Sword";
        swordMat.color = new Color(0.7f, 0.7f, 0.75f);
        swordMat.SetFloat("_Smoothness", 0.9f);
        swordMat.SetFloat("_Metallic", 1f);

        Material shieldMat = new Material(urpLit);
        shieldMat.name = "M_Shield";
        shieldMat.color = new Color(0.15f, 0.12f, 0.08f);
        shieldMat.SetFloat("_Smoothness", 0.3f);

        Material capeMat = new Material(urpLit);
        capeMat.name = "M_Cape";
        capeMat.color = new Color(0.12f, 0.05f, 0.05f); // Vermelho escuro
        capeMat.SetFloat("_Smoothness", 0.1f);

        Material helmetVisorMat = new Material(urpLit);
        helmetVisorMat.name = "M_HelmetVisor";
        helmetVisorMat.color = new Color(0.02f, 0.02f, 0.02f);
        helmetVisorMat.SetFloat("_Smoothness", 0.8f);

        // ========= CONSTRUÇÃO DO MODELO =========

        GameObject knight = new GameObject("KnightModel");
        knight.transform.position = position;

        // --- TORSO (peito) ---
        GameObject torso = CreatePart("Torso", knight.transform,
            new Vector3(0, 1.15f, 0), new Vector3(0.55f, 0.4f, 0.3f),
            PrimitiveType.Cube, armorMat);

        // Ombreira esquerda
        CreatePart("ShoulderPad_L", torso.transform,
            new Vector3(-0.32f, 0.15f, 0), new Vector3(0.18f, 0.12f, 0.22f),
            PrimitiveType.Sphere, armorMat);

        // Ombreira direita
        CreatePart("ShoulderPad_R", torso.transform,
            new Vector3(0.32f, 0.15f, 0), new Vector3(0.18f, 0.12f, 0.22f),
            PrimitiveType.Sphere, armorMat);

        // --- ABDÔMEN ---
        CreatePart("Abdomen", knight.transform,
            new Vector3(0, 0.9f, 0), new Vector3(0.45f, 0.2f, 0.25f),
            PrimitiveType.Cube, chainmailMat);

        // --- CINTO ---
        CreatePart("Belt", knight.transform,
            new Vector3(0, 0.78f, 0), new Vector3(0.5f, 0.06f, 0.28f),
            PrimitiveType.Cube, leatherMat);

        // --- CABEÇA + ELMO ---
        GameObject head = CreatePart("Head", knight.transform,
            new Vector3(0, 1.55f, 0), new Vector3(0.22f, 0.24f, 0.22f),
            PrimitiveType.Sphere, armorMat);

        // Visor do elmo
        CreatePart("Visor", head.transform,
            new Vector3(0, -0.02f, 0.08f), new Vector3(0.16f, 0.06f, 0.06f),
            PrimitiveType.Cube, helmetVisorMat);

        // Crista do elmo
        CreatePart("HelmetCrest", head.transform,
            new Vector3(0, 0.12f, -0.02f), new Vector3(0.04f, 0.06f, 0.14f),
            PrimitiveType.Cube, armorMat);

        // Pescoço
        CreatePart("Neck", knight.transform,
            new Vector3(0, 1.4f, 0), new Vector3(0.12f, 0.06f, 0.12f),
            PrimitiveType.Cylinder, chainmailMat);

        // --- BRAÇO ESQUERDO (escudo) ---
        CreatePart("UpperArm_L", knight.transform,
            new Vector3(-0.35f, 1.05f, 0), new Vector3(0.12f, 0.18f, 0.12f),
            PrimitiveType.Cylinder, chainmailMat);

        CreatePart("LowerArm_L", knight.transform,
            new Vector3(-0.38f, 0.82f, 0.05f), new Vector3(0.1f, 0.16f, 0.1f),
            PrimitiveType.Cylinder, armorMat);

        CreatePart("Hand_L", knight.transform,
            new Vector3(-0.38f, 0.68f, 0.08f), new Vector3(0.08f, 0.06f, 0.1f),
            PrimitiveType.Cube, leatherMat);

        // --- BRAÇO DIREITO (espada) ---
        CreatePart("UpperArm_R", knight.transform,
            new Vector3(0.35f, 1.05f, 0), new Vector3(0.12f, 0.18f, 0.12f),
            PrimitiveType.Cylinder, chainmailMat);

        CreatePart("LowerArm_R", knight.transform,
            new Vector3(0.38f, 0.82f, 0.05f), new Vector3(0.1f, 0.16f, 0.1f),
            PrimitiveType.Cylinder, armorMat);

        CreatePart("Hand_R", knight.transform,
            new Vector3(0.38f, 0.68f, 0.08f), new Vector3(0.08f, 0.06f, 0.1f),
            PrimitiveType.Cube, leatherMat);

        // --- PERNAS ---
        // Coxa esquerda
        CreatePart("Thigh_L", knight.transform,
            new Vector3(-0.15f, 0.58f, 0), new Vector3(0.14f, 0.2f, 0.14f),
            PrimitiveType.Cylinder, chainmailMat);

        // Canela esquerda
        CreatePart("Shin_L", knight.transform,
            new Vector3(-0.15f, 0.3f, 0), new Vector3(0.11f, 0.2f, 0.11f),
            PrimitiveType.Cylinder, armorMat);

        // Bota esquerda
        CreatePart("Boot_L", knight.transform,
            new Vector3(-0.15f, 0.08f, 0.03f), new Vector3(0.12f, 0.08f, 0.18f),
            PrimitiveType.Cube, leatherMat);

        // Coxa direita
        CreatePart("Thigh_R", knight.transform,
            new Vector3(0.15f, 0.58f, 0), new Vector3(0.14f, 0.2f, 0.14f),
            PrimitiveType.Cylinder, chainmailMat);

        // Canela direita
        CreatePart("Shin_R", knight.transform,
            new Vector3(0.15f, 0.3f, 0), new Vector3(0.11f, 0.2f, 0.11f),
            PrimitiveType.Cylinder, armorMat);

        // Bota direita
        CreatePart("Boot_R", knight.transform,
            new Vector3(0.15f, 0.08f, 0.03f), new Vector3(0.12f, 0.08f, 0.18f),
            PrimitiveType.Cube, leatherMat);

        // --- ESPADA (mão direita) ---
        GameObject swordObj = new GameObject("Sword");
        swordObj.transform.SetParent(knight.transform);
        swordObj.transform.localPosition = new Vector3(0.42f, 0.95f, 0.2f);
        swordObj.transform.localRotation = Quaternion.Euler(-10, 0, -15);

        // Lâmina
        CreatePart("Blade", swordObj.transform,
            new Vector3(0, 0.45f, 0), new Vector3(0.04f, 0.5f, 0.015f),
            PrimitiveType.Cube, swordMat);

        // Guarda
        CreatePart("Guard", swordObj.transform,
            new Vector3(0, 0.15f, 0), new Vector3(0.15f, 0.02f, 0.03f),
            PrimitiveType.Cube, leatherMat);

        // Punho
        CreatePart("Grip", swordObj.transform,
            new Vector3(0, 0.05f, 0), new Vector3(0.03f, 0.1f, 0.03f),
            PrimitiveType.Cylinder, leatherMat);

        // Pommel
        CreatePart("Pommel", swordObj.transform,
            new Vector3(0, -0.02f, 0), new Vector3(0.04f, 0.04f, 0.04f),
            PrimitiveType.Sphere, swordMat);

        // --- ESCUDO (mão esquerda) ---
        GameObject shieldObj = new GameObject("Shield");
        shieldObj.transform.SetParent(knight.transform);
        shieldObj.transform.localPosition = new Vector3(-0.48f, 0.85f, 0.15f);
        shieldObj.transform.localRotation = Quaternion.Euler(0, 10, 5);

        // Corpo do escudo
        CreatePart("ShieldBody", shieldObj.transform,
            Vector3.zero, new Vector3(0.06f, 0.4f, 0.32f),
            PrimitiveType.Cube, shieldMat);

        // Borda do escudo
        CreatePart("ShieldRim_Top", shieldObj.transform,
            new Vector3(0.01f, 0.2f, 0), new Vector3(0.065f, 0.02f, 0.34f),
            PrimitiveType.Cube, armorMat);

        CreatePart("ShieldRim_Bot", shieldObj.transform,
            new Vector3(0.01f, -0.2f, 0), new Vector3(0.065f, 0.02f, 0.34f),
            PrimitiveType.Cube, armorMat);

        // Boss/emblema central do escudo
        CreatePart("ShieldBoss", shieldObj.transform,
            new Vector3(0.035f, 0, 0), new Vector3(0.05f, 0.1f, 0.1f),
            PrimitiveType.Sphere, armorMat);

        // --- CAPA ---
        CreatePart("Cape_Upper", knight.transform,
            new Vector3(0, 1.15f, -0.18f), new Vector3(0.45f, 0.35f, 0.05f),
            PrimitiveType.Cube, capeMat);

        CreatePart("Cape_Lower", knight.transform,
            new Vector3(0, 0.65f, -0.2f), new Vector3(0.4f, 0.5f, 0.04f),
            PrimitiveType.Cube, capeMat);

        return knight;
    }

    /// <summary>
    /// Cria um modelo de inimigo estilo hollow/undead.
    /// </summary>
    public static GameObject CreateHollow(Vector3 position)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Material hollowSkin = new Material(urpLit);
        hollowSkin.name = "M_HollowSkin";
        hollowSkin.color = new Color(0.2f, 0.18f, 0.15f);
        hollowSkin.SetFloat("_Smoothness", 0.15f);

        Material hollowCloth = new Material(urpLit);
        hollowCloth.name = "M_HollowCloth";
        hollowCloth.color = new Color(0.12f, 0.1f, 0.08f);
        hollowCloth.SetFloat("_Smoothness", 0.05f);

        Material hollowWeapon = new Material(urpLit);
        hollowWeapon.name = "M_HollowWeapon";
        hollowWeapon.color = new Color(0.35f, 0.3f, 0.25f);
        hollowWeapon.SetFloat("_Smoothness", 0.3f);

        GameObject hollow = new GameObject("HollowModel");
        hollow.transform.position = position;

        // Corpo magro e curvado
        CreatePart("HollowTorso", hollow.transform,
            new Vector3(0, 1f, 0.03f), new Vector3(0.35f, 0.35f, 0.2f),
            PrimitiveType.Cube, hollowCloth);

        CreatePart("HollowAbdomen", hollow.transform,
            new Vector3(0, 0.75f, 0), new Vector3(0.3f, 0.15f, 0.18f),
            PrimitiveType.Cube, hollowSkin);

        // Cabeça (magra, hollow)
        CreatePart("HollowHead", hollow.transform,
            new Vector3(0.02f, 1.4f, 0.02f), new Vector3(0.17f, 0.2f, 0.17f),
            PrimitiveType.Sphere, hollowSkin);

        // Olhos (emissivos vermelhos)
        Material eyeMat = new Material(urpLit);
        eyeMat.color = Color.red;
        eyeMat.EnableKeyword("_EMISSION");
        eyeMat.SetColor("_EmissionColor", new Color(0.8f, 0.1f, 0f) * 2f);

        CreatePart("Eye_L", hollow.transform,
            new Vector3(-0.05f, 1.43f, 0.08f), Vector3.one * 0.025f,
            PrimitiveType.Sphere, eyeMat);

        CreatePart("Eye_R", hollow.transform,
            new Vector3(0.05f, 1.43f, 0.08f), Vector3.one * 0.025f,
            PrimitiveType.Sphere, eyeMat);

        // Braços finos
        CreatePart("HollowArm_L", hollow.transform,
            new Vector3(-0.25f, 0.9f, 0.05f), new Vector3(0.07f, 0.3f, 0.07f),
            PrimitiveType.Cylinder, hollowSkin);

        CreatePart("HollowArm_R", hollow.transform,
            new Vector3(0.25f, 0.9f, 0.05f), new Vector3(0.07f, 0.3f, 0.07f),
            PrimitiveType.Cylinder, hollowSkin);

        // Pernas finas
        CreatePart("HollowLeg_L", hollow.transform,
            new Vector3(-0.1f, 0.35f, 0), new Vector3(0.08f, 0.35f, 0.08f),
            PrimitiveType.Cylinder, hollowCloth);

        CreatePart("HollowLeg_R", hollow.transform,
            new Vector3(0.1f, 0.35f, 0), new Vector3(0.08f, 0.35f, 0.08f),
            PrimitiveType.Cylinder, hollowCloth);

        // Arma: espada quebrada/enferrujada
        GameObject weapon = new GameObject("HollowWeapon");
        weapon.transform.SetParent(hollow.transform);
        weapon.transform.localPosition = new Vector3(0.3f, 0.8f, 0.15f);
        weapon.transform.localRotation = Quaternion.Euler(-20, 0, -25);

        CreatePart("BrokenBlade", weapon.transform,
            new Vector3(0, 0.3f, 0), new Vector3(0.035f, 0.35f, 0.012f),
            PrimitiveType.Cube, hollowWeapon);

        CreatePart("WeaponGrip", weapon.transform,
            Vector3.zero, new Vector3(0.025f, 0.08f, 0.025f),
            PrimitiveType.Cylinder, hollowCloth);

        return hollow;
    }

    private static GameObject CreatePart(string name, Transform parent,
        Vector3 localPos, Vector3 scale, PrimitiveType type, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent);
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;

        if (mat != null)
            part.GetComponent<Renderer>().sharedMaterial = mat;

        // Remover colliders das partes (collider será no root)
        Collider col = part.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Object.Destroy(col);
            else Object.DestroyImmediate(col);
        }

        return part;
    }
}
