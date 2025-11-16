using System.Collections;
using System.Collections.Generic;
using Game.Levels;
using Game.UI.StateMachine;
using UnityEngine;

public class MedalGroup : MonoBehaviour
{
    [SerializeField] private LevelWonPresenter presenter;
    [SerializeField] private Medal bronze;
    [SerializeField] private Medal silver;
    [SerializeField] private Medal gold;

    public void Init()
    {
        var model = presenter.GetModel();

        if (model.TryGetMedal(MedalType.Bronze, out var medal))
        {
            bronze.gameObject.SetActive(true);
            
            bronze.Init(medal.MedalTime, medal.Unlocked);
        }
        else
        {
            bronze.gameObject.SetActive(false);
        }
        
        if (model.TryGetMedal(MedalType.Silver, out medal))
        {
            silver.gameObject.SetActive(true);
            
            silver.Init(medal.MedalTime, medal.Unlocked);
        }
        else
        {
            silver.gameObject.SetActive(false);
        }
        
        if (model.TryGetMedal(MedalType.Gold, out medal))
        {
            gold.gameObject.SetActive(true);
            
            gold.Init(medal.MedalTime, medal.Unlocked);
        }
        else
        {
            gold.gameObject.SetActive(false);
        }
    }
}
