using UnityEngine;
using System.Collections;

using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class FloorGenerator : MonoBehaviour
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

    [ContextMenu("Generate Floor")]
    public void GenerateFloor()
    {
        // clear old tiles
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        // Generate rect world
        _rect_tiles = new List<(float x, float y, float w, float h)>();
        SplitHalf(0, 0, width, height);
        Debug.Log(string.Join(", ", _rect_tiles.Select(r => $"({r.x},{r.y},{r.w},{r.h})")));

        foreach (var rect in _rect_tiles)
        {
            float prob = UnityEngine.Random.value;
            if (prob < 0.1)
                continue;

            CreateTile(rect.x, rect.y, rect.w, rect.h);
        }
    }


    void CreateTile(float x, float y, float w, float h){
        
        var go = new GameObject($"Tile_{x}_{y}");
        go.transform.SetParent(transform, false);

        float randomHeight = UnityEngine.Random.Range(0, heightVar);
        go.transform.localPosition = new Vector3(x, randomHeight, y);

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

        mf.mesh = BuildRectMesh(w, h);

        go.AddComponent<MeshCollider>().sharedMesh = mf.mesh;
    }

    void SplitHalf(float hleft, float htop, float hwidth, float hheight){
        if (hwidth + hheight < 4 || hwidth < 1.2 || hheight < 0.8){
            _rect_tiles.Add((x: hleft, y: htop, w: hwidth, h: hheight));
            return;
        }

        float prob = UnityEngine.Random.value;
        bool forceVerticalSplit = hwidth > hheight * 3f;
        bool forceHorizontalSplit = hheight > hwidth * 3f;

        if ((prob < 0.3f || forceVerticalSplit) && !forceHorizontalSplit){
            // Vertical Split
            float max_variation = hwidth * 0.2f;
            float split_variation = UnityEngine.Random.Range(-max_variation, max_variation);
            float split_line = hwidth / 2 + split_variation;

            SplitHalf(hleft, htop, split_line, hheight);
            SplitHalf(hleft + split_line, htop, hwidth - split_line, hheight);
        }
        else{
            // Horizontal Split
            float max_variation = hheight * 0.2f;
            float split_variation = UnityEngine.Random.Range(-max_variation, max_variation);
            float split_line = hheight / 2 + split_variation;

            SplitHalf(hleft, htop, hwidth, split_line);
            SplitHalf(hleft, htop + split_line, hwidth, hheight - split_line);
        }
    }

    Mesh BuildRectMesh(float hwidth, float hheight)
    {
        var mesh = new Mesh();

        float x0 = border;
        float x1 = hwidth - border;
        float z0 = border;
        float z1 = hheight - border;

        mesh.vertices = new Vector3[]
        {
            new Vector3(x0, 0, z0),
            new Vector3(x1, 0, z0),
            new Vector3(x1, 0, z1),
            new Vector3(x0, 0, z1),
        };

        mesh.triangles = new int[]
        {
            0, 2, 1,
            0, 3, 2
        };

        // UVs inset by the same fraction of the atlas
        Rect uvR = uvRects[0];
        float uB = border / topAtlas.width;
        float vB = border / topAtlas.height;

        mesh.uv = new Vector2[]
        {
            new Vector2(uvR.xMin + uB, uvR.yMin + vB),
            new Vector2(uvR.xMax - uB, uvR.yMin + vB),
            new Vector2(uvR.xMax - uB, uvR.yMax - vB),
            new Vector2(uvR.xMin + uB, uvR.yMax - vB),
        };

        mesh.RecalculateNormals();
        return mesh;
    }
}
