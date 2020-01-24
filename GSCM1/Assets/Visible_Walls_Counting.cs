using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visible_Walls_Counting : MonoBehaviour
{
    
    private GameObject[] Buildings;
    private GameObject[] Roads;

    // Start is called before the first frame update
    void Start()
    {
        Buildings = GameObject.FindGameObjectsWithTag("Reflecting_Obstacles");
        Roads = GameObject.FindGameObjectsWithTag("Roads");


        Debug.Log("Number of buildings = " + Buildings.Length);
        Debug.Log("Number of roads = " + Roads.Length);



        int road_count = 0;

        foreach (GameObject road in Roads)
        {
            road_count += 1;
            Vector3[] road_vertices = road.GetComponent<MeshFilter>().mesh.vertices;


            //Debug.Log("Number of verteces in the road mesh = " + road_vertices.Length);

            Vector3 road_cg = new Vector3(0, 0, 0);
            Vector3 elev_cg = new Vector3(0, 1, 0);
            
            for (int k = 0; k < road_vertices.Length; k++)
            {
                road_cg = road_cg + road_vertices[k];
            }
            road_cg = road_cg / road_vertices.Length;

            //Debug.Log("Position of the road elm " + road_cg);
            GameObject road_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            road_sphere.transform.position = road_cg + elev_cg;
            

            foreach(GameObject building in Buildings)
            {
                Vector3[] vrtx = building.GetComponent<MeshFilter>().mesh.vertices;
                Vector3[] nrml = building.GetComponent<MeshFilter>().mesh.normals;

                foreach(Vector3 v in vrtx)
                {
                    if ()
                }
            }
        }

        
        //Debug.Log("Number of roads = "+ road_count);


        //int building_count = 0;
        //foreach (GameObject building in Buildings)
        //{
        //    building_count += 1;
        //    Vector3[] vertices = building.GetComponent<MeshFilter>().mesh.vertices;
        //    Vector3[] normals = building.GetComponent<MeshFilter>().mesh.normals;

        //    int[] triangles = building.GetComponent<MeshFilter>().mesh.triangles;
        //    if (building_count == 1)
        //    {
        //        Debug.Log("Number of triangles in the first building = " + triangles.Length / 3);

        //        Debug.Log(vertices[triangles[0]] + " with normal " + normals[triangles[0]]);
        //        GameObject first_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //        first_sphere.transform.position = vertices[triangles[0]];
        //        var sphereRenderer1 = first_sphere.GetComponent<Renderer>();
        //        sphereRenderer1.material.SetColor("_Color", Color.red);

        //        Debug.Log(vertices[triangles[1]] + " with normal " + normals[triangles[1]]);
        //        GameObject second_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //        second_sphere.transform.position = vertices[triangles[1]];
        //        var sphereRenderer2 = second_sphere.GetComponent<Renderer>();
        //        sphereRenderer2.material.SetColor("_Color", Color.yellow);

        //        Debug.Log(vertices[triangles[2]] + " with normal " + normals[triangles[2]]);
        //        GameObject third_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //        third_sphere.transform.position = vertices[triangles[2]];
        //        var sphereRenderer3 = third_sphere.GetComponent<Renderer>();
        //        sphereRenderer3.material.SetColor("_Color", Color.green);

        //        /*
        //        for(int k=0; k < triangles.Length; k = k + 3)
        //        {
        //            Debug.Log("Triangle #"+k/3+" consists of ("+triangles[k]+", "+triangles[k+1]+", "+triangles[k+2]+")");
        //        }
        //        */

        //    }
        //}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
