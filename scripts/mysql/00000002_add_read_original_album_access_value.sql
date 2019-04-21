ALTER TABLE `album_access` ADD COLUMN `read_original` INT(11) NULL AFTER `read`;

UPDATE `album_access` SET `read_original`=`admin`;



UPDATE `album_access` SET `read`=0 WHERE `read` IS NULL;
UPDATE `album_access` SET `read_original`=0 WHERE `read_original` IS NULL;
UPDATE `album_access` SET `write`=0 WHERE `write` IS NULL;
UPDATE `album_access` SET `share`=0 WHERE `share` IS NULL;
UPDATE `album_access` SET `admin`=0 WHERE `admin` IS NULL;

ALTER TABLE `album_access`
CHANGE COLUMN `read` `read` INT(11) NOT NULL,
CHANGE COLUMN `read_original` `read_original` INT(11) NOT NULL,
CHANGE COLUMN `write` `write` INT(11) NOT NULL,
CHANGE COLUMN `share` `share` INT(11) NOT NULL,
CHANGE COLUMN `admin` `admin` INT(11) NOT NULL;



UPDATE `config` SET `value`=2 WHERE `key`='database_version' AND `value`<2;
