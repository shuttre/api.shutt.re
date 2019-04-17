CREATE TABLE `user` (
  `user_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `profile_name` varchar(128) NOT NULL,
  `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `profile_name_UNIQUE` (`profile_name`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;

CREATE TABLE `album` (
  `album_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `album_name` varchar(128) NOT NULL,
  `cover_image_id` bigint(20) unsigned DEFAULT NULL,
  `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`album_id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;

CREATE TABLE `image_source` (
  `image_source_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `user_id` bigint(20) unsigned NOT NULL,
  `path` text NOT NULL,
  `source_name` varchar(128) NOT NULL,
  `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`image_source_id`),
  KEY `fk_image_source_1_idx` (`user_id`),
  CONSTRAINT `fk_image_source_1` FOREIGN KEY (`user_id`) REFERENCES `user` (`user_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;

CREATE TABLE `image_file` (
  `image_file_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `path` text NOT NULL,
  `path_hash` varchar(64) NOT NULL,
  `mime_type` varchar(128) NOT NULL,
  `width` int(11) NOT NULL,
  `height` int(11) NOT NULL,
  `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`image_file_id`),
  UNIQUE KEY `path_hash_UNIQUE` (`path_hash`),
  KEY `index3` (`path_hash`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;

CREATE TABLE `image` (
  `image_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `icon_image_file_id` bigint(20) unsigned NOT NULL,
  `small_image_file_id` bigint(20) unsigned NOT NULL,
  `medium_image_file_id` bigint(20) unsigned NOT NULL,
  `large_image_file_id` bigint(20) unsigned NOT NULL,
  `fullsize_image_file_id` bigint(20) unsigned NOT NULL,
  `original_image_file_id` bigint(20) unsigned NOT NULL,
  `original_hash` varchar(64) NOT NULL,
  `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`image_id`),
  UNIQUE KEY `original_hash_UNIQUE` (`original_hash`),
  KEY `fk_image_1_idx` (`icon_image_file_id`,`small_image_file_id`,`medium_image_file_id`,`fullsize_image_file_id`,`original_image_file_id`),
  KEY `fk_image_2_idx` (`small_image_file_id`),
  KEY `fk_image_3_idx` (`medium_image_file_id`),
  KEY `fk_image_4_idx` (`fullsize_image_file_id`),
  KEY `fk_image_5_idx` (`original_image_file_id`),
  KEY `index8` (`icon_image_file_id`,`small_image_file_id`,`medium_image_file_id`,`large_image_file_id`,`fullsize_image_file_id`,`original_image_file_id`),
  CONSTRAINT `fk_image_1` FOREIGN KEY (`icon_image_file_id`) REFERENCES `image_file` (`image_file_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_image_2` FOREIGN KEY (`small_image_file_id`) REFERENCES `image_file` (`image_file_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_image_3` FOREIGN KEY (`medium_image_file_id`) REFERENCES `image_file` (`image_file_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_image_4` FOREIGN KEY (`fullsize_image_file_id`) REFERENCES `image_file` (`image_file_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_image_5` FOREIGN KEY (`original_image_file_id`) REFERENCES `image_file` (`image_file_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;

CREATE TABLE `album_image_map` (
  `album_id` bigint(20) unsigned NOT NULL,
  `image_id` bigint(20) unsigned NOT NULL,
  `original_file_name` text NOT NULL,
  `image_name` varchar(128) DEFAULT NULL,
  `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`album_id`,`image_id`),
  UNIQUE KEY `index3` (`album_id`,`image_name`),
  KEY `fk_album_image_map_2_idx` (`image_id`),
  CONSTRAINT `fk_album_image_map_1` FOREIGN KEY (`album_id`) REFERENCES `album` (`album_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_album_image_map_2` FOREIGN KEY (`image_id`) REFERENCES `image` (`image_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;


CREATE TABLE `album_access` (
  `album_access_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `album_id` bigint(20) unsigned NOT NULL,
  `user_id` bigint(20) unsigned NOT NULL,
  `read` int(11) DEFAULT NULL,
  `write` int(11) DEFAULT NULL,
  `share` int(11) DEFAULT NULL,
  `admin` int(11) DEFAULT NULL,
  `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`album_access_id`),
  UNIQUE KEY `UNIQUE_ALBUM_USER` (`user_id`,`album_id`),
  KEY `fk_album_access_1_idx` (`album_id`),
  CONSTRAINT `fk_album_access_1` FOREIGN KEY (`user_id`) REFERENCES `user` (`user_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_album_access_2` FOREIGN KEY (`album_id`) REFERENCES `album` (`album_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;

CREATE TABLE `queued_image` (
  `queued_image_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `image_source_id` bigint(20) unsigned NOT NULL,
  `user_id` bigint(20) unsigned NOT NULL,
  `album_id` bigint(20) unsigned NOT NULL,
  `path` text NOT NULL,
  `path_hash` varchar(64) NOT NULL,
  `status` smallint(5) unsigned NOT NULL,
  `status_msg` varchar(128) DEFAULT NULL,
  `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`queued_image_id`),
  UNIQUE KEY `unique_queue_req` (`image_source_id`,`user_id`,`album_id`,`path_hash`),
  KEY `fk_add_image_queue_1_idx` (`image_source_id`),
  KEY `fk_add_image_queue_2_idx` (`user_id`),
  KEY `fk_add_image_queue_3_idx` (`album_id`),
  CONSTRAINT `fk_add_image_queue_1` FOREIGN KEY (`image_source_id`) REFERENCES `image_source` (`image_source_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_add_image_queue_2` FOREIGN KEY (`user_id`) REFERENCES `user` (`user_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_add_image_queue_3` FOREIGN KEY (`album_id`) REFERENCES `album` (`album_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;

CREATE TABLE `user_oidc_profile` (
  `user_oidc_profile_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `user_id` bigint(20) unsigned NOT NULL,
  `oidc_id_hash` varchar(64) NOT NULL,
  `oidc_id` text NOT NULL,
  `created` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_oidc_profile_id`),
  UNIQUE KEY `user_oidc_profilecol_UNIQUE` (`oidc_id_hash`),
  KEY `fk_user_oidc_profile_1_idx` (`user_id`),
  CONSTRAINT `fk_user_oidc_profile_1` FOREIGN KEY (`user_id`) REFERENCES `user` (`user_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
