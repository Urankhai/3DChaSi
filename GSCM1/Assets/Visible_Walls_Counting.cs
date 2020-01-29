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

        var all_vrtx_list = new List<Vector3>();
        var all_nrml_list = new List<Vector3>();

        
        foreach (GameObject building in Buildings)
        {
            Vector3[] vrtx = building.GetComponent<MeshFilter>().mesh.vertices;
            Vector3[] nrml = building.GetComponent<MeshFilter>().mesh.normals;
            
            int vrtx_count = 0;
            foreach (Vector3 v in vrtx)
            {
                if (v.y == 0)
                {
                    /*GameObject v_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    v_sphere.transform.position = v;
                    var sphereRenderer = v_sphere.GetComponent<Renderer>();
                    sphereRenderer.material.SetColor("_Color", Color.red);*/

                    // adding all vertexes with zero elevation. Later on, this will be removed
                    all_vrtx_list.Add(v);
                    all_nrml_list.Add(nrml[vrtx_count]);
                }
                vrtx_count += 1;
            }
            
        }
        Debug.Log("The length of vertexes list = " + all_vrtx_list.Count);
        //Debug.Log(" The length of normals list = " + all_nrml_list.Count);


        int road_count = 0;

        int case_count = 0;

        foreach (GameObject road in Roads)
        {
            road_count += 1;
            if (road_count == 1)
            {
                Vector3[] road_vertices = road.GetComponent<MeshFilter>().mesh.vertices;

                Vector3 road_cg = new Vector3(0, 0, 0);
                Vector3 elev_cg = new Vector3(0, 1, 0);

                for (int k = 0; k < road_vertices.Length; k++)
                {
                    road_cg = road_cg + road_vertices[k];
                }
                road_cg = road_cg / road_vertices.Length;

                Vector3 elro_cg = road_cg + elev_cg;

                //Debug.Log("Position of the road elm " + road_cg);
                //GameObject road_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //road_sphere.transform.position = elro_cg;
                //road_sphere.gameObject.tag = "CG";

                for (int i = 0; i < all_nrml_list.Count; i++)
                {
                    // Vector that is pointing from the road center to a building vertex
                    Vector3 cg2vrtx = all_vrtx_list[i] - elro_cg;

                    //if ( Vector3.Dot(cg2vrtx, all_nrml_list[i]) < 0 )
                    //{
                        // Printing the directions
                        //Debug.Log("Normal = " + all_nrml_list[i] + "; cg2vrtx = " + cg2vrtx);

                        //Debug.DrawLine(all_vrtx_list[i], elro_cg, Color.magenta, 2.5f);
                        //Debug.Log("Vertex = " + all_vrtx_list[i] + "; road center = " + elro_cg);

                        /*RaycastHit hit;
                        if ( (Physics.Raycast(elro_cg, cg2vrtx.normalized, out hit, Mathf.Infinity)) )
                        {
                            if (hit.collider.tag == "CG")
                            {
                                case_count += 1;
                                GameObject v_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                v_sphere.transform.position = all_vrtx_list[i];
                                var sphereRenderer = v_sphere.GetComponent<Renderer>();
                                sphereRenderer.material.SetColor("_Color", Color.red);
                            }
                        }*/
                        RaycastHit hit;
                        if ( Physics.Linecast(elro_cg, all_vrtx_list[i] + all_nrml_list[i],out hit) )
                        {
                            //Debug.Log("Hit object name " + hit.collider.name );
                            //case_count += 1;
                        }
                        else
                        {
                            GameObject v_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            v_sphere.transform.position = all_vrtx_list[i];
                            var sphereRenderer = v_sphere.GetComponent<Renderer>();
                            sphereRenderer.material.SetColor("_Color", Color.red);
                            case_count += 1;
                        }
                            
                     




                        
                    //}
                }

            }
        }

        Debug.Log("Number of cases " + case_count);

        
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
