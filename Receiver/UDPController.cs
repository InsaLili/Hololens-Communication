using UnityEngine;
using System.Linq;
using System;
using System.IO;
using System.Text;

#if !UNITY_EDITOR

using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;

#endif

public class UDPController : MonoBehaviour {

	public static string message;
	public static bool msgReceived = false;

#if !UNITY_EDITOR
	private DatagramSocket socket;
	private string port = "12345";
	// ip for Lenovo network
	private string externalIP = "10.116.46.164";
	// ip for HDS network
	//private string externalIP = "192.168.1.105";
	private string externalPort = "12346";

	HostName IP = null;

	private void Start()
	{
		ConnectToServer();
	}

	private async void ConnectToServer()
	{
		Debug.Log("Waiting for a connection...");

		socket = new DatagramSocket();
		socket.MessageReceived += Socket_MessageReceived;

		try
		{
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

		await SendMessage("Connected...");

		//socket.JoinMulticastGroup(new HostName("225.4.5.6"));
	}

	private async new System.Threading.Tasks.Task SendMessage(string message)
	{
		using (var stream = await socket.GetOutputStreamAsync(new HostName(externalIP), externalPort))
		{
			using (var writer = new DataWriter(stream))
			{
				var data = Encoding.UTF8.GetBytes(message);

				writer.WriteBytes(data);
				await writer.StoreAsync();
				Debug.Log("Sent: " + message);
			}
		}
	}

	// receive message
	private async void Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
  {
    //Read the message that was received from the UDP echo client.
    Stream streamIn = args.GetDataStream().AsStreamForRead();
    StreamReader reader = new StreamReader(streamIn);
    message = await reader.ReadLineAsync();
		
		msgReceived = true;
	}
#endif
}
