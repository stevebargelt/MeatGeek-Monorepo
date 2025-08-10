/**
 * Local Workload API simulation for IoT Edge testing
 * Provides minimal endpoints to satisfy IoT Edge module requirements
 */

const express = require('express');
const cors = require('cors');

const app = express();
const port = 15580;

app.use(cors());
app.use(express.json());

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ 
    status: 'healthy', 
    service: 'local-workload-api',
    timestamp: new Date().toISOString()
  });
});

// IoT Edge workload endpoints (minimal implementation)
app.get('/modules/:moduleId/certificate/server', (req, res) => {
  const { moduleId } = req.params;
  console.log(`Certificate request for module: ${moduleId}`);
  
  // Return a mock certificate response
  res.json({
    certificate: 'mock-certificate-for-local-testing',
    privateKey: 'mock-private-key-for-local-testing',
    expiration: new Date(Date.now() + 86400000).toISOString() // 24 hours
  });
});

app.get('/modules/:moduleId/genid/:genId/sign', (req, res) => {
  const { moduleId, genId } = req.params;
  console.log(`Signing request for module: ${moduleId}, generation: ${genId}`);
  
  // Return a mock signing response
  res.json({
    digest: 'mock-digest-for-local-testing'
  });
});

app.post('/modules/:moduleId/genid/:genId/decrypt', (req, res) => {
  const { moduleId, genId } = req.params;
  console.log(`Decrypt request for module: ${moduleId}, generation: ${genId}`);
  
  // Return mock decrypted data
  res.json({
    plaintext: Buffer.from('mock-decrypted-data').toString('base64')
  });
});

app.post('/modules/:moduleId/genid/:genId/encrypt', (req, res) => {
  const { moduleId, genId } = req.params;
  console.log(`Encrypt request for module: ${moduleId}, generation: ${genId}`);
  
  // Return mock encrypted data
  res.json({
    ciphertext: Buffer.from('mock-encrypted-data').toString('base64')
  });
});

// Generic catch-all for unhandled workload requests
app.all('*', (req, res) => {
  console.log(`Unhandled workload request: ${req.method} ${req.path}`);
  res.json({ 
    message: 'Local workload API - request logged',
    method: req.method,
    path: req.path,
    timestamp: new Date().toISOString()
  });
});

app.listen(port, '0.0.0.0', () => {
  console.log(`🔧 Local Workload API running on port ${port}`);
  console.log('📋 Simulating IoT Edge workload endpoints for local testing');
});