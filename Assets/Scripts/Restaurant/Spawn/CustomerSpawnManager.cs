using UnityEngine;

public class CustomerSpawnManager : MonoBehaviour
{
    [SerializeField] private CustomerAgent customerPrefab;

    public void SpawnCustomer(float patience, Transform parent)
    {
        var customer = Instantiate(customerPrefab, transform.position, transform.rotation, parent);
        customer.SpawnCustomer(patience);
    }

}
