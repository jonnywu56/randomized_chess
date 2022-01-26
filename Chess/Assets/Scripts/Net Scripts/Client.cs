using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System;
using TMPro;

// Contains client logic
public class Client : MonoBehaviour
{
	// Sets implementation for singleton
    public static Client Instance { set; get; }
    private void Awake(){
    	Instance = this;
    }

	// Tracks variables for connection
    public NetworkDriver driver;
    private NetworkConnection connection;
	private bool isActive = false;

	// Listened to by client function to notify of connection dropping
    public Action connectionDropped;

    // Initializes client connection on ip:port
    public void Init(string ip, ushort port){
    	driver = NetworkDriver.Create();
    	NetworkEndPoint endpoint = NetworkEndPoint.Parse(ip, port);
    	endpoint.Port = port;

    	connection = driver.Connect(endpoint);

    	isActive = true;

    	RegisterToEvent();
    }

	// Closes client connection and resets variables
    public void Shutdown(){
    	if(isActive) {
    		UnregisterToEvent();
    		driver.Dispose();
    		isActive = false;
    		connection = default(NetworkConnection);
    	}
    }

	// Called when client object destroyed, shuts down client
    public void OnDestroy(){
    	Shutdown();	
    }

	// Called every frame to check for new messages
    public void Update(){
		// Check if client is active
    	if(!isActive){
    		return;
    	}

		// Update driver and check if connection is active
    	driver.ScheduleUpdate().Complete();
    	CheckAlive();

		// Update messages
    	try {
			UpdateMessagePump();
		// Used to catch a common error created by quitting after hosting
		} catch (ObjectDisposedException e) {
			Debug.Log("ObjectDisposedException caught");
			return;
		}
    }

	// Shuts down connection if connection isn't created or active
    private void CheckAlive(){
    	if(!connection.IsCreated && isActive){
    		connectionDropped?.Invoke();
    		Shutdown();
    	}
    }

	// Reads events from connection
    private void UpdateMessagePump(){
    	DataStreamReader stream;
    	
		NetworkEvent.Type cmd;

		// Checks if connection is broken, sets game state text accordingly
		if(!connection.IsCreated){
			GameObject.Find("/Canvas/GameUI/GameState/Text").GetComponent<TextMeshProUGUI>().text="Opp Left";
			BoardLogic boardScript = GameObject.Find("/Board").GetComponent<BoardLogic>();
			Debug.Log("Opponent disconnected");
			// Calls MultiShutdown in a later frame
			boardScript.Invoke("MultiShutdown",0.2f);
		}

		// Reads type of event from connection
		while((cmd = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
		{
			if (cmd == NetworkEvent.Type.Connect){
				SendToServer(new NetWelcome());
			} else if (cmd == NetworkEvent.Type.Data){
			    NetUtility.OnData(stream, default(NetworkConnection));
			} else if (cmd == NetworkEvent.Type.Disconnect) {
				connection = default(NetworkConnection);
				connectionDropped?.Invoke();
			}
		}
    	
    }

    // Serializes and sends msg to server
    public void SendToServer(NetMessage msg){
    	DataStreamWriter writer;
    	driver.BeginSend(connection, out writer);
    	msg.Serialize(ref writer);
    	driver.EndSend(writer);
    }

    // Listens for Keep Alive Messages
    private void RegisterToEvent()
    {
    	NetUtility.C_KEEP_ALIVE += OnKeepAlive;
    }
    private void UnregisterToEvent(){
    	NetUtility.C_KEEP_ALIVE -= OnKeepAlive;
    }
    
	// Send message back, track alive status
    private void OnKeepAlive(NetMessage nm){
    	SendToServer(nm);
    }
}