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
    private ArrayList[] graph;

    private Vector3[] directions;
    private String targetTag;
    private String obstacleTag;
    private ArrayList targetLocations;
    private Stack routeToTarget;

    // radius of the view circle around the drone
    private const int range = 8;
    private const int cellSize = 6;
    private const char empty = 'O', occupied = 'X', unkown = '?', unreachable = 'U';
    private const int MAX_VALUE = 100000;
    private int mapSize;
    private int V;



    void Start()
    {
        // the drone
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        // radius of vision
        mapSize = 6;
        map = new char[mapSize, mapSize];
        targetLocations = new ArrayList();
        routeToTarget = new Stack();
        graph = new ArrayList[mapSize * mapSize];
        V = mapSize * mapSize;
        targetTag = "Target";
        obstacleTag = "Obstacle";

        // raycast directions
        directions = new Vector3[8];
        directions[0] = new Vector3(0, 0, 1);
        directions[1] = new Vector3(0, 0, -1);
        directions[2] = new Vector3(1, 0, 0);
        directions[3] = new Vector3(-1, 0, 0);
        directions[4] = new Vector3(1, 0, 1);
        directions[5] = new Vector3(1, 0, -1);
        directions[6] = new Vector3(-1, 0, 1);
        directions[7] = new Vector3(-1, 0, -1);

        // initialize the map
        for (int i = 0; i < mapSize; i++)
            for (int j = 0; j < mapSize; j++)
            {
                map[i, j] = unkown;
                graph[i * cellSize + j] = new ArrayList();
            }

        // initial target, to start exploring
        MarkCell(ToIndices(agent.transform.position), empty);
        Raycast8();
        PrintMap();
        NewTarget();
    }



    void Update()
    {
        Raycast8();
        RemoveCandidates();
    }



    Boolean DoneExploring()
    {
        return targetLocations.Count == 0;
    }



    void OnCollisionEnter(Collision collision)
    {
        // if the drone hit a target and there are more places to explore
        if (collision.collider.tag == targetTag)
        {
            // destroy on reaching the object
            Destroy(collision.gameObject);

            // create a new target
            if (!DoneExploring())
            {
                NewTarget();
            }

            PrintMap();
        }
    }


    // remove all cells that don't have adjacent unknown cells from the target locations
    void RemoveCandidates()
    {
        for (int i = 0; i < targetLocations.Count; i++)
        {
            Vector3 tmp = (Vector3)targetLocations[i];
            if (!HasAdjacentUnknown(tmp))
            {
                targetLocations.RemoveAt(i);
            }
        }
    }



    // creating a new target destination
    void NewTarget()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        GameObject targetInstance;

        // to get the nearest cell
        int index = targetLocations.Count - 1;

        // get a new target destination
        targetInstance = Instantiate(targetPrefab);
        Vector3 tmpVector = (Vector3)targetLocations[index];
        Vector3 nextPosition = new Vector3(tmpVector.x * cellSize, 1, tmpVector.z * cellSize);


        // find a short path and add targets along that path
        if (routeToTarget.Count == 0)
        {
            FindRouteToTarget((int)tmpVector.x * cellSize + (int)tmpVector.z);
            targetLocations.RemoveAt(index);
        }

        // set target position and agent destination to the next point on the path
        nextPosition = (Vector3)routeToTarget.Pop();
        targetInstance.transform.position = nextPosition;
        agent.destination = targetInstance.transform.position;

    }



    void FindRouteToTarget(int src)
    {
        
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



    // Raycast in 8 directions
    void Raycast8()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        RaycastHit hit;

        // Raycast in the 8 directions
        for (int i = 0; i < directions.Length; i++)
        {
            if (Physics.Raycast(agent.transform.position, directions[i], out hit, range))
            {
                if (hit.collider.tag == obstacleTag)
                {
                    // There's an object, mark this cell as occupied
                    MarkCell(ToIndices(hit.transform.position), occupied);
                }
            }
            else
            {
                // mark this cell as free
                int tmpx = (int)agent.transform.position.x + (int)(range * directions[i].x),
                    tmpz = (int)agent.transform.position.z + (int)(range * directions[i].z);
                Vector3 seenCell = new Vector3(tmpx, 1, tmpz);

                if (CheckBoundaries(tmpx / cellSize, tmpz / cellSize))
                {
                    // add this cell to the candidate target locations if it has adjacent unkown cells, and if it wasn't added
                    // to the candidate list before.. i.e. if it has ?
                    if (HasAdjacentUnknown(ToIndices(seenCell)) && map[tmpx / cellSize, tmpz / cellSize] == '?')
                    {
                        targetLocations.Add(new Vector3(tmpx / cellSize, 1, tmpz / cellSize));
                    }
                    MarkCell(ToIndices(seenCell), empty);
                    AddEdge(ToIndices(agent.transform.position), ToIndices(seenCell));
                }
            }
        }
    }



    Vector3 ToIndices(Vector3 a)
    {
        return new Vector3(a.x / cellSize, 1, a.z / cellSize);
    }



    Boolean NotInCandidates(int x, int y, int z)
    {
        for (int i = 0; i < targetLocations.Count; i++)
        {
            Vector3 tmp = (Vector3)targetLocations[i];
            if (tmp.x == x && tmp.y == y && tmp.z == z)
                return false;
        }
        return true;
    }



    // given indices of the cell as a Vector
    Boolean HasAdjacentUnknown(Vector3 a)
    {
        int i = (int)a.x,
            j = (int)a.z;
        return (CheckBoundaries(i, j + 1) && map[i, j + 1] == '?')
            || (CheckBoundaries(i, j - 1) && map[i, j - 1] == '?')
            || (CheckBoundaries(i + 1, j + 1) && map[i + 1, j + 1] == '?')
            || (CheckBoundaries(i + 1, j) && map[i + 1, j] == '?')
            || (CheckBoundaries(i + 1, j - 1) && map[i + 1, j - 1] == '?')
            || (CheckBoundaries(i - 1, j) && map[i - 1, j] == '?')
            || (CheckBoundaries(i - 1, j + 1) && map[i - 1, j + 1] == '?')
            || (CheckBoundaries(i - 1, j - 1) && map[i - 1, j - 1] == '?');
    }



    // given indices of the cell
    Boolean CheckBoundaries(int i, int j)
    {
        return i > -1 && i < mapSize && j > -1 && j < mapSize;
    }



    // mark this cell as free or occupied
    // given indices of the cells
    void MarkCell(Vector3 a, char c)
    {
        int x = (int)a.x,
            z = (int)a.x;
        if (map[x, z] != occupied)
            map[x, z] = c;
    }



    // given indeces of the cells as vectors
    void AddEdge(Vector3 a, Vector3 b)
    {
        int a1 = (int)a.x * cellSize + (int)a.z,
            b1 = (int)b.x * cellSize + (int)b.z;
        if (!graph[a1].Contains(b1))
        {
            graph[a1].Add(b1);
            graph[b1].Add(a1);
        }
    }

}

