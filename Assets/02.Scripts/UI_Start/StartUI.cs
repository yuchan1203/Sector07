using UnityEngine;
using UnityEngine.SceneManagement;

// 시작 화면의 로그인/회원가입 UI. 실제 인증은 하지 않고 버튼을 누르면 바로 게임 씬으로 넘어간다.
public class StartUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _signUpPanel;

    [Header("씬")]
    [SerializeField] private string _gameSceneName = "GameScene";

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowLogin();
    }

    // 회원가입 버튼 OnClick (로그인 화면 -> 회원가입 화면)
    public void ShowSignUp()
    {
        _loginPanel.SetActive(false);
        _signUpPanel.SetActive(true);
    }

    // 돌아가기 버튼 OnClick (회원가입 화면 -> 로그인 화면)
    public void ShowLogin()
    {
        _signUpPanel.SetActive(false);
        _loginPanel.SetActive(true);
    }

    // 로그인/가입하기 버튼 OnClick. 지금은 입력값과 상관없이 바로 게임을 시작한다.
    public void StartGame()
    {
        SceneManager.LoadScene(_gameSceneName);
    }
}
