"use strict";

// Modules of nodejs
const path                              = require("path");
const logManager                        = require("@omarty/log-manager");


module.exports = logManager.create({
	name: "core-logger",
	levels: { fatal: 0, error: 1, warn: 2, info: 3, debug: 4, trace: 5, },
	level: "trace",
	path: path.normalize(`${__dirname}/logs`),
	maxSize: 10485760,
	maxFiles: 10,
});