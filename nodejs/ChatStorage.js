"use strict";

const Chat                              = require("./Chat");


module.exports = class ChatStorage
{
	constructor(options)
	{
		this.chats = new Map();
	}

	getChat(chatId, chatOptions) {
		let chat = this.chats.get(chatId);

		if (chat)
			return chat;

		chat = this.createChat(chatId, chatOptions);
		this.chats.set(chatId, chat);

		return chat;
	}

	getChatIfExists(chatId) {
		return this.chats.get(chatId);
	}

	createChat(chatId, chatOptions) {
		const chat = new Chat(chatId, chatOptions, this);
		return chat;
	}

	removeChat(chatId) {
		const chat = this.chats.get(chatId);

		if (chat == undefined)
			return false;
		
		this.chats.delete(chatId);
		return true;
	}
}

