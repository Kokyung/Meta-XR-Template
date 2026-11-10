using UnityEngine;

public abstract class LayoutItem : MonoBehaviour, IUserInterface
{
    protected Panel parentPanel;
    
    public int index = 0;
    
    public virtual void Register(Panel panel, int index)
    {
        this.index = index;
        parentPanel = panel;
    }
    
    public virtual void Unregister(Panel panel)
    {
        if (parentPanel == panel) parentPanel = null;
    }
    
    public virtual void FindReferences() { }

    public virtual void Initialize() { }

    public virtual void AddListeners() { }

    public virtual void RemoveListeners() { }

    public virtual void Open() { }

    public virtual void Close() { }

    protected virtual void OnValidate()
    {
        FindReferences();
    }
    
    protected virtual void Awake()
    {
        FindReferences();
        
        Initialize();
    }

    protected virtual void OnEnable()
    {
        AddListeners();
    }
    
    protected virtual void Start()
    {
        
    }

    protected void OnDisable()
    {
        RemoveListeners();
    }
}
