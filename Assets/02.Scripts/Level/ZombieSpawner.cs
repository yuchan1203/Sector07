using System;
using UnityEngine;
using UnityEngine.AI;

// 구역 좀비 그룹 오브젝트에 붙이는 스포너. 그룹이 켜질 때 좀비를 지정한 위치 주변에 한꺼번에 만든다.
// 좀비는 이 오브젝트의 자식으로 생기므로 LevelManager가 그대로 구역의 좀비로 센다.
public class ZombieSpawner : MonoBehaviour
{
    [Serializable]
    private struct SpawnEntry
    {
        public GameObject prefab;
        [Tooltip("클수록 자주 나온다.")]
        public int weight;
    }

    [SerializeField] private SpawnEntry[] _entries;
    [Tooltip("만들 좀비 수의 범위(최소, 최대). 이 안에서 무작위로 정한다.")]
    [SerializeField] private Vector2Int _countRange = new Vector2Int(20, 30);
    [Tooltip("좀비를 흩뿌릴 중심 위치들. 매번 이 중 하나를 골라 그 주변에 만든다.")]
    [SerializeField] private Transform[] _spawnPoints;
    [Tooltip("중심 위치에서 퍼지는 반경(m)")]
    [SerializeField] private float _spread = 6f;

    private const int MaxAttemptsPerZombie = 10;
    // 필드 초기화 시점에는 만들 수 없어서 처음 쓸 때 만든다.
    private NavMeshPath _path;
    private bool _spawned;

    private void OnEnable()
    {
        if (_spawned)
        {
            return;
        }
        _spawned = true;
        Spawn();
    }

    private void Spawn()
    {
        if (_entries == null || _entries.Length == 0 || _spawnPoints == null || _spawnPoints.Length == 0)
        {
            GameLogger.LogError("ZombieSpawner: 좀비 목록이나 위치가 비어 있습니다. (" + name + ")");
            return;
        }

        int total = 0;
        foreach (SpawnEntry entry in _entries)
        {
            total += Mathf.Max(0, entry.weight);
        }
        if (total <= 0)
        {
            GameLogger.LogError("ZombieSpawner: 가중치 합이 0입니다. (" + name + ")");
            return;
        }

        int count = UnityEngine.Random.Range(_countRange.x, Mathf.Max(_countRange.x, _countRange.y) + 1);
        int failed = 0;
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = PickPrefab(total);
            if (prefab == null || !TryFindPosition(out Vector3 position))
            {
                failed++;
                continue;
            }
            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            Instantiate(prefab, position, rotation, transform);
        }
        if (failed > 0)
        {
            GameLogger.LogWarning("ZombieSpawner: " + failed + "마리를 놓을 자리를 찾지 못했습니다. (" + name + ")");
        }
    }

    private GameObject PickPrefab(int totalWeight)
    {
        int roll = UnityEngine.Random.Range(0, totalWeight);
        foreach (SpawnEntry entry in _entries)
        {
            roll -= Mathf.Max(0, entry.weight);
            if (roll < 0)
            {
                return entry.prefab;
            }
        }
        return null;
    }

    private bool TryFindPosition(out Vector3 position)
    {
        for (int attempt = 0; attempt < MaxAttemptsPerZombie; attempt++)
        {
            Transform point = _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)];
            if (point == null)
            {
                continue;
            }
            Vector2 offset = UnityEngine.Random.insideUnitCircle * _spread;
            Vector3 candidate = point.position + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas) && IsReachable(point.position, hit.position))
            {
                position = hit.position;
                return true;
            }
        }
        position = Vector3.zero;
        return false;
    }

    // 건물 안이나 지붕처럼 중심 위치에서 걸어갈 수 없는 자리에는 좀비를 두지 않는다.
    private bool IsReachable(Vector3 from, Vector3 to)
    {
        if (!NavMesh.SamplePosition(from, out NavMeshHit anchor, 2f, NavMesh.AllAreas))
        {
            return false;
        }
        if (_path == null)
        {
            _path = new NavMeshPath();
        }
        _path.ClearCorners();
        return NavMesh.CalculatePath(anchor.position, to, NavMesh.AllAreas, _path) && _path.status == NavMeshPathStatus.PathComplete;
    }

    private void OnDrawGizmosSelected()
    {
        if (_spawnPoints == null)
        {
            return;
        }
        Gizmos.color = Color.red;
        foreach (Transform point in _spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, _spread);
            }
        }
    }
}
