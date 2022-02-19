using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class Test : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Hello()
    {
        CmdHello();
    }

    [Command]
    private void CmdHello()
    {
        Debug.Log("HELLO from the server");
    }
}
