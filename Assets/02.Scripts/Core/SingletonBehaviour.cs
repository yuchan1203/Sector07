using UnityEngine;

// 씬 단위 싱글톤 베이스. 씬을 다시 불러오면 함께 사라지도록 DontDestroyOnLoad를 쓰지 않는다.
// 파생 클래스에서 Awake/OnDestroy가 필요하면 override 하고 base를 호출한다.
public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    private static T _instance = null;
    public static T Instance => _instance;

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this as T;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
