using System;
using System.Collections;
using UnityEngine;

public class RosaDeiVenti : MonoBehaviour
{
    [SerializeField] Transform eye;
    [SerializeField][Range(0,1)] float rotationSpeed;


    [Header("Gestione Punte")]
    [SerializeField] Material baseMaterial;
    [SerializeField] Material selectedMaterial;


    [SerializeField] MeshRenderer[] punte;

    private Punta puntaSelezionata = 0;

    // Start is called before the first frame update
    void Start()
    {
        puntaSelezionata = 0;
        SelezionaPunta(puntaSelezionata);
    }

    #region Private Fields




    #endregion

    #region Public Fields

    public void Setup(Punta p, Material playerBaseMaterial = null)
    {
        if (playerBaseMaterial != null)
            baseMaterial = playerBaseMaterial;

        puntaSelezionata = p;
        SelezionaPunta(puntaSelezionata);
    }

    public void SelezionaPunta(Punta nuovaPunta )
    {
        punte[(int)puntaSelezionata].material = baseMaterial;
        puntaSelezionata = nuovaPunta;
        punte[(int)puntaSelezionata].material = selectedMaterial;
    }


    public void SetNextWind()
    {
        if ((int)puntaSelezionata == punte.Length - 1)
            SelezionaPunta(0);
        else
            SelezionaPunta( puntaSelezionata + 1);

    }

    public void SetPrevWind()
    {
        if ((int)puntaSelezionata == 0)
            SelezionaPunta((Punta)(punte.Length - 1));
        else
            SelezionaPunta(puntaSelezionata - 1);
    }

    #endregion
}

public enum Punta
{
    NORD = 0,
    NORD_EST = 1,
    EST = 2,
    SUD_EST = 3,
    SUD = 4,
    SUD_OVEST = 5,
    OVEST = 6,
    NORD_OVEST = 7
}
