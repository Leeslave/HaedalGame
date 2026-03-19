using System.Collections;
using UnityEngine;

public class CustomerBoostComponent : MonoBehaviour
{
    private bool canBoost;
    private bool checkBoost;
    [SerializeField] private float boostLoadingTime = 5f;


    public bool GetCheckBoost() { return checkBoost; }
    public void SetCheckBoost(bool flag) { checkBoost = flag; }

    void Start()
    {
        canBoost = false;
        checkBoost = false;
    }

    public void SetCanBoost(bool value)
    {
        canBoost = value;
    } 

    public void Boost(CustomerAgent cus)
    {
        if (!canBoost) { return; }
        canBoost = false;
        checkBoost = true;
        HallSystem.Instance.BoostCustomer(cus);
        
        StartCoroutine(BoostLoading());
    }

    private IEnumerator BoostLoading()
    {
        yield return new WaitForSeconds(boostLoadingTime);
        canBoost = true;
    }
}