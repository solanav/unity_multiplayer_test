using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;

public class Network : MonoBehaviour
{
    private const float MINDISTANCE = 0.1f;
    private const float MINANGLE = 1f;

    public GameObject enemy;
    public Dictionary<String, GameObject> enemies;
    public Vector3 lastPos;
    public Quaternion lastRot;
    public GameObject player;
    public EzNet ez;

    // Start is called before the first frame update
    void Start()
    {
        ez = new EzNet();

        lastPos = new Vector3(0, 0, 0);
        player = GameObject.Find("Player");

        enemies = new Dictionary<string, GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        this.updateEnemies();
        this.updateSelf();
    }

    void updateEnemies()
    {
        string data = ez.recvData();
        if (!data.Equals(string.Empty))
        {
            string[] parsed = data.Split(',');
            if (parsed.Length == 8)
            {
                // Get hash
                string id = parsed[0];
                
                // Get position
                float x = float.Parse(parsed[1]);
                float y = float.Parse(parsed[2]) + enemy.transform.lossyScale.y / 2;
                float z = float.Parse(parsed[3]);
                
                // Get rotation
                float rx = float.Parse(parsed[4]);
                float ry = float.Parse(parsed[5]);
                float rz = float.Parse(parsed[6]);
                float rw = float.Parse(parsed[7]);

                // If it exists, update the position, else add a new enemy
                if (enemies.ContainsKey(id))
                {
                    enemies[id].transform.position = new Vector3(x, y, z);
                    enemies[id].transform.rotation = new Quaternion(rx, ry, rz, rw);
                }
                else
                {
                    addPlayer(id, new Vector3(x, y, z));
                }
            }
        }
    }

    void updateSelf()
    {
        Debug.Log(player.transform.GetChild(0).rotation);

        // Only update if we changed position
        Vector3 distance = lastPos - player.transform.position;
        float angle = Quaternion.Angle(lastRot, player.transform.GetChild(0).rotation);
        Debug.Log(angle);
        if (Mathf.Abs(distance.x) > MINDISTANCE ||
            Mathf.Abs(distance.y) > MINDISTANCE ||
            Mathf.Abs(distance.z) > MINDISTANCE ||
            angle > MINANGLE)
        {
            ez.sendData(string.Format("{0},{1},{2},{3},{4},{5},{6}",
                player.transform.position.x,
                player.transform.position.y,
                player.transform.position.z,
                player.transform.GetChild(0).rotation.x,
                player.transform.GetChild(0).rotation.y,
                player.transform.GetChild(0).rotation.z,
                player.transform.GetChild(0).rotation.w));

            lastPos = player.transform.position;
            lastRot = player.transform.GetChild(0).rotation;
        }
    }

    void addPlayer(string id, Vector3 pos)
    {
        Debug.Log("Adding player " + id);

        if (pos == null)
            enemies.Add(id, Instantiate(enemy, new Vector3(0, 0, 0), Quaternion.identity));
        else
            enemies.Add(id, Instantiate(enemy, pos, Quaternion.identity));
    }
}

public class EzNet
{
    public const string server_name = "127.0.0.1"; // Server ip
    public const int server_port = 4343; // Server port
    public IPEndPoint server_rep; // Server remote ip end point

    UdpClient udpClient;

    public EzNet()
    {
        udpClient = new UdpClient((int) UnityEngine.Random.Range(1100.0f, 65000.0f));
        server_rep = new IPEndPoint(IPAddress.Parse(server_name), server_port);
    }

    public void sendData(string data)
    {
        try
        {
            udpClient.Connect(server_name, server_port);
            byte[] sendBytes = Encoding.ASCII.GetBytes(data);
            udpClient.Send(sendBytes, sendBytes.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to send data to server: " + e);
        }
    }

    public String recvData()
    {
        try
        {
            udpClient.Connect(server_name, server_port);
            if (udpClient.Available > 0)
            {
                byte[] recvBytes = udpClient.Receive(ref server_rep);
                return Encoding.ASCII.GetString(recvBytes);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to recv data from server: " + e);
        }

        return string.Empty;
    }
}