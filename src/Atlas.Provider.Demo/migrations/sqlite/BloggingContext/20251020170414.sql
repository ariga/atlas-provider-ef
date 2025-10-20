-- Create "AuditEntry" table
CREATE TABLE `AuditEntry` (
  `Name` text NULL
);
-- Create "Blogs" table
CREATE TABLE `Blogs` (
  `BlogId` integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  `Url` varchar NOT NULL,
  `Rating` decimal NOT NULL,
  `Title` text NOT NULL,
  `Content` text NOT NULL,
  `Author` text NOT NULL DEFAULT 'Anonymous'
);
-- Create index "Blogs_Url" to table: "Blogs"
CREATE UNIQUE INDEX `Blogs_Url` ON `Blogs` (`Url`);
-- Create "Posts" table
CREATE TABLE `Posts` (
  `PostId` integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  `Title` text NOT NULL,
  `Content` text NOT NULL,
  `BlogUrl` varchar NULL,
  CONSTRAINT `FK_Posts_Blogs_BlogUrl` FOREIGN KEY (`BlogUrl`) REFERENCES `Blogs` (`Url`) ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "IX_Posts_BlogUrl" to table: "Posts"
CREATE INDEX `IX_Posts_BlogUrl` ON `Posts` (`BlogUrl`);
