using System;
using System.Collections.Generic;
using UnityEngine;

public enum SuspectStatus
{
    Pending,    // 待审问 (默认)
    Cleared,    // 已解除嫌疑
    Arrested,   // 已逮捕
    Dead        // 已死亡
}

public enum PlayerJudgment
{
    None,           // 未判定
    Innocent,       // 1 无责判定
    ArrestSpy,      // 2 缉拿间谍
    ExecuteAndroid, // 3-1 就地击杀 (仿生人)
    TestAndroid     // 3-2 脊髓测试 (仿生人)
}

[Serializable]
public class RuntimeSuspect
{
    // 对静态基础数据的引用
    public SuspectScriptableObject BaseData { get; private set; }

    // 下面是运行时状态（可用于存档/读档）
    public bool isAnalysed;        // 是否已被分析
    public bool isInterrogated;      // 是否已进行过审问
    public string currentInkState;   // 用于保存该嫌疑人Ink对话的进度状态(JSON字符串)
    // 存储在分析阶段选中的 Ink 变量名
    public List<string> selectedQuestionVars = new List<string>();
    
    // 嫌疑人的当前状态
    public SuspectStatus currentStatus = SuspectStatus.Pending;
    
    // 玩家最终的判定结果
    public PlayerJudgment finalJudgment = PlayerJudgment.None;

    public RuntimeSuspect(SuspectScriptableObject data)
    {
        BaseData = data;
        isAnalysed = false;
        isInterrogated = false;
        currentInkState = "";
        currentStatus = SuspectStatus.Pending; // 初始化为待审问
        finalJudgment = PlayerJudgment.None;
    }
    
    
}