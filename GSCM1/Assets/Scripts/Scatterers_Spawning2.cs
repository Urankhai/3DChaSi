using System.Collections.Generic;
using UnityEngine;

public class Scatterers_Spawning2 : MonoBehaviour
{
    // setting the area of the scenario
    public Vector3 bottom_left_corner;
    public float area_height;
    public float area_width;
    private readonly int IfVisualize = 2;
    


    // setting the scatterers spawning parameters
    public float chiW1; // = 0.044; %W1 is first order
    public float chiW2; // = 1.5*0.044; %W2 is second order
    public float chiW3; // = 2*0.044; %W3 is third order

    // setting materials
    // public Material ObsPoint_Material;

    // subset of the seen MPCs
    public List<Vector3> MPC1_possiblepositionList = new List<Vector3>();
    public List<Vector3> MPC2_possiblepositionList = new List<Vector3>();
    public List<Vector3> MPC3_possiblepositionList = new List<Vector3>();

    //private GameObject[] Buildings;
    private GameObject[] Observation_Points;

    void Start()
    {
        float Size_Whole_Area = area_height * area_width;
        Debug.Log("Size of the Area = " + Size_Whole_Area + " square meters");

        Vector3 z_dir = new Vector3(0, 0, 1);
        Vector3 x_dir = new Vector3(1, 0, 0);
        Vector3 corner1 = bottom_left_corner;
        Vector3 corner2 = corner1 + area_height * z_dir;
        Vector3 corner3 = corner1 + area_height * z_dir + area_width * x_dir;
        Vector3 corner4 = corner1 + area_width * x_dir;

        if (IfVisualize == 1)
        {
            Debug.DrawLine(corner1, corner2, Color.white, 5.0f);
            Debug.DrawLine(corner2, corner3, Color.white, 5.0f);
            Debug.DrawLine(corner3, corner4, Color.white, 5.0f);
            Debug.DrawLine(corner4, corner1, Color.white, 5.0f);
        }


        int num_MPC1 = Mathf.FloorToInt(Size_Whole_Area * chiW1);
        int num_MPC2 = Mathf.FloorToInt(Size_Whole_Area * chiW2);
        int num_MPC3 = Mathf.FloorToInt(Size_Whole_Area * chiW3);
        //Debug.Log("MPC1s " + num_MPC1 + "; MPC2s " + num_MPC2 + "; MPC3s " + num_MPC3);
        //int num_DMC = Mathf.FloorToInt(Size_Whole_Area * chiW1);

        Vector3[] MPC1_possiblepositionArray = new Vector3[num_MPC1];
        for (int i = 0; i < num_MPC1; i++)
        {
            MPC1_possiblepositionArray[i].x = Random.Range(corner1.x, corner4.x);
            MPC1_possiblepositionArray[i].y = Random.Range(1.2f, 2.2f);
            MPC1_possiblepositionArray[i].z = Random.Range(corner1.z, corner2.z);
        }

        Vector3[] MPC2_possiblepositionArray = new Vector3[num_MPC2];
        for (int i = 0; i < num_MPC2; i++)
        {
            MPC2_possiblepositionArray[i].x = Random.Range(corner1.x, corner4.x);
            MPC2_possiblepositionArray[i].y = Random.Range(1.2f, 2.2f);
            MPC2_possiblepositionArray[i].z = Random.Range(corner1.z, corner2.z);
        }

        Vector3[] MPC3_possiblepositionArray = new Vector3[num_MPC3];
        for (int i = 0; i < num_MPC3; i++)
        {
            MPC3_possiblepositionArray[i].x = Random.Range(corner1.x, corner4.x);
            MPC3_possiblepositionArray[i].y = Random.Range(1.2f, 2.2f);
            MPC3_possiblepositionArray[i].z = Random.Range(corner1.z, corner2.z);
        }


        // Declaring observation points
        Observation_Points = GameObject.FindGameObjectsWithTag("Observation_Points");

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // manipulations with the scene roads

        

        int MPC1_count = 0;
        int MPC2_count = 0;
        int MPC3_count = 0;

        for (int k = 0; k < Observation_Points.Length; k++)
        {
            // Seen MPC1s
            for (int i1 = 0; i1 < num_MPC1; i1++)
            {
                if (!Physics.Linecast(Observation_Points[k].transform.position, MPC1_possiblepositionArray[i1]))
                {
                    if (!MPC1_possiblepositionList.Contains(MPC1_possiblepositionArray[i1]))
                    {
                        MPC1_possiblepositionList.Add(MPC1_possiblepositionArray[i1]);
                        MPC1_count += 1;

                    }
                }
            }

            // Seen MPC2s
            for (int i2 = 0; i2 < num_MPC2; i2++)
            {
                if (!Physics.Linecast(Observation_Points[k].transform.position, MPC2_possiblepositionArray[i2]))
                {
                    if (!MPC2_possiblepositionList.Contains(MPC2_possiblepositionArray[i2]))
                    {
                        MPC2_possiblepositionList.Add(MPC2_possiblepositionArray[i2]);
                        MPC2_count += 1;

                    }
                }
            }

            // Seen MPC3s
            for (int i3 = 0; i3 < num_MPC3; i3++)
            {
                if (!Physics.Linecast(Observation_Points[k].transform.position, MPC3_possiblepositionArray[i3]))
                {
                    if (!MPC3_possiblepositionList.Contains(MPC3_possiblepositionArray[i3]))
                    {
                        MPC3_possiblepositionList.Add(MPC3_possiblepositionArray[i3]);
                        MPC3_count += 1;

                    }
                }
            }
        }
        Debug.Log("MPC1s " + MPC1_possiblepositionList.Count + "; MPC2s " + MPC2_possiblepositionList.Count + "; MPC3s " + MPC3_possiblepositionList.Count);
    }
}
