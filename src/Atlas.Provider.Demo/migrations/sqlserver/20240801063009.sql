-- Create "Blogs" table
CREATE TABLE [Blogs] (
  [BlogId] int IDENTITY (1, 1) NOT NULL,
  [Url] varchar(200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
  [Rating] decimal(5,2) NOT NULL,
  [Title] nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
  [Content] nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
  [Author] nvarchar(200) COLLATE SQL_Latin1_General_CP1_CI_AS CONSTRAINT [DF__Blogs__Author__21D600EE] DEFAULT N'Anonymous' NOT NULL,
  CONSTRAINT [PK_Blogs] PRIMARY KEY CLUSTERED ([BlogId] ASC)
);
-- Create index "AK_Blogs_Url" to table: "Blogs"
CREATE UNIQUE NONCLUSTERED INDEX [AK_Blogs_Url] ON [Blogs] ([Url] ASC);
-- Create "Post" table
CREATE TABLE [Post] (
  [PostId] int IDENTITY (1, 1) NOT NULL,
  [Title] nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
  [Content] nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
  [BlogUrl] varchar(200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
  CONSTRAINT [PK_Post] PRIMARY KEY CLUSTERED ([PostId] ASC),
 
  CONSTRAINT [FK_Post_Blogs_BlogUrl] FOREIGN KEY ([BlogUrl]) REFERENCES [Blogs] ([Url]) ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "IX_Post_BlogUrl" to table: "Post"
CREATE NONCLUSTERED INDEX [IX_Post_BlogUrl] ON [Post] ([BlogUrl] ASC);
