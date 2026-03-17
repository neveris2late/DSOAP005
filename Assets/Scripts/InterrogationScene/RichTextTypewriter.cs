using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class RichTextTypewriter : MonoBehaviour
{
    [Header("打字机配置")]
    public float typingSpeed = 0.03f;
    [HideInInspector] 
    public float currentSpeedMultiplier = 1f;

    private TextMeshProUGUI tmpText;
    private Coroutine typingCoroutine;

    private AudioClip typingClip;
    private float currentPitch = 1f; 

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
    }

    public void PlayText(string textToType, bool forceInstant = false, AudioClip clip = null, float pitch = 1f)
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        tmpText.text = textToType;
        typingClip = clip;
        currentPitch = pitch; 

        if (forceInstant)
        {
            tmpText.ForceMeshUpdate();
            tmpText.maxVisibleCharacters = tmpText.textInfo.characterCount;
            IsPlaying = false;
        }
        else
        {
            typingCoroutine = StartCoroutine(TypeTextRoutine());
        }
    }

    private IEnumerator TypeTextRoutine()
    {
        IsPlaying = true;
        tmpText.ForceMeshUpdate(); 
        
        int totalCharacters = tmpText.textInfo.characterCount;
        tmpText.maxVisibleCharacters = 0;

        bool insideQuotes = false; 
        int charCountForAudio = 0; 

        for (int i = 0; i < totalCharacters; i++)
        {
            char c = tmpText.textInfo.characterInfo[i].character;

            // 维护是否在对话内的状态
            if (c == '“' || c == '「') insideQuotes = true;
            else if (c == '”' || c == '」') insideQuotes = false;
            else if (c == '"') insideQuotes = !insideQuotes; 

            tmpText.maxVisibleCharacters = i + 1;

            // 只在引号内（嫌疑人对白）或本身是引号时，产生打字机效果
            if (insideQuotes || c == '”' || c == '」' || c == '"' || c == '“' || c == '「')
            {
                if (typingClip != null && charCountForAudio % 3 == 0)
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayUISFX(typingClip, currentPitch);
                    }
                }
                charCountForAudio++;

                // --- 核心修改：动态计算这一帧的停顿时间 ---
                float currentWaitTime = typingSpeed * currentSpeedMultiplier;

                // 1. 逗号、顿号、分号 -> 短停顿 (大约 4 倍时间)
                if (c == '，' || c == ',' || c == '、' || c == '；' || c == ';')
                {
                    currentWaitTime *= 4f; 
                }
                // 2. 句号、感叹号、问号 -> 长停顿 (大约 12 倍时间，营造句子结束的呼吸感)
                else if (c == '。' || c == '！' || c == '!' || c == '？' || c == '?')
                {
                    currentWaitTime *= 12f; 
                }
                // 3. 省略号或英文句点 -> 连续顿挫 (大约 6 倍时间)
                // (注意：中文的……通常会被 TMPro 拆分成两个字元，英文的...是三个字元)
                else if (c == '…' || c == '.')
                {
                    currentWaitTime *= 6f; 
                }

                yield return new WaitForSeconds(currentWaitTime);
            }
        }

        IsPlaying = false;
    }
}