using UnityEngine;

namespace CaliNature
{
    // Sacudida de cámara aditiva para el nivel del terremoto.
    //
    // Escribe en PlayerCameraFollow.ShakeOffset, que se aplica DESPUÉS del
    // seguimiento. Por eso la sacudida no entra en el suavizado del follow: al
    // terminar, la cámara queda exactamente donde el seguimiento la dejó.
    //
    // Orden de ejecución alto para correr después de PlayerCameraFollow dentro
    // del mismo LateUpdate.
    [DefaultExecutionOrder(1000)]
    public sealed class CameraShake : MonoBehaviour
    {
        [SerializeField] private PlayerCameraFollow follow;
        [SerializeField, Min(0f)] private float amplitude = 0.15f;
        [SerializeField, Min(0.1f)] private float frequency = 8f;

        private float duration = 1f;
        private float remaining;
        private bool active;

        private void Awake() { if (!follow) follow = GetComponent<PlayerCameraFollow>(); }

        // Arranca la sacudida. `seconds` = duración, `amp` = amplitud en unidades,
        // `hz` = frecuencia en oscilaciones por segundo.
        public void Begin(float seconds, float amp, float hz)
        {
            if (amp > 0f) amplitude = amp;
            if (hz > 0f) frequency = hz;
            duration = Mathf.Max(0.01f, seconds);
            remaining = duration;
            active = true;
        }

        public void Stop()
        {
            active = false;
            remaining = 0f;
            if (follow) follow.ShakeOffset = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (!active) return;
            if (!follow) { Stop(); return; }
            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f) { Stop(); return; }

            float t = (duration - remaining) * frequency;
            // Dos senos desfasados por eje: se lee como un temblor, no como un vaivén.
            // Pequeño desvanecido al final para que el retorno no sea brusco.
            float fade = Mathf.Clamp01(remaining / Mathf.Min(0.6f, duration));
            follow.ShakeOffset = new Vector2(Mathf.Sin(t), Mathf.Sin(t * 1.37f + 1.1f)) * amplitude * fade;
        }

        private void OnDisable() { Stop(); }
    }
}
