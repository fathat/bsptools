using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(BSPTreeObject))]
public class BSPTreeEditor : Editor {

    public override void OnInspectorGUI()
    {
        var obj = this.target as BSPTreeObject;
        base.OnInspectorGUI();

        if (GUILayout.Button("Print polygons"))
        {
            var mf = obj.GetComponent<MeshFilter>();
            var polygons = BSPTree.PolygonsFromMesh(mf.sharedMesh);
            foreach (var polygon in polygons)
            {
                Debug.Log(polygon);
            }
        }

        if (GUILayout.Button("Rebuild BSP"))
        {
            BSPTree tree = new BSPTree();
            var mf = obj.GetComponent<MeshFilter>();
            var convexNodes = tree.Build(mf.sharedMesh);
            Debug.Log(convexNodes.Count);
            
            int materialIndex = 0;
            foreach (var convexNode in convexNodes)
            {
                var mesh = convexNode.ConvertToMesh();
                
                var go = new GameObject();
                go.transform.parent = obj.transform;
                var newMeshFilter = go.AddComponent<MeshFilter>();
                newMeshFilter.sharedMesh = mesh;
                /*
                var meshRenderer = go.AddComponent<MeshRenderer>();
                if (obj.defaultMaterial != null)
                {
                    meshRenderer.sharedMaterial = obj.defaultMaterial[materialIndex];
                    materialIndex++;

                    if (materialIndex >= obj.defaultMaterial.Length)
                        materialIndex = 0;
                }*/
                var meshCollider = go.AddComponent<MeshCollider>();
                meshCollider.convex = true;
                meshCollider.sharedMesh = mesh;
            }
        }
    }
}
