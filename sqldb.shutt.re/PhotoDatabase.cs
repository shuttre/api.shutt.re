using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using sqldb.shutt.re.Models;
using api.shutt.re.Models.RequestBody;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using MySql.Data.MySqlClient;

namespace sqldb.shutt.re
{
    public class PhotoDatabase : IPhotoDatabase
    {
        private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private readonly string _connectionString;

        public PhotoDatabase(string connectionString)
        {
            _connectionString = connectionString;
        }

        private static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        private static byte[] GetHash(Stream inputStream)
        {
            HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(inputStream);
        }

        private static string GetHashString(string inputString)
        {
            return BitConverter.ToString(GetHash(inputString)).Replace("-", "").ToLower();
        }

        private static string GetHashString(Stream inputStream)
        {
            return BitConverter.ToString(GetHash(inputStream)).Replace("-", "").ToLower();
        }

        public async Task<User> GetUserByOidcId(string oidcId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add(QueryParameters.OidcIdHash, GetHashString(oidcId));
                    var userRows = 
                        (await conn.QueryAsync<UserResultRow>(Queries.SelectUserByOidcId, parameters)).ToList();
                    return !userRows.Any() ? null : new User(userRows);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:" + e);
                    return null;
                }
            }
        }
        
        public async Task<User> GetUserByOidcIdCached(string oidcId)
        {
            if (Cache.TryGetValue(oidcId, out User cachedUser))
            {
                return cachedUser;
            }

            var userFromDb = await GetUserByOidcId(oidcId);
            if (userFromDb != null)
            {
                Cache.Set(oidcId, userFromDb);
            }

            return userFromDb;
        }

        public async Task<User> GetUserByUserId(ulong userId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add(QueryParameters.UserId, userId);
                    var userRows = await conn.QueryAsync<UserResultRow>(Queries.SelectUserByUserId, parameters);
                    return !userRows.Any() ? null : new User(userRows);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:" + e);
                    return null;
                }
            }
        }

        public async Task<User> CreateUser(User user)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // Add user
                        var paramInsertUser = new DynamicParameters();
                        paramInsertUser.Add(QueryParameters.ProfileName, user.ProfileName);
                        var numberOfInsertedUsers = await conn.ExecuteAsync(
                            Queries.InsertUserByProfileName, paramInsertUser, trans);

                        var userId = await conn.QuerySingleAsync<ulong>(Queries.SelectLastInsertedId, trans);

                        // Add OIDC ids
                        var numberOfInsertedOidcIds = 0;
                        foreach (var userOidcProfile in user.OidcProfiles)
                        {
                            var paramInsertOidcId = new DynamicParameters();
                            paramInsertOidcId.Add(QueryParameters.OidcId, userOidcProfile.OidcId);
                            paramInsertOidcId.Add(QueryParameters.UserId, userId);
                            numberOfInsertedOidcIds += await conn.ExecuteAsync(
                                Queries.InsertOidcIdToUserByUserId, paramInsertOidcId, trans);
                        }

                        Console.WriteLine(
                            $"Committing {numberOfInsertedUsers} users and {numberOfInsertedOidcIds} OIDC " +
                            $"profiles to the database");

                        var paramFetchUser = new DynamicParameters();
                        paramFetchUser.Add(QueryParameters.UserId, userId);
                        var userRows = await conn.QueryAsync<UserResultRow>(Queries.SelectUserByUserId, paramFetchUser, trans);
                        var newUser = !userRows.Any() ? null : new User(userRows);

                        trans.Commit();

                        return newUser;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error:" + e);
                        trans.Rollback();
                        return null;
                    }
                }
            }
        }

        public async Task<bool> AddOidcIdsToUserByUserId(IEnumerable<OidcProfile> oidcProfiles, ulong userId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // Add OIDC ids
                        var numberOfInsertedOidcIds = 0;
                        var numberOfOidcProfiles = 0;
                        foreach (var userOidcProfile in oidcProfiles)
                        {
                            var paramInsertOidcId = new DynamicParameters();
                            paramInsertOidcId.Add(QueryParameters.OidcId, userOidcProfile.OidcId);
                            paramInsertOidcId.Add(QueryParameters.UserId, userId);
                            numberOfInsertedOidcIds += await conn.ExecuteAsync(
                                Queries.InsertOidcIdToUserByUserId, paramInsertOidcId, trans);
                            numberOfOidcProfiles++;
                        }

                        if (numberOfInsertedOidcIds == numberOfOidcProfiles)
                        {
                            trans.Commit();                            
                            Console.WriteLine(
                                $"Committing {numberOfInsertedOidcIds} OIDC " +
                                $"profiles to the user with user id {userId}");
                            return true;
                        }
                        else
                        {
                            trans.Rollback();
                            Console.WriteLine(
                                $"Rolling back transaction because {numberOfInsertedOidcIds} oidc ids " +
                                $"were inserted while {numberOfOidcProfiles} profiles was requested to be " +
                                $"inserted.");
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error:" + e);
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }

        public async Task<bool> DeleteUserByUserId(ulong userId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add(QueryParameters.UserId, userId);
                        var numberOfDeletedOidcIds = await conn.ExecuteAsync(
                            Queries.DeleteAllOidcIdsFromUserByUserId, parameters, trans);
                        var numberOfDeletedUsers = await conn.ExecuteAsync(
                            Queries.DeleteUserByUserId, parameters, trans);

                        if (numberOfDeletedUsers == 1)
                        {
                            trans.Commit();
                            Console.WriteLine($"Deleted {numberOfDeletedOidcIds} oidcIds and {numberOfDeletedUsers} users from the database.");    
                            return numberOfDeletedUsers != 0;
                        }
                        else
                        {
                            Console.WriteLine($"Rolled back transaction after deleting {numberOfDeletedOidcIds} oidcIds and {numberOfDeletedUsers} (which is not exactly 1) users from the database.");
                            trans.Rollback();
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error:" + e);
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }

        // TODO: Should be done by referencing an entry in a "merge request table".
        public async Task<bool> MergeUserIntoUser(ulong fromUserId, ulong toUserId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var parametersMove = new DynamicParameters();
                        parametersMove.Add(QueryParameters.UserIdOld, fromUserId);
                        parametersMove.Add(QueryParameters.UserIdNew, toUserId);
                        var numerOfAffectedRows = await conn.ExecuteAsync(
                            Queries.MoveAllOidcProfilesBetweenUsers, parametersMove, trans);

                        var parametersDelete = new DynamicParameters();
                        parametersDelete.Add(QueryParameters.UserId, fromUserId);
                        var numerOfDeletedUsers = await conn.ExecuteAsync(
                            Queries.DeleteUserByUserId, parametersDelete, trans);

                        Console.WriteLine($"Updated {numerOfAffectedRows} records in the database.");
                        Console.WriteLine($"Deleted {numerOfDeletedUsers} records from the database.");

                        if (numerOfAffectedRows == 0 || numerOfDeletedUsers == 0)
                        {
                            trans.Rollback();
                            return false;
                        }
                        else
                        {
                            trans.Commit();
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error:" + e);
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }

        public async Task<bool> DeleteOidcIdFromUser(ulong userId, string oidcId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add(QueryParameters.UserId, userId);
                    parameters.Add(QueryParameters.OidcId, oidcId);
                    var numerOfDeletedOidcIds = await conn.ExecuteAsync(
                        Queries.DeleteOidcIdFromUser, parameters);

                    Console.WriteLine($"Deleted {numerOfDeletedOidcIds} records from the database.");

                    return numerOfDeletedOidcIds != 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:" + e);
                    return false;
                }
            }
        }

        public async Task<Album> CreateNewAlbum(
            ulong userId,
            string albumName)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var paramAlbum = new DynamicParameters();
                        paramAlbum.Add(QueryParameters.AlbumName, albumName);
                        var numerOfInsertedAlbums = await conn.ExecuteAsync(
                            Queries.InsertNewAlbum, paramAlbum, trans);

                        var albumId = await conn.QuerySingleAsync<ulong>(Queries.SelectLastInsertedId, trans);

                        var paramAlbumAccess = new DynamicParameters();
                        paramAlbumAccess.Add(QueryParameters.AlbumId, albumId);
                        paramAlbumAccess.Add(QueryParameters.UserId, userId);
                        paramAlbumAccess.Add(QueryParameters.Read, 1);
                        paramAlbumAccess.Add(QueryParameters.Share, 1);
                        paramAlbumAccess.Add(QueryParameters.Write, 1);
                        paramAlbumAccess.Add(QueryParameters.Admin, 1);
                        var numerOfInsertedEntries = await conn.ExecuteAsync(
                            Queries.InsertNewAlbumAccess, paramAlbumAccess, trans);

                        var paramGetAlbum = new DynamicParameters();
                        paramGetAlbum.Add(QueryParameters.UserId, userId);
                        paramGetAlbum.Add(QueryParameters.AlbumId, albumId);
                        var fetchedAlbum = await conn.QuerySingleAsync<Album>(
                            Queries.SelectAlbumByUserIdAndAlbumIdWalc, paramGetAlbum, trans);

                        trans.Commit();

                        Console.WriteLine($"Inserted {numerOfInsertedAlbums} albums into the database.");
                        Console.WriteLine(
                            $"Inserted {numerOfInsertedEntries} albums_access entries into the database.");

                        return fetchedAlbum;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error:" + e);
                        trans.Rollback();
                        return null;
                    }
                }
            }
        }

        public async Task<Album> GetAlbumByUserIdAndAlbumId(ulong userId, ulong albumId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    var paramGetAlbum = new DynamicParameters();
                    paramGetAlbum.Add(QueryParameters.UserId, userId);
                    paramGetAlbum.Add(QueryParameters.AlbumId, albumId);
                    var fetchedAlbum = await conn.QuerySingleAsync<Album>(
                        Queries.SelectAlbumByUserIdAndAlbumIdWalc, paramGetAlbum);
                    return fetchedAlbum;
                }
                catch
                {
                    Console.WriteLine($"Error: User with userId {userId} do not have access to album with albumId {albumId}");
                    return null;
                }
            }
        }

        public async Task<bool> DeleteAlbumByUserIdAndAlbumId(ulong userId, ulong albumId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var param = new DynamicParameters();
                        param.Add(QueryParameters.ActingUserId, userId);
                        param.Add(QueryParameters.UserId, userId);
                        param.Add(QueryParameters.AlbumId, albumId);
                        var numberOfDeletedAdminAlbumAccessRecords = await conn.ExecuteAsync(
                            Queries.DeleteAlbumAccessByUserIdAndAlbumIdWalc, param, trans);

                        if (numberOfDeletedAdminAlbumAccessRecords >= 1)
                        {
                            var numberOfDeletedAlbumAccessRecords = await conn.ExecuteAsync(
                                Queries.DeleteAllAlbumAccessForAlbumId, param, trans);
                            var numberOfDeletedAlbumRecords = await conn.ExecuteAsync(
                                Queries.DeleteAlbumByAlbumId, param, trans);

                            var deletedRecords = numberOfDeletedAdminAlbumAccessRecords +
                                                numberOfDeletedAlbumAccessRecords +
                                                numberOfDeletedAlbumRecords;
                            trans.Commit();
                            Console.WriteLine($"Deleted {deletedRecords} records from the database.");
                            return true;

                        }
                        else
                        {
                            trans.Rollback();
                            Console.WriteLine($"User do not have access to album");
                            Console.WriteLine($"Transaction is rolled back (Even though no changes have been made).");
                            return false;
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error:" + e);
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }

        public async Task<bool> CreateNewAlbumAccess(
            ulong actingUserId,
            ulong albumId,
            ulong userId,
            bool read,
            bool share,
            bool write,
            bool admin)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    var param = new DynamicParameters();
                    param.Add(QueryParameters.ActingUserId, actingUserId);
                    param.Add(QueryParameters.AlbumId, albumId);
                    param.Add(QueryParameters.UserId, userId);
                    param.Add(QueryParameters.Read, read ? 1 : 0);
                    param.Add(QueryParameters.Share, share ? 1 : 0);
                    param.Add(QueryParameters.Write, write ? 1 : 0);
                    param.Add(QueryParameters.Admin, admin ? 1 : 0);
                    var numerOfInsertedRecords = await conn.ExecuteAsync(
                        Queries.InsertNewAlbumAccessWalc, param);

                    Console.WriteLine($"Inserted {numerOfInsertedRecords} records into the database.");

                    return numerOfInsertedRecords != 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:" + e);
                    return false;
                }
            }
        }

        public async Task<bool> DeleteAlbumAccessByUserIdAndAlbumId(ulong actingUserId, ulong userId, ulong albumId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var param = new DynamicParameters();
                        param.Add(QueryParameters.ActingUserId, actingUserId);
                        param.Add(QueryParameters.UserId, userId);
                        param.Add(QueryParameters.AlbumId, albumId);
                        var numberOfDeletedRecords = await conn.ExecuteAsync(
                            Queries.DeleteAlbumAccessByUserIdAndAlbumIdWalc, param, trans);

                        var paramGetAlbum = new DynamicParameters();
                        paramGetAlbum.Add(QueryParameters.AlbumId, albumId);
                        var numberOfRemainingAdminsAlbumAccess = await conn.QuerySingleAsync<int>(
                            Queries.GetNumberOfAdminsForAlbum, paramGetAlbum, trans);

                        if (numberOfRemainingAdminsAlbumAccess == 0)
                        {
                            trans.Rollback();
                            Console.WriteLine($"At first, deleted {numberOfDeletedRecords} records.");
                            Console.WriteLine($"Transaction is rolled back because album would be without admins.");
                            return false;
                        }

                        trans.Commit();

                        Console.WriteLine($"Deleted {numberOfDeletedRecords} records from the database.");
                        return numberOfDeletedRecords != 0;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error:" + e);
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }

        public async Task<IEnumerable<AlbumAccess>> GetAlbumAccessByUserIdAndAlbumId(ulong userId, ulong albumId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    var paramGetAlbum = new DynamicParameters();
                    paramGetAlbum.Add(QueryParameters.AlbumId, albumId);
                    paramGetAlbum.Add(QueryParameters.ActingUserId, userId);
                    var fetchedAlbum = await conn.QueryAsync<AlbumAccess>(
                        Queries.GetListOfUsersWithAlbumAccess, paramGetAlbum);
                    return fetchedAlbum;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:" + e);
                    return null;
                }
            }
        }

        public async Task<IEnumerable<Album>> GetAlbumsAccessibleByUser(ulong userId, AlbumAccessLevel accessLevel)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    var sql = "";
                    switch (accessLevel)
                    {
                        case AlbumAccessLevel.Read:
                            sql = Queries.GetListOfAlbumForUserWithAccLevelRead;
                            break;
                        case AlbumAccessLevel.Share:
                            sql = Queries.GetListOfAlbumForUserWithAccLevelShare;
                            break;
                        case AlbumAccessLevel.Write:
                            sql = Queries.GetListOfAlbumForUserWithAccLevelWrite;
                            break;
                        case AlbumAccessLevel.Admin:
                            sql = Queries.GetListOfAlbumForUserWithAccLevelAdmin;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(accessLevel), accessLevel, null);
                    }
                    var paramGetAlbum = new DynamicParameters();
                    paramGetAlbum.Add(QueryParameters.UserId, userId);
                    var fetchedAlbum = await conn.QueryAsync<Album>(
                        sql, paramGetAlbum);
                    return fetchedAlbum;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:" + e);
                    return null;
                }
            }
        }

        public async Task<IEnumerable<ImageSource>> GetImageSourcesForUser(ulong userId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    var paramGetAlbum = new DynamicParameters();
                    paramGetAlbum.Add(QueryParameters.UserId, userId);
                    var fetchedAlbum = await conn.QueryAsync<ImageSource>(
                        Queries.GetImageSourcesByUserId, paramGetAlbum);
                    return fetchedAlbum;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:" + e);
                    return null;
                }
            }
        }
        
        public async Task<List<QueuedImage>> AddImagesToQueue(
            ulong userId, 
            List<QueuedImage> queuedImages)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {

                        foreach (var queuedImage in queuedImages.Where(queuedImage => queuedImage.Status == 0))
                        {                            
                            var paramInsert = new DynamicParameters();
                            paramInsert.Add(QueryParameters.UserId, userId);
                            paramInsert.Add(QueryParameters.AlbumId, queuedImage.AlbumId);
                            paramInsert.Add(QueryParameters.ImageSourceId, queuedImage.ImageSourceId);
                            paramInsert.Add(QueryParameters.Path, queuedImage.Path);

                            int numberOfInsertedRecords;
                            try
                            {
                                numberOfInsertedRecords = await conn.ExecuteAsync(
                                    Queries.InsertQueuedImage, paramInsert, trans);
                            }
                            catch (MySqlException e)
                            {
                                if (e.Number != 1062)
                                {
                                    throw e;
                                }
                                queuedImage.Status = 4;
                                queuedImage.StatusMsg = "Duplicate queue request";
                                continue;
                            }

                            // ReSharper disable once InvertIf
                            if (numberOfInsertedRecords > 1)
                            {
                                trans.Rollback();
                                Console.WriteLine($"Transaction is rolled back because number of records " +
                                                  $"inserted was more than 1, for one queuedImage");
                                return null;
                            }

                            if (numberOfInsertedRecords == 1)
                            {
                                var lastInsertedId = await conn.QuerySingleAsync<ulong>(Queries.SelectLastInsertedId, trans);

                                var paramSelect = new DynamicParameters();
                                paramSelect.Add(QueryParameters.QueuedImageId, lastInsertedId);
                                var insertedQueuedImage = await conn.QuerySingleAsync<QueuedImage>(
                                    Queries.GetQueuedImageByQueuedImageId, paramSelect, trans);

                                // ReSharper disable once InvertIf
                                if (insertedQueuedImage == null || insertedQueuedImage.Path != queuedImage.Path)
                                {
                                    trans.Rollback();
                                    Console.WriteLine($"Transaction is rolled back because QueuedImage " +
                                                      $"record was not inserted correctly.");
                                    return null;
                                }
                                
                                queuedImage.Status = 1;
                            }
                            else
                            {
                                queuedImage.Status = 4;
                                queuedImage.StatusMsg = "Rejected";
                            }

                        }

                        trans.Commit();
                        return queuedImages;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error:" + e);
                        trans.Rollback();
                        return null;
                    }
                }
            }
        }
        
        public async Task<IEnumerable<QueuedImage>> GetImageQueueForUser(ulong userId)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    var paramSelect = new DynamicParameters();
                    paramSelect.Add(QueryParameters.UserId, userId);
                    var queuedImages = await conn.QueryAsync<QueuedImage>(
                        Queries.GetQueuedImageByUserId, paramSelect);
                    return queuedImages;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:" + e);
                    return null;
                }
            }
        }
        
        public async Task<IEnumerable<QueuedImage>> GetImageQueueEntries(int numberOfQueueEntries)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    var paramSelect = new DynamicParameters();
                    paramSelect.Add(QueryParameters.Limit, numberOfQueueEntries);
                    var queuedImages = await conn.QueryAsync<QueuedImage>(
                        Queries.GetQueuedImageEntries, paramSelect);
                    return queuedImages;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:" + e);
                    return null;
                }
            }
        }

        public async Task<bool> AddImageToAlbum(Image image, AlbumImageMap albumImageMap, List<int> sizes,
            Dictionary<int, ImageFile> files, ulong queuedImageId)
        {
            if (sizes.Count != 6 || sizes.First() != 0)
            {
                Console.WriteLine("There should be exactly 6 values for sizes, and the first should be 0");
                return false;
            }

            if (sizes.Any(x => !files.Keys.Contains(x)) || files.Keys.Any(x => !sizes.Contains(x)))
            {
                // TODO: If condition does not capture the "EXACTLY 1 file per size" condition
                Console.WriteLine("There should be exactly 1 file per size, and at least one size per file");
                return false;
            }

            using (var conn = new MySqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {

                        var imageFileIdPerSize = await AddImageFiles(files, conn, trans);
                        if (imageFileIdPerSize == null)
                        {
                            trans.Rollback();
                            return false;
                        }

                        image.OriginalImageFileId = imageFileIdPerSize[sizes[0]];
                        image.FullSizeImageFileId = imageFileIdPerSize[sizes[1]];
                        image.LargeImageFileId = imageFileIdPerSize[sizes[2]];
                        image.MediumImageFileId = imageFileIdPerSize[sizes[3]];
                        image.SmallImageFileId = imageFileIdPerSize[sizes[4]];
                        image.IconImageFileId = imageFileIdPerSize[sizes[5]];
                    
                        var lastInsertedImageId = await AddImage(image, conn, trans);
                        if (lastInsertedImageId == null)
                        {
                            trans.Rollback();
                            return false;
                        }

                        if (!await AddAlbumImageMap(
                            albumImageMap,
                            lastInsertedImageId.GetValueOrDefault(),
                            conn,
                            trans))
                        {
                            trans.Rollback();
                            return false;
                        }

                        if (!await SetQueuedImageStatus(albumImageMap, queuedImageId, conn, trans))
                        {
                            trans.Rollback();
                            return false;
                        }

                        trans.Commit();                        
                        return true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error:" + e);
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }

        private static async Task<Dictionary<int, ulong>> AddImageFiles(
            Dictionary<int, ImageFile> files,
            IDbConnection conn,
            IDbTransaction trans)
        {
            var imageFileIdPerSize = new Dictionary<int, ulong>();

            foreach (var (size, imageFile) in files)
            {
                var paramInsertImageFile = new DynamicParameters();
                paramInsertImageFile.Add(QueryParameters.Path, imageFile.Path);
                paramInsertImageFile.Add(QueryParameters.Width, imageFile.Width);
                paramInsertImageFile.Add(QueryParameters.Height, imageFile.Height);
                paramInsertImageFile.Add(QueryParameters.MimeType, imageFile.MimeType);

                var numberOfInsertedImageFiles = 0;
                try
                {
                    numberOfInsertedImageFiles = await conn.ExecuteAsync(
                        Queries.InsertImageFile, paramInsertImageFile, trans);
                }
                catch (MySqlException e)
                {
                    if (e.Number != 1062)
                    {
                        throw e;
                    }
                }

                ImageFile insertedImageFile;
                if (numberOfInsertedImageFiles == 1)
                {
                    var lastInsertedImageFileId = await conn.QuerySingleAsync<ulong>(
                        Queries.SelectLastInsertedId,
                        trans);
                    var paramSelectImageFile = new DynamicParameters();
                    paramSelectImageFile.Add(QueryParameters.ImageFileId, lastInsertedImageFileId);
                    insertedImageFile = await conn.QuerySingleAsync<ImageFile>(
                        Queries.GetImageFileByImageFileId, paramSelectImageFile, trans);
                }
                else
                {
                    var paramSelectImageFile = new DynamicParameters();
                    paramSelectImageFile.Add(QueryParameters.Path, imageFile.Path);
                    insertedImageFile = await conn.QuerySingleAsync<ImageFile>(
                        Queries.GetImageFileByPath, paramSelectImageFile, trans);
                }

                // ReSharper disable once InvertIf
                if (insertedImageFile == null || insertedImageFile.Path != imageFile.Path)
                {
                    Console.WriteLine($"Transaction is rolled back because image_file " +
                                      $"record was not inserted correctly.");
                    return null;
                }

                imageFileIdPerSize.Add(size, insertedImageFile.ImageFileId);

                imageFile.ImageFileId = insertedImageFile.ImageFileId;
            }

            return imageFileIdPerSize;
        }

        private static async Task<ulong?> AddImage(Image image, IDbConnection conn, IDbTransaction trans)
        {
            var paramInsertImage = new DynamicParameters();
            paramInsertImage.Add(QueryParameters.OriginalHash, image.OriginalHash);
            paramInsertImage.Add(QueryParameters.OriginalImageFileId, image.OriginalImageFileId);
            paramInsertImage.Add(QueryParameters.FullSizeImageFileId, image.FullSizeImageFileId);
            paramInsertImage.Add(QueryParameters.LargeImageFileId, image.LargeImageFileId);
            paramInsertImage.Add(QueryParameters.MediumImageFileId, image.MediumImageFileId);
            paramInsertImage.Add(QueryParameters.SmallImageFileId, image.SmallImageFileId);
            paramInsertImage.Add(QueryParameters.IconImageFileId, image.IconImageFileId);

            var numberOfInsertedImageRecords = 0;
            try
            {
                numberOfInsertedImageRecords = await conn.ExecuteAsync(
                    Queries.InsertImage, paramInsertImage, trans);
            }
            catch (MySqlException e)
            {
                if (e.Number != 1062)
                {
                    throw e;
                }
            }

            Image insertedImage;
            if (numberOfInsertedImageRecords == 1)
            {
                var lastInsertedImageId = await conn.QuerySingleAsync<ulong>(Queries.SelectLastInsertedId, trans);

                var paramSelectImage = new DynamicParameters();
                paramSelectImage.Add(QueryParameters.ImageId, lastInsertedImageId);
                insertedImage = await conn.QuerySingleAsync<Image>(
                    Queries.GetImageByImageId, paramSelectImage, trans);
            }
            else
            {
                var paramSelectImage = new DynamicParameters();
                paramSelectImage.Add(QueryParameters.OriginalImageFileId, image.OriginalImageFileId);
                paramSelectImage.Add(QueryParameters.FullSizeImageFileId, image.FullSizeImageFileId);
                paramSelectImage.Add(QueryParameters.LargeImageFileId, image.LargeImageFileId);
                paramSelectImage.Add(QueryParameters.MediumImageFileId, image.MediumImageFileId);
                paramSelectImage.Add(QueryParameters.SmallImageFileId, image.SmallImageFileId);
                paramSelectImage.Add(QueryParameters.IconImageFileId, image.IconImageFileId);
                insertedImage = await conn.QuerySingleAsync<Image>(
                    Queries.GetImageByImageFileIds, paramSelectImage, trans);
            }

            // ReSharper disable once InvertIf
            if (insertedImage == null || insertedImage.OriginalHash != image.OriginalHash)
            {
                Console.WriteLine($"Transaction is rolled back because image " +
                                  $"record was not inserted correctly.");
                return null;
            }

            image.ImageId = insertedImage.ImageId;
            
            return insertedImage.ImageId;
        }

        private static async Task<bool> AddAlbumImageMap(
            AlbumImageMap albumImageMap,
            ulong lastInsertedImageId,
            IDbConnection conn,
            IDbTransaction trans)
        {
            var paramInsertAlbumImageMap = new DynamicParameters();
            paramInsertAlbumImageMap.Add(QueryParameters.AlbumId, albumImageMap.AlbumId);
            paramInsertAlbumImageMap.Add(QueryParameters.ImageId, lastInsertedImageId);
            paramInsertAlbumImageMap.Add(QueryParameters.OriginalFileName, albumImageMap.OriginalFileName);

            try
            {
                await conn.ExecuteAsync(
                    Queries.InsertAlbumImageMap, paramInsertAlbumImageMap, trans);
            }
            catch (MySqlException e)
            {
                if (e.Number != 1062)
                {
                    throw e;
                }
            }

            var paramSelectAlbumImageMap = new DynamicParameters();
            paramSelectAlbumImageMap.Add(QueryParameters.AlbumId, albumImageMap.AlbumId);
            paramSelectAlbumImageMap.Add(QueryParameters.ImageId, lastInsertedImageId);
            var insertedAlbumImageMap = await conn.QuerySingleAsync<AlbumImageMap>(
                Queries.GetAlbumImageMap, paramSelectAlbumImageMap, trans);

            // ReSharper disable once InvertIf
            if (insertedAlbumImageMap == null ||
                insertedAlbumImageMap.OriginalFileName != albumImageMap.OriginalFileName)
            {
                Console.WriteLine($"Transaction is rolled back because album_image_map " +
                                  $"record was not inserted correctly.");
                return false;
            }

            albumImageMap.ImageId = lastInsertedImageId;

            return true;
        }

        private static async Task<bool> SetQueuedImageStatus(
            AlbumImageMap albumImageMap,
            ulong queuedImageId,
            IDbConnection conn,
            IDbTransaction trans)
        {
            var paramSetStatusQueuedImage = new DynamicParameters();
            paramSetStatusQueuedImage.Add(QueryParameters.QueuedImageId, queuedImageId);
            paramSetStatusQueuedImage.Add(QueryParameters.Status, QueuedImage.StatusCompleted);

            var numberOfModifiedQueuedImageRecords = await conn.ExecuteAsync(
                Queries.SetQueuedImageStatus, paramSetStatusQueuedImage, trans);
            if (numberOfModifiedQueuedImageRecords != 1)
            {
                Console.WriteLine($"Transaction is rolled back because number of inserted " +
                                  $"album_image_map records was not 1.");
                return false;
            }

            var paramSelectQueuedImage = new DynamicParameters();
            paramSelectQueuedImage.Add(QueryParameters.QueuedImageId, queuedImageId);
            var insertedQueuedImage = await conn.QuerySingleAsync<QueuedImage>(
                Queries.GetQueuedImageByQueuedImageId, paramSelectQueuedImage, trans);

            // ReSharper disable once InvertIf
            if (insertedQueuedImage == null || insertedQueuedImage.Status != QueuedImage.StatusCompleted)
            {
                Console.WriteLine($"Transaction is rolled back because album_image_map " +
                                  $"record was not inserted correctly.");
                return false;
            }

            return numberOfModifiedQueuedImageRecords == 1;
        }
    }
}