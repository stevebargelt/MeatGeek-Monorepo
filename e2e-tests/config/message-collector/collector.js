/**
 * Message collector for E2E test validation
 * Monitors and validates telemetry messages from IoT Edge modules
 */

const express = require('express');
const fs = require('fs-extra');
const path = require('path');

const app = express();
const port = 3001;

const config = {
  expectedMessageCount: parseInt(process.env.EXPECTED_MESSAGE_COUNT) || 30,
  collectionTimeout: parseInt(process.env.COLLECTION_TIMEOUT) || 120,
  logLevel: process.env.LOG_LEVEL || 'info',
  resultsPath: process.env.RESULTS_PATH || '/app/results'
};

let collectedMessages = [];
let startTime = Date.now();
let isCollecting = true;

app.use(express.json());

// Endpoint for receiving telemetry messages
app.post('/telemetry', (req, res) => {
  if (!isCollecting) {
    return res.status(503).json({ message: 'Collection finished' });
  }

  const message = {
    timestamp: new Date().toISOString(),
    receivedAt: Date.now(),
    data: req.body,
    messageId: collectedMessages.length + 1
  };

  collectedMessages.push(message);
  
  if (config.logLevel === 'debug') {
    console.log(`📥 Message ${message.messageId}: ${JSON.stringify(message.data)}`);
  } else {
    console.log(`📥 Collected message ${message.messageId}/${config.expectedMessageCount}`);
  }

  // Check if we've reached the expected count
  if (collectedMessages.length >= config.expectedMessageCount) {
    finishCollection('target_reached');
  }

  res.json({ 
    status: 'received', 
    messageId: message.messageId,
    totalCollected: collectedMessages.length 
  });
});

// Health check
app.get('/health', (req, res) => {
  res.json({
    status: 'healthy',
    collecting: isCollecting,
    messagesCollected: collectedMessages.length,
    expectedMessages: config.expectedMessageCount,
    elapsedSeconds: Math.floor((Date.now() - startTime) / 1000)
  });
});

// Status endpoint
app.get('/status', (req, res) => {
  res.json({
    collecting: isCollecting,
    messagesCollected: collectedMessages.length,
    expectedMessages: config.expectedMessageCount,
    elapsedSeconds: Math.floor((Date.now() - startTime) / 1000),
    timeoutSeconds: config.collectionTimeout
  });
});

// Results endpoint
app.get('/results', (req, res) => {
  const results = generateResults();
  res.json(results);
});

function generateResults() {
  const endTime = Date.now();
  const elapsedSeconds = Math.floor((endTime - startTime) / 1000);
  
  const results = {
    summary: {
      totalMessages: collectedMessages.length,
      expectedMessages: config.expectedMessageCount,
      successRate: (collectedMessages.length / config.expectedMessageCount) * 100,
      elapsedSeconds: elapsedSeconds,
      messagesPerSecond: collectedMessages.length / elapsedSeconds,
      status: collectedMessages.length >= config.expectedMessageCount ? 'success' : 'incomplete'
    },
    messages: collectedMessages.map(msg => ({
      messageId: msg.messageId,
      timestamp: msg.timestamp,
      deviceId: msg.data?.deviceId || 'unknown',
      sessionId: msg.data?.sessionId || 'unknown',
      grillTemp: msg.data?.temps?.grillTemp || msg.data?.probes?.[0]?.currentTemp,
      probe1Temp: msg.data?.temps?.probe1Temp || msg.data?.probes?.[1]?.currentTemp
    })),
    validation: {
      hasDeviceId: collectedMessages.every(m => m.data?.deviceId),
      hasTemperatureData: collectedMessages.every(m => 
        m.data?.temps?.grillTemp !== undefined || 
        m.data?.probes?.[0]?.currentTemp !== undefined
      ),
      hasTimestamps: collectedMessages.every(m => m.data?.timestamp),
      temperatureRangeValid: collectedMessages.every(m => {
        const temp = m.data?.temps?.grillTemp || m.data?.probes?.[0]?.currentTemp;
        return temp >= 50 && temp <= 600; // Reasonable temperature range
      })
    }
  };

  return results;
}

async function finishCollection(reason) {
  if (!isCollecting) return;
  
  isCollecting = false;
  console.log(`🏁 Collection finished: ${reason}`);
  
  // Generate final results
  const results = generateResults();
  
  // Write results to file
  await fs.ensureDir(config.resultsPath);
  const resultsFile = path.join(config.resultsPath, 'message-collection-results.json');
  await fs.writeJson(resultsFile, results, { spaces: 2 });
  
  console.log(`📊 Results written to: ${resultsFile}`);
  console.log(`📈 Final stats: ${results.summary.totalMessages}/${results.summary.expectedMessages} messages (${results.summary.successRate.toFixed(1)}%)`);
  
  // Exit after a short delay to allow final requests
  setTimeout(() => {
    process.exit(results.summary.status === 'success' ? 0 : 1);
  }, 2000);
}

// Set up timeout
setTimeout(() => {
  finishCollection('timeout');
}, config.collectionTimeout * 1000);

app.listen(port, '0.0.0.0', () => {
  console.log(`📊 Message Collector running on port ${port}`);
  console.log(`🎯 Collecting up to ${config.expectedMessageCount} messages`);
  console.log(`⏱️  Timeout: ${config.collectionTimeout} seconds`);
  startTime = Date.now();
});