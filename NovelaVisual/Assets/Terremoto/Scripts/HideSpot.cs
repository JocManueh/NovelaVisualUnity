using UnityEngine;

namespace CaliNature
{
    // Punto para esconderse del Puzzle 1 del nivel del terremoto.
    //
    // Vive en el mundo, delante de un mueble (mesa, ventana o repisa), y solo se
    // activa mientras dura el puzzle: cuando está apagado, PlayerInteraction lo
    // ignora por completo. El jugador se acerca, lo mira y pulsa E; en ese
    // momento WorldInteractable.Interact() avisa al controlador, que decide si
    // es el escondite seguro o no.
    //
    // La posición de este GameObject es el sitio donde se para el jugador para
    // interactuar (y donde el controlador dibuja el anillo de resaltado).
    public sealed class HideSpot : WorldInteractable
    {
        [Tooltip("Nombre que se muestra sobre el anillo de resaltado.")]
        [SerializeField] private string label = "Lugar";

        [Tooltip("¿Es el escondite seguro? Solo uno debería estarlo.")]
        [SerializeField] private bool correct;

        [Tooltip("Explicación que se muestra si el jugador elige este punto.")]
        [TextArea, SerializeField] private string wrongReason = "";

        [Tooltip("Desplazamiento desde este punto hasta donde queda el personaje escondido.")]
        [SerializeField] private Vector2 hideOffset = new Vector2(0f, 0.3f);

        public string Label => label;
        public bool IsCorrect => correct;
        public string WrongReason => wrongReason;
        public Vector2 HidePosition => (Vector2)transform.position + hideOffset;

        private HidePlacePuzzle puzzle;

        // El controlador se engancha al activar el punto y se suelta al apagarlo.
        public void Bind(HidePlacePuzzle owner) { puzzle = owner; }

        public override void Interact()
        {
            if (puzzle != null) puzzle.OnSpotChosen(this);
        }
    }
}
