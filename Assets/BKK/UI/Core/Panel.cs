using System;
using BKK.Extension;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(CanvasGroup))]
public class Panel : MonoBehaviour, IUserInterface
{
    [SerializeField] protected Button button_Close;
    [SerializeField] protected CanvasGroup canvasGroup;
    
    public UnityEvent onOpen = new UnityEvent();
    public UnityEvent onClose = new UnityEvent();

    private Transform buttonCloseTr;
    
    private const string buttonCloseName = "Button_Close";

    private bool isVisible = true;
    public bool IsVisible => isVisible;
    
    private bool isOpen = false;
    public bool IsOpen => isOpen;

    [SerializeField] protected bool closeToStart = true;

    public bool CloseToStart => closeToStart;

    [HideInInspector] public bool quitting = false;
    
    protected virtual void Awake()
    {
        FindReferences();
        
        Initialize();
    }

    protected virtual void Start()
    {
        if (closeToStart) Close();
    }

    protected virtual void OnEnable()
    {
        AddListeners();
    }

    protected virtual void OnDisable()
    {
        RemoveListeners();
    }

    private void Reset()
    {
        FindReferences();
    }

    protected virtual void OnDestroy()
    {
        
    }

    protected virtual void OnValidate()
    {
        // FindReferences();
    }
    
    private void OnApplicationQuit()
    {
        quitting = true;
    }
    
    public virtual void Initialize()
    {
        
    }
    
    public virtual void FindReferences()
    {
        button_Close = FindCloseButton();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public virtual void AddListeners()
    {
        if (button_Close) button_Close.onClick.AddListener(Close);
    }

    public virtual void RemoveListeners()
    {
        if (button_Close) button_Close.onClick.RemoveListener(Close);
    }

    protected virtual Button FindCloseButton()
    {
        buttonCloseTr = transform.FindRecursively(buttonCloseName);

        return buttonCloseTr ? buttonCloseTr.GetComponent<Button>() : null;
    }

    public virtual void Open()
    {
        Visible();
        
        gameObject.SetActive(true);
        
        onOpen?.Invoke();

        isOpen = true;
    }

    public virtual void Close()
    {
        Invisible();

        onClose?.Invoke();

        // gameObject.SetActive(false);

        isOpen = false;
    }

    public void Visible()
    {
        canvasGroup.alpha = 1;
        SetInteractable(true);
        isVisible = true;
    }

    public void Invisible()
    {
        canvasGroup.alpha = 0;
        SetInteractable(false);
        isVisible = false;
    }

    public void SetInteractable(bool enable)
    {
        canvasGroup.interactable = enable;
        canvasGroup.blocksRaycasts = enable;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(Panel), true)]
public class PanelEditor : Editor
{
    private Panel panel;
    
    public void OnEnable()
    {
        panel = target as Panel;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!panel.IsVisible)
        {
            if (GUILayout.Button("UI 켜기"))
            {
                panel.Visible();
            }
        }
        else
        {
            if (GUILayout.Button("UI 끄기"))
            {
                panel.Invisible();
            }
        }
    }
}

#endif


