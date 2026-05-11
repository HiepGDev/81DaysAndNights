using UnityEngine;

public interface IInteractable
{
    string GetInteractText(); // For the UI "Press E to something something"
    void Interact();
}