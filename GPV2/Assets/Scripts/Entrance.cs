using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;

public class Entrance: MonoBehaviour
{
    public bool isLocked = false;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 getCurrentPosition()
    {
        return transform.position;
    }
}
