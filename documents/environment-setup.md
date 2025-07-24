# Environment Setup

This document describes how to configure environment variables and secret settings for the Eduva Backend project.

## Configuration Files

### 1. .env (for Docker/local development)

Use `.env.example` as a template for environment variables when running with Docker or local development. Copy to `.env` and fill in your values.

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
    "QueueName": "eduva_ai_tasks_queue",
    "ExchangeName": "eduva_exchange",
    "RoutingKey": "eduva_routing_key",
    "DeadLetterQueueName": "eduva_ai_tasks_dlq_queue",
    "DeadLetterExchange": "eduva_ai_task_dlq",
    "DeadLetterRoutingKey": "eduva_dlq_routing_key"
  }
}
```

## Notes

- For development, use the .NET Secret Manager to store secrets securely: `dotnet user-secrets set "Key" "Value"`
- For production, use environment variables or secure configuration providers.
- Never commit secrets.json or .env files with real credentials to source control.
- See `.env.example` for Docker/local environment variable reference.

---

> For more details, see the official ASP.NET Core documentation on [Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/).
