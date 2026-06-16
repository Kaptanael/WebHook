USE [MVPWebhookDB]
GO
/****** Table [dbo].[WebhookEvents] — backing table for WebhookEventRepository ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WebhookEvents](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[WebhookId] [int] NOT NULL,
	[Provider] [nvarchar](255) NULL,
	[EventType] [nvarchar](255) NOT NULL,
	[Payload] [nvarchar](max) NOT NULL,
	[Status] [tinyint] NOT NULL CONSTRAINT [DF_WebhookEvents_Status] DEFAULT ((1)),
	[Attempts] [int] NOT NULL CONSTRAINT [DF_WebhookEvents_Attempts] DEFAULT ((0)),
	[LastError] [nvarchar](max) NULL,
	[ReceivedAtUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_WebhookEvents_ReceivedAtUtc] DEFAULT (getutcdate()),
	[NextAttemptAtUtc] [datetime2](7) NULL,
	[ProcessingStartedAtUtc] [datetime2](7) NULL,
	[ProcessedAtUtc] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[WebhookEvents]  WITH CHECK ADD  CONSTRAINT [FK_WebhookEvents_WebhookEndpoints] FOREIGN KEY([WebhookId])
REFERENCES [dbo].[WebhookEndpoints] ([Id])
GO
ALTER TABLE [dbo].[WebhookEvents] CHECK CONSTRAINT [FK_WebhookEvents_WebhookEndpoints]
GO
ALTER TABLE [dbo].[WebhookEvents]  WITH CHECK ADD  CONSTRAINT [CHK_WebhookEvents_Status] CHECK  (([Status]=(5) OR [Status]=(4) OR [Status]=(3) OR [Status]=(2) OR [Status]=(1)))
GO
ALTER TABLE [dbo].[WebhookEvents] CHECK CONSTRAINT [CHK_WebhookEvents_Status]
GO
/****** Supports the dispatcher polling query (Status IN (...) AND NextAttemptAtUtc <= now ORDER BY NextAttemptAtUtc) ******/
CREATE NONCLUSTERED INDEX [IX_WebhookEvents_Status_NextAttemptAtUtc] ON [dbo].[WebhookEvents]
(
	[Status] ASC,
	[NextAttemptAtUtc] ASC
)
WHERE [NextAttemptAtUtc] IS NOT NULL
GO
