# 🚀 Deployment Guide

## 📋 Tổng quan

- **SSL Setup**: Chỉ cần chạy 1 lần đầu tiên
- **CICD Deployment**: Tự động qua GitHub Actions
- **Manual Deployment**: Dùng script có sẵn

---

## 🔧 Setup ban đầu (Chỉ chạy 1 lần)

### Bước 1: SSH vào server và clone repo

```bash
git clone https://github.com/your-repo/eduva-backend.git
cd eduva-backend
```

### Bước 2: Setup environment variables

```bash
# Tạo file .env với các secrets cần thiết
cp .env.example .env
# Chỉnh sửa .env với các giá trị thực tế
```

### Bước 3: Setup SSL Certificate (CHỈ CHẠY 1 LẦN)

```bash
chmod +x scripts/init-letsencrypt.sh
./scripts/init-letsencrypt.sh
```

Sau khi chạy xong, bạn có:

- ✅ SSL certificate từ Let's Encrypt
- ✅ Nginx chạy với HTTPS
- ✅ Auto-renewal certbot (tự động gia hạn)

---

## 🔄 CICD Deployment (Tự động)

### GitHub Actions Workflow

- **Trigger**: Khi có image mới được publish
- **Manual**: Có thể trigger manual qua GitHub UI
- **Process**:
  1. Pull latest backend image
  2. Update chỉ backend service (zero-downtime)
  3. Health check
  4. Cleanup old images

### Workflow chỉ làm những việc này:

```bash
# 1. Pull image mới
docker compose pull eduva-api

# 2. Update backend (không restart nginx/ssl)
docker compose up -d eduva-api

# 3. Health check
curl -f http://localhost/health

# 4. Cleanup
docker image prune -f
```

---

## 🛠️ Manual Deployment

### Option 1: Dùng script có sẵn

```bash
chmod +x scripts/deploy.sh
./scripts/deploy.sh
```

### Option 2: Manual commands

```bash
# Pull image mới
docker compose pull eduva-api

# Update backend
docker compose up -d eduva-api

# Kiểm tra logs
docker compose logs eduva-api -f
```

---

## 🌐 Các endpoint

- **API**: `https://eduva.tech/api/`
- **Health Check**: `https://eduva.tech/health`
- **Swagger**: `https://eduva.tech/swagger`
- **Portainer**: `https://eduva.tech/portainer/`

---

## 🔍 Troubleshooting

### Backend không healthy

```bash
# Xem logs
docker compose logs eduva-api --tail=50

# Restart service
docker compose restart eduva-api

# Xem resource usage
docker stats eduva-api
```

### SSL certificate issues

```bash
# Kiểm tra certificate
openssl x509 -in ./certbot/conf/live/eduva.tech/fullchain.pem -text -noout

# Kiểm tra expiry
openssl x509 -checkend 86400 -noout -in ./certbot/conf/live/eduva.tech/fullchain.pem

# Manual renew nếu cần
docker compose run --rm certbot certbot renew
docker compose exec nginx nginx -s reload
```

### CICD workflow fails

```bash
# Kiểm tra GitHub Actions logs
# Thường do:
# 1. Health check timeout
# 2. Image pull fails
# 3. Environment variables missing
```

---

## ⚠️ Lưu ý quan trọng

1. **SSL chỉ setup 1 lần**: Không cần lo về SSL trong CICD
2. **Zero-downtime**: Nginx không restart khi deploy
3. **Auto-cleanup**: Old images tự động xóa
4. **Health check**: Workflow tự động kiểm tra health
5. **Rollback**: Deploy image cũ để rollback

---

## 📝 Checklist lần đầu setup

- [ ] Clone repo về server
- [ ] Setup .env file với secrets
- [ ] Chạy `init-letsencrypt.sh` để có SSL
- [ ] Test HTTPS endpoints
- [ ] Setup GitHub Actions secrets
- [ ] Test 1 lần deploy qua GitHub Actions

Sau khi checklist xong → CICD hoạt động tự động! 🎉
