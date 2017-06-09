using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ConvexHullScript : Paintable
{
    private MeshFilter _meshFilter;
    private List<Vector3> _pointList;
    private Mesh _mesh;
    private List<Tri> _triList;

    private void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _pointList = GetFourCloudOfPoints();
        _triList = GetInitialTris(_pointList);
        _mesh = new Mesh();
        UpdateMesh(_mesh, _triList);
        _meshFilter.mesh = _mesh;
    }

    public override void AddPoint(Vector3 newPoint)
    {
        _pointList.Add(newPoint);
        
        FaceStatus faceStates = GetFaceStates(newPoint, _triList);
        _triList = faceStates.Keep.ToList();
        IEnumerable<Edge> openEdges = faceStates.GetOpenEdges();
        IEnumerable<Tri> newTries = CreateNewFacesToPoint(newPoint, _pointList.Count - 1, openEdges);
        _triList.AddRange(newTries);
        UpdateMesh(_mesh, _triList);

    }

    private static IEnumerable<Tri> CreateNewFacesToPoint(Vector3 newVertPos, int newVertIndex, IEnumerable<Edge> openEdges)
    {
        List<Tri> ret = new List<Tri>();
        Vector3 normalGuide = GetEdgeAverage(openEdges);
        foreach (Edge edge in openEdges)
        {
            Tri newTri = new Tri(newVertPos, edge.Vert0.Pos, edge.Vert1.Pos, newVertIndex, edge.Vert0.ID, edge.Vert1.ID, normalGuide);
            ret.Add(newTri);
        }
        return ret;
    }

    private static Vector3 GetEdgeAverage(IEnumerable<Edge> openEdges)
    {
        int count = 0;
        Vector3 average = new Vector3();
        foreach (Edge edge in openEdges)
        {
            average += (edge.Vert0.Pos + edge.Vert1.Pos) / 2;
            count++;
        }
        return average / count;
    }

    private static FaceStatus GetFaceStates(Vector3 newVertPos, List<Tri> tris)
    {
        List<Tri> keep = new List<Tri>();
        List<Tri> delete = new List<Tri>();
        foreach (Tri tri in tris)
        {
            Vector3 triToPos = (tri.Vert0.Pos - newVertPos).normalized;
            bool isFacing = Vector3.Dot(triToPos, tri.Normal) > 0;
            (isFacing ? keep : delete).Add(tri);
        }
        return new FaceStatus(keep, delete);
    }

    private static List<Tri> GetInitialTris(List<Vector3> pointsList)
    {
        Vector3 centerPoint = (pointsList[0] + pointsList[1] + pointsList[2] + pointsList[3]) / 4;
        Tri tri0 = new Tri(0, 1, 2, pointsList, centerPoint);
        Tri tri1 = new Tri(1, 2, 3, pointsList, centerPoint);
        Tri tri2 = new Tri(0, 2, 3, pointsList, centerPoint);
        Tri tri3 = new Tri(0, 1, 3, pointsList, centerPoint);
        return new List<Tri> { tri0, tri1, tri2, tri3 };
    }

    private struct FaceStatus
    {
        public readonly IEnumerable<Tri> Keep;
        public readonly IEnumerable<Tri> Delete;

        public FaceStatus(IEnumerable<Tri> keep, IEnumerable<Tri> delete)
        {
            Keep = keep;
            Delete = delete;
        }

        public IEnumerable<Edge> GetOpenEdges()
        {
            Dictionary<Edge, int> dictionary = new Dictionary<Edge, int>();
            foreach (Tri deletedTri in Delete)
            {
                foreach (Edge edge in deletedTri.Edges)
                {
                    if(dictionary.ContainsKey(edge))
                    {
                        dictionary[edge]++;
                    }
                    else
                    {
                        dictionary.Add(edge, 1);
                    }
                }
            }
            return dictionary.Where(item => item.Value == 1).Select(item => item.Key);
        }
    }

    private void UpdateMesh(Mesh mesh, IEnumerable<Tri> tris)
    {
        List<Vector3> vertices = new List<Vector3>();
        Dictionary<int, int> vertsDictionary = new Dictionary<int, int>();
        List<int> triangle = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        foreach (Tri tri in tris)
        {
            foreach (Vert polyVert in tri.Verts)
            {
                if (!vertsDictionary.ContainsKey(polyVert.ID))
                {
                    vertices.Add(polyVert.Pos);
                    normals.Add(tri.Normal);
                    vertsDictionary.Add(polyVert.ID, vertsDictionary.Count);
                }
                int newIndex = vertsDictionary[polyVert.ID];
                triangle.Add(newIndex);
            }
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangle.ToArray();
        mesh.normals = normals.ToArray();
    }

    private List<Vector3> GetFourCloudOfPoints()
    {
        List<Vector3> ret = new List<Vector3>(4);
        for (int i = 0; i < 4; i++)
        {
            ret.Add(new Vector3(UnityEngine.Random.value,
              UnityEngine.Random.value,
              UnityEngine.Random.value));
        }
        return ret;
    }

    private struct Edge
    {
        private readonly Vert _vert0;
        public Vert Vert0 { get { return _vert0; } }
        private readonly Vert _vert1;
        public Vert Vert1 { get { return _vert1; } }

        public Edge(Vert vert0, Vert vert1)
        {
            if(vert0.ID < vert1.ID)
            {
                _vert0 = vert0;
                _vert1 = vert1;
            }
            else
            {
                _vert0 = vert1;
                _vert1 = vert0;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Edge && this == (Edge)obj;
        }
        public override int GetHashCode()
        {
            return _vert0.ID.GetHashCode() ^ _vert1.ID.GetHashCode();
        }
        public static bool operator == (Edge x, Edge y)
        {
            return x._vert0.ID == y._vert0.ID && x._vert1.ID == y._vert1.ID;
        }
        public static bool operator != (Edge x, Edge y)
        {
            return !(x == y);
        }
    }

    public struct Vert
    {
        public readonly Vector3 Pos;
        public readonly int ID;
        public Vert(Vector3 pos, int id)
        {
            Pos = pos;
            ID = id;
        }
    }

    private struct Tri
    {
        private readonly Vert _vert0;
        public Vert Vert0 { get{ return _vert0; } }
        private readonly Vert _vert1;
        public Vert Vert1 { get{ return _vert1; } }
        private readonly Vert _vert2;
        public Vert Vert2 { get{ return _vert2; } }

        private readonly Vector3 _normal;
        public Vector3 Normal { get{ return _normal; } }

        private readonly Edge _edge0;
        public Edge Edge0 { get{ return _edge0; } }
        private readonly Edge _edge1;
        public Edge Edge1 { get { return _edge1; } }
        private readonly Edge _edge2;
        public Edge Edge2 { get { return _edge2; } }
        
        public IEnumerable<Vert> Verts
        {
            get
            {
                yield return _vert0;
                yield return _vert1;
                yield return _vert2;
            }
        }

        public IEnumerable<Edge> Edges
        {
            get
            {
                yield return _edge0;
                yield return _edge1;
                yield return _edge2;
            }
        }

        public Tri(int vert0Id,
        int vert1Id,
        int vert2Id,
        List<Vector3> points,
        Vector3 normalGuide)
            :this(points[vert0Id], points[vert1Id], points[vert2Id], vert0Id, vert1Id, vert2Id, normalGuide)
        { }

        public Tri(Vector3 vert0,
          Vector3 vert1,
          Vector3 vert2,
          int vert0Id,
          int vert1Id,
          int vert2Id,
          Vector3 normalGuide)
        {
            _vert0 = new Vert(vert0, vert0Id);
            Vector3 toNormalGuide = (normalGuide - vert0).normalized;
            Vector3 normal = Vector3.Cross(vert0 - vert1, vert0 - vert2).normalized;
            if(Vector3.Dot(toNormalGuide, normal) < 0)
            {
                _normal = normal;
                _vert1 = new Vert(vert1, vert1Id);
                _vert2 = new Vert(vert2, vert2Id);
            }
            else
            {
                _normal = normal * -1;
                _vert1 = new Vert(vert2, vert2Id);
                _vert2 = new Vert(vert1, vert1Id);
            }
            _edge0 = new Edge(_vert0, _vert1);
            _edge1= new Edge(_vert1, _vert2);
            _edge2 = new Edge(_vert0, _vert2);
        }
    }
}