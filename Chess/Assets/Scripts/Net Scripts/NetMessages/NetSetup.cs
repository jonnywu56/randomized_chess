using UnityEngine;
using Unity.Networking.Transport;

// Communicates initial board setup at start of games
public class NetSetup : NetMessage
{
    public int mod1 = 0;
    public int mod2 = 0;
    public int mod3 = 0;
    public int[,] boardSetup = new int [8,8]; 

    public NetSetup()
    {
        Code = OpCode.SETUP;
    }
    public NetSetup(DataStreamReader reader){
        Code = OpCode.SETUP;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer){
        writer.WriteByte((byte)Code);
        writer.WriteInt(mod1);
        writer.WriteInt(mod2);
        writer.WriteInt(mod3);
        for (int row=0;row<8;row++){
            for(int col=0;col<8;col++){
                writer.WriteInt(boardSetup[row,col]);
            }
        }
    }

    public override void Deserialize(DataStreamReader reader){
        mod1 = reader.ReadInt();
        mod2 = reader.ReadInt();
        mod3 = reader.ReadInt();
        for (int row=0;row<8;row++){
            for(int col=0;col<8;col++){
                boardSetup[row,col] = reader.ReadInt();
            }
        }
    }

    public override void ReceivedOnClient(){
        NetUtility.C_SETUP?.Invoke(this);
    }

    public override void ReceivedOnServer(NetworkConnection cnn){
        NetUtility.S_SETUP?.Invoke(this,cnn);
    }
}
