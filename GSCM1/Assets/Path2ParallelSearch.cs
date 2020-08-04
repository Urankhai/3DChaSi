using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

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
                if ((Vector3.Dot(n_perp1, level2_to_level1) * Vector3.Dot(n_perp1, level2_to_Tx) < 0) && (Vector3.Dot(n_perp2, level2_to_level1) * Vector3.Dot(n_perp2, Rx_to_level1) < 0))
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
