using UnityEngine;

public class WindRoseController : MonoBehaviour
{
    [SerializeField] RosaDeiVenti windRose;

    public void On_leftButtonReleased()
    {
        windRose.SetPrevWind();
        this.gameObject.SetActive(false);
    }

    public void On_rightButtonReleased()
    {
        windRose.SetNextWind();
        this.gameObject.SetActive(false);
    }
}
