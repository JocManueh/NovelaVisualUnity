using UnityEngine;

namespace CaliNature
{
    public sealed class ZoneEntryPoint : MonoBehaviour
    {
        [SerializeField] private string entryId = "main";
        public string EntryId => entryId;
        private void OnDrawGizmos() { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, 0.3f); Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 0.7f); }
    }
}
