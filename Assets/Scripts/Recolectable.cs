using UnityEngine;

public class Recolectable : MonoBehaviour
{
    public int pointsToGive = 0;
    
    public delegate void Message(int points);
    public static event Message OnHit;

    void OnTriggerEnter(Collider collider) {
        if (collider.gameObject.CompareTag("Player")) {
            if (OnHit != null) {
                OnHit(pointsToGive);
            }
        }
    }
}

// Jefe de Equipo is very bad...