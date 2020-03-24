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
        Debug.Log("Number of MPC1 is " + MPC1.Count);
        /*
        for (int i = 0; i < MPC1.Count; i++)
        {
            GameObject cleared_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            cleared_sphere.transform.position = MPC1[i];
            Destroy(cleared_sphere.GetComponent<SphereCollider>()); // remove collider
            var sphereRenderer = cleared_sphere.GetComponent<Renderer>();
            sphereRenderer.material = MPC1_mat;
            cleared_sphere.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        }
        */

        List<Vector3> MPC2 = MPC_Script.MPC2_possiblepositionList;
        List<Vector3> MPC3 = MPC_Script.MPC3_possiblepositionList;

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
            NearbyElements(x_l, x_r, z_d, z_u, MPC1, out List<Vector3> NeabyMPCs);
            Debug.Log("Building " + k + " has " + NeabyMPCs.Count + " from function 1st order scatterers nearby");



            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Defining areas around building where scatterers can be located
            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            // define the first point and its protruding point defined by the first point's normal
            Vector3 point1 = floor_vrtx[floor_vrtx.Count - 1];
            Vector3 shift1 = floor_vrtx[floor_vrtx.Count - 1] + WW1 * floor_nrml[floor_vrtx.Count - 1];

            // define the second point and its protruding point defined by the second point's normal
            Vector3 point2 = floor_vrtx[0];
            Vector3 shift2 = floor_vrtx[0] + WW1 * floor_nrml[0];

            // define the area's normal to define normals for the scatterers
            Vector3 area_nrml = (floor_nrml[floor_vrtx.Count - 1] + floor_nrml[0]).normalized;

            Area34 area = new Area34(point1, shift1, point2, shift2);
            //DrawArea(area);

            // draw areas for all vertices
            for (int l = 0; l < floor_vrtx.Count - 1; l++)
            {
                point1 = floor_vrtx[l];
                point2 = floor_vrtx[l + 1];
                
                shift1 = floor_vrtx[l] + WW1 * floor_nrml[l];
                shift2 = floor_vrtx[l+1] + WW1 * floor_nrml[l+1];

                area_nrml = (floor_nrml[floor_vrtx.Count - 1] + floor_nrml[0]).normalized;

                area = new Area34(point1, shift1, point2, shift2);
                //DrawArea(area);
            }
            


        }
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

