using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class FlexorScript : Paintable
{
    public float FlexDist;

    private MeshFilter _meshFilter;
    private List<Vert> _pointList;
    private Mesh _mesh;
    private List<Tri> _triList;

    private void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _pointList = GetBox();
        _triList = GetInitialTris(_pointList);
        _mesh = new Mesh();
        UpdateMesh();
        _meshFilter.mesh = _mesh;
    }

    public override void AddPoint(Vector3 newPoint)
    {
        Vert newVert = new Vert(newPoint);
        _pointList.Add(newVert);

        FaceStatus faceStates = GetFaceStates(newPoint, _triList);
        _triList = faceStates.Keep.ToList();
        IEnumerable<Edge> openEdges = faceStates.GetOpenEdges();
        IEnumerable<Tri> newTris = CreateNewFacesToPoint(newVert, openEdges);
        _triList.AddRange(newTris);

        List<FlexerTriPoint> flexPoints = GetFlexerPoints(newTris);
        UpdateFlexerMesh(flexPoints);
        UpdateMesh();
    }

    private List<FlexerTriPoint> GetFlexerPoints(IEnumerable<Tri> newTris)
    {
        List<FlexerTriPoint> ret = new List<FlexerTriPoint>();
        List<Vert> pointsForFlex = GetPointsForFlex();
        foreach (Vert vert in pointsForFlex)
        {
            foreach (Tri tri in newTris)
            {
                FlexerTriPoint? triPoint = GetFlexerTriPoint(tri, vert);
                if(triPoint.HasValue)
                {
                    ret.Add(triPoint.Value);
                }
            }
        }
        return ret;
    }

    private FlexerTriPoint? GetFlexerTriPoint(Tri tri, Vert vert)
    {
        float dist = tri.GetDistToTriPlane(vert.Pos);
        if(Mathf.Abs(dist) > FlexDist)
        {
            return null;
        }
        bool withinBounds = tri.IsPointWithinBounds(vert.Pos);
        if(!withinBounds)
        {
            return null;
        }
        return new FlexerTriPoint(vert, tri, dist);
    }

    private List<Vert> GetPointsForFlex()
    {
        HashSet<Vert> triVerts = new HashSet<Vert>();
        foreach (Tri tri in _triList)
        {
            triVerts.Add(tri.Vert0);
            triVerts.Add(tri.Vert1);
            triVerts.Add(tri.Vert2);
        }
        return _pointList.Where(item => !triVerts.Contains(item)).ToList();
    }

    private void UpdateFlexerMesh(List<FlexerTriPoint> points)
    {
        while (points.Count != 0)
        {
            FlexerTriPoint bestPoint = GetBestPoint(points);
            IEnumerable<Tri> newTriangles = DrawTrianglesToPoint(bestPoint);
            IEnumerable<FlexerTriPoint> newTriPoints = GetFlexerPoints(newTriangles);
            points = points.Where(item => item.Tri != bestPoint.Tri).ToList();
            points.AddRange(newTriPoints);
            _triList.AddRange(newTriangles);
        }
    }

    private Tri TriFromEdgeAndVert(Edge edge, Vert vert, Vector3 normalsGuide)
    {
        return new Tri(vert, edge.Vert0, edge.Vert1, normalsGuide);
    }

    private IEnumerable<Tri> DrawTrianglesToPoint(FlexerTriPoint bestPoint)
    {
        yield return TriFromEdgeAndVert(bestPoint.Tri.Edge0, bestPoint.Point, bestPoint.Tri.Normal);
        yield return TriFromEdgeAndVert(bestPoint.Tri.Edge1, bestPoint.Point, bestPoint.Tri.Normal);
        yield return TriFromEdgeAndVert(bestPoint.Tri.Edge2, bestPoint.Point, bestPoint.Tri.Normal);
    }

    private static FlexerTriPoint GetBestPoint(List<FlexerTriPoint> points)
    {
        FlexerTriPoint currentBest = points[0];
        for (int i = 1; i < points.Count; i++)
        {
            if(points[i].DistToTri < currentBest.DistToTri)
            {
                currentBest = points[i];
            }
        }
        return currentBest;
    }

    private struct FlexerTriPoint
    {
        private readonly Vert _point;
        public Vert Point { get { return _point; } }

        private readonly Tri _tri;
        public Tri Tri { get { return _tri; } }

        private readonly float _distToTri;
        public float DistToTri { get { return _distToTri; } }

        public FlexerTriPoint(Vert point, Tri tri, float distToTri)
        {
            _point = point;
            _tri = tri;
            _distToTri = distToTri;
        }
    }

    private static IEnumerable<Tri> CreateNewFacesToPoint(Vert newVert, IEnumerable<Edge> openEdges)
    {
        List<Tri> ret = new List<Tri>();
        Vector3 normalGuide = GetEdgeAverage(openEdges) - newVert.Pos;
        foreach (Edge edge in openEdges)
        {
            Tri newTri = new Tri(newVert, edge.Vert0, edge.Vert1, normalGuide);
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

    private static List<Tri> GetInitialTris(List<Vert> pointsList)
    {
        List<Tri> ret = new List<Tri>();
        ret.AddRange(GetTrisOfCube(pointsList[0], pointsList[1], pointsList[2], pointsList[3]));
        ret.AddRange(GetTrisOfCube(pointsList[2], pointsList[3], pointsList[6], pointsList[7]));
        ret.AddRange(GetTrisOfCube(pointsList[6], pointsList[7], pointsList[4], pointsList[5]));
        ret.AddRange(GetTrisOfCube(pointsList[0], pointsList[1], pointsList[4], pointsList[5]));
        ret.AddRange(GetTrisOfCube(pointsList[4], pointsList[0], pointsList[6], pointsList[2]));
        ret.AddRange(GetTrisOfCube(pointsList[1], pointsList[3], pointsList[5], pointsList[7]));
        return ret;
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
                    if (dictionary.ContainsKey(edge))
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

    private void UpdateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        Dictionary<Vert, int> vertsDictionary = new Dictionary<Vert, int>();
        List<int> triangle = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        foreach (Tri tri in _triList)
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

        _mesh.Clear();
        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangle.ToArray();
        _mesh.normals = normals.ToArray();
    }

    private List<Vert> GetBox()
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

    private struct Edge
    {
        private readonly Vert _vert0;
        public Vert Vert0 { get { return _vert0; } }
        private readonly Vert _vert1;
        public Vert Vert1 { get { return _vert1; } }

        private readonly float _length;
        public float Length { get { return _length; } }

        public Edge(Vert vert0, Vert vert1)
        {
            _vert0 = vert0;
            _vert1 = vert1;
            _length = (_vert0.Pos - _vert1.Pos).magnitude;
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
        public Vector3 Normal { get { return _plane.normal; } }

        private readonly Vert _vert0;
        public Vert Vert0 { get { return _vert0; } }
        private readonly Vert _vert1;
        public Vert Vert1 { get { return _vert1; } }
        private readonly Vert _vert2;
        public Vert Vert2 { get { return _vert2; } }

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

        internal float GetDistToTriPlane(Vector3 point)
        {
            return _plane.GetDistanceToPoint(point);
        }

        internal bool IsPointWithinBounds(Vector3 point)
        {
            return Edges.All(edge => edge.IsWithinSegmentBounds(point));
        }
    }
}