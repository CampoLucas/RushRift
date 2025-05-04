using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineEmiter : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private Transform _origin;
    private Vector3 _end;
    
    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    Vector3 Pos()
    {
        var dir = (_end - _origin.position);
        return _origin.position/* + Vector3.ClampMagnitude(dir, dir.magnitude * .5f)*/;
    }
    
    private void Update()
    {
        _lineRenderer.SetPosition(0, Pos());
        _lineRenderer.SetPosition(1, _end);
    }
    
    public void SetInfo(Transform hand, Vector3 endPoint)
    {
        _origin = hand;
        _end = endPoint;
    }
}
