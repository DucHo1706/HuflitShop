namespace HuflitShopCore.DTOs
{
    public class EmailConfirmDTO
    {
        public string Email { get; set; }
        public bool EmailVerified { get; set; }
        public bool EmailSent { get; set; }
    }
}