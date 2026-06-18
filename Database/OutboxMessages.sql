USE [MVPWebhookDB]
GO
/****** Table [dbo].[OutboxMessages] — transactional outbox for inbound webhook events ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OutboxMessages](
	[Id]             [uniqueidentifier] NOT NULL CONSTRAINT [DF_OutboxMessages_Id] DEFAULT (NEWSEQUENTIALID()),
	[EventType]      [nvarchar](100)    NOT NULL,
	[Payload]        [nvarchar](max)    NOT NULL,
	[Provider]       [nvarchar](100)    NULL,
	[Attempts]       [int]              NOT NULL CONSTRAINT [DF_OutboxMessages_Attempts] DEFAULT ((0)),
	[Error]          [nvarchar](max)    NULL,
	[CreatedAtUtc]   [datetime2](7)     NOT NULL CONSTRAINT [DF_OutboxMessages_CreatedAtUtc] DEFAULT (GETUTCDATE()),
	[ProcessedAtUtc] [datetime2](7)     NULL,
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Supports the outbox processor claim query                                         ******/
/****** (ProcessedAtUtc IS NULL AND Attempts < N ORDER BY CreatedAtUtc) with UPDLOCK+READPAST ******/
CREATE NONCLUSTERED INDEX [IX_OutboxMessages_Pending] ON [dbo].[OutboxMessages]
(
	[CreatedAtUtc] ASC
)
INCLUDE ([EventType], [Payload], [Provider], [Attempts], [Error])
WHERE [ProcessedAtUtc] IS NULL
GO
