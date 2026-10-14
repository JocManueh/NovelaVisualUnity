using System.Collections.Generic;
using UnityEngine;

namespace CaliNature
{
    /// Nivel de incendio 0..3 según las decisiones del capítulo:
    /// cada decisión irresponsable lo sube (+1), cada responsable lo baja (-1, mínimo 0).
    /// Los focos se encienden por nivel y los objetos cercanos pasan a seco o quemado; al bajar el nivel se revierte.
    public class FireStateController : MonoBehaviour
    {
        [SerializeField] string chapterId = "wind";
        [SerializeField] Transform[] foci;                     // Foco_Fuego_01..04 (contienen las partículas)
        [SerializeField] int[] activeFromLevel = { 1, 2, 3, 3 };
        [SerializeField] GameObject[] groundPatches;           // manchas de suelo quemado, una por foco
        [SerializeField] float[] dryRadius = { 0, 9, 13, 18 };
        [SerializeField] float[] burnRadius = { 0, 4, 7, 10 };
        [SerializeField] int debugForceLevel = -1;             // 0..3 para probar sin jugar el diálogo

        BurnableSprite[] burnables;
        int lastLevel = -1;

        void Start()
        {
            burnables = FindObjectsByType<BurnableSprite>(FindObjectsInactive.Include);
            Refresh(true);
            // El nivel depende de las decisiones guardadas: nos avisamos cuando el guardado cambie
            // (RecordDecision/ClearChapter -> GameSaveManager.Store() dispara Changed).
            if (GameSession.Instance != null && GameSession.Instance.Save != null)
                GameSession.Instance.Save.Changed += OnSaveChanged;
        }

        void OnDestroy()
        {
            if (GameSession.Instance != null && GameSession.Instance.Save != null)
                GameSession.Instance.Save.Changed -= OnSaveChanged;
        }

        void OnSaveChanged() { Refresh(false); }

        int ReadLevel()
        {
            if (debugForceLevel >= 0) return Mathf.Clamp(debugForceLevel, 0, 3);
            int level = 0;
            foreach (var phase in new[] { "a", "b", "c" })
            {
                bool? responsible = FindDecision(chapterId + "/" + phase);
                if (responsible == null) continue;
                level = responsible.Value ? Mathf.Max(0, level - 1) : Mathf.Min(3, level + 1);
            }
            return level;
        }

        // Devuelve DecisionRecord.responsible de la decisión con esa key, o null si no existe.
        bool? FindDecision(string key)
        {
            var session = GameSession.Instance;
            if (session == null || session.Save == null) return null;
            var record = session.Save.Data.decisions.Find(x => x.key == key);
            return record == null ? (bool?)null : record.responsible;
        }

        void Refresh(bool force)
        {
            int level = ReadLevel();
            if (!force && level == lastLevel) return;
            lastLevel = level;

            var active = new List<Vector2>();
            for (int i = 0; i < foci.Length; i++)
            {
                bool on = level >= activeFromLevel[i];
                if (foci[i]) foci[i].gameObject.SetActive(on);
                if (groundPatches != null && i < groundPatches.Length && groundPatches[i]) groundPatches[i].SetActive(on);
                if (on && foci[i]) active.Add(foci[i].position);
            }

            foreach (var b in burnables)
            {
                int state = 0;
                if (active.Count > 0)
                {
                    Vector2 p = b.transform.position;
                    float dmin = float.MaxValue;
                    foreach (var f in active) dmin = Mathf.Min(dmin, Vector2.Distance(p, f));
                    if (dmin < burnRadius[level]) state = 2;
                    else if (dmin < dryRadius[level]) state = 1;
                }
                b.SetState(state);
            }
        }
    }
}
