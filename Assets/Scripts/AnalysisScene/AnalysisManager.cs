using UnityEngine;
using System.Collections.Generic;

public class AnalysisManager : MonoBehaviour
{
    public static AnalysisManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 接收来自 ScoreController 的最终分析结果
    /// </summary>
    /// <param name="totalScore">有效线索(true)的总得分</param>
    /// <param name="totalClues">参与分析的线索总数</param>
    public void ProcessCaseAnalysis(int totalScore, int totalClues)
    {
        Debug.Log($"<color=orange>[AnalysisManager] 开始处理案情分析，得分: {totalScore}/{totalClues}</color>");

        // 【核心】通过 GameManager 获取 RuntimeSuspectManager
        RuntimeSuspectManager suspectManager = GameManager.Instance.SuspectManager;
        Dictionary<string, RuntimeSuspect> allSuspects = suspectManager.GetAllSuspects();

        // 根据分数进行逻辑判定
        if (totalScore == totalClues && totalClues > 0)
        {
            // 完美推论：所有放入分析池的线索都是正确的
            HandlePerfectAnalysis(suspectManager);
        }
        else if (totalScore > 0)
        {
            // 部分正确：可能混入了干扰项(false的线索)
            Debug.Log("推论存在瑕疵，或许该重新整理线索。");
        }
        else
        {
            // 完全错误：放进去的全是干扰项
            Debug.Log("南辕北辙的推论。");
        }
    }

    private void HandlePerfectAnalysis(RuntimeSuspectManager suspectManager)
    {
        Debug.Log("完美推论！准备推进剧情或解锁特定嫌疑人的对话。");

        // 示例：假设这次分析指向了 ID 为 "suspect_A" 的嫌疑人
        RuntimeSuspect targetSuspect = suspectManager.GetSuspect("suspect_A");
        if (targetSuspect != null)
        {
            targetSuspect.isDiscovered = true; // 更新运行时状态
            
            // TODO: 这里可以调用你的 Ink 剧情系统
            // 例如：InkDialogueManager.Instance.SetVariable("is_suspect_A_exposed", true);
            Debug.Log($"嫌疑人 {targetSuspect.BaseData.suspectName} 的隐藏状态已被揭开！");
        }

        // 如果需要遍历所有 4 个嫌疑人做群体状态更新，可以这样写：
        /*
        foreach (var kvp in suspectManager.GetAllSuspects())
        {
            RuntimeSuspect suspect = kvp.Value;
            // 检查或更新他们的状态
        }
        */
    }
}