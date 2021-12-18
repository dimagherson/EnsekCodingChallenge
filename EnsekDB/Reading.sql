CREATE TABLE [dbo].[Reading]
(
	[AccountId] INT NOT NULL PRIMARY KEY, 
    [DateTime] DATETIME NOT NULL, 
    [Value] INT NOT NULL, 
    CONSTRAINT [FK_Reading_Account] FOREIGN KEY ([AccountId]) REFERENCES [Account]([AccountId])
)
