using System.Collections.Generic;
using UnityEngine;
    public class ToggleSwitchGroupManager : MonoBehaviour
    {
        [Header("Start Value")]
        [SerializeField] private ToggleSwitch initialToggleSwitch;

        [Header("Toggle Options")]
        [SerializeField] private bool allCanBeToggledOff; // "true"

        private List<ToggleSwitch> _toggleSwitches = new List<ToggleSwitch>();

        private void Awake()
        {
            ToggleSwitch[] toggleSwitches = GetComponentsInChildren<ToggleSwitch>();
            foreach (var toggleSwitch in toggleSwitches)
            {
                RegisterToggleButtonToGroup(toggleSwitch);
            }
        }

        private void RegisterToggleButtonToGroup(ToggleSwitch toggleSwitch)
        {
            if (_toggleSwitches.Contains(toggleSwitch))
                return;

            _toggleSwitches.Add(toggleSwitch);
            toggleSwitch.SetupForManager(this);
        }

    private void Start()
    {
        bool areAllToggledOff = true;
        foreach (var button/*:ToggleSwitch*/ in _toggleSwitches)
        {
            if (!button.CurrentValue)
                continue;

            areAllToggledOff = false;
            break;
        }

        if (!areAllToggledOff || allCanBeToggledOff)
            return;

        if (initialToggleSwitch != null)
            initialToggleSwitch.ToggleByGroupManager(/*valueToSetTo:*/ true);
        else
            _toggleSwitches[0].ToggleByGroupManager(/*valueToSetTo:*/ true);
    }

    public void ToggleGroup(ToggleSwitch toggleSwitch)
    {
        if (_toggleSwitches.Count <= 1)
            return;

        if (allCanBeToggledOff && toggleSwitch.CurrentValue)
        {
            foreach (var button/*:ToggleSwitch*/ in _toggleSwitches)
            {
                if (button == null)
                    continue;

                button.ToggleByGroupManager(/*valueToSetTo:*/ false);
            }
        }
        else
        {
            foreach (var button/*:ToggleSwitch*/ in _toggleSwitches)
            {
                if (button == null)
                    continue;

                if (button == toggleSwitch)
                    button.ToggleByGroupManager(/*valueToSetTo:*/ true);
                else
                    button.ToggleByGroupManager(/*valueToSetTo:*/ false);
            }
        }
    }

}