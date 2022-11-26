using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets;

public class TestChat : MonoBehaviour, IChatClientListener
{
	ChatClient chatClient;
	//Coroutine corAbort;
	Coroutine corReceive;
	InputField inp_Connect_clientId;
	InputField inp_JoinChat_chatName;
	InputField inp_SendToUser_userId;
	InputField inp_SendToUser_text;
	InputField inp_SendToChat_chatName;
	InputField inp_SendToChat_text;
	ScrollRect panel_Console;
	Text txt_Console;

	// Use this for initialization
	void Start () {
		Debug.Log("Start -------------------------------- TestChat");

		chatClient = new ChatClient(this);

		var Inp_Connect_clientId = GameObject.Find("Inp_Connect_clientId");
		inp_Connect_clientId = Inp_Connect_clientId.GetComponent<InputField>();
		inp_Connect_clientId.text = "CLI_1";

		var Inp_JoinChat_chatName = GameObject.Find("Inp_JoinChat_chatName");
		inp_JoinChat_chatName = Inp_JoinChat_chatName.GetComponent<InputField>();
		inp_JoinChat_chatName.text = "CHAT_1";

		var Inp_SendToUser_userId = GameObject.Find("Inp_SendToUser_userId");
		inp_SendToUser_userId = Inp_SendToUser_userId.GetComponent<InputField>();
		inp_SendToUser_userId.text = "CLI_1";

		var Inp_SendToUser_text = GameObject.Find("Inp_SendToUser_text");
		inp_SendToUser_text = Inp_SendToUser_text.GetComponent<InputField>();
		inp_SendToUser_text.text = "Hello to user";

		var Inp_SendToChat_chatName = GameObject.Find("Inp_SendToChat_chatName");
		inp_SendToChat_chatName = Inp_SendToChat_chatName.GetComponent<InputField>();
		inp_SendToChat_chatName.text = "CHAT_1";

		var Inp_SendToChat_text = GameObject.Find("Inp_SendToChat_text");
		inp_SendToChat_text = Inp_SendToChat_text.GetComponent<InputField>();
		inp_SendToChat_text.text = "Hello to chat";

		var Panel_Console = GameObject.Find("Panel_Console");
		panel_Console = Panel_Console.GetComponent<UnityEngine.UI.ScrollRect>();
		panel_Console.scrollSensitivity = 100;

		var Txt_Console = GameObject.Find("Txt_Console");
		txt_Console = Txt_Console.GetComponent<Text>();
		txt_Console.text = "Start...\n";

		chatClient.onConnect += (sender, evt) => {
			Debug.Log("[ CONNECT ] ------------------------------");
		};

		chatClient.onDisconnect += (sender, e) => {
			Debug.Log("[ DISCONNECT ] ---------------------------");
		};

		//chatClient.onMessage += (sender, e) => {
		//	Debug.Log("[ MESSAGE + + + 2 ] ----- "+ Encoding.UTF8.GetString(e.message));
		//};

		//chatClient.onError += (sender, e) => {
		//	Debug.Log("[ ERROR ] ----- code "+ e.errCode.ToString() +", "+ e.message.ToString());
		//};
	}

	// Update is called once per frame
	void Update () {
		
	}

	public void connect() {
		Debug.Log("[StartScript] connect");
		StartCoroutine(
			chatClient.connect(new Hashtable() {
				{ "url", "http://localhost:8777" },
				{ "clientId", inp_Connect_clientId.text },
			})
		);
	}

	public void disconnect() {
		Debug.Log("[StartScript] disconnect");
		StartCoroutine(
			chatClient.disconnect()
		);

		//StopCoroutine(corAbort);
		//StopCoroutine(corReceive);
	}

	public void joinChat() {
		Debug.Log("[StartScript] joinChat");
		StartCoroutine(
			chatClient.joinChat(inp_JoinChat_chatName.text)
		);
	}

	public void leaveChat() {
		Debug.Log("[StartScript] leaveChat");
		StartCoroutine(
			chatClient.leaveChat(inp_JoinChat_chatName.text)
		);
	}

	public void sendToUser() {
		Debug.Log("[StartScript] sendToUser");
		StartCoroutine(
			chatClient.sendToUser(inp_SendToUser_userId.text, inp_SendToUser_text.text)
		);
	}

	public void sendToChat() {
		Debug.Log("[StartScript] sendToChat");
		StartCoroutine(
			chatClient.sendToChat(inp_SendToChat_chatName.text, inp_SendToChat_text.text)
		);
	}

	public void onMessageByUser(string userId, string text) {
		txt_Console.text += "[ USER ] userId: "+ userId +" say: "+ text +"\n";
		panel_Console.verticalNormalizedPosition = 0;
	}

	public void onMessageByChat(string chatName, string userId, string text) {
		txt_Console.text += "[ CHAT ] chatName: "+ chatName +" userId: "+ userId +" say: "+ text +"\n";
		panel_Console.verticalNormalizedPosition = 0;
	}

	public void onConnect() {
		txt_Console.text += "[ CONNECT ]\n";

		//corAbort = StartCoroutine(
		//	chatClient.startAbort(5)
		//);
		//corReceive = StartCoroutine(
		//	chatClient.startReceive(5)
		//);
		//corReceive = StartCoroutine(
		//	chatClient.receive()
		//);

		panel_Console.verticalNormalizedPosition = 0;
	}

	public void onDisconnect() {
		txt_Console.text += "[ DISCONNECT ]\n";

		//StopCoroutine(corAbort);
		//StopCoroutine(corReceive);

		panel_Console.verticalNormalizedPosition = 0;
	}
}
