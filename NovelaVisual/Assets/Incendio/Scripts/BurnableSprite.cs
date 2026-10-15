using UnityEngine;

namespace CaliNature
{
    /// Cambia el sprite de un objeto del escenario según su estado de fuego:
    /// 0 = intacto, 1 = seco, 2 = quemado. Si el sprite "seco" es el mismo que el intacto, se aplica un tinte.
    [RequireComponent(typeof(SpriteRenderer))]
    public class BurnableSprite : MonoBehaviour
    {
        [SerializeField] Sprite[] sprites = new Sprite[3];   // [intacto, seco, quemado]
        [SerializeField] float[] scales = { 1f, 1f, 1f };     // factor de escala por estado
        [SerializeField] bool tintWhenDry;
        [SerializeField] Color dryTint = new Color(1f, 0.86f, 0.62f, 1f);

        SpriteRenderer sr;
        Vector3 baseScale;
        int current = -1;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
        }

        public void SetState(int state)
        {
            if (state == current) return;
            current = state;
            int i = Mathf.Clamp(state, 0, sprites.Length - 1);
            if (sprites[i] != null) sr.sprite = sprites[i];
            float s = (scales != null && i < scales.Length) ? scales[i] : 1f;
            transform.localScale = baseScale * s;
            bool sameSprite = sprites.Length > 1 && sprites[0] == sprites[1];
            sr.color = (state == 1 && tintWhenDry && sameSprite) ? dryTint : Color.white;
        }
    }
}
