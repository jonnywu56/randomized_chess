using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    // Multiplayer: Tracks server and client
    public Server server;
    public Client client;

    // Multiplayer: Input for IP address
    [SerializeField] private TMP_InputField addressInput;

    // Initializes server and connects player to server as client
    public void OnHostButton(){
    	server.Init(8007);
    	client.Init("127.0.0.1",8007);
    }

    // Connects player to server as client
    public void OnConnectButton(){
    	client.Init(addressInput.text, 8007);
    }

    // Shuts down server and client (from hostMenu)
    public void OnHostBackButton(){
    	server.Shutdown();
    	client.Shutdown();
    }

    // Shuts down client (from joinMenu)
    public void OnJoinBackButton(){
        client.Shutdown();
    }

    // Quits application
    public void OnQuitButton(){
        Application.Quit();
    }

    // Shutsdown server and client if they exist
    public void OnDestroy(){
        if (server!=null){
            server.Shutdown();
        }
        if (client!=null){
            client.Shutdown();
        }
    }

}
