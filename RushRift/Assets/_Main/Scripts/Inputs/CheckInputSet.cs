using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CheckInputSet : MonoBehaviour
{
    [SerializeField] private CheckInputChange checkInput;
    public void Init()
    {
        checkInput.gameObject.SetActive(true);
        checkInput.OnEnteredSubMenu();
    }

    public void End()
    {
        checkInput.OnExitedSubMenu();
        checkInput.gameObject.SetActive(false);
    }

    public void SetActiveButton(Selectable selectable)
    {
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}
