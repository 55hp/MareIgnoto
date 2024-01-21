using UnityEngine;

public class HandController : MonoBehaviour
{
    [SerializeField] UICard cardPrefab;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            UICard nc = Instantiate(cardPrefab,this.transform);
            nc.Setup(AssetDatabaseManager.Instance.GetRandomCard());
        }
    }
}
