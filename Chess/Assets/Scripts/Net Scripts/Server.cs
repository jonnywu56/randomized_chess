using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System;

// Contains server logic
public class Server : MonoBehaviour
{
	// Sets implementation for singleton
    public static Server Instance { set; get; }
    private void Awake(){
    	Instance = this;
    }

	// Tracks variables for connection
    public NetworkDriver driver;
    private NativeList<NetworkConnection> connections;

	// Tracks whether connection active and keepAlive variables
    private bool isActive = false;
    private const float keepAliveTickRate = 20.0f;
    private float lastKeepAlive;

	// Listened to by server function to notify of connection dropping
    public Action connectionDropped;

    // Initializes server connection on port
    public void Init(ushort port){
    	driver = NetworkDriver.Create();
    	NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;
    	endpoint.Port = port;

    	if(driver.Bind(endpoint) != 0){
    		Debug.Log("Unable to bind on port " + endpoint.Port);
    		return;
    	} else {
    		driver.Listen();
    	}

    	connections = new NativeList<NetworkConnection>(2, Allocator.Persistent);
    	isActive = true;
    }

	// Closes server connection and resets variables
    public void Shutdown(){
    	if(isActive) {
    		driver.Dispose();
    		connections.Dispose();
    		isActive = false;
    	}
    }

	// Called when server object destroyed, shuts down server
    public void OnDestroy(){
    	Shutdown();	
    }

	// Called every frame to check for new messages
    public void Update(){
		// Check if client is active
    	if(!isActive){
    		return;
    	}

		// Send message to keep connection alive
    	KeepAlive();

		// Update driver and check if connection is active
    	driver.ScheduleUpdate().Complete();
    	CleanupConnections();
    	AcceptNewConnections();

		// Update messages
		try {
			UpdateMessagePump();
		}
		catch (ObjectDisposedException e) {
			// Used to catch a common error created by quitting after hosting
			Debug.Log("ObjectDisposedException caught");
			return;
		}
    	

    }

    // Sends message based on keepAliveTickRate to keep connection alive
    private void KeepAlive(){
        if (Time.time - lastKeepAlive > keepAliveTickRate)
        {
            lastKeepAlive = Time.time;
            Broadcast(new NetKeepAlive());
        }
    }

	// Closes all active connections
    private void CleanupConnections(){
    	for (int i = 0; i < connections.Length; i++){
    		if (!connections[i].IsCreated){
    			connections.RemoveAtSwapBack(i);
    			i--;
    		}
    	}
    }

	// Accepts incomding connections
    private void AcceptNewConnections() {
    	NetworkConnection c;
    	while ((c = driver.Accept()) != default(NetworkConnection))
    	{
    		connections.Add(c);
    	}
    }

	// Reads events from connection
    private void UpdateMessagePump(){
    	DataStreamReader stream;

		// Checks each connection for new events
    	for(int i = 0; i < connections.Length; i++){
    		NetworkEvent.Type cmd;

			// Reads type of event from connection
    		while((cmd = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
    		{
				// Processes data
    			if (cmd == NetworkEvent.Type.Data){
    				NetUtility.OnData(stream, connections[i], this);
				// Closes connection and cleans up
    			} else if (cmd == NetworkEvent.Type.Disconnect){
    				Debug.Log("Client disconnected from server");
    				connections[i] = default(NetworkConnection);
    				connectionDropped?.Invoke();
    				Shutdown();
    			}	
    		}
    	}
    }

    // Sends message to specific connection
    public void SendToClient(NetworkConnection connection, NetMessage msg){
    	DataStreamWriter writer;
    	driver.BeginSend(connection, out writer);
    	msg.Serialize(ref writer);
    	driver.EndSend(writer);
    }

	// Broadcasts message to all clients
    public void Broadcast(NetMessage msg){
    	for (int i = 0; i < connections.Length; i++){
    		if (connections[i].IsCreated){
				Debug.Log($"Sending {msg.Code} to : {connections[i].InternalId}");
				SendToClient(connections[i], msg);
			}				
    	}
	}
    
}