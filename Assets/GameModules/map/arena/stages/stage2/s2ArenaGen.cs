using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public struct TileList
{
    public List<Rect> floorTiles;
    public List<Rect> outerRightWallTiles;
    public List<Rect> outerLeftWallTiles;
    public List<Rect> innerRightWallTiles;
    public List<Rect> innerLeftWallTiles;

    public TileList(bool random)
    {
        floorTiles          = new List<Rect>();
        outerRightWallTiles = new List<Rect>();
        outerLeftWallTiles  = new List<Rect>();
        innerRightWallTiles = new List<Rect>();
        innerLeftWallTiles  = new List<Rect>();
    }
}


public class s2ArenaGen : MonoBehaviour
{

    #region Public Vars

    [Header("Arena Size")]
    public float width = 20; // X-Axis
    public float height = 0.5f; // Y-Axis
    public float depth = 20; // Z-Axis

    [Header("Tiles Size")]
    public float widthScaleFactor = 10;

    [Header("Walls Size")]
    public float wallHeight = 5;

    [Header("Rendering")]
    public Texture2D topAtlas;
    public Texture2D sideAtlas;

    public Rect[] uvRects; // Using it entire for now
    public int[,] pattern; // Only using one pattern for now

    public Material topMaterial;
    public Material sideMaterial;

    #endregion

    #region Private Vars

    private TileList tiles = new TileList(true);

    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        topMaterial.mainTexture = topAtlas;
        sideMaterial.mainTexture = sideAtlas;
        
        GenerateFloor();
    }

    [ContextMenu("Generate Floor")]
    public void GenerateFloor()
    {
        // Destroy existing floor
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        TileList tiles = makeArenaTiles(new Rect(0f, 0f, width, depth));
    }

    #region Random Gen Alg
    private TileList makeArenaTiles(Rect arenaDim)
    {
        TileList tiles = new TileList(true);

        int tilesSize = Mathf.FloorToInt(width / widthScaleFactor);

        int columns = Mathf.FloorToInt(width / tilesSize);
        int rows = Mathf.FloorToInt(height / tilesSize);

        for (int y = 0; y < rows - 1; y++)
        {
            tiles.innerRightWallTiles.Add(new Rect(0, y * tilesSize, tilesSize, tilesSize));
        }
        for (int x = 0; x < columns - 1; x++)
        {
            tiles.innerLeftWallTiles.Add(new Rect(x * tilesSize, 0, tilesSize, tilesSize));
        }

        for (int y = 1; y < rows - 1; y++)
        {
            for (int x = 1; x < columns - 1; x++)
            {
                tiles.floorTiles.Add(new Rect(x * tilesSize, y * tilesSize, tilesSize, tilesSize));
            }
        }

        for (int y = 0; y < rows; y++)
        {
            tiles.outerLeftWallTiles.Add(new Rect(columns * tilesSize, y * tilesSize, tilesSize, tilesSize));
        }
        for (int x = 0; x < columns; x++)
        {
            tiles.outerRightWallTiles.Add(new Rect(x * tilesSize, rows * tilesSize, tilesSize, tilesSize));
        }

        globalHelpers.printRectList(tiles.floorTiles);
        return tiles;
    }
    #endregion

    #region Floor
    GameObject CreateTile(Rect rect, Transform parent)
    {
        var gameObj = new GameObject($"Tile_{parent.childCount:D3}");
        gameObj.transform.SetParent(parent, false);

        gameObj.SetActive(false);

        gameObj.transform.localPosition = new Vector3(rect.x, 0, rect.y);

        var meshFilter = gameObj.AddComponent<MeshFilter>();

        meshFilter.mesh = BuildTileMesh(rect, 0);

        var meshRenderer = gameObj.AddComponent<MeshRenderer>();

        meshRenderer.sharedMaterials = new Material[]{
            topMaterial,
            sideMaterial,
            sideMaterial
        };

        var collider = gameObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(rect.width, 0.2f, rect.height);
        collider.center = new Vector3(rect.width * 0.5f, 0 + height - 0.2f, rect.height * 0.5f);

        return gameObj;
    }

    Mesh BuildTileMesh(Rect rect, float elevation)
    {
        float x = rect.x;
        float y = 0f;
        float z = rect.y;
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
    #endregion

    #region Outer Walls
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
        float x = rect.x;
        float y = 0f;
        float z = rect.y;
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

    #region Inner Walls
    GameObject CreateInnerWallTile(Rect rect, Transform parent)
    {
        var gameObj = new GameObject($"Outer_Wall_{parent.childCount:D3}");
        gameObj.transform.SetParent(parent, false);

        gameObj.transform.localPosition = new Vector3(rect.x, 0, rect.y);

        var collider = gameObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(rect.width, wallHeight, rect.height);
        collider.center = new Vector3(rect.width * 0.5f, wallHeight * 0.5f, rect.height * 0.5f);

        return gameObj;
    }
    #endregion


    #region Testing and Debuggin
    [ContextMenu("Show Rect Map")]
    public void ShowRectMap()
    {
        
        TileList tiles = makeArenaTiles(new Rect(0f, 0f, width, depth));

        MapWindow.Tiles = tiles;

        // instantiate/focus it
        var win = EditorWindow.GetWindow<MapWindow>("Rect Map");
        win.minSize = new Vector2(200, 200);
        win.Show();

    }

    private class MapWindow : EditorWindow
    {
        public static TileList Tiles;
        const float M = 10f;

        // Persisted between GUI calls:
        Vector2 _scrollPos;
        Dictionary<Rect, Color> _floorColors;

        // wall base colors
        static readonly Color _outerRightBase = new Color(1f, 0.4f, 0.4f);
        static readonly Color _outerLeftBase  = new Color(0.4f, 1f, 0.4f);
        static readonly Color _innerRightBase = new Color(0.4f, 0.4f, 1f);
        static readonly Color _innerLeftBase  = new Color(1f, 1f, 0.4f);

        void OnEnable()
        {
            GenerateFloorColors();
        }

        void GenerateFloorColors()
        {
            _floorColors = new Dictionary<Rect, Color>();
            foreach (var r in Tiles.floorTiles)
            {
                // one random color per floor tile
                _floorColors[r] = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.6f, 0.8f);
            }
        }

        void OnGUI()
        {
            if (Tiles.floorTiles == null)
            {
                EditorGUILayout.LabelField("No tiles to display. Run ShowRectMap() first.");
                return;
            }

            // figure out how big the canvas needs to be
            float maxX = 0f, maxY = 0f;
            foreach (var list in new[] {
                Tiles.floorTiles,
                Tiles.outerRightWallTiles,
                Tiles.outerLeftWallTiles,
                Tiles.innerRightWallTiles,
                Tiles.innerLeftWallTiles })
            {
                foreach (var r in list)
                {
                    maxX = Mathf.Max(maxX, r.x + r.width);
                    maxY = Mathf.Max(maxY, r.y + r.height);
                }
            }
            var canvasPx = new Rect(0, 0, maxX * M, maxY * M);

            _scrollPos = GUI.BeginScrollView(
                new Rect(0, 0, position.width, position.height),
                _scrollPos,
                canvasPx,
                alwaysShowHorizontal: true,
                alwaysShowVertical: true
            );

            // draw floor
            foreach (var r in Tiles.floorTiles)
            {
                var drawR = new Rect(r.x * M, r.y * M, r.width * M, r.height * M);
                EditorGUI.DrawRect(drawR, _floorColors[r]);
            }

            // helper to draw walls
            void DrawWalls(List<Rect> list, Color baseColor)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var r = list[i];
                    var drawR = new Rect(r.x * M, r.y * M, r.width * M, r.height * M);
                    // alternate light/dark
                    float t = (i % 2 == 0) ? 0.3f : -0.3f;
                    Color c = (t > 0)
                        ? Color.Lerp(baseColor, Color.white, t)
                        : Color.Lerp(baseColor, Color.black, -t);
                    EditorGUI.DrawRect(drawR, c);
                }
            }

            DrawWalls(Tiles.outerRightWallTiles, _outerRightBase);
            DrawWalls(Tiles.outerLeftWallTiles,  _outerLeftBase);
            DrawWalls(Tiles.innerRightWallTiles, _innerRightBase);
            DrawWalls(Tiles.innerLeftWallTiles,  _innerLeftBase);

            GUI.EndScrollView();
        }
    }
    #endregion
}
