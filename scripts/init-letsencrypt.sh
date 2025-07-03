#!/bin/bash

domains=(eduva.tech)
email="huytde.work@gmail.com"

# Don't modify below this line unless you know what you're doing
rsa_key_size=4096
data_path="./certbot"
staging=0 # Set to 1 if you're testing your setup to avoid hitting request limits

# Create required directories
mkdir -p "$data_path/conf/live/$domains"
mkdir -p "$data_path/www"

# Create temporary directories to store the webroot challenge
mkdir -p "$data_path/www/.well-known/acme-challenge"
chmod -R 777 "$data_path/www"

echo "### Creating dummy certificate for $domains ..."
openssl req -x509 -nodes -newkey rsa:$rsa_key_size -days 1 \
  -keyout "$data_path/conf/live/$domains/privkey.pem" \
  -out "$data_path/conf/live/$domains/fullchain.pem" \
  -subj "/CN=localhost"

echo "### Starting all services ..."
docker compose down
docker compose up -d
echo "### Waiting for services to start ..."
sleep 30

# Verify nginx is working by checking its response
echo "### Testing nginx response ..."
curl -I http://localhost || echo "Warning: Cannot connect to nginx locally. This might be okay if you're on a remote system."

# Give additional time for all services to be fully ready
echo "### Waiting for all services to be fully ready ..."
sleep 10

echo "### Deleting dummy certificate for $domains ..."
rm -Rf "$data_path/conf/live/$domains"

echo "### Requesting Let's Encrypt certificate for $domains ..."
# Select appropriate email arg
case "$email" in
  "") email_arg="--register-unsafely-without-email" ;;
  *) email_arg="--email $email" ;;
esac

# Enable staging mode if needed
if [ $staging != "0" ]; then staging_arg="--staging"; fi

# Request the actual certificate with verbose output for debugging
docker compose run --rm --entrypoint "\
  certbot certonly --webroot -w /var/www/certbot \
    $staging_arg \
    $email_arg \
    -d ${domains[0]} \
    --rsa-key-size $rsa_key_size \
    --agree-tos \
    --force-renewal \
    --debug-challenges \
    -v" certbot

echo "### Reloading nginx ..."
docker compose exec nginx nginx -s reload