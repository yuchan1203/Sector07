using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : SingletonBehaviour<GameManager>
{
    private GameState _gameState = GameState.Ready;
    public GameState GameState => _gameState;

    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private Animator _playerAnimator;
    [Header("일시정지 UI")]
    [SerializeField] private GameObject _stopUI;
    [Header("게임오버 UI")]
    [SerializeField] private GameObject _gameOverUI;
    [Header("커서")]
    [SerializeField] private CursorManager _cursorManager;

    protected override void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
        }
        base.OnDestroy();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
        else if (Input.GetKeyDown(KeyCode.R) && (_gameState == GameState.GameOver || _gameState == GameState.Clear))
        {
            Restart();
        }
    }

    private void TogglePause()
    {
        if (_stateText == null)
        {
            return;
        }
        if (_gameState == GameState.Playing)
        {
            _gameState = GameState.Pause;
            Time.timeScale = 0f;
            _stateText.gameObject.SetActive(true);
            _stateText.color = Color.white;
            _stateText.text = "일시정지";
            SetStopUIActive(true);
            if (_cursorManager != null)
            {
                _cursorManager.SetAltPeekEnabled(false);
                _cursorManager.UnlockCursor();
            }
        }
        else if (_gameState == GameState.Pause)
        {
            Resume();
        }
    }

    private void SetStopUIActive(bool active)
    {
        if (_stopUI != null)
        {
            _stopUI.SetActive(active);
        }
    }

    // 계속하기 버튼 OnClick
    public void Resume()
    {
        if (_gameState != GameState.Pause)
        {
            return;
        }
        _gameState = GameState.Playing;
        Time.timeScale = 1f;
        _stateText.text = "";
        _stateText.gameObject.SetActive(false);
        SetStopUIActive(false);
        if (_cursorManager != null)
        {
            _cursorManager.LockCursor();
            _cursorManager.SetAltPeekEnabled(true);
        }
    }

    // 다시 시작 버튼 OnClick
    public void Restart()
    {
        // 로딩 화면은 짧게만 보여 준다.
        SceneLoader.Load(SceneManager.GetActiveScene().name, "구역 재진입 중...", 0.6f);
    }

    // 게임 종료 버튼 OnClick
    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void SetGameOverUIActive(bool active)
    {
        if (_gameOverUI != null)
        {
            _gameOverUI.SetActive(active);
        }
    }

    private void Start()
    {
        SetStopUIActive(false);
        SetGameOverUIActive(false);
        StartCoroutine(StartSequence_Coroutine());
    }
    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.color = Color.white;
        _stateText.text = "3";
        yield return new WaitForSeconds(0.5f);
        _stateText.text = "2";
        yield return new WaitForSeconds(0.5f);
        _stateText.text = "1";
        yield return new WaitForSeconds(0.5f);
        _stateText.text = "시작!";
        yield return new WaitForSeconds(0.5f);
        _stateText.text = "";
        _stateText.gameObject.SetActive(false);
        _gameState = GameState.Playing;
        if (_cursorManager != null)
        {
            _cursorManager.SetAltPeekEnabled(true);
        }
    }

    // 모든 목표를 마쳤을 때 LevelManager가 부른다. 결과 화면은 LevelManager가 띄운다.
    public void GameClear()
    {
        if (_gameState != GameState.Playing)
        {
            return;
        }
        _gameState = GameState.Clear;
        if (_cursorManager != null)
        {
            _cursorManager.SetAltPeekEnabled(false);
            _cursorManager.UnlockCursor();
        }
        if (_playerAnimator != null)
        {
            _playerAnimator.SetFloat(AnimatorParams.MoveMotion, 0f);
        }
    }

    public void GameOver()
    {
        if (_cursorManager != null)
        {
            _cursorManager.SetAltPeekEnabled(false);
            _cursorManager.UnlockCursor();
        }
        _stateText.gameObject.SetActive(true);
        _gameState = GameState.GameOver;
        _stateText.text = "게임오버";
        _stateText.color = Color.red;
        SetGameOverUIActive(true);
        // 쓰러지는 소리가 먼저 들리도록 잠시 뒤에 낸다.
        SoundManager.Play(SfxId.GameOver, 0.8f);
        if (_playerAnimator != null)
        {
            _playerAnimator.SetFloat(AnimatorParams.MoveMotion, 0f);
        }
    }
}