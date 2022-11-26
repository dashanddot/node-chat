using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets
{
	public enum States {
		Connect,
		Disconnect,
		SendBegin,
		SendEnd,
		ReceiveBegin,
		ReceiveEnd,
		AbortForRecv,
	}

	public enum Errors {
		NetworkError,
		HttpError,
		AlreadyConnected,
		AlreadyDisconnected,
		NotImplemented,
		MessagesCapacityOverflow,
		ConnectFailure,
		RequestCanceled,
		ReceiveFailure,
	}

	public class LongpollClient
	{
		// Public
		public States state = States.Disconnect;
		public bool isConnected = false;

		// Private
		private List<byte[]> _msgList = new List<byte[]>();
		private int _msgsCapacity = 0;
		//private Timer _timerRecnct = null;
		private UnityWebRequest _reqRecv;
		private long _tsRecvStart = 0;
		private int _timeoutReceive = 0;

		// Events
		public EventHandler<OnMessageEventArgs> onMessage;
		public EventHandler<OnErrorEventArgs> onError;
		public EventHandler onConnect;
		public EventHandler onDisconnect;

		public LongpollClient(int timeoutReceive = 5)
		{
			this._timeoutReceive = timeoutReceive;
		}

		// Options
		private string _url;
		private string _clientId = null;
		private int _msgsCapacityMax;

		#region connect

		public IEnumerator connect(Hashtable options) {
			Debug.Log("[LongpollClient] connect");
			if (state != States.Disconnect) {
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.AlreadyConnected, "Client already connected", null));
				yield break;
			}

			state = States.Connect;
			
			// Check url
			if (options["url"].GetType() == typeof(string)) {
				_url = (string)options["url"];
			} else {
				throw new Exception("Parameter 'url' must be a String.");
			}

			// Check clientId
			if (options["clientId"].GetType() == typeof(string)) {
				_clientId = (string)options["clientId"];
			} else {
				throw new Exception("Parameter 'clientId' must be a String.");
			}

			// Check msgsCapacityMax
			if (options["msgsCapacityMax"] == null) {
				_msgsCapacityMax = 65536;
			} else if (options["msgsCapacityMax"].GetType() == typeof(int)) {
				_msgsCapacityMax = (int)options["msgsCapacityMax"];
			} else {
				throw new Exception("Parameter 'msgsCapacityMax' must be a Int32.");
			}
			
			yield return _connect();
		}

		public IEnumerator _connect() {
			Debug.Log("[LongpollClient] _connect");
			var req = new UnityWebRequest(_url, "POST");
			req.SetRequestHeader("x-op", "CLI_CNCT");
			req.SetRequestHeader("x-client-id", _clientId);
			yield return req.SendWebRequest();

			if (req.isNetworkError == true) {
				var webErr = new WebError(req.responseCode, req.error);
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.NetworkError, req.error, webErr));
				yield break;
			}
			if (req.isHttpError == true) {
				var webErr = new WebError(req.responseCode, req.error);
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.HttpError, req.error, webErr));
				yield break;
			}
			
			isConnected = true;
			if (onConnect != null) onConnect(this, null);

			yield return receive();
		}

		#endregion connect

		#region disconnect

		public IEnumerator disconnect() {
			//Debug.Log("[LongpollClient] disconnect");
			if (state == States.Disconnect) {
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.AlreadyDisconnected, "Client already disconnected", null));
				yield break;
			}

			yield return _disconnect();
		}

		public IEnumerator _disconnect() {
			//Debug.Log("[LongpollClient] _disconnect");
			state = States.Disconnect;

			var req = new UnityWebRequest(_url, "POST");
			req.SetRequestHeader("x-op", "CLI_DISCNCT");
			req.SetRequestHeader("x-client-id", _clientId);
			yield return req.SendWebRequest();

			if (req.isNetworkError == true) {
				var webErr = new WebError(req.responseCode, req.error);
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.NetworkError, req.error, webErr));
				yield break;
			}
			if (req.isHttpError == true) {
				var webErr = new WebError(req.responseCode, req.error);
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.HttpError, req.error, webErr));
				yield break;
			}

			req.Abort();
			req.Dispose();

			if (isConnected == false) yield break;
			isConnected = false;
			if (onDisconnect != null) onDisconnect(this, null);
		}

		#endregion disconnect

		#region send

		public IEnumerator send(byte[] message) {
			//Debug.Log("[LongpollClient] send");
			if (state == States.Disconnect) {
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.AlreadyDisconnected, "Client already disconnected", null));
				yield break;
			}
				
			if ((_msgsCapacity + message.Length) > _msgsCapacityMax) {
				var errMessage = "Messges capacity overflow (Number of messages " + _msgList.Count + ", Current messages capacity " + _msgsCapacity + ")";
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.MessagesCapacityOverflow, errMessage, null));
				yield break;
			}

			_msgsCapacity += message.Length;
			_msgList.Add(message);

			yield return _send();
		}

		public IEnumerator _send() {
			//Debug.Log("[LongpollClient] _send");

			var req = new UnityWebRequest(_url, "POST");
			var contentLen = 0;

			if (_msgList.Count > 0) {
				for (int i = 0; i < _msgList.Count; i++) {
					contentLen += 4 /* msg len */ + _msgList[i].Length;
				}
				var bufMessageList = new byte[contentLen];
				var ptr = 0;
				for (int i = 0; i < _msgList.Count; i++) {
					var message = _msgList[i];
					var messageLen = BitConverter.GetBytes((int)message.Length);
					Array.Copy(messageLen, 0, bufMessageList, ptr, 4);
					ptr += 4;
					Array.Copy(message, 0, bufMessageList, ptr, message.Length);
					ptr += message.Length;
				}
				_msgList.Clear();
				_msgsCapacity = 0;

				req.uploadHandler = new UploadHandlerRaw(bufMessageList);
				req.downloadHandler = new DownloadHandlerBuffer();
			}

			req.SetRequestHeader("x-op", "CLI_SEND");
			req.SetRequestHeader("x-client-id", _clientId);
			//req.SetRequestHeader("Content-Length", contentLen.ToString());
			yield return req.SendWebRequest();

			if (req.isNetworkError == true) {
				var webErr = new WebError(req.responseCode, req.error);
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.NetworkError, req.error, webErr));
				yield break;
			}
			if (req.isHttpError == true) {
				var webErr = new WebError(req.responseCode, req.error);
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.HttpError, req.error, webErr));
				yield break;
			}

			if (req.GetResponseHeader("x-op") == "SRV_DISCNCT") {
				isConnected = false;
				state = States.Disconnect;
				if (onDisconnect != null) onDisconnect(this, null);
				yield break;
			}

			yield break;
		}

		#endregion send

		#region receive

		public IEnumerator receive() {
			//Debug.Log("[LongpollClient] receive");
			if (state == States.Disconnect) {
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.AlreadyDisconnected, "Client already disconnected", null));
				yield break;
			}

			var req = new UnityWebRequest(_url, "POST");
			_reqRecv = req;
			req.downloadHandler = new DownloadHandlerBuffer();
			req.SetRequestHeader("x-op", "CLI_RECV");
			req.SetRequestHeader("x-client-id", _clientId);
			req.SetRequestHeader("x-timeout-receive", _timeoutReceive.ToString());
			yield return req.SendWebRequest();

			if (req.isNetworkError == true) {
				if (state == States.AbortForRecv) {
					Debug.Log("* * * - Abort for receive");
					state = States.Connect;
					yield break;
				}
				var webErr = new WebError(req.responseCode, req.error);
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.NetworkError, req.error, webErr));
				yield break;
			}
			if (req.isHttpError == true) {
				var webErr = new WebError(req.responseCode, req.error);
				if (onError != null) onError(this, new OnErrorEventArgs(Errors.HttpError, req.error, webErr));
				yield break;
			}

			yield return _receive(req);
		}

		public IEnumerator _receive(UnityWebRequest req) {
			//Debug.Log("[LongpollClient] _receive");
			if (req.GetResponseHeader("x-op") == "SRV_DISCNCT") {
				isConnected = false;
				state = States.Disconnect;
				if (onDisconnect != null) onDisconnect(this, null);
				yield break;
			}

			byte[] bufMessageList = req.downloadHandler.data;
			var ptr = 0;
			while (ptr < bufMessageList.Length) {
				var messageLen = BitConverter.ToInt32(bufMessageList, ptr);
				ptr += 4;
				var message = new byte[messageLen];
				Array.Copy(bufMessageList, ptr, message, 0, messageLen);
				ptr += messageLen;
				if (onMessage != null) onMessage(this, new OnMessageEventArgs(message));
			}

			_reqRecv = null;

			yield return new WaitForSeconds(_timeoutReceive);
			
			if (isConnected == false) {
				yield break;
			}

			yield return receive();
		}

		#endregion receive

		//public IEnumerator startAbort(int timeReceive) {
		//	while (true) {
		//		yield return new WaitForSeconds(1);
		//		Debug.Log("[LongpollClient] startAbort + + + 1, _reqRecv "+ (_reqRecv != null));

		//		if (_reqRecv == null)
		//			continue;

		//		var tsNow = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
		//		if (tsNow > _tsRecvStart + (timeReceive * 1000)) {
		//			Debug.Log("[LongpollClient] startAbort + + + 2");
		//			state = States.AbortForRecv;
		//			_reqRecv.Abort();
		//			_reqRecv = null;
		//		}
		//	}
		//}

		//public IEnumerator startReceive(int timeIdle) {
		//	//Debug.Log("[LongpollClient] startReceive + + +");
		//	while (true) {
		//		if (isConnected == true && _reqRecv == null) {
		//			Debug.Log("[LongpollClient] startReceive + + + receive start");
		//			_tsRecvStart = (long) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
		//			yield return receive();
		//			Debug.Log("[LongpollClient] startReceive + + + receive end");
		//		}

		//		yield return new WaitForSeconds(timeIdle);
		//	}
		//}
	}

	// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	// -----------------------------------------------------------------------------

	public class OnMessageEventArgs : EventArgs
	{
		public byte[] message;

		public OnMessageEventArgs(byte[] message) {
			this.message = message;
		}
	}

	// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	// -----------------------------------------------------------------------------

	public class OnErrorEventArgs : EventArgs
	{
		public Errors errCode;
		public String message;
		public WebError webError;

		public OnErrorEventArgs(Errors errCode, String message, WebError webError) {
			this.errCode = errCode;
			this.message = message;
			this.webError = webError;
		}
	}

	// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	// -----------------------------------------------------------------------------

	public class WebError
	{
		public long responseCode;
		public String message;

		public WebError(long responseCode, String message) {
			this.responseCode = responseCode;
			this.message = message;
		}
	}
}
