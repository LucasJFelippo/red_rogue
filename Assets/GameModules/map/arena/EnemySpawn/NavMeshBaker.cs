using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class NavMeshBaker : MonoBehaviour
{
    private NavMeshSurface navMeshSurface;
    public void BakeNavMesh()
    {
        navMeshSurface = GetComponentInChildren<NavMeshSurface>();

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh baked successfully at runtime.");
        }
        else
        {
            Debug.LogError("Could not find NavMeshSurface component to bake! Make sure the 'Floor' object is being created.", this);
        }
    }
}

