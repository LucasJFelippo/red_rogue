using UnityEngine;
using System.Collections.Generic;

public interface IArenaGenInterface
{
    void GenerateArena();
    void GenerateNavMesh();

    List<GameObject> GetFloorTiles();
}
