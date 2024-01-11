using System.Collections;
using UnityEngine;

public class RosaDeiVenti : MonoBehaviour
{
    [SerializeField] Transform eye;
    [SerializeField][Range(0,1)] float rotationSpeed;

    private bool alive;

    // Start is called before the first frame update
    void Start()
    {
        alive = true;
        StartCoroutine(RandomMove());
    }

    public IEnumerator RandomMove()
    {
        while (alive)
        {
            float tempoPassato = 0f;
            float durataRotazione = Random.Range(2,6);
            while (tempoPassato < durataRotazione)
            {
                // Calcola l'interpolazione tra la rotazione corrente e la rotazione desiderata
                float angoloRotazione = Mathf.Lerp(0f, 90f, (tempoPassato / durataRotazione)* rotationSpeed);

                // Ruota l'oggetto attorno all'asse Y
                eye.rotation = Quaternion.Euler(angoloRotazione, 0f, 0f);

                // Aggiorna il tempo trascorso
                tempoPassato += Time.deltaTime;
            }



               float waitingTime = Random.Range(2, 10);
            yield return new WaitForSeconds(waitingTime);
        }
    }
}
