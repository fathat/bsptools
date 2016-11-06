using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SolidLeafBSPTree {

#region PublicFields

    /// <summary>
    /// The plane used to define this node.
    /// </summary>
    public Plane Plane;

    /// <summary>
    /// Subtree in front of plane.
    /// </summary>
    public SolidLeafBSPTree FrontTree = null;
    
    /// <summary>
    /// Subtree behind the plane. If null indicates a solid area.
    /// </summary>
    public SolidLeafBSPTree BackTree = null;

    /// <summary>
    /// Is this a convex leaf?
    /// </summary>
    public bool IsConvexLeaf = false;

    /// <summary>
    /// List of polygons associated with this node. Note that only
    /// leafs have polygons.
    /// </summary>
    public List<Polygon> Polygons = new List<Polygon>();

    /// <summary>
    /// List of convex nodes in this tree. Note that this is only
    /// populated for the root of the tree.
    /// </summary>
    public List<SolidLeafBSPTree> ConvexNodes;

#endregion

    /// <summary>
    /// A convex polygon used for internal processing.
    /// </summary>
    public class Polygon
    {
        /// <summary>
        /// A list of three or more vertices.
        /// </summary>
        public List<Vector3> Vertices;

        /// <summary>
        /// The plane this polygon is one.
        /// </summary>
        public Plane Plane;

        /// <summary>
        /// Has this polygon been used as a split plane?
        /// </summary>
        public bool Used = false;

        /// <summary>
        /// Creates an empty polygon.
        /// </summary>
        public Polygon()
        {
            Vertices = new List<Vector3>();
        }

        /// <summary>
        /// Creates a polygon from a list of vertices.
        /// </summary>
        /// <param name="verts"></param>
        public Polygon(IEnumerable<Vector3> verts)
        {
            Vertices = verts.ToList();
            Plane = new UnityEngine.Plane(Vertices[0], Vertices[1], Vertices[2]);
        }
        
        /// <summary>
        /// Marks this polygon as having been used as a split plane.
        /// </summary>
        public void MarkUsed()
        {
            Used = true;
        }

        /// <summary>
        /// Adds a vertex to this polygon.
        /// </summary>
        /// <param name="v"></param>
        public void AddVertex(Vector3 v)
        {
            var d = Mathf.Abs(Plane.GetDistanceToPoint(v));
            if (d > Epsilon)
            {
                Debug.Assert(d < Epsilon, d.ToString());
            }
            Vertices.Add(v);
        }

        /// <summary>
        /// Debug string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Polygon: " + string.Join(", ", Vertices.Select(vector3 => string.Format("({0}, {1}, {2})", vector3.x, vector3.y, vector3.z)).ToArray());
        }

        int mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        /// <summary>
        /// Return a vertex. If index is less than zero or greater than count,
        /// the value is wrapped.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Vector3 Vertex(int index)
        {
            return Vertices[mod(index, Vertices.Count)];
        }
    }

    /// <summary>
    /// A polygon classification
    /// </summary>
    private enum PolygonClassification
    {
        Coincident,
        Spanning,
        InFront,
        InBack
    }

    /// <summary>
    /// Point classification.
    /// </summary>
    private enum PointClassification
    {
        On,
        Front,
        Back
    }
    
    /// <summary>
    /// Build settings for constructing the tree.
    /// </summary>
    public class BuildSettings
    {
        public enum AreaFillMode
        {
            Front,
            Back
        }

        /// <summary>
        /// If fill mode is "Front", we assume that areas in front of polygons
        /// are empty (IE, this is useful for hallways/corridors/rooms). 
        /// 
        /// If fill mode is "Back", the inverse happens. This is useful for
        /// solid objects. (IE, the sort of objects that would populate a corridor)
        /// </summary>
        public AreaFillMode FillMode;

        /// <summary>
        /// If high quality is true, we try to generate an optimal tree. Otherwise
        /// we just generate an adequate one (which is usually faster to create).
        /// </summary>
        public bool HighQuality = true;
    }
    

    /// <summary>
    /// Tolerance for floating point equality comparisons.
    /// </summary>
    const float Epsilon = 0.0001f;

    /// <summary>
    /// Classifies a polygon with relation to a plane.
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="plane"></param>
    /// <returns></returns>
    private static PolygonClassification ClassifyPolygon(Polygon poly, Plane plane)
    {
        bool inFront = false;
        bool inBack = false;

        foreach (var vertex in poly.Vertices)
        {
            var d = plane.GetDistanceToPoint(vertex);
            if (Mathf.Abs(d) < Epsilon)
                continue;

            if (d < 0.0f)
                inBack = true;
            else if (d > 0.0f)
                inFront = true;
        }

        if (inFront == false && inBack == false)
            return PolygonClassification.Coincident;

        if(inFront && !inBack)
            return PolygonClassification.InFront;

        if(!inFront && inBack)
            return PolygonClassification.InBack;
        
        return PolygonClassification.Spanning;
    }

    /// <summary>
    /// Classifies a point as either being on, in front, or in back of a plane.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="plane"></param>
    /// <returns></returns>
    private static PointClassification ClassifyPoint(Vector3 point, Plane plane)
    {
        var d = plane.GetDistanceToPoint(point);
        if (Mathf.Abs(d) < Epsilon)
            return PointClassification.On;

        if (d < 0.0f)
            return PointClassification.Back;
        
        return PointClassification.Front;
    }

    /// <summary>
    /// Splits a polygon by a given plane, resulting in two polygons. Note
    /// that this function assumes that the polygon intersects the split plane.
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="splitPlane"></param>
    /// <returns>Two polygons in a list.</returns>
    public static List<Polygon> SplitPolygon(Polygon poly, Plane splitPlane)
    {
        Polygon front = new Polygon();
        Polygon back = new Polygon();

        front.Used = poly.Used;
        front.Plane = poly.Plane;
        back.Used = poly.Used;
        back.Plane = poly.Plane;

        Vector3 pointA, pointB;
        PointClassification sideA, sideB;

        pointA = poly.Vertices.Last();
        sideA = ClassifyPoint(pointA, splitPlane);
        
        for (int i = 0; i < poly.Vertices.Count; i++)
        {
            pointB = poly.Vertex(i);
            sideB = ClassifyPoint(pointB, splitPlane);

            if (sideB == PointClassification.Front)
            {
                if (sideA == PointClassification.Back)
                {
                    Vector3 v = (pointB - pointA).normalized;
                    Ray r = new Ray(pointA, v);

                    float e;
                    splitPlane.Raycast(r, out e);

                    Vector3 intersectionPoint = pointA + v*e;
                    front.AddVertex(intersectionPoint);
                    back.AddVertex(intersectionPoint);
                }
                front.AddVertex(pointB);
            }
            else if (sideB == PointClassification.Back)
            {
                if (sideA == PointClassification.Front)
                {
                    Vector3 v = (pointB - pointA).normalized;
                    Ray r = new Ray(pointA, v);

                    float e;
                    splitPlane.Raycast(r, out e);

                    Vector3 intersectionPoint = pointA + v * e;
                    front.AddVertex(intersectionPoint);
                    back.AddVertex(intersectionPoint);
                }
                back.AddVertex(pointB);
            }
            else
            {
                front.AddVertex(pointB);
                back.AddVertex(pointB);
            }

            pointA = pointB;
            sideA = sideB;
        }

        return new List<Polygon>() {front, back};
    }

    /// <summary>
    /// Contains the result of splitting a list of polygons.
    /// </summary>
    private class ChooseSplitPolygonResult
    {
        /// <summary>
        /// The polygon to use as a splitter.
        /// </summary>
        public Polygon Polygon;

        /// <summary>
        /// Classified polygons.
        /// </summary>
        public List<Polygon> Front = new List<Polygon>();
        public List<Polygon> Back = new List<Polygon>();
        public List<Polygon> Spanning = new List<Polygon>();
        public List<Polygon> Coincident = new List<Polygon>();

        public ChooseSplitPolygonResult()
        {
        }

        public ChooseSplitPolygonResult(Polygon polygon)
        {
            Polygon = polygon;
        }
        
        /// <summary>
        /// Copies the field values from another instance.
        /// </summary>
        /// <param name="cp"></param>
        public void Assign(ChooseSplitPolygonResult cp)
        {
            this.Polygon = cp.Polygon;
            this.Front = cp.Front;
            this.Back = cp.Back;
            this.Spanning = cp.Spanning;
            this.Coincident = cp.Coincident;
        }

        /// <summary>
        /// Takes a list of polygons, and then classifies whether
        /// they are in front, in back, spanning, or coincident. Does
        /// not modify original list.
        /// </summary>
        /// <param name="polygons"></param>
        public void Classify(List<Polygon> polygons)
        {
            foreach (var p2 in polygons)
            {
                var classification = ClassifyPolygon(p2, Polygon.Plane);
                switch (classification)
                {
                    case PolygonClassification.InFront:
                        Front.Add(p2);
                        break;
                    case PolygonClassification.InBack:
                        Back.Add(p2);
                        break;
                    case PolygonClassification.Spanning:
                        Spanning.Add(p2);
                        break;
                    default:
                        Coincident.Add(p2);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Chooses a split polygon at random (non-deterministic). Also classifies
    /// the rest of the polygons by whether they are in front, behind, spanning, or coincident.
    /// 
    /// This runs as a coroutine, however, this specific version only iterates once.
    /// </summary>
    /// <param name="polygons">The polygons to choose from</param>
    /// <param name="result">The resulting polygons and polygon classification</param>
    /// <returns></returns>
    private static IEnumerator ChooseSplitPolygonRandom(List<Polygon> polygons, ChooseSplitPolygonResult result)
    {
        int index = Random.Range(0, polygons.Count);
        result.Assign(new ChooseSplitPolygonResult(polygons[index]));
        result.Classify(polygons);
        yield return null;
    }

    /// <summary>
    /// Chooses a split polygon by scoring the polygon choices.
    /// 
    /// This algorithm is approximately O(N**2), so it's likely to take a while.
    /// 
    /// This method runs as a coroutine so should be run until completion.
    /// </summary>
    /// <param name="polygons">The polygons to choose from</param>
    /// <param name="result">The resulting split polygon, along with classifications for other polygons in relation.</param>
    /// <returns></returns>
    private static IEnumerator ChooseSplitPolygonBestChoice(List<Polygon> polygons, ChooseSplitPolygonResult result)
    {
        int bestChoiceScore = int.MaxValue;

        foreach (var p1 in polygons)
        {
            if (p1.Used)
            {
                continue;
            }

            ChooseSplitPolygonResult potentialResult = new ChooseSplitPolygonResult(p1);
            potentialResult.Classify(polygons);

            int frontfaces = potentialResult.Front.Count-1; // -1 because p1 is included.
            int backfaces = potentialResult.Back.Count;
            int splits = potentialResult.Spanning.Count;

            int score = Math.Abs(frontfaces - backfaces) + (splits*8);

            if (score < bestChoiceScore)
            {
                bestChoiceScore = score;
                result.Assign(potentialResult);
            }

            yield return null;
        }
        
    } 

    /// <summary>
    /// Creates a list of polygons from a unity mesh. (Ignores materials and uvs).
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="reverseNormals"></param>
    /// <returns></returns>
    private static List<Polygon> PolygonsFromMesh(Mesh mesh, bool reverseNormals)
    {
        List<Polygon> polygons = new List<Polygon>();
        for (int m = 0; m < mesh.subMeshCount; m++)
        {
            var triangles = mesh.GetTriangles(m);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a, b, c;
                a = mesh.vertices[triangles[i]];

                if (reverseNormals)
                {
                    b = mesh.vertices[triangles[i + 2]];
                    c = mesh.vertices[triangles[i + 1]];
                }
                else
                {
                    b = mesh.vertices[triangles[i + 1]];
                    c = mesh.vertices[triangles[i + 2]];
                }
                var polygon = new Polygon(new[] {a, b, c});
                polygons.Add(polygon);
            }
        }
        return polygons;
    }

    /// <summary>
    /// Convert a convex polygon into a set of triangles.
    /// </summary>
    /// <param name="polygon"></param>
    /// <returns></returns>
    private List<Vector3> Facet(Polygon polygon)
    {
        List<Vector3> facets = new List<Vector3>();
        int nTriangles = polygon.Vertices.Count - 2;

        int index = 1;
        for (int i = 0; i < nTriangles; i++)
        {
            facets.Add(polygon.Vertices[0]);
            facets.Add(polygon.Vertices[index]);
            facets.Add(polygon.Vertices[index + 1]);
            index++;
        }

        return facets;
    } 

    /// <summary>
    /// Convert a local list of polygons into a mesh.
    /// </summary>
    /// <returns></returns>
    public Mesh ConvertToMesh()
    {
        Mesh m = new Mesh();

        var meshTriangles = new List<Vector3>();
        var meshNormals = new List<Vector3>();
        foreach (var polygon in Polygons)
        {
            var facets = Facet(polygon);
            meshTriangles.AddRange(facets);

            for (int i = 0; i < facets.Count; i++)
            {
                meshNormals.Add(polygon.Plane.normal);
            }
        }

        m.vertices = meshTriangles.ToArray();
        m.normals = meshNormals.ToArray();
        m.triangles = Enumerable.Range(0, m.vertices.Length).ToArray();
        return m;
    }
    
    /// <summary>
    /// Builds the BSP tree. 
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    public IEnumerator Build(Mesh mesh, BuildSettings settings)
    {
        ConvexNodes = new List<SolidLeafBSPTree>();
        var polygons = PolygonsFromMesh(mesh, settings.FillMode != BuildSettings.AreaFillMode.Front);
        var it = BuildTree(polygons, ConvexNodes, settings);
        while (it.MoveNext()) yield return null;
    }

    /// <summary>
    /// Builds each node of the tree
    /// </summary>
    /// <param name="polygons"></param>
    /// <param name="convexNodes">A list to add convex leaf nodes to.</param>
    /// <param name="settings"></param>
    /// <returns></returns>
    private IEnumerator BuildTree(List<Polygon> polygons, List<SolidLeafBSPTree> convexNodes, BuildSettings settings)
    {
        ChooseSplitPolygonResult result = new ChooseSplitPolygonResult();
        if (settings.HighQuality)
        {
            var it = ChooseSplitPolygonBestChoice(polygons, result);
            while (it.MoveNext()) yield return null;
        }
        else
        {
            var it = ChooseSplitPolygonRandom(polygons, result);
            while (it.MoveNext()) yield return null;
        }

        var splitPolygon = result.Polygon;
        splitPolygon.MarkUsed();

        this.Plane = splitPolygon.Plane;

        //Classify the coincident polygons to either the front of back list.
        foreach (var polygon in result.Coincident)
        {
            var dp = Vector3.Dot(splitPolygon.Plane.normal, polygon.Plane.normal);
            if (dp > 0)
                result.Front.Add(polygon);
            else
                result.Back.Add(polygon);
        }

        //Split the spanning polygons and add them to front/back list.
        foreach (var polygon in result.Spanning)
        {
            var faces = SplitPolygon(polygon, Plane);
            result.Front.Add(faces.First());
            result.Back.Add(faces.Last());
        }

        //check if all frontfaces are used
        bool allUsed = result.Front.Count > 0 && result.Front.All(ff => ff.Used);

        if (allUsed)
        {
            this.Polygons = result.Front;
            this.IsConvexLeaf = true;
            convexNodes.Add(this);
        }
        else
        {
            if (result.Front.Count > 0)
            {
                FrontTree = new SolidLeafBSPTree();
                var it = FrontTree.BuildTree(result.Front, convexNodes, settings);
                while (it.MoveNext()) yield return null;
            }
            
            if(result.Back.Count > 0)
            {
                BackTree = new SolidLeafBSPTree();
                var it = BackTree.BuildTree(result.Back, convexNodes, settings);
                while (it.MoveNext()) yield return null;
            }
        }
    }
}
