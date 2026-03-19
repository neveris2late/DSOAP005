using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic; // 引入泛型集合命名空间

[RequireComponent(typeof(TextMeshProUGUI))]
public class InteractableText : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
{
    private TextMeshProUGUI textMeshPro;
    public Color hoverColor = Color.yellow;
    private int currentLinkIndex = -1;

    // 用于缓存当前悬停关键词的原始顶点颜色快照
    private List<Color32> savedVertexColors = new List<Color32>();

    public GameObject flyingCharPrefab; 
    public Transform poolTargetTransform; 

    void Awake()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textMeshPro, Input.mousePosition, eventData.pressEventCamera);
        if (linkIndex != currentLinkIndex)
        {
            ResetLinkColor(); // 恢复上一个link的原始颜色
            currentLinkIndex = linkIndex;
            if (currentLinkIndex != -1) SetLinkColor(hoverColor); // 设置新link的高亮色并保存快照
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetLinkColor();
        currentLinkIndex = -1;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentLinkIndex != -1)
        {
            TMP_LinkInfo linkInfo = textMeshPro.textInfo.linkInfo[currentLinkIndex];
            string rawLinkID = linkInfo.GetLinkID(); // 获取原始LinkID，例如 "android_core|true"
            string clueText = linkInfo.GetLinkText();

            string clueID = rawLinkID;
            bool isGood = false; // 默认给 false

            // 使用 '|' 符号分割ID和布尔值
            if (rawLinkID.Contains("|"))
            {
                string[] parts = rawLinkID.Split('|');
                clueID = parts[0]; // 前半部分是线索ID
                if (parts.Length > 1)
                {
                    // 尝试将后半部分转换为布尔值 (true 或 false)
                    bool.TryParse(parts[1], out isGood);
                }
            }

            // 把解析出来的 isGood 传给协程
            StartCoroutine(SpawnAndFlyCharacters(linkInfo, clueID, clueText, isGood));
        }
    }

    private IEnumerator SpawnAndFlyCharacters(TMP_LinkInfo linkInfo, string clueID, string clueText, bool isGood)
    {
        float currentDelay = 0.01f; // 初始发射间隔非常短（产生“密”的效果）

        for (int i = 0; i < linkInfo.linkTextLength; i++)
        {
            int charIndex = linkInfo.linkTextfirstCharacterIndex + i;
            TMP_CharacterInfo charInfo = textMeshPro.textInfo.characterInfo[charIndex];

            Vector3 bottomLeft = textMeshPro.transform.TransformPoint(charInfo.bottomLeft);
            Vector3 topRight = textMeshPro.transform.TransformPoint(charInfo.topRight);
            Vector3 centerPos = (bottomLeft + topRight) / 2f;

            GameObject flyingChar = Instantiate(flyingCharPrefab, transform.root); 
            flyingChar.transform.position = centerPos;
            
            // 获取 TextMeshProUGUI 组件以便后续做透明度渐变
            TextMeshProUGUI charTMP = flyingChar.GetComponent<TextMeshProUGUI>();
            charTMP.text = charInfo.character.ToString();

            Sequence seq = DOTween.Sequence();
            
            // 1. 移除 DOMoveY 的上跳，直接计算飞行时间。
            // 越往后的字符，飞行总时长越长。这会让排在前面的字符飞得更快，在空中物理拉开距离（变疏）
            float flyDuration = 0.6f + (i * 0.03f); 

            // 2. 整齐飞去：直接 DOMove 到目标点。
            // 使用 Ease.InQuint（先慢后快），产生类似数据被加速吸入的效果
            seq.Append(flyingChar.transform.DOMove(poolTargetTransform.position, flyDuration).SetEase(Ease.InQuint));
            seq.Join(flyingChar.transform.DOScale(0.2f, flyDuration).SetEase(Ease.InQuad)); 
            
            // 3. 逐个消失：利用 DOFade 配合 Ease.InExpo，让字符在靠近终点时迅速变透明
            seq.Join(charTMP.DOFade(0f, flyDuration).SetEase(Ease.InExpo));

            seq.OnComplete(() => Destroy(flyingChar));

            // 4. 动态发射间隔：每次循环递增 Delay 时间。
            // 第一批字符几乎同时出发（密），后面的字符出发间隔越来越大（疏）
            yield return new WaitForSeconds(currentDelay); 
            currentDelay += 0.015f; 
        }

        // 等待所有字符的动画（包括拉长的间隔和飞行时间）基本完成，再将数据推入分析池
        yield return new WaitForSeconds(1.0f); 
        
        if (ScoreController.Instance != null)
        {
            // 将 isGood 传递给分析池
            ScoreController.Instance.AddClueToPool(clueID, clueText, isGood);
        }
    }
    
    // --- 核心修复逻辑：颜色设置与恢复 ---

    private void SetLinkColor(Color color)
    {
        if (currentLinkIndex == -1) return;
        TMP_LinkInfo linkInfo = textMeshPro.textInfo.linkInfo[currentLinkIndex];
        
        savedVertexColors.Clear(); // 清空之前的快照

        for (int i = 0; i < linkInfo.linkTextLength; i++)
        {
            int charIndex = linkInfo.linkTextfirstCharacterIndex + i;
            int meshIndex = textMeshPro.textInfo.characterInfo[charIndex].materialReferenceIndex;
            int vertexIndex = textMeshPro.textInfo.characterInfo[charIndex].vertexIndex;

            Color32[] vertexColors = textMeshPro.textInfo.meshInfo[meshIndex].colors32;
            if (vertexColors != null && vertexColors.Length > 0)
            {
                // 【关键步骤】：变色前，将该字符4个顶点的原始颜色保存到快照中
                savedVertexColors.Add(vertexColors[vertexIndex + 0]);
                savedVertexColors.Add(vertexColors[vertexIndex + 1]);
                savedVertexColors.Add(vertexColors[vertexIndex + 2]);
                savedVertexColors.Add(vertexColors[vertexIndex + 3]);

                // 赋予悬停高亮色
                vertexColors[vertexIndex + 0] = color;
                vertexColors[vertexIndex + 1] = color;
                vertexColors[vertexIndex + 2] = color;
                vertexColors[vertexIndex + 3] = color;
            }
        }
        textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void ResetLinkColor()
    {
        // 如果没有快照或者没有选中的link，直接返回
        if (currentLinkIndex == -1 || savedVertexColors.Count == 0) return;
        TMP_LinkInfo linkInfo = textMeshPro.textInfo.linkInfo[currentLinkIndex];
        
        int colorIndex = 0;
        for (int i = 0; i < linkInfo.linkTextLength; i++)
        {
            int charIndex = linkInfo.linkTextfirstCharacterIndex + i;
            int meshIndex = textMeshPro.textInfo.characterInfo[charIndex].materialReferenceIndex;
            int vertexIndex = textMeshPro.textInfo.characterInfo[charIndex].vertexIndex;

            Color32[] vertexColors = textMeshPro.textInfo.meshInfo[meshIndex].colors32;
            // 确保快照列表越界安全
            if (vertexColors != null && vertexColors.Length > 0 && colorIndex + 3 < savedVertexColors.Count)
            {
                // 【关键步骤】：从快照中按顺序读取颜色，原样还给顶点
                vertexColors[vertexIndex + 0] = savedVertexColors[colorIndex++];
                vertexColors[vertexIndex + 1] = savedVertexColors[colorIndex++];
                vertexColors[vertexIndex + 2] = savedVertexColors[colorIndex++];
                vertexColors[vertexIndex + 3] = savedVertexColors[colorIndex++];
            }
        }
        textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        savedVertexColors.Clear(); // 恢复完毕，清空快照
    }
}