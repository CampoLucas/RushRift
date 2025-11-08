using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.DataBase;

public class Database : IDataBase
{
    private string serverIp = "[2802:8010:8b2a:901::555]";
    //private void Awake()
    //{
    //    SendScore(1,1,"00:30:00", 0, 0, 0);
    //}

    //public static void SendScore(int user, int level, string time, int a1, int a2, int a3)
    //{
    //    StartCoroutine(SendScoreCoroutine(user, level, time, a1, a2, a3));
    //}

    

    public IEnumerator SendUsernameCoroutine(string user, Action<int> callback)
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

    public IEnumerator SendScoreCoroutine(int user, int level, string time, int a1, int a2, int a3)
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

    public async UniTask<DBRequestState> SendUsername(string user, Action<int> successCallback, CancellationToken token)
    {
        var form = new WWWForm();
        form.AddField("name", user);

        using (UnityWebRequest www = UnityWebRequest.Post($"http://{serverIp}/api/save_username.php", form))
        {
            await www.SendWebRequest().WithCancellation(token);

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                ResponseData response = JsonUtility.FromJson<ResponseData>(json);

                if (response.status == "success")
                {
                    Debug.Log("Username enviado correctamente. ID: " + response.id + response.name);
                    successCallback?.Invoke(response.id);
                    return DBRequestState.Success;
                }
                
                return ErrorResult(DBRequestState.SavingError, "Error al guardar: " + response.message);
            }
            
            return ErrorResult(DBRequestState.SendingError, "Error al enviar el username: " + www.error);
        }
    }

    public async UniTask<DBRequestState> SendScore(int user, int level, string time, int a1, int a2, int a3, CancellationToken token)
    {
        WWWForm form = new WWWForm();
        form.AddField("user", user);
        form.AddField("level", level);
        form.AddField("time", time);
        form.AddField("a1", a1);
        form.AddField("a2", a2);
        form.AddField("a3", a3);

        UnityWebRequest www = UnityWebRequest.Post($"http://{serverIp}/api/save_score.php", form);

        await www.SendWebRequest().WithCancellation(token);

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log(" Score enviado: " + www.downloadHandler.text);
            return DBRequestState.Success;
        }
        return ErrorResult(DBRequestState.SendingError, "Error al enviar score: " + www.error);
    }

    private DBRequestState ErrorResult(DBRequestState state, string message)
    {
        // open pop up
        Debug.Log(message);
        return state;
    }
}



