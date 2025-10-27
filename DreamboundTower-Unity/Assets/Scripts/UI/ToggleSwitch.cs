using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
{
    [Header("Slider setup")]
    [SerializeField, Range(0, 1f)] private float sliderValue;

    public bool CurrentValue { get; private set; }

    private Slider _slider;

    [Header("Animation")]
    [SerializeField, Range(0, 1f)] private float animationDuration = 0.5f;
    [SerializeField] private AnimationCurve slideEase = AnimationCurve.EaseInOut(0,0,1,1);

    private Coroutine _animationSliderCoroutine;

    [Header("Events")]
    [SerializeField] private UnityEvent onToggleOn;
    [SerializeField] private UnityEvent onToggleOff;

    private ToggleSwitchGroupManager _toggleSwitchGroupManager;

    protected void OnValidate()
    {
        SetupToggleComponents();
        _slider.value = sliderValue;
    }
    
    private void SetupToggleComponents()
    {
        if (_slider != null) return;
        SetupSliderComponent();
    }

    private void SetupSliderComponent()
    {
        _slider = GetComponent<Slider>();

        if (_slider == null)
        {
            Debug.Log("No slider found!", this);
            return;
        }

        _slider.interactable = false;
        ColorBlock sliderColors = _slider.colors;
        sliderColors.disabledColor = Color.white;
        _slider.colors = sliderColors;

        _slider.transition = Selectable.Transition.None;
    }

    public void SetupForManager(ToggleSwitchGroupManager manager)
    {
        _toggleSwitchGroupManager = manager;
    }

    private void Awake()
    {
        SetupToggleComponents();
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        Toggle();
    }

    private void Toggle()
    {
        if (_toggleSwitchGroupManager != null)
        {
            _toggleSwitchGroupManager.ToggleGroup(this);
        }
        else
        {
            SetStateAndStartAnimation(!CurrentValue);
        }
    }

    public void ToggleByGroupManager(bool valueToSetTo)
    {
        SetStateAndStartAnimation(valueToSetTo);
    }

    private bool previousValue;
    private void SetStateAndStartAnimation(bool state)
    {
        previousValue = CurrentValue;
        CurrentValue = state;

        if (previousValue != CurrentValue)
        {
            if (CurrentValue)
                onToggleOn?.Invoke();
            else
                onToggleOff?.Invoke();
        }

        if(_animationSliderCoroutine != null)
            StopCoroutine(_animationSliderCoroutine);

        _animationSliderCoroutine = StartCoroutine(AnimateSliderAndLabels());
    }

    private IEnumerator AnimateSliderAndLabels()
    {
        float startValue = _slider.value;
        float endValue = CurrentValue ? 1 : 0;
        float time = 0;

        // Alpha đích (không đổi)
        float targetAlphaOn = CurrentValue ? 1f : 0f;
        float targetAlphaOff = !CurrentValue ? 1f : 0f;


        if (animationDuration > 0)
        {
            while (time < animationDuration)
            {
                time += Time.deltaTime;
                float rawLerpFactor = Mathf.Clamp01(time / animationDuration); // Clamp01 để an toàn
                float easedLerpFactor = slideEase.Evaluate(rawLerpFactor);

                // Animate Slider
                _slider.value = sliderValue = Mathf.Lerp(a: startValue, b: endValue, t: easedLerpFactor);

                yield return null;
            }
        }
    }

}
