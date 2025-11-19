using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.UI;

public class CheckInputChange : MonoBehaviour
{
    [SerializeField] private GameObject _lastSelectableUsed;

    private IDisposable _m_EventListener;
    private InputDevice _lastDeviceUsed;
    private bool _onSubMenu;


    private void OnEnable()
    {
        _m_EventListener = InputSystem.onAnyButtonPress.Call(OnButtonPressed);
    }

    private void OnDisable()
    {
        _m_EventListener.Dispose();

    }


    public void OnEnteredSubMenu()
    {
        _onSubMenu = true;
    }

    public void OnExitedSubMenu()
    {
        _onSubMenu = false;
        CursorHandler.lockState = CursorLockMode.Locked;
        CursorHandler.visible = false;
    }

    private void OnButtonPressed(InputControl button)
    {
        var device = button.device;

        if (device == _lastDeviceUsed) return;

        _lastDeviceUsed = device;

        // Ignore presses on devices that are already used by a player.
        if (PlayerInput.FindFirstPairedToDevice(device) != null)
            return;

        if (device is Gamepad)
        {
            CursorHandler.lockState = CursorLockMode.Locked;
            CursorHandler.visible = false;
            EventSystem.current.SetSelectedGameObject(_lastSelectableUsed.gameObject);
            Debug.Log("Gamepad");   
        }
        if (device is Mouse)
        {
            CursorHandler.lockState = CursorLockMode.None;
            CursorHandler.visible = true;
            //if (_onSubMenu)
            //{
            //}
            _lastSelectableUsed = EventSystem.current.currentSelectedGameObject;
            Debug.Log("mouse");
        }
        if (device is Keyboard)
        {
            CursorHandler.lockState = CursorLockMode.None;
            CursorHandler.visible = true;
            //if (_onSubMenu)
            //{  
            //}
            EventSystem.current.SetSelectedGameObject(_lastSelectableUsed.gameObject);
            Debug.Log("keyboard");
        }


    }


}
