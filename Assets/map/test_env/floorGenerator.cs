using UnityEngine;
using System.Collections;

using System.Collections.Generic;
using System.Linq;
using System;

[ExecuteInEditMode]
public class floorGenerator : MonoBehaviour
{
    [Header("Arena Size")]
    public float width = 10; // X-Axis
    public float height = 5; // Y-Axis
    public float depth = 10; // Z-Axis

    [Header("Tiles Size")]
    public float minPerimeter = 4;
    public float minWidth = 1.2f;
    public float minDepth = 0.8f;
    public float maxElevation = 0.5f;

    [Header("Fractures")]
    public float fractureSize = 0.02f;

    [Header("Rendering")]
    public Texture2D topAtlas;
    public Texture2D sideAtlas;

    public Rect[] uvRects; // Using it entire for now
    public int[,] pattern; // Only using one pattern for now

    public Material topMaterial;
    public Material sideMaterial;

    #region Private Vars

    private List<GameObject> floorTiles = new List<GameObject>();

    #endregion

    void Start()
    {
        // GenerateFloor();
    }

    [ContextMenu("Generate Floor")]
    public void GenerateFloor()
    {
        // Destroy existing floor
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        List<Rect> _tile_array = new List<Rect>();
        SplitHalf(new Rect(0f, 0f, width, depth), _tile_array);

        Debug.Log(string.Join(", ", _tile_array));

        float distArena = Vector2.Distance(new Vector2(0, 0), new Vector2(width, depth));

        foreach (Rect rect in _tile_array)
        {
            float prob = UnityEngine.Random.value * 100;

            float modifier = 80;

            float distTile = Vector2.Distance(new Vector2(rect.x, rect.y), new Vector2(width, depth));
            float distNomalized = distTile / distArena;
            
            modifier *= (float)Math.Pow(distNomalized, 3);

            Debug.Log(distNomalized);
            if  (prob > modifier)
            {
                CreateTile(rect);
            }
        }

    }

    void SplitHalf(Rect rect, List<Rect> tile_array)
    {
        if ( rect.width + rect.height < minPerimeter || rect.width < minWidth || rect.height < minDepth)
        {
            tile_array.Add(rect);
            return;
        }

        float prob = UnityEngine.Random.value;
        bool forceV = rect.width > rect.height * 3f;
        bool forceH = rect.height > rect.width * 3f;

        if ((prob < 0.3f || forceV) && !forceH)
        {
            // Vertical Split
            float max_variation = rect.width * 0.2f;
            float split_variation = UnityEngine.Random.Range(-max_variation, max_variation);
            float split_line = rect.width / 2 + split_variation;

            Rect leftRect = new Rect(rect.x, rect.y, split_line, rect.height);
            Rect rightRect = new Rect(rect.x + split_line, rect.y, rect.width - split_line, rect.height);
            SplitHalf(leftRect, tile_array);
            SplitHalf(rightRect, tile_array);
        }
        else
        {
            // Horizontal Split
            float max_variation = rect.height * 0.2f;
            float split_variation = UnityEngine.Random.Range(-max_variation, max_variation);
            float split_line = rect.height / 2 + split_variation;

            Rect upperRect = new Rect(rect.x, rect.y, rect.width, split_line);
            Rect lowerRect = new Rect(rect.x, rect.y + split_line, rect.width, rect.height - split_line);
            SplitHalf(upperRect, tile_array);
            SplitHalf(lowerRect, tile_array);
        }
    }

    GameObject CreateTile(Rect rect)
    {
        var gameObj = new GameObject($"Tile_{transform.childCount:D3}");
        gameObj.transform.SetParent(transform, false);

        float randElevation= UnityEngine.Random.Range(0, maxElevation);
        gameObj.transform.localPosition = new Vector3(rect.x, randElevation, rect.y);

        var meshFilter = gameObj.AddComponent<MeshFilter>();

        float f = fractureSize;
        Rect localRect = new Rect(f, f, rect.width - f, rect.height - f);
        meshFilter.mesh = BuildTileMesh(localRect, randElevation);

        var meshRenderer = gameObj.AddComponent<MeshRenderer>();

        topMaterial.mainTexture = topAtlas;
        sideMaterial.mainTexture = sideAtlas;
        meshRenderer.sharedMaterials = new Material[]{
            topMaterial,
            sideMaterial,
            sideMaterial
        };

        return gameObj;
    }

    Mesh BuildTileMesh(Rect rect, float elevation)
    {
        float x = rect.x;
        float y = 0f;
        float z = rect.y;
        float w = rect.width;
        float h = elevation;
        float d = rect.height;

        Vector3 p1 = new Vector3(w, -height, 0);
        Vector3 p0 = new Vector3(0, -height, 0);
        Vector3 p2 = new Vector3(w, -height, d);
        Vector3 p3 = new Vector3(0, -height, d);
        Vector3 p4 = new Vector3(0, h, 0);
        Vector3 p5 = new Vector3(w, h, 0);
        Vector3 p6 = new Vector3(w, h, d);
        Vector3 p7 = new Vector3(0, h, d);

        var verts = new List<Vector3>();
        verts.AddRange(new[]
        {
            // Top
            p4, p5, p6, p7,

            // Left
            p0, p4, p7, p3,

            // Back
            p0, p1, p5, p4
        });

        var norms = new List<Vector3>();
        norms.AddRange(new[]
        {
            // Top
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,

            // Left
            Vector3.left, Vector3.left, Vector3.left, Vector3.left,

            // Back
            Vector3.back, Vector3.back, Vector3.back, Vector3.back
        });

        Vector2 _00 = new Vector2( 0f, 0f );
        Vector2 _10 = new Vector2( 1f, 0f );
        Vector2 _01 = new Vector2( 0f, 1f );
        Vector2 _11 = new Vector2( 1f, 1f );

        var uvs = new List<Vector2>();
        uvs.AddRange(new[]
        {
            // Top
            _00, _10, _11, _01,

            // Left
            _00, _10, _11, _01,

            // Back
            _00, _10, _11, _01,
        });

        var trigTop = new List<int>();
        trigTop.AddRange(new[]
        {
            // Top
            0, 2, 1,
            0, 3, 2
        });

        var trigLeft = new List<int>();
        trigLeft.AddRange(new[]
        {
            // Left
            4, 6, 5,
            4, 7, 6
        });
        var trigBack = new List<int>();
        trigBack.AddRange(new[]
        {
            // Back
            8, 10, 9,
            8, 11, 10
        });



        var mesh = new Mesh();

        mesh.subMeshCount = 3;

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(trigTop, 0);
        mesh.SetTriangles(trigLeft, 1);
        mesh.SetTriangles(trigBack, 2);

        mesh.RecalculateBounds();
        return mesh;
    }
}
