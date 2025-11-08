using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using System;

public static class Database
{
    private static string serverIp = "[2802:8010:8b2a:901::555]";
    //private void Awake()
    //{
    //    SendScore(1,1,"00:30:00", 0, 0, 0);
    //}

    //public static void SendScore(int user, int level, string time, int a1, int a2, int a3)
    //{
    //    StartCoroutine(SendScoreCoroutine(user, level, time, a1, a2, a3));
    //}

    

    public static IEnumerator SendUsernameCoroutine(string user, Action<int> callback)
    {
        WWWForm form = new WWWForm();
        form.AddField("name", user);

        using (UnityWebRequest www = UnityWebRequest.Post($"http://{serverIp}/api/save_username.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                ResponseData response = JsonUtility.FromJson<ResponseData>(json);

                if (response.status == "success")
                {
                    Debug.Log("Username enviado correctamente. ID: " + response.id + response.name);
                    callback?.Invoke(response.id);
                }
                else
                {
                    Debug.LogError("Error al guardar: " + response.message);
                }
            }
            else
            {
                Debug.LogError("Error al enviar el username: " + www.error);
            }
        }
    }

    public static IEnumerator SendScoreCoroutine(int user, int level, string time, int a1, int a2, int a3)
    {
        WWWForm form = new WWWForm();
        form.AddField("user", user);
        form.AddField("level", level);
        form.AddField("time", time);
        form.AddField("a1", a1);
        form.AddField("a2", a2);
        form.AddField("a3", a3);

        UnityWebRequest www = UnityWebRequest.Post($"http://{serverIp}/api/save_score.php", form);
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


    public static IEnumerator GetScoreCoroutine(int level, Action<ScoreList> callback)
    {
        UnityWebRequest www = UnityWebRequest.Get($"http://{serverIp}/api/get_scores.php?level= {level}");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string json = www.downloadHandler.text;
            Debug.Log("Datos recibidos: " + json);


            string wrappedJson = "{\"scores\":" + json + "}";
            ScoreList scoreList = JsonUtility.FromJson<ScoreList>(wrappedJson);

            callback?.Invoke(scoreList);
        }
    }
}



