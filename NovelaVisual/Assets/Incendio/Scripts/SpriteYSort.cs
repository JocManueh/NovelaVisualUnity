using UnityEngine;

namespace CaliNature
{
    /// Ordena el dibujo por la coordenada Y de los pies (pivote abajo-centro):
    /// lo que está más abajo en pantalla se dibuja delante. isStatic=false para jugador y NPC.
    public class SpriteYSort : MonoBehaviour
    {
        [SerializeField] bool isStatic = true;
        [SerializeField] int orderOffset = 0;

        SpriteRenderer[] rs;
        int[] rel;

        void Awake()
        {
            rs = GetComponentsInChildren<SpriteRenderer>(true);
            rel = new int[rs.Length];
            int min = int.MaxValue;
            foreach (var r in rs) if (r.sortingOrder < min) min = r.sortingOrder;
            for (int i = 0; i < rs.Length; i++) rel[i] = rs[i].sortingOrder - min;
            Apply();
        }

        void LateUpdate() { if (!isStatic) Apply(); }

        void Apply()
        {
            int baseOrder = Mathf.RoundToInt(-transform.position.y * 100f) + orderOffset;
            for (int i = 0; i < rs.Length; i++) rs[i].sortingOrder = baseOrder + rel[i];
        }
    }
}
