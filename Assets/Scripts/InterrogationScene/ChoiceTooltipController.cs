using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[Serializable]
public struct ChoiceAttribute
{
    public string tagKey;      
    public string title;       
    [TextArea(2, 4)]
    public string description; 
}

public class ChoiceTooltipController : MonoBehaviour
{
    public static ChoiceTooltipController Instance { get; private set; }

    [Header("UI 引用")]
    public RectTransform tooltipPanel;     
    public TextMeshProUGUI titleText;      
    public TextMeshProUGUI descriptionText;

    [Header("属性配置")]
    public List<ChoiceAttribute> attributes; 

    [Header("跟随设置")]
    [Tooltip("在 Camera 模式下，这里的数值代表 UI 局部坐标系的偏移量")]
    public Vector2 offset = new Vector2(20f, -20f); 

    // 新增：用于坐标转换的组件引用
    private RectTransform parentRect; 
    private Canvas parentCanvas;      

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
            
            // 获取 Tooltip 父物体的 RectTransform 和所在的 Canvas
            parentRect = tooltipPanel.parent.GetComponent<RectTransform>();
            parentCanvas = tooltipPanel.GetComponentInParent<Canvas>();
        }
    }

    private void Update()
    {
        if (tooltipPanel != null && tooltipPanel.gameObject.activeSelf)
        {
            // 获取鼠标当前的屏幕像素坐标
            Vector2 mousePos = Input.mousePosition;
            Vector2 localPoint;

            // 核心魔法：将屏幕像素坐标转换成 UI 父节点下的局部坐标
            // 必须传入 Canvas 使用的渲染摄像机 (worldCamera)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, 
                mousePos, 
                parentCanvas.worldCamera, 
                out localPoint);

            // 使用 anchoredPosition 赋值，并加上偏移量
            tooltipPanel.anchoredPosition = localPoint + offset;
        }
    }

    public void ShowTooltip(string key)
    {
        ChoiceAttribute attr = attributes.Find(a => a.tagKey == key);
        
        if (!string.IsNullOrEmpty(attr.tagKey))
        {
            titleText.text = attr.title;
            descriptionText.text = attr.description;
            tooltipPanel.gameObject.SetActive(true);
            
            // 雷达 3：检查是否成功匹配到了 Inspector 里配置的数据
            Debug.Log($"<color=cyan>【雷达3-面板已激活】成功匹配！即将显示标题: {attr.title}</color>");
        }
        else
        {
            Debug.LogWarning($"<color=red>【匹配失败】管理器中找不到名为 '{key}' 的词条！请检查 Inspector 中 Tag Key 的配置是否带了空格。</color>");
        }
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
        }
    }
}