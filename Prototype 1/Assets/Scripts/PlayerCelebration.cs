﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCelebration : MonoBehaviour
{
    [SerializeField]
    private Rigidbody car;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Celebrate()
    {
        print(gameObject.name + " Celebrates!");
        car.isKinematic = true;
        car.GetComponent<MeshCollider>().enabled = false;
        car.GetComponent<PlayerCarController>().enabled = false;
        car.transform.eulerAngles = Vector3.forward;
        GetComponent<Animator>().enabled = true;
    }
}
