using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class TabGroup : MonoBehaviour
{
    [SerializeField] private List<Tab> tabs;
    [SerializeField] private int defaultIndex = 0;

    public void Init()
    {
        if (tabs.Count == 0) return;  
        
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i;
            tabs[i].TabButton.onClick.AddListener(() => ShowTab(index));
        }

        ShowTab(defaultIndex);
    }

    private void ShowTab(int index)
    {
        if (tabs.Count == 0) return;  
        for (int i = 0; i < tabs.Count; i++)
        {
            if(i == index)
            {
                tabs[i].TabContent.SetActive(true);
            }
            else
            {
                tabs[i].TabContent.SetActive(false);
            }
        }
    }
}
