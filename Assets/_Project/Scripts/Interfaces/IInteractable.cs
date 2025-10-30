public interface IInteractable
{
    string GetInteractText();
    void Interact();
    void OnEnterRange();
    void OnExitRange();
}