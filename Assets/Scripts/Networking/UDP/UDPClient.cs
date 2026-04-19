using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UDPClient : MonoBehaviour, IClient
{
    private UdpClient udpClient; // UDP client to handle network communication
    private IPEndPoint remoteEndPoint; // Endpoint to identify the remote server
    public bool isServerConnected = false; // Flag to check if the client is connected to the server

    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    public bool isConnected { get; private set; }

    public async Task ConnectToServer(string ipAddress, int port)
    {
        udpClient = new UdpClient(); // Creates a new instance of the UdpClient class
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);//The remote endpoint is the server's IP address and port number that the client will connect to

        isConnected = true;
        _ = ReceiveLoop(); // Starts the receive loop in a separate task to continuously listen for incoming messages from the server without blocking the main thread

        await SendMessageAsync("CONNECT"); // Sends an initial message to the server to confirm the handshake
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (isConnected)
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();// Waits for incoming messages from the server asynchronously
                string message = Encoding.UTF8.GetString(result.Buffer); // Converts the received bytes into a string message using UTF-8 encoding

                if (message == "CONNECTED")
                {
                    Debug.Log("[Client] Server Answered");
                    OnConnected?.Invoke(); // Invokes the OnConnected event, notifying any subscribed listeners that a client has connected
                    continue; // Skip the rest of the loop and wait for the next message
                }

                Debug.Log("[Client] Received: " + message);
                OnMessageReceived?.Invoke(message);//Invokes the OnMessageReceived event, passing the received message to any subscribed listeners
            }
        }
        finally
        {
            Disconnect();
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (!isConnected) // Checks if there is an active connection to the server
        {
            Debug.Log("[Client] Not connected to server."); 
            return;
        }

        byte[] data = Encoding.UTF8.GetBytes(message);// Converts the message string into a byte array
        await udpClient.SendAsync(data, data.Length, remoteEndPoint); // Sends the byte array to the server using UDP asynchronously

        Debug.Log("[Client] Sent: " + message);
    }

    public void Disconnect()
    {
        if (!isConnected)
        {
            Debug.Log("[Client] The client is not connected");
            return;
        }
            
        isConnected = false;

        udpClient?.Close();
        udpClient?.Dispose();// Closes the UDP client and releases any resources associated with it
        udpClient = null;

        Debug.Log("[Client] Disconnected");
        OnDisconnected?.Invoke();// Invokes the OnDisconnected event, notifying any subscribed listeners that the client has disconnected from the server
    }

    private async void OnDestroy()
    {
        Disconnect();
        await Task.Delay(100);
    }
}
