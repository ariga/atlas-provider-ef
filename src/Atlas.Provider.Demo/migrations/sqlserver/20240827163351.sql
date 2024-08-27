-- Set comment to column: "Content" on table: "Blogs"
EXECUTE sp_addextendedproperty @name = N'MS_Description', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Blogs', @level2type = N'COLUMN', @level2name = N'Content', @value = N'Content contains new lines \n\r and 
 for example';
