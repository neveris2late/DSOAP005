using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class GlitchTypeWriterEffect : MonoBehaviour
{
    private TextMeshProUGUI textMesh;

    [Header("打字机设置")]
    [Tooltip("乱序模式下，每个字符弹出的间隔时间")]
    public float charDelay = 0.03f;
    
    // 状态标记：供外部 Controller 查询当前是否正在播放
    public bool IsPlaying { get; private set; }

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    // ==========================================
    // 模式一：逐行瞬间显示 (读取当前文本，按视觉行显现)
    // ==========================================
    public void PlayLineByLine(float delayBetweenLines)
    {
        StopAllCoroutines();
        StartCoroutine(LineByLineRoutine(delayBetweenLines));
    }

    private IEnumerator LineByLineRoutine(float delayBetweenLines)
    {
        IsPlaying = true;
        PrepareText(out TMP_TextInfo textInfo);

        for (int i = 0; i < textInfo.lineCount; i++)
        {
            TMP_LineInfo lineInfo = textInfo.lineInfo[i];

            for (int j = lineInfo.firstCharacterIndex; j <= lineInfo.lastCharacterIndex; j++)
            {
                if (textInfo.characterInfo[j].isVisible) RevealCharacter(textInfo, j);
            }

            yield return WaitWithSkip(delayBetweenLines);
        }

        IsPlaying = false; 
    }

    // ==========================================
    // 模式二：逐行乱序显示 (读取当前文本，行内乱序打字)
    // ==========================================
    public void PlayLineByLineGlitch(float delayBetweenLines)
    {
        StopAllCoroutines();
        StartCoroutine(LineByLineGlitchRoutine(delayBetweenLines));
    }

    private IEnumerator LineByLineGlitchRoutine(float delayBetweenLines)
    {
        IsPlaying = true;
        PrepareText(out TMP_TextInfo textInfo);

        for (int i = 0; i < textInfo.lineCount; i++)
        {
            TMP_LineInfo lineInfo = textInfo.lineInfo[i];
            
            List<int> validCharIndices = new List<int>();
            for (int j = lineInfo.firstCharacterIndex; j <= lineInfo.lastCharacterIndex; j++)
            {
                if (textInfo.characterInfo[j].isVisible) validCharIndices.Add(j);
            }

            ShuffleList(validCharIndices);
            bool skipTyping = false; 

            foreach (int charIndex in validCharIndices)
            {
                RevealCharacter(textInfo, charIndex);

                if (!skipTyping)
                {
                    float t = 0;
                    while (t < charDelay)
                    {
                        if (Input.GetMouseButtonDown(0)) skipTyping = true; 
                        t += Time.deltaTime;
                        yield return null;
                    }
                }
            }

            yield return WaitWithSkip(delayBetweenLines);
        }

        IsPlaying = false;
    }

    // --- 内部辅助方法 ---

    // 预处理文本：强制刷新网格并隐藏所有字符
    private void PrepareText(out TMP_TextInfo textInfo)
    {
        // 不再重新拼接字符串，直接使用 TextMeshPro 框内现有的排版
        textMesh.ForceMeshUpdate();
        textInfo = textMesh.textInfo;
        HideAllCharacters(textInfo);
    }

    private IEnumerator WaitWithSkip(float waitTime)
    {
        float timer = 0f;
        yield return new WaitForSeconds(0.05f); 
        
        while (timer < waitTime - 0.05f)
        {
            if (Input.GetMouseButtonDown(0)) break; 
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void HideAllCharacters(TMP_TextInfo textInfo)
    {
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            SetCharacterAlpha(textInfo, i, 0);
        }
        textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void RevealCharacter(TMP_TextInfo textInfo, int charIndex)
    {
        SetCharacterAlpha(textInfo, charIndex, 255);
        textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void SetCharacterAlpha(TMP_TextInfo textInfo, int charIndex, byte alpha)
    {
        int materialIndex = textInfo.characterInfo[charIndex].materialReferenceIndex;
        int vertexIndex = textInfo.characterInfo[charIndex].vertexIndex;
        Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;
        vertexColors[vertexIndex + 0].a = alpha;
        vertexColors[vertexIndex + 1].a = alpha;
        vertexColors[vertexIndex + 2].a = alpha;
        vertexColors[vertexIndex + 3].a = alpha;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}