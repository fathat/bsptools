using UnityEngine;
using UnityEditor;
using System.Collections;

public class BSPTreeWindow : EditorWindow {

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/BSP Tools")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        BSPTreeWindow window = (BSPTreeWindow)EditorWindow.GetWindow(typeof(BSPTreeWindow), false, "BSP Tools");
        window.Show();
    }

    private bool isDebug = false;
    private bool isTrigger = true;

    void OnGUI()
    {
        isDebug = GUILayout.Toggle(isDebug, "Debug Mode");
        isTrigger = GUILayout.Toggle(isTrigger, "Is Trigger");

        if (GUILayout.Button("Build Convex Objects"))
        {
            if (Selection.activeGameObject)
            {
                var obj = Selection.activeGameObject;
                var meshFilter = Selection.activeGameObject.GetComponent<MeshFilter>();

                if (meshFilter)
                {
                    BSPTree tree = new BSPTree();
                    var convexNodes = tree.Build(meshFilter.sharedMesh);
                    Debug.Log(convexNodes.Count);

                    var materialFiles = AssetDatabase.FindAssets("BspTestMaterial t:material");
                    int index = 0;
                    foreach (var convexNode in convexNodes)
                    {
                        var mesh = convexNode.ConvertToMesh();

                        var go = new GameObject("Collider" + index);
                        go.transform.SetParent(obj.transform, false);
                        var newMeshFilter = go.AddComponent<MeshFilter>();
                        newMeshFilter.sharedMesh = mesh;

                        if (isDebug)
                        {
                            if (materialFiles.Length > 0)
                            {
                                string guid = materialFiles[index%materialFiles.Length];
                                string path = AssetDatabase.GUIDToAssetPath(guid);

                                var meshRenderer = go.AddComponent<MeshRenderer>();
                                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                                meshRenderer.material = material;
                            }
                        }


                        var meshCollider = go.AddComponent<MeshCollider>();
                        meshCollider.convex = true;
                        meshCollider.isTrigger = isTrigger;
                        meshCollider.sharedMesh = mesh;

                        index++;
                    }


                }
            }
        }

    }   
}
