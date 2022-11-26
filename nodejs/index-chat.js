'use strict';

const ChatServer                        = require('./ChatServer');


const chatServer = new ChatServer({
	host: "0.0.0.0",
	port: 8777,
});
