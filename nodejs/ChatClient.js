'use strict';

const VEnum                             = require("@omarty/v-enum");
const logger                            = require("./logger");


const ops = new VEnum ("ops", [
	"joinChat",
	"leaveChat",
	"sendToUser",
	"sendByUser",
	"sendToChat",
	"sendByChat",
]);


const chats = {};


module.exports = class ChatClient
{
	_lpClient = null;

	constructor(options)
	{
		this._lpClient = options.lpClient;
		this._chatServer = options.chatServer;
		this._chatStorage = options.chatStorage;

		this.chats = {};

		this._lpClient.on("message", (rawMessage) => {
			let message;
			try {
				message = JSON.parse(rawMessage.toString());
				logger.trace("", { className: "ChatClient", instanceName: this._lpClient.clientId, funcName: 'this._lpClient.on("message")', data: message, });
			}
			catch (err) {
				logger.error(err, { className: "ChatClient", instanceName: this._lpClient.clientId, funcName: 'this._lpClient.on("message")', data: null, });
				return;
			}
			if (ChatClient._opHandlers[message.opCode] == undefined) {
				logger.error(`Op handler "${message.opCode}" not exists.`, { className: "ChatClient", instanceName: this._lpClient.clientId, funcName: 'this._lpClient.on("message")', data: null, });
				return;
			}
			try {
				ChatClient._opHandlers[message.opCode].call(this, message.params);
			}
			catch (err) {
				logger.error(err, { className: "ChatClient", instanceName: this._lpClient.clientId, funcName: 'this._lpClient.on("message")', data: null, });
			}
		});

		this._lpClient.on("error", (err) => {
			logger.error(err, { className: "ChatClient", instanceName: this._lpClient.clientId, funcName: 'this._lpClient.on("error")', data: null, });
		});
	}

	send(message) {
		this._lpClient.send(message);
	}

	static _opHandlers = {
		[ops.joinChat]: function (params) {
			const chat = this._chatStorage.getChat(params.chatName, null);

			if (chat.getClient(this._lpClient.clientId))
				return;

			chat.setClient(this._lpClient.clientId, this);
			this.chats[params.chatName] = chat;
		},
		[ops.leaveChat]: function (params) {
			const chat = this._chatStorage.getChatIfExists(params.chatName);

			if (chat == undefined)
				return;

			chat.deleteClient(this._lpClient.clientId);
		},
		[ops.sendToUser]: function (params) {
			const chatClient = this._chatServer.chatClients[params.userId];

			if (chatClient == undefined)
				return;
			
			const message = {
				opCode: ops.sendByUser,
				params: {
					userId: this._lpClient.clientId,
					text: params.text,
				}
			};

			chatClient.send(Buffer.from(JSON.stringify(message)));
		},
		[ops.sendToChat]: function (params) {
			const chat = this._chatStorage.getChatIfExists(params.chatName);

			if (chat == undefined)
				return;

			if (chat.clients.has(this._lpClient.clientId) == false)
				return;

			const message = {
				opCode: ops.sendByChat,
				params: {
					chatName: chat.chatId,
					userId: this._lpClient.clientId,
					text: params.text,
				}
			};

			const rawMessage = Buffer.from(JSON.stringify(message));
			
			for (let [clientId, client] of chat.clients) {
				client.send(rawMessage);
			}
		},
	};
}