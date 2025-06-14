using Game.DesignPatterns.Observers;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ScreenStruct
{
    public ScreenName screenName;
    public Transform screenObject; 
}

public class ScreenManager : MonoBehaviour
{
    public static ISubject OnPaused = new Subject();
    public static ISubject OnDispaused = new Subject();
    

    [SerializeField] private ScreenName initialScreen;
    [SerializeField] private ScreenStruct[] screens;

    private Dictionary<ScreenName, Transform> _transformsDictionary = new();
    private Dictionary<ScreenName, IScreen> _screenDictionary = new();
    private Stack<IScreen> _screenStack = new();

    private void Awake()
    {
        for (var i = 0; i < screens.Length; i++)
        {
            _transformsDictionary[screens[i].screenName] = screens[i].screenObject;
            _screenDictionary[screens[i].screenName] = screens[i].screenObject.GetComponent<IScreen>();
            //screens[i].screenObject.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        PushScreen(initialScreen);
    }

    public void PushScreen(ScreenName screen)
    {
        if (_screenStack.Count > 0) _screenStack.Peek().Deactivate();
        _screenStack.Push(_screenDictionary[screen]);
        _screenDictionary[screen].Activate();
    }

    public void PopScreen()
    {
        if (_screenStack.Count <= 1) return;
        _screenStack.Pop().Free();
        _screenStack.Peek().Activate();
    }

    public Transform GetScreenTransform(ScreenName screenName)
    {
        return _transformsDictionary[ScreenName.Gameplay];
    }

    private void OnDestroy()
    {
        OnPaused.DetachAll();
        OnDispaused.DetachAll();
        
        
        screens = null;
        _transformsDictionary.Clear();
        _transformsDictionary = null;
        
        _screenDictionary.Clear();
        _screenDictionary = null;
        
        _screenStack.Clear();
        _screenStack = null;

    }
}
