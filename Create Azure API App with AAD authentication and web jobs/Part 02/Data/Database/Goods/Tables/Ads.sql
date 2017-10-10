CREATE TABLE [Goods].[Ads] (
    [Id]   BIGINT IDENTITY(1, 1) NOT NULL,
	[Version] ROWVERSION NOT NULL,
    [CategoryId] BIGINT NOT NULL, 
    [Title] NVARCHAR(100)  NOT NULL, 
    [Price] MONEY NOT NULL CONSTRAINT [DF_Goods.Ads_Price] DEFAULT (0),
    [Description] NVARCHAR(MAX) NULL, 
    [Image Url] NVARCHAR (2083) NULL,
    [Thumbnail Url] NVARCHAR(2083) NULL, 
    [Phone] NVARCHAR (20) NULL,
    [Posted] DATETIME NOT NULL CONSTRAINT [DF_Goods.Ads_Posted] DEFAULT SYSUTCDATETIME(),
    [LastModified] DATETIME2(5) NOT NULL CONSTRAINT [DF_Goods.Ads_LastModified] DEFAULT SYSUTCDATETIME(), 
    CONSTRAINT [PK_Goods.Ads] PRIMARY KEY CLUSTERED ([Id] ASC), 
    CONSTRAINT [FK_Goods.Ads_Goods.Categories] FOREIGN KEY ([CategoryId]) REFERENCES [Goods].[Categories]([Id])
);

GO

-- =============================================
-- Author:		Reznykov Illya
-- Create date: 2017-08-09
-- Description:	Update last modified time
-- =============================================
CREATE TRIGGER [Goods].[TR_Update_Goods.Ads_LastModified]
	ON [Goods].[Ads]
	AFTER UPDATE
AS 
BEGIN
	SET NOCOUNT ON;

	-- update time in updated rows
	UPDATE [Goods].[Ads]
	SET [LastModified] = SYSUTCDATETIME()
	FROM
		[Goods].[Ads] AS t1
		INNER JOIN inserted AS t2
			ON t1.[Id] = t2.[Id]
END
GO

sp_settriggerorder @triggername = '[Goods].[TR_Update_Goods.Ads_LastModified]', @order = 'first', @stmttype = 'UPDATE'
GO
