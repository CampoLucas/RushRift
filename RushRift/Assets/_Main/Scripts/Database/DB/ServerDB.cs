using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.DataBase;

public class ServerDB : IDataBase
{
    private string serverIp;

    public ServerDB(string ip)
    {
        serverIp = ip;
    }
    
    public bool Enabled()
    {
        return true;
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
            Debug.Log("Score sent: " + www.downloadHandler.text);
            return DBRequestState.Success;
        }
        return ErrorResult(DBRequestState.SendingError, "Error sending score: " + www.error);
    }
    
    public async UniTask<DBRequestState> GetScore(int level, Action<ScoreList> successCallback, CancellationToken token)
    {
        var www = UnityWebRequest.Get($"http://{serverIp}/api/get_scores.php?level= {level}");
        await www.SendWebRequest().WithCancellation(token);

        if (www.result != UnityWebRequest.Result.Success)
        {
            return ErrorResult(DBRequestState.SendingError, "Didn't recieve data");
        }

        var json = www.downloadHandler.text;
        Debug.Log($"Data recived: {json}");
        
        var wrappedJson = "{\"scores\":" + json + "}";
        var scoreList = JsonUtility.FromJson<ScoreList>(wrappedJson);

        successCallback?.Invoke(scoreList);
        return DBRequestState.Success;
    }

    private DBRequestState ErrorResult(DBRequestState state, string message)
    {
        // open pop up
        DataBaseHandler.ErrorFallback.NotifyAll(state, message);
        Debug.Log(message);
        return state;
    }
}



