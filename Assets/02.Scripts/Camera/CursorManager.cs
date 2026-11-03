using UnityEngine;
public class CursorManager : MonoBehaviour
{
    private bool _altPeekEnabled;
    private void Awake()
    {
        LockCursor();
    }
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void SetAltPeekEnabled(bool enabled)
    {
        _altPeekEnabled = enabled;
    }
    private void Update()
    {
        if (!_altPeekEnabled)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            UnlockCursor();
        }
        else if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            LockCursor();
        }
    }
}
