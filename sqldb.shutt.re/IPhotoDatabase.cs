using System.Collections.Generic;
using System.Threading.Tasks;
using sqldb.shutt.re.Models;
using api.shutt.re.Models.RequestBody;

namespace sqldb.shutt.re
{
    public interface IPhotoDatabase
    {
        Task<User> GetUserByOidcId(string oidcId);
        Task<User> GetUserByOidcIdCached(string oidcId);
        Task<User> GetUserByUserId(ulong userId);
        Task<User> CreateUser(User user);
        Task<bool> DeleteUserByUserId(ulong userId);
        Task<bool> MergeUserIntoUser(ulong fromUserId, ulong toUserId);
        Task<bool> DeleteOidcIdFromUser(ulong userId, string oidcId);

        Task<Album> CreateNewAlbum(
            ulong userId,
            string albumName);

        Task<Album> GetAlbumByUserIdAndAlbumId(ulong userId, ulong albumId);
        Task<bool> DeleteAlbumByUserIdAndAlbumId(ulong userId, ulong albumId);

        Task<bool> CreateNewAlbumAccess(
            ulong actingUserId,
            ulong albumId,
            ulong userId,
            bool read,
            bool share,
            bool write,
            bool admin);

        Task<bool> DeleteAlbumAccessByUserIdAndAlbumId(ulong actingUserId, ulong userId, ulong albumId);
        Task<IEnumerable<AlbumAccess>> GetAlbumAccessByUserIdAndAlbumId(ulong userId, ulong albumId);
        Task<IEnumerable<Album>> GetAlbumsAccessibleByUser(ulong userId, AlbumAccessLevel accessLevel);
        Task<IEnumerable<ImageSource>> GetImageSourcesForUser(ulong userId);
        Task<List<QueuedImage>> AddImagesToQueue(ulong userId, List<QueuedImage> queuedImages);
        Task<IEnumerable<QueuedImage>> GetImageQueueForUser(ulong userId);
        Task<IEnumerable<QueuedImage>> GetImageQueueEntries(int numberOfQueueEntries);
        Task<bool> AddImageToAlbum(Image image, AlbumImageMap albumImageMap, List<int> sizes,
            Dictionary<int, ImageFile> files, ulong queuedImageId);

        Config GetConfig();
        Task<IEnumerable<AlbumImage>> GetImagesInAlbumByUserIdAndAlbumId(ulong userId, ulong albumId);
        Task<AlbumImage> GetImageByUserIdAlbumIdAndImageId(ulong userId, ulong albumId, ulong imageId);
    }
}