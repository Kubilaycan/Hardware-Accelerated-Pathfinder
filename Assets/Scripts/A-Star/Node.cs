using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public bool isObstacle;
    public Vector3 position;

    public int gridX;
    public int gridY;

    public int gCost;
    public int hCost;
    public int fCost{
        get{
            return gCost + hCost;
        }
    }

    public Node parent;

    public Node(bool isObstacle, Vector3 position, int gridX, int gridY){
        this.isObstacle = isObstacle;
        this.position = position;
        this.gridX = gridX;
        this.gridY = gridY;
    }
}
