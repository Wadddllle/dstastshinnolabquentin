public abstract class BaseState
{
    protected AppManager Manager => AppManager.Instance; // Shortcut to access Brain

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
}