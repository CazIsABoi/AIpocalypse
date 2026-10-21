using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UIGradient : BaseMeshEffect
{
    public Color top = Color.white, bottom = Color.black;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        Rect rect = graphic.rectTransform.rect;   // same local space as the vertex positions
        UIVertex vertex = new UIVertex();

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float t = Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y);   // 0 = bottom, 1 = top

            Color c = vertex.color;               
            c *= Color.Lerp(bottom, top, t);
            vertex.color = c;                     

            vh.SetUIVertex(vertex, i);
        }
    }

    public void Set(Color newTop, Color newBottom)
    {
        top = newTop;
        bottom = newBottom;
        if (graphic != null) graphic.SetVerticesDirty();   // rebuild the mesh with the new colors
    }

#if UNITY_EDITOR
    // Redraw while tweaking the colors in the Inspector
    protected override void OnValidate()
    {
        base.OnValidate();
        if (graphic != null) graphic.SetVerticesDirty();
    }
#endif
}
