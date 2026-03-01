using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AnalysisManager : MonoBehaviour
{
    public static AnalysisManager Instance { get; private set; }

    [Header("左侧 UI：4个嫌疑人头像")]
    public Button[] avatarButtons; // 请在Inspector拖入左侧的4个Button
    public Image[] avatarImages;   // 对应Button上的Image组件

    [Header("中央与右侧 UI")]
    public Image centerPortrait;
    public TextMeshProUGUI profileText;

    [Header("审问问题 UI (分析成功后弹出)")]
    public GameObject questionPanel;        // 问题选择面板的父节点 (默认隐藏)
    public Transform questionListParent;    // 动态生成问题的容器
    public GameObject questionTogglePrefab; // 包含 Toggle 和 TextMeshProUGUI 的预制体
    public Button confirmQuestionsButton;   // 确认按钮
    public TextMeshProUGUI selectionCountText; // 显示 "已选 0/3"

    // 记录当前生成的问题Toggle列表及其对应的数据
    private Dictionary<Toggle, string> currentQuestionToggles = new Dictionary<Toggle, string>();
    private int selectedCount = 0;
    private const int MAX_SELECTION = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject); //
        else Instance = this; //
    }

private void Start()
    {
        // 1. 绑定确认按钮事件
        confirmQuestionsButton.onClick.AddListener(OnConfirmQuestionsClicked);
        questionPanel.SetActive(false);

        // 【修改点 1】：事件监听分离
        // 让嫌疑人切换事件只去刷新中央和右侧的数据，绝不碰左侧的按钮
        GameManager.Instance.SuspectManager.OnCurrentSuspectChanged += UpdateSuspectDetailsUI;

        // 获取测试 IDs
        List<string> testDailyIDs = new List<string>();
        foreach(var kvp in GameManager.Instance.SuspectManager.GetAllSuspects())
        {
            testDailyIDs.Add(kvp.Key);
            if (testDailyIDs.Count == 4) break;
        }

        // 【修改点 2】：先生成并绑定左侧的头像按钮，整个生命周期只执行这一次！
        InitLineupUI(testDailyIDs);

        // 最后设置每日阵容，这会触发默认选中第一个嫌疑人，从而安全调用 UpdateSuspectDetailsUI
        GameManager.Instance.SuspectManager.SetupDailyLineup(testDailyIDs);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.SuspectManager != null)
        {
            GameManager.Instance.SuspectManager.OnCurrentSuspectChanged -= UpdateSuspectDetailsUI;
        }
    }

    // --- UI 刷新模块 ---

    // 【新增】：只负责把左侧的 4 个按钮绑好（头像 + 点击事件）
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
                    
                    // 闭包绑定点击事件，这里只会执行一次，非常安全
                    avatarButtons[i].onClick.RemoveAllListeners();
                    avatarButtons[i].onClick.AddListener(() => 
                    {
                        GameManager.Instance.SuspectManager.SetCurrentSuspect(idToSelect);
                        // 加一句强力 Log 验证点击是否穿透
                        Debug.Log($"<color=#00FF00>[UI 交互]</color> 成功点击了嫌疑人: {idToSelect}");
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

    // 【修改点 3】：精简后的刷新方法，只负责渲染当前选中的嫌疑人数据
    private void UpdateSuspectDetailsUI(RuntimeSuspect currentSuspect)
    {
        if (currentSuspect == null) return;

        centerPortrait.sprite = currentSuspect.BaseData.portrait;
        profileText.text = currentSuspect.BaseData.profileRichText;
        
        // 切换嫌疑人时，重置并隐藏问题面板
        questionPanel.SetActive(false); 

        // 【新增】：切换嫌疑人时，强制清空上一个人的线索池
        if (ScoreController.Instance != null)
        {
            ScoreController.Instance.ClearAllClues();
        }
    }

    // --- 核心逻辑：接收分数并匹配问题池 ---
    public void ProcessCaseAnalysis(int totalScore, int totalClues)
    {
        RuntimeSuspect targetSuspect = GameManager.Instance.SuspectManager.CurrentSuspect;
        if (targetSuspect == null) return; //

        List<InterrogationQuestion> matchedQuestions = null;
        foreach (var tier in targetSuspect.BaseData.interrogationTiers)
        {
            if (totalScore >= tier.minScore && totalScore <= tier.maxScore)
            {
                matchedQuestions = tier.availableQuestions;
                break;
            }
        }

        if (matchedQuestions == null || matchedQuestions.Count == 0)
        {
            Debug.LogWarning("未匹配到任何问题列表！");
            return;
        }

        ShowQuestionSelectionPanel(matchedQuestions);
    }

    // --- UI：生成问题选项并控制选中数量 ---
    private void ShowQuestionSelectionPanel(List<InterrogationQuestion> questions)
    {
        questionPanel.SetActive(true);
        selectedCount = 0;
        UpdateSelectionCountText();

        // 清理旧的 Toggle
        foreach (Transform child in questionListParent) Destroy(child.gameObject);
        currentQuestionToggles.Clear();

        // 生成新的 Toggle
        foreach (var q in questions)
        {
            GameObject toggleObj = Instantiate(questionTogglePrefab, questionListParent);
            Toggle toggle = toggleObj.GetComponent<Toggle>(); 
            TextMeshProUGUI label = toggleObj.GetComponentInChildren<TextMeshProUGUI>();

            // --- 加上这层安全校验 ---
            if (toggle == null)
            {
                Debug.LogError($"[UI 实例化失败] 在预制体上找不到 Toggle 组件！请检查 {questionTogglePrefab.name} 的根节点。");
                continue;
            }
            if (label == null)
            {
                Debug.LogError($"[UI 实例化失败] 找不到 TextMeshProUGUI 组件！是不是错用成了旧版 Text？");
                continue;
            }

            label.text = q.questionInfo;
            toggle.isOn = false;

            toggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(toggle, isOn));
            currentQuestionToggles.Add(toggle, q.inkVariableName);
        }
    }

    private void OnToggleValueChanged(Toggle changedToggle, bool isOn)
    {
        if (isOn)
        {
            if (selectedCount >= MAX_SELECTION)
            {
                // 如果已经选了3个，强制取消当前的勾选
                changedToggle.SetIsOnWithoutNotify(false);
                Debug.Log("最多只能选择3个问题！");
                return;
            }
            selectedCount++;
        }
        else
        {
            selectedCount--;
        }
        
        UpdateSelectionCountText();
    }

    private void UpdateSelectionCountText()
    {
        if (selectionCountText != null)
        {
            selectionCountText.text = $"已选择质询方向: {selectedCount} / {MAX_SELECTION}";
        }
    }

    // --- 确认按钮：将结果写入 Ink ---
    private void OnConfirmQuestionsClicked()
    {
        RuntimeSuspect targetSuspect = GameManager.Instance.SuspectManager.CurrentSuspect;
        if (targetSuspect == null) return;

        List<string> finalSelectedVars = new List<string>();
        foreach (var kvp in currentQuestionToggles)
        {
            if (kvp.Key.isOn)
            {
                finalSelectedVars.Add(kvp.Value);
            }
        }

        Debug.Log($"确认提交！共选中 {finalSelectedVars.Count} 个问题。");

        // 更新 Ink 变量
        foreach (string inkVar in finalSelectedVars)
        {
            // 你的实际项目里在这里对接 Ink 变量更新，例如：
            // DialogueManager.Instance.currentStory.variablesState[inkVar] = true;
            Debug.Log($"[Ink更新] 变量 {inkVar} = true");
        }

        targetSuspect.isInterrogated = true; // 标记嫌疑人状态为已审问
        questionPanel.SetActive(false); // 隐藏面板
        
        // TODO: 可以在这里直接跳转到审问场景 (LoadScene)
    }
}