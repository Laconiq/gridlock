namespace AIWE.Interfaces
{
    public interface IInteractable
    {
        string GetPromptText();
        bool CanInteract();
        void Interact();
    }
}
