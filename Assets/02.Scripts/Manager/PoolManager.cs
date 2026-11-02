using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// 자주 생성·삭제되는 이펙트와 데미지 텍스트를 재사용해 Instantiate/Destroy 비용을 줄인다.
// 처음 Spawn할 때 씬에 만들어지고, 씬을 다시 불러오면 함께 사라지도록 DontDestroyOnLoad를 쓰지 않는다.
public class PoolManager : MonoBehaviour
{
    private static PoolManager _instance = null;

    private readonly Dictionary<GameObject, ObjectPool<PooledObject>> _pools = new Dictionary<GameObject, ObjectPool<PooledObject>>();

    // 쉬고 있는 오브젝트를 담아 두는 비활성 부모. 여기 있는 동안은 Update와 렌더링이 멈춘다.
    private Transform _inactiveRoot;

    private static PoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObject("PoolManager").AddComponent<PoolManager>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        GameObject inactiveRoot = new GameObject("Inactive");
        inactiveRoot.SetActive(false);
        _inactiveRoot = inactiveRoot.transform;
        _inactiveRoot.SetParent(transform, false);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null, float lifetime = 0f) where T : Component
    {
        GameObject instance = Spawn(prefab.gameObject, position, rotation, parent, lifetime);
        return instance.GetComponent<T>();
    }

    // lifetime이 0보다 크면 그 시간 뒤에 자동으로 풀에 돌아간다.
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, float lifetime = 0f)
    {
        PooledObject pooled = Instance.GetPool(prefab).Get();
        Transform pooledTransform = pooled.transform;
        // 비활성 부모 아래에서 위치를 먼저 잡은 뒤 옮겨야 OnEnable이 올바른 위치에서 실행된다.
        pooledTransform.SetPositionAndRotation(position, rotation);
        pooledTransform.SetParent(parent, true);
        pooled.OnSpawned();
        if (lifetime > 0f)
        {
            pooled.ReleaseAfter(lifetime);
        }
        return pooled.gameObject;
    }

    private ObjectPool<PooledObject> GetPool(GameObject prefab)
    {
        if (_pools.TryGetValue(prefab, out ObjectPool<PooledObject> pool))
        {
            return pool;
        }
        pool = new ObjectPool<PooledObject>(
            createFunc: () => Create(prefab),
            actionOnDestroy: pooled =>
            {
                if (pooled != null)
                {
                    Destroy(pooled.gameObject);
                }
            },
            collectionCheck: false,
            defaultCapacity: 16,
            maxSize: 200);
        _pools.Add(prefab, pool);
        return pool;
    }

    private PooledObject Create(GameObject prefab)
    {
        // 비활성 부모 아래에 만들어 생성 순간에는 Awake/OnEnable이 실행되지 않게 한다.
        GameObject instance = Instantiate(prefab, _inactiveRoot);
        if (instance.TryGetComponent(out PooledObject pooled) == false)
        {
            pooled = instance.AddComponent<PooledObject>();
        }
        // 다 재생되면 스스로 Destroy되는 파티클은 풀에 돌아오도록 콜백으로 바꾼다.
        if (instance.TryGetComponent(out ParticleSystem particle) && particle.main.stopAction == ParticleSystemStopAction.Destroy)
        {
            ParticleSystem.MainModule main = particle.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }
        pooled.Init(_pools[prefab], _inactiveRoot);
        return pooled;
    }
}
