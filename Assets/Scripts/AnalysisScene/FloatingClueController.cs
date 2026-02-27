//职责：让线索在池中漂浮，管理线索prefab的开关功能

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class FloatingClue : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string clueID { get; private set; }
    public bool isGood { get; private set; }

    public TextMeshProUGUI clueNameText;
    public Button closeButton; // 右上角的X按钮
    public CanvasGroup canvasGroup; // 用于渐变消失

    public void Init(string id, string text, bool goodAttr)
    {
        clueID = id;
        clueNameText.text = text;
        isGood = goodAttr;
        closeButton.gameObject.SetActive(false); // 初始隐藏关闭按钮

        closeButton.onClick.AddListener(OnCloseClicked);
        StartFloating();
    }

    private void StartFloating()
    {
        // 随机在原位置附近做不规则漂浮
        transform.localPosition = new Vector3(Random.Range(-50f, 50f), Random.Range(-50f, 50f), 0);
        
        transform.DOLocalMove(transform.localPosition + new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), 0), Random.Range(2f, 3f))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        closeButton.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        closeButton.gameObject.SetActive(false);
    }

    private void OnCloseClicked()
    {
        // 停止动画，播放缩小消失效果后通知 Controller 移除
        transform.DOKill();
        canvasGroup.DOFade(0, 0.3f);
        transform.DOScale(0.5f, 0.3f).OnComplete(() => 
        {
            ScoreController.Instance.RemoveClue(this);
        });
    }
}