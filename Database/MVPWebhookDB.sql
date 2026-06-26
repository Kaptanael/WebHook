USE [MVPWebhookDB]
GO
/****** Object:  Table [dbo].[WebHookConnection]    Script Date: 25/06/2026 10:03:49 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WebHookConnection](
	[Id] [uniqueidentifier] NOT NULL,
	[CompanyId] [int] NOT NULL,
	[ApplicationName] [nvarchar](255) NOT NULL,
	[ClientToken] [nvarchar](512) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[MVPApiToken] [nvarchar](max) NOT NULL,
	[MVPApiRefreshToken] [nvarchar](max) NOT NULL,
	[MVPAuthKeyJson] [nvarchar](max) NOT NULL,
	[MVPApiExpiresIn] [datetime2](7) NOT NULL,
	[SigningSecret] [nvarchar](256) NULL,
	[CreatedAtUtc] [datetime] NOT NULL,
 CONSTRAINT [PK_WebHookConnection] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_WebHookConnection_ClientToken] UNIQUE NONCLUSTERED 
(
	[ClientToken] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WebhookInbound]    Script Date: 25/06/2026 10:03:49 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WebhookInbound](
	[Id] [uniqueidentifier] NOT NULL,
	[WebhookId] [uniqueidentifier] NOT NULL,
	[EventType] [nvarchar](255) NOT NULL,
	[Payload] [nvarchar](max) NOT NULL,
	[Status] [tinyint] NOT NULL,
	[Attempts] [int] NOT NULL,
	[LastError] [nvarchar](max) NULL,
	[ReceivedAtUtc] [datetime2](7) NOT NULL,
	[NextAttemptAtUtc] [datetime2](7) NULL,
	[ProcessingStartedAtUtc] [datetime2](7) NULL,
	[ProcessedAtUtc] [datetime2](7) NULL,
	[Provider] [nvarchar](255) NULL,
	[IdempotencyKey] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_WebhookEvents] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WebhookOutbound]    Script Date: 25/06/2026 10:03:49 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WebhookOutbound](
	[Id] [uniqueidentifier] NOT NULL,
	[EndPointToken] [nvarchar](512) NOT NULL,
	[Endpoint] [nvarchar](2048) NOT NULL,
	[CompanyId] [int] NOT NULL,
	[TriggerConfigJson] [nvarchar](max) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAtUtc] [datetime] NOT NULL,
	[ActionDataSchema] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_WebhookEndpoints] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_WebhookEndpoints_Endpoint] UNIQUE NONCLUSTERED 
(
	[Endpoint] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_WebhookEndpoints_EndPointToken] UNIQUE NONCLUSTERED 
(
	[EndPointToken] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[WebHookConnection] ADD  CONSTRAINT [DF__WebHookConne__Id__5165187F]  DEFAULT (newsequentialid()) FOR [Id]
GO
ALTER TABLE [dbo].[WebHookConnection] ADD  CONSTRAINT [DF__WebHookCo__IsAct__52593CB8]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[WebHookConnection] ADD  CONSTRAINT [DF__WebHookCo__Creat__534D60F1]  DEFAULT (getutcdate()) FOR [CreatedAtUtc]
GO
ALTER TABLE [dbo].[WebhookInbound] ADD  CONSTRAINT [DF__WebhookEvent__Id__72910220]  DEFAULT (newsequentialid()) FOR [Id]
GO
ALTER TABLE [dbo].[WebhookInbound] ADD  CONSTRAINT [DF__WebhookEv__Statu__73852659]  DEFAULT ((1)) FOR [Status]
GO
ALTER TABLE [dbo].[WebhookInbound] ADD  CONSTRAINT [DF__WebhookEv__Attem__74794A92]  DEFAULT ((0)) FOR [Attempts]
GO
ALTER TABLE [dbo].[WebhookInbound] ADD  CONSTRAINT [DF__WebhookEv__Recei__756D6ECB]  DEFAULT (getutcdate()) FOR [ReceivedAtUtc]
GO
ALTER TABLE [dbo].[WebhookOutbound] ADD  CONSTRAINT [DF__WebhookEndpo__Id__395884C4]  DEFAULT (newsequentialid()) FOR [Id]
GO
ALTER TABLE [dbo].[WebhookOutbound] ADD  CONSTRAINT [DF__WebhookEn__IsAct__3A4CA8FD]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[WebhookOutbound] ADD  CONSTRAINT [DF__WebhookEn__Creat__3B40CD36]  DEFAULT (getutcdate()) FOR [CreatedAtUtc]
GO
