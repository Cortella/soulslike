using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gera árvores procedurais com variação visual realista.
/// Cria troncos, galhos e copas usando meshes combinadas.
/// Cada árvore é única usando variação de seed.
/// </summary>
public static class ProceduralTreeFactory
{
    /// <summary>
    /// Cria uma árvore completa como GameObject.
    /// </summary>
    public static GameObject CreateTree(Vector3 position, int seed = -1, TreeStyle style = TreeStyle.Oak)
    {
        if (seed < 0) seed = Random.Range(0, 99999);
        System.Random rng = new System.Random(seed);

        GameObject tree = new GameObject($"Tree_{style}_{seed}");
        tree.transform.position = position;
        tree.isStatic = true;

        float heightVar = NextFloat(rng, 0.7f, 1.3f);

        switch (style)
        {
            case TreeStyle.Oak:
                BuildOakTree(tree, rng, heightVar);
                break;
            case TreeStyle.Pine:
                BuildPineTree(tree, rng, heightVar);
                break;
            case TreeStyle.DeadTree:
                BuildDeadTree(tree, rng, heightVar);
                break;
            case TreeStyle.Willow:
                BuildWillowTree(tree, rng, heightVar);
                break;
            case TreeStyle.TallPine:
                BuildTallPine(tree, rng, heightVar);
                break;
        }

        return tree;
    }

    /// <summary>
    /// Cria um prefab de árvore e retorna (para uso no Terrain).
    /// </summary>
    public static GameObject CreateTreePrefab(TreeStyle style, Material trunkMat, Material leavesMat)
    {
        GameObject prefab = CreateTree(Vector3.zero, 42, style);

        // Aplicar materiais
        ApplyMaterials(prefab, trunkMat, leavesMat);

        return prefab;
    }

    #region Tree Styles

    private static void BuildOakTree(GameObject parent, System.Random rng, float scale)
    {
        float trunkHeight = 4f * scale;
        float trunkRadius = 0.25f * scale;

        // Tronco principal
        GameObject trunk = CreateCylinder("Trunk", parent.transform,
            Vector3.up * (trunkHeight * 0.5f),
            new Vector3(trunkRadius * 2, trunkHeight * 0.5f, trunkRadius * 2));

        // Raízes visíveis na base
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f + NextFloat(rng, -20f, 20f);
            Vector3 rootDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            GameObject root = CreateCylinder($"Root_{i}", parent.transform,
                rootDir * 0.3f * scale + Vector3.up * 0.15f * scale,
                new Vector3(0.12f * scale, 0.2f * scale, 0.12f * scale));
            root.transform.rotation = Quaternion.Euler(NextFloat(rng, 50, 70), angle, 0);
        }

        // Copa (várias esferas sobrepostas = aparência orgânica)
        Vector3 canopyCenter = Vector3.up * (trunkHeight + 0.5f * scale);
        float canopyRadius = 2.5f * scale;

        // Esfera central grande
        CreateSphere("Canopy_Main", parent.transform,
            canopyCenter,
            Vector3.one * canopyRadius * 2f);

        // Esferas secundárias (clusters)
        int clusters = NextInt(rng, 4, 7);
        for (int i = 0; i < clusters; i++)
        {
            Vector3 offset = new Vector3(
                NextFloat(rng, -1.5f, 1.5f) * scale,
                NextFloat(rng, -0.5f, 1f) * scale,
                NextFloat(rng, -1.5f, 1.5f) * scale);
            float clusterSize = NextFloat(rng, 1.2f, 2f) * scale;

            CreateSphere($"Canopy_{i}", parent.transform,
                canopyCenter + offset,
                Vector3.one * clusterSize);
        }

        // Galhos saindo do tronco
        int numBranches = NextInt(rng, 2, 4);
        for (int i = 0; i < numBranches; i++)
        {
            float branchAngle = NextFloat(rng, 0, 360);
            float branchTilt = NextFloat(rng, 30, 60);
            float branchLen = NextFloat(rng, 1f, 2f) * scale;

            GameObject branch = CreateCylinder($"Branch_{i}", parent.transform,
                Vector3.up * (trunkHeight * NextFloat(rng, 0.5f, 0.85f)),
                new Vector3(0.08f * scale, branchLen * 0.5f, 0.08f * scale));
            branch.transform.rotation = Quaternion.Euler(branchTilt, branchAngle, 0);
        }
    }

    private static void BuildPineTree(GameObject parent, System.Random rng, float scale)
    {
        float trunkHeight = 6f * scale;

        // Tronco fino e alto
        CreateCylinder("Trunk", parent.transform,
            Vector3.up * (trunkHeight * 0.5f),
            new Vector3(0.2f * scale, trunkHeight * 0.5f, 0.2f * scale));

        // Camadas de copa cônica (de baixo para cima, diminuindo)
        int layers = NextInt(rng, 4, 6);
        for (int i = 0; i < layers; i++)
        {
            float t = (float)i / layers;
            float y = trunkHeight * 0.3f + (trunkHeight * 0.7f * t);
            float radius = (1f - t * 0.7f) * 1.8f * scale;
            float layerHeight = 0.6f * scale;

            // Cone simulado com esfera achatada
            CreateSphere($"PineLayer_{i}", parent.transform,
                Vector3.up * y,
                new Vector3(radius * 2, layerHeight, radius * 2));
        }

        // Topo pontudo
        CreateSphere("PineTop", parent.transform,
            Vector3.up * (trunkHeight + 0.5f * scale),
            new Vector3(0.4f * scale, 1f * scale, 0.4f * scale));
    }

    private static void BuildTallPine(GameObject parent, System.Random rng, float scale)
    {
        float trunkHeight = 9f * scale;

        // Tronco muito alto
        CreateCylinder("Trunk", parent.transform,
            Vector3.up * (trunkHeight * 0.5f),
            new Vector3(0.3f * scale, trunkHeight * 0.5f, 0.3f * scale));

        // Copa estreita e alta no topo
        int layers = NextInt(rng, 6, 9);
        float canopyStart = trunkHeight * 0.4f;
        for (int i = 0; i < layers; i++)
        {
            float t = (float)i / layers;
            float y = canopyStart + (trunkHeight * 0.65f * t);
            float radius = (1f - t * 0.6f) * 1.5f * scale;

            CreateSphere($"TallPineLayer_{i}", parent.transform,
                Vector3.up * y,
                new Vector3(radius * 2, 0.5f * scale, radius * 2));
        }
    }

    private static void BuildDeadTree(GameObject parent, System.Random rng, float scale)
    {
        float trunkHeight = 3.5f * scale;

        // Tronco retorcido
        CreateCylinder("Trunk", parent.transform,
            Vector3.up * (trunkHeight * 0.5f),
            new Vector3(0.3f * scale, trunkHeight * 0.5f, 0.25f * scale));

        // Galhos secos (sem folhas)
        int numBranches = NextInt(rng, 3, 6);
        for (int i = 0; i < numBranches; i++)
        {
            float angle = NextFloat(rng, 0, 360);
            float tilt = NextFloat(rng, 20, 70);
            float len = NextFloat(rng, 0.8f, 2f) * scale;
            float yPos = trunkHeight * NextFloat(rng, 0.4f, 0.95f);

            GameObject branch = CreateCylinder($"DeadBranch_{i}", parent.transform,
                Vector3.up * yPos,
                new Vector3(0.06f * scale, len * 0.5f, 0.06f * scale));
            branch.transform.rotation = Quaternion.Euler(tilt, angle, NextFloat(rng, -10, 10));

            // Sub-galhos
            if (rng.NextDouble() > 0.4)
            {
                GameObject subBranch = CreateCylinder($"SubBranch_{i}", branch.transform,
                    Vector3.up * len * 0.4f,
                    new Vector3(0.03f * scale, 0.4f * scale, 0.03f * scale));
                subBranch.transform.localRotation = Quaternion.Euler(
                    NextFloat(rng, 20, 50), NextFloat(rng, -40, 40), 0);
            }
        }
    }

    private static void BuildWillowTree(GameObject parent, System.Random rng, float scale)
    {
        float trunkHeight = 4.5f * scale;

        // Tronco
        CreateCylinder("Trunk", parent.transform,
            Vector3.up * (trunkHeight * 0.5f),
            new Vector3(0.35f * scale, trunkHeight * 0.5f, 0.35f * scale));

        // Copa espalhada
        Vector3 canopyCenter = Vector3.up * (trunkHeight + 0.3f * scale);
        CreateSphere("WillowCanopy", parent.transform,
            canopyCenter,
            new Vector3(3f * scale, 1.2f * scale, 3f * scale));

        // Galhos pendentes (simulam cipós)
        int hangingBranches = NextInt(rng, 8, 14);
        for (int i = 0; i < hangingBranches; i++)
        {
            float angle = NextFloat(rng, 0, 360);
            float dist = NextFloat(rng, 0.8f, 1.8f) * scale;
            Vector3 hangStart = canopyCenter + Quaternion.Euler(0, angle, 0) * Vector3.forward * dist;
            float hangLen = NextFloat(rng, 2f, 4f) * scale;

            GameObject hang = CreateCylinder($"Hanging_{i}", parent.transform,
                hangStart - Vector3.up * (hangLen * 0.5f),
                new Vector3(0.04f * scale, hangLen * 0.5f, 0.04f * scale));

            // Pequeno cluster de folha na ponta
            CreateSphere($"HangLeaf_{i}", parent.transform,
                hangStart - Vector3.up * hangLen,
                Vector3.one * 0.3f * scale);
        }
    }

    #endregion

    #region Mesh Helpers

    private static GameObject CreateCylinder(string name, Transform parent, Vector3 localPos, Vector3 scale)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = scale;
        obj.isStatic = true;

        // Remover collider para performance (adicionar apenas no tronco principal se necessário)
        Object.DestroyImmediate(obj.GetComponent<Collider>());

        return obj;
    }

    private static GameObject CreateSphere(string name, Transform parent, Vector3 localPos, Vector3 scale)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = scale;
        obj.isStatic = true;

        Object.DestroyImmediate(obj.GetComponent<Collider>());

        return obj;
    }

    public static void ApplyMaterials(GameObject tree, Material trunkMat, Material leavesMat)
    {
        Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            string name = rend.gameObject.name.ToLower();
            if (name.Contains("trunk") || name.Contains("branch") || name.Contains("root") ||
                name.Contains("dead") || name.Contains("sub") || name.Contains("hang"))
            {
                if (trunkMat != null) rend.sharedMaterial = trunkMat;
            }
            else
            {
                if (leavesMat != null) rend.sharedMaterial = leavesMat;
            }
        }
    }

    #endregion

    #region Random Helpers

    private static float NextFloat(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private static int NextInt(System.Random rng, int min, int max)
    {
        return rng.Next(min, max);
    }

    #endregion

    public enum TreeStyle
    {
        Oak,
        Pine,
        DeadTree,
        Willow,
        TallPine
    }
}
