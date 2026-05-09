using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
    public string questID;
    public string questName;
    public string description;
    public List<QuestObjective> objectives;

    //Called when scriptable object is edited
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(questID))
        {
            questID = questName + Guid.NewGuid().ToString();
        }
    }

    [System.Serializable]
    public class QuestObjective
    {
        public string objectiveID; //Matches with the item ID that you'd need to collect
        public string description;
        public ObjectiveType type;
        public int requiredAmount;
        public int currentAmount;

        public bool IsCompleted => currentAmount >= requiredAmount;
    }

    public enum ObjectiveType {CollectItem, TalkNPC, Custom}

    [System.Serializable]
    public class QuestProgress
    {
        public Quest quest;
        public List <QuestObjective> objectives;

        public QuestProgress(Quest quest)
        {
            this.quest = quest;
            objectives = new List<QuestObjective>();

            //Deep copy to avoid changing the original
            foreach (var obj in quest.objectives)
            {
                objectives.Add(new QuestObjective
                {
                    objectiveID = obj.objectiveID,
                    description = obj.description,
                    type = obj.type,
                    requiredAmount = obj.requiredAmount,
                    currentAmount = 0
                });
                
            }
        }
        public bool IsCompleted => objectives.TrueForAll(o => o.IsCompleted);

        public string QuestID => quest.questID;
    }

}
