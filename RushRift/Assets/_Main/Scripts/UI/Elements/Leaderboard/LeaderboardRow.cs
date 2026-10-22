using TMPro;
using UnityEngine;

public class LeaderboardRow : MonoBehaviour
{
    [SerializeField] private TMP_Text posText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private GameObject highlight; // Imagen de fondo que se activa para el jugador

    public RectTransform Rect => (RectTransform)transform;

    public void Set(int position, string userName, string formattedTime, bool isPlayer)
    {
        posText.text = position.ToString();
        nameText.text = userName;
        timeText.text = formattedTime;

        if (highlight != null)
            highlight.SetActive(isPlayer);
    }
}