namespace Slackbot.Net.SlackClients.Http.Models.Responses.UsersList
{
    public class UsersListResponse : Response
    {
        public User[] Members { get; set; }
    }
    
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Deleted { get; set; }
        public string Color { get; set; }
        public UserProfile Profile { get; set; }
        public bool Is_Admin { get; set; }
        public bool Is_Owner { get; set; }
        public bool Is_Primary_Owner { get; set; }
        public bool Is_Restricted { get; set; }
        public bool Is_Ultra_restricted { get; set; }
        public bool Has_2fa { get; set; }
        public string Two_factor_type { get; set; }
        public bool Has_files { get; set; }
        public string Presence { get; set; }
        public bool Is_Bot{ get; set; }
        public string Tz { get; set; }
        public string Tz_Label { get; set; }
        public int Tz_Offset { get; set; }
        public string Team_Id { get; set; }
        public string Real_name { get; set; }

    }
    
    public class UserProfile : ProfileIcons
    {
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
        public string Real_Name { get; set; }
        public string Email { get; set; }
        public string Skype { get; set; }
        public string Status_Emoji { get; set; }
        public string Status_Text { get; set; }
        public string Phone { get; set; }
    }
    
    public class ProfileIcons
    {
        public string image_24 { get; set; }
        public string image_32 { get; set; }
        public string image_48 { get; set; }
        public string image_72 { get; set; }
        public string image_192 { get; set; }
        public string image_512 { get; set; }
    }
}