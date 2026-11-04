using TMPro;
using UnityEngine;

// 클리어 결과 화면. 처음에는 꺼 두고 LevelManager가 Show로 띄운다.
public class ClearResultUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _timeText;
    [SerializeField] private TMP_Text _killText;

    public void Show(float seconds, int kills)
    {
        int minutes = (int)(seconds / 60f);
        int rest = (int)(seconds % 60f);
        if (_timeText != null)
        {
            _timeText.text = "클리어 시간  " + minutes.ToString("00") + ":" + rest.ToString("00");
        }
        if (_killText != null)
        {
            _killText.text = "처치한 좀비  " + kills;
        }
        gameObject.SetActive(true);
    }
}
