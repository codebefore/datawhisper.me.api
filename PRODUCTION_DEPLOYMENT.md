# 🚀 DataWhisper Production Deployment Guide

**Version:** 2.0
**Last Updated:** 2026-01-03
**Status:** Production Ready ✅

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Network Configuration](#network-configuration)
4. [Server Requirements](#server-requirements)
5. [Ports and Endpoints](#ports-and-endpoints)
6. [GitHub Secrets](#github-secrets)
7. [Initial Setup](#initial-setup)
8. [Deployment](#deployment)
9. [Health Checks](#health-checks)
10. [Monitoring](#monitoring)
11. [Troubleshooting](#troubleshooting)
12. [Maintenance](#maintenance)

---

## 🎯 Overview

DataWhisper application consists of 3 main services:

| Service | Tech Stack | Port | Docker Image |
|---------|-----------|------|--------------|
| **API** | .NET 10.0 | 8080 | datawhisperme/datawhisper-api |
| **AI Service** | Python 3.11/Flask | 5003 | datawhisper-ai |
| **PostgreSQL** | 15-alpine | 5432 (internal) | postgres:15-alpine |
| **MongoDB** | 7.0 | 27017 (internal) | mongo:7.0 |
| **Redis** | 7-alpine | 6379 (internal) | redis:7-alpine |

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  Production Server                            │
│                                                              │
│  ┌──────────────┐         ┌──────────────┐                  │
│  │  AI Service  │         │     API      │                  │
│  │  :5003       │◄──────►│  :8080       │                  │
│  │  (Python)    │         │  (.NET)      │                  │
│  └──────┬───────┘         └──────┬───────┘                  │
│         │                        │                           │
│         └────────────────────────┼──────┐                   │
│                                 │      │                   │
│         ┌────────────────────────▼──────▼───────┐            │
│         │        datawhisperme_datawhisper-network │         │
│         └────────────────────────────────────────┘           │
│                                    │                        │
│         ┌────────────────────────▼──────────────┐           │
│         │              Redis :6379              │           │
│         │         (Metrics & Cache)            │           │
│         └───────────────────────────────────────┘           │
│                                                              │
│  ┌──────────────┐    ┌──────────────┐                     │
│  │ PostgreSQL   │    │   MongoDB    │                     │
│  │  :5432       │    │   :27017     │                     │
│  └──────────────┘    └──────────────┘                     │
│                                                              │
└──────────────────────────────────────────────────────────────┘
         ▲
         │ Nginx Reverse Proxy (SSL/Termination)
         │
    Client Browser (HTTPS)
```

---

## 🌐 Network Configuration

### **Network Name:** `datawhisperme_datawhisper-network`

**CRITICAL:** This name must be THE SAME everywhere:

| File | Network Name | Status |
|------|--------------|--------|
| `~/Repos/datawhisper.me.ai/docker-compose.yml` | `datawhisperme_datawhisper-network` | ✅ |
| `~/Repos/datawhisper.me.api/docker-compose.yml` | `datawhisperme_datawhisper-network` | ✅ |
| `~/Repos/datawhisper.me.ai/.github/workflows/deploy.yml` | `datawhisperme_datawhisper-network` | ✅ |
| **Production Server** | `datawhisperme_datawhisper-network` | ✅ |

### Network Properties:
- **Type:** External Bridge
- **Driver:** bridge
- **Scope:** Shared across all DataWhisper services

---

## 💻 Server Requirements

### Minimum:
- **CPU:** 2 core
- **RAM:** 4 GB
- **Disk:** 20 GB SSD

### Recommended:
- **CPU:** 4+ core
- **RAM:** 8+ GB
- **Disk:** 50+ GB SSD

### Software:
- **OS:** Ubuntu 22.04 LTS / Debian 12
- **Docker:** 24.0+
- **Docker Compose:** 2.20+
- **Git:** Latest

---

## 🔌 Ports and Endpoints

### Internal Ports (Docker Network):
```
PostgreSQL:   5432
MongoDB:      27017
Redis:        6379
```

### External Ports (127.0.0.1 bind):
```
API:          8080 → 127.0.0.1:8080
AI Service:   5003 → 127.0.0.1:5003
```

### Health Endpoints:
```
http://localhost:8080/api/system          → API Root
http://localhost:8080/api/system/metrics  → API Metrics (Redis)
http://localhost:8080/api/system/ai-status → AI Status
http://localhost:5003/api/health          → AI Health
```

---

## 🔑 GitHub Secrets

### API Repository (datawhisper.me.api)

**GitHub:** https://github.com/codebefore/datawhisper.me.api/settings/secrets/actions

| Secret Name | Required | Description |
|-------------|----------|-------------|
| `SERVER_HOST` | ✅ | Production server IP address |
| `SERVER_USER` | ✅ | SSH username (root) |
| `SERVER_PORT` | ✅ | SSH port (22) |
| `SSH_PRIVATE_KEY` | ✅ | SSH private key (PEM format) |
| `CORS_ALLOWED_ORIGINS` | ✅ | CORS origins (https://datawhisper.me) |
| `PROD_DB_PASSWORD` | ✅ | PostgreSQL password |
| `PROD_MONGO_PASSWORD` | ✅ | MongoDB password |
| `PROD_AI_SERVICE_URL` | ✅ | AI service URL (http://datawhisper-ai:5003) |
| `PROD_OPENAI_API_KEY` | ⚠️ | OpenAI API key (future use) |
| `PROD_RATE_LIMIT_QUERY` | ❌ | Rate limit for query endpoint (default: 30) |
| `PROD_RATE_LIMIT_ANALYTICS` | ❌ | Rate limit for analytics (default: 60) |
| `PROD_RATE_LIMIT_SYSTEM` | ❌ | Rate limit for system endpoint (default: 200) |
| `PROD_RATE_LIMIT_GLOBAL` | ❌ | Global rate limit (default: 100) |
| `PROD_RATE_LIMIT_GLOBAL_FALLBACK` | ❌ | Global fallback rate limit (default: 150) |
| `LARGE_DATASET_THRESHOLD` | ❌ | Pagination threshold (default: 10) |
| `REDIS_HOST` | ✅ | 🆕 Redis hostname (redis) |
| `REDIS_PORT` | ✅ | 🆕 Redis port (6379) |
| `REDIS_DB` | ✅ | 🆕 Redis database ID (0) |

### AI Repository (datawhisper.me.ai)

**GitHub:** https://github.com/codebefore/datawhisper.me.ai/settings/secrets/actions

| Secret Name | Required | Description |
|-------------|----------|-------------|
| `SERVER_HOST` | ✅ | Same production IP |
| `SERVER_USER` | ✅ | Same SSH user |
| `SERVER_PORT` | ✅ | Same SSH port |
| `SSH_PRIVATE_KEY` | ✅ | Same SSH key |
| `SERVICE_HOST` | ✅ | AI service bind address (0.0.0.0) |
| `SERVICE_PORT` | ✅ | AI service port (5003) |
| `SQL_MODEL` | ✅ | OpenAI model for SQL (gpt-4o-mini) |
| `SUGGESTION_MODEL` | ✅ | OpenAI model for suggestions (gpt-4o-mini) |
| `OPENAI_API_KEY` | ✅ | OpenAI API key |
| `REDIS_HOST` | ✅ | 🆕 Redis hostname (redis) |
| `REDIS_PORT` | ✅ | 🆕 Redis port (6379) |
| `REDIS_DB` | ✅ | 🆕 Redis database ID (0) |

---

## 🎯 Initial Setup (One-Time Setup)

### 1. SSH Key Setup

**On your local machine:**

```bash
# Generate SSH key pair
ssh-keygen -t ed25519 -C "github-actions-datawhisper" -f ~/.ssh/datawhisper_deploy

# Copy public key to server
ssh-copy-id -i ~/.ssh/datawhisper_deploy.pub root@PRODUCTION_SERVER_IP

# Test connection
ssh -i ~/.ssh/datawhisper_deploy root@PRODUCTION_SERVER_IP
```

**Add private key to GitHub:**
```bash
cat ~/.ssh/datawhisper_deploy
# Copy the content and add it as GitHub Secret: SSH_PRIVATE_KEY
```

### 2. Connect to Production Server

```bash
ssh root@PRODUCTION_SERVER_IP
```

### 3. Install Docker

```bash
# Update system
sudo apt-get update

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose
sudo apt-get install docker-compose-plugin

# Start Docker
sudo systemctl start docker
sudo systemctl enable docker

# Verify installation
docker --version
docker compose version
```

### 4. Create Network

```bash
# Create external network (VERY IMPORTANT!)
docker network create datawhisperme_datawhisper-network

# Verify
docker network ls | grep datawhisper
```

### 5. Create Deploy Directories

```bash
# API deploy directory
mkdir -p /root/datawhisper/api-deploy

# AI deploy directory
mkdir -p /root/datawhisper/ai-deploy
```

### 6. Synchronize Time

```bash
# Enable NTP
sudo timedatectl set-ntp true

# Check time
timedatectl status
date
```

---

## 🚀 Deployment

### Method 1: GitHub Actions (Automatic) ✅

#### Step 1: Add GitHub Secrets
Add all required secrets from the tables above to both repositories.

#### Step 2: Push Code

```bash
# API repository
cd ~/Repos/datawhisper.me.api
git add .
git commit -m "feat: production deployment"
git push origin main

# AI repository
cd ~/Repos/datawhisper.me.ai
git add .
git commit -m "feat: production deployment"
git push origin main
```

#### Step 3: Monitor GitHub Actions

- **API:** https://github.com/codebefore/datawhisper.me.api/actions
- **AI:** https://github.com/codebefore/datawhisper.me.ai/actions

Green ✅ = Success

---

### Method 2: Manual Deployment (Backup)

```bash
ssh root@PRODUCTION_SERVER_IP

# API deploy
cd /root/datawhisper/api-deploy
git pull
docker compose down
docker compose up -d --build

# AI deploy
cd /root/datawhisper/ai-deploy
git pull
docker compose down
docker compose up -d --build
```

---

## ✅ Health Checks

After deployment, verify all services are healthy:

### 1. Check Container Status

```bash
docker ps
```

**Expected output:**
```
datawhisper-api      Up      127.0.0.1:8080->8080/tcp
datawhisper-ai       Up      127.0.0.1:5003->5003/tcp
datawhisper-redis     Up      6379/tcp
datawhisper-db        Up      127.0.0.1:5433->5432/tcp
datawhisper-mongodb    Up      127.0.0.1:27017->27017/tcp
```

### 2. Database Health Checks

```bash
# Redis check
docker exec datawhisper-redis redis-cli ping
# Expected output: PONG

# PostgreSQL check
docker exec datawhisper-db pg_isready -U datawhisper_user
# Expected output: /tmp:5432 - accepting connections

# MongoDB check
docker exec datawhisper-mongodb mongosh --eval "db.adminCommand('ping')" --quiet
# Expected output: { ok: 1 }
```

### 3. API Health Check

```bash
# API root endpoint
curl http://localhost:8080/api/system

# API metrics (Redis)
curl http://localhost:8080/api/system/metrics | jq

# AI status
curl http://localhost:8080/api/system/ai-status | jq
```

### 4. AI Service Health Check

```bash
# AI health endpoint
curl http://localhost:5003/api/health | jq
```

### 5. Redis Keys Check

```bash
docker exec datawhisper-redis redis-cli KEYS "*"
```

**Expected keys:**
```
ai_requests:YYYY-MM-DD
ai_metrics:aggregate
dotnet_requests:YYYY-MM-DD
dotnet_metrics:aggregate
dotnet_errors:YYYY-MM-DD
```

---

## 📊 Monitoring

### Real-time Monitoring

```bash
# Container resource usage
docker stats

# Follow API logs
docker logs -f datawhisper-api

# Follow AI logs
docker logs -f datawhisper-ai

# Redis memory usage
docker exec datawhisper-redis redis-cli INFO memory
```

### Metrics Endpoints

```bash
# API metrics (JSON)
curl http://localhost:8080/api/system/metrics | jq '.'

# AI health (JSON)
curl http://localhost:5003/api/health | jq '.'
```

### Redis Monitoring

```bash
# All keys
docker exec datawhisper-redis redis-cli KEYS "*"

# Daily request counts
docker exec datawhisper-redis redis-cli HLEN dotnet_requests:$(date +%Y-%m-%d)
docker exec datawhisper-redis redis-cli HLEN ai_requests:$(date +%Y-%m-%d)

# Aggregate metrics
docker exec datawhisper-redis redis-cli HGETALL ai_metrics:aggregate
docker exec datawhisper-redis redis-cli HGETALL dotnet_metrics:aggregate
```

---

## 🔧 Troubleshooting

### Issue 1: Redis Connection Error

**Error:** `UnableToConnect on redis:6379`

**Solution:**
```bash
# Check network
docker network inspect datawhisperme_datawhisper-network

# Check Redis container
docker ps | grep redis

# Restart Redis
docker restart datawhisper-redis

# Restart AI service (to reconnect to Redis)
cd /root/datawhisper/ai-deploy
docker compose restart
```

---

### Issue 2: Network Not Found

**Error:** `network datawhispermeapi_datawhisper-network not found`

**Solution:**
```bash
# Create correct network
docker network create datawhisperme_datawhisper-network

# Verify
docker network ls | grep datawhisper
```

---

### Issue 3: Container Not Starting

**Error:** Container exited

**Solution:**
```bash
# Check logs
docker logs datawhisper-api --tail 50
docker logs datawhisper-ai --tail 50

# Restart
cd /root/datawhisper/api-deploy
docker compose restart
```

---

### Issue 4: Port Already in Use

**Error:** `port is already allocated`

**Solution:**
```bash
# Find what's using the port
lsof -i :8080
lsof -i :5003

# Stop old container
docker stop <container_name>
docker rm <container_name>
```

---

### Issue 5: Environment Variable Not Set

**Error:** `Key not found in environment`

**Solution:**
```bash
# Check environment variables
docker exec datawhisper-api env | grep REDIS
docker exec datawhisper-ai env | grep REDIS

# Check docker-compose.yml
cat docker-compose.yml | grep -A5 environment:
```

---

## 🔄 Maintenance

### Update Code

```bash
# On local machine
cd ~/Repos/datawhisper.me.api
git add .
git commit -m "feat: new feature"
git push origin main

# GitHub Actions will auto-deploy!
```

### Manual Update

```bash
ssh root@PRODUCTION_SERVER_IP

# Update API
cd /root/datawhisper/api-deploy
git pull
docker compose down
docker compose up -d --build

# Update AI
cd /root/datawhisper/ai-deploy
git pull
docker compose down
docker compose up -d --build
```

### Change Environment Variables

```bash
# Edit docker-compose.yml
cd /root/datawhisper/api-deploy
vim docker-compose.yml

# Restart containers
docker compose up -d
```

### Restart All Services

```bash
ssh root@PRODUCTION_SERVER_IP

# Stop all
cd /root/datawhisper/api-deploy
docker compose down

cd /root/datawhisper/ai-deploy
docker compose down

# Start all
cd /root/datawhisper/api-deploy
docker compose up -d

cd /root/datawhisper/ai-deploy
docker compose up -d
```

### Rollback to Previous Version

```bash
ssh root@PRODUCTION_SERVER_IP

# API rollback
cd /root/datawhisper/api-deploy
git log --oneline -5
git reset --hard <previous_commit_hash>
docker compose down
docker compose up -d --build

# AI rollback
cd /root/datawhisper/ai-deploy
git log --oneline -5
git reset --hard <previous_commit_hash>
docker compose down
docker compose up -d --build
```

---

## ✅ Deployment Checklist

### Pre-Deployment:
- [ ] All GitHub secrets added
- [ ] SSH keys working
- [ ] Docker and Docker Compose installed
- [ ] Network created (`datawhisperme_datawhisper-network`)
- [ ] Time synchronized
- [ ] Deploy directories created

### Post-Deployment:
- [ ] All containers running (6 containers)
- [ ] All services on same network
- [ ] API endpoints responding
- [ ] AI endpoints responding
- [ ] **Redis PING working**
- [ ] **PostgreSQL ready**
- [ ] **MongoDB responding**
- [ ] Metrics accumulating in Redis
- [ ] End-to-end test successful

---

## 📌 Important Notes

⚠️ **CRITICAL WARNINGS:**

1. **Network Name:** Always use `datawhisperme_datawhisper-network`
2. **Redis:** AI and API share the same Redis instance
3. **Ports:** Database ports are internal only (127.0.0.1)
4. **Secrets:** Never commit .env files to git
5. **Backup:** Do not perform major updates without database backup
6. **Health Checks:** All database services must pass health checks before API starts

---

## 📞 Support

If you encounter issues:

1. **Check this guide first**
2. **Review logs:** `docker logs <container>`
3. **Check network:** `docker network inspect datawhisperme_datawhisper-network`
4. **Review GitHub Actions logs**
5. **Check container status:** `docker ps -a`

---

**Last Updated:** 2026-01-03
**Version:** 2.0
**Status:** Production Ready ✅
