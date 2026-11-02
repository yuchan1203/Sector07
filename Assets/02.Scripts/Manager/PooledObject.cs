using UnityEngine;
using UnityEngine.Pool;

// PoolManager가 만든 오브젝트에 붙어, 자신을 원래 풀에 돌려보내는 역할을 한다.
public class PooledObject : MonoBehaviour
{
    private ObjectPool<PooledObject> _pool;
    private Transform _inactiveRoot;
    private bool _isReleased = true;

    public void Init(ObjectPool<PooledObject> pool, Transform inactiveRoot)
    {
        _pool = pool;
        _inactiveRoot = inactiveRoot;
    }

    public void OnSpawned()
    {
        _isReleased = false;
    }

    public void ReleaseAfter(float seconds)
    {
        CancelInvoke(nameof(Release));
        Invoke(nameof(Release), seconds);
    }

    public void Release()
    {
        // 수명 타이머와 파티클 콜백이 겹쳐도 한 번만 돌려보낸다.
        if (_isReleased)
        {
            return;
        }
        _isReleased = true;
        CancelInvoke(nameof(Release));

        // 씬이 바뀌어 풀이 사라졌으면 그냥 삭제한다.
        if (_inactiveRoot == null)
        {
            Destroy(gameObject);
            return;
        }
        transform.SetParent(_inactiveRoot, false);
        _pool.Release(this);
    }

    // 파티클의 Stop Action이 Callback일 때 재생이 끝나면 호출된다.
    private void OnParticleSystemStopped()
    {
        Release();
    }

    // 풀에서 꺼낸 오브젝트면 돌려보내고, 아니면 기존처럼 삭제한다.
    public static void ReleaseOrDestroy(GameObject target)
    {
        if (target.TryGetComponent(out PooledObject pooled) && pooled._pool != null)
        {
            pooled.Release();
            return;
        }
        Destroy(target);
    }
}
