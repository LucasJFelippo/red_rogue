using UnityEngine;
using System.Collections.Generic;

public interface IArenaGenInterface
{
    float ArenaWidth { get; }
    float ArenaHeight { get; }
    float ArenaDepth { get; }

    void GenerateArena();
    void GenerateNavMesh();

    List<GameObject> GetFloorTiles();
}
