using UnityEngine;

public class SpawnManager2D : MonoBehaviour {
    public Transform[] points;

    public Vector3 GetSpawnPoint() {
        if (points == null || points.Length == 0) return Vector3.zero;
        return points[Random.Range(0, points.Length)].position;
    }
}
