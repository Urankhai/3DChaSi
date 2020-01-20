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

        int building_count = 0;
        foreach(GameObject building in Buildings)
        {
            building_count += 1;
            Vector3[] vertices = building.GetComponent<MeshFilter>().mesh.vertices;
            int[] triangles = building.GetComponent<MeshFilter>().mesh.triangles;
            if (building_count == 1)
            {
                Debug.Log("Number of triangles in the first building = " + triangles);
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
