using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NodeStruct{
        public int isObstacle; //1 = obstacle, 0 = empty
        public Vector3 position;

        public int gridX;
        public int gridY;

        public int gCost;
        public int hCost;
        public int fCost;

        public int parentX;
        public int parentY;
};
