// Yerel test PostgreSQL'i 55440'ta çalışırken testlerin beklediği 127.0.0.1:55439'u ona yönlendirir (yalnız loopback).
const net = require('net');
net.createServer((istemci) => {
  const sunucu = net.connect(55440, '127.0.0.1');
  istemci.pipe(sunucu).pipe(istemci);
  istemci.on('error', () => sunucu.destroy());
  sunucu.on('error', () => istemci.destroy());
}).listen(55439, '127.0.0.1');
