using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

[BurstCompile]
public struct Path3ActiveSet : IJobParallelFor
{
    [ReadOnly] public NativeArray<V6> SeenMPC3Table;
    [ReadOnly] public NativeArray<Path3Half> InputArray;
    [ReadOnly] public NativeArray<Path3Half> CompareArray;
    [ReadOnly] public int MaxListsLength;
    [ReadOnly] public Path3 EmptyElement;

    [WriteOnly] public NativeArray<Path3> Output;
    public void Execute(int index)
    {
        Output[index] = EmptyElement;

        // calculate a relative index of the element inside each batch
        var temp_out_index = Mathf.FloorToInt(index / MaxListsLength);
        var relative_index = index - temp_out_index * MaxListsLength;
        Vector3 direction1 = InputArray[temp_out_index].MPC3_2 - InputArray[temp_out_index].MPC3_1;


        int number_of_entrance = 0;
        
        for (int i = 0; i < CompareArray.Length; i++)
        {
            if (InputArray[temp_out_index].MPC3_2ID == CompareArray[i].MPC3_2ID)
            {
                if (relative_index == number_of_entrance)
                {
                    Vector3 direction2 = CompareArray[i].MPC3_2 - CompareArray[i].MPC3_1;
                    Vector3 n_perp = new Vector3(-SeenMPC3Table[CompareArray[i].MPC3_2ID].Normal.z, 0, SeenMPC3Table[CompareArray[i].MPC3_2ID].Normal.x);
                    if ((Vector3.Dot(direction1, n_perp) * Vector3.Dot(direction2, n_perp)) < 0)
                    {
                        var total_distance = InputArray[temp_out_index].Distance + CompareArray[i].Distance;
                        Output[index] = new Path3(InputArray[temp_out_index].Point, InputArray[temp_out_index].MPC3_1, InputArray[temp_out_index].MPC3_2, CompareArray[i].MPC3_1, CompareArray[i].Point, total_distance);
                        break;
                    }
                }

                number_of_entrance += 1;
            }
        }
        
    }
}
