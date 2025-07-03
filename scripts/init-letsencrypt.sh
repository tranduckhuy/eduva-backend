#!/bin/bash

# One-time SSL setup script
# Run this ONLY ONCE when setting up the server for the first time

domains=(eduva.tech)
email="huytde.work@gmail.com"
rsa_key_size=4096
data_path="./certbot"
staging=0 # Set to 1 if you're testing

echo "🔧 One-time SSL setup for ${domains[0]}..."

# Create required directories
mkdir -p "$data_path/conf/live/${domains[0]}"
mkdir -p "$data_path/www"

echo "📝 Creating dummy certificate for ${domains[0]}..."
openssl req -x509 -nodes -newkey rsa:$rsa_key_size -days 1 \
  -keyout "$data_path/conf/live/${domains[0]}/privkey.pem" \
  -out "$data_path/conf/live/${domains[0]}/fullchain.pem" \
  -subj "/CN=localhost"

echo "🚀 Starting all services..."
docker compose up -d

echo "⏳ Waiting for services to start..."
sleep 15

# Test nginx
if curl -f http://localhost/health > /dev/null 2>&1; then
    echo "✅ Nginx and backend are working!"
else
    echo "⚠️ Warning: Backend might not be ready yet, continuing with SSL setup..."
fi

echo "🗑️ Removing dummy certificate..."
rm -Rf "$data_path/conf/live/${domains[0]}"

echo "🔐 Requesting Let's Encrypt certificate..."
case "$email" in
  "") email_arg="--register-unsafely-without-email" ;;
  *) email_arg="--email $email" ;;
esac

if [ $staging != "0" ]; then staging_arg="--staging"; fi

docker compose run --rm --entrypoint "\
certbot certonly --webroot -w /var/www/certbot \
  $staging_arg \
  $email_arg \
  -d ${domains[0]} \
  --rsa-key-size $rsa_key_size \
  --agree-tos \
  --force-renewal" certbot

if [ -f "$data_path/conf/live/${domains[0]}/fullchain.pem" ]; then
    echo "✅ SSL certificate obtained successfully!"
    echo "🔄 Reloading nginx with SSL..."
    docker compose exec nginx nginx -s reload
    echo "🎉 SSL setup completed! Your site should now be available at https://${domains[0]}"
else
    echo "❌ Failed to obtain SSL certificate"
    echo "🔍 Check the logs above for details"
    exit 1
fi