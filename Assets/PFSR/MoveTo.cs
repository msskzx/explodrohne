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
    private ArrayList targetLocations;

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
        targetLocations = new ArrayList();

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


        // pool of all coordinates to explore
        Vector3[] tmparr = new Vector3[mapSize * mapSize];
        for (int i = 0; i < mapSize; i++)
            for (int j = 0; j < mapSize; j++)
                tmparr[mapSize * i + j] = new Vector3(i, 1, j);

        // shuffle the locations
        System.Random rnd = new System.Random();
        for (int i = 0; i < tmparr.Length; i++)
        {
            Vector3 tmpVector = tmparr[0];
            int ind = rnd.Next(0, tmparr.Length);
            tmparr[0] = tmparr[ind];
            tmparr[ind] = tmpVector;
        }

        // fill the targetLocations
        for (int i = 0; i < tmparr.Length; i++)
            targetLocations.Add(tmparr[i]);

        // mark the current cell as free
        MarkCell((int)agent.transform.position.x, (int)agent.transform.position.z, empty);
    }

    void OnCollisionEnter(Collision collision)
    {
        // hit a target and you have more places to explore
        if (collision.collider.tag == targetTag && targetLocations.Count > 0)
        {
            // destroy on reaching the object
            Destroy(collision.gameObject);

            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            GameObject targetInstance;

            // get a new target destination, remove it from the array
            targetInstance = Instantiate(targetPrefab);
            Vector3 nextPosition = (Vector3)targetLocations[0];
            targetLocations.RemoveAt(0);
            targetInstance.transform.position = new Vector3(nextPosition.x * cellSize, 1, nextPosition.z * cellSize);
            agent.destination = targetInstance.transform.position;

            PrintMap();

            // on reaching a target, remove all cells in the visiblity circle from the locations array
            for (int i = 0; i < targetLocations.Count; i++)
            {
                Vector3 tmp = (Vector3)targetLocations[i];
                if (map[(int)tmp.x, (int)tmp.z] != unkown)
                    targetLocations.RemoveAt(i);
            }
        }

    }

    // print the map
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
                MarkCell((int)hit.transform.position.x, (int)hit.transform.position.z, occupied);

            }
            else
            {
                // mark this cell as free
                MarkCell((int)agent.transform.position.x + (int)(cellSize * directions[i].x), (int)agent.transform.position.z + (int)(cellSize * directions[i].x), empty);
            }
        }
    }

    // mark this cell as free or occupied
    void MarkCell(int x, int z, char c)
    {
        x /= cellSize;
        z /= cellSize;
        if (x > -1 && x < mapSize && z > -1 && z < mapSize && map[x, z] != occupied)
            map[x, z] = c;
    }

}

