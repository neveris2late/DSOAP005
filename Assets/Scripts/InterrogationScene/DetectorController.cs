using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public enum DetectorState
{
    None,
    Uncalibrated,
    Calm,
    ColorChanged,
    Nervous
}

[RequireComponent(typeof(Image))]
public class DetectorController : MonoBehaviour
{
    private Material _material;
    
    [Header("State Settings")]
    public DetectorState currentState = DetectorState.None;

    [Header("Bar Settings")]
    [Range(0f, 1f)]
    public float currentFill = 0.5f;

    [Header("Stress & Elasticity Settings (压力与弹性系数)")]
    [Tooltip("弹力追踪系数：值越小，追赶标签目标的速度越快；值越大，越有粘滞感和缓慢的平滑感")]
    public float elasticSmoothTime = 1.5f; 
    
    [Tooltip("闲置衰减时间：多少秒没有收到新标签后，压力开始回落")]
    public float decayDelay = 5.0f;
    [Tooltip("衰减目标底线：衰减后试图回落到的压力值 (0.25即25%)")]
    public float baselineStress = 0.25f;
    [Tooltip("衰减速率：回落时的平缓程度，值越大回落越慢")]
    public float decaySmoothTime = 3.0f;

    [Header("Dynamic Breathing Settings (已校准状态的动态呼吸)")]
    [Tooltip("低压力时的最小呼吸幅度")]
    public float minBreathingAmplitude = 0.015f;
    [Tooltip("高压力时的最大呼吸幅度（抖动更剧烈）")]
    public float maxBreathingAmplitude = 0.08f;
    
    [Tooltip("低压力时的呼吸频率（次/秒），值越小越平缓")]
    public float slowestBreathFrequency = 0.4f;
    [Tooltip("高压力时的呼吸频率（次/秒），值越大越急促")]
    public float fastestBreathFrequency = 1.5f;

    [Header("Open Animation Settings")]
    public float openFillDuration = 2.0f;
    
    [Header("Uncalibrated Settings")]
    public float minUncalibratedSpeed = 0.05f;
    public float maxUncalibratedSpeed = 0.15f;

    [Header("UI & Text Feedback")]
    public Button testCalibrateBtn;
    public TextMeshProUGUI statusTextMesh;
    public GlitchTypeWriterEffect glitchTextEffect;
    public RectTransform entireContainerRect; // 【新增这一行】：检测器总体的父节点
    
    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");

    private Tween _uncalibratedTween;
    private Tween _openTween;
    private CanvasGroup _canvasGroup;
    
    // 物理系统核心变量
    private float _targetStress = 0f;       // Ink标签设定的目标压力
    private float _currentStress = 0f;      // 当前真实的压力基值（正在平滑追赶_targetStress）
    private float _stressVelocity = 0f;     // SmoothDamp用的速度缓存
    
    // 衰减计时器
    private float _idleTimer = 0f;
    
    // 呼吸相位
    private float _breathPhase = 0f;

    void Awake()
    {
        Image img = GetComponent<Image>();
        _material = new Material(img.material);
        img.material = _material;
        
        _canvasGroup = GetComponent<CanvasGroup>();
        
        _currentStress = currentFill;
        _targetStress = currentFill;
        _material.SetFloat(FillAmountID, currentFill);
    }

    void Start()
    {
        if (testCalibrateBtn != null)
        {
            testCalibrateBtn.onClick.AddListener(OnTestCalibrateClicked);
            testCalibrateBtn.gameObject.SetActive(false); 
        }

        if (statusTextMesh != null) statusTextMesh.text = "";
    }

    void Update()
    {
        // 仅在已校准状态下，运行物理压力计算
        if (currentState == DetectorState.Calm || currentState == DetectorState.ColorChanged || currentState == DetectorState.Nervous)
        {
            CalculatePhysicsAndBreathing();
        }
        
        // 测试按键（模拟接收到 Ink 标签）
        if (Input.GetKeyDown(KeyCode.W)) SetFillAmount(_targetStress + 0.2f);
        if (Input.GetKeyDown(KeyCode.S)) SetFillAmount(_targetStress - 0.2f);
    }

    /// <summary>
    /// 每帧执行：计算压力追踪、5秒衰减机制以及正弦波呼吸
    /// </summary>
    private void CalculatePhysicsAndBreathing()
    {
        // 1. 更新闲置计时器与衰减机制
        _idleTimer += Time.deltaTime;
        if (_idleTimer >= decayDelay)
        {
            // 超过5秒未收到标签，目标压力开始向 baselineStress (25%) 滑落
            _targetStress = Mathf.SmoothDamp(_targetStress, baselineStress, ref _stressVelocity, decaySmoothTime);
        }

        // 2. 当前压力平滑追赶目标压力 (核心缓冲系统，消除生硬跳变)
        _currentStress = Mathf.SmoothDamp(_currentStress, _targetStress, ref _stressVelocity, elasticSmoothTime);

        // 3. 基于当前压力，动态映射出当下的呼吸幅度和频率
        float currentAmplitude = Mathf.Lerp(minBreathingAmplitude, maxBreathingAmplitude, _currentStress);
        float currentFreq = Mathf.Lerp(slowestBreathFrequency, fastestBreathFrequency, _currentStress);

        // 4. 计算正弦波呼吸偏移
        _breathPhase += Time.deltaTime * currentFreq * Mathf.PI * 2;
        float breathOffset = Mathf.Sin(_breathPhase) * currentAmplitude;

        // 5. 最终合成并应用到材质
        currentFill = Mathf.Clamp01(_currentStress + breathOffset);
        _material.SetFloat(FillAmountID, currentFill);
    }

    // ==========================================
    // UI Text 更新方法 (修复的缺失部分)
    // ==========================================
    private void UpdateStatusText(string message)
    {
        if (statusTextMesh != null)
        {
            statusTextMesh.text = message;
            if (glitchTextEffect != null)
            {
                glitchTextEffect.PlayLineByLineGlitch(0f); 
            }
        }
    }

    // ==========================================
    // 供外部调用的控制方法 (Ink入口)
    // ==========================================
    public void HideCalibrateButton()
    {
        if (testCalibrateBtn != null) testCalibrateBtn.gameObject.SetActive(false);
    }

    public void SetFillAmount(float targetValue)
    {
        // 如果是未校准的剧烈跳动状态，不走平滑物理系统
        if (currentState == DetectorState.Uncalibrated) 
        {
            _targetStress = Mathf.Clamp01(targetValue);
            return; 
        }

        // 收到新标签：重置闲置计时器，并赋予新的目标压力！
        _idleTimer = 0f;
        _targetStress = Mathf.Clamp01(targetValue);
        
        Debug.Log($"[Detector] 收到新压力值 {_targetStress}，计时器清零。检测条开始平滑推拉...");
    }

    private void OnTestCalibrateClicked()
    {
        if (currentState == DetectorState.Uncalibrated)
        {
            if (testCalibrateBtn != null) testCalibrateBtn.gameObject.SetActive(false);
            SetState(DetectorState.Calm);
            SetFillAmount(0.15f); // 初始校准后赋予一个低压力值
        }
    }

    // ==========================================
    // 状态机控制
    // ==========================================
    public void SetState(DetectorState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"<color=#00FFFF>[DetectorController] 状态切换为: {newState}</color>");

        switch (currentState)
        {
            case DetectorState.Uncalibrated:
                UpdateStatusText("> SYS_WARNING: SIGNAL UNCALIBRATED. AWAITING MANUAL SYNC...");
                if (testCalibrateBtn != null) testCalibrateBtn.gameObject.SetActive(true); 
                PlayUncalibratedFluctuation();
                break;
                
            case DetectorState.Calm:
            case DetectorState.ColorChanged: 
            case DetectorState.Nervous:
                // 进入已校准状态时，清除 DOTween 动画，把控制权交给 Update 里的物理系统
                _uncalibratedTween?.Kill();
                _openTween?.Kill();
                UpdateStatusText(currentState == DetectorState.Calm ? "> SYNC COMPLETE. BASELINE ESTABLISHED." : "> WARNING: ABNORMAL SIGNAL DETECTED.");
                break;
        }
    }

    // ==========================================
    // 演出动画：入场与未校准
    // ==========================================
    public void PlayOpenAnimation()
    {
        currentFill = 0f;
        _targetStress = 0f;
        _currentStress = 0f;
        _material.SetFloat(FillAmountID, 0f);

        UpdateStatusText("> SYSTEM INITIALIZING...");
        if (testCalibrateBtn != null) testCalibrateBtn.gameObject.SetActive(false);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, openFillDuration * 0.5f).SetEase(Ease.InQuad);
        }

        _openTween?.Kill();
        _uncalibratedTween?.Kill();
        
        _openTween = _material.DOFloat(1f, FillAmountID, openFillDuration)
            .SetEase(Ease.InOutSine) 
            .OnUpdate(() => currentFill = _material.GetFloat(FillAmountID))
            .OnComplete(() => 
            {
                currentFill = 1f;
                _targetStress = 1f;
                _currentStress = 1f;
                SetState(DetectorState.Uncalibrated);
                
                if (SuperHintController.Instance != null)
                {
                    RectTransform targetRect = entireContainerRect != null ? entireContainerRect : GetComponent<RectTransform>();
                    SuperHintController.Instance.ShowSuperHint(targetRect, "人格指征拟合检测仪已开启，它将对被检测人的瞳孔、脑电波、微表情与心率进行扫描，当出现与人类指征不吻合的情形，指数将增长");
                }
            });
    }

    private void PlayUncalibratedFluctuation()
    {
        if (currentState != DetectorState.Uncalibrated) return;

        float randomFill = Random.Range(0f, 1f);
        float randomDuration = Random.Range(minUncalibratedSpeed, maxUncalibratedSpeed);

        _uncalibratedTween?.Kill();
        _uncalibratedTween = _material.DOFloat(randomFill, FillAmountID, randomDuration)
            .SetEase(Ease.Linear) 
            .OnUpdate(() => currentFill = _material.GetFloat(FillAmountID))
            .OnComplete(PlayUncalibratedFluctuation); 
    }
}