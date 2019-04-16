namespace sqldb.shutt.re
{
    public static class Queries
    {
        public static string SelectLastInsertedId => "SELECT LAST_INSERT_ID()";

//        public static string GetAllAlbums =>
//            "SELECT album_id AlbumId, album_name AlbumName, cover_image_id CoverImageId " +
//            "FROM album";
//
//        public static string AlbumsVisibleToUser =>
//            "SELECT album_id AlbumId, album_name AlbumName, cover_image_id CoverImageId " +
//            "FROM album " +
//            "WHERE LOWER(album_name) like @album_name";

        public static string SelectUserByOidcId =>
            @"SELECT 
            user.user_id UserId,
            user.profile_name ProfileName,
            oidc.user_oidc_profile_id OidcProfileId,
            oidc.oidc_id OidcId
            FROM 
            (
	            user INNER JOIN 
                user_oidc_profile oidc_i
	            ON (
		            user.user_id = oidc_i.user_id AND 
                    oidc_i.oidc_id_hash = @oidc_id_hash
	            )
            )
            LEFT JOIN user_oidc_profile oidc ON user.user_id = oidc.user_id";

        public static string SelectUserByUserId =>
            @"SELECT 
            user.user_id UserId,
            user.profile_name ProfileName,
            oidc.user_oidc_profile_id OidcProfileId,
            oidc.oidc_id OidcId
            FROM user LEFT JOIN user_oidc_profile oidc ON user.user_id = oidc.user_id
            WHERE user.user_id = @user_id";

        public static string SelectUserIdByProfileName =>
            @"SELECT user_id FROM user WHERE profile_name = @profile_name";

        public static string InsertUserByProfileName =>
            @"INSERT INTO user SET profile_name = @profile_name";

        public static string InsertOidcIdToUserByUserId =>
            @"INSERT INTO user_oidc_profile 
            SET user_oidc_profile.oidc_id = @oidc_id,
            user_oidc_profile.oidc_id_hash = SHA2(@oidc_id, 256),
            user_oidc_profile.user_id = @user_id";

        public static string DeleteAllOidcIdsFromUserByUserId =>
            @"DELETE user_oidc_profile
            FROM user_oidc_profile
            WHERE user_id = @user_id";

        public static string DeleteUserByUserId =>
            @"DELETE user FROM user WHERE user_id = @user_id";

        public static string MoveAllOidcProfilesBetweenUsers =>
            @"UPDATE user_oidc_profile SET user_id = @user_id_new WHERE user_id = @user_id_old";

        public static string DeleteOidcIdFromUser =>
            @"DELETE FROM user_oidc_profile WHERE oidc_id = @oidc_id AND user_id = @user_id";

        public static string InsertNewAlbum =>
            @"INSERT INTO album SET album_name = @album_name";

        public static string InsertNewAlbumAccess =>
            @"INSERT INTO album_access
            SET album_id = @album_id, user_id = @user_id,
            `read` = @read, `write` = @write, `share` = @share, `admin` = @admin";

        public static string SelectAlbumByUserIdAndAlbumIdWalc =>
            @"SELECT a.album_id AlbumId, a.album_name AlbumName, a.cover_image_id CoverImageId,
            aa.album_access_id AlbumAccessId, aa.user_id UserId, aa.`read` `Read`,
            aa.`write` `Write`, aa.`share` `Share`, aa.`admin` `Admin`
            FROM album a LEFT JOIN album_access aa 
            ON a.album_id = aa.album_id 
            WHERE a.album_id = @album_id AND aa.user_id = @user_id AND aa.`read` != 0";

        public static string DeleteAllAlbumAccessForAlbumId =>
            /*
             * Replace
            DELETE album, album_access 
            FROM album LEFT JOIN album_access 
            ON album.album_id=album_access.album_id
            WHERE album.album_id = @album_id AND 
            album_access.user_id = @user_id AND 
            album_access.admin != 0
             
            * with (IFF first succeed):
            DELETE album_access 
            FROM album LEFT JOIN album_access 
            ON album.album_id=album_access.album_id
            WHERE album.album_id = 27 AND 
            album_access.user_id = 66 AND 
            album_access.admin != 0;

            * and
            
            DELETE album_access FROM album_access WHERE album_id = 27;
            
            * and
            
            DELETE album FROM album WHERE album_id = 27;
             */
            @"DELETE album_access FROM album_access WHERE album_id = @album_id";

        public static string DeleteAlbumByAlbumId =>
            @"DELETE album FROM album WHERE album_id = @album_id";

        public static string InsertNewAlbumAccessWalc =>
            @"INSERT INTO album_access 
            (album_id, user_id, `read`, `write`, `share`, `admin`)
            SELECT y.album_id, @user_id, @read, @write, @share, @admin
            FROM album_access y WHERE y.album_id = @album_id AND y.user_id = @acting_user_id AND y.admin != 0";

        public static string DeleteAlbumAccessByUserIdAndAlbumIdWalc =>
            @"DELETE x 
            FROM album_access x LEFT JOIN album_access y 
            ON x.album_id = y.album_id
            WHERE x.album_id = @album_id AND x.user_id = @user_id
            AND y.user_id = @acting_user_id AND y.admin != 0";

        public static string GetNumberOfAdminsForAlbum =>
            @"SELECT COUNT(*)
            FROM album a LEFT JOIN album_access aa 
            ON a.album_id = aa.album_id
            WHERE a.album_id = @album_id
            AND aa.admin = 1";

        public static string GetListOfUsersWithAlbumAccess =>
            @"SELECT x.album_id AlbumId, x.album_access_id AlbumAccessId, 
            u.user_id UserId, u.profile_name ProfileName, x.`read` `Read`, 
            x.`write` `Write`, x.`share` `Share`, x.`admin` `Admin`
            FROM user u LEFT JOIN 
            (album_access x LEFT JOIN album_access y 
            ON x.album_id = y.album_id) 
            ON u.user_id = x.user_id
            WHERE x.album_id = @album_id AND y.user_id = @acting_user_id 
            AND y.admin = 1";
        
        public static string GetListOfAlbumForUserWithAccLevelAdmin =>
            @"SELECT a.album_id AlbumId, a.album_name AlbumName, 
            a.cover_image_id CoverImageId, aa.album_access_id AlbumAccessId, 
            aa.user_id UserId, aa.`read` `Read`, aa.`write` `Write`, 
            aa.`share` `Share`, aa.`admin` `Admin`
            FROM album a, album_access aa
            WHERE a.album_id = aa.album_id
            AND aa.`read` = 1
            AND aa.`admin` = 1
            AND aa.user_id = @user_id";
        public static string GetListOfAlbumForUserWithAccLevelWrite =>
            @"SELECT a.album_id AlbumId, a.album_name AlbumName, 
            a.cover_image_id CoverImageId, aa.album_access_id AlbumAccessId, 
            aa.user_id UserId, aa.`read` `Read`, aa.`write` `Write`, 
            aa.`share` `Share`, aa.`admin` `Admin`
            FROM album a, album_access aa
            WHERE a.album_id = aa.album_id
            AND aa.`read` = 1 
            AND aa.`write` = 1
            AND aa.user_id = @user_id";
        public static string GetListOfAlbumForUserWithAccLevelShare =>
            @"SELECT a.album_id AlbumId, a.album_name AlbumName, 
            a.cover_image_id CoverImageId, aa.album_access_id AlbumAccessId, 
            aa.user_id UserId, aa.`read` `Read`, aa.`write` `Write`, 
            aa.`share` `Share`, aa.`admin` `Admin`
            FROM album a, album_access aa
            WHERE a.album_id = aa.album_id
            AND aa.`read` = 1
            AND aa.`share` = 1
            AND aa.user_id = @user_id";
        public static string GetListOfAlbumForUserWithAccLevelRead =>
            @"SELECT a.album_id AlbumId, a.album_name AlbumName, 
            a.cover_image_id CoverImageId, aa.album_access_id AlbumAccessId, 
            aa.user_id UserId, aa.`read` `Read`, aa.`write` `Write`, 
            aa.`share` `Share`, aa.`admin` `Admin`
            FROM album a, album_access aa
            WHERE a.album_id = aa.album_id
            AND aa.`read` = 1
            AND aa.user_id = @user_id";

        public static string GetImageSourcesByUserId =>
            @"SELECT image_source_id ImageSourceId, user_id UserId, 
            path Path, source_name SourceName 
            FROM image_source
            WHERE user_id = @user_id";
        
        public static string InsertQueuedImage =>
            @"INSERT INTO queued_image (image_source_id, user_id, album_id, path, path_hash, `status`)
            SELECT @image_source_id, @user_id, y.album_id, @path, SHA2(@path, 256), 1
            FROM album_access y WHERE y.album_id = @album_id AND y.user_id = @user_id AND y.admin != 0;";

        public static string GetQueuedImageByQueuedImageId =>
            @"SELECT queued_image_id QueuedImageId, image_source_id ImageSourceId, 
            user_id UserId, album_id AlbumId, path Path, `status` `Status`, status_msg StatusMsg
            FROM queued_image
            WHERE queued_image_id = @queued_image_id";
        public static string GetQueuedImageByUserId =>
            @"SELECT queued_image_id QueuedImageId, image_source_id ImageSourceId, 
            user_id UserId, album_id AlbumId, path Path, `status` `Status`, status_msg StatusMsg
            FROM queued_image
            WHERE user_id = @user_id";
        public static string GetQueuedImageEntries =>
            @"SELECT queued_image_id QueuedImageId, image_source_id ImageSourceId, 
            user_id UserId, album_id AlbumId, path Path, `status` `Status`, status_msg StatusMsg
            FROM queued_image
            LIMIT @lim";
        
        public static string InsertImageFile =>
            @"INSERT INTO image_file (path, path_hash, mime_type, width, height)
              VALUES (@path, SHA2(@path, 256), @mime_type, @width, @height)";
        public static string GetImageFileByImageFileId =>
            @"SELECT image_file_id ImageFileId, path Path, 
            mime_type MimeType, width Width, height Height
            FROM image_file
            WHERE image_file_id = @image_file_id";
        public static string GetImageFileByPath =>
            @"SELECT image_file_id ImageFileId, path Path, 
            mime_type MimeType, width Width, height Height
            FROM image_file
            WHERE path_hash = SHA2(@path, 256)";
        
        public static string InsertImage =>
            @"INSERT INTO image 
            (icon_image_file_id, small_image_file_id, medium_image_file_id, 
            large_image_file_id, fullsize_image_file_id, original_image_file_id, 
            original_hash)
            VALUES (@icon_image_file_id, @small_image_file_id, @medium_image_file_id, 
            @large_image_file_id, @fullsize_image_file_id, @original_image_file_id, 
            @original_hash)";

        public static string GetImageByImageId =>
            @"SELECT 
            image_id ImageId,
            icon_image_file_id IconImageFileId,
            small_image_file_id SmallImageFileId,
            medium_image_file_id MediumImageFileId,
            large_image_file_id LargeImageFileId,
            fullsize_image_file_id FullSizeImageFileId,
            original_image_file_id OriginalImageFileId,
            original_hash OriginalHash
            FROM image
            WHERE image_id = @image_id";
        public static string GetImageByImageFileIds =>
            @"SELECT 
            image_id ImageId,
            icon_image_file_id IconImageFileId,
            small_image_file_id SmallImageFileId,
            medium_image_file_id MediumImageFileId,
            large_image_file_id LargeImageFileId,
            fullsize_image_file_id FullSizeImageFileId,
            original_image_file_id OriginalImageFileId,
            original_hash OriginalHash
            FROM image
            WHERE icon_image_file_id = @icon_image_file_id
            AND small_image_file_id = @small_image_file_id
            AND medium_image_file_id = @medium_image_file_id
            AND large_image_file_id = @large_image_file_id
            AND fullsize_image_file_id = @fullsize_image_file_id
            AND original_image_file_id = @original_image_file_id";

        public static string InsertAlbumImageMap =>
            @"INSERT INTO album_image_map
            (album_id, image_id, original_file_name)
            VALUES (@album_id, @image_id, @original_file_name)";
        public static string GetAlbumImageMap =>
            @"SELECT 
            album_id AlbumId, image_id ImageId, original_file_name OriginalFileName, 
            image_name ImageName
            FROM album_image_map
            WHERE album_id = @album_id AND image_id = @image_id";

        public static string SetQueuedImageStatus =>
            @"UPDATE queued_image SET status = @status WHERE queued_image_id = @queued_image_id";
    }
}
