using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System;

public class MoveTo : MonoBehaviour
{
    // the target spot that the drone will move towards
    public GameObject targetPrefab;
    // map containing discretized view of the environment which has been explored so far
    private char[,] map;
    // radius of the view circle around the drone
    private int radius;
    private int mapSize;
    private int cellSize;
    private Vector3[] directions = new Vector3[8];
    private char empty, occupied, unkown;
    private String targetTag;
    

    void Start()
    {
        // the drone
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        GameObject targetInstance;
        // diameter 18 -> each cell in the map = 6 X 6 unit
        // these numbers simplify the calculations
        radius = 9;
        mapSize = 6;
        cellSize = radius * 2 / 3;
        map = new char[mapSize, mapSize];

        empty = 'O';
        occupied = 'X';
        unkown = '?';
        targetTag = "Target";

        directions[0] = new Vector3(0, 0, 1);
        directions[1] = new Vector3(0, 0, -1);
        directions[2] = new Vector3(1, 0, 0);
        directions[3] = new Vector3(-1, 0, 0);
        directions[4] = new Vector3(1, 0, 1);
        directions[5] = new Vector3(1, 0, -1);
        directions[6] = new Vector3(-1, 0, 1);
        directions[7] = new Vector3(-1, 0, -1);

        // initial target, to start exploring
        targetInstance = Instantiate(targetPrefab);
        targetInstance.transform.position = agent.transform.position;
        agent.destination = targetInstance.transform.position;

        for (int i = 0; i < mapSize; i++)
            for (int j = 0; j < mapSize; j++)
                map[i, j] = unkown;

        PrintMap();

        // mark the current cell as free
        MarkCell((int)agent.transform.position.x, (int)agent.transform.position.z, empty);

    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.tag == targetTag)
        {
            // destroy on reaching the object
            Destroy(collision.gameObject);

            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            GameObject targetInstance;

            // generate a position outside the view circle
            // this will be the position of the next target
            System.Random rnd = new System.Random();

            // TODO
            // make sure the next position is an unexplored spot
            float xt = rnd.Next(-1*radius, radius+1) + (2*radius);
            float zt = rnd.Next(-1*radius, radius+1) + (2*radius);

            // the new destination
            targetInstance = Instantiate(targetPrefab);
            targetInstance.transform.position = new Vector3(xt, 1, zt);
            agent.destination = targetInstance.transform.position;

            PrintMap();
        }

    }

    void PrintMap()
    {
        String s = "";
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
                s += " " + map[i, j];
            s += "\n";
        }
        Debug.Log(s);
    }
    
    void Update()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        RaycastHit hit;
        int range = radius;

        // Raycast in the 8 directions
        for (int i = 0; i < 8; i++)
        {
            if (Physics.Raycast(agent.transform.position, directions[i], out hit, range))
            {
                // There's an object, mark this cell as occupied
                MarkCell((int) hit.transform.position.x, (int) hit.transform.position.z, occupied);
            }
            else
            {
                // mark this cell as free
                MarkCell((int) agent.transform.position.x + (int)(cellSize * directions[i].x), (int) agent.transform.position.z + (int)(cellSize * directions[i].x), empty);
            }
        }
    }

    void MarkCell(int x, int z, char c)
    {
        x /= cellSize;
        z /= cellSize;
        if(map[x, z] != occupied)
            map[x, z] = c;
    }
    
}