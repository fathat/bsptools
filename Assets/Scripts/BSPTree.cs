using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

public class BSPTree {

    public class Polygon
    {
        public List<Vector3> vertices;
        public Plane plane;

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
            Debug.Assert(Mathf.Abs(plane.GetDistanceToPoint(v)) < Epsilon);
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

    public BSPTree Left;
    public BSPTree Right;

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

    public static List<Polygon> SplitPolygon(Polygon poly, Plane plane)
    {
        Polygon front = new Polygon();
        Polygon back = new Polygon();

        Vector3 pointA, pointB;
        PointClassification sideA, sideB;

        pointA = poly.vertices.Last();
        sideA = ClassifyPoint(pointA, plane);
        
        for (int i = 0; i < poly.vertices.Count; i++)
        {
            pointB = poly.Vertex(i);
            sideB = ClassifyPoint(pointB, plane);

            if (sideB == PointClassification.Front)
            {
                if (sideA == PointClassification.Back)
                {
                    Vector3 v = (pointB - pointA).normalized;
                    Ray r = new Ray(pointA, v);

                    float e;
                    plane.Raycast(r, out e);

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
                    plane.Raycast(r, out e);

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
        //LAME!
        return polygons[Random.Range(0, polygons.Count)];
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

    public void Build(Mesh mesh)
    {
        PolygonsFromMesh(mesh);
    }

    private void BuildTree(BSPTree tree, List<Polygon> polygons)
    {
        
    }

}
