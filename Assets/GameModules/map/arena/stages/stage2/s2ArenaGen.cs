using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// IArenaGenInterface
public class s2ArenaGen : MonoBehaviour
{
    [Header("Arena Size")]
    public float arenaWidth = 20;
    public float arenaHeight = 0;
    public float arenaDepth = 20;

    public float ArenaWidth  => arenaWidth;
    public float ArenaHeight => arenaHeight;
    public float ArenaDepth  => arenaDepth;

    private float floorColliderHeight = 0.2f;

    [Header("Rendering")]
    public Texture2D topAtlas;
    public Texture2D sideAtlas;
    public Rect[] uvRects;
    public int[,] pattern;
    public Material topMaterial;
    public Material sideMaterial;

    [Header("Internals")]
    private TileLists _arenaTiles = new TileLists(true);
    private GameObject _floorGameObject = null;


    [Header("Dependencies")]
    public EnemySpawner enemySpawner;
}
