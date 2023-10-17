using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// For converting Non-Blittable type data to a Blittable type so we can asynchronously process the data on other threads.
/// </summary>
public static class BlittableExtensions
{
    /// Converting Triangle class data to blittable format
    public static NativeArray<float> ToBlittable(this Triangle[] tris)
    {
        NativeArray<float> triangleVerticesBlittable = new NativeArray<float>(tris.Length * 6, Allocator.Persistent);
        for (int i = 0; i < tris.Length; i++)
        {
            triangleVerticesBlittable[(i * 6)] = tris[i].Vertices[0].x;
            triangleVerticesBlittable[(i * 6) + 1] = tris[i].Vertices[0].y;
            triangleVerticesBlittable[(i * 6) + 2] = tris[i].Vertices[1].x;
            triangleVerticesBlittable[(i * 6) + 3] = tris[i].Vertices[1].y;
            triangleVerticesBlittable[(i * 6) + 4] = tris[i].Vertices[2].x;
            triangleVerticesBlittable[(i * 6) + 5] = tris[i].Vertices[2].y;
        }

        return triangleVerticesBlittable;
    }

    // convert back from blittable format triangle data to regular Triangle class type
    public static Triangle[] ToTriangles(this NativeArray<float> triVertsBlittable)
    {
        Triangle[] triangles = new Triangle[triVertsBlittable.Length / 6];
        for (int i = 0; i < triVertsBlittable.Length; i += 6)
        {
            triangles[i / 6] = new Triangle(
                new Vector2(triVertsBlittable[i], triVertsBlittable[i + 1]),
                new Vector2(triVertsBlittable[i + 2], triVertsBlittable[i + 3]),
                new Vector2(triVertsBlittable[i + 4], triVertsBlittable[i + 5]));
        }
        return triangles;
    }
    
    /// <summary>
    /// Converts a list of Node class data to blittable format. Format as follows:
    /// ----------------------------------------------------------------------------------------------------------------<br/>
    /// [node index + 0] - node name <br/>
    /// [node index + 1] - node position X <br/>
    /// [node index + 2] - node position Y <br/>
    /// [node index + 3] - 1st Connected Node Name <br/>
    /// [node index + 4] - 1st Connected Node Path weight <br/>
    /// [node index + 5] - 2nd Connected Node Name                                                      (if applicable) <br/>
    /// [node index + 6] - 2nd Connected Node Path weight                                               (if applicable) <br/>
    /// [node index + 7] - 3rd Connected Node Name                                                      (if applicable) <br/>
    /// [node index + 8] - 3rd Connected Node Path weight                                               (if applicable) <br/>
    /// [node index + 9] - 4th Connected Node Name                                                      (if applicable) <br/>
    /// [node index + 10] - 4th Connected Node Path weight                                              (if applicable) <br/>
    /// ----------------------------------------------------------------------------------------------------------------<br/>
    /// (node index + 11) would be the next node 
    /// All Nodes are connected to at least one other node but indexes 5 - 10 may be unused if so will be set to a value of -1 
    /// </summary>
    /// <returns></returns>
    public static NativeArray<int> ToBlittable(this List<Node> nodes)
    {
        NativeArray<int> nodeDetailsBlittable = new NativeArray<int>(nodes.Count * 11, Allocator.Persistent);
        for (int nodeIndex = 0; nodeIndex < nodes.Count * 11; nodeIndex += 11)
        {
            int i = nodeIndex / 11;
            nodeDetailsBlittable[nodeIndex] = (int)nodes[i].Name.ToCharArray()[0];
            nodeDetailsBlittable[nodeIndex + 1] = (int)nodes[i].Position.x;
            nodeDetailsBlittable[nodeIndex + 2] = (int)nodes[i].Position.y;
            
            int connectedNodesOffset = nodeIndex + 3; // Index at which connected Node Data starts
            for (int connectedNodeIndex = 0; connectedNodeIndex < 4; connectedNodeIndex++)
            {
                if (connectedNodeIndex < nodes[i].ConnectedNodes.Count)
                {
                    nodeDetailsBlittable[connectedNodesOffset + (connectedNodeIndex * 2)] = (int)nodes[i].ConnectedNodes[connectedNodeIndex].NodeName.ToCharArray()[0];
                    nodeDetailsBlittable[connectedNodesOffset + (connectedNodeIndex * 2) + 1] = (int)nodes[i].ConnectedNodes[connectedNodeIndex].PathWeight;
                }
                else
                {
                    nodeDetailsBlittable[connectedNodesOffset + (connectedNodeIndex * 2)] = -1;
                    nodeDetailsBlittable[connectedNodesOffset + (connectedNodeIndex * 2) + 1] = -1;
                }
            }
        }

        return nodeDetailsBlittable;
    }

    // Convert back from blittable format Node data to regular Node class type
    public static Node[] ToNodes(this NativeArray<int> nodeDetailsBlittable)
    {
        Node[] nodes = new Node[nodeDetailsBlittable.Length / 11];
        for (int i = 0; i < nodeDetailsBlittable.Length; i += 11)
        {
            Node node = new Node(
                new Vector2(nodeDetailsBlittable[i + 1], nodeDetailsBlittable[i + 2]),
                ((char)nodeDetailsBlittable[i]).ToString());

            for (int m = i + 3; m < i + 10; m += 2)
            {
                if (nodeDetailsBlittable[m] != -1)
                {
                    node.ConnectedNodes.Add(new NodePathPair(((char)nodeDetailsBlittable[m]).ToString(), nodeDetailsBlittable[m + 1]));
                }
            }

            nodes[i / 11] = node;
        }

        return nodes;
    }

    // Converting PathFinder Position data to blittable format
    public static NativeArray<float> ToBlittable(this List<SeparatePathFinderAI> pathFinders)
    {
        NativeArray<float> pathFinderPositionsBlittable = new NativeArray<float>(pathFinders.Count * 3, Allocator.Persistent);
        for (int i = 0; i < pathFinders.Count; i++)
        {
            pathFinderPositionsBlittable[i * 3] = pathFinders[i].transform.position.x;
            pathFinderPositionsBlittable[(i * 3) + 1] = pathFinders[i].transform.position.y;
            pathFinderPositionsBlittable[(i * 3) + 2] = pathFinders[i].transform.position.z;
        }

        return pathFinderPositionsBlittable;
    }

    // Convert back from blittable format position data to regular Vector3 type
    public static Vector3[] ToPositions(this NativeArray<float> pathFinderPositionsBlittable)
    {
        Vector3[] pathFindersPositions = new Vector3[pathFinderPositionsBlittable.Length / 3];
        for (int i = 0; i < pathFinderPositionsBlittable.Length; i += 3)
        {
            pathFindersPositions[i / 3] = new Vector3(
                pathFinderPositionsBlittable[i],
                pathFinderPositionsBlittable[i + 1],
                pathFinderPositionsBlittable[i + 2]);
        }
        
        return pathFindersPositions;
    }
}