using UnityEngine;

namespace CaliNature
{
    public sealed class ChapterCinematicController : MonoBehaviour
    {
        private ChapterData chapter;
        private int index;
        private float elapsed;
        private bool active;
        public void Play(ChapterData data)
        {
            chapter = data; index = 0; elapsed = 0;
            if (data?.cinematic == null || data.cinematic.Length == 0) { GameSession.Instance.State.SetMode(GameMode.Exploration); return; }
            active = true; GameSession.Instance.State.SetMode(GameMode.Cinematic); ShowCard();
        }
        private void ShowCard()
        {
            var session = GameSession.Instance;
            if (index >= chapter.cinematic.Length)
            {
                active = false; session.Narration.Stop(); session.State.SetMode(GameMode.Exploration);
                if (!session.Save.Data.tutorialSeen) session.UI.OpenHelp(true);
                return;
            }
            var card = chapter.cinematic[index];
            session.UI.ShowCinematic(card);
            session.Narration.Play(chapter.id + "/cinematic/" + index, card.voiceResource);
        }
        private void Update()
        {
            var session = GameSession.Instance;
            if (!active || !session || session.State.Mode != GameMode.Cinematic) return;
            elapsed += Time.unscaledDeltaTime;
            float duration = Mathf.Max(5, chapter.cinematic[index].seconds);
            session.UI.AnimateCinematic(Mathf.Clamp01(elapsed / duration));
            if (elapsed >= duration && !session.Narration.Playing) { elapsed = 0; index++; ShowCard(); }
        }
        public void Skip() { if (!active) return; active = false; GameSession.Instance.Narration.Stop(); GameSession.Instance.State.SetMode(GameMode.Exploration); }
    }
}
