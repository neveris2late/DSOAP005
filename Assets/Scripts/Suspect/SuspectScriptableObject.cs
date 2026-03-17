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
    
    [Header("真实身份判定 (剧透)")]
    [Tooltip("是否为间谍。如果和仿生人同时为false，则为普通人。")]
    public bool isSpy;      
    [Tooltip("是否为仿生人。注：嫌疑人可以同时是间谍和仿生人。")]
    public bool isAndroid;

    [Header("美术资源")]
    public Sprite avatar;            // 角色头像
    
    [Tooltip("用于档案界面的全身/半身立绘")]
    public Sprite filePortrait;          

    [Tooltip("用于在场景中与玩家对话时弹出的正面立绘（交由UI层Canvas显示）")]
    public Sprite dialoguePortrait;  

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
    
    [Header("审问场景表现")]
    [Tooltip("包含该嫌疑人专属的场景背景以及角色【工作状态】立绘的Prefab容器。")]
    public GameObject interrogationScenePrefab; 
    
    //进入场景后的提示文本
    [Tooltip("入场动画结束后显示的提示文字")]
    [TextArea(2, 4)]
    public string enterHintText;
    
    [Header("音频配置")]
    [Tooltip("该嫌疑人专属的背景音乐 (BGM)")]
    public AudioClip bgmClip;

    [Tooltip("该嫌疑人专属的环境音效 (Ambient)，例如：工厂机器声、雨声、电流声。如果没有可留空 (None)")]
    public AudioClip ambientClip;
    
    // === 新增：嫌疑人专属打字机音高 ===
    [Tooltip("该嫌疑人打字机音效的基础音高 (Pitch)。低于1声音低沉，高于1声音尖锐。")]
    [Range(0.5f, 2.5f)]
    public float typingBasePitch = 1f;
    
    [Header("地图配置")]
    [Tooltip("该嫌疑人所在的地点名称，如：霓虹街区、废弃工厂")]
    public string locationName; 

    [Tooltip("地图上代表该地点的UI按钮Prefab")]
    public GameObject locationButtonPrefab;
}