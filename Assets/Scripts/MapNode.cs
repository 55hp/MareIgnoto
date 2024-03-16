using UnityEngine;
public class MapNode : MonoBehaviour
{
    public string name;
    public bool free = true;
    public Player onThisNodePlayer;
    public MeshRenderer nodeRenderer;
    public GameObject tile;

    public void HoldIt(Player p)
    {
        onThisNodePlayer = p;
        free = false;

        //Magari cambiamo colore o addirittura nascondiamo il frame intorno
        nodeRenderer.enabled = false;
        tile.gameObject.SetActive(false);
    }

    public void FreeIt()
    {
        onThisNodePlayer = null;
        free = true;

        //Ovviamente revertiamo il cambio fatto in hold it
        nodeRenderer.enabled = true;
        tile.gameObject.SetActive(true);
    }
}
