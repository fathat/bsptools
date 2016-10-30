using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEditor;

public class SplitPolygonTest : MonoBehaviour
{
    public Vector3[] vertices;

    public Transform splitPlane;
    
    // Use this for initialization
    void Start()
    {

    }

    void DrawPolygon(Vector3[] verts)
    {
        if (verts != null && verts.Length > 2)
        {
            for (int i = 1; i < verts.Length; i++)
            {
                Gizmos.DrawLine(verts[i - 1], verts[i]);
            }
            Gizmos.DrawLine(verts.First(), verts.Last());
        }
    }
    void OnDrawGizmos()
    {
        if (!splitPlane)
            return;
        
        var plane = new Plane(splitPlane.up, splitPlane.transform.position);
        //DrawPolygon(vertices);
        var polygon = new BSPTree.Polygon(vertices);
        var polygons = BSPTree.SplitPolygon(polygon, plane);

        var colors = new[]
        {
            new Color(1.0f, 0.0f, 0.0f),
            new Color(0.0f, 1.0f, 0.0f),
        };
        int c = 0;
        foreach (var poly in polygons)
        {
            Gizmos.color = colors[c];
            DrawPolygon(poly.vertices.ToArray());
            c++;
            if (c > colors.Length) c = 0;
        }
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}
