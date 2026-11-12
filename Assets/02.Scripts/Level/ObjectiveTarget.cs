using UnityEngine;

// 단계 목표가 되는 오브젝트(폭발물 픽업, 막힌 길 장애물)의 공통 기반.
// 완료되면 오브젝트가 꺼져도 IsDone은 유지되므로 LevelManager가 계속 확인할 수 있다.
public abstract class ObjectiveTarget : MonoBehaviour
{
    public bool IsDone { get; private set; }

    protected void MarkDone()
    {
        IsDone = true;
    }
}
