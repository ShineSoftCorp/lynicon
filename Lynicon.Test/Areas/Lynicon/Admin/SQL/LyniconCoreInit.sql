USE [<Database>]
GO

/****** Object:  Table [dbo].[ContentItems]    Script Date: 10/04/2013 09:30:28 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[ContentItems](
	[Id] [uniqueidentifier] NOT NULL,
	[Identity] [uniqueidentifier] NOT NULL,
	[DataType] [varchar](250) NOT NULL,
	[Path] [nvarchar](250) NULL,
	[Locale] [varchar](10) NULL,
	[Summary] [nvarchar](max) NULL,
	[Content] [nvarchar](max) NULL,
	[Title] [nvarchar](250) NULL,
	[Created] [datetime] NOT NULL,
	[UserCreated] [uniqueidentifier] NULL,
	[Updated] [datetime] NOT NULL,
	[UserUpdated] [uniqueidentifier] NULL,
 CONSTRAINT [PK_ContentItems] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE TABLE [dbo].[Users](
	[Id] [uniqueidentifier] NOT NULL,
	[UserName] [nvarchar](100) NOT NULL,
	[Email] [nvarchar](128) NULL,
	[Password] [nvarchar](128) NULL,
	[Roles] [varchar](30) NULL,
	[Created] [date] NOT NULL,
	[Modified] [date] NOT NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

INSERT INTO [dbo].[Users]
           ([Id]
           ,[UserName]
           ,[Email]
           ,[Password]
           ,[Roles]
           ,[Created]
           ,[Modified])
     VALUES
           ('9A86E183-8C19-4AE1-8A72-44E6A38192C6'
           ,'Jimmy'
           ,'admin@lynicon-cms.com'
           ,'8zz9HQqRHBLXfTqTIcIjMw=='
           ,'AEU'
           ,GETDATE()
           ,GETDATE())
GO

/****** Object:  Table [dbo].[DbChanges]    Script Date: 11/18/2013 11:50:58 ******/

CREATE TABLE [dbo].[DbChanges](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Change] [nvarchar](100) NOT NULL,
	[WhenChanged] [datetime] NOT NULL,
 CONSTRAINT [PK_DbChanges] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

INSERT INTO [dbo].[DbChanges]
           ([Change]
           ,[WhenChanged])
     VALUES
           ('LyniconInit 0.1'
           ,GETDATE())
GO

SET ANSI_PADDING OFF
GO




