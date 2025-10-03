using UnityEngine;
using System.Collections;

public interface IArenaGenInterface
{
    void GenerateArena();
    IEnumerator GenerateNavMesh();
}
