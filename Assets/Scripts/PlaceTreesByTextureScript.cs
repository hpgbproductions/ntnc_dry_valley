using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

public class PlaceTreesByTextureScript : EditorWindow
{
    private Terrain t;
    private AnimationCurve treeChanceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    private float minTreeHeight = 1f;
    private float maxTreeHeight = 1f;
    private float minTreeWidth = 1f;
    private float maxTreeWidth = 1f;
    private bool lockWidthToHeight = true;
    private int layerIndex = 0;
    private int maxTrees = 10000;

    private int iterations = 0;

    [MenuItem("Window/Terrain/Trees by Texture")]
    private static void Init()
    {
        PlaceTreesByTextureScript window = (PlaceTreesByTextureScript)GetWindow(typeof(PlaceTreesByTextureScript));

        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("--- Terrain Settings ---");
        t = (Terrain)EditorGUILayout.ObjectField("Terrain", t, typeof(Terrain), true);
        layerIndex = EditorGUILayout.IntField("Terrain Layer Index", layerIndex);

        EditorGUILayout.LabelField("--- Tree Settings ---");
        treeChanceCurve = EditorGUILayout.CurveField("Tree Chance over Texture", treeChanceCurve, Color.green, new Rect(0f, 0f, 1f, 1f));
        maxTrees = EditorGUILayout.IntField("Tree Count", maxTrees);
        EditorGUILayout.MinMaxSlider("Tree Height", ref minTreeHeight, ref maxTreeHeight, 0.01f, 2f);
        lockWidthToHeight = EditorGUILayout.Toggle("Lock Width to Height", lockWidthToHeight);
        GUI.enabled = !lockWidthToHeight;
        EditorGUILayout.MinMaxSlider("Tree Width", ref minTreeWidth, ref maxTreeWidth, 0.01f, 2f);
        GUI.enabled = true;

        if (t == null)
        {
            EditorGUILayout.HelpBox("Terrain is required.", MessageType.Error, true);
            GUI.enabled = false;
        }
        else if (layerIndex < 0 || layerIndex >= t.terrainData.alphamapLayers)
        {
            EditorGUILayout.HelpBox("Invalid layer index.", MessageType.Error, true);
            GUI.enabled = false;
        }
        // else GUI.enabled = true

        if (GUILayout.Button("Generate"))
        {
            float[,,] alphamap = t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight);

            TreeInstance[] treeInstances = new TreeInstance[maxTrees];

            iterations = 0;
            int currentTrees = 0;
            while (currentTrees < maxTrees)
            {
                iterations++;

                // Location on the terrain in alphamap space
                float alphamap_x = Random.Range(0f, t.terrainData.alphamapWidth - 1f);
                float alphamap_y = Random.Range(0f, t.terrainData.alphamapHeight - 1f);

                // Alphamap value at the location using bilinear interpolation
                float alpha_0x_0y = alphamap[Mathf.FloorToInt(alphamap_x), Mathf.FloorToInt(alphamap_y), layerIndex];
                float alpha_0x_1y = alphamap[Mathf.FloorToInt(alphamap_x), Mathf.Min(Mathf.FloorToInt(alphamap_y) + 1, t.terrainData.alphamapHeight - 1), layerIndex];
                float alpha_1x_0y = alphamap[Mathf.Min(Mathf.FloorToInt(alphamap_x) + 1, t.terrainData.alphamapWidth - 1), Mathf.FloorToInt(alphamap_y), layerIndex];
                float alpha_1x_1y = alphamap[Mathf.Min(Mathf.FloorToInt(alphamap_x) + 1, t.terrainData.alphamapWidth - 1), Mathf.Min(Mathf.FloorToInt(alphamap_y) + 1, t.terrainData.alphamapHeight - 1), layerIndex];
                float alpha_tx_0y = Mathf.Lerp(alpha_0x_0y, alpha_1x_0y, Mathf.Repeat(alphamap_x, 1f));
                float alpha_tx_1y = Mathf.Lerp(alpha_0x_1y, alpha_1x_1y, Mathf.Repeat(alphamap_x, 1f));
                float alpha_t = Mathf.Lerp(alpha_tx_0y, alpha_tx_1y, Mathf.Repeat(alphamap_y, 1f));

                float treeChance = treeChanceCurve.Evaluate(alpha_t);
                if (Random.value > treeChance)
                {
                    // Generation of tree cancelled, try again
                    continue;
                }

                treeInstances[currentTrees] = new TreeInstance();
                treeInstances[currentTrees].color = Color.white;
                treeInstances[currentTrees].heightScale = Random.Range(minTreeHeight, maxTreeHeight);
                treeInstances[currentTrees].lightmapColor = Color.white;
                treeInstances[currentTrees].position = new Vector3(
                    Mathf.InverseLerp(0f, t.terrainData.alphamapHeight - 1f, alphamap_y),
                    0f,
                    Mathf.InverseLerp(0f, t.terrainData.alphamapWidth - 1f, alphamap_x)
                    );
                treeInstances[currentTrees].prototypeIndex = Random.Range(0, t.terrainData.treePrototypes.Length);
                treeInstances[currentTrees].rotation = Random.Range(0f, 2f * Mathf.PI);
                if (lockWidthToHeight)
                {
                    treeInstances[currentTrees].widthScale = treeInstances[currentTrees].heightScale;
                }
                else
                {
                    treeInstances[currentTrees].widthScale = Random.Range(minTreeWidth, maxTreeWidth);
                }

                currentTrees++;
            }

            t.terrainData.SetTreeInstances(treeInstances, true);

            Debug.Log($"Completed generation of {t.terrainData.treeInstances.Length} trees in {iterations} tries.");
        }
    }
}

#endif