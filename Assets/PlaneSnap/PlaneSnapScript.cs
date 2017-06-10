﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class PlaneSnapScript : Paintable
{
    public float Margin;
    
    private HashSet<Tri> _triList;

    private Mesh _mesh;
    private MeshFilter _meshFilter;

    private void Start()
    {
        _mesh = new Mesh();
        _meshFilter = GetComponent<MeshFilter>();
        _meshFilter.mesh = _mesh;
        _triList = new HashSet<Tri>(GetInitialTris());
        UpdateMesh(_mesh, _triList);
    }

    private static List<Vert> GetBox()
    {
        Vert[] ret = new Vert[8];
        ret[0] = new Vert(new Vector3(1, 1, 1));
        ret[1] = new Vert(new Vector3(1, 0, 1));
        ret[2] = new Vert(new Vector3(1, 1, 0));
        ret[3] = new Vert(new Vector3(1, 0, 0));
        ret[4] = new Vert(new Vector3(0, 1, 1));
        ret[5] = new Vert(new Vector3(0, 0, 1));
        ret[6] = new Vert(new Vector3(0, 1, 0));
        ret[7] = new Vert(new Vector3(0, 0, 0));
        return ret.ToList();
    }

    private void UpdateMesh(Mesh mesh, IEnumerable<Tri> tris)
    {
        List<Vector3> vertices = new List<Vector3>();
        Dictionary<Vert, int> vertsDictionary = new Dictionary<Vert, int>();
        List<int> triangle = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        foreach (Tri tri in tris)
        {
            foreach (Vert polyVert in tri.Verts)
            {
                if (!vertsDictionary.ContainsKey(polyVert))
                {
                    vertices.Add(polyVert.Pos);
                    normals.Add(tri.Normal);
                    vertsDictionary.Add(polyVert, vertsDictionary.Count);
                }
                int newIndex = vertsDictionary[polyVert];
                triangle.Add(newIndex);
            }
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangle.ToArray();
        mesh.normals = normals.ToArray();
    }

    private static Tri GetInitialTri(Vert vert0, Vert vert1, Vert vert2, Vector3 centerPoint)
    {
        Vector3 polyCenter = (vert0.Pos + vert1.Pos + vert2.Pos) / 3;
        Vector3 normalGuide = centerPoint - polyCenter;
        return new Tri(vert0, vert1, vert2, normalGuide);
    }

    private static IEnumerable<Tri> GetTrisOfCube(Vert vert0, Vert vert1, Vert vert2, Vert vert3)
    {
        yield return GetInitialTri(vert0, vert1, vert2, Vector3.one / 2);
        yield return GetInitialTri(vert1, vert2, vert3, Vector3.one / 2);
    }
    private static List<Tri> GetInitialTris()
    {
        List<Vert> pointsList = GetBox();
        List<Tri> ret = new List<Tri>();
        ret.AddRange(GetTrisOfCube(pointsList[0], pointsList[1], pointsList[2], pointsList[3]));
        ret.AddRange(GetTrisOfCube(pointsList[2], pointsList[3], pointsList[6], pointsList[7]));
        ret.AddRange(GetTrisOfCube(pointsList[6], pointsList[7], pointsList[4], pointsList[5]));
        ret.AddRange(GetTrisOfCube(pointsList[0], pointsList[1], pointsList[4], pointsList[5]));
        ret.AddRange(GetTrisOfCube(pointsList[4], pointsList[0], pointsList[6], pointsList[2]));
        ret.AddRange(GetTrisOfCube(pointsList[1], pointsList[3], pointsList[5], pointsList[7]));
        return ret;
    }

    public override void AddPoint(Vector3 point)
    {
        Vert vert = new Vert(point);
        bool addPoint = false;
        Tri[] oldTriList = _triList.ToArray();
        foreach (Tri tri in oldTriList)
        {
            bool newTris = ProcessTri(tri, vert);
            addPoint = addPoint || newTris;
        }
        UpdateMesh(_mesh, _triList);
    }

    private bool ProcessTri(Tri tri, Vert vert)
    {
        if(!tri.IsPointCloseToTrianglesPlane(vert.Pos, Margin))
        {
            return false;
        }

        EdgeDistStatus[] EdgeDistStatus = tri.Edges.Select(edge => new EdgeDistStatus(edge, vert.Pos, Margin)).ToArray();

        if (EdgeDistStatus.Any(item => item.WithinMargin) || tri.IsPointWithinBounds(vert.Pos))
        {
            _triList.Remove(tri);
            foreach (EdgeDistStatus newEdgeSource in EdgeDistStatus.Where(item => !item.WithinMargin))
            {
                Tri newTri = TriFromEdgeToPoint(newEdgeSource.Edge, vert, -tri.Normal);
                _triList.Add(newTri);
            }
            return true;
        }
        return false;
    }

    private Tri TriFromEdgeToPoint(Edge newEdgeSource, Vert point, Vector3 normal)
    {
        Vert vert0 = newEdgeSource.Vert0;
        Vert vert1 = newEdgeSource.Vert1;
        return new Tri(vert0, vert1, point, normal);
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
            _vert0 = vert0;
            _vert1 = vert1;
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
            return _vert0.GetHashCode() ^ _vert1.GetHashCode();
        }
        public static bool operator ==(Edge x, Edge y)
        {
            return (x._vert0 == y._vert0 && x._vert1 == y._vert1)
            || (x._vert0 == y._vert1 && x._vert1 == y._vert0);
        }
        public static bool operator !=(Edge x, Edge y)
        {
            return !(x == y);
        }
    }
    
    public class Vert
    {
        public readonly Vector3 Pos;
        public Vert(Vector3 pos)
        {
            Pos = pos;
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

        public Tri(Vert vert0,
            Vert vert1,
            Vert vert2,
            Vector3 normalGuide)
        {
            _vert0 = vert0;
            Vector3 normal = Vector3.Cross(vert0.Pos - vert1.Pos, vert0.Pos - vert2.Pos).normalized;
            if (Vector3.Dot(normalGuide, normal) < 0)
            {
                _vert1 = vert1;
                _vert2 = vert2;
                _plane = new Plane(vert0.Pos, vert1.Pos, vert2.Pos);
            }
            else
            {
                _vert1 = vert2;
                _vert2 = vert1;
                _plane = new Plane(vert0.Pos, vert2.Pos, vert1.Pos);
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