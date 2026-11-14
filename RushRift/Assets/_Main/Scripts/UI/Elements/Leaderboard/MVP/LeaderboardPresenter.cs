using Game;
using Game.Saves;
using Game.UI.StateMachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class LeaderboardPresenter : UIPresenter<LeaderboardModel, LeaderboardView>
{
    [Header("Referencias")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private LeaderboardRow rowPrefab;
    [SerializeField] private TMP_Text currPlayerPos;
    [SerializeField] private TMP_Text currPlayerName;
    [SerializeField] private TMP_Text currPlayerTime;

    [Header("Configuración")]
    [SerializeField] private int rankLimit = 100;          // Máximo de puestos a cargar
    [SerializeField] private int scrollLimit = 10;        
    [SerializeField] private float waitAtTop = 1f;         // Segundos mostrando el Top 10 antes de scrollear
    [SerializeField] private float scrollDuration = 0.8f;  // Duración del scroll hacia tu puesto

    private readonly List<LeaderboardRow> rows = new List<LeaderboardRow>();
    private Coroutine scrollRoutine;

    // Datos del jugador para saber cuál es "su" fila
    private bool hasPlayerEntry;
    private bool isPlayer;
    private string playerId;
    private float playerTime;

    private void Awake()
    {
        //GetScoreData();
    }

    public void Init()
    {
        StopScroll();

        for (int i = 0; i < rows.Count; i++)
            rows[i].gameObject.SetActive(false);

        hasPlayerEntry = false;
    }

    public override void Begin()
    {
        base.Begin();
    }

    /// <summary>
    /// Llamar antes de GetScoreData() con el resultado que acaba de hacer el jugador.
    /// </summary>
    public void SetPlayerEntry(string name, float time)
    {
        playerId = name;
        playerTime = time;
        hasPlayerEntry = true;
    }

    public void GetScoreData()
    {
        var leaderBoardSave = SaveSystem.LoadLeaderBoard();
        var saveData = SaveSystem.LoadGame();
        var currentPlayer = saveData.GetPlayerName();
        var id = GlobalLevelManager.GetID();
        var endTime = GlobalLevelManager.CompleteTime;
        saveData.CheckBestTime(id, endTime, out var prevBest, out var currBest, out var newRecord);
        SetPlayerEntry(currentPlayer.id, currBest);
        currPlayerName.text = currentPlayer.name;
        currPlayerTime.text = FormatTime(currBest);
        OnrecievedScore(leaderBoardSave);
    }

    private void OnrecievedScore(LeaderBoardData leaderBoardData)
    {
        StopScroll();
        ResetLeaderboard();

        var levelLeaderBoard = leaderBoardData.GetLevelLeaderBoard(GlobalLevelManager.GetID());
        

        int count = Mathf.Min(levelLeaderBoard.Count, rankLimit);
        int playerIndex = -1;

        for (int i = 0; i < count; i++)
        {
            var row = GetRow(i);
            row.gameObject.SetActive(true);

            isPlayer = hasPlayerEntry
                            && levelLeaderBoard[i].id == playerId;
                            

            if (isPlayer) playerIndex = i;

            row.Set(i + 1, levelLeaderBoard[i].name, FormatTime(levelLeaderBoard[i].timescore), isPlayer);
        }

        // Empezamos arriba (Top 10 visible)
        scrollRect.verticalNormalizedPosition = 1f;

        currPlayerPos.text = (playerIndex + 1).ToString();

        // Si el jugador está fuera del Top 10, scrolleamos hasta su puesto
        if (playerIndex >= scrollLimit)
        {
            scrollRoutine = StartCoroutine(ScrollToRow(rows[playerIndex]));
        }

    }

    private LeaderboardRow GetRow(int index)
    {
        // Reutilizamos filas existentes y solo instanciamos si faltan
        while (rows.Count <= index)
            rows.Add(Instantiate(rowPrefab, content));

        return rows[index];
    }

    private void ResetLeaderboard()
    {
        for (int i = 0; i < rows.Count; i++)
            rows[i].gameObject.SetActive(false);
    }

    private IEnumerator ScrollToRow(LeaderboardRow targetRow)
    {
        // Esperamos un frame para que el layout se calcule bien
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        float scrollableHeight = content.rect.height - ((RectTransform)scrollRect.viewport ?? scrollRect.GetComponent<RectTransform>()).rect.height;
        if (scrollableHeight <= 0f) yield break; // Todo cabe en pantalla, no hay nada que scrollear

        float viewportHeight = ((RectTransform)scrollRect.viewport ?? scrollRect.GetComponent<RectTransform>()).rect.height;

        // Distancia desde el borde superior del content hasta el centro de la fila
        // (el Content debe tener el pivot arriba: Y = 1)
        Vector3 rowCenterLocal = content.InverseTransformPoint(targetRow.Rect.TransformPoint(targetRow.Rect.rect.center));
        float distanceFromTop = -rowCenterLocal.y;

        // Queremos la fila centrada en el viewport
        float desiredTop = Mathf.Clamp(distanceFromTop - viewportHeight * 0.5f, 0f, scrollableHeight);
        float targetNormalized = 1f - (desiredTop / scrollableHeight);

        // Pausa mostrando el Top 10
        float t = 0f;
        while (t < waitAtTop)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Scroll suave
        float startNormalized = scrollRect.verticalNormalizedPosition;
        t = 0f;
        while (t < scrollDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / scrollDuration);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startNormalized, targetNormalized, k);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = targetNormalized;
        scrollRoutine = null;
    }

    private void StopScroll()
    {
        if (scrollRoutine != null)
        {
            StopCoroutine(scrollRoutine);
            scrollRoutine = null;
        }
    }

    private string FormatTime(float time)
    {
        var minutes = Mathf.FloorToInt(time / 60f);
        var seconds = Mathf.FloorToInt(time % 60f);
        var milliseconds = Mathf.FloorToInt((time * 1000f) % 1000f);

        return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
    }

    public override bool TryGetState(out UIState state)
    {
        state = new LeaderboardState(null); // Ver nota abajo
        return true;
    }

    public override void Dispose()
    {
        StopScroll();
        base.Dispose();
    }

    public override void End()
    {
        StopScroll();
        EventSystem.current.SetSelectedGameObject(null);
        base.End();
    }
}