CREATE TABLE `config` (
  `key` varchar(128) NOT NULL,
  `value` text,
  `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

INSERT INTO `config` (`key`, `value`) VALUES ('database_version','1');
