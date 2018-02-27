using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lineMoveBot : MonoBehaviour 
{

	// Use this for initialization
    IEnumerator Start () 
    {
        gameObject.SetActive(true);

        yield return new WaitForSeconds(10);

        gameObject.SetActive(false);
	}
	
	// Update is called once per frame

}
