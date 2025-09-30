using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class floorGen : MonoBehaviour
{
    [Header("Arena Size")]
    public float width = 20;
    public float height = 0.5f;
    public float depth = 20;

    [Header("Tiles Size")]
    public float minPerimeter = 4;
    public float minWidth = 1.2f;
    public float minDepth = 0.8f;
    public float maxElevation = 0.25f;

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

    [Header("Dependencies")]
    [Tooltip("The spawner that will populate the level with enemies.")]
    public EnemySpawner enemySpawner;
    [Tooltip("The component responsible for baking the NavMesh at runtime.")]
    public NavMeshBaker navMeshBaker;

    private List<GameObject> floorTiles = new List<GameObject>();
    private List<GameObject> wallTiles = new List<GameObject>();

    void Start()
    {
        topMaterial.mainTexture = topAtlas;
        sideMaterial.mainTexture = sideAtlas;
        GenerateFloor();
    }

    public List<GameObject> GetFloorTiles()
    {
        return floorTiles;
    }

    [ContextMenu("Generate Floor")]
    public void GenerateFloor()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        floorTiles.Clear();
        wallTiles.Clear();

        List<Rect> _tile_array = new List<Rect>();
        SplitHalf(new Rect(0f, 0f, width, depth), _tile_array);

        Transform floorParent = new GameObject("Floor").transform;
        floorParent.SetParent(transform, false);
        floorParent.gameObject.AddComponent<NavMeshSurface>(); // Add NavMeshSurface for the tiles

        Transform wallsParent = new GameObject("Walls").transform;
        wallsParent.SetParent(transform, false);

        CreateFloorCollider(new Rect(0, 0, width, depth), floorParent);

        foreach (Rect rect in _tile_array)
        {
            if (rect.x == 0 || rect.y == 0)
            {
                wallTiles.Add(CreateInnerWallTile(rect, wallsParent));
                continue; }
            if (rect.x + rect.width > width - 0.1f)
            {
                wallTiles.Add(CreateOuterWallTile(rect, wallsParent));
                continue; }
            if (rect.y + rect.height > depth - 0.1f)
            {
                wallTiles.Add(CreateOuterWallTile(rect, wallsParent));
                continue; }
            floorTiles.Add(CreateTile(rect, floorParent));
        }

        if (Application.isPlaying)
        {
            StartCoroutine(GenerationSequence());
        }
        else
        {
            foreach (var tile in floorTiles) { tile.SetActive(true); }
        }
    }

    private IEnumerator GenerationSequence()
    {
        yield return StartCoroutine(FloorRisingAnimation(floorTiles));

        if (navMeshBaker != null)
        {
            navMeshBaker.BakeNavMesh();
        }
        else
        {
            Debug.LogError("NavMeshBaker is not assigned on the FloorGenerator!", this);
            yield break;
        }

        yield return new WaitForEndOfFrame();

        if (enemySpawner != null)
        {
            enemySpawner.SpawnEnemies();
        }
        else
        {
            Debug.LogWarning("EnemySpawner is not assigned. No enemies will be spawned.", this);
        }
    }

    #region Random Gen Alg
    void SplitHalf(Rect rect, List<Rect> tile_array)
    {
        if (rect.width + rect.height < minPerimeter || rect.width < minWidth || rect.height < minDepth)
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

    #region Floor
    GameObject CreateTile(Rect rect, Transform parent)
    {
        var gameObj = new GameObject($"Tile_{parent.childCount:D3}");
        gameObj.transform.SetParent(parent, false);

        gameObj.SetActive(false);

        float randElevation = UnityEngine.Random.Range(0, maxElevation);
        gameObj.transform.localPosition = new Vector3(rect.x, randElevation, rect.y);

        var meshFilter = gameObj.AddComponent<MeshFilter>();

        float f = fractureSize;
        Rect localRect = new Rect(f, f, rect.width - f, rect.height - f);
        meshFilter.mesh = BuildTileMesh(localRect, randElevation);

        var meshRenderer = gameObj.AddComponent<MeshRenderer>();

        meshRenderer.sharedMaterials = new Material[] {
            topMaterial,
            sideMaterial,
            sideMaterial
        };

        var collider = gameObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(rect.width, 0.2f, rect.height);
        collider.center = new Vector3(rect.width * 0.5f, randElevation + height - 0.2f, rect.height * 0.5f);

        return gameObj;
    }

    Mesh BuildTileMesh(Rect rect, float elevation)
    {
        float w = rect.width;
        float h = height;
        float d = rect.height;

        Vector3 p1 = new Vector3(w, 0, 0);
        Vector3 p0 = new Vector3(0, 0, 0);
        Vector3 p2 = new Vector3(w, 0, d);
        Vector3 p3 = new Vector3(0, 0, d);
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
    #endregion

    #region Walls & Collider
    GameObject CreateOuterWallTile(Rect rect, Transform parent)
    {
        var gameObj = new GameObject($"Outer_Wall_{parent.childCount:D3}");
        gameObj.transform.SetParent(parent, false);

        gameObj.transform.localPosition = new Vector3(rect.x, 0, rect.y);

        var meshFilter = gameObj.AddComponent<MeshFilter>();

        meshFilter.mesh = BuildOuterWallTileMesh(rect);

        var meshRenderer = gameObj.AddComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = topMaterial;

        var collider = gameObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(rect.width, wallHeight, rect.height);
        collider.center = new Vector3(rect.width * 0.5f, wallHeight * 0.5f, rect.height * 0.5f);

        return gameObj;
    }

    Mesh BuildOuterWallTileMesh(Rect rect)
    {
        float w = rect.width;
        float h = wallHeight;
        float d = rect.height;

        Vector3 p0 = new Vector3(0, 0, 0);
        Vector3 p1 = new Vector3(w, 0, 0);
        Vector3 p2 = new Vector3(w, 0, d);
        Vector3 p3 = new Vector3(0, 0, d);
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

    GameObject CreateInnerWallTile(Rect rect, Transform parent)
    {
        var gameObj = new GameObject($"Inner_Wall_{parent.childCount:D3}");
        gameObj.transform.SetParent(parent, false);

        gameObj.transform.localPosition = new Vector3(rect.x, 0, rect.y);

        var collider = gameObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(rect.width, wallHeight, rect.height);
        collider.center = new Vector3(rect.width * 0.5f, wallHeight * 0.5f, rect.height * 0.5f);

        return gameObj;
    }

    GameObject CreateFloorCollider(Rect rect, Transform parent)
    {
        var gameObj = new GameObject($"Floor_Collider");
        gameObj.transform.SetParent(parent, false);

        gameObj.transform.localPosition = new Vector3(rect.x, 0, rect.y);

        var collider = gameObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(rect.width, 0.2f, rect.height);
        collider.center = new Vector3(rect.width * 0.5f, height + maxElevation, rect.height * 0.5f);

        return gameObj;
    }
    #endregion

    #region Mount Animation
    public IEnumerator FloorRisingAnimation(List<GameObject> tiles, float delay = 0.005f, float riseDistance = 10f, float duration = 0.3f)
    {
        foreach (GameObject tile in tiles)
        {
            tile.SetActive(true);

            Vector3 startPos = tile.transform.localPosition;
            Vector3 below = startPos - new Vector3(0f, riseDistance, 0f);

            // Start from below
            tile.transform.localPosition = below;

            // Animate rise using LeanTween
            LeanTween.moveLocal(tile, startPos, duration).setEaseOutBack();

            yield return new WaitForSeconds(delay);
        }
        yield return new WaitForSeconds(duration);
    }
    #endregion

    #region Testing and Debuggin
    [ContextMenu("Show Rect Map")]
    public void ShowRectMap()
    {
        List<Rect> _tile_array = new List<Rect>();
        SplitHalf(new Rect(0f, 0f, width, depth), _tile_array);

        MapWindow.Rects = _tile_array;

        // instantiate/focus it
        var win = EditorWindow.GetWindow<MapWindow>("Rect Map");
        win.minSize = new Vector2(200, 200);
        win.Show();
    }

    private class MapWindow : EditorWindow
    {
        public static List<Rect> Rects;
        const float M = 10f;
        void OnGUI()
        {
            if (Rects == null || Rects.Count == 0)
            {
                EditorGUILayout.LabelField("No rects to display.");
                return;
            }

            // compute world-bounds
            float minX = Rects.Min(r => r.x);
            float minY = Rects.Min(r => r.y);
            float maxX = Rects.Max(r => r.x + r.width);
            float maxY = Rects.Max(r => r.y + r.height);

            float availW = position.width - 2 * M;
            float availH = position.height - 2 * M;
            float totalW = maxX - minX;
            float totalH = maxY - minY;
            float s = Mathf.Min(availW / totalW, availH / totalH);

            // draw each rect
            for (int i = 0; i < Rects.Count; i++)
            {
                var r = Rects[i];
                var dr = new Rect(
                    M + (r.x - minX) * s,
                    M + (r.y - minY) * s,
                    r.width * s,
                    r.height * s
                );
                var c = Color.HSVToRGB(i / (float)Rects.Count, 0.7f, 0.8f);
                EditorGUI.DrawRect(dr, c);
            }
        }
    }
    #endregion
}