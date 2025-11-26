using UnityEngine;
using Game.InputSystem;

public class SetCustomCursor : MonoBehaviour
{
    [SerializeField] private Texture2D normalCursor;
    [SerializeField] private Texture2D clickedCursor;

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
        ChangeCursor(normalCursor);
    }

    private void Start()
    {
        controls.UI.Click.started += _ => OnClick();
        controls.UI.Click.canceled += _ => OnCancelledClick();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void ChangeCursor(Texture2D cursor)
    {
        Cursor.SetCursor(cursor, Vector2.zero, CursorMode.ForceSoftware);

    }

    private void OnClick()
    {
        ChangeCursor(clickedCursor);

    }

    private void OnCancelledClick()
    {
        ChangeCursor(normalCursor);

    }
}
