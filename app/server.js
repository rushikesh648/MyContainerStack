const express = require('express');
const app = express();
const port = 8080; // Changed to 8080 for standard container apps

app.get('/', (req, res) => {
  res.send('<h1>Hello from your Serverless Container App!</h1>');
});

app.listen(port, () => {
  console.log(`Container App listening on port ${port}`);
});
