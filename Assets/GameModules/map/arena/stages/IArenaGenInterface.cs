using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public interface IArenaGenInterface
{
    float ArenaWidth { get; }
    float ArenaHeight { get; }
    float ArenaDepth { get; }

    void GenerateArena();
    void GenerateNavMesh();

    List<GameObject> GetFloorTiles();

    IEnumerator SpawnAnimation(float delay = 0.005f, float riseDistance = 10f, float duration = 0.3f);
    IEnumerator StageCompletedAnimation();
}
