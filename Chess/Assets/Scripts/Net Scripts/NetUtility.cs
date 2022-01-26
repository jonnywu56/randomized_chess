using System;
using Unity.Networking.Transport;
using UnityEngine;

// Differentiates types of NetMessages
public enum OpCode {KEEP_ALIVE, WELCOME, START_GAME, MAKE_MOVE, REMATCH, SETUP}

// Helps handle NetMessage logic by organizing OpCodes and declaring relevant actions
public static class NetUtility
{
	// Reads message from stream
	public static void OnData(DataStreamReader stream, NetworkConnection cnn, Server server = null){
		// Reads OpCode and creates appropraite NetMessage
		NetMessage msg = null;
		var opCode = (OpCode)stream.ReadByte();
		switch (opCode){
			case OpCode.KEEP_ALIVE: msg = new NetKeepAlive(stream); break;
			case OpCode.WELCOME: msg = new NetWelcome(stream); break;
			case OpCode.START_GAME: msg = new NetStartGame(stream); break;
			case OpCode.MAKE_MOVE: msg = new NetMakeMove(stream); break;
			case OpCode.REMATCH: msg = new NetRematch(stream); break;
			case OpCode.SETUP: msg = new NetSetup(stream); break;
			default:
				Debug.LogError("Message received had no OpCode");
				break;
		}

		// Determines whether function is called by server or client
		if (server != null) {
			msg.ReceivedOnServer(cnn);
		} else {
			msg.ReceivedOnClient();
		}
	}

    // Net message list
    public static Action<NetMessage> C_KEEP_ALIVE;
    public static Action<NetMessage> C_WELCOME;
    public static Action<NetMessage> C_START_GAME;
    public static Action<NetMessage> C_MAKE_MOVE;
    public static Action<NetMessage> C_REMATCH;
	public static Action<NetMessage> C_SETUP;
    public static Action<NetMessage, NetworkConnection> S_KEEP_ALIVE;
    public static Action<NetMessage, NetworkConnection> S_WELCOME;
    public static Action<NetMessage, NetworkConnection> S_START_GAME;
    public static Action<NetMessage, NetworkConnection> S_MAKE_MOVE;
    public static Action<NetMessage, NetworkConnection> S_REMATCH;
	public static Action<NetMessage, NetworkConnection> S_SETUP;




}
