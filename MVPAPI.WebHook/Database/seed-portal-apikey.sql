-- ============================================================================
-- Seed data: PortalDB API key for inbound "full Standard Webhooks" testing.
--
-- The inbound pipeline matches the X-Api-Key header against ApiKeys.RawApiKey,
-- then verifies the webhook-id/webhook-timestamp/webhook-signature triplet using
-- ApiKeys.Salt as the HMAC-SHA256 key (Salt is base64; its decoded bytes are the key).
-- CompanyId ties the request to a WebHookConnection + active WebhookEndpoint.
--
-- CompanyId 100 is used so this lines up with seed-inbound.sql, which already creates
-- an active WebHookConnection and WebhookEndpoint for company 100. Run seed-inbound.sql
-- (against MVPWebhookDB) first, then this script (against PortalDB).
--
-- Schema matched to the live [PortalDB].[dbo].[ApiKeys]:
--   Status      tinyint NOT NULL  -> 1 = Active (0 = inactive). ApiKey.IsActive treats "1" as active.
--   Environment tinyint NOT NULL  -> 0 = Development, 1 = Production (not used by the webhook app).
--   CompanyId   bigint  NULL,  Salt nvarchar(64) NOT NULL (base64, 32 bytes = 44 chars),
--   RawApiKey   nvarchar(512) NULL,  IssuedAt/UpdatedAt datetime NOT NULL,  IssuedBy bigint NOT NULL.
-- This inserts a clearly-marked test row (fixed Id) into the real table; delete by that Id to undo.
-- ============================================================================

USE [PortalDB]
GO

PRINT '========================================='
PRINT 'Seeding PortalDB ApiKeys Test Data'
PRINT '========================================='

DECLARE @ApiKeyId UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000001'
DECLARE @CompanyId BIGINT = 100

-- Read by the webhook app:
DECLARE @RawApiKey NVARCHAR(512) = 'a9fe0f92103b9e2d2a6e0c0e5d5379bc7ecf1878b8321488c013bf01fe383157'  -- X-Api-Key header value
DECLARE @Salt      NVARCHAR(64)  = '8I5Buxsdrq5zNJOZ83mvXTrCMLdnbxHll2hHZNQPQio='                       -- base64; HMAC key = base64decode(Salt)

IF NOT EXISTS (SELECT 1 FROM [dbo].[ApiKeys] WHERE [Id] = @ApiKeyId)
BEGIN
    INSERT INTO [dbo].[ApiKeys]
        ([Id], [ApiKeyHash], [Salt], [ApplicationName], [CompanyId], [AllowedIPs],
         [Scopes], [Status], [Environment], [JsonConfig], [IssuedAt], [UpdatedAt],
         [IssuedBy], [UpdatedBy], [RawApiKey], [AppRefId], [ApplicationType])
    VALUES
        (@ApiKeyId,
         'seed-placeholder-hash',          -- ApiKeyHash NOT NULL (not used by the webhook app)
         @Salt,                            -- Salt NOT NULL (base64) — the HMAC signing secret
         'Inbound Webhook Test App',       -- ApplicationName NOT NULL
         @CompanyId,                       -- CompanyId (bigint)
         NULL,                             -- AllowedIPs
         'webhook:inbound',                -- Scopes
         1,                                -- Status NOT NULL: 1 = Active
         0,                                -- Environment NOT NULL: 0 = Development
         NULL,                             -- JsonConfig
         GETUTCDATE(),                     -- IssuedAt NOT NULL
         GETUTCDATE(),                     -- UpdatedAt NOT NULL
         0,                                -- IssuedBy NOT NULL (bigint user id)
         NULL,                             -- UpdatedBy
         @RawApiKey,                       -- RawApiKey — the X-Api-Key value
         NULL,                             -- AppRefId
         'Service')                        -- ApplicationType
    PRINT 'Created ApiKey:'
    PRINT '  - Id:        ' + CONVERT(NVARCHAR(36), @ApiKeyId)
    PRINT '  - CompanyId: ' + CONVERT(NVARCHAR(20), @CompanyId)
    PRINT '  - RawApiKey (X-Api-Key): ' + @RawApiKey
    PRINT '  - Salt (base64 HMAC key): ' + @Salt
END
ELSE
    PRINT 'ApiKey already exists'

GO

PRINT ''
PRINT 'Seeded test ApiKey:'
SELECT [Id], [RawApiKey], [Salt], [CompanyId], [Status], [Environment]
FROM [dbo].[ApiKeys]
WHERE [Id] = 'C0000000-0000-0000-0000-000000000001'

PRINT ''
PRINT '========================================='
PRINT 'How to send a signed test request'
PRINT '========================================='
-- Compute the signature in PowerShell, then POST to /api/inbound:
--
--   $salt = "8I5Buxsdrq5zNJOZ83mvXTrCMLdnbxHll2hHZNQPQio="
--   $id   = [guid]::NewGuid().ToString()
--   $ts   = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds().ToString()
--   $body = '{"order":1}'
--   $signed = "$id.$ts.$body"
--   $hmac = [System.Security.Cryptography.HMACSHA256]::new([Convert]::FromBase64String($salt))
--   $sig  = "v1," + [Convert]::ToBase64String($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($signed)))
--   Invoke-RestMethod -Method Post -Uri "http://localhost:5062/api/inbound" -Body $body -ContentType "application/json" -Headers @{
--     "X-Api-Key"         = "a9fe0f92103b9e2d2a6e0c0e5d5379bc7ecf1878b8321488c013bf01fe383157"
--     "webhook-id"        = $id
--     "webhook-timestamp" = $ts
--     "webhook-signature" = $sig
--     "X-Event-Type"      = "order.created"
--   }
--
-- To remove the test row:
--   DELETE FROM [PortalDB].[dbo].[ApiKeys] WHERE [Id] = 'C0000000-0000-0000-0000-000000000001'
