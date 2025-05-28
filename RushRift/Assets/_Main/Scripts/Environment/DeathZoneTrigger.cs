using Game;
using UnityEngine;

public class DeathZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (LevelManager.TryGetGameOver(out var gameOverSubject))
        {
            gameOverSubject.NotifyAll(); // Triggers OnPlayerDeath via observer
        }
    }
}

