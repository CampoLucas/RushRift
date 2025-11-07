using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using Game.DesignPatterns.Observers;
using Game.UI;
using Game.Utils;


public class DisableBehaviour : MonoBehaviour
{
    [SerializeField] private Behaviour[] behavioursToDisable;

    private NullCheck<ActionObserver<bool>> _onPause;

    private void Start()
    {
        //_onPause = new ActionObserver<bool>(OnPauseHandler);
        
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

    private void OnPauseHandler(bool pause)
    {
        if (pause)
        {
            Pause();
        }
        else
        {
            Unpause();
        }
    }

    private void Pause()
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

    private void Unpause()
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
        if (!_onPause)
        {
            _onPause = new ActionObserver<bool>(OnPauseHandler);
        }
        
        PauseHandler.Attach(_onPause.Get());
        
        OnPauseHandler(PauseHandler.IsPaused);
    }

    private void OnDisable()
    {
        PauseHandler.Detach(_onPause.Get());
    }

    private void OnDestroy()
    {
        _onPause.Get()?.Dispose();
        _onPause = null;
    }
}
