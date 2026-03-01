using UnityEngine;
using System;
using System.Collections.Generic;

// 定义单个问题的数据结构
[Serializable]
public class InterrogationQuestion
{
    [Tooltip("对应 Ink 脚本中的变量名，例如 q1_selected")]
    public string inkVariableName; 
    
    [Tooltip("显示在UI上的问题简介")]
    [TextArea(2, 4)]
    public string questionInfo; 
}

// 定义分数段的数据结构
[Serializable]
public class ScoreTier
{
    public string tierName; // 方便在Inspector里辨认，例如 "低分 0-2"
    public int minScore;    // 包含该分数
    public int maxScore;    // 包含该分数
    
    [Tooltip("该分数段下，供玩家挑选的问题池")]
    public List<InterrogationQuestion> availableQuestions;
}



// 在Project窗口右键即可创建该资产： Create -> 游戏数据 -> 嫌疑人档案
[CreateAssetMenu(fileName = "New Suspect", menuName = "游戏数据/嫌疑人档案")]
public class SuspectScriptableObject : ScriptableObject
{
    [Header("基础信息")]
    public string suspectID;         // 嫌疑人唯一ID，推荐使用英文，方便代码调用
    public string suspectName;       // 嫌疑人显示名称

    [Header("美术资源")]
    public Sprite avatar;            // 角色头像
    public Sprite portrait;          // 角色立绘

    [Header("叙事与对话")]
    [Tooltip("拖入Ink编译后的 .json 文件")]
    public TextAsset inkStory;       // Ink 文件关联

    [Header("档案信息")]
    [TextArea(10, 20)]               // 让输入框更大，方便编辑富文本
    [Tooltip("支持富文本格式，例如：<b>加粗</b>, <color=#FF0000>红色</color>")]
    public string profileRichText;  // 嫌疑人档案（富文本）
    
    [Header("审问问题配置")]
    [Tooltip("根据线索得分配置不同的可用问题池")]
    public List<ScoreTier> interrogationTiers;
}