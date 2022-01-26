using Unity.Networking.Transport;

// Communicates information for chess moves
public class NetMakeMove : NetMessage
{
    public int fromPosRow;
    public int fromPosCol;
    public int toPosRow;
    public int toPosCol;
	public int promotedPiece;
    public int teamId;

	public NetMakeMove(){
		Code = OpCode.MAKE_MOVE;
	}

	public NetMakeMove(DataStreamReader reader){
		Code = OpCode.MAKE_MOVE;
		Deserialize(reader);
	}

	public override void Serialize(ref DataStreamWriter writer) {
		writer.WriteByte((byte)Code);
        writer.WriteInt(fromPosRow);
        writer.WriteInt(fromPosCol);
        writer.WriteInt(toPosRow);
        writer.WriteInt(toPosCol);
		writer.WriteInt(promotedPiece);
        writer.WriteInt(teamId);
    }

	public override void Deserialize(DataStreamReader reader){
        fromPosRow = reader.ReadInt();
        fromPosCol = reader.ReadInt();
        toPosRow = reader.ReadInt();
        toPosCol = reader.ReadInt();
		promotedPiece = reader.ReadInt();
        teamId = reader.ReadInt();
	}

	public override void ReceivedOnClient(){
		NetUtility.C_MAKE_MOVE?.Invoke(this);
	}

	public override void ReceivedOnServer(NetworkConnection cnn){
		NetUtility.S_MAKE_MOVE?.Invoke(this, cnn);
	}

}
