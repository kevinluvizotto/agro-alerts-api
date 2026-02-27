agro-alerts-api

API de Alertas do projeto AgroSolutions IoT (FIAP Tech Challenge – Fase 5).

Responsável por:
- Consumir eventos da Telemetry API via RabbitMQ/MassTransit
- Gerar alertas (ex.: baixa umidade, risco de seca, etc.)
- Expor endpoints para consulta e ack (resolver alerta)
- Endpoints protegidos por JWT

STACK
- .NET (Minimal API / ASP.NET)
- Azure SQL
- RabbitMQ + MassTransit (consumers)
- JWT Bearer Authentication
- Swagger/OpenAPI

FLUXO (ALTO NÍVEL)
1) Recebe evento sensor-reading-* no RabbitMQ
2) Executa regras (ex.: soilMoisture < 30%)
3) Persiste alerta e disponibiliza via API

PRINCIPAIS ENDPOINTS (EXEMPLO)
- GET /health
- GET /alerts?plotId=...&ack=false&severity=WARN
- PUT /alerts/{id}/ack

CONFIGURAÇÃO (ENV VARS)
- ConnectionStrings__Default
- Jwt__Issuer
- Jwt__Audience
- Jwt__Key
- Rabbit__Host
- Rabbit__User
- Rabbit__Pass

RODAR LOCALMENTE
Requer RabbitMQ (veja o README do agro-telemetry-api ou use o agro-platform).
dotnet restore
dotnet run

TESTE RÁPIDO (COM JWT)
TOKEN="(cole o token aqui)"
curl -s "http://localhost:8083/alerts?ack=false" \
  -H "Authorization: Bearer $TOKEN"

OBSERVAÇÕES PARA AKS/INGRESS
Quando publicado atrás do Ingress, costuma ser acessado via:
- /alerts/...