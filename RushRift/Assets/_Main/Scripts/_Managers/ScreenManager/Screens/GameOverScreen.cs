using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour, IScreen
{
    [SerializeField] private GameObject[] gameOverObjects;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene("Level_Hub");
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        for (int i = 0; i < gameOverObjects.Length; i++)
        {
            gameOverObjects[i].SetActive(true);
        }
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public void Deactivate()
    {
        for (int i = 0; i < gameOverObjects.Length; i++)
        {
            gameOverObjects[i].SetActive(false);
        }
        gameObject.SetActive(false);
    }

    public void Free()
    {
        for (int i = 0; i < gameOverObjects.Length; i++)
        {
            gameOverObjects[i].SetActive(false);
        }
        gameObject.SetActive(false);
    }
}
