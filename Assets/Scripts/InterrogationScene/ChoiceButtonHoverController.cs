using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro; 

public class ChoiceHoverController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string tooltipKey = ""; 
    private TextMeshProUGUI textUI; 

    private void Awake()
    {
        textUI = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Initialize(List<string> tags)
    {
        if (tags == null || tags.Count == 0) return;
        
        foreach (var rawTag in tags)
        {
            string normalizedTag = rawTag.Trim().ToLower();
            if (normalizedTag.StartsWith("hover:") || normalizedTag.StartsWith("hover："))
            {
                char[] separators = { ':', '：' };
                string[] parts = rawTag.Split(separators, 2); 
                
                if (parts.Length > 1)
                {
                    tooltipKey = parts[1].Trim(); 
                    // 雷达 1：检查是否从 Ink 成功拿到了标签
                    Debug.Log($"<color=green>【雷达1-解析成功】选项提取到 Key: [{tooltipKey}]</color>");
                    break; 
                }
            }
        }
    }
    
    // 新增的方法：直接接收切好的字符串
    public void InitializeByString(string tagString)
    {
        if (!string.IsNullOrEmpty(tagString))
        {
            tooltipKey = tagString; // 直接把 "施加压力" 存进钥匙孔
        }
        else
        {
            tooltipKey = ""; 
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (textUI != null) textUI.fontStyle |= FontStyles.Underline;

        if (!string.IsNullOrEmpty(tooltipKey))
        {
            if (ChoiceTooltipController.Instance != null)
            {
                // 雷达 2：检查鼠标悬停是否触发，且 Manager 是否存在
                Debug.Log($"<color=yellow>【雷达2-触发面板】正在呼叫管理器显示: [{tooltipKey}]</color>");
                ChoiceTooltipController.Instance.ShowTooltip(tooltipKey);
            }
            else
            {
                Debug.LogError("【严重错误】ChoiceTooltipController.Instance 为空！请检查场景中是否有管理器物体，且该物体是激活状态。");
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (textUI != null) textUI.fontStyle &= ~FontStyles.Underline;
        
        if (ChoiceTooltipController.Instance != null)
        {
            ChoiceTooltipController.Instance.HideTooltip();
        }
    }
}