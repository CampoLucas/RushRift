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
        var viewportH = viewport.rect.height;
        var contentH = GetContentHeight();
        return Mathf.Max(0f, contentH - viewportH);
    }

    private float GetContentHeight()
    {
        // Prefer container rect height after rebuild (includes layout sizing).
        var h = container.rect.height;

        // If something keeps it at 0, compute manually from child count and constraint count.
        if (h > 0.01f) return h;

        var childCount = container.childCount;
        if (childCount <= 0) return 0f;

        var cols = GetColumnCountFallback();
        var rows = Mathf.CeilToInt(childCount / (float)cols);
        
        var padding = grid.padding.top + grid.padding.bottom;

        // rows * cellHeight + (rows-1)*spacing
        return padding + rows * grid.cellSize.y + Mathf.Max(0, rows - 1) * grid.spacing.y;
    }

    private int GetColumnCountFallback()
    {
        // Best: use constraint count if FixedColumnCount.
        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0)
            return grid.constraintCount;

        // Otherwise estimate from viewport width.
        var viewportW = viewport.rect.width;
        float pad = grid.padding.left + grid.padding.right;
        var cellW = grid.cellSize.x;
        var stepW = cellW + grid.spacing.x;

        var cols = Mathf.FloorToInt((viewportW - pad + grid.spacing.x) / stepW);
        return Mathf.Max(1, cols);
    }

    private bool IsPointerOverViewport()
    {
        // This checks if pointer is inside viewport rect in screen space.
        Vector2 sp = Input.mousePosition;
        return RectTransformUtility.RectangleContainsScreenPoint(viewport, sp, uiEventCamera);
    }
}