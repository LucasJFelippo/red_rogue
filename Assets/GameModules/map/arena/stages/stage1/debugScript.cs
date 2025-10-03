using UnityEngine;
using UnityEngine.AI;

[ExecuteAlways]
public class NavMeshDebugger : MonoBehaviour
{
    void OnDrawGizmos()
    {
        var tri = NavMesh.CalculateTriangulation();
        Gizmos.color = Color.blue;

        // Draw every triangle
        for (int i = 0; i < tri.indices.Length; i += 3)
        {
            Vector3 v0 = tri.vertices[tri.indices[i]];
            Vector3 v1 = tri.vertices[tri.indices[i + 1]];
            Vector3 v2 = tri.vertices[tri.indices[i + 2]];

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v0);
        }
    }
}
