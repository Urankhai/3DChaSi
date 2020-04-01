using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



public class Correcting_polygons : MonoBehaviour
{
    public float WW1; // 3m width of the spawning area for MPC1
    // setting materials
    public Material MPC1_mat;
    public Material MPC2_mat;
    public Material MPC3_mat;
    public Material ObsPoint_Material;
    public Material CG_Material;
    
    private GameObject[] Buildings;
    private GameObject[] Observation_Points;


    // Start is called before the first frame update
    void Start()
    {
        GameObject MPC_Spawner = GameObject.Find("MPC_spawner");
        Scatterers_Spawning2 MPC_Script = MPC_Spawner.GetComponent<Scatterers_Spawning2>();
        List<Vector3> MPC1 = MPC_Script.MPC1_possiblepositionList;
        List<Vector3> MPC2 = MPC_Script.MPC2_possiblepositionList;
        List<Vector3> MPC3 = MPC_Script.MPC3_possiblepositionList;

        /*
        Debug.Log("Number of MPC1 is " + MPC1.Count);
        for (int i = 0; i < MPC1.Count; i++)
        {
            GameObject cleared_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            cleared_sphere.transform.position = MPC1[i];
            Destroy(cleared_sphere.GetComponent<SphereCollider>()); // remove collider
            var sphereRenderer = cleared_sphere.GetComponent<Renderer>();
            sphereRenderer.material = MPC2_mat;
            cleared_sphere.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        }
        */


        // Declaring buildings and observation points
        Buildings = GameObject.FindGameObjectsWithTag("Reflecting_Obstacles");
        // The number of observation points should not be too big, a few is enough
        Observation_Points = GameObject.FindGameObjectsWithTag("Observation_Points");
        // Define the main road direction
        Vector3 main_axis = (Observation_Points[0].transform.position - Observation_Points[1].transform.position).normalized;
        Vector3 perp_axis = new Vector3(main_axis.z, 0, -main_axis.x);

        // subset of the seen Buldings
        List<GameObject> Building_list = new List<GameObject>();

        // Going through all observation points
        for (int k = 0; k < Observation_Points.Length; k++)
        {
            // analyzing which buidings are seen
            for (int b = 0; b < Buildings.Length; b++)
            {
                Vector3[] vrtx = Buildings[b].GetComponent<MeshFilter>().mesh.vertices;
                Vector3[] nrml = Buildings[b].GetComponent<MeshFilter>().mesh.normals;

                int seen_vrtx_count = 0;

                for (int v = 0; v < vrtx.Length; v++)
                {
                    if (vrtx[v].y == 0)
                    {
                        if (!Physics.Linecast(Observation_Points[k].transform.position, vrtx[v] + 1.1f * nrml[v]))
                        {
                            seen_vrtx_count += 1;
                        }
                    }
                }
                if (seen_vrtx_count != 0)
                {
                    if (!Building_list.Contains(Buildings[b]))
                    {
                        Building_list.Add(Buildings[b]);
                    }
                }
            }
        }

        // deactivating nonseen buildings
        for (int b = 0; b < Buildings.Length; b++)
        {
            if (!Building_list.Contains(Buildings[b]))
            {
                Buildings[b].SetActive(false);
            }

        }
        Debug.Log("The number of seen Buildings = " + Building_list.Count);

        List<V6> GlobalMPC1V6 = new List<V6>();
        List<Vector3> GlobalMPC1 = new List<Vector3>();

        List<V6> GlobalMPC2V6 = new List<V6>();
        List<Vector3> GlobalMPC2 = new List<Vector3>();

        List<V6> GlobalMPC3V6 = new List<V6>();
        List<Vector3> GlobalMPC3 = new List<Vector3>();

        // analyzing each seen building separately
        for (int k = 0; k < Building_list.Count; k++)
        {
            // writing MPCs into a txt file
            //string writePath;
            List<string> Obj_List = new List<string>();

            Vector3[] vrtx = Building_list[k].GetComponent<MeshFilter>().mesh.vertices;
            Vector3[] nrml = Building_list[k].GetComponent<MeshFilter>().mesh.normals;

            List<Vector3> floor_vrtx = new List<Vector3>();
            List<Vector3> floor_nrml = new List<Vector3>();
            var pairs = new List<V6>();

            List<Vector3> possible_vrtx = new List<Vector3>();
            List<Vector3> possible_nrml = new List<Vector3>();



            for (int l = 0; l < vrtx.Length; l++)
            {
                if (vrtx[l].y == 0 && Mathf.Abs(nrml[l].y) < 0.9)
                {
                    floor_vrtx.Add(vrtx[l]);
                    floor_nrml.Add(nrml[l]);

                    // adding coordinates and normals of the vertices into a single object V6 (like a N x 6 matrix)
                    V6 valid_pair = new V6(vrtx[l], nrml[l]);
                    pairs.Add(valid_pair);

                }
            }
            // finding the edges of each building
            List<Vector3> SortedByX = floor_vrtx.OrderBy(vertex => vertex.x).ToList();
            List<Vector3> SortedByZ = floor_vrtx.OrderBy(vertex => vertex.z).ToList();
            float x_l = SortedByX[0].x - WW1;                     // x left
            float x_r = SortedByX[SortedByX.Count - 1].x + WW1;   // x right
            float z_d = SortedByZ[0].z - WW1;                     // z down
            float z_u = SortedByZ[SortedByX.Count - 1].z + WW1;   // z up

            // searching which scatterers are located near the building
            NearbyElements(x_l, x_r, z_d, z_u, MPC1, out List<Vector3> NeabyMPC1);
            NearbyElements(x_l, x_r, z_d, z_u, MPC2, out List<Vector3> NeabyMPC2);
            NearbyElements(x_l, x_r, z_d, z_u, MPC3, out List<Vector3> NeabyMPC3);

            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Defining areas around building where scatterers can be located
            //////////////////////////////////////////////////////////////////////////////////////////////////////////



            // defining necessary elements for the polygon that connects the last and the first vertices
            Vector3 n1_0 = floor_nrml[floor_vrtx.Count - 1];
            Vector3 p1_0 = floor_vrtx[floor_vrtx.Count - 1];
            Vector3 s1_0 = floor_vrtx[floor_vrtx.Count - 1] + WW1 * floor_nrml[floor_vrtx.Count - 1];

            Vector3 n2_0 = floor_nrml[0];
            Vector3 p2_0 = floor_vrtx[0]; 
            Vector3 s2_0 = floor_vrtx[0] + WW1 * floor_nrml[0];
            
            Area34 area = new Area34(p1_0, s1_0, p2_0, s2_0);
            DrawArea(area);

            PickMPC(n1_0, p1_0, s1_0, n2_0, p2_0, s2_0, NeabyMPC1, out List<V6> MPC1_V6);
            PickMPC(n1_0, p1_0, s1_0, n2_0, p2_0, s2_0, NeabyMPC2, out List<V6> MPC2_V6);
            PickMPC(n1_0, p1_0, s1_0, n2_0, p2_0, s2_0, NeabyMPC3, out List<V6> MPC3_V6);

            for (int ii = 0; ii < MPC1_V6.Count; ii++)
            {
                if (!GlobalMPC1.Contains(MPC1_V6[ii].Coordinates))
                {
                    GlobalMPC1V6.Add(MPC1_V6[ii]);
                    GlobalMPC1.Add(MPC1_V6[ii].Coordinates);

                    GameObject cleared_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                    cleared_sphere.transform.position = MPC1_V6[ii].Coordinates;
                    Destroy(cleared_sphere.GetComponent<SphereCollider>()); // remove collider
                    var sphereRenderer = cleared_sphere.GetComponent<Renderer>();
                    sphereRenderer.material = MPC1_mat;
                    cleared_sphere.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                }
            }

            for (int ii = 0; ii < MPC2_V6.Count; ii++)
            {
                if (!GlobalMPC1.Contains(MPC2_V6[ii].Coordinates))
                {
                    GlobalMPC1V6.Add(MPC2_V6[ii]);
                    GlobalMPC1.Add(MPC2_V6[ii].Coordinates);

                    GameObject cleared_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                    cleared_sphere.transform.position = MPC2_V6[ii].Coordinates;
                    Destroy(cleared_sphere.GetComponent<SphereCollider>()); // remove collider
                    var sphereRenderer = cleared_sphere.GetComponent<Renderer>();
                    sphereRenderer.material = MPC2_mat;
                    cleared_sphere.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                }
            }

            for (int ii = 0; ii < MPC3_V6.Count; ii++)
            {
                if (!GlobalMPC1.Contains(MPC3_V6[ii].Coordinates))
                {
                    GlobalMPC1V6.Add(MPC3_V6[ii]);
                    GlobalMPC1.Add(MPC3_V6[ii].Coordinates);

                    GameObject cleared_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                    cleared_sphere.transform.position = MPC3_V6[ii].Coordinates;
                    Destroy(cleared_sphere.GetComponent<SphereCollider>()); // remove collider
                    var sphereRenderer = cleared_sphere.GetComponent<Renderer>();
                    sphereRenderer.material = MPC3_mat;
                    cleared_sphere.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                }
            }



            // draw areas for all vertices
            Vector3 point1 = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 point2 = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 shift1 = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 shift2 = new Vector3(0.0f, 0.0f, 0.0f);
            for (int l = 0; l < floor_vrtx.Count - 1; l++)
            {
                point1 = floor_vrtx[l];
                point2 = floor_vrtx[l + 1];
                
                shift1 = floor_vrtx[l] + WW1 * floor_nrml[l];
                shift2 = floor_vrtx[l+1] + WW1 * floor_nrml[l+1];

                PickMPC(floor_nrml[l], point1, shift1, floor_nrml[l + 1], point2, shift2, NeabyMPC1, out List<V6> for_MPC1_V6);
                PickMPC(floor_nrml[l], point1, shift1, floor_nrml[l + 1], point2, shift2, NeabyMPC2, out List<V6> for_MPC2_V6);
                PickMPC(floor_nrml[l], point1, shift1, floor_nrml[l + 1], point2, shift2, NeabyMPC3, out List<V6> for_MPC3_V6);

                for (int ii = 0; ii < for_MPC1_V6.Count; ii++)
                {
                    if (!GlobalMPC1.Contains(for_MPC1_V6[ii].Coordinates))
                    {
                        GlobalMPC1V6.Add(for_MPC1_V6[ii]);
                        GlobalMPC1.Add(for_MPC1_V6[ii].Coordinates);

                        GameObject cleared_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                        cleared_sphere.transform.position = for_MPC1_V6[ii].Coordinates;
                        Destroy(cleared_sphere.GetComponent<SphereCollider>()); // remove collider
                        var sphereRenderer = cleared_sphere.GetComponent<Renderer>();
                        sphereRenderer.material = MPC1_mat;
                        cleared_sphere.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                    }
                }

                for (int ii = 0; ii < for_MPC2_V6.Count; ii++)
                {
                    if (!GlobalMPC1.Contains(for_MPC2_V6[ii].Coordinates))
                    {
                        GlobalMPC1V6.Add(for_MPC2_V6[ii]);
                        GlobalMPC1.Add(for_MPC2_V6[ii].Coordinates);

                        GameObject cleared_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                        cleared_sphere.transform.position = for_MPC2_V6[ii].Coordinates;
                        Destroy(cleared_sphere.GetComponent<SphereCollider>()); // remove collider
                        var sphereRenderer = cleared_sphere.GetComponent<Renderer>();
                        sphereRenderer.material = MPC2_mat;
                        cleared_sphere.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                    }
                }

                for (int ii = 0; ii < for_MPC3_V6.Count; ii++)
                {
                    if (!GlobalMPC1.Contains(for_MPC3_V6[ii].Coordinates))
                    {
                        GlobalMPC1V6.Add(for_MPC3_V6[ii]);
                        GlobalMPC1.Add(for_MPC3_V6[ii].Coordinates);

                        GameObject cleared_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                        cleared_sphere.transform.position = for_MPC3_V6[ii].Coordinates;
                        Destroy(cleared_sphere.GetComponent<SphereCollider>()); // remove collider
                        var sphereRenderer = cleared_sphere.GetComponent<Renderer>();
                        sphereRenderer.material = MPC3_mat;
                        cleared_sphere.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                    }
                }

                area = new Area34(point1, shift1, point2, shift2);
                DrawArea(area);
            }
            


        }
        Debug.Log("Buildings ");
    }

















    void PickMPC(Vector3 n1, Vector3 p1, Vector3 s1, Vector3 n2, Vector3 p2, Vector3 s2, List<Vector3> ListOfNearbyScatterers, out List<V6> MPC_V6)
    {
        MPC_V6 = new List<V6>();

        if (p1 == p2) // the polygon is a triangle
        {
            Vector3 side1 = s1 - p1;
            Vector3 side2 = s2 - p2;
            // the area of the polygon 
            Area2D(side1, side2, out float S);

            // check if MPC is within the given traingle
            for (int i = 0; i < ListOfNearbyScatterers.Count; i++)
            {
                Vector3 v1 = s1 - ListOfNearbyScatterers[i];
                Vector3 v2 = p1 - ListOfNearbyScatterers[i];
                Vector3 v3 = s2 - ListOfNearbyScatterers[i];
                // calculate areas of the triangles; if the sum of 3 is equal to the polygon area, then the point is in the trianlge
                Area2D(v1, v2, out float S1);
                Area2D(v2, v3, out float S2);
                Area2D(v3, v1, out float S3);
                if (Mathf.Abs(S - S1 - S2 - S3) < 0.001)
                {
                    // if the sum of areas is almost equal to the whole area, then the MPC is included into the active set of MPCs
                    MPC_V6.Add(new V6(ListOfNearbyScatterers[i], 0.5f * (n1 + n2)));
                }
            }
        }
        else // the polygon is not a triangle
        {
            float dot_prod_normals = Vector3.Dot(n1, n2);

            if (dot_prod_normals <= 0)    // then the polygon is not convex (or triangle where p1 != p2)
            {
                // in this case, we consider two triangles separately
                Vector3 v11 = p1 - s1;
                Vector3 v12 = p2 - s1;
                Area2D(v11, v12, out float S1);

                Vector3 v21 = p1 - s2;
                Vector3 v22 = p2 - s2;
                Area2D(v21, v22, out float S2);
                // we consider such strange triangles in order to cover the biggest area defined at leas with one correct triangle
                // and at the same time to avoid considering a non-convex quadrangle

                for (int i = 0; i < ListOfNearbyScatterers.Count; i++)
                {
                    Vector3 vp1 = p1 - ListOfNearbyScatterers[i];
                    Vector3 vp2 = p2 - ListOfNearbyScatterers[i];

                    Vector3 vs1 = s1 - ListOfNearbyScatterers[i];
                    Vector3 vs2 = s2 - ListOfNearbyScatterers[i];

                    // common area that is related to both S1 and S2
                    Area2D(vp1, vp2, out float Spp);

                    // areas related to S1
                    Area2D(vp1, vs1, out float Sp1s1);
                    Area2D(vp2, vs1, out float Sp2s1);
                    
                    // areas related to S2
                    Area2D(vp1, vs2, out float Sp1s2);
                    Area2D(vp2, vs2, out float Sp2s2);
                    
                    // check if the point [i] belongs to S1 or S2
                    if (Mathf.Abs(S1 - Spp - Sp1s1 - Sp2s1) < 0.001)
                    {
                        MPC_V6.Add(new V6(ListOfNearbyScatterers[i], n1));
                    }
                    else if (Mathf.Abs(S2 - Spp - Sp1s2 - Sp2s2) < 0.001)
                    {
                        MPC_V6.Add(new V6(ListOfNearbyScatterers[i], n2));
                    }
                }
            }
            else // the polygon is convex
            {
                // the first triangle of the quadrangle
                Vector3 v11 = p1 - s1; 
                Vector3 v12 = s2 - s1;
                Area2D(v11, v12, out float S1);
                
                // the second triangle of the quadrangle
                Vector3 v21 = p1 - p2; 
                Vector3 v22 = s2 - p2;
                Area2D(v21, v22, out float S2);

                for (int i = 0; i < ListOfNearbyScatterers.Count; i++)
                {
                    Vector3 v1 = p1 - ListOfNearbyScatterers[i];
                    Vector3 v2 = s1 - ListOfNearbyScatterers[i];
                    Vector3 v3 = s2 - ListOfNearbyScatterers[i];
                    Vector3 v4 = p2 - ListOfNearbyScatterers[i];

                    Area2D(v1, v2, out float A1);
                    Area2D(v2, v3, out float A2);
                    Area2D(v3, v4, out float A3);
                    Area2D(v4, v1, out float A4);

                    float area_diff = Mathf.Abs(S1 + S2 - A1 - A2 - A3 - A4);

                    if (area_diff < 1)
                    {
                        MPC_V6.Add(new V6(ListOfNearbyScatterers[i], 0.5f * (n1 + n2)));
                    }
                }
            }
        }
    }






















    void Area2D(Vector3 v1, Vector3 v2, out float S)
    {
        S = 0.5f * Mathf.Abs(-v1.x * v2.z + v1.z * v2.x);
    }

    void NearbyElements(float x_l, float x_r, float z_d, float z_u, List<Vector3> MPCs, out List<Vector3> NearbyMPCs)
    {
        NearbyMPCs = new List<Vector3>();
        for (int n = 0; n < MPCs.Count; n++)
        {
            if (MPCs[n].x >= x_l && MPCs[n].x <= x_r && MPCs[n].z >= z_d && MPCs[n].z <= z_u)
            {
                NearbyMPCs.Add(MPCs[n]);
            }
        }
    }


    void DrawArea(Area34 area)
    {
        Debug.DrawLine(area.p1, area.p2, Color.cyan, 5.0f);
        Debug.DrawLine(area.p2, area.s2, Color.yellow, 5.0f);
        Debug.DrawLine(area.s2, area.s1, Color.green, 5.0f);
        Debug.DrawLine(area.s1, area.p1, Color.yellow, 5.0f);
    }
}

public class V6
{
    public Vector3 Coordinates;
    public Vector3 Normal;


    public V6(Vector3 vrtx, Vector3 nrml)
    {
        Coordinates = vrtx;
        Normal = nrml;
    }

}

public class Area34
{
    public Vector3 p1;
    public Vector3 s1; // s1 = p1 + n1 * Width

    public Vector3 p2;
    public Vector3 s2; // s2 = p2 + n2 * Width

    public Area34(Vector3 point1, Vector3 shift1, Vector3 point2, Vector3 shift2)
    {
        p1 = point1;
        s1 = shift1;

        p2 = point2;
        s2 = shift2;
    }

}

