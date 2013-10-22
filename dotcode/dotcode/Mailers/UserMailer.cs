using Mvc.Mailer;

namespace dotcode.Mailers
{ 
    public class UserMailer : MailerBase, IUserMailer 	
	{
		public UserMailer()
		{
			MasterName="_Layout";
		}
		
        public virtual MvcMailMessage SendPasswordReset(string email, string resetToken)
        {
            ViewBag.Email = email;
            ViewBag.Token = resetToken;

            return Populate(x =>
            {
                x.Subject = "jitjot.net password reset";
                x.ViewName = "PasswordReset";
                x.To.Add(email);
            });
        }

		public virtual MvcMailMessage Welcome(string email, string username, string token)
		{
			ViewBag.Token = token;
		    ViewBag.UserName = username;

			return Populate(x =>
			{
				x.Subject = "jitjot.net account activation";
				x.ViewName = "Welcome";
				x.To.Add(email);
			});
		}

        public MvcMailMessage Welcome()
        {
            throw new System.NotImplementedException();
        }

        public virtual MvcMailMessage PasswordReset()
		{
			//ViewBag.Data = someObject;
			return Populate(x =>
			{
				x.Subject = "PasswordReset";
				x.ViewName = "PasswordReset";
				x.To.Add("some-email@example.com");
			});
		}
 	}
}