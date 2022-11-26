'use strict';

const EventEmitter                      = require('events');
const http                              = require('http');
const LongpollClient                    = require('./LongpollClient');


module.exports = class LongpollServer extends EventEmitter
{
	_clients = {};

	constructor(options)
	{
		super();

		this._server = http.createServer();

		this._server.on("request", (request, response) => {
			const clientId = request.headers["x-client-id"];
			let client = this._clients[clientId];

			if (client === undefined) {
				client = new LongpollClient({ clientId, });

				client.onConnectionSuccess = () => {
					client.tsReq = Date.now();
					this._clients[clientId] = client;
					this.emit("connection", client);
				}

				client.onConnectionFail = () => {
					this.emit("error", { code: "CONNECT_FAIL", client, request, response, });
				}
				
				client.onAuthSuccess = (request, response) => {
					client.tsReq = Date.now();
					client.onOp(request, response);
				}

				client.onAuthFail = () => {
					this.emit("error", { code: "AUTH_FAIL", client, request, response, });
				}

				client.onDisconnection = () => {
					delete this._clients[clientId];
					this.emit("disconnection", client);
				}

				client.tryConnection(request, response);
			}
			else {
				client.checkAuth(request, response);
			}
		});
		
		this._server.on('connect', (err, socket) => {
			// console.log("connect");
		});
		
		this._server.on('connection', (err, socket) => {
			// console.log("connection");
		});
		
		this._server.on('clientError', (err, socket) => {
			// console.log(err);
			// socket.end('HTTP/1.1 400 Bad Request\r\n\r\n');
		});

		this._timer = setInterval(() => {
			for (let clientId in this._clients) {
				const client = this._clients[clientId];
				if (client.isReceive && Date.now() > client.tsReceiveEnd) {
					client.receiveEnd();
					continue;
				}
				if (Date.now() > client.tsReq + options.clientTTL) {
					client.tryDisconnection();
				}
			}
		}, 1000);
	}

	listen() {
		this._server.listen(...arguments);
	}
}