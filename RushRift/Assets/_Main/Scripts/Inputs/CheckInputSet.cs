using Game.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckInputSet : MonoBehaviour
{
    [SerializeField] private CheckInputChange checkInput;
    public void Init()
    {
        CursorHandler.lockState = CursorLockMode.None;
        CursorHandler.visible = true;
        checkInput.gameObject.SetActive(true);
        checkInput.OnEnteredSubMenu();
    }

    public void End()
    {
        checkInput.OnExitedSubMenu();
        checkInput.gameObject.SetActive(false);
    }
}
