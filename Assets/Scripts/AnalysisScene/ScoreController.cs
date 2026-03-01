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
        if (activeClues.Count == 0) return; //

        analyzeButton.interactable = false; //
        int totalScore = 0; //

        foreach (var clue in activeClues) 
        {
            if (clue.isGood) totalScore++; 
            
            // 【新增】：在执行 DOTween 聚拢前，先关掉这个线索的物理模拟
            Rigidbody2D rb = clue.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;

            clue.transform.DOKill(); 
            clue.transform.DOMove(analyzeCenterPoint.position, 0.5f).SetEase(Ease.InBack); 
            clue.transform.DOScale(Vector3.zero, 0.5f).SetDelay(0.3f); 
        }

        DOVirtual.DelayedCall(1.5f, () =>  //
        {
            Debug.Log($"案件分析完成！得分: {totalScore} / {activeClues.Count}"); //
            
            // 【关键点】：将分数传递给 AM
            if (AnalysisManager.Instance != null)
            {
                AnalysisManager.Instance.ProcessCaseAnalysis(totalScore, activeClues.Count);
            }
            
            foreach (var clue in activeClues) Destroy(clue.gameObject); //
            activeClues.Clear(); //
            analyzeButton.interactable = true; //
        });
    }
    
    // 【新增】：用于清空分析池
    public void ClearAllClues()
    {
        foreach (var clue in activeClues)
        {
            if (clue != null && clue.gameObject != null)
            {
                clue.transform.DOKill(); // 停止所有 DOTween 动画
                Destroy(clue.gameObject);
            }
        }
        activeClues.Clear();
    }
}