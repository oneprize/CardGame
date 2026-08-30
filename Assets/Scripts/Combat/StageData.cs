using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Combat
{
    // One node on the linear map: which enemies appear together in this fight.
    [CreateAssetMenu(menuName = "CardGame/Stage", fileName = "NewStage")]
    public class StageData : ScriptableObject
    {
        public string label;
        public List<EnemyData> enemies; // 1~3
    }
}
