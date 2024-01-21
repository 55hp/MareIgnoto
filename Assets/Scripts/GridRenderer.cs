using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridRenderer : MonoBehaviour
{
    public int numeroDiDivisioni = 10;
    [SerializeField] Vector3[] puntiGriglia;
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        DrawGrid();
    }
    private void OnGUI()
    {
        if(GUILayout.Button("Repaint Grid"))
        {

            DrawGrid();
        }
    }

    private void DrawGrid()
    {
        if (puntiGriglia == null || puntiGriglia.Length <1)
            return;

            lineRenderer.positionCount = (numeroDiDivisioni + 1) * 2;
            lineRenderer.SetPositions(puntiGriglia);
    }
}
