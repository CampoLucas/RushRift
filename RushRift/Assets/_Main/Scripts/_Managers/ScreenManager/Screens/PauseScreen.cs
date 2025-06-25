using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game;

public class PauseScreen : MonoBehaviour, IScreen
{
    [SerializeField] private GameObject[] pauseObjects;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;


    private void Awake()
    {
        //resumeButton.onClick.AddListener(() => LevelManager.Instance.ScreenManager.PopScreen());
        restartButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        quitButton.onClick.AddListener(() => Application.Quit());
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Escape)) LevelManager.Instance.ScreenManager.PopScreen();
    }

    

    public void Activate()
    {
        gameObject.SetActive(true);
        for (int i = 0; i < pauseObjects.Length; i++)
        {
            pauseObjects[i].SetActive(true);
        }

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public void Deactivate()
    {
        for (int i = 0; i < pauseObjects.Length; i++)
        {
            pauseObjects[i].SetActive(false);
        }
        gameObject.SetActive(false);
    }

    public void Free()
    {
        for (int i = 0; i < pauseObjects.Length; i++)
        {
            pauseObjects[i].SetActive(false);
        }
        gameObject.SetActive(false);
    }
}
