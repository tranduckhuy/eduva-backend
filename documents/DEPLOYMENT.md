# Eduva Backend Deployment Guide

## Quick Start

### Local Development
```bash
# Copy environment template
cp .env.example .env

# Edit .env file with your configuration
nano .env

# Start development environment
docker compose -f docker-compose.yaml -f docker-compose.dev.yaml up -d
```

### Production Deployment (with Development Features)
**Note**: This deployment runs in Development mode to enable Swagger UI and detailed error responses.

```bash
# Copy and configure environment
cp .env.example .env
nano .env

# Deploy with development features
docker compose up -d
```

### Production Deployment

#### Prerequisites
- Docker and Docker Compose installed
- Domain name pointing to your server
- GitHub self-hosted runner configured

#### Setup SSL Certificate
```bash
# Make script executable
chmod +x ./scripts/init-letsencrypt.sh

# Initialize Let's Encrypt SSL for eduva.tech
./scripts/init-letsencrypt.sh
```

#### Deploy
```bash
# Copy and configure environment
cp .env.example .env
nano .env

# Make deploy script executable
chmod +x ./scripts/deploy-local.sh

# Deploy
./scripts/deploy-local.sh
```

## CI/CD Setup

### GitHub Secrets Required
- `CONNECTION_STRING`
- `JWT_SECRET_KEY`
- `JWT_VALID_ISSUER`
- `JWT_VALID_AUDIENCE`
- `JWT_EXPIRY_IN_SECONDS`
- `EMAIL_API_KEY`
- `EMAIL_FROM`
- `EMAIL_SMTP_SERVER`
- `EMAIL_SMTP_PORT`
- `EMAIL_USERNAME`
- `EMAIL_PASSWORD`
- `REDIS_INSTANCE_NAME`
- `AZURE_BLOB_STORAGE_CONNECTION_STRING`
- `AZURE_BLOB_STORAGE_CONTAINER_NAME`
- `AZURE_BLOB_STORAGE_TEMP_CONTAINER_NAME`
- `AZURE_BLOB_STORAGE_ACCOUNT_NAME`
- `AZURE_BLOB_STORAGE_ACCOUNT_KEY`
- `PAYOS_CLIENT_ID`
- `PAYOS_API_KEY`
- `PAYOS_CHECKSUM_KEY`
- `PAYOS_RETURN_URL`
- `PAYOS_CANCEL_URL`
- `IMPORT_TEMPLATE_URL`
- `RABBITMQ_DEFAULT_USER`
- `RABBITMQ_DEFAULT_PASS`

### Self-hosted Runner Setup
1. Go to your GitHub repository
2. Settings → Actions → Runners
3. Add new self-hosted runner
4. Follow the setup instructions on your production server

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Internet      │───▶│   Nginx         │───▶│   Eduva API     │
│                 │    │   (Port 80/443) │    │   (Port 9001)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │                        │
                                │                        ▼
                       ┌─────────────────┐    ┌─────────────────┐
                       │   Certbot       │    │   Redis         │
                       │   (SSL Certs)   │    │   (Cache)       │
                       └─────────────────┘    └─────────────────┘
                                                        │
                                                        ▼
                                              ┌─────────────────┐
                                              │   RabbitMQ      │
                                              │   (Message Queue)│
                                              └─────────────────┘
```

## Services

### Nginx (Reverse Proxy)
- **Ports**: 80 (HTTP), 443 (HTTPS)
- **Purpose**: Load balancing, SSL termination, static file serving
- **Config**: `nginx/conf.d/default.conf`

### Eduva API
- **Port**: 9001 (internal)
- **Environment**: Production
- **Health Check**: `/health`

### Redis
- **Port**: 6379 (internal)
- **Purpose**: Caching, session storage

### RabbitMQ
- **Ports**: 5672 (AMQP), 15672 (Management UI)
- **Purpose**: Message queuing for background jobs

### Certbot
- **Purpose**: Automatic SSL certificate management
- **Renewal**: Every 24 hours

### Portainer
- **Port**: 9000
- **Purpose**: Docker container management UI

## Monitoring

### Health Checks
- **API**: `https://eduva.tech/api/` (các endpoints API)
- **Health Check**: `https://eduva.tech/health`
- **Swagger UI**: `https://eduva.tech/swagger` (Development mode enabled)
- **SignalR Hub**: `wss://eduva.tech/hubs/question-comment`
- **Portainer**: `https://eduva.tech/portainer/` (Container Management)
- **RabbitMQ Management**: `http://eduva.tech:15672` (chỉ port này vẫn expose trực tiếp)

**Note**: API, Redis và RabbitMQ AMQP port chỉ accessible nội bộ qua Docker network để tăng cường bảo mật.

### Logs
```bash
# View all logs
docker-compose logs

# View specific service logs
docker-compose logs eduva-api

# Follow logs
docker-compose logs -f
```

## Troubleshooting

### Common Issues

1. **SSL Certificate Issues**
   ```bash
   # Renew certificate manually
   docker compose run --rm certbot certonly --webroot -w /var/www/certbot -d eduva.tech
   ```

2. **API Not Responding**
   ```bash
   # Check API health
   docker compose exec eduva-api curl -f http://localhost:9001/health
   
   # Restart API
   docker compose restart eduva-api
   ```

3. **Database Connection Issues**
   - Check `CONNECTION_STRING` in `.env`
   - Ensure database server is accessible

4. **Image Pull Issues (Private Repository)**
   ```bash
   # Login to GitHub Container Registry
   docker login ghcr.io -u YOUR_GITHUB_USERNAME -p YOUR_GITHUB_TOKEN
   ```

### Useful Commands

```bash
# Check running containers
docker compose ps

# View resource usage
docker stats

# Clean up unused images
docker image prune -f

# Backup volumes
docker run --rm -v eduva-backend_redis-data:/source -v $(pwd):/backup alpine tar czf /backup/redis-backup.tar.gz -C /source .

# Restore volumes
docker run --rm -v eduva-backend_redis-data:/target -v $(pwd):/backup alpine tar xzf /backup/redis-backup.tar.gz -C /target
```
