using UnityEngine;
using Game;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class WinLevelScreen : MonoBehaviour, IScreen
{
    [SerializeField] private GameObject[] winLevelObjects;
    [SerializeField] private Button continueButton;
    [SerializeField] private string sceneToLoad;


    private void Awake()
    {
        continueButton.onClick.AddListener(() => LoadNextLevel());
    }

    private void LoadNextLevel()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        for (int i = 0; i < winLevelObjects.Length; i++)
        {
            winLevelObjects[i].SetActive(true);
        }

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public void Deactivate()
    {
        for (int i = 0; i < winLevelObjects.Length; i++)
        {
            winLevelObjects[i].SetActive(false);
        }
        gameObject.SetActive(false);
    }

    public void Free()
    {
        for (int i = 0; i < winLevelObjects.Length; i++)
        {
            winLevelObjects[i].SetActive(false);
        }
        gameObject.SetActive(false);
    }
}
