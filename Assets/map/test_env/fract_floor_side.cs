using UnityEngine;
using System.Collections;

using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class FloorGeneratorSide : MonoBehaviour
{
    [Header("Grid Size")]
    public int   width     = 10;
    public int   height    = 10;

    private List<(float x, float y, float w, float h)> _rect_tiles;

    [Header("Crack Width (world units)")]
    public float border    = 0.02f;
    public float   heightVar = 0.5f;
    public float tileHeight = 0.5f;

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


    void CreateTile(float x, float y, float w, float h)
    {   
        var go = new GameObject($"Tile_{x}_{y}");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(x, 0, y);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        // 1) Prepare instances of the two materials so we can tweak them
        Material topMatInstance, sideMatInstance;

        if (topMaterial != null)
        {
            topMatInstance = new Material(topMaterial);
        }
        else
        {
            topMatInstance = new Material(Shader.Find("Standard"));
        }
        topMatInstance.mainTexture = topAtlas;

        if (sideMaterial != null)
        {
            sideMatInstance = new Material(sideMaterial);
        }
        else
        {
            sideMatInstance = new Material(Shader.Find("Standard"));
        }
        sideMatInstance.mainTexture = sideAtlas;

        mr.materials = new[] { topMatInstance, sideMatInstance };

        mf.mesh = BuildRectMesh(w, h);

        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh   = mf.mesh;
        //mc.subMeshIndex = 0;
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
        float x1 = hwidth  - border;
        float z0 = border;
        float z1 = hheight - border;
        float y0 = 0f;
        float y1 = tileHeight;

        var verts   = new List<Vector3>();
        var uvs     = new List<Vector2>();
        var norms   = new List<Vector3>();
        var tris0   = new List<int>(); // Submesh Top
        var tris1   = new List<int>(); // Submesh Size 

        // Top Quad
        int topIndex = verts.Count;
        verts.AddRange(new[]
        {
            new Vector3(x0, y1, z0),
            new Vector3(x1, y1, z0),
            new Vector3(x1, y1, z1),
            new Vector3(x0, y1, z1),
        });
        norms.AddRange(new[]{ Vector3.up, Vector3.up, Vector3.up, Vector3.up });

        Rect uvR = uvRects[0];
        float uB = border / topAtlas.width;
        float vB = border / topAtlas.height;
        uvs.AddRange(new[]
        {
            new Vector2(uvR.xMin + uB, uvR.yMin + vB),
            new Vector2(uvR.xMax - uB, uvR.yMin + vB),
            new Vector2(uvR.xMax - uB, uvR.yMax - vB),
            new Vector2(uvR.xMin + uB, uvR.yMax - vB),
        });

        tris0.AddRange(new[]
        {
            topIndex,   topIndex+2, topIndex+1,
            topIndex,   topIndex+3, topIndex+2
        });

        // Left Quad
        int leftIndex = verts.Count;
        verts.AddRange(new[]
        {
            new Vector3(x0, y0, z0),
            new Vector3(0f, y0, z0),
            new Vector3(0f, y1, z0),
            new Vector3(x0, y1, z0),
        });
        norms.AddRange(new[]{ Vector3.back, Vector3.back, Vector3.back, Vector3.back });
        uvs.AddRange(new[]
        {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(1,1),
            new Vector2(0,1),
        });
        tris1.AddRange(new[]
        {
            leftIndex,   leftIndex+2, leftIndex+1,
            leftIndex,   leftIndex+3, leftIndex+2
        });

        // Right Quad
        int rightIndex = verts.Count;
        verts.AddRange(new[]
        {
            new Vector3(x1, y0, z1),
            new Vector3(hwidth, y0, z1),
            new Vector3(hwidth, y1, z1),
            new Vector3(x1, y1, z1),
        });
        norms.AddRange(new[]{ Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward });
        uvs.AddRange(new[]
        {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(1,1),
            new Vector2(0,1),
        });
        tris1.AddRange(new[]
        {
            rightIndex,   rightIndex+2, rightIndex+1,
            rightIndex,   rightIndex+3, rightIndex+2
        });


        mesh.subMeshCount = 2;
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);

        mesh.SetTriangles(tris0, 0);
        mesh.SetTriangles(tris1, 1);

        mesh.RecalculateBounds();
        return mesh;
    }
}
