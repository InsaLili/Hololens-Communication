using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif


public class UDPController : MonoBehaviour {

	public string port = "12346";
	//ip for Lenovo network
	public string externalIP = "10.116.22.236";

	//ip for HDS network
	//public string externalIP = "192.168.1.100";

	public string externalPort = "12345";

#if !UNITY_EDITOR
	private DatagramSocket socket = null;
#endif

	// Use this for initialization
	void Start () {
#if !UNITY_EDITOR
		UdpSocketClient();
#endif
	}

	//--------------------Datagram socket
#if !UNITY_EDITOR
	private async void UdpSocketClient()
	{
		socket = new DatagramSocket();
		socket.MessageReceived += MessageReceived;
		//socket.Control.MulticastOnly = true;
		HostName IP = null;
		try
		{
			//HostName serverHost = new HostName("225.4.5.6");
			var icp = NetworkInformation.GetInternetConnectionProfile();

			IP = NetworkInformation.GetHostNames()
			.SingleOrDefault(
					hn =>
							hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
							== icp.NetworkAdapter.NetworkAdapterId);

			await socket.BindEndpointAsync(IP, port);
		}
		catch (Exception e)
		{
			Debug.Log(e.ToString());
			Debug.Log(SocketError.GetStatus(e.HResult).ToString());
			return;
		}
	}

	public async System.Threading.Tasks.Task SendMessage(string message)
	{
		try
		{
			IOutputStream outputStream;
			outputStream = await socket.GetOutputStreamAsync(new HostName(externalIP), externalPort);
			string stringToSend = message;
			DataWriter writer = new DataWriter(outputStream);
			writer.WriteString(stringToSend);
			await writer.StoreAsync();

			//Debug.Log("\"" + stringToSend + "\" sent successfully.");
		}
		catch (Exception exception)
		{
			// If this is an unknown status it means that the error is fatal and retry will likely fail.
			if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
			{
				throw;
			}

			Debug.Log("Send failed with error: ");
		}
	}

	private async void MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
	{
		//Read the message that was received from the UDP echo client.
		//Stream streamIn = args.GetDataStream().AsStreamForRead();
		//StreamReader reader = new StreamReader(streamIn);
		//string message = await reader.ReadLineAsync();
		//Debug.Log("Received:" + message);

		//uint stringLength = args.GetDataReader().UnconsumedBufferLength;
		//string receivedMessage = args.GetDataReader().ReadString(stringLength);
		//Debug.Log("Received:" + receivedMessage);

		Stream streamIn = args.GetDataStream().AsStreamForRead();
		StreamReader reader = new StreamReader(streamIn);
		string message = await reader.ReadLineAsync();

		Debug.Log("MESSAGE: " + message);
	}

	public enum NotifyType
	{
		StatusMessage,
		ErrorMessage
	}
#endif
}
