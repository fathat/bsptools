using UnityEngine;
using UnityEditor;
using System.Collections;

public class SolidLeafBSPTreeWindow : EditorWindow {

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/BSP Tools")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        SolidLeafBSPTreeWindow window = (SolidLeafBSPTreeWindow)EditorWindow.GetWindow(typeof(SolidLeafBSPTreeWindow), false, "BSP Tools");
        window.Show();
    }

    private bool _createDebugVisuals = false;
    private bool _isTrigger = true;
    private bool _highQuality = true;

    SolidLeafBSPTree.BuildSettings.AreaFillMode _fillMode = SolidLeafBSPTree.BuildSettings.AreaFillMode.Front;

    private IEnumerator _buildRoutine;
    private SolidLeafBSPTree _bspTree;
    private GameObject _targetObject;

    /// <summary>
    /// Current progress
    /// </summary>
    private int _steps = 0;
    private int _stepsPerFrame = 100;

    void Update()
    {
        if (_buildRoutine == null)
            return;

        if (!_targetObject)
        {
            //if we lost our target object, abort the build
            _buildRoutine = null;
            _bspTree = null;
            return;
        }

        for (int i = 0; i < _stepsPerFrame; i++)
        {
            bool isDone = !_buildRoutine.MoveNext();

            if (!isDone)
            {
                _steps++;

                if (_steps%30 == 0)
                {
                    Repaint();
                }
            }
            else
            {
                var convexNodes = _bspTree.ConvexNodes;
                Debug.Log(convexNodes.Count);

                var materialFiles = AssetDatabase.FindAssets("BspTestMaterial t:material");
                int index = 0;
                foreach (var convexNode in convexNodes)
                {
                    var mesh = convexNode.ConvertToMesh();

                    var go = new GameObject("Collider" + index);
                    go.transform.SetParent(_targetObject.transform, false);
                    var newMeshFilter = go.AddComponent<MeshFilter>();
                    newMeshFilter.sharedMesh = mesh;

                    if (_createDebugVisuals)
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
                    meshCollider.isTrigger = _isTrigger;
                    meshCollider.sharedMesh = mesh;

                    index++;
                }
                _bspTree = null;
                _buildRoutine = null;

                Repaint();
                break;
            }
        }
    }

    void OnGUI()
    {
        _createDebugVisuals = GUILayout.Toggle(_createDebugVisuals, "Create Debug Visuals");
        _isTrigger = GUILayout.Toggle(_isTrigger, "Create Colliders As Trigger");
        _highQuality = GUILayout.Toggle(_highQuality, "High Quality");
        _fillMode = (SolidLeafBSPTree.BuildSettings.AreaFillMode)EditorGUILayout.EnumPopup("Fill Polygons", _fillMode);
        _stepsPerFrame = EditorGUILayout.IntSlider("Steps Per Frame", _stepsPerFrame, 1, 1000);

        if (GUILayout.Button("Fill With Convex Colliders"))
        {
            if (Selection.activeGameObject)
            {
                var obj = Selection.activeGameObject;
                var meshFilter = Selection.activeGameObject.GetComponent<MeshFilter>();

                if (meshFilter)
                {
                    var settings = new SolidLeafBSPTree.BuildSettings
                    {
                        FillMode = _fillMode,
                        HighQuality = _highQuality
                    };

                    _targetObject = obj;
                    _steps = 0;
                    _bspTree = new SolidLeafBSPTree();
                    _buildRoutine = _bspTree.Build(meshFilter.sharedMesh, settings);
                }
            }
        }

        if (_steps > 0)
        {
            if (_buildRoutine != null)
            {
                GUILayout.Label("Building... " + _steps);
            }
            else
            {
                GUILayout.Label("Built in " + _steps + " steps");
            }
        }

    }
}
