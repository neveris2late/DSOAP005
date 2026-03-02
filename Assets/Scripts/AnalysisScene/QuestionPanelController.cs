using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class QuestionPanelController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject questionPanel;        
    public Transform questionListParent;    
    public GameObject questionTogglePrefab; 
    public Button confirmQuestionsButton;   // QuestionPanel 专属的 Confirm 按钮
    public TextMeshProUGUI selectionCountText; 

    private Dictionary<Toggle, string> currentQuestionToggles = new Dictionary<Toggle, string>();
    private int selectedCount = 0;
    private const int MAX_SELECTION = 3;

    // 事件：当玩家点击问题面板的确认按钮后触发，用来通知 AnalysisManager
    public event Action OnQuestionsConfirmed;

    private void Start()
    {
    // 【修改点】：初始化时，同时失活确认按钮和计数文本
        if (confirmQuestionsButton != null) confirmQuestionsButton.gameObject.SetActive(false);
        if (selectionCountText != null) selectionCountText.gameObject.SetActive(false);
        
        confirmQuestionsButton.onClick.AddListener(OnConfirmQuestionsClicked); //
        questionPanel.SetActive(false); //
    }

    public void OpenPanel(List<InterrogationQuestion> questions)
    {
    // 【修改点】：打开面板时，重新激活它俩
        if (confirmQuestionsButton != null) confirmQuestionsButton.gameObject.SetActive(true);
        if (selectionCountText != null) selectionCountText.gameObject.SetActive(true);

        questionPanel.SetActive(true); //
        selectedCount = 0; //
        UpdateSelectionCountText(); //

        // 清理旧的 Toggle
        foreach (Transform child in questionListParent) Destroy(child.gameObject);
        currentQuestionToggles.Clear();

        // 生成新的 Toggle
        foreach (var q in questions)
        {
            GameObject toggleObj = Instantiate(questionTogglePrefab, questionListParent);
            Toggle toggle = toggleObj.GetComponent<Toggle>(); 
            TextMeshProUGUI label = toggleObj.GetComponentInChildren<TextMeshProUGUI>();

            if (toggle == null || label == null) continue;

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

    private void OnConfirmQuestionsClicked()
    {
        RuntimeSuspect targetSuspect = GameManager.Instance.SuspectManager.CurrentSuspect;
        if (targetSuspect == null) return;

        List<string> finalSelectedVars = new List<string>();
        foreach (var kvp in currentQuestionToggles)
        {
            if (kvp.Key.isOn) finalSelectedVars.Add(kvp.Value);
        }

        Debug.Log($"确认提交！共选中 {finalSelectedVars.Count} 个问题。");

        // 将选中的变量存储到当前嫌疑人的运行时数据中
        targetSuspect.selectedQuestionVars = finalSelectedVars;
        
        // 【新增】：修改状态为已分析
        targetSuspect.isAnalysed = true; 

        questionPanel.SetActive(false); // 隐藏面板
        
        // 通知 AnalysisManager 分析结束了
        OnQuestionsConfirmed?.Invoke(); 
    }

    // 强制关闭面板的方法（用于切换嫌疑人时）
    public void ForceClose()
    {
        // 【修改点】：强制关闭时，一并失活计数文本和确认按钮
        if (confirmQuestionsButton != null) confirmQuestionsButton.gameObject.SetActive(false);
        if (selectionCountText != null) selectionCountText.gameObject.SetActive(false);
        questionPanel.SetActive(false);
    }
}