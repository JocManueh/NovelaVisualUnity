using UnityEngine;

namespace CaliNature
{
    public abstract class WorldInteractable : MonoBehaviour
    {
        [SerializeField] private string prompt = "Interactuar";
        [SerializeField, Min(0.2f)] private float range = 1.6f;
        public string Prompt => prompt;
        public float Range => range;
        public abstract void Interact();
    }
}
