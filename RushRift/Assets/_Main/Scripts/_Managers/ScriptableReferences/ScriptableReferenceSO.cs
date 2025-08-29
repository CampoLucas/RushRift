using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableReferenceSO", menuName = "ScriptableReference", order = 3)]
public  class ScriptableReferenceSO : ScriptableObject
{
    public List<EffectsReferences> effectsReferences = new();
    public List<LevelMedalsSO> medalReferences = new();
}
