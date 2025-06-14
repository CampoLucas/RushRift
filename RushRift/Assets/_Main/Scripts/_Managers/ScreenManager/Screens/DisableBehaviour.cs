using UnityEngine;
using Game.DesignPatterns.Observers;


public class DisableBehaviour : MonoBehaviour
{
    [SerializeField] private Behaviour[] behavioursToDisable;

    private IObserver _onDisableCall;
    private IObserver _onEnableCall;

    private void Start()
    {
        _onDisableCall = new ActionObserver(OnDisableHandler);
        _onEnableCall = new ActionObserver(OnEnableHandler);

        ScreenManager.OnPaused.Attach(_onDisableCall);
        ScreenManager.OnDispaused.Attach(_onEnableCall);
    }

    public void SetBehaviour(Behaviour[] behaviours)
    {
        behavioursToDisable = behaviours;
    }

    private void OnDisableHandler()
    {
        for (int i = 0; i < behavioursToDisable.Length; i++)
        {
            var b = behavioursToDisable[i];
            if (b == null) continue;
            b.enabled = false;
        }
    }

    private void OnEnableHandler()
    {
        for (int i = 0; i < behavioursToDisable.Length; i++)
        {
            behavioursToDisable[i].enabled = true;
        }
    }

    private void OnEnable()
    {
        _onDisableCall = new ActionObserver(OnDisableHandler);
        _onEnableCall = new ActionObserver(OnEnableHandler);
    }

    private void OnDisable()
    {
        _onDisableCall.Dispose();
        _onEnableCall.Dispose();
    }

}
