namespace Slackbot.Net.SlackClients.Http.Models.Responses.UsersList
{
    public class UsersListResponse : Response
    {
        public User[] Members;
    }
    
    public class User
    {
        public string Id;
        public string Name;
        public bool Deleted;
        public string Color;
        public UserProfile Profile;
        public bool Is_Admin;
        public bool Is_Owner;
        public bool Is_Primary_Owner;
        public bool Is_Restricted;
        public bool Is_Ultra_restricted;
        public bool Has_2fa;
        public string Two_factor_type;
        public bool Has_files;
        public string Presence;
        public bool Is_Bot;
        public string Tz;
        public string Tz_Label;
        public int Tz_Offset;
        public string Team_Id;
        public string Real_name;

    }
    
    public class UserProfile : ProfileIcons
    {
        public string First_Name;
        public string Last_Name;
        public string Real_Name;
        public string Email;
        public string Skype;
        public string Status_Emoji;
        public string Status_Text;
        public string Phone;
    }
    
    public class ProfileIcons
    {
        public string image_24;
        public string image_32;
        public string image_48;
        public string image_72;
        public string image_192;
        public string image_512;
    }
}