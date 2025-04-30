using System.Collections.Generic;
using UnityEngine;
using Game;

public class GameplayScreen : MonoBehaviour, IScreen
{
    private Dictionary<Behaviour, bool> _behaviourDictionary = new Dictionary<Behaviour, bool>();


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) LevelManager.Instance.ScreenManager.PushScreen(ScreenName.Pause);
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        foreach (var behaviour in _behaviourDictionary) behaviour.Key.enabled = behaviour.Value;
    }

    public void Deactivate()
    {
        foreach (var behaviour in GetComponentsInChildren<Behaviour>())
        {
            if (behaviour.GetComponent<Camera>() != null)
            {
                _behaviourDictionary.Remove(behaviour);
                continue;
            }
            _behaviourDictionary[behaviour] = behaviour.enabled;
            behaviour.enabled = false;
        }

    }

    public void Free()
    {
        gameObject.SetActive(false);
    }
}
