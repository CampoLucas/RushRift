using Game;
using UnityEngine;

public class DeathZoneTrigger : MonoBehaviour
{
    [SerializeField] private bool reset;
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (reset)
        {
            GlobalLevelManager.Restart();
        }
        else
        {
            GlobalEvents.GameOver.NotifyAll(false);
        }
    }
}

