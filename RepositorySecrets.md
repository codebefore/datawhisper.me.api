# Production Secrets (GitHub Repository Secrets)

## Database & Authentication
- `DB_PASSWORD` - PostgreSQL database password
- `MONGO_PASSWORD` - MongoDB password

## AI Service
- `AI_SERVICE_URL` - AI service endpoint URL (default: http://datawhisper-ai:5003)
- `OPENAI_API_KEY` - OpenAI API key (optional, if using OpenAI)

## Rate Limiting
- `RATE_LIMIT_QUERY` - Query endpoint rate limit (default: 30 requests/min)
- `RATE_LIMIT_ANALYTICS` - Analytics endpoint rate limit (default: 60 requests/min)
- `RATE_LIMIT_SYSTEM` - System endpoint rate limit (default: 200 requests/min)
- `RATE_LIMIT_GLOBAL` - Global rate limit (default: 100 requests/min)
- `RATE_LIMIT_GLOBAL_FALLBACK` - Fallback rate limit (default: 150 requests/min)

## CORS
- `CORS_ALLOWED_ORIGINS` - Allowed CORS origins (semicolon-separated)
  - Example: `https://datawhisper.me;https://www.datawhisper.me`

## Server Access
- `SERVER_HOST` - Production server IP/hostname
- `SERVER_PORT` - SSH port (default: 22)
- `SERVER_USER` - SSH username (default: root)
- `SSH_PRIVATE_KEY` - SSH private key for deployment