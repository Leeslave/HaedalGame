using UnityEngine;

public class CustomerSpawnManager : MonoBehaviour
{
    [SerializeField] private CustomerAgent customerPrefab;

    public void SpawnCustomer(float patience)
    {
        var customer = Instantiate(customerPrefab, transform.position, transform.rotation);
        customer.SpawnCustomer(patience);
    }

}
