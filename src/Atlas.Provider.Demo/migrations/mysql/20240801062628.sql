-- Create "Blogs" table
CREATE TABLE `Blogs` (
  `BlogId` int NOT NULL AUTO_INCREMENT,
  `Url` varchar(200) NOT NULL,
  `Rating` decimal(5,2) NOT NULL,
  `Title` longtext NOT NULL,
  `Content` longtext NOT NULL,
  `Author` varchar(200) NOT NULL DEFAULT "Anonymous",
  PRIMARY KEY (`BlogId`),
  UNIQUE INDEX `AK_Blogs_Url` (`Url`)
) CHARSET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
-- Create "Post" table
CREATE TABLE `Post` (
  `PostId` int NOT NULL AUTO_INCREMENT,
  `Title` longtext NOT NULL,
  `Content` longtext NOT NULL,
  `BlogUrl` varchar(200) NULL,
  PRIMARY KEY (`PostId`),
  INDEX `IX_Post_BlogUrl` (`BlogUrl`),
  CONSTRAINT `FK_Post_Blogs_BlogUrl` FOREIGN KEY (`BlogUrl`) REFERENCES `Blogs` (`Url`) ON UPDATE NO ACTION ON DELETE NO ACTION
) CHARSET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
