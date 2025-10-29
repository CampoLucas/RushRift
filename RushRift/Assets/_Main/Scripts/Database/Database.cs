using System.Collections;
using UnityEngine.Networking;
using UnityEngine;

public  class Database : MonoBehaviour
{
    private void Awake()
    {
        SendScore(2,2,"00:50:00", 0, 0, 0);
    }

    public void SendScore(int user, int level, string time, int a1, int a2, int a3)
    {
        StartCoroutine(SendScoreCoroutine(user, level, time, a1, a2, a3));
    }

    private IEnumerator SendScoreCoroutine(int user, int level, string time, int a1, int a2, int a3)
    {
        WWWForm form = new WWWForm();
        form.AddField("user", user);
        form.AddField("level", level);
        form.AddField("time", time);
        form.AddField("a1", a1);
        form.AddField("a2", a2);
        form.AddField("a3", a3);

        UnityWebRequest www = UnityWebRequest.Post("http://localhost/api/save_score.php", form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log(" Score enviado: " + www.downloadHandler.text);
        }
        else
        {
            Debug.LogError(" Error al enviar score: " + www.error);
        }
    }
}

