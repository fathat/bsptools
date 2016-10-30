using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(BSPTreeObject))]
public class BSPTreeEditor : Editor {

    public override void OnInspectorGUI()
    {
        GUILayout.Label("Test");

        if (GUILayout.Button("Print polygons"))
        {
            var obj = this.target as BSPTreeObject;
            var mf = obj.GetComponent<MeshFilter>();
            var polygons = BSPTree.PolygonsFromMesh(mf.sharedMesh);
            foreach (var polygon in polygons)
            {
                Debug.Log(polygon);
            }
        }

        if (GUILayout.Button("Rebuild BSP"))
        {
            //do rebuild
        }
    }
}
