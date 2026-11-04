using TMPro;
using UnityEngine;

// 화면 상단에 현재 목표와 진행 상황을 보여 준다.
public class ObjectiveHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text _stageText;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _progressText;

    private void Start()
    {
        if (_stageText == null || _titleText == null || _progressText == null)
        {
            GameLogger.LogError("ObjectiveHUD: 텍스트 참조가 비어 있습니다.");
            enabled = false;
            return;
        }
        if (LevelManager.Instance == null)
        {
            GameLogger.LogWarning("ObjectiveHUD: LevelManager가 없어 숨깁니다.");
            gameObject.SetActive(false);
            return;
        }
        LevelManager.Instance.ObjectiveChanged += Refresh;
        _stageText.text = "";
        _titleText.text = "";
        _progressText.text = "";
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ObjectiveChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        LevelManager level = LevelManager.Instance;
        _stageText.text = "STAGE " + level.StageNumber + " / " + level.StageCount;
        _titleText.text = level.ObjectiveTitle;
        _progressText.text = level.ProgressText;
    }
}
