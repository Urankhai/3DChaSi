﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System;
//using System.Numerics;

public class ChenGenParallel : MonoBehaviour
{
    //[SerializeField] private bool useJobs;
    public float SpeedofLight = 299792458.0f; // m/s
    public float CarrierFrequency = 5.9f * (float)Math.Pow(10,9); // GHz
    public float fsubcarriers = 200000; // kHz

    // defining the properties of .11p signals
    public System.Numerics.Complex[] H = new System.Numerics.Complex[1024]; // Half of LTE BandWidth, instead of 2048 subcarriers
    public NativeArray<System.Numerics.Complex> H_LoS; // creat a NativeArray that will be used to calculate channel response in parallel manner
    public NativeArray<System.Numerics.Complex> H_MPC1;
    public NativeArray<System.Numerics.Complex> H_MPC2;
    public NativeArray<System.Numerics.Complex> H_MPC3;
    public NativeArray<float> Subcarriers;
    

    //List<List<GeoComp>> GlobeCom2;
    //List<List<GeoComp>> GlobeCom3;

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
        if (H_LoS.IsCreated)
        { H_LoS.Dispose(); }
        if (H_MPC1.IsCreated)
        { H_MPC1.Dispose(); }
        if (H_MPC2.IsCreated)
        { H_MPC2.Dispose(); }
        if (H_MPC3.IsCreated)
        { H_MPC3.Dispose(); }
        if (Subcarriers.IsCreated)
        { Subcarriers.Dispose(); }
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

    /// <summary>
    ///  Data for Fourier transform
    /// </summary>
    double[] Y_output;
    double[] H_output;
    double[] X_inputValues;
    [Space(10)]
    [Header("CHARTS FOR DRAWING")]
    [Space]
    public Transform tfTime;
    public Transform tfFreq;

    //int update_flag = 0;

    List<V6> MPC1;
    List<V6> MPC2;
    List<V6> MPC3;

    // defining empty elements
    public Path3Half empty_path3Half = new Path3Half(new Vector3(0,0,0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0, 0f, 0f);
    public Path3 empty_path3 = new Path3(new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0f, 0f);
    public Path2 empty_path2 = new Path2(new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0f, 0f);
    public Path1 empty_path = new Path1(new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0f, 0f);

    

    // Start is called before the first frame update
    void Start()
    {
        /// for Fourier transform
        X_inputValues = new double[H.Length];
        for (int i = 0; i < H.Length; i++)
        { X_inputValues[i] = i; }


        // finding the script
        GameObject LookUpT = GameObject.Find("LookUpTables");
        LookUpTableGen LUT_Script = LookUpT.GetComponent<LookUpTableGen>();

        // this may not be used
        // GlobeCom2 = LUT_Script.GC2; // coordinates, normals, distances, departing and arriving angles
        // GlobeCom3 = LUT_Script.GC3; // coordinates, normals, distances, departing and arriving angles

        // reading info about MPC2
        LinearGlobeCom2 = LUT_Script.Linear_GC2;
        IndexesGlobeCom2 = LUT_Script.Indexes_GC2;
        MaxLengthOfSeenMPC2Lists = LUT_Script.MaxLengthOfSeenMPC2Lists;

        // initialize the H_parallel NativeArray
        H_LoS = new NativeArray<System.Numerics.Complex>(H.Length, Allocator.Persistent); // Check if one array can be used by a number of jobs and correctly changed
        H_MPC1 = new NativeArray<System.Numerics.Complex>(H.Length, Allocator.Persistent);
        H_MPC2 = new NativeArray<System.Numerics.Complex>(H.Length, Allocator.Persistent);
        H_MPC3 = new NativeArray<System.Numerics.Complex>(H.Length, Allocator.Persistent);
        Subcarriers = new NativeArray<float>(H.Length, Allocator.Persistent);
        for (int i = 0; i < H.Length; i++)
        {
            H[i] = new System.Numerics.Complex(0, 0);
            H_LoS[i] = new System.Numerics.Complex(0, 0);
            H_MPC1[i] = new System.Numerics.Complex(0, 0);
            H_MPC2[i] = new System.Numerics.Complex(0, 0);
            H_MPC3[i] = new System.Numerics.Complex(0, 0);
            Subcarriers[i] = CarrierFrequency + fsubcarriers * (i + 1);
        }

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

        NativeList<JobHandle> jobHandleList_Channel = new NativeList<JobHandle>(Allocator.Temp);

        /*
        for (int i = 0; i < H.Length; i++)
        {
            H_LoS[i] = new System.Numerics.Complex(0, 0);
            H_MPC1[i] = new System.Numerics.Complex(0, 0);
            H_MPC2[i] = new System.Numerics.Complex(0, 0);
            H_MPC3[i] = new System.Numerics.Complex(0, 0);
        }
        */

        ///////////////////////////////////////////////////////////////////////////////////
        /// LOS
        ///////////////////////////////////////////////////////////////////////////////////

        float dtLoS = 0;
        float PathGainLoS = 0;

        if (!Physics.Linecast(Tx.transform.position, Rx.transform.position))
        {
            if (LOS_Tracer)
            {
                Debug.DrawLine(Tx.transform.position, Rx.transform.position, Color.magenta);
            }

            float LOS_distance = (Tx.transform.position - Rx.transform.position).magnitude;
            //LOS_distance = 200;
            dtLoS = LOS_distance / SpeedofLight;// + 1000/SpeedofLight;
            PathGainLoS = (float)Math.Pow(1 / LOS_distance, 2);

        }
        
        var dtLoSParallel = new NativeArray<float>(1, Allocator.TempJob);
        var PathGainLoSParallel = new NativeArray<float>(1, Allocator.TempJob);
        for (int i = 0; i < dtLoSParallel.Length; i++)
        {
            dtLoSParallel[i] = dtLoS;
            PathGainLoSParallel[i] = PathGainLoS;
        }

        ChannelParallel channelParallel = new ChannelParallel
        {
            TimeDelayArray = dtLoSParallel,
            PathsGainArray = PathGainLoSParallel,
            FrequencyArray = Subcarriers,

            HH = H_LoS,
        };
        JobHandle jobHandleLoSChannel = channelParallel.Schedule(Subcarriers.Length, 8);
        // we add the job to the list of jobs related to channel calculation in order to complete them all at once later
        //jobHandleList_Channel.Add(jobHandleLoSChannel);

        //JobHandle.CompleteAll(jobHandleList_Channel);
        jobHandleLoSChannel.Complete();
        dtLoSParallel.Dispose();
        PathGainLoSParallel.Dispose();
        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC1
        ///////////////////////////////////////////////////////////////////////////////////

        var RxArray1 = new NativeArray<int>(Rx_MPC1.Count, Allocator.TempJob);
        var TxArray1 = new NativeArray<int>(Tx_MPC1.Count, Allocator.TempJob);
        
        var possiblePath1 = new NativeArray<Path1>(Tx_MPC1.Count, Allocator.TempJob);
        var dtMPC1Array = new NativeArray<float>(Tx_MPC1.Count, Allocator.TempJob);
        var PathGainMPC1 = new NativeArray<float>(Tx_MPC1.Count, Allocator.TempJob);
        for (int i = 0; i < Rx_MPC1.Count; i++)
        { RxArray1[i] = Rx_MPC1[i]; }
        for (int i = 0; i < Tx_MPC1.Count; i++)
        {
            TxArray1[i] = Tx_MPC1[i];
            possiblePath1[i] = empty_path;
        }
        CommonMPC1Parallel commonMPC1Parallel = new CommonMPC1Parallel
        {
            Speed_of_Light = SpeedofLight,
            MPC1 = SeenMPC1Table,
            Array1 = RxArray1,
            Array2 = TxArray1,
            Rx_Point = Rx.transform.position,
            Tx_Point = Tx.transform.position,

            Output = possiblePath1,
            OutputDelays = dtMPC1Array,
            OutputAmplitudes = PathGainMPC1,
        };
        JobHandle jobHandleMPC1 = commonMPC1Parallel.Schedule(TxArray1.Length, 2);
        jobHandleMPC1.Complete();

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

        

        // channel calculation
        ChannelParallel channelParallelMPC1 = new ChannelParallel
        {
            TimeDelayArray = dtMPC1Array,
            PathsGainArray = PathGainMPC1,
            FrequencyArray = Subcarriers,

            HH = H_MPC1,
        };
        JobHandle jobHandleChannelMPC1 = channelParallelMPC1.Schedule(Subcarriers.Length, 8, jobHandleMPC1);
        // we add the job to the list of jobs related to channel calculation in order to complete them all at once later
        //jobHandleList_Channel.Add(jobHandleChannelMPC1);
        jobHandleChannelMPC1.Complete();

        RxArray1.Dispose();
        TxArray1.Dispose();
        possiblePath1.Dispose();
        dtMPC1Array.Dispose();
        PathGainMPC1.Dispose();
        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC2 Parallel
        ///////////////////////////////////////////////////////////////////////////////////

        var level2MPC2 = new NativeArray<int>(Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists, Allocator.TempJob);
        var possiblePath2 = new NativeArray<Path2>(Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists, Allocator.TempJob);
        var dtArrayMPC2 = new NativeArray<float>(Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists, Allocator.TempJob);
        var PathGainMPC2 = new NativeArray<float>(Rx_MPC2.Count * MaxLengthOfSeenMPC2Lists, Allocator.TempJob);

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
            Speed_of_Light = SpeedofLight,

            SecondOrderPaths = possiblePath2,
            OutputDelays = dtArrayMPC2,
            OutputAmplitudes = PathGainMPC2,
        };
        // create a job handle list
        JobHandle jobHandleMPC2 = path2ParallelSearch.Schedule(level2MPC2.Length, MaxLengthOfSeenMPC2Lists, jobHandleMPC1);
        
        jobHandleMPC2.Complete();



        /// MPC2
        List<Path2> second_order_paths_full_parallel = new List<Path2>();

        if (MPC2_Tracer)
        {
            for (int l = 0; l < possiblePath2.Length; l++)
            {
                if (possiblePath2[l].Distance > 0)
                {
                    second_order_paths_full_parallel.Add(possiblePath2[l]);
                    Debug.DrawLine(possiblePath2[l].Rx_Point, possiblePath2[l].MPC2_1, Color.white);
                    Debug.DrawLine(possiblePath2[l].MPC2_1, possiblePath2[l].MPC2_2, Color.white);
                    Debug.DrawLine(possiblePath2[l].MPC2_2, possiblePath2[l].Tx_Point, Color.white);
                }
            }
        }

        // channel calculation
        ChannelParallel channelParallelMPC2 = new ChannelParallel
        {
            TimeDelayArray = dtArrayMPC2,
            PathsGainArray = PathGainMPC2,
            FrequencyArray = Subcarriers,

            HH = H_MPC2,
        };
        JobHandle jobHandleChannelMPC2 = channelParallelMPC2.Schedule(Subcarriers.Length, 8, jobHandleMPC2);
        // we add the job to the list of jobs related to channel calculation in order to complete them all at once later
        //jobHandleList_Channel.Add(jobHandleChannelMPC1);
        jobHandleChannelMPC2.Complete();

        level2MPC2.Dispose();
        possiblePath2.Dispose();
        TxMPC2Array.Dispose();
        dtArrayMPC2.Dispose();
        PathGainMPC2.Dispose();

        ///////////////////////////////////////////////////////////////////////////////////
        /// MPC3
        ///////////////////////////////////////////////////////////////////////////////////


        // define how many elements should be processed in a single core
        int innerloopBatchCount = MaxLengthOfSeenMPC3Lists;
        
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
        
        JobHandle RxjobHandleMPC3 = RxhalfPath3Set.Schedule(Rx_Seen_MPC3.Length, innerloopBatchCount, jobHandleMPC2);
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

        JobHandle TxjobHandleMPC3 = TxhalfPath3Set.Schedule(Tx_Seen_MPC3.Length, innerloopBatchCount, jobHandleMPC2);
        jobHandleList.Add(TxjobHandleMPC3);

        JobHandle.CompleteAll(jobHandleList);


        

        // storing nonempty path3s
        List<Path3Half> TxHalfPath3 = new List<Path3Half>();
        List<Path3Half> RxHalfPath3 = new List<Path3Half>();

        // introducing a little bit of randomness to the third order of paths selection
        int MPC3PathStep = 3; // otherwise, the sets of possible third order of paths become too big
        for (int l = 0; l < TxReachableHalfPath3Array.Length; l += MPC3PathStep)
        {
            if (TxReachableHalfPath3Array[l].Distance > 0)
            {
                TxHalfPath3.Add(TxReachableHalfPath3Array[l]);
            }
        }
        for (int l = 0; l < RxReachableHalfPath3Array.Length; l += MPC3PathStep)
        {
            if (RxReachableHalfPath3Array[l].Distance > 0)
            {
                RxHalfPath3.Add(RxReachableHalfPath3Array[l]);
            }
        }
        


        
        //float startTime2 = Time.realtimeSinceStartup;

        NativeArray<Path3Half> RxNativeArray = new NativeArray<Path3Half>(RxHalfPath3.Count, Allocator.TempJob);
        for (int i = 0; i < RxNativeArray.Length; i++)
        { RxNativeArray[i] = RxHalfPath3[i]; }

        NativeArray<Path3Half> TxNativeArray = new NativeArray<Path3Half>(TxHalfPath3.Count, Allocator.TempJob);
        for (int i = 0; i < TxNativeArray.Length; i++)
        { TxNativeArray[i] = TxHalfPath3[i]; }

        NativeArray<Path3> activepath3 = new NativeArray<Path3>(RxHalfPath3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
        NativeArray<float> dtArrayMPC3 = new NativeArray<float>(RxHalfPath3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);
        NativeArray<float> PathGainMPC3 = new NativeArray<float>(RxHalfPath3.Count * MaxLengthOfSeenMPC3Lists, Allocator.TempJob);

        Path3ActiveSet Rx_path3ActiveSet = new Path3ActiveSet
        {
            SeenMPC3Table = SeenMPC3Table,
            InputArray = RxNativeArray,
            CompareArray = TxNativeArray,
            MaxListsLength = MaxLengthOfSeenMPC3Lists,
            EmptyElement = empty_path3,
            Speed_of_Light = SpeedofLight,

            Output = activepath3,
            OutputDelays = dtArrayMPC3,
            OutputAmplitudes = PathGainMPC3

        };
        JobHandle Jobforpath3ActiveSet = Rx_path3ActiveSet.Schedule(activepath3.Length, MaxLengthOfSeenMPC3Lists, TxjobHandleMPC3);

        Jobforpath3ActiveSet.Complete();

        // channel calculation
        ChannelParallel channelParallelMPC3 = new ChannelParallel
        {
            TimeDelayArray = dtArrayMPC3,
            PathsGainArray = PathGainMPC3,
            FrequencyArray = Subcarriers,

            HH = H_MPC3,
        };
        JobHandle jobHandleChannelMPC3 = channelParallelMPC3.Schedule(Subcarriers.Length, 8, Jobforpath3ActiveSet);
        // we add the job to the list of jobs related to channel calculation in order to complete them all at once later
        //jobHandleList_Channel.Add(jobHandleChannelMPC1);
        jobHandleChannelMPC3.Complete();

        List<Path3> third_order_paths_full_parallel = new List<Path3>();

        if (MPC3_Tracer)
        {
            int trace_count = 0; 
            for (int l = 0; l < activepath3.Length; l++)
            //for (int l = 0; l < 10; l++)
            {
                if (activepath3[l].Distance > 0)
                {
                    trace_count += 1;
                    third_order_paths_full_parallel.Add(activepath3[l]);
                    Debug.DrawLine(activepath3[l].Rx_Point, activepath3[l].MPC3_1, Color.green);
                    Debug.DrawLine(activepath3[l].MPC3_1, activepath3[l].MPC3_2, Color.blue);
                    Debug.DrawLine(activepath3[l].MPC3_2, activepath3[l].MPC3_3, Color.yellow);
                    Debug.DrawLine(activepath3[l].MPC3_3, activepath3[l].Tx_Point, Color.red);
                    //if (trace_count == 10)
                    //{ break; }
                }
            }
        }
        //Debug.Log("Number of 3rd order paths = " + third_order_paths_full_parallel.Count);
        TxNativeArray.Dispose();
        RxNativeArray.Dispose();
        activepath3.Dispose();
        dtArrayMPC3.Dispose();
        PathGainMPC3.Dispose();

        //Debug.Log("Check 2: " + ((Time.realtimeSinceStartup - startTime2) * 1000000f) + " microsec");

        




        Rx_Seen_MPC3.Dispose();
        RxReachableHalfPath3Array.Dispose();
        Tx_Seen_MPC3.Dispose();
        TxReachableHalfPath3Array.Dispose();

        ///////////////////////////////////////////////////////////////////////////////////
        /// complete the works
        ///////////////////////////////////////////////////////////////////////////////////
        /// LoS
        // this part should be moved to the end at least after the completion of the list of the works
        //jobHandleList_Channel.Complete();

        //double asd = System.Numerics.Complex.Abs( H_MPC2[0]);
        for (int i = 0; i < H.Length; i++)
        {

            //H[i] = H_LoS[i];
            //H[i] = H_MPC1[i];
            //H[i] = H_MPC2[i];

            //H[i] = H_LoS[i] + H_MPC1[i];
            H[i] = H_LoS[i] + H_MPC1[i] + H_MPC2[i] + H_MPC3[i];
            //H[i] = H_MPC3[i];

            // zeroing all the inputs
            H_LoS[i] = new System.Numerics.Complex(0,0);
            H_MPC1[i] = new System.Numerics.Complex(0, 0);
            H_MPC2[i] = new System.Numerics.Complex(0, 0);
            H_MPC3[i] = new System.Numerics.Complex(0, 0);
        }


        

        
        System.Numerics.Complex[] outputSignal_Freq = FastFourierTransform.FFT(H, false);

        Y_output = new double[H.Length];
        H_output = new double[H.Length];
        //get module of complex number
        for (int ii = 0; ii < H.Length; ii++)
        {
            //Debug.Log(ii);
            if (System.Numerics.Complex.Abs(outputSignal_Freq[ii]) > 0)
            {
                Y_output[ii] = 10 * Math.Log10(System.Numerics.Complex.Abs(outputSignal_Freq[ii]));// + 0.0000000000001);
                H_output[ii] = 10 * Math.Log10(System.Numerics.Complex.Abs(H[ii]));// + 0.0000000000001);
            }
            else 
            {
                Y_output[ii] = 10 * Math.Log10(System.Numerics.Complex.Abs(outputSignal_Freq[ii]) + 0.0000000000001);
                H_output[ii] = 10 * Math.Log10(System.Numerics.Complex.Abs(H[ii]) + 0.0000000000001);
            }
            //Y_output[ii] = (System.Numerics.Complex.Abs(outputSignal_Freq[ii]) + 0.0000000000001);
            //H_output[ii] = (System.Numerics.Complex.Abs(H[ii]) + 0.0000000000001);
            //if (ii < 50)
            //{ H_output[ii] = 0.1; }
            //H[ii] = new System.Numerics.Complex(0, 0);
        }
        Drawing.drawChart(tfTime, X_inputValues, Y_output, "time");
        Drawing.drawChart(tfFreq, X_inputValues, H_output, "frequency");
        

        
        

        


        
    }


    
    
}


[BurstCompile]
public struct ChannelParallel : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> TimeDelayArray;
    [ReadOnly] public NativeArray<float> PathsGainArray;

    [ReadOnly] public NativeArray<float> FrequencyArray;

    [WriteOnly] public NativeArray<System.Numerics.Complex> HH;

    public void Execute(int index)
    {
        System.Numerics.Complex temp_channel = new System.Numerics.Complex(0, 0);
        if (TimeDelayArray.Length == 1)
        {
            double cosine = Math.Cos(2 * Math.PI * FrequencyArray[index] * TimeDelayArray[0]);
            double sine = Math.Sin(2 * Math.PI * FrequencyArray[index] * TimeDelayArray[0]);
            // defining exponent
            System.Numerics.Complex exponent = new System.Numerics.Complex(cosine, sine);

            temp_channel = PathsGainArray[0] * exponent;
        }
        else
        {
            for (int i = 0; i < TimeDelayArray.Length; i++)
            {
                if (TimeDelayArray[i] > 0)
                {
                    double cosine = Math.Cos(2 * Math.PI * FrequencyArray[index] * TimeDelayArray[i]);
                    double sine = Math.Sin(2 * Math.PI * FrequencyArray[index] * TimeDelayArray[i]);
                    // defining exponent
                    System.Numerics.Complex exponent = new System.Numerics.Complex(cosine, sine);

                    temp_channel = temp_channel + PathsGainArray[i] * exponent;
                }
            }
        }
        HH[index] = temp_channel;
    }
}

[BurstCompile]
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
        V6 level1 = SeenMPC3Table[Seen_MPC3[index]];
        Vector3 point_to_level1 = (level1.Coordinates - Point).normalized;
        float distance1 = (level1.Coordinates - Point).magnitude;

        int temp_index = Mathf.FloorToInt(index / MaxListsLength);
        int temp_i = index - temp_index * MaxListsLength;
        if (temp_i <= LookUpTable3ID[Seen_MPC3[index]].y - LookUpTable3ID[Seen_MPC3[index]].x)
        {
            
            int temp_l = LookUpTable3ID[Seen_MPC3[index]].x + temp_i;
            // defining the second level of points
            V6 level2 = SeenMPC3Table[LookUpTable3[temp_l].MPCIndex];
            Vector3 level2_to_level1 = (level1.Coordinates - level2.Coordinates).normalized;

            // we define a perpendicular direction to the normal of the level1
            Vector3 normal1 = level1.Normal;
            Vector3 n_perp1 = new Vector3(-normal1.z, 0, normal1.x);

            if ( (Vector3.Dot(n_perp1, point_to_level1) * Vector3.Dot(n_perp1, level2_to_level1)) < 0)
            {
                double theta1 = Math.Acos(Vector3.Dot(normal1, -point_to_level1));
                double theta2 = Math.Acos(Vector3.Dot(normal1, -level2_to_level1));
                int I1, I2, I3;
                if (theta1 + theta2 < 0.35)
                { I1 = 0; }
                else { I1 = 1; }
                if (theta1 < 1.22)
                { I2 = 0; }
                else { I2 = 1; }
                if (theta2 < 1.22)
                { I3 = 0; }
                else { I3 = 1; }

                float angular_gain = (float)Math.Exp(-12 * ((theta1 + theta2 - 0.35) * I1 + (theta1 - 1.22) * I2 + (theta2 - 1.22) * I3));

                float distance2 = (level1.Coordinates - level2.Coordinates).magnitude;
                float distance = distance1 + distance2;
                Path3Half temp_Path3Half = new Path3Half(Point, level1.Coordinates, SeenMPC3Table[LookUpTable3[temp_l].MPCIndex].Coordinates, LookUpTable3[temp_l].MPCIndex, distance, angular_gain);
                ReachableHalfPath3[index] = temp_Path3Half;
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
    [ReadOnly] public float Speed_of_Light;
    // this is an array where boolean yes are written
    [WriteOnly] public NativeArray<Path1> Output;
    [WriteOnly] public NativeArray<float> OutputDelays;
    [WriteOnly] public NativeArray<float> OutputAmplitudes;

    public void Execute(int index)
    {
        OutputDelays[index] = 0;
        OutputAmplitudes[index] = 0;
        for (int i = 0; i < Array1.Length; i++)
        {
            if (Array2[index] == Array1[i])
            {
                Vector3 temp_normal = MPC1[Array2[index]].Normal;
                Vector3 n_perp = new Vector3(-temp_normal.z, 0, temp_normal.x);
                Vector3 rx_direction = (Rx_Point - MPC1[Array2[index]].Coordinates).normalized;
                Vector3 tx_direction = (Tx_Point - MPC1[Array2[index]].Coordinates).normalized;

                if ((Vector3.Dot(n_perp, rx_direction) * Vector3.Dot(n_perp, tx_direction)) < 0)
                {
                    // we mimic that we calculate the angles
                    double theta1 = Math.Acos(Vector3.Dot(temp_normal, rx_direction));
                    double theta2 = Math.Acos(Vector3.Dot(temp_normal, tx_direction));
                    
                    int I1, I2, I3;
                    if (theta1 + theta2 < 0.35)
                    { I1 = 0; } 
                    else  { I1 = 1; }
                    if (theta1 < 1.22)
                    { I2 = 0; }
                    else { I2 = 1; }
                    if (theta2 < 1.22)
                    { I3 = 0; }
                    else { I3 = 1; }

                    float angular_gain = (float)Math.Exp(  -12*( (theta1 + theta2 - 0.35)*I1 + (theta1 - 1.22)*I2 + (theta2 - 1.22)*I3 )  );


                    float rx_distance = (Rx_Point - MPC1[Array2[index]].Coordinates).magnitude;
                    float tx_distance = (Tx_Point - MPC1[Array2[index]].Coordinates).magnitude;
                    float distance = rx_distance + tx_distance;
                    Path1 temp_Path1 = new Path1(Rx_Point, MPC1[Array2[index]].Coordinates, Tx_Point, distance, angular_gain);
                    Output[index] = temp_Path1;
                    OutputDelays[index] = distance / Speed_of_Light;
                    OutputAmplitudes[index] = angular_gain * (float)Math.Pow(1 / distance, 2);
                }
                break;
            }
        }
    }
}