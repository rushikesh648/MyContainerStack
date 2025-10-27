// A basic Express.js server to run on the cloud VM

const express = require('express');
const app = express();
const port = 80; // Cloud servers typically use port 80 for HTTP traffic

// Define a simple GET route
app.get('/', (req, res) => {
  console.log('Request received for /');
  res.send('<h1>Hello from your C#-provisioned Cloud Server!</h1><p>The application is running via Node.js/JavaScript.</p>');
});

// Define a health check route
app.get('/status', (req, res) => {
  res.json({
    status: 'Running',
    language: 'Node.js/JS',
    time: new Date().toISOString()
  });
});

// Start the server
app.listen(port, () => {
  console.log(`Cloud Server app listening at http://0.0.0.0:${port}`);
});
