using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

namespace Assets
{
	enum ops {
		joinChat,
		leaveChat,
		sendToUser,
		sendByUser,
		sendToChat,
		sendByChat,
	}

	class ChatClient : LongpollClient
	{
		// Public

		// Private
		IChatClientListener chat;

		// Events

		public ChatClient(IChatClientListener chat) : base(5)
		{
			this.chat = chat;

			base.onConnect += (sender, e) => {
				chat.onConnect();
			};

			base.onDisconnect += (sender, e) => {
				chat.onDisconnect();
			};

			base.onMessage += (sender, e) => {
				Debug.Log("[ MESSAGE + + + 1 ] ----- "+ Encoding.UTF8.GetString(e.message));
				var message = JSONObject.Parse(Encoding.UTF8.GetString(e.message));
				var opCodeJSON = message["opCode"];
				if (opCodeJSON.GetType() != typeof(JSONNumber)) {
					throw new Exception("Parameter 'opCode' must be a JSONNumber.");
				}
				var opCode = (int)opCodeJSON;
				if (_opHandlers.ContainsKey((ops)opCode) == false) {
					throw new Exception("Dictionary '_opHandlers' does not contains key '"+ ((ops)opCode).ToString() +"'.");
				}
				_opHandlers[(ops)opCode](this, (JSONObject)message["params"]);
			};
		}

		public IEnumerator joinChat(String chatName) {
			Debug.Log("[ChatClient] joinChat");
			var dataJSON = new JSONObject();
			dataJSON.Add("opCode", new JSONNumber((int)ops.joinChat));
			var paramsJSON = new JSONObject();
			paramsJSON.Add("chatName", chatName);
			dataJSON.Add("params", paramsJSON);
			return this.send(Encoding.UTF8.GetBytes(dataJSON.ToString()));
		}

		public IEnumerator leaveChat(String chatName) {
			Debug.Log("[ChatClient] joinChat");
			var dataJSON = new JSONObject();
			dataJSON.Add("opCode", new JSONNumber((int)ops.leaveChat));
			var paramsJSON = new JSONObject();
			paramsJSON.Add("chatName", chatName);
			dataJSON.Add("params", paramsJSON);
			return this.send(Encoding.UTF8.GetBytes(dataJSON.ToString()));
		}

		public IEnumerator sendToUser(String userId, String text, string nameSpace=null, string nsText=null) {
			Debug.Log("[ChatClient] sendToUser");
			var dataJSON = new JSONObject();
			dataJSON.Add("opCode", new JSONNumber((int)ops.sendToUser));
			var paramsJSON = new JSONObject();
			paramsJSON.Add("userId", userId);
			paramsJSON.Add("text", text);
			dataJSON.Add("params", paramsJSON);
			return this.send(Encoding.UTF8.GetBytes(dataJSON.ToString()));
		}

		public IEnumerator sendToChat(String chatName, String text, string nameSpace=null, string nsText=null) {
			Debug.Log("[ChatClient] sendToChat");
			var dataJSON = new JSONObject();
			dataJSON.Add("opCode", new JSONNumber((int)ops.sendToChat));
			var paramsJSON = new JSONObject();
			paramsJSON.Add("chatName", chatName);
			paramsJSON.Add("text", text);
			dataJSON.Add("params", paramsJSON);
			return this.send(Encoding.UTF8.GetBytes(dataJSON.ToString()));
		}

		static Dictionary<ops, Action<ChatClient, JSONObject>> _opHandlers = new Dictionary<ops, Action<ChatClient, JSONObject>>() {
			{ops.sendByUser, (_this, _params) => { 
				_this.chat.onMessageByUser(_params["userId"].Value, (string)_params["text"].Value);
			}},
			{ops.sendByChat, (_this, _params) => { 
				_this.chat.onMessageByChat(_params["chatName"].Value, _params["userId"].Value, (string)_params["text"].Value);
			}},
		};
	}
}


