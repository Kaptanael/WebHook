-- Test data: one connection + two endpoints for inbound testing
-- Run this script in SQL Server Management Studio against MVPWebhookDB

USE [MVPWebhookDB]
GO

-- 1. Create a test company connection (tenant) if it doesn't exist
DECLARE @ConnectionId UNIQUEIDENTIFIER = '550e8400-e29b-41d4-a716-446655440000'
IF NOT EXISTS (SELECT 1 FROM [dbo].[WebHookConnection] WHERE [Id] = @ConnectionId)
BEGIN
    INSERT INTO [dbo].[WebHookConnection]
        ([Id], [CompanyId], [ApplicationName], [ClientToken], [IsActive], [MVPApiToken], [MVPApiRefreshToken], [MVPApiExpiresIn], [CreatedAtUtc])
    VALUES
        (@ConnectionId, 1, 'Test Company', 'test-client-token-123', 1, 'mock-api-token', 'mock-refresh', DATEADD(hour, 1, GETUTCDATE()), GETUTCDATE())
    PRINT 'Created test WebHookConnection (ID: ' + CONVERT(NVARCHAR(36), @ConnectionId) + ', CompanyId: 1)'
END
ELSE
    PRINT 'Test WebHookConnection already exists'
GO

-- 2. Standard Webhooks endpoint with X-Api-Key
DECLARE @Endpoint1 UNIQUEIDENTIFIER = '650e8400-e29b-41d4-a716-446655440001'
IF NOT EXISTS (SELECT 1 FROM [dbo].[WebhookEndpoints] WHERE [Id] = @Endpoint1)
BEGIN
    INSERT INTO [dbo].[WebhookEndpoints]
        ([Id], [EndPointToken], [Endpoint], [CompanyId], [TriggerConfigJson], [ActionDataSchema], [SigningSecret], [IsActive], [CreatedAtUtc])
    VALUES
        (@Endpoint1,
         'api-key-secret-123',
         'https://webhook.example/inbound/standard-apikey',
         1,
         N'{"triggerType":"order.created"}',
         N'{"type":"object"}',
         'whsec_test1234567890abcdefghijklmnopqr',
         1,
         GETUTCDATE())
    PRINT 'Created endpoint 1: Standard Webhooks + X-Api-Key'
    PRINT '  - EndPointToken (for X-Api-Key): api-key-secret-123'
    PRINT '  - SigningSecret (for SW signature): whsec_test1234567890abcdefghijklmnopqr'
END
ELSE
    PRINT 'Endpoint 1 already exists'
GO

-- 3. Custom token endpoint
DECLARE @Endpoint2 UNIQUEIDENTIFIER = '650e8400-e29b-41d4-a716-446655440002'
IF NOT EXISTS (SELECT 1 FROM [dbo].[WebhookEndpoints] WHERE [Id] = @Endpoint2)
BEGIN
    INSERT INTO [dbo].[WebhookEndpoints]
        ([Id], [EndPointToken], [Endpoint], [CompanyId], [TriggerConfigJson], [ActionDataSchema], [SigningSecret], [IsActive], [CreatedAtUtc])
    VALUES
        (@Endpoint2,
         'custom-token-secret-456',
         'https://webhook.example/inbound/custom-token',
         1,
         N'{"triggerType":"order.updated"}',
         N'{"type":"object"}',
         '',
         1,
         GETUTCDATE())
    PRINT 'Created endpoint 2: Custom token'
    PRINT '  - EndPointToken (for token auth): custom-token-secret-456'
    PRINT '  - SigningSecret: (empty, not used for token auth)'
END
ELSE
    PRINT 'Endpoint 2 already exists'
GO

PRINT ''
PRINT '=============================================='
PRINT 'Test endpoints ready for HTML tester'
PRINT '=============================================='
PRINT ''
PRINT 'Endpoint 1 (Standard Webhooks + X-Api-Key):'
PRINT '  - X-Api-Key header value: api-key-secret-123'
PRINT '  - Standard Webhooks secret: whsec_test1234567890abcdefghijklmnopqr'
PRINT ''
PRINT 'Endpoint 2 (Custom token):'
PRINT '  - token header value: custom-token-secret-456'
PRINT ''
PRINT 'Both endpoints queue events to CompanyId=1 (test connection).'
GO
