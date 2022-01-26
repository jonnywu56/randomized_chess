using Unity.Networking.Transport;

// Outlines key functions for other NetMessages
public class NetMessage 
{
	// Tracks type of message
	public OpCode Code { set; get; }

	// Encodes message
	public virtual void Serialize(ref DataStreamWriter writer){
		writer.WriteByte((byte)Code);
	}

	// Decodes message
	public virtual void Deserialize(DataStreamReader reader){
	}

	// Function called when client recieves message
	public virtual void ReceivedOnClient(){}

	// Function called when server recieves message
	public virtual void ReceivedOnServer(NetworkConnection cnn){}
}
