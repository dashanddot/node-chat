'use strict';

const EventEmitter                      = require('events');
const logger                            = require("./logger");


module.exports = class LongpollClient extends EventEmitter
{
	clientId = null;
	isConnected = false;
	tsReq = 0;
	isReceive = false;
	tsReceiveEnd = 0;
	_msgList = [];
	_request = null;
	_response = null;

	constructor(options)
	{
		super();
		this.clientId = options.clientId;
	}

	tryConnection(request, response) {
		logger.trace("", { className: "LongpollClient", instanceName: this.clientId, funcName: 'tryConnection', data: { headers: request.headers, }, });

		if (request.headers["x-op"] != "CLI_CNCT") {
			logger.trace("SRV_DISCNCT", { className: "LongpollClient", instanceName: this.clientId, funcName: 'tryConnection', data: null, });
			response.setHeader("x-op", "SRV_DISCNCT");
			response.end();
			return;
		}

		this.isConnected = true;
		response.end();
		this.onConnectionSuccess();
	}

	checkAuth(request, response) {
		this.onAuthSuccess(request, response);
	}

	onOp(request, response) {
		logger.trace("", { className: "LongpollClient", instanceName: this.clientId, funcName: 'onOp', data: { "x-op": request.headers["x-op"], }, });
		if (request.headers["x-op"] == "CLI_RECV") {
			if (this._msgList.length > 0) {
				this._writeNextOp(request, response);
			} else {
				this.isReceive = true;
				this.tsReceiveEnd = this.tsReq + (Number(request.headers["x-timeout-receive"]) * 1000);
				this._request = request;
				this._response = response;
			}
		}
		else if (request.headers["x-op"] == "CLI_SEND") {
			let bufMessageList = Buffer.alloc(Number(request.headers["content-length"]));
			let ptr = 0;

			request.on("data", (chunk) => {
				chunk.copy(bufMessageList, ptr, 0, chunk.length);
				ptr += chunk.length;
			});
	
			request.on("end", () => {
				let ptr = 0;
				while (ptr < bufMessageList.length) {
					let messageLen = bufMessageList.readInt32LE(ptr);
					ptr += 4;
					let message = Buffer.alloc(messageLen);
					bufMessageList.copy(message, 0, ptr, ptr + messageLen);
					ptr += messageLen;
					this.emit("message", message);
				}
				response.end();
			});

			request.on("aborted", () => {
				/* console.log("___ aborted CLI_SEND", request.complete, response.writableEnded); */
			});
		}
		else if (request.headers["x-op"] == "CLI_DISCNCT") {
			response.end();
			this.tryDisconnection();
		}
		else {
			response.end();
		}
	}

	tryDisconnection() {
		this.isConnected = false;
		this.onDisconnection();
	}

	_writeNextOp(request, response) {
		if (response.writableEnded == true) return;

		let bufMessageList = null;
		
		if (this._msgList.length > 0) {
			let contentLen = 0;
			for (let i = 0; i < this._msgList.length; i++) {
				contentLen += 4 /* msg len */ + this._msgList[i].length;
			}
			bufMessageList = Buffer.alloc(contentLen);
			let ptr = 0;
			for (let i = 0; i < this._msgList.length; i++) {
				let message = this._msgList[i];
				bufMessageList.writeInt32LE(message.length, ptr);
				ptr += 4;
				message.copy(bufMessageList, ptr, 0, message.length);
				ptr += message.length;
			}
			response.setHeader("Content-Length", contentLen);
			this._msgList.splice(0, this._msgList.length);
		}
		
		response.setHeader("x-op", "SRV_SEND");
		response.end(bufMessageList);
	}

	send(message) {
		logger.trace("", { className: "LongpollClient", instanceName: this.clientId, funcName: 'send', data: { message, }, });

		if (this.isConnected == false) {
			this.emit("error", {
				code: "CLIENT_DISCONNECTED",
				message: "Client already disconnected. (send)",
			});
			return;
		}
		
		this._msgList.push(message);

		if (this.isReceive == true) {
			this._writeNextOp(this._request, this._response);
			this.isReceive = false;
			this._request = null;
			this._response = null;
		}
	}

	receiveEnd() {
		logger.trace("", { className: "LongpollClient", instanceName: this.clientId, funcName: 'receiveEnd', data: { clientId: this.clientId, }, });
		
		if (this._request) {
			this._response.end();
			this.isReceive = false;
			this._request = null;
			this._response = null;
		}
	}
}