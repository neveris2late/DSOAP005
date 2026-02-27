//职责： 管理分析池、记录分数、执行最终的“分析”聚拢动画

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;

public class ScoreController : MonoBehaviour
{
    public static ScoreController Instance;

    [Header("UI References")]
    public Transform analysisPool; // 分析池的父节点
    public Button analyzeButton;   // 分析按钮
    public Transform analyzeCenterPoint; // 聚拢的中心点

    [Header("Prefabs")]
    public GameObject floatingCluePrefab; // 漂浮线索预制体

    // 存储当前池中的线索
    private List<FloatingClue> activeClues = new List<FloatingClue>();

    // 模拟线索数据库 (实际开发中可以读取配置表)
    public Dictionary<string, bool> clueDatabase = new Dictionary<string, bool>()
    {
        {"android_core", true}, // 核心是被破坏的 (+1分)
        {"fake_id", false},     // 伪造的ID (0分)
    };

    private void Awake()
    {
        Instance = this;
        analyzeButton.onClick.AddListener(OnAnalyzeClicked);
    }

    // 供 InteractableText 调用的生成方法
    // 接收从 InteractableText 传来的 isGood 参数
    public void AddClueToPool(string clueID, string clueName, bool isGood)
    {
        if (activeClues.Exists(c => c.clueID == clueID)) return;

        GameObject newClueObj = Instantiate(floatingCluePrefab, analysisPool);
        FloatingClue clueScript = newClueObj.GetComponent<FloatingClue>();
        
        // 直接使用传入的 isGood 属性进行初始化
        clueScript.Init(clueID, clueName, isGood);
        activeClues.Add(clueScript);
    }

    public void RemoveClue(FloatingClue clue)
    {
        activeClues.Remove(clue);
        Destroy(clue.gameObject);
    }

    private void OnAnalyzeClicked()
    {
        if (activeClues.Count == 0) return; // 如果池子里没线索，不执行分析

        analyzeButton.interactable = false;
        int totalScore = 0;

        // 1. 聚拢动画与分数计算
        foreach (var clue in activeClues)
        {
            // 如果线索标签被判定为 true，则 +1 分；false 默认 +0 分
            if (clue.isGood) 
            {
                totalScore++; 
            }
            
            clue.transform.DOKill(); 
            clue.transform.DOMove(analyzeCenterPoint.position, 0.5f).SetEase(Ease.InBack);
            clue.transform.DOScale(Vector3.zero, 0.5f).SetDelay(0.3f);
        }

        // 2. 延迟等待动画播放完毕，输出最终得分
        DOVirtual.DelayedCall(1.5f, () => 
        {
            Debug.Log($"<color=cyan>案件分析完成！当前线索池总得分: {totalScore} / {activeClues.Count}</color>");
            
            // TODO: 在这里根据 totalScore 触发不同的剧情分支或 UI 提示
            
            foreach (var clue in activeClues) Destroy(clue.gameObject);
            activeClues.Clear();
            analyzeButton.interactable = true;
        });
    }
}