using UnityEngine;
using System.Collections;

using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class FloorGeneratorOld : MonoBehaviour
{
    [Header("Grid Size")]
    public int   width    = 10;
    public int   height   = 10;
    public float tileSize = 1f;

    private List<(float x, float y, float w, float h)> _rect_world;

    [Header("Crack Width (world units)")]
    public float border   = 0.05f;

    [Header("Atlas & UVs")]
    public Texture2D atlas;       // your tile‐atlas
    public Rect[]    uvRects;     // UV rect for each tile type
    public int[,]    pattern;     // pattern[x,y] → index into uvRects

    [Header("Optional")]
    public Material  tileMaterial; // a Material that uses your atlas


    void Start()
    {
        // If you haven’t filled pattern in the inspector,
        // you can procedurally generate it here:

        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        if (pattern == null || pattern.GetLength(0) != width)
            pattern = MakeDemoPattern();

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                CreateTile(x, y);
    }

    void tCreateTile(int x, int y, int w, int height){
        
        var go = new GameObject($"Tile_{x}_{y}");
        go.transform.SetParent(transform, false);
    }


    void split_half(float hleft, float htop, float hwidth, float hheight){
        if (hwidth + hheight < 2 || hwidth < 0.6 || hheight < 0.4){
            _rect_world.Add((x: hleft, y: htop, w: hwidth, h: hheight));
            return;
        }

        float prob = UnityEngine.Random.value;

        if (prob < 0.3f){
            float max_variation = hwidth * 0.2f;
            float split_variation = UnityEngine.Random.Range(-max_variation, max_variation);
            float split_line = hwidth / 2 + split_variation;

            split_half(hleft, htop, split_line, hheight);
            split_half(hleft + split_line, htop, hwidth - split_line, hheight);
        }
        else{
            float max_variation = hheight * 0.2f;
            float split_variation = UnityEngine.Random.Range(-max_variation, max_variation);
            float split_line = hheight / 2 + split_variation;

            split_half(hleft, htop, hwidth, split_line);
            split_half(hleft, htop + split_line, hwidth, hheight - split_line);
        }
    }







    [ContextMenu("Generate Floor")]
    public void GenerateTiles()
    {
        // clear old tiles
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        
        // Generate rect world
        _rect_world = new List<(float x, float y, float w, float h)>();
        split_half(0, 0, width, height);
        Debug.Log(string.Join(", ", _rect_world.Select(r => $"({r.x},{r.y},{r.w},{r.h})")));

        // recreate them
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                CreateTile(x, y);
    }

    void CreateTile(int x, int y)
    {
        Debug.Log($"[FloorGenerator] CreateTile({x},{y}) → idx {pattern[x,y]}");
        int idx = pattern[x, y];
        if (idx < 0) return;

        int t = pattern[x, y];
        if (t < 0) return;   // skip “empty” tiles if you want

        // 1) Make a new GameObject for this tile
        var go = new GameObject($"Tile_{x}_{y}");
        go.transform.parent = transform;
        go.transform.localPosition = new Vector3(x * tileSize, 0, y * tileSize);

        // 2) Add the mesh components
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        if (tileMaterial != null)
            mr.material = tileMaterial;
        else
        {
            // fallback: create a simple material
            mr.material = new Material(Shader.Find("Standard"));
            mr.material.mainTexture = atlas;
        }

        // 3) Build a single “inset” quad
        Debug.Log($"[FloorGenerator] Created mesh with {mf.mesh.vertexCount} verts for tile {x},{y}");
        mf.mesh = BuildRectMesh(t);

        // 4) If you need physics/raycasting
        go.AddComponent<MeshCollider>().sharedMesh = mf.mesh;

        // 5) (Optional) add your interaction script
        // go.AddComponent<YourTileBehavior>();
    }

    Mesh BuildRectMesh(int tileIndex)
    {
        var mesh = new Mesh();

        // inset corners by `border`
        float x0 = border;
        float x1 = tileSize - border;
        float z0 = border;
        float z1 = tileSize - border;

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
        Rect uvR = uvRects[tileIndex];
        float uB = border / atlas.width;
        float vB = border / atlas.height;

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

    // Just an example to fill `pattern` if you didn’t hook one up in the Inspector
    int[,] MakeDemoPattern()
    {
        var p = new int[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                p[x, y] = Random.Range(0, uvRects.Length);
        return p;
    }
}