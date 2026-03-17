using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening; // 引入 DOTween 用于动效

public class MapLocationButtonController : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI locationNameText;
    public Image statusStampImage; // 用于显示“已逮捕/已死亡”的印章图标
    public Button nodeButton;
    public CanvasGroup canvasGroup;

    [Header("状态图标配置")]
    public Sprite stampCleared;
    public Sprite stampArrested;
    public Sprite stampDead;

    private string boundSuspectID;
    private Action<string> onClickCallback;

    public void Setup(RuntimeSuspect suspect, Action<string> onClick)
    {
        boundSuspectID = suspect.BaseData.suspectID;
        onClickCallback = onClick;

        if (locationNameText != null) 
            locationNameText.text = suspect.BaseData.locationName;

        // 清理旧的监听
        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(OnNodeClicked);

        // 根据状态更新 UI 表现
        UpdateStatusUI(suspect.currentStatus);
    }

    private void UpdateStatusUI(SuspectStatus status)
    {
        if (status == SuspectStatus.Pending)
        {
            nodeButton.interactable = true;
            if (statusStampImage != null) statusStampImage.gameObject.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }
        else
        {
            // 已经有结果了，失活按钮
            nodeButton.interactable = false;
            
            // 降低透明度，或者你可以把它变成黑黄配色的警告样式
            if (canvasGroup != null) canvasGroup.alpha = 0.6f;

            if (statusStampImage != null)
            {
                statusStampImage.gameObject.SetActive(true);
                switch (status)
                {
                    case SuspectStatus.Cleared: statusStampImage.sprite = stampCleared; break;
                    case SuspectStatus.Arrested: statusStampImage.sprite = stampArrested; break;
                    case SuspectStatus.Dead: statusStampImage.sprite = stampDead; break;
                }
                
                // 给印章加一个简单的 DOTween 盖章动画
                statusStampImage.transform.localScale = Vector3.one * 2f;
                statusStampImage.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBounce);
            }
        }
    }

    private void OnNodeClicked()
    {
        // 播放个小动效并触发回调
        transform.DOScale(0.95f, 0.1f).OnComplete(() => 
        {
            transform.DOScale(1f, 0.1f);
            onClickCallback?.Invoke(boundSuspectID);
        });
    }
}