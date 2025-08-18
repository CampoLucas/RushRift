using System.Collections.Generic;
using UnityEngine;
using Game.DesignPatterns.Observers;
using Game.UI;
using Game.Utils;


public class DisableBehaviour : MonoBehaviour
{
    [SerializeField] private Behaviour[] behavioursToDisable;

    private IObserver _onDisableCall;
    private IObserver _onEnableCall;

    private void Start()
    {
        _onDisableCall = new ActionObserver(OnDisableHandler);
        _onEnableCall = new ActionObserver(OnEnableHandler);

        UIManager.OnPaused.Attach(_onDisableCall);
        UIManager.OnUnPaused.Attach(_onEnableCall);
    }

    public bool TrySetBehaviour(Behaviour[] behaviours)
    {
        var behavioursToAdd = new List<Behaviour>();
        
        for (var i = 0; i < behaviours.Length; i++)
        {
            var behaviour = behaviours[i];
            if (behaviour == null || behaviour == this) continue;
            
            behavioursToAdd.Add(behaviour);
        }

        var behavioursAdded = behavioursToAdd.Count;
        behavioursToDisable = behavioursToAdd.ToArray();
        
        behavioursToAdd.Clear();
        return behavioursAdded > 0;
    }

    private void OnDisableHandler()
    {
        for (int i = 0; i < behavioursToDisable.Length; i++)
        {
            var behaviour = behavioursToDisable[i];
            if (behaviour == null)
            {
#if UNITY_EDITOR
                var o = gameObject;
                Debug.LogError($"ERROR: Trying to disable a null behaviour in {o.name}", o);
#endif
                continue;
            }

            if (behaviour.IsNullOrMissingReference())
            {
#if UNITY_EDITOR
                var o = gameObject;
                Debug.LogError($"ERROR: Trying to disable a missing behaviour in {o.name}", o);
#endif
                continue;
            }
            
            behaviour.enabled = false;
        }
    }

    private void OnEnableHandler()
    {
        for (int i = 0; i < behavioursToDisable.Length; i++)
        {
            var behaviour = behavioursToDisable[i];
            if (behaviour == null)
            {
#if UNITY_EDITOR
                var o = gameObject;
                Debug.LogError($"ERROR: Trying to enable a null behaviour in {o.name}", o);
#endif
                continue;
            }

            if (behaviour.IsNullOrMissingReference())
            {
#if UNITY_EDITOR
                var o = gameObject;
                Debug.LogError($"ERROR: Trying to enable a missing behaviour in {o.name}", o);
#endif
                continue;
            }
            
            behaviour.enabled = true;
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
