-- Seed data for inbound webhook testing
-- This creates a WebHookConnection and two WebhookEndpoints for testing /api/inbound

USE [MVPWebhookDB]
GO

PRINT '========================================='
PRINT 'Seeding Inbound Webhook Test Data'
PRINT '========================================='

-- 1. Create WebHookConnection (required for FK constraint)
DECLARE @ConnectionId UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000001'
DECLARE @CompanyId INT = 100

IF NOT EXISTS (SELECT 1 FROM [dbo].[WebHookConnection] WHERE [Id] = @ConnectionId)
BEGIN
    INSERT INTO [dbo].[WebHookConnection]
        ([Id], [CompanyId], [ApplicationName], [ClientToken], [IsActive], [MVPApiToken], [MVPApiRefreshToken], [MVPApiExpiresIn], [CreatedAtUtc])
    VALUES
        (@ConnectionId,
         @CompanyId,
         'Inbound Test Company',
         'test-client-token-inbound-001',
         1,
         'mock-api-token-test',
         'mock-refresh-test',
         DATEADD(hour, 1, GETUTCDATE()),
         GETUTCDATE())
    PRINT 'Created WebHookConnection:'
    PRINT '  - ID: ' + CONVERT(NVARCHAR(36), @ConnectionId)
    PRINT '  - CompanyId: ' + CONVERT(NVARCHAR(10), @CompanyId)
    PRINT '  - ClientToken: test-client-token-inbound-001'
END
ELSE
    PRINT 'WebHookConnection already exists'

GO

-- 2. Create Inbound Endpoint 1: X-Api-Key authentication
DECLARE @Endpoint1Id UNIQUEIDENTIFIER = 'B0000000-0000-0000-0000-000000000001'
DECLARE @CompanyId INT = 100

IF NOT EXISTS (SELECT 1 FROM [dbo].[WebhookEndpoints] WHERE [Id] = @Endpoint1Id)
BEGIN
    INSERT INTO [dbo].[WebhookEndpoints]
        ([Id], [EndPointToken], [Endpoint], [CompanyId], [TriggerConfigJson], [ActionDataSchema], [SigningSecret], [IsActive], [CreatedAtUtc])
    VALUES
        (@Endpoint1Id,
         'inbound-apikey-secret-001',
         'https://webhook.test/inbound/apikey',
         @CompanyId,
         N'{"triggerType":"order.created"}',
         N'{"type":"object"}',
         '',
         1,
         GETUTCDATE())
    PRINT ''
    PRINT 'Created Inbound Endpoint 1 (X-Api-Key):'
    PRINT '  - ID: ' + CONVERT(NVARCHAR(36), @Endpoint1Id)
    PRINT '  - EndPointToken (for X-Api-Key header): inbound-apikey-secret-001'
    PRINT '  - CompanyId: ' + CONVERT(NVARCHAR(10), @CompanyId)
END
ELSE
    PRINT 'Endpoint 1 already exists'

GO

-- 3. Create Inbound Endpoint 2: Custom token authentication
DECLARE @Endpoint2Id UNIQUEIDENTIFIER = 'B0000000-0000-0000-0000-000000000002'
DECLARE @CompanyId INT = 100

IF NOT EXISTS (SELECT 1 FROM [dbo].[WebhookEndpoints] WHERE [Id] = @Endpoint2Id)
BEGIN
    INSERT INTO [dbo].[WebhookEndpoints]
        ([Id], [EndPointToken], [Endpoint], [CompanyId], [TriggerConfigJson], [ActionDataSchema], [SigningSecret], [IsActive], [CreatedAtUtc])
    VALUES
        (@Endpoint2Id,
         'inbound-token-secret-002',
         'https://webhook.test/inbound/token',
         @CompanyId,
         N'{"triggerType":"order.updated"}',
         N'{"type":"object"}',
         '',
         1,
         GETUTCDATE())
    PRINT ''
    PRINT 'Created Inbound Endpoint 2 (Custom Token):'
    PRINT '  - ID: ' + CONVERT(NVARCHAR(36), @Endpoint2Id)
    PRINT '  - EndPointToken (for token header): inbound-token-secret-002'
    PRINT '  - CompanyId: ' + CONVERT(NVARCHAR(10), @CompanyId)
END
ELSE
    PRINT 'Endpoint 2 already exists'

GO

-- 4. Verify data was inserted
PRINT ''
PRINT '========================================='
PRINT 'Verification'
PRINT '========================================='

PRINT ''
PRINT 'WebHookConnections:'
SELECT [Id], [CompanyId], [ApplicationName], [ClientToken], [IsActive]
FROM [dbo].[WebHookConnection]
WHERE [CompanyId] = 100

PRINT ''
PRINT 'Inbound WebhookEndpoints:'
SELECT [Id], [EndPointToken], [Endpoint], [CompanyId], [IsActive]
FROM [dbo].[WebhookEndpoints]
WHERE [CompanyId] = 100

PRINT ''
PRINT '========================================='
PRINT 'Test Data Ready'
PRINT '========================================='
PRINT ''
PRINT 'Test with /api/inbound endpoint:'
PRINT ''
PRINT 'Test 1 - X-Api-Key:'
PRINT '  POST http://localhost:5062/api/inbound'
PRINT '  Header: X-Api-Key = inbound-apikey-secret-001'
PRINT '  Header: X-Event-Type = order.created'
PRINT ''
PRINT 'Test 2 - Custom Token:'
PRINT '  POST http://localhost:5062/api/inbound'
PRINT '  Header: token = inbound-token-secret-002'
PRINT '  Header: X-Event-Type = order.updated'
PRINT ''
