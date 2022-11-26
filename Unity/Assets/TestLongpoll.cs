using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Assets;
using SimpleJSON;

public class TestLongpoll : MonoBehaviour
{
	LongpollClient lpClient = new LongpollClient();
	Coroutine corAbort;
	Coroutine corReceive;

	// Use this for initialization
	void Start () {
		Debug.Log("Start -------------------------------- TestLongpoll");

		lpClient.onConnect += (sender, evt) => {
			Debug.Log("[ CONNECT ] ------------------------------");
			//corAbort = StartCoroutine(
			//	lpClient.startAbort(10)
			//);
			//corReceive = StartCoroutine(
			//	lpClient.startReceive(10)
			//);
		};

		lpClient.onDisconnect += (sender, e) => {
			Debug.Log("[ DISCONNECT ] ---------------------------");
			//StopCoroutine(corAbort);
			//StopCoroutine(corReceive);
		};

		lpClient.onMessage += (sender, e) => {
			Debug.Log("[ MESSAGE ] ----- "+ Encoding.UTF8.GetString(e.message));
		};

		lpClient.onError += (sender, e) => {
			Debug.Log("[ ERROR ] ----- code "+ e.errCode.ToString() +", "+ e.message.ToString());
		};
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void connect() {
		Debug.Log("[StartScript] connect");
		StartCoroutine(
			lpClient.connect(new Hashtable() {
				{ "url", "http://127.0.0.1:8777" },
				{ "clientId", "CLI_1" },
			})
		);
	}

	public void disconnect() {
		Debug.Log("[StartScript] disconnect");
		StartCoroutine(
			lpClient.disconnect()
		);
	}

	public void send() {
		Debug.Log("[StartScript] receive");
		StartCoroutine(
			lpClient.send(Encoding.UTF8.GetBytes("Hello"))
		);
	}

	public void receive() {
		Debug.Log("[StartScript] receive");
		//corReceive = StartCoroutine(
		//	lpClient.startReceive(5000)
		//);
	}

	public void testJson() {
		Debug.Log("[StartScript] testJson");
		var json0 = new JSONObject();
		json0.Add("op", new JSONNumber(1));
		Debug.Log(json0.ToString());

		var json1 = new SimpleJSON.JSONString("{\"a\": 123}");
		
		var json2 = SimpleJSON.JSON.Parse("[\"a\"]");
		//Debug.Log(json2[0].GetType());
		//Debug.Log(json2.Count);
	}
}

	
	
