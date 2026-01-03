// MongoDB initialization script for DataWhisper Analytics

// Switch to the analytics database
db = db.getSiblingDB('datawhisper_analytics');

// Create collections and indexes
db.createCollection('query_history');
db.createCollection('query_analytics');
db.createCollection('error_logs');

// Create indexes for query_history
db.query_history.createIndex({ "timestamp": -1 });
db.query_history.createIndex({ "requestId": 1 }, { unique: true });
db.query_history.createIndex({ "success": 1 });
db.query_history.createIndex({ "tablesAccessed": 1 });
db.query_history.createIndex({ "aiGenerated": 1 });

// Create indexes for query_analytics
db.query_analytics.createIndex({ "date": -1 }, { unique: true });
db.query_analytics.createIndex({ "totalQueries": -1 });

// Create indexes for error_logs
db.error_logs.createIndex({ "timestamp": -1 });
db.error_logs.createIndex({ "errorType": 1 });

// Insert sample analytics data (optional)
db.query_analytics.insertOne({
  date: new Date(),
  totalQueries: 0,
  successfulQueries: 0,
  failedQueries: 0,
  avgExecutionTime: 0,
  topTables: [],
  aiGeneratedQueries: 0,
  errorTypes: {}
});

print('MongoDB initialized successfully for DataWhisper Analytics');
print('Collections created: query_history, query_analytics, error_logs');