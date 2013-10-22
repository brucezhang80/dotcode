using Mvc.Mailer;

namespace dotcode.Mailers
{ 
    public interface IUserMailer
    {
			MvcMailMessage Welcome();
			MvcMailMessage PasswordReset();
	}
}