-- Modify "Blogs" table
ALTER TABLE `Blogs` MODIFY COLUMN `Content` longtext NOT NULL COMMENT "Content contains new lines \\n\\r and \n for example";
