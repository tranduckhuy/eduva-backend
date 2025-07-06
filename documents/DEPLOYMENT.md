# 🚀 Deployment Guide

## 📋 Overview

- **SSL Setup**: Only needs to be run once initially
- **CICD Deployment**: Automated via GitHub Actions
- **Manual Deployment**: Use available scripts

---

## 🔧 Initial Setup (Run only once)

### Step 1: SSH to server and clone repo

```bash
git clone https://github.com/your-repo/eduva-backend.git
cd eduva-backend
```

### Step 2: Setup environment variables

```bash
# Create .env file with necessary secrets
cp .env.example .env
# Edit .env with actual values
```

### Step 3: Setup SSL Certificate (RUN ONLY ONCE)

```bash
chmod +x scripts/init-letsencrypt.sh
./scripts/init-letsencrypt.sh
```

After completion, you have:

- ✅ SSL certificate from Let's Encrypt
- ✅ Nginx running with HTTPS
- ✅ Auto-renewal certbot (automatic renewal)

---

## 🔄 CICD Deployment (Automated)

### GitHub Actions Workflow

- **Trigger**: When new image is published
- **Manual**: Can be triggered manually via GitHub UI
- **Process**:
  1. Pull latest backend image
  2. Update only backend service (zero-downtime)
  3. Health check
  4. Cleanup old images

### Workflow only performs these tasks:

```bash
# 1. Pull new image
docker compose pull eduva-api

# 2. Update backend (don't restart nginx/ssl)
docker compose up -d eduva-api

# 3. Health check
curl -f http://localhost/health

# 4. Cleanup
docker image prune -f
```

---

## 🛠️ Manual Deployment

### Option 1: Use available script

```bash
chmod +x scripts/deploy.sh
./scripts/deploy.sh
```

### Option 2: Manual commands

```bash
# Pull new image
docker compose pull eduva-api

# Update backend
docker compose up -d eduva-api

# Check logs
docker compose logs eduva-api -f
```

---

## 🌐 Endpoints

- **API**: `https://eduva.tech/api/`
- **Health Check**: `https://eduva.tech/health`
- **Swagger**: `https://eduva.tech/swagger`
- **Portainer**: `https://eduva.tech/portainer/`

---

## 🔍 Troubleshooting

### Backend not healthy

```bash
# View logs
docker compose logs eduva-api --tail=50

# Restart service
docker compose restart eduva-api

# Check resource usage
docker stats eduva-api
```

### SSL certificate issues

```bash
# Check certificate
openssl x509 -in ./certbot/conf/live/eduva.tech/fullchain.pem -text -noout

# Check expiry
openssl x509 -checkend 86400 -noout -in ./certbot/conf/live/eduva.tech/fullchain.pem

# Manual renew if needed
docker compose run --rm certbot certbot renew
docker compose exec nginx nginx -s reload
```

### CICD workflow fails

```bash
# Check GitHub Actions logs
# Usually caused by:
# 1. Health check timeout
# 2. Image pull fails
# 3. Environment variables missing
```

---

## ⚠️ Important Notes

1. **SSL setup only once**: No need to worry about SSL in CICD
2. **Zero-downtime**: Nginx doesn't restart during deployment
3. **Auto-cleanup**: Old images are automatically removed
4. **Health check**: Workflow automatically checks health
5. **Rollback**: Deploy old image to rollback

---

## 📝 Initial Setup Checklist

- [ ] Clone repo to server
- [ ] Setup .env file with secrets
- [ ] Run `init-letsencrypt.sh` to get SSL
- [ ] Test HTTPS endpoints
- [ ] Setup GitHub Actions secrets
- [ ] Test deployment once via GitHub Actions

After checklist completion → CICD works automatically! 🎉
