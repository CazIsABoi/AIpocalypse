using System.Collections.Generic;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float duration = 0.06f;
    private Renderer[] renderers;
    private Material[][] originals;
    private Material[][] whites;
    private float timer;

    void Awake()
    {
        var list = new List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r.GetComponent<MapIcon>() != null) continue;
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            list.Add(r);
        }
        renderers = list.ToArray();
        originals = new Material[renderers.Length][];
        whites = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            whites[i] = new Material[renderers[i].sharedMaterials.Length];   
            for (int j = 0; j < whites[i].Length; j++)                       
                whites[i][j] = flashMaterial;
            originals[i] = renderers[i].sharedMaterials;
            originals[i] = renderers[i].sharedMaterials;
        }
    }

    public void Flash()
    {
        if (timer <= 0f)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterials = whites[i];
            }
        }
        timer = duration;
    }
    void Update()
    {
        if (timer <= 0f) return;
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterials = originals[i];
            }
        }
    }
}