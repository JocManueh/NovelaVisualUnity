using UnityEngine;

namespace CaliNature
{
    public sealed class ZoneChapterController : MonoBehaviour
    {
        [SerializeField] private PlayerGridMovement player;
        [SerializeField] private ZoneEntryPoint entry;
        private void Start()
        {
            var session = GameSession.Instance;
            if (!session) { Debug.LogError("Inicia desde 01_MainMenu para cargar los sistemas de Cali ante la naturaleza."); return; }
            if (!player) foreach (var candidate in FindObjectsByType<PlayerGridMovement>()) if (candidate.enabled) { player = candidate; break; }
            if (!entry) entry = FindAnyObjectByType<ZoneEntryPoint>();
            if (!player || !entry) { session.UI.ShowLoadError("Falta Player o EntryPoints/MainEntry en esta zona."); return; }
            player.PlaceAt(entry.transform);
            session.UI.BindPlayer(player.GetComponent<PlayerInteraction>());
            session.Cinematic.Play(session.CurrentChapter);
        }
    }
}
