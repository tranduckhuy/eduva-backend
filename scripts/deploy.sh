#!/bin/bash

# Simple deployment script for CICD
# This script only updates the backend service without affecting nginx/SSL

echo "🚀 Starting deployment..."

# Pull latest images
echo "📥 Pulling latest images..."
docker compose pull eduva-api

# Update only the backend service
echo "🔄 Updating backend service..."
docker compose up -d eduva-api

# Wait for health check
echo "⏳ Waiting for backend to be healthy..."
for i in {1..30}; do
    if curl -f http://localhost/health > /dev/null 2>&1; then
        echo "✅ Backend is healthy!"
        break
    fi
    echo "⏳ Attempt $i/30: Waiting for backend..."
    sleep 10
done

# Check if backend is healthy
if curl -f http://localhost/health > /dev/null 2>&1; then
    echo "🎉 Deployment successful!"
    
    # Clean up old images
    echo "🧹 Cleaning up old images..."
    docker image prune -f
    
    echo "✅ Deployment completed successfully!"
else
    echo "❌ Deployment failed - backend not healthy"
    echo "📋 Container logs:"
    docker compose logs eduva-api --tail=50
    exit 1
fi
