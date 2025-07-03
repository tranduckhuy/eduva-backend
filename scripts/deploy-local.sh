#!/bin/bash

# Local deployment script
# Usage: ./deploy-local.sh

echo "🚀 Starting local deployment..."

# Check if .env file exists
if [ ! -f .env ]; then
    echo "❌ .env file not found. Please copy .env.example to .env and configure it."
    exit 1
fi

# Pull latest images
echo "📥 Pulling latest images..."
docker compose pull

# Stop existing services
echo "🛑 Stopping existing services..."
docker compose down

# Start services
echo "🔧 Starting services..."
docker compose up -d

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 30

# Check service health
echo "🏥 Checking service health..."
docker compose ps

# Show logs
echo "📋 Recent logs:"
docker compose logs --tail=20

echo "✅ Deployment completed!"
echo "🌐 API should be available at: https://eduva.tech/api"
echo "📖 Swagger UI available at: https://eduva.tech/swagger"
echo "📊 Portainer available at: https://eduva.tech/portainer/"
echo "🐰 RabbitMQ Management available at: http://eduva.tech:15672"
echo ""
echo "🔒 Security Note: API, Redis và RabbitMQ AMQP chỉ accessible nội bộ"
