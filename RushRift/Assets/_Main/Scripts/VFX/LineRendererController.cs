using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRendererController : MonoBehaviour
{
    [SerializeField] private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    public void SetPosition(Vector3 startPos, Vector3 endPos)
    {
        if (lineRenderers.Count <= 0) return;

        for (var i = 0; i < lineRenderers.Count; i++)
        {
            if (lineRenderers[i].positionCount < 2)
            {
#if UNITY_EDITOR
                Debug.Log("The line renderer should have at least 2 positions.");
#endif
            }
            
            lineRenderers[i].SetPosition(0, startPos);
            lineRenderers[i].SetPosition(1, endPos);
        }
    }
}
