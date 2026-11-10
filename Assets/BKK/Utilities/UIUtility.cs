using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIUtility
{
    public static readonly string guideText = "안내";
    public static readonly string alertText = "경고!";

    public static bool IsInputNullOrSame(this TMP_InputField inputField, string input, string before)
    {
        if (string.IsNullOrEmpty(input))
        {
            inputField.text = before;
            return true;
        }

        if (before.Trim() == input.Trim())
        {
            return true;
        }

        return false;
    }

    public static bool IsPointerOverUI(int layer = 5)
    {
        // 프로젝트가 Input System 전용이므로 기존 Input 클래스 대신 Mouse를 사용한다. 마우스가 없으면 false.
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return false;

        var raycastResult = GetEventSystemRaycastResults(mouse.position.ReadValue());
        return IsPointerOverUI(raycastResult, layer);
    }

    public static bool IsPointerOverUI(IReadOnlyList<RaycastResult> eventSystemRaycastResults,
        int layer = 5)
    {
        foreach (var curRaycastResult in eventSystemRaycastResults)
        {
            if (curRaycastResult.gameObject.layer == layer) return true;
        }

        return false;
    }

    public static bool IsPointerOverUI(this Vector2 point, GraphicRaycaster graphicRaycaster)
    {
        if (EventSystem.current == null) return false;

        var uiExists = false;
        var eventData = new PointerEventData(EventSystem.current);
        var results = new List<RaycastResult>();

        eventData.position = point;

        graphicRaycaster.Raycast(eventData, results);

        uiExists = results.Count > 0;

        return uiExists;
    }

    public static List<RaycastResult> GetEventSystemRaycastResults(Vector2 pos)
    {
        var raycastResults = new List<RaycastResult>();
        if (EventSystem.current == null) return raycastResults;

        var eventData = new PointerEventData(EventSystem.current);

        eventData.position = pos;
        EventSystem.current.RaycastAll(eventData, raycastResults);
        return raycastResults;
    }

    #region Select

    public static void SelectDown<T>() where T : Selectable
    {
        SelectDown<T>(GetCurrentSelectable());
    }

    public static void SelectUp<T>() where T : Selectable
    {
        SelectUp<T>(GetCurrentSelectable());
    }

    public static void SelectDown<T1, T2>()
        where T1 : Selectable
        where T2 : Selectable
    {
        SelectDown<T1, T2>(GetCurrentSelectable());
    }

    public static void SelectUp<T1, T2>()
        where T1 : Selectable
        where T2 : Selectable
    {
        SelectUp<T1, T2>(GetCurrentSelectable());
    }

    public static void SelectDown<T1, T2, T3>()
        where T1 : Selectable
        where T2 : Selectable
        where T3 : Selectable
    {
        SelectDown<T1, T2, T3>(GetCurrentSelectable());
    }

    public static void SelectUp<T1, T2, T3>()
        where T1 : Selectable
        where T2 : Selectable
        where T3 : Selectable
    {
        SelectUp<T1, T2, T3>(GetCurrentSelectable());
    }

    public static void SelectDown<T>(Selectable selectable) where T : Selectable
    {
        SelectNext(selectable, true, next => next is T);
    }

    public static void SelectUp<T>(Selectable selectable) where T : Selectable
    {
        SelectNext(selectable, false, next => next is T);
    }

    public static void SelectDown<T1, T2>(Selectable selectable)
        where T1 : Selectable
        where T2 : Selectable
    {
        SelectNext(selectable, true, next => next is T1 || next is T2);
    }

    public static void SelectUp<T1, T2>(Selectable selectable)
        where T1 : Selectable
        where T2 : Selectable
    {
        SelectNext(selectable, false, next => next is T1 || next is T2);
    }

    public static void SelectDown<T1, T2, T3>(Selectable selectable)
        where T1 : Selectable
        where T2 : Selectable
        where T3 : Selectable
    {
        SelectNext(selectable, true, next => next is T1 || next is T2 || next is T3);
    }

    public static void SelectUp<T1, T2, T3>(Selectable selectable)
        where T1 : Selectable
        where T2 : Selectable
        where T3 : Selectable
    {
        SelectNext(selectable, false, next => next is T1 || next is T2 || next is T3);
    }

    private static Selectable GetCurrentSelectable()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null) return null;

        return eventSystem.currentSelectedGameObject.GetComponent<Selectable>();
    }

    private static void SelectNext(Selectable selectable, bool down, Func<Selectable, bool> isValid)
    {
        if (selectable == null) return;

        var next = down ? selectable.FindSelectableOnDown() : selectable.FindSelectableOnUp();

        if (next != null && isValid(next))
        {
            next.Select();
        }
    }

    #endregion

    #region Add Listener

    // 전달한 UnityAction을 그대로 등록하므로 같은 인스턴스로 RemoveListener할 수 있다.

    public static void AddButtonListener(this Button button, UnityAction onClick)
    {
        if (button != null && onClick != null)
        {
            button.onClick.AddListener(onClick);
        }
    }

    public static void AddToggleListener(this Toggle toggle, UnityAction<bool> onValueChanged)
    {
        if (toggle != null && onValueChanged != null)
        {
            toggle.onValueChanged.AddListener(onValueChanged);
        }
    }

    public static void AddInputFieldListener(this InputField inputField,
        UnityAction<string> onValueChanged = null, UnityAction<string> onEndEdit = null)
    {
        if (inputField != null)
        {
            if (onValueChanged != null) inputField.onValueChanged.AddListener(onValueChanged);
            if (onEndEdit != null) inputField.onEndEdit.AddListener(onEndEdit);
        }
    }

    public static void AddTMPInputFieldListener(this TMP_InputField inputField,
        UnityAction<string> onValueChanged = null, UnityAction<string> onSubmit = null,
        UnityAction<string> onEndEdit = null)
    {
        if (inputField != null)
        {
            if (onValueChanged != null) inputField.onValueChanged.AddListener(onValueChanged);
            if (onSubmit != null) inputField.onSubmit.AddListener(onSubmit);
            if (onEndEdit != null) inputField.onEndEdit.AddListener(onEndEdit);
        }
    }

    #endregion

    public static void SetTextWithoutNotify(this TMP_InputField inputField, string text, bool setPlaceholder)
    {
        inputField.SetTextWithoutNotify(text);

        // 알파를 0으로 바꾸면 다시 비었을 때 복구되지 않으므로, TMP와 같은 방식으로 표시 여부만 맞춘다.
        if (setPlaceholder && inputField.placeholder != null)
        {
            inputField.placeholder.enabled = string.IsNullOrEmpty(text);
        }
    }

    public static void ForceLayoutUpdate(this RectTransform rectTransform)
    {
        RectTransform[] rects = rectTransform.GetComponentsInChildren<RectTransform>();

        // 자식 레이아웃 크기가 부모에 반영되도록 역순(자식 먼저)으로 리빌드하고,
        // 레이아웃 컴포넌트가 없는 RectTransform은 리빌드하지 않는다.
        for (int i = rects.Length - 1; i >= 0; i--)
        {
            if (rects[i].TryGetComponent<ILayoutController>(out _))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rects[i]);
            }
        }
    }
}
