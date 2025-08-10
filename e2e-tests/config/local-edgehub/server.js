/**
 * Local Edge Hub simulation for IoT Edge testing
 * Provides minimal endpoints to satisfy IoT Edge Hub requirements
 */

const express = require('express');
const cors = require('cors');

const app = express();
const port = 8443;

app.use(cors());
app.use(express.json());
app.use(express.text());

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ 
    status: 'healthy', 
    service: 'local-edgehub',
    timestamp: new Date().toISOString()
  });
});

// IoT Hub device communication endpoints
app.post('/devices/:deviceId/messages/events', (req, res) => {
  const { deviceId } = req.params;
  console.log(`📤 Telemetry from device: ${deviceId}`, req.body);
  
  res.status(202).json({
    status: 'accepted',
    deviceId,
    timestamp: new Date().toISOString()
  });
});

// Module-to-module communication
app.post('/devices/:deviceId/modules/:moduleId/inputs/:inputName', (req, res) => {
  const { deviceId, moduleId, inputName } = req.params;
  console.log(`📨 Module message: ${deviceId}/${moduleId} -> ${inputName}`, req.body);
  
  res.json({
    status: 'routed',
    deviceId,
    moduleId,
    inputName,
    timestamp: new Date().toISOString()
  });
});

// Direct method calls
app.post('/devices/:deviceId/modules/:moduleId/methods/:methodName', (req, res) => {
  const { deviceId, moduleId, methodName } = req.params;
  console.log(`🎯 Direct method: ${methodName} on ${deviceId}/${moduleId}`, req.body);
  
  res.json({
    status: 'completed',
    result: {
      deviceId,
      moduleId,
      methodName,
      response: `Method ${methodName} executed successfully`,
      timestamp: new Date().toISOString()
    }
  });
});

// Device twin operations
app.get('/devices/:deviceId/modules/:moduleId/twin', (req, res) => {
  const { deviceId, moduleId } = req.params;
  console.log(`🔍 Twin GET: ${deviceId}/${moduleId}`);
  
  res.json({
    deviceId,
    moduleId,
    properties: {
      desired: {},
      reported: {
        lastActivity: new Date().toISOString(),
        status: 'running'
      }
    },
    version: 1,
    etag: 'mock-etag'
  });
});

app.patch('/devices/:deviceId/modules/:moduleId/twin', (req, res) => {
  const { deviceId, moduleId } = req.params;
  console.log(`📝 Twin PATCH: ${deviceId}/${moduleId}`, req.body);
  
  res.json({
    deviceId,
    moduleId,
    properties: {
      reported: {
        ...req.body,
        lastActivity: new Date().toISOString()
      }
    },
    version: 2,
    etag: 'updated-mock-etag'
  });
});

// Generic catch-all for unhandled Edge Hub requests
app.all('*', (req, res) => {
  console.log(`📋 Unhandled Edge Hub request: ${req.method} ${req.path}`);
  res.json({ 
    message: 'Local Edge Hub - request logged',
    method: req.method,
    path: req.path,
    timestamp: new Date().toISOString()
  });
});

app.listen(port, '0.0.0.0', () => {
  console.log(`🏠 Local Edge Hub running on port ${port}`);
  console.log('📡 Simulating IoT Hub message routing for local testing');
});