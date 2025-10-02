using UnityEngine;
using System.Collections.Generic;

public struct TileLists
{
    public List<GameObject> floorTiles;
    public List<GameObject> outerRightWallTiles;
    public List<GameObject> outerLeftWallTiles;
    public List<GameObject> innerRightWallTiles;
    public List<GameObject> innerLeftWallTiles;

    public TileLists(bool random)
    {
        floorTiles          = new List<GameObject>();
        outerRightWallTiles = new List<GameObject>();
        outerLeftWallTiles  = new List<GameObject>();
        innerRightWallTiles = new List<GameObject>();
        innerLeftWallTiles  = new List<GameObject>();
    }

    public void clearTiles()
    {
        floorTiles.Clear();
        outerRightWallTiles.Clear();
        outerLeftWallTiles.Clear();
        innerRightWallTiles.Clear();
        innerLeftWallTiles.Clear();
    }
}