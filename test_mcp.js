#!/usr/bin/env node

const { spawn } = require('child_process');

// Apify API token
const APIFY_TOKEN = 'apify_api_dxhXr9Jj5nAWJZBPpTHnsybN6KZ5oX1tMFRX';

// Start the MCP server
const server = spawn('npx', ['-y', '@apify/actors-mcp-server', '--tools', 'apify/website-content-crawler'], {
  env: {
    ...process.env,
    APIFY_TOKEN: APIFY_TOKEN
  },
  stdio: ['pipe', 'pipe', 'pipe']
});

// Handle server output
server.stdout.on('data', (data) => {
  console.log(`Server output: ${data}`);
});

server.stderr.on('data', (data) => {
  console.error(`Server error: ${data}`);
});

// Send a test request to list tools
const listToolsRequest = {
  jsonrpc: '2.0',
  id: 1,
  method: 'tools/list',
  params: {}
};

console.log('Sending list tools request...');
server.stdin.write(JSON.stringify(listToolsRequest) + '\n');

// Wait for response and then exit
setTimeout(() => {
  console.log('Closing server...');
  server.kill();
  process.exit(0);
}, 5000);