using UnityEngine;
using System.Collections;

using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class MakeTestCube : MonoBehaviour
{
    [Header("Grid Size")]
    public int   width     = 10;
    public int   height    = 10;

    private List<(float x, float y, float w, float h)> _rect_tiles;

    [Header("Crack Width (world units)")]
    public float border    = 0.02f;
    public float   heightVar = 0.5f;

    [Header("Atlas & UVs")]
    public Texture2D topAtlas;
    public Texture2D sideAtlas;
    public Rect[]    uvRects;     // Using it as entire
    public int[,]    pattern;     // Only one pattern

    [Header("Optional")]
    public Material  topMaterial;
    public Material  sideMaterial;


    void Start()
    {
        
    }

    [ContextMenu("Make Cube")]
    public void GenerateFloor()
    {
        // clear old tiles
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        createCube(0f, 0f, 10f, 10f);
    }


    void createCube(float x, float z, float w, float h){
        
        var go = new GameObject($"Tile_{x}_{z}");
        go.transform.SetParent(transform, false);

        float randomHeight = UnityEngine.Random.Range(0, heightVar);
        go.transform.localPosition = new Vector3(x, randomHeight, z);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        if (topMaterial != null)
        {
            mr.material = topMaterial;
            mr.material.mainTexture = topAtlas;
        }
        else
        {
            mr.material = new Material(Shader.Find("Standard"));
            mr.material.mainTexture = topAtlas;
        }

        mf.mesh = BuildRectMesh(x, z, w, h);

        go.AddComponent<MeshCollider>().sharedMesh = mf.mesh;
    }

    Mesh BuildRectMesh(float x, float z, float w, float h)
    {
        var mesh = new Mesh();

        float y = 0f;
        float u = 10f;

        Vector3 p0 = new Vector3(0, 0, 0);
        Vector3 p1 = new Vector3(w, 0, 0);
        Vector3 p2 = new Vector3(w, h, 0);
        Vector3 p3 = new Vector3(0, h, 0);
        Vector3 p4 = new Vector3(0, 0, u);
        Vector3 p5 = new Vector3(w, 0, u);
        Vector3 p6 = new Vector3(w, h, u);
        Vector3 p7 = new Vector3(0, h, u);

        var verts   = new List<Vector3>();
        verts.AddRange(new[]
        {
            // Top
            p5, p6, p7, p4,

            // Left
            p1, p2, p6, p5,

            // Front
            p2, p3, p7, p6
        });

        var norms   = new List<Vector3>();
        norms.AddRange(new[]
        {
            // Top
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,

            // Left
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,

            // Front
            Vector3.left, Vector3.left, Vector3.left, Vector3.left
        });

        Vector2 _00 = new Vector2( 0f, 0f );
        Vector2 _10 = new Vector2( 1f, 0f );
        Vector2 _01 = new Vector2( 0f, 1f );
        Vector2 _11 = new Vector2( 1f, 1f );

        var uvs   = new List<Vector2>();
        uvs.AddRange(new[]
        {
            // Top
            _00, _10, _01, _11,

            // Left
            _00, _10, _01, _11,

            // Front
            _00, _10, _01, _11
        });


        var trigs = new List<int>();
        trigs.AddRange(new[]
        {
            // Top
            0, 1, 2,
            0, 2, 3,

            // Left
            4, 5, 6,
            4, 6, 7,

            // Front
            8, 9, 10,
            8, 10, 11

        });

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(trigs, 0);

        mesh.RecalculateBounds();
        return mesh;
    }
}
