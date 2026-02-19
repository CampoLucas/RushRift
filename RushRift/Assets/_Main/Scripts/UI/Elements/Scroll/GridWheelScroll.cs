using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GridWheelScroll : MonoBehaviour
{
    [Header("References")]
    [Tooltip("the mask rect (RectMask2D object)")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform container;
    [Tooltip("The grid component in the container")]
    [SerializeField] private GridLayoutGroup grid;

    [Header("Scroll")]
    [Tooltip("1 = one row per wheel tick-ish")]
    [SerializeField] private float rowsPerWheelNotch = 1f;
    [Tooltip("0 = no smoothing")]
    [SerializeField] private float smooth = 18f;
    [SerializeField] private bool invert;

    [Header("Optional")]
    [SerializeField] private bool onlyWhenPointerOverViewport = true;
    [Tooltip("World-space canvas event camera if needed")]
    [SerializeField] private Camera uiEventCamera;

    private float _baseY;
    private float _targetOffsetY;
    private float _currentOffsetY;

    private void Awake()
    {
        if (!viewport) viewport = GetComponent<RectTransform>();
        if (!grid && container) grid = container.GetComponent<GridLayoutGroup>();
    }

    private void Start()
    {
        if (!viewport || !container || !grid)
        {
            Debug.LogError("SimpleWheelGridScroll: Assign viewport, container, and grid.");
            enabled = false;
            return;
        }

        _baseY = container.anchoredPosition.y;
        RebuildAndClamp();
        ApplyOffsetInstant();
    }

    private void Update()
    {
        if (onlyWhenPointerOverViewport && !IsPointerOverViewport())
            return;

        var wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.0001f)
        {
            if (invert) wheel = -wheel;

            RebuildAndClamp(); // cheap enough, but you can throttle if you want

            var rowStep = GetRowStep();
            _targetOffsetY += -wheel * rowsPerWheelNotch * rowStep; // wheel up usually scrolls content down
            _targetOffsetY = Mathf.Clamp(_targetOffsetY, 0f, GetMaxOffset());
        }

        if (smooth <= 0f)
        {
            _currentOffsetY = _targetOffsetY;
        }
        else
        {
            _currentOffsetY = Mathf.Lerp(_currentOffsetY, _targetOffsetY, 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime));
        }

        var p = container.anchoredPosition;
        p.y = _baseY + _currentOffsetY;
        container.anchoredPosition = p;
    }
    
    public void ResetScroll(bool instant = true)
    {
        _targetOffsetY = 0f;

        if (instant)
        {
            _currentOffsetY = 0f;

            var p = container.anchoredPosition;
            p.y = _baseY;
            container.anchoredPosition = p;
        }
    }

    public void RebuildAndClamp()
    {
        // Force layout to update so measurements are correct.
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);

        var max = GetMaxOffset();
        _targetOffsetY = Mathf.Clamp(_targetOffsetY, 0f, max);
        _currentOffsetY = Mathf.Clamp(_currentOffsetY, 0f, max);
    }
    
    private void Reset()
    {
        // Try auto-wire if placed on viewport (mask)
        viewport = GetComponent<RectTransform>();
    }

    private void ApplyOffsetInstant()
    {
        _currentOffsetY = _targetOffsetY;
        var p = container.anchoredPosition;
        p.y = _baseY + _currentOffsetY;
        container.anchoredPosition = p;
    }

    private float GetRowStep()
    {
        // How far to move to reveal the next row.
        return grid.cellSize.y + grid.spacing.y;
    }

    private float GetMaxOffset()
    {
        var contentH = GetContentHeight();
        var viewportH = viewport.rect.height;
        return Mathf.Max(0f, contentH - viewportH);
    }

    private float GetContentHeight()
    {
        var childCount = container.childCount;
        if (childCount == 0)
            return 0f;

        var columns = GetColumnCount();
        var rows = Mathf.CeilToInt(childCount / (float)columns);

        var cellH = grid.cellSize.y;
        var spacingH = grid.spacing.y;
        var padding = grid.padding.top + grid.padding.bottom;

        var total = padding + rows * cellH + Mathf.Max(0, rows - 1) * spacingH;

        return total;
    }

    private int GetColumnCount()
    {
        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount) return grid.constraintCount;

        // If not fixed, compute from viewport width.
        var available = viewport.rect.width - grid.padding.left - grid.padding.right;
        var step = grid.cellSize.x + grid.spacing.x;
        return Mathf.Max(1, Mathf.FloorToInt((available + grid.spacing.x) / step));
    }

    private bool IsPointerOverViewport()
    {
        // This checks if pointer is inside viewport rect in screen space.
        Vector2 sp = Input.mousePosition;
        return RectTransformUtility.RectangleContainsScreenPoint(viewport, sp, uiEventCamera);
    }
}