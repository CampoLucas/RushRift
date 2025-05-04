using UnityEngine;
using Game.DesignPatterns.Observers;


public class DisableBehaviour : MonoBehaviour
{
    [SerializeField] private Behaviour[] behavioursToDisable;

    private IObserver _onDisableCall;
    private IObserver _onEnableCall;

    private void Start()
    {
        _onDisableCall = new ActionObserver(OnDisableCall);
        _onEnableCall = new ActionObserver(OnEnableCall);

        ScreenManager.disableBehaviour.Attach(_onDisableCall);
        ScreenManager.enableBehaviour.Attach(_onEnableCall);
    }

    private void OnDisableCall()
    {
        for (int i = 0; i < behavioursToDisable.Length; i++)
        {
            behavioursToDisable[i].enabled = false;
        }
    }

    private void OnEnableCall()
    {
        for (int i = 0; i < behavioursToDisable.Length; i++)
        {
            behavioursToDisable[i].enabled = true;
        }
    }

    private void OnEnable()
    {
        _onDisableCall = new ActionObserver(OnDisableCall);
        _onEnableCall = new ActionObserver(OnEnableCall);
    }

    private void OnDisable()
    {
        _onDisableCall.Dispose();
        _onEnableCall.Dispose();
    }

}
