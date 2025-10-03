using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.AI;
using Unity.AI.Navigation;


public class s1Gen : MonoBehaviour, IArenaGenInterface
{
    [Header("Arena Size")]
    public float arenaWidth = 20;
    public float arenaHeight = 0;
    public float arenaDepth = 20;

    private float floorColliderHeight = 0.2f;

    [Header("Tiles Size")]
    public float minTilePerimeter = 4;
    public float minTileWidth = 1.2f;
    public float minTileDepth = 0.8f;

    public float tileHeight = 0.5f;
    public float maxTileDescend = 0.25f;

    [Header("Walls Size")]
    public float wallHeight = 5;

    [Header("Fractures")]
    public float fractureSize = 0.05f;

    [Header("Rendering")]
    public Texture2D topAtlas;
    public Texture2D sideAtlas;
    public Rect[] uvRects;
    public int[,] pattern;
    public Material topMaterial;
    public Material sideMaterial;

    [Header("Internals")]
    private TileLists arenaTiles = new TileLists(true);


    [Header("Dependencies")]
    public EnemySpawner enemySpawner;

    void Awake()
    {
        GameManager.instance.ChangeArenaGen(this);
    }

    void Start()
    {
        topMaterial.mainTexture = topAtlas;
        sideMaterial.mainTexture = sideAtlas;
    }

    #region Arena Generation
    [ContextMenu("Generate Arena")]
    public void GenerateArena()
    {

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        arenaTiles.clearTiles();

        List<Rect> _tileArray = new List<Rect>();
        SplitHalf(new Rect(0f, 0f, arenaWidth, arenaDepth), _tileArray);

        Transform floorParent = new GameObject("Floor").transform;
        floorParent.SetParent(transform, false);

        Transform wallsParent = new GameObject("Walls").transform;
        wallsParent.SetParent(transform, false);


        CreateFloorCollider(new Rect(0, 0, arenaWidth, arenaDepth), floorParent);


        foreach (Rect tileDim in _tileArray)
        {
            if (tileDim.x == 0)
            {
                arenaTiles.innerLeftWallTiles.Add(CreateInnerWallTile(tileDim, wallsParent));
            }
            else if (tileDim.y == 0)
            {
                arenaTiles.innerRightWallTiles.Add(CreateInnerWallTile(tileDim, wallsParent));
            }
            else if (tileDim.x + tileDim.width > arenaWidth - 0.1f)
            {
                arenaTiles.outerRightWallTiles.Add(CreateOuterWallTile(tileDim, wallsParent));
            }
            else if (tileDim.y + tileDim.height > arenaDepth - 0.1f)
            {
                arenaTiles.outerLeftWallTiles.Add(CreateOuterWallTile(tileDim, wallsParent));
            }
            else
            {
                arenaTiles.floorTiles.Add(CreateFloorTile(tileDim, floorParent));
            }
        }



        var floorNavMeshSurf = floorParent.gameObject.AddComponent<NavMeshSurface>();
        floorNavMeshSurf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        floorNavMeshSurf.BuildNavMesh();



        if (Application.isPlaying)
        {
        }
        else
        {
            // Enable floor in Edit Mode
            foreach (var tile in arenaTiles.floorTiles) { tile.GetComponent<MeshRenderer>().enabled = true; }
        }
    }

    #endregion
    #region GameObjects Creators
    GameObject CreateFloorCollider(Rect arenaDim, Transform parent)
    {
        var gameObj = new GameObject($"Floor_Collider");
        gameObj.transform.SetParent(parent, false);

        gameObj.transform.localPosition = new Vector3(arenaDim.x, 0, arenaDim.y);

        var collider = gameObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(arenaDim.width, floorColliderHeight, arenaDim.height);
        collider.center = new Vector3(arenaDim.width * 0.5f, arenaHeight, arenaDim.height * 0.5f);

        return gameObj;
    }

    GameObject CreateFloorTile(Rect tileDim, Transform parent)
    {
        var gameObj = new GameObject($"Tile_{parent.childCount:D3}");
        gameObj.transform.SetParent(parent, false);

        float randDescend = UnityEngine.Random.Range(0, maxTileDescend);
        gameObj.transform.localPosition = new Vector3(tileDim.x, -randDescend, tileDim.y);

        var meshFilter = gameObj.AddComponent<MeshFilter>();

        float f = fractureSize;
        Rect localTimeDim = new Rect(f, f, tileDim.width - f, tileDim.height - f);
        meshFilter.mesh = BuildFloorTileMesh(localTimeDim);

        var meshRenderer = gameObj.AddComponent<MeshRenderer>();
        meshRenderer.enabled = false;

        meshRenderer.sharedMaterials = new Material[] {
            topMaterial,
            sideMaterial,
            sideMaterial
        };

        return gameObj;
    }

    GameObject CreateInnerWallTile(Rect tileDim, Transform parent)
    {
        var gameObj = new GameObject($"Inner_Wall_{parent.childCount:D3}");
        gameObj.transform.SetParent(parent, false);

        gameObj.transform.localPosition = new Vector3(tileDim.x, arenaHeight, tileDim.y);

        var collider = gameObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(tileDim.width, wallHeight, tileDim.height);

        var verticalCenter = (wallHeight + floorColliderHeight) * 0.5f;
        collider.center = new Vector3(tileDim.width * 0.5f, verticalCenter, tileDim.height * 0.5f);

        return gameObj;
    }

    GameObject CreateOuterWallTile(Rect tileDim, Transform parent)
    {
        var gameObj = new GameObject($"Outer_Wall_{parent.childCount:D3}");
        gameObj.transform.SetParent(parent, false);

        gameObj.transform.localPosition = new Vector3(tileDim.x, arenaHeight, tileDim.y);

        var meshFilter = gameObj.AddComponent<MeshFilter>();

        meshFilter.mesh = BuildOuterWallTileMesh(tileDim);

        var meshRenderer = gameObj.AddComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = topMaterial;

        var collider = gameObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(tileDim.width, wallHeight, tileDim.height);

        var verticalCenter = (wallHeight + floorColliderHeight) * 0.5f;
        collider.center = new Vector3(tileDim.width * 0.5f, verticalCenter, tileDim.height * 0.5f);

        return gameObj;
    }

    #endregion
    #region Mesh Builders

    Mesh BuildFloorTileMesh(Rect tileDim)
    {
        float w = tileDim.width;
        float h = tileHeight;
        float d = tileDim.height;

        Vector3 p0 = new Vector3(0, -h, 0);
        Vector3 p1 = new Vector3(w, -h, 0);
        Vector3 p2 = new Vector3(w, -h, d);
        Vector3 p3 = new Vector3(0, -h, d);
        Vector3 p4 = new Vector3(0, 0, 0);
        Vector3 p5 = new Vector3(w, 0, 0);
        Vector3 p6 = new Vector3(w, 0, d);
        Vector3 p7 = new Vector3(0, 0, d);

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

        Vector2 _00 = new Vector2(0f, 0f);
        Vector2 _10 = new Vector2(1f, 0f);
        Vector2 _01 = new Vector2(0f, 1f);
        Vector2 _11 = new Vector2(1f, 1f);

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


    Mesh BuildOuterWallTileMesh(Rect tileDim)
    {
        float w = tileDim.width;
        float minH = tileHeight;
        float maxh = wallHeight;
        float d = tileDim.height;

        Vector3 p0 = new Vector3(0, -minH, 0);
        Vector3 p1 = new Vector3(w, -minH, 0);
        Vector3 p2 = new Vector3(w, -minH, d);
        Vector3 p3 = new Vector3(0, -minH, d);
        Vector3 p4 = new Vector3(0, maxh, 0);
        Vector3 p5 = new Vector3(w, maxh, 0);
        Vector3 p6 = new Vector3(w, maxh, d);
        Vector3 p7 = new Vector3(0, maxh, d);

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

        Vector2 _00 = new Vector2(0f, 0f);
        Vector2 _10 = new Vector2(1f, 0f);
        Vector2 _01 = new Vector2(0f, 1f);
        Vector2 _11 = new Vector2(1f, 1f);

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

        var trig = new List<int>();
        trig.AddRange(new[]
        {
            // Top
            0, 2, 1,
            0, 3, 2,
            // Left
            4, 6, 5,
            4, 7, 6,
            // Back
            8, 10, 9,
            8, 11, 10
        });



        var mesh = new Mesh();

        mesh.subMeshCount = 3;

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(trig, 0);

        mesh.RecalculateBounds();
        return mesh;
    }

    #endregion


    #region Floor Gen Alg
    void SplitHalf(Rect rect, List<Rect> tile_array)
    {
        if (rect.width + rect.height < minTilePerimeter || rect.width < minTileWidth || rect.height < minTileDepth)
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
    #endregion
}