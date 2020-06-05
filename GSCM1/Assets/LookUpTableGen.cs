using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LookUpTableGen : MonoBehaviour
{
    public List<List<int>> LUT2 = new List<List<int>>();
    public List<List<int>> LUT3 = new List<List<int>>();

    public List<List<GeoComp>> GC2 = new List<List<GeoComp>>();
    public List<List<GeoComp>> GC3 = new List<List<GeoComp>>();


    // Start is called before the first frame update
    void Start()
    {
        GameObject MPC_Spawner = GameObject.Find("Direct_Solution");
        Correcting_polygons MPC_Script = MPC_Spawner.GetComponent<Correcting_polygons>();
        List<V6> MPC1 = MPC_Script.SeenV6_MPC1;
        List<V6> MPC2 = MPC_Script.SeenV6_MPC2;
        List<V6> MPC3 = MPC_Script.SeenV6_MPC3;

        Debug.Log("LookUpTableGen Script");
        Debug.Log("#MPC1 = " + MPC1.Count + "; #MPC2 = " + MPC2.Count + "; #MPC3 = " + MPC3.Count);


        string do_we_need_third_order = "n";

        // for MPC1, we do not need to have a LookUp table
        // LookUpTable for MPC2
        double angle_threshold2 = 0.1;
        for (int i = 0; i < MPC2.Count; i++)
        {
            List<int> tempV6list = new List<int>();
            List<GeoComp> temp_goeComps = new List<GeoComp>();
            int temp_seen_mpc = 0;
            for (int ii = 0; ii < MPC2.Count; ii++)
            {
                if (i != ii)
                {
                    Vector3 temp_direction = (MPC2[ii].Coordinates - MPC2[i].Coordinates).normalized;
                    if(!Physics.Linecast(MPC2[i].Coordinates, MPC2[ii].Coordinates))
                    {
                        if (Vector3.Dot(temp_direction, MPC2[ii].Normal) < -angle_threshold2 && Vector3.Dot(temp_direction, MPC2[i].Normal) > angle_threshold2)
                        {
                            tempV6list.Add(ii);
                            temp_seen_mpc += 1;

                            float dist = (MPC2[ii].Coordinates - MPC2[i].Coordinates).magnitude;
                            float aod = Mathf.Acos( Vector3.Dot(temp_direction, MPC2[i].Normal) );
                            float aoa = Mathf.Acos(-Vector3.Dot(temp_direction, MPC2[ii].Normal));
                            GeoComp all_comps = new GeoComp(ii, dist, aod, aoa);
                            temp_goeComps.Add(all_comps);

                            if (do_we_need_third_order == "y")
                            {
                                if (i == 20)
                                {

                                    Debug.DrawLine(MPC2[i].Coordinates, MPC2[ii].Coordinates, Color.yellow, 5.0f);

                                    Debug.Log("The normal of seen mpc " + MPC2[ii].Normal + "; the connecting line direction " + temp_direction);
                                    Debug.DrawLine(MPC2[ii].Coordinates, MPC2[ii].Coordinates + temp_direction * 3.0f, Color.green, 5.0f);
                                    Debug.DrawLine(MPC2[ii].Coordinates, MPC2[ii].Coordinates + MPC2[ii].Normal * 3.0f, Color.red, 5.0f);
                                }
                            }
                        }
                    }
                }
            }
            LUT2.Add(tempV6list); 
            GC2.Add(temp_goeComps);
        }


        // LookUpTable for MPC3
        double angle_threshold3 = 0.3;
        for (int i = 0; i < MPC3.Count; i++)
        {
            List<int> tempV6list = new List<int>();
            List<GeoComp> temp_goeComps = new List<GeoComp>();
            
            int temp_seen_mpc = 0;
            for (int ii = 0; ii < MPC3.Count; ii++)
            {
                if (i != ii)
                {
                    Vector3 temp_direction = (MPC3[ii].Coordinates - MPC3[i].Coordinates).normalized;
                    if (!Physics.Linecast(MPC3[i].Coordinates, MPC3[ii].Coordinates))
                    {
                        if (Vector3.Dot(temp_direction, MPC3[ii].Normal) < -angle_threshold3 && Vector3.Dot(temp_direction, MPC3[i].Normal) > angle_threshold3)
                        {
                            tempV6list.Add(ii);
                            temp_seen_mpc += 1;

                            float dist = (MPC3[ii].Coordinates - MPC3[i].Coordinates).magnitude;
                            float aod = Mathf.Acos(Vector3.Dot(temp_direction, MPC3[i].Normal));
                            float aoa = Mathf.Acos(-Vector3.Dot(temp_direction, MPC3[ii].Normal));
                            GeoComp all_comps = new GeoComp(ii, dist, aod, aoa);
                            temp_goeComps.Add(all_comps);

                            if (do_we_need_third_order == "y")
                            {
                                if (i == 20)
                                {

                                    Debug.DrawLine(MPC3[i].Coordinates, MPC3[ii].Coordinates, Color.yellow, 5.0f);

                                    Debug.Log("The normal of seen mpc " + MPC3[ii].Normal + "; the connecting line direction " + temp_direction);
                                    Debug.DrawLine(MPC3[ii].Coordinates, MPC3[ii].Coordinates + temp_direction * 3.0f, Color.green, 5.0f);
                                    Debug.DrawLine(MPC3[ii].Coordinates, MPC3[ii].Coordinates + MPC3[ii].Normal * 3.0f, Color.red, 5.0f);
                                }
                            }
                        }
                    }
                }
            }
            LUT3.Add(tempV6list); 
            GC3.Add(temp_goeComps);
        } 


    }

}

// class for geometrical components
public class GeoComp
{
    public int MPCIndex;
    public float Distance;
    public float AoD; // angle that indicates departed ray from the considered MPC
    public float AoA; // angle that indicates arrived ray to the observable MPC


    public GeoComp(int k, float d, float aod, float aoa)
    {
        MPCIndex = k;
        Distance = d;
        AoD = aod;
        AoA = aoa;
    }
    // TODO create a class that includes attenuations as well
}