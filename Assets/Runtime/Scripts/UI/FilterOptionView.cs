using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 필터 카테고리와 세대 행. 포커스와 적용 표시를 자식으로 켠다.
/// </summary>
public class FilterOptionView : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private const string SELECT_NAME = "Select";
    private const string ON_NAME = "on";
    private const string OFF_NAME = "off";

    [SerializeField] private GameObject _select;
    [SerializeField] private GameObject _on;
    [SerializeField] private GameObject _off;

    private Action<FilterOptionView> _onFocus;
    private Action<FilterOptionView> _onConfirm;

    /// <summary>
    /// 포커스와 확정 콜백을 연결하고 표시를 끈다.
    /// </summary>
    public void Bind(Action<FilterOptionView> onFocus, Action<FilterOptionView> onConfirm)
    {
        CacheIndicators();
        DisableChildRaycasts();
        _onFocus = onFocus;
        _onConfirm = onConfirm;
        SetFocused(false);
    }

    /// <summary>
    /// 포커스 표시를 켜거나 끈다.
    /// </summary>
    public void SetFocused(bool focused)
    {
        if (_select != null)
        {
            _select.SetActive(focused);
        }
    }

    /// <summary>
    /// 적용된 필터면 on, 아니면 off를 켠다.
    /// </summary>
    public void SetApplied(bool applied)
    {
        if (_on != null)
        {
            _on.SetActive(applied);
        }

        if (_off != null)
        {
            _off.SetActive(!applied);
        }
    }

    /// <summary>
    /// 포인터가 항목 위에 오면 포커스만 옮긴다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _onFocus?.Invoke(this);
    }

    /// <summary>
    /// 항목을 클릭하면 확정을 시도한다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        _onConfirm?.Invoke(this);
    }

    private void CacheIndicators()
    {
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;
            if (_select == null && child.name == SELECT_NAME)
            {
                _select = child;
            }
            else if (_on == null && child.name == ON_NAME)
            {
                _on = child;
            }
            else if (_off == null && child.name == OFF_NAME)
            {
                _off = child;
            }
        }

        DisableRaycast(_select);
        DisableRaycast(_on);
        DisableRaycast(_off);
    }

    private void DisableChildRaycasts()
    {
        var graphics = GetComponentsInChildren<Graphic>(true);
        for (var i = 0; i < graphics.Length; i++)
        {
            if (graphics[i].gameObject != gameObject)
            {
                graphics[i].raycastTarget = false;
            }
        }
    }

    private static void DisableRaycast(GameObject child)
    {
        if (child == null)
        {
            return;
        }

        var graphic = child.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = false;
        }
    }
}
