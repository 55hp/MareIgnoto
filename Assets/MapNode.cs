using UnityEngine;
public class MapNode : MonoBehaviour
{
    public string name;
    public bool free = true;
    public Player onThisNodePlayer;

    public void HoldIt(Player p)
    {
        onThisNodePlayer = p;
        free = false;
    }

    public void FreeIt()
    {
        onThisNodePlayer = null;
        free = true;
    }
}
