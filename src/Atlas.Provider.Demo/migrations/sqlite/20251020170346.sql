-- Create "Categories" table
CREATE TABLE `Categories` (
  `CategoryId` integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  `Name` text NOT NULL
);
-- Create "Products" table
CREATE TABLE `Products` (
  `ProductId` integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  `Name` text NOT NULL,
  `Price` text NOT NULL,
  `CategoryId` integer NOT NULL,
  CONSTRAINT `FK_Products_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`CategoryId`) ON UPDATE NO ACTION ON DELETE CASCADE
);
-- Create index "IX_Products_CategoryId" to table: "Products"
CREATE INDEX `IX_Products_CategoryId` ON `Products` (`CategoryId`);
