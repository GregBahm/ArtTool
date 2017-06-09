using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class NewTryScript : Paintable
{
    public float Margin;

    private List<Vector3> _pointList;
    private HashSet<Tri> _triList;

    private Mesh _mesh;
    private MeshFilter _meshFilter;

    private void Start()
    {
        _mesh = new Mesh();
        _meshFilter = GetComponent<MeshFilter>();
        _meshFilter.mesh = _mesh;
        CreateInitialMeshData();
        UpdateMesh(_mesh, _triList);
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

    private void CreateInitialMeshData()
    {
        Vector3 v0 = new Vector3(0, 0, 0);
        Vector3 v1 = new Vector3(0, 0, 1);
        Vector3 v2 = new Vector3(1, 0, 1);
        Vector3 v3 = new Vector3(1, 0, 0);
        _pointList = new List<Vector3> { v0, v1, v2, v3 };
        Tri tri0 = new Tri(v0, v1, v2, 0, 1, 2, new Vector3(0, 1, 0));

        _triList = new HashSet<Tri> { tri0, };
        
        //Tri tri1 = new Tri(v0, v2, v3, 0, 2, 3, new Vector3(0, 1, 0));
        //_triList = new List<Tri> { tri0, tri1 };
    }

    public override void AddPoint(Vector3 point)
    {
        bool addPoint = false;
        Tri[] oldTriList = _triList.ToArray();
        foreach (Tri tri in oldTriList)
        {
            bool newTris = ProcessTri(tri, point, _pointList.Count);
            addPoint = addPoint || newTris;
        }
        if(addPoint)
        {
            _pointList.Add(point);
        }
        UpdateMesh(_mesh, _triList);
    }

    private bool ProcessTri(Tri tri, Vector3 point, int pointId)
    {
        if(!tri.IsPointCloseToTrianglesPlane(point, Margin))
        {
            return false;
        }

        EdgeDistStatus[] EdgeDistStatus = tri.Edges.Select(edge => new EdgeDistStatus(edge, point, Margin)).ToArray();

        if (EdgeDistStatus.Any(item => item.WithinMargin) || tri.IsPointWithinBounds(point))
        {
            _triList.Remove(tri);
            foreach (EdgeDistStatus newEdgeSource in EdgeDistStatus.Where(item => !item.WithinMargin))
            {
                Tri newTri = TriFromEdgeToPoint(newEdgeSource.Edge, point, pointId, tri.Normal);
                _triList.Add(newTri);
            }
            return true;
        }
        return false;
    }

    private Tri TriFromEdgeToPoint(Edge newEdgeSource, Vector3 point, int pointId, Vector3 normal)
    {
        Vert vert0 = newEdgeSource.Vert0;
        Vert vert1 = newEdgeSource.Vert1;
        return new Tri(vert0.Pos, vert1.Pos, point, vert0.ID, vert1.ID, pointId, normal);
    }

    private struct EdgeDistStatus
    {
        private readonly Edge _edge;
        public Edge Edge { get { return _edge; } }

        private bool _withinMargin;
        public bool WithinMargin { get { return _withinMargin; } }

        public EdgeDistStatus(Edge edge, Vector3 point, float margin)
        {
            _edge = edge;
            _withinMargin = edge.DistanceTo(point) < margin;
        }
    }

    private struct Edge
    {
        private readonly Vert _vert0;
        public Vert Vert0 { get { return _vert0; } }
        private readonly Vert _vert1;
        public Vert Vert1 { get { return _vert1; } }

        private readonly float _length;
        public float Length{ get{ return _length; } }

        public Edge(Vert vert0, Vert vert1)
        {
            if (vert0.ID < vert1.ID)
            {
                _vert0 = vert0;
                _vert1 = vert1;
            }
            else
            {
                _vert0 = vert1;
                _vert1 = vert0;
            }
            _length = (_vert0.Pos - _vert1.Pos).magnitude;
        }

        private float DistanceTo(Vector3 edgeVertA, Vector3 edgeVertB, Vector3 point)
        {
            Vector3 normal = (edgeVertA - edgeVertB).normalized;
            Vector3 vect = point - edgeVertA;
            Vector3 projectedPoint = Vector3.Project(vect, normal) + edgeVertA;

            float distTo0 = (edgeVertA - point).magnitude;
            float distTo1 = (edgeVertB - point).magnitude;

            if (distTo0 > Length)
            {
                projectedPoint = edgeVertB;
            }
            if (distTo1 > Length)
            {
                projectedPoint = edgeVertA;
            }
            return (point - projectedPoint).magnitude;
        }

        public bool IsWithinSegmentBounds(Vector3 point)
        {
            return IsWithinSegmentBounds(Vert0.Pos, Vert1.Pos, point);
        }

        private bool IsWithinSegmentBounds(Vector3 edgeVertA, Vector3 edgeVertB, Vector3 point)
        {
            Vector3 normal = (edgeVertA - edgeVertB).normalized;
            Vector3 vect = point - edgeVertA;
            Vector3 projectedPoint = Vector3.Project(vect, normal) + edgeVertA;

            float distTo0 = (edgeVertA - projectedPoint).magnitude;
            float distTo1 = (edgeVertB - projectedPoint).magnitude;

            return (distTo0 < Length && distTo1 < Length);
        }

        public float DistanceTo(Vector3 point)
        {
            return DistanceTo(Vert0.Pos, Vert1.Pos, point);
        }

        public override bool Equals(object obj)
        {
            return obj is Edge && this == (Edge)obj;
        }
        public override int GetHashCode()
        {
            return _vert0.ID.GetHashCode() ^ _vert1.ID.GetHashCode();
        }

        public static bool operator ==(Edge x, Edge y)
        {
            return x._vert0.ID == y._vert0.ID && x._vert1.ID == y._vert1.ID;
        }
        public static bool operator !=(Edge x, Edge y)
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

    private class Tri
    {
        private readonly Plane _plane;

        private readonly Vert _vert0;
        public Vert Vert0 { get { return _vert0; } }
        private readonly Vert _vert1;
        public Vert Vert1 { get { return _vert1; } }
        private readonly Vert _vert2;
        public Vert Vert2 { get { return _vert2; } }
        public Vector3 Normal { get { return _plane.normal; } }

        private readonly Edge _edge0;
        public Edge Edge0 { get { return _edge0; } }
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
            : this(points[vert0Id], points[vert1Id], points[vert2Id], vert0Id, vert1Id, vert2Id, normalGuide)
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
            Vector3 normal = Vector3.Cross(vert0 - vert1, vert0 - vert2).normalized;
            if (Vector3.Dot(normalGuide, normal) > 0) //TODO: Might need to reverse this
            {
                _vert1 = new Vert(vert1, vert1Id);
                _vert2 = new Vert(vert2, vert2Id);
                _plane = new Plane(vert0, vert1, vert2);
            }
            else
            {
                _vert1 = new Vert(vert2, vert2Id);
                _vert2 = new Vert(vert1, vert1Id);
                _plane = new Plane(vert0, vert2, vert1);
            }

            _edge0 = new Edge(_vert0, _vert1);
            _edge1 = new Edge(_vert1, _vert2);
            _edge2 = new Edge(_vert0, _vert2);
        }

        

        internal bool IsPointCloseToTrianglesPlane(Vector3 point, float margin)
        {
            return Math.Abs(_plane.GetDistanceToPoint(point)) < margin;
        }

        internal bool IsPointWithinBounds(Vector3 point)
        {
            return Edges.All(edge => edge.IsWithinSegmentBounds(point));
        }
    }
}
