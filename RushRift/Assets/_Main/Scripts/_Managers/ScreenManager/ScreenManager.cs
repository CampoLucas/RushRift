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

    public Dictionary<ScreenName, Transform> screenTransformsDictionary = new Dictionary<ScreenName, Transform>();
    public static ISubject disableBehaviour = new Subject();
    public static ISubject enableBehaviour = new Subject();

    [SerializeField] private ScreenName initialScreen;
    [SerializeField] private ScreenStruct[] screens;

    private Stack<IScreen> _screenStack = new Stack<IScreen>();
    private Dictionary<ScreenName, IScreen> _screenDictionary = new Dictionary<ScreenName, IScreen>();

    private void Awake()
    {
        for (int i = 0; i < screens.Length; i++)
        {
            screenTransformsDictionary[screens[i].screenName] = screens[i].screenObject;
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
}
