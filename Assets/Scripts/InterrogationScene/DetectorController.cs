using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class DetectorController : MonoBehaviour
{
    private Material _material;
    
    [Header("Bar Settings")]
    [Range(0f, 1f)]
    public float currentFill = 0.5f;
    public float animationDuration = 0.4f;
    
    // Shader 属性 ID 缓存
    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
    private static readonly int WarningLerpID = Shader.PropertyToID("_WarningLerp");

    private Tween _fillTween;
    private Tween _warningTween;

    void Awake()
    {
        Image img = GetComponent<Image>();
        _material = new Material(img.material);
        img.material = _material;
        
        _material.SetFloat(FillAmountID, currentFill);
        UpdateWarningState(currentFill, 0f);
    }

    void Update()
    {
        // 测试按键：W 增加 0.01，S 减少 0.01
        if (Input.GetKeyDown(KeyCode.W))
        {
            SetFillAmount(currentFill + 0.01f);
        }
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            SetFillAmount(currentFill - 0.01f);
        }
    }

    public void SetFillAmount(float targetValue)
    {
        // 限制在 0.0 到 1.0 之间
        targetValue = Mathf.Clamp01(targetValue);
        
        // 避免重复触发相同值的动画
        if (Mathf.Approximately(currentFill, targetValue)) return;
        
        currentFill = targetValue;

        // 平滑移动进度
        _fillTween?.Kill();
        _fillTween = _material.DOFloat(targetValue, FillAmountID, animationDuration).SetEase(Ease.OutCubic);

        // 判定警告状态
        UpdateWarningState(targetValue, animationDuration);
    }

    private void UpdateWarningState(float value, float duration)
    {
        // 核心逻辑：超过 70% (0.7f) 进入 Warning State (1.0f)，否则保持 Normal (0.0f)
        float targetWarningLerp = value > 0.7f ? 1.0f : 0.0f;

        _warningTween?.Kill();
        
        if (duration > 0f)
        {
            _warningTween = _material.DOFloat(targetWarningLerp, WarningLerpID, duration).SetEase(Ease.InOutSine);
        }
        else
        {
            _material.SetFloat(WarningLerpID, targetWarningLerp);
        }
    }
}