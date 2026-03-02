using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;

public class ScoreController : MonoBehaviour
{
    public static ScoreController Instance;

    [Header("UI References")]
    public Transform analysisPool; 
    public Button analyzeButton;   
    public Transform analyzeCenterPoint; 
    
    // 【新增】：超出上限时的警告提示框
    public GameObject maxCluesWarningBox; 

    [Header("Prefabs")]
    public GameObject floatingCluePrefab; 

    private List<FloatingClue> activeClues = new List<FloatingClue>();
    
    // 【新增】：设置最大线索数量
    private const int MAX_CLUES_COUNT = 5;

    public Dictionary<string, bool> clueDatabase = new Dictionary<string, bool>()
    {
        {"android_core", true}, 
        {"fake_id", false},     
    };

    private void Awake()
    {
        Instance = this;
        analyzeButton.onClick.AddListener(OnAnalyzeClicked);
        
        // 【新增】：初始状态隐藏警告框
        if (maxCluesWarningBox != null) maxCluesWarningBox.SetActive(false);
    }

    // 【新增】：供 InteractableText 调用的预检方法
    public bool CanAddClue(string clueID)
    {
        // 如果池子里已经有这个线索了，直接返回 false (不显示警告，只是防止重复添加和重复飞行动画)
        if (activeClues.Exists(c => c.clueID == clueID)) return false; 

        // 如果已经达到或超过5个
        if (activeClues.Count >= MAX_CLUES_COUNT)
        {
            // 激活警告提示框
            if (maxCluesWarningBox != null) maxCluesWarningBox.SetActive(true);
            return false;
        }

        return true;
    }

    public void AddClueToPool(string clueID, string clueName, bool isGood)
    {
        if (activeClues.Exists(c => c.clueID == clueID)) return;
        // 双重保险
        if (activeClues.Count >= MAX_CLUES_COUNT) return; 

        GameObject newClueObj = Instantiate(floatingCluePrefab, analysisPool);
        FloatingClue clueScript = newClueObj.GetComponent<FloatingClue>();
        
        clueScript.Init(clueID, clueName, isGood);
        activeClues.Add(clueScript);
    }

    public void RemoveClue(FloatingClue clue)
    {
        activeClues.Remove(clue);
        Destroy(clue.gameObject);
        
        // 【新增】：当线索数量减少到安全线以下时，隐藏警告框
        if (activeClues.Count < MAX_CLUES_COUNT && maxCluesWarningBox != null)
        {
            maxCluesWarningBox.SetActive(false);
        }
    }

    private void OnAnalyzeClicked()
    {
        if (activeClues.Count == 0) return; 

        analyzeButton.interactable = false; 
        int totalScore = 0; 
        
        // 【新增】：点击分析时也把警告框关掉
        if (maxCluesWarningBox != null) maxCluesWarningBox.SetActive(false);

        foreach (var clue in activeClues) 
        {
            if (clue.isGood) totalScore++; 
            
            Rigidbody2D rb = clue.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;

            clue.transform.DOKill(); 
            clue.transform.DOMove(analyzeCenterPoint.position, 0.5f).SetEase(Ease.InBack); 
            clue.transform.DOScale(Vector3.zero, 0.5f).SetDelay(0.3f); 
        }

        DOVirtual.DelayedCall(1.5f, () =>  
        {
            Debug.Log($"案件分析完成！得分: {totalScore} / {activeClues.Count}"); 
            
            if (AnalysisManager.Instance != null)
            {
                AnalysisManager.Instance.ProcessCaseAnalysis(totalScore, activeClues.Count);
            }
            
            foreach (var clue in activeClues) Destroy(clue.gameObject); 
            activeClues.Clear(); 
            analyzeButton.interactable = true; 
        });
    }
    
    public void ClearAllClues()
    {
        foreach (var clue in activeClues)
        {
            if (clue != null && clue.gameObject != null)
            {
                clue.transform.DOKill(); 
                Destroy(clue.gameObject);
            }
        }
        activeClues.Clear();
        
        // 【新增】：清空池子时，重置警告框状态
        if (maxCluesWarningBox != null) maxCluesWarningBox.SetActive(false);
    }
}