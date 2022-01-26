using UnityEngine;
using Unity.Networking.Transport;

// Tells client what team they are on
public class NetWelcome : NetMessage
{
    public int AssignedTeam { set; get;}


    public NetWelcome()
    {
        Code = OpCode.WELCOME;
    }
    public NetWelcome(DataStreamReader reader){
        Code = OpCode.WELCOME;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer){
        writer.WriteByte((byte)Code);
        writer.WriteInt(AssignedTeam);
    }

    public override void Deserialize(DataStreamReader reader){
        // Already read type byte in NetUtility::OnData
        AssignedTeam = reader.ReadInt();
        
    }

    public override void ReceivedOnClient(){
        NetUtility.C_WELCOME?.Invoke(this);
    }

    public override void ReceivedOnServer(NetworkConnection cnn){
        NetUtility.S_WELCOME?.Invoke(this,cnn);
    }
}
