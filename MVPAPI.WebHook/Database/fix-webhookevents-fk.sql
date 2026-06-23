-- ============================================================================
-- Migration: correct the WebhookEvents.WebhookId foreign key.
--
-- WebhookEvents.WebhookId holds a WebhookEndpoints.Id (the code sets it from
-- endpoint.Id and the dispatcher resolves the endpoint via
-- endpointRepository.GetByIdAsync(WebhookId)). The original FK mistakenly
-- referenced WebHookConnection(Id), so every event insert failed with
-- "conflicted with the FOREIGN KEY constraint FK_WebhookEvents_WebHookConnection".
--
-- This drops the wrong FK and re-points it at WebhookEndpoints(Id). Safe to
-- re-run. Aborts if orphan rows exist (WebhookId not present in WebhookEndpoints).
-- ============================================================================

USE [MVPWebhookDB]
GO

-- Drop the incorrect constraint if it is still present.
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WebhookEvents_WebHookConnection')
BEGIN
    ALTER TABLE [dbo].[WebhookEvents] DROP CONSTRAINT [FK_WebhookEvents_WebHookConnection];
    PRINT 'Dropped FK_WebhookEvents_WebHookConnection.';
END

-- Guard: any event whose WebhookId is not a known endpoint would block the new FK.
IF EXISTS (
    SELECT 1 FROM [dbo].[WebhookEvents] e
    WHERE NOT EXISTS (SELECT 1 FROM [dbo].[WebhookEndpoints] ep WHERE ep.[Id] = e.[WebhookId])
)
BEGIN
    RAISERROR('Orphan WebhookEvents rows reference a missing WebhookEndpoints.Id; resolve them before adding the FK.', 16, 1);
    RETURN;
END

-- Add the correct constraint if it does not already exist.
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WebhookEvents_WebhookEndpoints')
BEGIN
    ALTER TABLE [dbo].[WebhookEvents] WITH CHECK
        ADD CONSTRAINT [FK_WebhookEvents_WebhookEndpoints] FOREIGN KEY ([WebhookId])
        REFERENCES [dbo].[WebhookEndpoints] ([Id]);
    PRINT 'Added FK_WebhookEvents_WebhookEndpoints -> WebhookEndpoints(Id).';
END
ELSE
    PRINT 'FK_WebhookEvents_WebhookEndpoints already exists.';
GO
