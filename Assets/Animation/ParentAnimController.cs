using System.Collections.Generic;
using UnityEngine;

public class ParentAnimController : MonoBehaviour
{
    [Header("瀑布流设置")]
    [Tooltip("每个子元素动画触发的错开时间")]
    public float elementStaggerDelay = 0.05f;
    
    [Tooltip("退场时是否倒序播放（最后一个先消失）")]
    public bool reverseOnExit = true;

    [Header("子元素列表")]
    [Tooltip("按希望出场的顺序拖入子节点的 ElementAnimController")]
    public List<ElementAnimController> elements;

    public void PlayEnter()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] != null)
            {
                elements[i].PlayEnter(i * elementStaggerDelay);
            }
        }
    }

    public void PlayExit()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] != null)
            {
                // 如果勾选了倒序，计算对应的索引
                int index = reverseOnExit ? elements.Count - 1 - i : i;
                elements[index].PlayExit(i * elementStaggerDelay);
            }
        }
    }
}