using System.Text;

namespace sqldb.shutt.re.Models
{
    public class AlbumAccess
    {
        public ulong AlbumId { get; set; }
        public ulong AlbumAccessId { get; set; }
        public ulong UserId { get; set; }
        public string ProfileName { get; set; }
        public int Read { get; set; }
        public int Write { get; set; }
        public int Share { get; set; }
        public int Admin { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"AlbumId: {AlbumId}");
            sb.AppendLine($"AlbumAccessId: {AlbumAccessId}");
            sb.AppendLine($"UserId: {UserId}");
            sb.AppendLine($"ProfileName: {ProfileName}");
            sb.AppendLine($"Read: {(Read == 0 ? "No" : "Yes")}");
            sb.AppendLine($"Write: {(Write == 0 ? "No" : "Yes")}");
            sb.AppendLine($"Share: {(Share == 0 ? "No" : "Yes")}");
            sb.AppendLine($"Admin: {(Admin == 0 ? "No" : "Yes")}");

            return sb.ToString();
        }
    }

}
