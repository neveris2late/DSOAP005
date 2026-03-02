using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AnalysisManager : MonoBehaviour
{
    public static AnalysisManager Instance { get; private set; }
    
    // 【新增】：用于整体控制分析界面的父级物体
    [Header("场景主要面板")]
    public GameObject analysisMainPanel;

    [Header("左侧 UI：4个嫌疑人头像")]
    public Button[] avatarButtons; 
    public Image[] avatarImages;   

    [Header("中央与右侧 UI")]
    public Image centerPortrait;
    public TextMeshProUGUI profileText;

    [Header("引用外部控制器")]
    // 【新增】：引用新抽离的问题面板控制器
    public QuestionPanelController questionPanelController; 
    // 【新增】：引用提示面板控制器
    public HintPanelController hintPanelController;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject); 
        else Instance = this; 
    }

    private void Start()
    {
        // 1. 初始状态下，先隐藏整个分析主界面
        if (analysisMainPanel != null) analysisMainPanel.SetActive(false);

        GameManager.Instance.SuspectManager.OnCurrentSuspectChanged += UpdateSuspectDetailsUI;
        questionPanelController.OnQuestionsConfirmed += OnQuestionPanelConfirmed;

        // 加载数据结构 (此处修改组件的状态不会导致它们立刻显示，因为父节点被隐藏了)
        List<string> testDailyIDs = new List<string>();
        foreach(var kvp in GameManager.Instance.SuspectManager.GetAllSuspects())
        {
            testDailyIDs.Add(kvp.Key);
            if (testDailyIDs.Count == 4) break;
        }

        InitLineupUI(testDailyIDs);
        GameManager.Instance.SuspectManager.SetupDailyLineup(testDailyIDs);

        // 【新增】：强制让UI进入未选中任何嫌疑人的空状态
        UpdateSuspectDetailsUI(null);

        // 2. 启动 Hint 面板逻辑
        if (hintPanelController != null)
        {
            // 监听 Hint 结束点击事件
            hintPanelController.OnHintFinished += OnHintCompleted;
            // 开启 Hint 流程
            hintPanelController.StartHint();
        }
        else
        {
            // 防御性代码：如果没有挂载 HintPanel，直接打开分析面板
            if (analysisMainPanel != null) analysisMainPanel.SetActive(true);
        }
    }
    
    // 【新增】：当 HintPanel 发出结束信号时触发
    private void OnHintCompleted()
    {
        // 激活主要的分析面板
        if (analysisMainPanel != null) analysisMainPanel.SetActive(true);
    }
    
    private void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.SuspectManager != null)
        {
            GameManager.Instance.SuspectManager.OnCurrentSuspectChanged -= UpdateSuspectDetailsUI;
        }
        if (questionPanelController != null)
        {
            questionPanelController.OnQuestionsConfirmed -= OnQuestionPanelConfirmed;
        }
    }

    private void InitLineupUI(List<string> dailyIDs)
    {
        for (int i = 0; i < avatarButtons.Length; i++)
        {
            if (i < dailyIDs.Count)
            {
                string idToSelect = dailyIDs[i];
                RuntimeSuspect suspect = GameManager.Instance.SuspectManager.GetSuspect(idToSelect);

                if (suspect != null)
                {
                    avatarButtons[i].gameObject.SetActive(true);
                    avatarImages[i].sprite = suspect.BaseData.avatar;
                    
                    avatarButtons[i].onClick.RemoveAllListeners();
                    avatarButtons[i].onClick.AddListener(() => 
                    {
                        GameManager.Instance.SuspectManager.SetCurrentSuspect(idToSelect);
                    });
                }
            }
            else
            {
                avatarButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void UpdateSuspectDetailsUI(RuntimeSuspect currentSuspect)
    {
        // 【新增】：处理没有任何嫌疑人被选中的空状态
        if (currentSuspect == null) 
        {
            centerPortrait.enabled = false; // 关闭Image组件，防止空Sprite渲染成白块
            profileText.text = "待指定嫌疑人";
            
            if (ScoreController.Instance != null)
            {
                ScoreController.Instance.ClearAllClues();
            }
            return;
        }

        // 【新增】：恢复立绘Image的显示
        centerPortrait.enabled = true;
        centerPortrait.sprite = currentSuspect.BaseData.portrait;
        
        // 【修改点】：判断该嫌疑人是否已经被分析过
        if (currentSuspect.isAnalysed)
        {
            // 在原有档案下方追加红色的已分析提示
            profileText.text = currentSuspect.BaseData.profileRichText + "\n\n<color=#FF3333><b>[ Already Analysed - 档案分析已完成 ]</b></color>";
        }
        else
        {
            profileText.text = currentSuspect.BaseData.profileRichText;
        }
        
        // 切换嫌疑人时，强制关闭问题面板
        questionPanelController.ForceClose(); 
        
        // 确保头像按钮是激活状态
        SetAvatarButtonsInteractable(true);

        if (ScoreController.Instance != null)
        {
            ScoreController.Instance.ClearAllClues();
        }
    }

    // --- 分析池确认(AnalyzeButton)触发此处 ---
    public void ProcessCaseAnalysis(int totalScore, int totalClues)
    {
        RuntimeSuspect targetSuspect = GameManager.Instance.SuspectManager.CurrentSuspect;
        if (targetSuspect == null) return; // 如果当前是空状态，直接返回拦截

        List<InterrogationQuestion> matchedQuestions = null;
        foreach (var tier in targetSuspect.BaseData.interrogationTiers)
        {
            if (totalScore >= tier.minScore && totalScore <= tier.maxScore)
            {
                matchedQuestions = tier.availableQuestions;
                break;
            }
        }

        if (matchedQuestions == null || matchedQuestions.Count == 0) return;

        // 【核心逻辑】：打开问题面板，并失活左侧的所有嫌疑人头像
        questionPanelController.OpenPanel(matchedQuestions);
        SetAvatarButtonsInteractable(false);
    }

    // 【新增】：当 QuestionPanel 点击了它自己的 Confirm 按钮后，会触发这里
    private void OnQuestionPanelConfirmed()
    {
        // 1. 重新激活所有头像按钮
        SetAvatarButtonsInteractable(true);
        
        // 2. 刷新当前嫌疑人的 UI 以便显示 "[ Already Analysed ]" 文本
        UpdateSuspectDetailsUI(GameManager.Instance.SuspectManager.CurrentSuspect);
    }

    // 【新增】：控制左侧所有头像按钮是否可交互的方法
    private void SetAvatarButtonsInteractable(bool state)
    {
        foreach (var btn in avatarButtons)
        {
            if (btn != null)
            {
                btn.interactable = state;
            }
        }
    }
}