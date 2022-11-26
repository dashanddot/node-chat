'use strict';

const LongpollServer                    = require('./LongpollServer');
const ChatClient                        = require('./ChatClient');
const ChatStorage                       = require("./ChatStorage");
const logger                            = require("./logger");


module.exports = class ChatServer
{
	constructor(options)
	{
		this._lpServer = null;
		this.chatClients = {};
		this.chatStorage = new ChatStorage();

		// setInterval(() => {
		// 	console.log("Chat counts: ", this.chatStorage.chats.size);
		// }, 5000);

		this._lpServer = new LongpollServer({
			clientTTL: 60000,
		});

		this._lpServer.on("connection", (lpClient) => {
			logger.info("Client connection", { className: "ChatServer", instanceName: null, funcName: 'this._lpServer.on("connection")', data: { clientId: lpClient.clientId, }, });

			let chatClient = this.chatClients[lpClient.clientId];

			if (chatClient == undefined) {
				chatClient = new ChatClient({
					lpClient: lpClient,
					chatServer: this,
					chatStorage: this.chatStorage,
				});
				this.chatClients[lpClient.clientId] = chatClient;
			}
		});
		
		this._lpServer.on("disconnection", (lpClient) => {
			logger.info("Client disconnection", { className: "ChatServer", instanceName: null, funcName: 'this._lpServer.on("disconnection")', data: { clientId: lpClient.clientId, }, });

			const chatClient = this.chatClients[lpClient.clientId];

			for (let chatName in chatClient.chats) {
				chatClient.chats[chatName].deleteClient(lpClient.clientId);
			}

			delete this.chatClients[lpClient.clientId];
		});
		
		this._lpServer.listen(options.port, options.host || "localhost", () => {
			logger.info(`Server listening on ${options.host}:${options.port}`, { className: "ChatServer", instanceName: null, funcName: 'this._lpServer.listen', data: null, });
		});
	}
	
}