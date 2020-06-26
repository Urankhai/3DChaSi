using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System;

public class ChenGenParallel : MonoBehaviour
{
    [SerializeField] private bool useJobs;
    List<List<GeoComp>> GlobeCom2;
    List<List<GeoComp>> GlobeCom3;

    List<GeoComp> LinearGlobeCom2;
    List<Vector2Int> IndexesGlobeCom2;
    public int MaxLengthOfSeenMPC2Lists;

    public NativeArray<V6> SeenMPC1Table;

    public NativeArray<V6> SeenMPC2Table;
    public NativeArray<GeoComp> LookUpTable2;
    public NativeArray<Vector2Int> LookUpTable2ID;
    
    List<GeoComp> LinearGlobeCom3;
    List<Vector2Int> IndexesGlobeCom3;
    public int MaxLengthOfSeenMPC3Lists;
    
    public NativeArray<V6> SeenMPC3Table;
    public NativeArray<GeoComp> LookUpTable3;
    public NativeArray<Vector2Int> LookUpTable3ID;
    private void OnDisable()
    {
        if (SeenMPC1Table.IsCreated)
        { SeenMPC1Table.Dispose(); }
        if (SeenMPC2Table.IsCreated)
        { SeenMPC2Table.Dispose(); }
        if (LookUpTable2.IsCreated)
        { LookUpTable2.Dispose(); }
        if (LookUpTable2ID.IsCreated)
        { LookUpTable2ID.Dispose(); }
        if (SeenMPC3Table.IsCreated)
        { SeenMPC3Table.Dispose(); }
        if (LookUpTable3.IsCreated)
        { LookUpTable3.Dispose(); }
        if (LookUpTable3ID.IsCreated)
        { LookUpTable3ID.Dispose(); }
    }

    // extracting Rx data
    public GameObject Rx;
    Transceiver_Channel Rx_Seen_MPC_Script;
    List<int> Rx_MPC1;
    List<int> Rx_MPC2;
    List<int> Rx_MPC3;
    // extrecting Tx data
    public GameObject Tx;
    Transceiver_Channel Tx_Seen_MPC_Script;
    List<int> Tx_MPC1;
    List<int> Tx_MPC2;
    List<int> Tx_MPC3;

    public bool LOS_Tracer;
    public bool MPC1_Tracer;
    public bool MPC2_Tracer;
    public bool MPC3_Tracer;

    //int update_flag = 0;

    List<V6> MPC1;
    List<V6> MPC2;
    List<V6> MPC3;

    // defining empty elements
    public Path3Half empty_path3Half = new Path3Half(new Vector3(0,0,0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0, 0f);
    public Path2 empty_path2 = new Path2(new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0f);
    public Path1 empty_path = new Path1(new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0f);

    // Start is called before the first frame update
    void Start()
    {
        // finding the script
        GameObject LookUpT = GameObject.Find("LookUpTables");
        LookUpTableGen LUT_Script = LookUpT.GetComponent<LookUpTableGen>();

        // this may not be used
        GlobeCom2 = LUT_Script.GC2; // coordinates, normals, distances, departing and arriving angles
        GlobeCom3 = LUT_Script.GC3; // coordinates, normals, distances, departing and arriving angles

        // reading info about MPC2
        LinearGlobeCom2 = LUT_Script.Linear_GC2;
        IndexesGlobeCom2 = LUT_Script.Indexes_GC2;
        MaxLengthOfSeenMPC2Lists = LUT_Script.MaxLengthOfSeenMPC2Lists;

        // test allocation nativearrays
        LookUpTable2 = new NativeArray<GeoComp>(LinearGlobeCom2.Count, Allocator.Persistent);
        LookUpTable2ID = new NativeArray<Vector2Int>(IndexesGlobeCom2.Count, Allocator.Persistent);

        for (int i = 0; i < LinearGlobeCom2.Count; i++)
        { LookUpTable2[i] = LinearGlobeCom2[i]; }
        for (int i = 0; i < IndexesGlobeCom2.Count; i++)
        { LookUpTable2ID[i] = IndexesGlobeCom2[i]; }

        // reading info about MPC3
        LinearGlobeCom3 = LUT_Script.Linear_GC3;
        IndexesGlobeCom3 = LUT_Script.Indexes_GC3;
        MaxLengthOfSeenMPC3Lists = LUT_Script.MaxLengthOfSeenMPC3Lists;

        LookUpTable3 = new NativeArray<GeoComp>(LinearGlobeCom3.Count, Allocator.Persistent);
        LookUpTable3ID = new NativeArray<Vector2Int>(IndexesGlobeCom3.Count, Allocator.Persistent);

        for (int i = 0; i < LinearGlobeCom3.Count; i++)
        { LookUpTable3[i] = LinearGlobeCom3[i]; }
        for (int i = 0; i < IndexesGlobeCom3.Count; i++)
        { LookUpTable3ID[i] = IndexesGlobeCom3[i]; }
        // test allocation nativearrays

        
        

        GameObject MPC_Spawner = GameObject.Find("Direct_Solution");
        Correcting_polygons MPC_Script = MPC_Spawner.GetComponent<Correcting_polygons>();
        MPC1 = MPC_Script.SeenV6_MPC1;
        MPC2 = MPC_Script.SeenV6_MPC2;
        MPC3 = MPC_Script.SeenV6_MPC3;

        SeenMPC1Table = new NativeArray<V6>(MPC1.Count, Allocator.Persistent);
        for (int i = 0; i < MPC1.Count; i++)
        { SeenMPC1Table[i] = MPC1[i]; }

        SeenMPC2Table = new NativeArray<V6>(MPC2.Count, Allocator.Persistent);
        for (int i = 0; i < MPC2.Count; i++)
        { SeenMPC2Table[i] = MPC2[i]; }

        SeenMPC3Table = new NativeArray<V6>(MPC3.Count, Allocator.Persistent);
        for (int i = 0; i < MPC3.Count; i++)
        { SeenMPC3Table[i] = MPC3[i]; }

        // assigning sripts names to the Tx and Rx
        Tx_Seen_MPC_Script = Tx.GetComponent<Transceiver_Channel>();
        Rx_Seen_MPC_Script = Rx.GetComponent<Transceiver_Channel>();
    }

   

    private void FixedUpdate()
    {
        Rx_MPC1 = Rx_Seen_MPC_Script.seen_MPC1;
        Rx_MPC2 = Rx_Seen_MPC_Script.seen_MPC2;
        Rx_MPC3 = Rx_Seen_MPC_Script.seen_MPC3;

        Tx_MPC1 = Tx_Seen_MPC_Script.seen_MPC1;
        Tx_MPC2 = Tx_Seen_MPC_Script.seen_MPC2;
        Tx_MPC3 = Tx_Seen_MPC_Script.seen_MPC3;

        ///////////////////////////////////////////////////////////////////////////////////
        /// LOS
        ///////////////////////////////////////////////////////////////////////////////////

        if (!Physics.Linecast(Tx.transform.position, Rx.transform.position))
        {
            float LOS_distance = (Tx.transform.position - Rx.transform.position).magnitude;
            if (LOS_Tracer)
            {
                Debug.DrawLine(Tx.transform.position, Rx.transform.position, Color.magenta);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC1
        ///////////////////////////////////////////////////////////////////////////////////

        var RxArray1 = new NativeArray<int>(Rx_MPC1.Count, Allocator.TempJob);
        var TxArray1 = new NativeArray<int>(Tx_MPC1.Count, Allocator.TempJob);
        var possiblePath1 = new NativeArray<Path1>(Tx_MPC1.Count, Allocator.TempJob);
        for (int i = 0; i < Rx_MPC1.Count; i++)
        { RxArray1[i] = Rx_MPC1[i]; }
        for (int i = 0; i < Tx_MPC1.Count; i++)
        {
            TxArray1[i] = Tx_MPC1[i];
            possiblePath1[i] = empty_path;
        }
        CommonMPC1Parallel commonMPC1Parallel = new CommonMPC1Parallel
        {
            MPC1 = SeenMPC1Table,
            Array1 = RxArray1,
            Array2 = TxArray1,
            Rx_Point = Rx.transform.position,
            Tx_Point = Tx.transform.position,

            Output = possiblePath1,
        };
        JobHandle jobHandleMPC1 = commonMPC1Parallel.Schedule(TxArray1.Length, 2);
        // this can be completed later on
        



        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC2 Parallel
        ///////////////////////////////////////////////////////////////////////////////////
        
        var level2MPC2 = new NativeArray<int>(Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists, Allocator.TempJob);
        var possiblePath2 = new NativeArray<Path2>(Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists, Allocator.TempJob);
        for (int l = 0; l < Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists; l++)
        {
            level2MPC2[l] = Rx_MPC2[Mathf.FloorToInt(l / MaxLengthOfSeenMPC2Lists)];
            possiblePath2[l] = empty_path2;
        }

        var TxMPC2Array = new NativeArray<int>(Tx_MPC2.Count, Allocator.TempJob);
        for (int l = 0; l < Tx_MPC2.Count; l++)
        { TxMPC2Array[l] = Tx_MPC2[l]; }

        Path2ParallelSearch path2ParallelSearch = new Path2ParallelSearch
        {
            // common data
            SeenMPC2Table = SeenMPC2Table,
            LookUpTable2 = LookUpTable2,
            LookUpTable2ID = LookUpTable2ID,
            // must be disposed
            Rx_MPC2Array = level2MPC2,
            Tx_MPC2 = TxMPC2Array,

            // other data
            Rx_Position = Rx.transform.position,
            Tx_Position = Tx.transform.position,
            MaxListsLength = MaxLengthOfSeenMPC2Lists,

            SecondOrderPaths = possiblePath2,
        };
        // create a job handle list
        JobHandle Path2Job = path2ParallelSearch.Schedule(level2MPC2.Length, MaxLengthOfSeenMPC2Lists);


        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC3
        ///////////////////////////////////////////////////////////////////////////////////




        // define how many elements should be processed in a single core
        int innerloopBatchCount = MaxLengthOfSeenMPC3Lists;
        
        // List<Path3Half> Rx_halfPath3Parallel = new List<Path3Half>();
        // List<Path3Half> Tx_halfPath3Parallel = new List<Path3Half>();
        Vector3 Rx_Point = Rx.transform.position;
        Vector3 Tx_Point = Tx.transform.position;

        NativeArray<int> Rx_Seen_MPC3 = new NativeArray<int>(Rx_MPC3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
        NativeArray<Path3Half> RxReachableHalfPath3Array = new NativeArray<Path3Half>(Rx_MPC3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
        for (int l = 0; l < Rx_MPC3.Count * MaxLengthOfSeenMPC3Lists; l++)
        {
            Rx_Seen_MPC3[l] = Rx_MPC3[Mathf.FloorToInt(l / MaxLengthOfSeenMPC3Lists)];
            RxReachableHalfPath3Array[l] = empty_path3Half; 
        }

        NativeArray<int> Tx_Seen_MPC3 = new NativeArray<int>(Tx_MPC3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
        NativeArray<Path3Half> TxReachableHalfPath3Array = new NativeArray<Path3Half>(Tx_MPC3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
        for (int l = 0; l < Tx_MPC3.Count * MaxLengthOfSeenMPC3Lists; l++)
        {
            Tx_Seen_MPC3[l] = Tx_MPC3[Mathf.FloorToInt(l / MaxLengthOfSeenMPC3Lists)];
            TxReachableHalfPath3Array[l] = empty_path3Half; 
        }

        HalfPath3Set RxhalfPath3Set = new HalfPath3Set
        {
            // common data
            SeenMPC3Table = SeenMPC3Table,
            LookUpTable3 = LookUpTable3,
            LookUpTable3ID = LookUpTable3ID,
            MaxListsLength = MaxLengthOfSeenMPC3Lists,

            // Car specific data
            Point = Rx_Point,
            // must be disposed
            Seen_MPC3 = Rx_Seen_MPC3,
            ReachableHalfPath3 = RxReachableHalfPath3Array,
        };
        // create a job handle list
        NativeList<JobHandle> jobHandleList = new NativeList<JobHandle>(Allocator.Temp);
        
        JobHandle RxjobHandleMPC3 = RxhalfPath3Set.Schedule(Rx_Seen_MPC3.Length, innerloopBatchCount);
        jobHandleList.Add(RxjobHandleMPC3);

        HalfPath3Set TxhalfPath3Set = new HalfPath3Set
        {
            // common data
            SeenMPC3Table = SeenMPC3Table,
            LookUpTable3 = LookUpTable3,
            LookUpTable3ID = LookUpTable3ID,
            MaxListsLength = MaxLengthOfSeenMPC3Lists,

            // Car specific data
            Point = Tx_Point,
            // must be disposed
            Seen_MPC3 = Tx_Seen_MPC3,
            ReachableHalfPath3 = TxReachableHalfPath3Array,

        };

        JobHandle TxjobHandleMPC3 = TxhalfPath3Set.Schedule(Tx_Seen_MPC3.Length, innerloopBatchCount);
        jobHandleList.Add(TxjobHandleMPC3);

        JobHandle.CompleteAll(jobHandleList);

        /*
        for (int i = 0; i < RxReachableHalfPath3Array.Length; i++)
        {
            if (RxReachableHalfPath3Array[i].Distance > 0)
            {
                Rx_halfPath3Parallel.Add(RxReachableHalfPath3Array[i]);
            }
        }
        */

        Rx_Seen_MPC3.Dispose();
        RxReachableHalfPath3Array.Dispose();
        Tx_Seen_MPC3.Dispose();
        TxReachableHalfPath3Array.Dispose();

        // complete all works
        jobHandleMPC1.Complete();
        Path2Job.Complete();

        // transition from NativeArrays to Lists
        List<Path1> first_order_paths_full_parallel = new List<Path1>();
        if (MPC1_Tracer)
        {
            for (int i = 0; i < possiblePath1.Length; i++)
            {
                if (possiblePath1[i].Distance > 0)
                {
                    first_order_paths_full_parallel.Add(possiblePath1[i]);
                    Debug.DrawLine(possiblePath1[i].Rx_Point, possiblePath1[i].MPC1, Color.cyan);
                    Debug.DrawLine(possiblePath1[i].Tx_Point, possiblePath1[i].MPC1, Color.cyan);
                }
            }
        }

        List<Path2> second_order_paths_full_parallel = new List<Path2>();

        if (MPC2_Tracer)
        {
            for (int l = 0; l < possiblePath2.Length; l++)
            {
                if (possiblePath2[l].Distance > 0)
                {
                    second_order_paths_full_parallel.Add(possiblePath2[l]);
                    Debug.DrawLine(possiblePath2[l].Rx_Point, possiblePath2[l].MPC2_1, Color.green);
                    Debug.DrawLine(possiblePath2[l].MPC2_1, possiblePath2[l].MPC2_2, Color.yellow);
                    Debug.DrawLine(possiblePath2[l].MPC2_2, possiblePath2[l].Tx_Point, Color.green);
                }
            }
        }


        RxArray1.Dispose();
        TxArray1.Dispose();
        possiblePath1.Dispose();

        level2MPC2.Dispose();
        possiblePath2.Dispose();
        TxMPC2Array.Dispose();

        


        
    }


    void CommonMPC1(List<int> List1, List<int> List2, out List<int> CommonList)
    {
        CommonList = List1.Intersect(List2).ToList();
    }
    
    
}

public struct Path3ParallelSearch : IJobParallelFor
{
    [ReadOnly] public NativeArray<Path3Half> Array1;
    [ReadOnly] public NativeArray<Path3Half> Array2;

    //[WriteOnly] public NativeArray<Path3> OutputArray;
    [WriteOnly] public NativeArray<int> OutputArray;
    public void Execute(int index)
    {
        if (Array1[index].Distance > 0)
        {
            int number_of_seen_mpcs = 0; 
            for (int i = 0; i < Array2.Length; i++)
            {
                if (Array1[index].MPC3_2ID == Array2[i].MPC3_2ID)
                {
                    number_of_seen_mpcs += 1;
                }
            }
            OutputArray[index] = number_of_seen_mpcs;
        }
    }
}

[BurstCompile]
public struct Path2ParallelSearch : IJobParallelFor
{
    // the followings are disposed in the end of simulation
    [ReadOnly] public NativeArray<V6> SeenMPC2Table;
    [ReadOnly] public NativeArray<Vector2Int> LookUpTable2ID;
    [ReadOnly] public NativeArray<GeoComp> LookUpTable2;

    // the folliwings are disposed after the parallel job
    [ReadOnly] public NativeArray<int> Rx_MPC2Array;
    [ReadOnly] public NativeArray<int> Tx_MPC2;

    // simple reads for the execution
    [ReadOnly] public Vector3 Rx_Position;
    [ReadOnly] public Vector3 Tx_Position;
    [ReadOnly] public int MaxListsLength;

    // the folliwings are disposed after the parallel job
    [WriteOnly] public NativeArray<Path2> SecondOrderPaths;

    public void Execute(int index)
    {
        V6 level1 = SeenMPC2Table[Rx_MPC2Array[index]];

        float distance1 = (level1.Coordinates - Rx_Position).magnitude;

        int temp_index = Mathf.FloorToInt(index / MaxListsLength);
        int temp_i = index - temp_index * MaxListsLength;
        if (temp_i <= LookUpTable2ID[Rx_MPC2Array[index]].y - LookUpTable2ID[Rx_MPC2Array[index]].x)
        {
            int temp_l = LookUpTable2ID[Rx_MPC2Array[index]].x + temp_i;
            
            V6 level2 = SeenMPC2Table[LookUpTable2[temp_l].MPCIndex];
            Vector3 level2_to_level1 = level1.Coordinates - level2.Coordinates;
            float distance2 = level2_to_level1.magnitude;

            // check if level2 is seen by Tx
            int existence_flag = 0;
            for (int i = 0; i < Tx_MPC2.Length; i++)
            {
                if (LookUpTable2[temp_l].MPCIndex == Tx_MPC2[i])
                { existence_flag = 1; break; }
            }

            if (existence_flag == 1)
            {
                Vector3 n_perp1 = new Vector3(-level2.Normal.z, 0, level2.Normal.x);
                Vector3 level2_to_Tx = Tx_Position - level2.Coordinates;

                Vector3 n_perp2 = new Vector3(-level1.Normal.z, 0, level1.Normal.x);
                Vector3 Rx_to_level1 = level1.Coordinates - Rx_Position;

                // check if the two vectors come from different sides of the normal
                if ( (Vector3.Dot(n_perp1, level2_to_level1) * Vector3.Dot(n_perp1, level2_to_Tx) < 0) && (Vector3.Dot(n_perp2, level2_to_level1) * Vector3.Dot(n_perp2, Rx_to_level1) < 0))
                {
                    float distance3 = level2_to_Tx.magnitude;
                    float distance = distance1 + distance2 + distance3;
                    Path2 temp_path2 = new Path2(Rx_Position, level1.Coordinates, level2.Coordinates, Tx_Position, distance);
                    SecondOrderPaths[index] = temp_path2;
                }
            }
        }

        
    }
}

//[BurstCompile]
public struct HalfPath3Set : IJobParallelFor
{
    [ReadOnly] public NativeArray<V6> SeenMPC3Table;
    [ReadOnly] public NativeArray<GeoComp> LookUpTable3;
    [ReadOnly] public NativeArray<Vector2Int> LookUpTable3ID;
    [ReadOnly] public Vector3 Point;
    [ReadOnly] public int MaxListsLength;

    [ReadOnly] public NativeArray<int> Seen_MPC3;

    [WriteOnly] public NativeArray<Path3Half> ReachableHalfPath3;
    public void Execute(int index)
    {

        float distance1 = (SeenMPC3Table[Seen_MPC3[index]].Coordinates - Point).magnitude;

        int temp_index = Mathf.FloorToInt(index / MaxListsLength);
        int temp_i = index - temp_index * MaxListsLength;
        if (temp_i <= LookUpTable3ID[Seen_MPC3[index]].y - LookUpTable3ID[Seen_MPC3[index]].x)
        {
            int temp_l = LookUpTable3ID[Seen_MPC3[index]].x + temp_i;

            
            float distance2 = (SeenMPC3Table[LookUpTable3[temp_l].MPCIndex].Coordinates - SeenMPC3Table[Seen_MPC3[index]].Coordinates).magnitude;
            float distance = distance1 + distance2;
            Path3Half temp_Path3Half = new Path3Half(Point, SeenMPC3Table[Seen_MPC3[index]].Coordinates, SeenMPC3Table[LookUpTable3[temp_l].MPCIndex].Coordinates, LookUpTable3[temp_l].MPCIndex, distance);
            ReachableHalfPath3[index] = temp_Path3Half;
        }

    }
}

[BurstCompile]
public struct LightPath2ParallelSearch : IJobParallelFor
{
    [ReadOnly] public NativeArray<V6> MPC2;
    [ReadOnly] public NativeArray<GeoComp> temp_array;
    [ReadOnly] public NativeArray<int> Tx_MPC2_Array;
    
    [ReadOnly] public V6 temp_Rx_MPC2;
    [ReadOnly] public int Rx_MPC2_Value;
    [ReadOnly] public Vector3 RxPosition, TxPosition;

    [WriteOnly] public NativeArray<Path2> SecondOrderPaths;
    public void Execute(int index)
    {
        int flag_existence = 0;
        for (int i = 0; i < temp_array.Length; i++)
        {
            if (temp_array[i].MPCIndex == Tx_MPC2_Array[index])
            { flag_existence = 1; break; }
        }
        if (flag_existence == 1)
        {
            // defining peprendicular to the considered normal that is parallel to the ground. 
            V6 temp_Tx_MPC2 = MPC2[Tx_MPC2_Array[index]];

            Vector3 n_perp1 = new Vector3(-temp_Rx_MPC2.Normal.z, 0, temp_Rx_MPC2.Normal.x);
            Vector3 Rx_dir1 = (RxPosition - temp_Rx_MPC2.Coordinates).normalized;
            Vector3 RT = (temp_Tx_MPC2.Coordinates - temp_Rx_MPC2.Coordinates).normalized;

            Vector3 n_perp2 = new Vector3(-temp_Tx_MPC2.Normal.z, 0, temp_Tx_MPC2.Normal.x);
            Vector3 Tx_dir2 = (TxPosition - temp_Tx_MPC2.Coordinates).normalized;
            Vector3 TR = (temp_Rx_MPC2.Coordinates - temp_Tx_MPC2.Coordinates).normalized;

            if ((Vector3.Dot(n_perp1, Rx_dir1) * Vector3.Dot(n_perp1, RT) < 0) && (Vector3.Dot(n_perp2, Tx_dir2) * Vector3.Dot(n_perp2, TR) < 0))
            {
                float rx_distance2MPC2 = (RxPosition - MPC2[Rx_MPC2_Value].Coordinates).magnitude;
                float tx_distance2MPC2 = (TxPosition - MPC2[Tx_MPC2_Array[index]].Coordinates).magnitude;
                
                float MPC2_distance = (MPC2[Rx_MPC2_Value].Coordinates - MPC2[Tx_MPC2_Array[index]].Coordinates).magnitude;
                float total_distance = rx_distance2MPC2 + MPC2_distance + tx_distance2MPC2;

                Path2 temp_path2 = new Path2(RxPosition, MPC2[Rx_MPC2_Value].Coordinates, MPC2[Tx_MPC2_Array[index]].Coordinates, TxPosition, total_distance);
                SecondOrderPaths[index] = temp_path2;
            }
        }
    }
}




[BurstCompile]
public struct CommonMPC1Parallel : IJobParallelFor
{
    // parallel run performed only above this array
    [ReadOnly] public NativeArray<int> Array1;
    // this is a standard array
    [ReadOnly] public NativeArray<int> Array2;
    [ReadOnly] public NativeArray<V6> MPC1;

    [ReadOnly] public Vector3 Rx_Point, Tx_Point;
    // this is an array where boolean yes are written
    [WriteOnly] public NativeArray<Path1> Output;

    public void Execute(int index)
    {
        for (int i = 0; i < Array1.Length; i++)
        {
            if (Array2[index] == Array1[i])
            {
                Vector3 n_perp = new Vector3(-MPC1[Array2[index]].Normal.z, 0, MPC1[Array2[index]].Normal.x);
                Vector3 rx_direction = (Rx_Point - MPC1[Array2[index]].Coordinates).normalized;
                Vector3 tx_direction = (Tx_Point - MPC1[Array2[index]].Coordinates).normalized;

                if ((Vector3.Dot(n_perp, rx_direction) * Vector3.Dot(n_perp, tx_direction)) < 0)
                {
                    float rx_distance = (Rx_Point - MPC1[Array2[index]].Coordinates).magnitude;
                    float tx_distance = (Tx_Point - MPC1[Array2[index]].Coordinates).magnitude;
                    float distance = rx_distance + tx_distance;
                    Path1 temp_Path1 = new Path1(Rx_Point, MPC1[Array2[index]].Coordinates, Tx_Point, distance);
                    Output[index] = temp_Path1;
                }
                break;
            }
        }
    }
}