using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visible_Triangles_Extraction : MonoBehaviour
{
    // Start is called before the first frame update
    private GameObject[] Buildings;
    private GameObject[] Roads;
    void Start()
    {
        Buildings = GameObject.FindGameObjectsWithTag("Reflecting_Obstacles");
        Roads = GameObject.FindGameObjectsWithTag("Roads");

        Debug.Log("Number of buildings = " + Buildings.Length);
        Debug.Log("Number of roads = " + Roads.Length);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
