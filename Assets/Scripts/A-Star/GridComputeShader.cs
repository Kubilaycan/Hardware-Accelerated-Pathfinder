using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridComputeShader : MonoBehaviour
{
    public LayerMask obstacleMask;
    public int gridWorldSize;
    public float nodeRadius;
    private NodeStruct[,] grid;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY;
    public List<NodeStruct> path;
    public ComputeShader computeShader;
    public NodeStruct[] data;

    public NodeStruct[] openSet;
    public NodeStruct[] closedSet;

    public Transform startPos;
    private NodeStruct startNode;
    public Transform endPos;
    private NodeStruct endNode;

    private void Start() {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize / nodeDiameter);

        CreateGrid();
    }

    private void CreateGrid(){
        grid = new NodeStruct[gridSizeX, gridSizeY];
        data = new NodeStruct[gridSizeX * gridSizeY];

        openSet = new NodeStruct[100];
        closedSet = new NodeStruct[100];

        Vector3 worldBottomLeft = transform.position 
            + Vector3.left * gridWorldSize / 2 
            + Vector3.back * gridWorldSize / 2;

        for(int x = 0; x < gridSizeX; x++){
            for(int y = 0; y < gridSizeY; y++){
                Vector3 worldPoint = worldBottomLeft 
                    + Vector3.right   * (x * nodeDiameter + nodeRadius) 
                    + Vector3.forward * (y * nodeDiameter + nodeRadius);
                    bool obstacle = Physics.CheckSphere(worldPoint, nodeRadius, obstacleMask);
                    
                    var tempNode = new NodeStruct();
                    tempNode.gridX = x;
                    tempNode.gridY = y;
                    tempNode.position = worldPoint;
                    tempNode.isObstacle = obstacle ? 1 : 0;
                    tempNode.parentX = -1;
                    tempNode.parentY = -1;

                    grid[x, y] = tempNode;
                    data[x * gridSizeY + y] = tempNode;
            }
        }

        for(int i = 0; i < 100; i++){
            var tempNode = new NodeStruct();
            tempNode.gridX = -1;
            tempNode.gridY = -1;
            openSet[i] = tempNode;
            closedSet[i] = tempNode;
        }

        SendDataToShader();
    }

    private void SendDataToShader(){
        int totalSize = TotalSize();
        ComputeBuffer dataBuffer = new ComputeBuffer(data.Length, totalSize);
        dataBuffer.SetData(data);

        ComputeBuffer openSetBuffer = new ComputeBuffer(openSet.Length, totalSize);
        openSetBuffer.SetData(openSet);

        ComputeBuffer closedSetBuffer = new ComputeBuffer(closedSet.Length, totalSize);
        closedSetBuffer.SetData(closedSet);

        startNode = NodeFromWorldPosition(startPos.position);
        endNode = NodeFromWorldPosition(endPos.position);
        NodeStruct[] positions = new NodeStruct[2];
        positions[0] = startNode; positions[1] = endNode;
        ComputeBuffer positionBuffer = new ComputeBuffer(positions.Length, totalSize);
        positionBuffer.SetData(positions);

        computeShader.SetBuffer(0, "data", dataBuffer);
        computeShader.SetBuffer(0, "openSet", openSetBuffer);
        computeShader.SetBuffer(0, "closedSet", closedSetBuffer);
        computeShader.SetBuffer(0, "positionBuffer", positionBuffer);

        computeShader.SetInt("gridSizeX", gridSizeX);
        computeShader.SetInt("gridSizeY", gridSizeY);

        computeShader.Dispatch(0, data.Length / 10, 1, 1);

        dataBuffer.GetData(data);
        dataBuffer.Dispose();

        openSetBuffer.GetData(openSet);
        openSetBuffer.Dispose();

        closedSetBuffer.GetData(closedSet);
        closedSetBuffer.Dispose();

        positionBuffer.Dispose();

        PrintDataWithParent();
    }

    private void PrintOpenSet(){
        int i = 0;
        while(openSet[i].gridX != -1 && openSet[i].gridY != -1){
            Debug.Log(openSet[i].gridX.ToString() + " " + openSet[i].gridY.ToString() + " " + openSet[i].isObstacle);
            i++;
        }
    }

    private void PrintClosedSet(){
        int i = 0;
        while(closedSet[i].gridX != -1 && closedSet[i].gridY != -1){
            Debug.Log(closedSet[i].gridX.ToString() + " " + closedSet[i].gridY.ToString() + " " + closedSet[i].isObstacle);
            i++;
        }
    }

    void PrintDataWithParent(){
        foreach(NodeStruct item in data){
            if(item.parentX != -1 || item.parentY != -1){
                Debug.Log(item.gridX + " " + item.gridY);
            }
        }
    }

    private int TotalSize(){
        int vector3size = sizeof(float) * 3;
        int intSize = sizeof(int);
        int totalSize = intSize + vector3size + (intSize * 7);

        return totalSize;
    }

    public NodeStruct NodeFromWorldPosition(Vector3 worldPosition){
        float percentX = (worldPosition.x + gridWorldSize/2) / gridWorldSize;
        float percentY = (worldPosition.z + gridWorldSize/2) / gridWorldSize;

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return data[x * gridSizeX + y];
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize, 1.0f, gridWorldSize));

        if(data != null){
            foreach(NodeStruct node in data){
                if(node.isObstacle == 1){
                    Gizmos.color = Color.red;
                }else{
                    Gizmos.color = Color.green;
                }
                if(node.gridX == 2 && node.gridY == 3){
                    Gizmos.color = Color.black;
                }
                Gizmos.DrawWireCube(node.position, Vector3.one * (nodeDiameter * 0.9f));
            }
        }
    }
}
