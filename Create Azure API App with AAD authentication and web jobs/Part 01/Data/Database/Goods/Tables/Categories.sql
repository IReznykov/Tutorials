CREATE TABLE [Goods].[Categories] (
    [Id]   BIGINT IDENTITY(1, 1) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Goods.Categories] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO
