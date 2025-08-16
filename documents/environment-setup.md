# Environment Setup

This document describes how to configure environment variables and secret settings for the Eduva Backend project.

## Configuration Files

### 1. .env (for Docker/local development)

Use `.env.example` as a template for environment variables when running with Docker or local development. Copy to `.env` and fill in your values.

Example structure:

```env
# Database
CONNECTION_STRING=Server=your-server;Port=5432;Database=your-database;User Id=your-user;Password=your-password;

# JWT Settings
JWT_SECRET_KEY=your-jwt-secret-key
JWT_VALID_ISSUER=your-issuer
JWT_VALID_AUDIENCE=your-audience
JWT_EXPIRY_IN_SECONDS=3600

# Email Configuration
# If you use Brevo (Sendinblue), only set EMAIL_API_KEY, EMAIL_FROM, and EMAIL_USERNAME. The SMTP variables below are optional.
EMAIL_API_KEY=your-email-api-key
EMAIL_FROM=noreply@eduva.tech
EMAIL_USERNAME=your-email-username
EMAIL_SMTP_SERVER=smtp.gmail.com    # optional, only needed if using traditional SMTP (e.g. Gmail)
EMAIL_SMTP_PORT=465                 # optional, only needed if using traditional SMTP
EMAIL_PASSWORD=your-email-password  # optional, only needed if using traditional SMTP

# Redis
REDIS_CONNECTION_STRING=eduva-redis:6379
REDIS_INSTANCE_NAME=EduvaRedis

# Azure Blob Storage
AZURE_BLOB_STORAGE_CONNECTION_STRING=your-azure-connection-string
AZURE_BLOB_STORAGE_CONTAINER_NAME=eduva-files
AZURE_BLOB_STORAGE_TEMP_CONTAINER_NAME=eduva-temp
AZURE_BLOB_STORAGE_ACCOUNT_NAME=your-storage-account
AZURE_BLOB_STORAGE_ACCOUNT_KEY=your-storage-key

# PayOS
PAYOS_CLIENT_ID=your-payos-client-id
PAYOS_API_KEY=your-payos-api-key
PAYOS_CHECKSUM_KEY=your-payos-checksum-key

# RabbitMQ
RABBITMQ_DEFAULT_USER=eduva
RABBITMQ_DEFAULT_PASS=eduva2025
RABBITMQ_EXCHANGE_NAME=eduva_exchange
CONTENT_QUEUE_NAME=eduva.content.queue
PRODUCT_QUEUE_NAME=eduva.product.queue
CONTENT_ROUTING_KEY=eduva.content.routing_key
PRODUCT_ROUTING_KEY=eduva.product.routing_key
RABBITMQ_DLQ_NAME=eduva.dlq
RABBITMQ_DEAD_LETTER_EXCHANGE=eduva.dlq.exchange
RABBITMQ_DLQ_ROUTING_KEY=eduva.dlq.routing_key

# Worker
WORKER_API_KEY=your-worker-api-key
```

### 2. appsettings.json / appsettings.Development.json (for C# backend)

Configuration for production and development environments. Sensitive values should be stored in `secrets.json` using the .NET Secret Manager during development.

### 3. secrets.json (for local development)

Store sensitive values outside of source control. Example structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=Eduva;User Id=eduva;Password=eduva2025;"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "ValidIssuer": "https://localhost:9001",
    "ValidAudience": "https://localhost:4200",
    "ExpiryInSecond": 3600
  },
  "EmailConfiguration": {
    "ApiKey": "your-email-api-key",
    "From": "noreply@eduva.tech",
    "SmtpServer": "smtp.gmail.com",
    "Port": 465,
    "Username": "your-email",
    "Password": "your-email-password"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "EduvaRedis"
  },
  "AzureBlobStorage": {
    "ConnectionString": "your-azure-connection-string",
    "ContainerName": "eduva-files",
    "TemporaryContainerName": "eduva-temp",
    "StorageAccountName": "your-storage-account",
    "StorageAccountKey": "your-storage-key"
  },
  "PayOS": {
    "PAYOS_CLIENT_ID": "your-payos-client-id",
    "PAYOS_API_KEY": "your-payos-api-key",
    "PAYOS_CHECKSUM_KEY": "your-payos-checksum-key"
  },
  "RabbitMQ": {
    "ConnectionUri": "amqp://user:pass@localhost:5679",
    "ExchangeName": "eduva_exchange",
    "QueueNames": {
      "GenerateContent": "eduva.content.queue",
      "CreateProduct": "eduva.product.queue"
    },
    "RoutingKeys": {
      "GenerateContent": "eduva.content.routing_key",
      "CreateProduct": "eduva.product.routing_key"
    },
    "DeadLetterQueueName": "eduva.dlq",
    "DeadLetterExchange": "eduva.dlq.exchange",
    "DeadLetterRoutingKey": "eduva.dlq.routing_key"
  },
  "WorkerApiKey": "your-worker-api-key"
}
```

## Notes

- For development, use the .NET Secret Manager to store secrets securely: `dotnet user-secrets set "Key" "Value"`
- For production, use environment variables or secure configuration providers.
- Never commit secrets.json or .env files with real credentials to source control.
- See `.env.example` for Docker/local environment variable reference.

---

> For more details, see the official ASP.NET Core documentation on [Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/).
