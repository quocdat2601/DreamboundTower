using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject settingPanel; // Parent panel (transparent background)
    public GameObject settingMenuPrefab; // Setting menu prefab
    public Button settingsButton; // Button để mở settings
    
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private bool isSettingOpen = false;
    private GameObject currentSettingMenu;
    private Coroutine animationCoroutine;
    
    void Start()
    {
        // Ẩn setting panel ban đầu
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
        
        // Đăng ký event cho settings button
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettings);
        }
    }
    
    public void PlayGame()
    {
        SceneManager.LoadSceneAsync(2); //Scene 1
    }

    public void Settings()
    {
        if (isSettingOpen)
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }
    
    public void OpenSettings()
    {
        if (isSettingOpen || settingMenuPrefab == null || settingPanel == null) return;
        
        // Hiển thị setting panel (transparent background)
        settingPanel.SetActive(true);
        
        // Tạo setting menu từ prefab
        currentSettingMenu = Instantiate(settingMenuPrefab, settingPanel.transform);
        
        // Force set offset về 0 để fill full parent
        RectTransform settingRect = currentSettingMenu.GetComponent<RectTransform>();
        if (settingRect != null)
        {
            // Set offset về 0 (fill full parent)
            settingRect.offsetMin = Vector2.zero; // Left, Bottom = 0
            settingRect.offsetMax = Vector2.zero; // Right, Top = 0
        }
        
        // Tìm close button trong setting menu và đăng ký event
        Button closeBtn = currentSettingMenu.GetComponentInChildren<Button>();
        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(CloseSettings);
        }
        
        // Start animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateIn());
        
        isSettingOpen = true;
    }
    
    public void CloseSettings()
    {
        if (!isSettingOpen) return;
        
        // Start close animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateOut());
    }
    
    private IEnumerator AnimateIn()
    {
        if (currentSettingMenu == null) yield break;
        
        // Setup initial state
        currentSettingMenu.transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            float scale = scaleCurve.Evaluate(progress);
            
            currentSettingMenu.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        currentSettingMenu.transform.localScale = Vector3.one;
        animationCoroutine = null;
    }
    
    private IEnumerator AnimateOut()
    {
        if (currentSettingMenu == null) yield break;
        
        float elapsed = 0f;
        Vector3 startScale = currentSettingMenu.transform.localScale;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            float scale = scaleCurve.Evaluate(1f - progress);
            
            currentSettingMenu.transform.localScale = startScale * scale;
            yield return null;
        }
        
        // Destroy panel
        if (currentSettingMenu != null)
        {
            Destroy(currentSettingMenu);
            currentSettingMenu = null;
        }
        
        // Ẩn setting panel
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
        
        isSettingOpen = false;
        animationCoroutine = null;
    }
    
    public void Quit()
    {
        Application.Quit();
    }
}
