using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 이동은 모두 이 클래스를 거친다. 가벼운 LoadingScene을 먼저 열어 이전 씬을 메모리에서 내린 뒤,
// LoadingUI가 목표 씬을 비동기로 불러온다.
public static class SceneLoader
{
    public const string LoadingSceneName = "LoadingScene";
    public const string DefaultMessage = "구역 진입 중...";
    public const float DefaultMinLoadingTime = 1f;

    // LoadingUI가 읽어 가는 값
    public static string TargetScene { get; private set; }
    public static string Message { get; private set; } = DefaultMessage;
    public static float MinLoadingTime { get; private set; } = DefaultMinLoadingTime;

    // 도메인 리로드를 끈 상태에서도 이전 플레이의 값이 남지 않게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        TargetScene = null;
        Message = DefaultMessage;
        MinLoadingTime = DefaultMinLoadingTime;
    }

    // targetScene: 빌드 설정에 들어 있는 씬 이름. minLoadingTime: 로딩 화면을 최소한 보여 줄 시간(초).
    public static void Load(string targetScene, string message = DefaultMessage, float minLoadingTime = DefaultMinLoadingTime)
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            GameLogger.LogError("SceneLoader: 이동할 씬 이름이 비어 있습니다.");
            return;
        }

        Time.timeScale = 1f;
        TargetScene = targetScene;
        Message = message;
        MinLoadingTime = Mathf.Max(0f, minLoadingTime);

        // 로딩 씬이 빌드에 없으면 로딩 화면 없이 바로 이동한다.
        if (!Application.CanStreamedLevelBeLoaded(LoadingSceneName))
        {
            GameLogger.LogWarning("SceneLoader: " + LoadingSceneName + "이 빌드 설정에 없어 바로 이동합니다.");
            SceneManager.LoadScene(targetScene);
            return;
        }
        SceneManager.LoadScene(LoadingSceneName);
    }
}
