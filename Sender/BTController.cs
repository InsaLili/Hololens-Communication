using System;
using UnityEngine;

#if !UNITY_EDITOR
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif

public class BTController : MonoBehaviour {
#if !UNITY_EDITOR
	private StreamSocket chatSocket = null;
	private DataWriter chatWriter = null;
	private RfcommDeviceService chatService = null;
	private BluetoothDevice bluetoothDevice;
#endif

	// Use this for initialization
	void Start () {
#if !UNITY_EDITOR
		//ConnectToServer();
#endif
	}

#if !UNITY_EDITOR
	public async void SendMessage(string message)
	{
		try
		{
			var txt = message;
			chatWriter.WriteUInt32((uint)txt.Length);
			chatWriter.WriteString(txt);

			Debug.Log("Sent: " + txt);
			await chatWriter.StoreAsync();
		}
		catch (Exception ex) when ((uint)ex.HResult == 0x80072745)
		{
			// The remote device has disconnected the connection
			Debug.Log("Remote side disconnect: " + ex.HResult.ToString() + " - " + ex.Message);
		}
	}

	private async void ConnectToServer()
	{
		try
		{
			//Hololens
			bluetoothDevice = await BluetoothDevice.FromIdAsync("Bluetooth#Bluetooth58:00:e3:cf:52:da-b4:ae:2b:bf:1b:57");

			//Yoga
			//bluetoothDevice = await BluetoothDevice.FromIdAsync("Bluetooth#Bluetooth58:00:e3:cf:52:da-58:00:e3:d0:fa:22");
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message);
			return;
		}

		if (bluetoothDevice == null)
		{
			Debug.Log("Bluetooth Device returned null.");
		}

		var rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(
							RfcommServiceId.FromUuid(Constants.RfcommChatServiceUuid), BluetoothCacheMode.Uncached);

		if (rfcommServices.Services.Count > 0)
		{
			chatService = rfcommServices.Services[0];
		}
		else
		{
			Debug.Log(
				 "Could not discover the chat service on the remote device");
			return;
		}

		// Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service
		var attributes = await chatService.GetSdpRawAttributesAsync();
		if (!attributes.ContainsKey(Constants.SdpServiceNameAttributeId))
		{
			Debug.Log(
					"The Chat service is not advertising the Service Name attribute (attribute id=0x100). " +
					"Please verify that you are running the BluetoothRfcommChat server.");
			return;
		}
		var attributeReader = DataReader.FromBuffer(attributes[Constants.SdpServiceNameAttributeId]);
		var attributeType = attributeReader.ReadByte();
		if (attributeType != Constants.SdpServiceNameAttributeType)
		{
			Debug.Log(
					"The Chat service is using an unexpected format for the Service Name attribute. " +
					"Please verify that you are running the BluetoothRfcommChat server.");
			return;
		}
		var serviceNameLength = attributeReader.ReadByte();

		// The Service Name attribute requires UTF-8 encoding.
		attributeReader.UnicodeEncoding = UnicodeEncoding.Utf8;

		lock (this)
		{
			chatSocket = new StreamSocket();
		}
		try
		{

			await chatSocket.ConnectAsync(chatService.ConnectionHostName, chatService.ConnectionServiceName);

			chatWriter = new DataWriter(chatSocket.OutputStream);

			DataReader chatReader = new DataReader(chatSocket.InputStream);
			ReceiveStringLoop(chatReader);
		}
		catch (Exception ex) when ((uint)ex.HResult == 0x80070490) // ERROR_ELEMENT_NOT_FOUND
		{
			Debug.Log("Please verify that you are running the BluetoothRfcommChat server.");
		}
		catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
		{
			Debug.Log("Please verify that there is no other RFCOMM connection to the same device.");
		}

	}

	private async void ReceiveStringLoop(DataReader chatReader)
	{
		try
		{
			uint size = await chatReader.LoadAsync(sizeof(uint));
			if (size < sizeof(uint))
			{
				Disconnect("Remote device terminated connection - make sure only one instance of server is running on remote device");
				return;
			}

			uint stringLength = chatReader.ReadUInt32();
			uint actualStringLength = await chatReader.LoadAsync(stringLength);
			if (actualStringLength != stringLength)
			{
				// The underlying socket was closed before we were able to read the whole data
				return;
			}

			Debug.Log("Received: " + chatReader.ReadString(stringLength));

			ReceiveStringLoop(chatReader);
		}
		catch (Exception ex)
		{
			lock (this)
			{
				if (chatSocket == null)
				{
					// Do not print anything here -  the user closed the socket.
					if ((uint)ex.HResult == 0x80072745)
						Debug.Log("Disconnect triggered by remote device");
					else if ((uint)ex.HResult == 0x800703E3)
						Debug.Log("The I/O operation has been aborted because of either a thread exit or an application request.");
				}
				else
				{
					Disconnect("Read stream failed with error: " + ex.Message);
				}
			}
		}
	}

	private void Disconnect(string disconnectReason)
	{
		if (chatWriter != null)
		{
			chatWriter.DetachStream();
			chatWriter = null;
		}


		if (chatService != null)
		{
			chatService.Dispose();
			chatService = null;
		}
		lock (this)
		{
			if (chatSocket != null)
			{
				chatSocket.Dispose();
				chatSocket = null;
			}
		}

		Debug.Log(disconnectReason);
	}


	class Constants
	{
		// The Chat Server's custom service Uuid: 34B1CF4D-1069-4AD6-89B6-E161D79BE4D8
		public static readonly Guid RfcommChatServiceUuid = Guid.Parse("34B1CF4D-1069-4AD6-89B6-E161D79BE4D8");

		// The Id of the Service Name SDP attribute
		public const UInt16 SdpServiceNameAttributeId = 0x100;

		// The SDP Type of the Service Name SDP attribute.
		// The first byte in the SDP Attribute encodes the SDP Attribute Type as follows :
		//    -  the Attribute Type size in the least significant 3 bits,
		//    -  the SDP Attribute Type value in the most significant 5 bits.
		public const byte SdpServiceNameAttributeType = (4 << 3) | 5;

		// The value of the Service Name SDP attribute
		public const string SdpServiceName = "Bluetooth Rfcomm Chat Service";
	}
#endif

}
