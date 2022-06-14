using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pathfinding : MonoBehaviour
{
    Grid grid;

    public Transform source;
    public Transform target;
    
    private float shaderCalculationTime = 0;
    private float cpuCalculationTime = 0;

    public Text cpuText;
    public Text gpuText;

    public ComputeShader computeShader;

    private void Awake() {
        grid = GetComponent<Grid>();
    }

    public void ButtonMethod(){
        shaderCalculationTime = 0;
        cpuCalculationTime = 0;
        FindPath(source.position, target.position);
        FindPathViaShader(source.position, target.position);
        SetTexts();
    }

    private void SetTexts(){
        cpuText.text = cpuCalculationTime + "ms";
        gpuText.text = shaderCalculationTime + "ms";
    }

    void FindPath(Vector3 startPos, Vector3 endPos){
        Node startNode = grid.NodeFromWorldPosition(startPos);
        Node endNode = grid.NodeFromWorldPosition(endPos);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closeSet = new HashSet<Node>();
        openSet.Add(startNode);

        while(openSet.Count > 0){
            var t1 = Time.realtimeSinceStartup;
            Node currentNode = openSet[0];

            for(int i = 1; i < openSet.Count; i++){
                if(openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost){
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closeSet.Add(currentNode);

            if(currentNode == endNode){
                RetracePath(startNode, endNode);
                return;
            }

            foreach(Node neighbour in grid.GetNeighbours(currentNode)){
                bool isNeighbourInClosedSet = closeSet.Contains(neighbour); 
                if(neighbour.isObstacle || isNeighbourInClosedSet){
                    continue;
                }

                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                bool isNeighbourInOpeSet = openSet.Contains(neighbour);
                if(newMovementCostToNeighbour < neighbour.gCost || !isNeighbourInOpeSet){
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, endNode);
                    neighbour.parent = currentNode;

                    if(!isNeighbourInOpeSet){
                        openSet.Add(neighbour);
                    }
                }
            }
            var t2 = Time.realtimeSinceStartup;
            cpuCalculationTime += (t2 - t1);
        }
    }

    void FindPathViaShader(Vector3 startPos, Vector3 endPos){
        Node startNode = grid.NodeFromWorldPosition(startPos);
        Node endNode = grid.NodeFromWorldPosition(endPos);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closeSet = new HashSet<Node>();
        openSet.Add(startNode);

        while(openSet.Count > 0){
            Node currentNode = openSet[0];

            for(int i = 1; i < openSet.Count; i++){
                if(openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost){
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closeSet.Add(currentNode);

            if(currentNode == endNode){
                RetracePath(startNode, endNode);
                return;
            }

            foreach(Node neighbour in grid.GetNeighbours(currentNode)){
                bool isNeighbourInClosedSet = closeSet.Contains(neighbour); 
                bool isNeighbourInOpeSet = openSet.Contains(neighbour);
                
                int response;
                Node newNeighbour = new Node();
                (response, newNeighbour) = ShaderCalculation(isNeighbourInClosedSet, isNeighbourInOpeSet, neighbour, currentNode, endNode);

                neighbour.parent = newNeighbour.parent;

                if(response == -1){
                    continue;
                }
                if(response == 1){
                    openSet.Add(neighbour);
                }
            }
        }
    }

    (int, Node) ShaderCalculation(bool isNeighbourInClosedSet, bool isNeighbourInOpeSet, Node neighbour, Node current, Node endNode){
        NodeStruct[] inputBufferData = new NodeStruct[3];
        inputBufferData[0] = toNodeStruct(neighbour);
        inputBufferData[1] = toNodeStruct(current);
        inputBufferData[2] = toNodeStruct(endNode);
        ComputeBuffer inputBuffer = new ComputeBuffer(inputBufferData.Length, TotalSize());
        inputBuffer.SetData(inputBufferData);
        computeShader.SetBuffer(0, "inputBuffer", inputBuffer);

        int[] outputBufferData = new int[1];
        ComputeBuffer outputBuffer = new ComputeBuffer(outputBufferData.Length, sizeof(int));
        outputBuffer.SetData(outputBufferData);
        computeShader.SetBuffer(0, "integerOutputBuffer", outputBuffer);

        computeShader.SetInt("isNeighbourInClosedSet", isNeighbourInClosedSet ? 1 : 0);
        computeShader.SetInt("isNeighbourInOpeSet",    isNeighbourInOpeSet    ? 1 : 0);
        var t1 = Time.realtimeSinceStartup;
        computeShader.Dispatch(0, inputBufferData.Length, 1, 1);
        var t2 = Time.realtimeSinceStartup;
        shaderCalculationTime += (t2 - t1);
        inputBuffer.GetData(inputBufferData);
        inputBuffer.Dispose();

        outputBuffer.GetData(outputBufferData);
        outputBuffer.Dispose();


        if(inputBufferData[0].parentX != -1 && inputBufferData[0].parentY != -1){
            // parent
            neighbour.parent = grid.grid[inputBufferData[0].parentX, inputBufferData[0].parentY];
        }
        return (outputBufferData[0], neighbour);
    }

    void RetracePath(Node startNode, Node endNode){
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while(currentNode != startNode){
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        grid.path = path;
    }

    int GetDistance(Node nodeA, Node nodeB){
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if(distX > distY){
            return 14 * (distY) + 10 * (distX - distY);
        }
        return 14 * (distX) + 10 * (distY - distX);
    }

    private int TotalSize(){
        int vector3size = sizeof(float) * 3;
        int intSize = sizeof(int);
        int totalSize = intSize + vector3size + (intSize * 7);

        return totalSize;
    }

    public NodeStruct toNodeStruct(Node node){
        NodeStruct temp = new NodeStruct();
        temp.isObstacle = node.isObstacle ? 1 : 0;
        temp.position = node.position;
        temp.gridX = node.gridX;
        temp.gridY = node.gridY;
        temp.gCost = node.gCost;
        temp.hCost = node.hCost;
        temp.fCost = node.fCost;
        temp.parentX = -1;
        temp.parentY = -1;
        return temp;
    }
}