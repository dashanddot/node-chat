'use strict';

const LongpollServer                    = require('./LongpollServer');
const logger                            = require('./logger');


const lpServer = new LongpollServer({
	clientTTL: 20000,
});

lpServer.on("connection", (client) => {
	logger.info("Client connection", { className: "index-longpoll.js", instanceName: null, funcName: 'lpServer.on("connection")', data: { clientId: client.clientId, }, });

	let num = 0;

	// setTimeout(() => {
	setInterval(() => {
		if (client.isConnected == false) return;
		client.send(Buffer.from("hello "+ num++));
		// client.send(Buffer.from("hello "+ num++));
		// setTimeout(() => client.disconnect(), 1000);
	}, 500);

	let msgNum = 0;
	
	client.on("message", (message) => {
		let msgString = message.toString();
		let arr = msgString.split(" ");
		// console.log(msgString, arr[2]);
	});

	client.on("error", (err) => {
		logger.error(err, { className: "index-longpoll.js", instanceName: null, funcName: 'client.on("error")', data: null, });
	});
});

lpServer.on("disconnection", (client) => {
	logger.info("Client disconnection", { className: "index-longpoll.js", instanceName: null, funcName: 'lpServer.on("disconnection")', data: { clientId: client.clientId, }, });
});

const host = "0.0.0.0";
const port = 8777;

lpServer.listen(port, host, () => {
	logger.info(`Server listening on ${host}:${port}`, { className: "index-longpoll.js", instanceName: null, funcName: 'lpServer.listen', data: null, });
});