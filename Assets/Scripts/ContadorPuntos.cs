using TMPro;
using UnityEngine;

public class ContadorPuntos : MonoBehaviour
{
    public int playerPoints;
    private TMP_Text textToKeepUpdated;

    void Awake() {
        Recolectable.OnHit += AddPointsAndUpdateText; // Violation of SRP!!!!!!!!!!!!!!!!!!!!!!!!!111!!!!111!! (jEFE DE EQUIPO vibes)
    }

    void Start() {
        playerPoints = 0;
        GameObject goTexto = GameObject.FindGameObjectWithTag("Puntuacion");

        if (goTexto == null) {
            Debug.LogError(name + ": no hay ningún objeto con el tag 'Puntuación'.");
            return;
        }

        textToKeepUpdated = goTexto.GetComponent<TMP_Text>();

        if (textToKeepUpdated == null) {
            Debug.LogError(name + ": el objeto con tag 'Puntuación' no tiene un componente TMP_Text asignado.");
        }
    }

    void AddPointsAndUpdateText(int points) {
        playerPoints += points;
        UpdateText();
    }

    void UpdateText() {
        textToKeepUpdated.text = "Gusanos: " + playerPoints;
    }
}


// 1vs1 Gabri VS Jefe de Equipo
// Tournament fight to the death: only one survivor
// Epic match: only for fighters and non-cowards. Jefe de Equipo has NOT connected = coward!!!1!!
// Invitation sent... no response from JdE...
// This event has no suscriber... Isa Sánches does NOT approve!!!!!!!
// Pe!r!!rrrrrrrrrooooooo Sanxeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee
// No estudiaremos para Luzma en enero!!!111!!!