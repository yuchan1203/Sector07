using UnityEngine;

public class Mover : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 2f; // 위로 상승하는 속도
    [SerializeField] private float _duration = 1f;  // 유지되는 시간 (N초)

    private float _elapsedTime = 0f;

    // 풀에서 다시 꺼낼 때마다 시간을 처음부터 센다.
    private void OnEnable()
    {
        _elapsedTime = 0f;
    }

    void Update()
    {
        _elapsedTime += Time.deltaTime;

        // 위로 이동
        transform.position += Vector3.up * (_moveSpeed * Time.deltaTime);

        // N초가 지나면 풀로 돌려보낸다 (풀에서 만든 게 아니면 삭제)
        if (_elapsedTime >= _duration)
        {
            PooledObject.ReleaseOrDestroy(gameObject);
        }
    }
}