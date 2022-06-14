using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public LayerMask obstacleMask;
    public int gridWorldSize;
    public float nodeRadius;
    public Node[,] grid;

    private float nodeDiameter;
    private int gridSizeX, gridSizeY;

    public List<Node> path;

    public ComputeShader computeShader;
    public NodeStruct[] data;

    private void Start() {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize / nodeDiameter);

        CreateGrid();
    }

    private void CreateGrid(){
        grid = new Node[gridSizeX, gridSizeY];
        data = new NodeStruct[gridSizeX *gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize/2 - Vector3.forward * gridWorldSize/2;

        for(int x = 0; x < gridSizeX; x++){
            for(int y = 0; y < gridSizeY; y++){
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool obstacle = Physics.CheckSphere(worldPoint, nodeRadius, obstacleMask);
                grid[x,y] = new Node(obstacle, worldPoint, x, y);
            }
        }
    }

    public List<Node> GetNeighbours(Node node){
        List<Node> neighbours = new List<Node>();

        for(int x = -1; x <= 1; x++){
            for(int y = -1; y <= 1; y++){
                if(x == y){
                    continue;
                }

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if(checkX >=0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY){
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    public Node NodeFromWorldPosition(Vector3 worldPosition){
        float percentX = (worldPosition.x + gridWorldSize/2) / gridWorldSize;
        float percentY = (worldPosition.z + gridWorldSize/2) / gridWorldSize;

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }


    private void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize, 1.0f, gridWorldSize));

        if(grid != null) {
            foreach(Node node in grid){
                bool isPath = false;
                if(node.isObstacle){
                    Gizmos.color = Color.red;
                }else{
                    Gizmos.color = Color.green;
                }
                if(path != null && path.Contains(node)){
                    Gizmos.color = Color.blue;
                    isPath = true;
                }
                if(isPath){
                    Gizmos.DrawCube(node.position, Vector3.one * (nodeDiameter * 0.9f));    
                }else{
                    Gizmos.DrawWireCube(node.position, Vector3.one * (nodeDiameter * 0.9f));
                }
            }
        }
    }
}
