using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Random = UnityEngine.Random;

public class BSPTree {

    public class Polygon
    {
        public List<Vector3> vertices;
        public Plane plane;
        public bool used = false;

        public Polygon()
        {
            vertices = new List<Vector3>();
        }

        public Polygon(IEnumerable<Vector3> verts)
        {
            vertices = verts.ToList();
            plane = new UnityEngine.Plane(vertices[0], vertices[1], vertices[2]);
        }

        int mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        public void AddVertex(Vector3 v)
        {
            var d = Mathf.Abs(plane.GetDistanceToPoint(v));
            if (d > Epsilon)
            {
                Debug.Assert(d < Epsilon, d.ToString());
            }
            vertices.Add(v);
        }

        public override string ToString()
        {
            List<string> strings = new List<string>();
            foreach (var vector3 in vertices)
            {
                strings.Add(string.Format("({0}, {1}, {2})", vector3.x, vector3.y, vector3.z));
            }
            return "Polygon: " + string.Join(", ", strings.ToArray());
        }

        public Vector3 Vertex(int index)
        {
            return vertices[mod(index, vertices.Count)];
        }
    }

    public enum PolygonClassification
    {
        Coincident,
        Spanning,
        InFront,
        InBack
    }

    public enum PointClassification
    {
        On,
        Front,
        Back
    }

    public Plane Plane;
    public List<Polygon> Polygons = new List<Polygon>();

    public BSPTree FrontTree = null;
    public BSPTree BackTree = null;

    public bool isConvexLeaf = false;


    const float Epsilon = 0.0001f;

    public static PolygonClassification ClassifyPolygon(Polygon poly, Plane plane)
    {
        bool inFront = false;
        bool inBack = false;

        foreach (var vertex in poly.vertices)
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

    public static PointClassification ClassifyPoint(Vector3 point, Plane plane)
    {
        var d = plane.GetDistanceToPoint(point);
        if (Mathf.Abs(d) < Epsilon)
            return PointClassification.On;

        if (d < 0.0f)
            return PointClassification.Back;
        
        return PointClassification.Front;
    }

    public static List<Polygon> SplitPolygon(Polygon poly, Plane splitPlane)
    {
        Polygon front = new Polygon();
        Polygon back = new Polygon();

        front.used = poly.used;
        front.plane = poly.plane;
        back.used = poly.used;
        back.plane = poly.plane;

        Vector3 pointA, pointB;
        PointClassification sideA, sideB;

        pointA = poly.vertices.Last();
        sideA = ClassifyPoint(pointA, splitPlane);
        
        for (int i = 0; i < poly.vertices.Count; i++)
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

    public static Polygon ChooseSplitPolygon(List<Polygon> polygons)
    {
        Polygon bestChoice = null;
        int bestChoiceScore = int.MaxValue;
        foreach (var p1 in polygons)
        {
            if (p1.used)
            {
                continue;
            }

            int frontfaces = 0;
            int backfaces = 0;
            int splits = 0;

            foreach (var p2 in polygons)
            {
                if(p1 == p2) continue;

                var cls = ClassifyPolygon(p2, p1.plane);
                if (cls == PolygonClassification.InFront)
                {
                    frontfaces++;
                }
                else if (cls == PolygonClassification.InBack)
                {
                    backfaces++;
                }
                else if(cls == PolygonClassification.Spanning)
                {
                    splits++;
                }
            }

            int score = Math.Abs(frontfaces - backfaces) + (splits*8);

            if (score < bestChoiceScore)
            {
                bestChoice = p1;
                bestChoiceScore = score;
            }
        }
        
        return bestChoice;
    } 

    public static List<Polygon> PolygonsFromMesh(Mesh mesh)
    {
        List<Polygon> polygons = new List<Polygon>();
        for (int m = 0; m < mesh.subMeshCount; m++)
        {
            var triangles = mesh.GetTriangles(m);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = mesh.vertices[triangles[i]];
                var b = mesh.vertices[triangles[i + 1]];
                var c = mesh.vertices[triangles[i + 2]];
                var polygon = new Polygon(new[] {a, b, c});
                polygons.Add(polygon);
            }
        }
        return polygons;
    }

    public List<Vector3> Facet(Polygon polygon)
    {
        List<Vector3> facets = new List<Vector3>();
        int nTriangles = polygon.vertices.Count - 2;

        int index = 1;
        for (int i = 0; i < nTriangles; i++)
        {
            facets.Add(polygon.vertices[0]);
            facets.Add(polygon.vertices[index]);
            facets.Add(polygon.vertices[index + 1]);
            index++;
        }

        return facets;
    } 

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
                meshNormals.Add(polygon.plane.normal);
            }
        }

        m.vertices = meshTriangles.ToArray();
        m.normals = meshNormals.ToArray();
        m.triangles = Enumerable.Range(0, m.vertices.Length).ToArray();
        return m;
    }

    public List<BSPTree> Build(Mesh mesh)
    {
        var convexNodes = new List<BSPTree>();
        var polygons = PolygonsFromMesh(mesh);
        BuildTree(polygons, convexNodes);

        return convexNodes;
    }

    private void BuildTree(List<Polygon> polygons, List<BSPTree> convexNodes)
    {
        var splitPolygon = ChooseSplitPolygon(polygons);
        splitPolygon.used = true;
        this.Plane = splitPolygon.plane;

        List<Polygon> frontfaces = new List<Polygon>();
        List<Polygon> backfaces = new List<Polygon>();

        foreach (var p2 in polygons)
        {
            var cls = ClassifyPolygon(p2, Plane);
            if (cls == PolygonClassification.InFront)
            {
                frontfaces.Add(p2);
            }
            else if (cls == PolygonClassification.InBack)
            {
                backfaces.Add(p2);
            }
            else if (cls == PolygonClassification.Spanning)
            {
                var faces = SplitPolygon(p2, Plane);
                frontfaces.Add(faces.First());
                backfaces.Add(faces.Last());
            }
            else //coincident
            {
                var dp = Vector3.Dot(
                    splitPolygon.plane.normal, p2.plane.normal);

                if (dp > 0)
                {
                    frontfaces.Add(p2);
                }
                else
                {
                    backfaces.Add(p2);
                }
            }
        }

        //check if all frontfaces are used
        bool allUsed = frontfaces.Count > 0 && frontfaces.All(ff => ff.used);

        if (allUsed)
        {
            this.Polygons = frontfaces;
            this.isConvexLeaf = true;
            convexNodes.Add(this);
        }
        else
        {
            if (frontfaces.Count > 0)
            {
                FrontTree = new BSPTree();
                FrontTree.BuildTree(frontfaces, convexNodes);
            }
            
            if(backfaces.Count > 0)
            {
                BackTree = new BSPTree();
                BackTree.BuildTree(backfaces, convexNodes);
            }

        }
    }

}
