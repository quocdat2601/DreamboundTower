using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class IntroClick : MonoBehaviour
{
    [SerializeField]
    private string nextSceneName = "Main Menu";

    [SerializeField]
    private float minDelaySeconds = 0.5f;

    private float elapsedSeconds = 0f;

    private void Update()
    {
        elapsedSeconds += Time.deltaTime;
        if (elapsedSeconds < minDelaySeconds)
        {
            return;
        }

        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool anyKeyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool touchPressed = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        if (mouseClicked || anyKeyPressed || touchPressed)
        {
            SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }
    }
}


