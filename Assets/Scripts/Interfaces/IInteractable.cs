namespace AIWE.Interfaces
{
    public interface IInteractable
    {
        string GetPromptText();
        bool CanInteract(ulong clientId);
        void Interact(ulong clientId);
    }
}
