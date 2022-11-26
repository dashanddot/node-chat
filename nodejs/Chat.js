"use strict";


module.exports = class Chat
{
	constructor(chatId, chatOptions, chatStorage)
	{
		this.chatId = chatId;
		this.chatStorage = chatStorage;
		this.clients = new Map();
	}

	getClient(clientId) {
		this.clients.get(clientId);
	}

	setClient(clientId, client) {
		this.clients.set(clientId, client);
	}

	deleteClient(clientId) {
		this.clients.delete(clientId);

		if (this.clients.size == 0)
			this.chatStorage.removeChat(this.chatId);
	}
}

