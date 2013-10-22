using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using JitJotData;
using WebMatrix.WebData;
using dotcode.Extensions;
using dotcode.Mailers;
using dotcode.Models;

namespace dotcode.Controllers
{
    [RequireHttps]
    public class AccountController : ApiController
    {
        [System.Web.Http.HttpGet]
        public void Logout()
        {
            if(WebSecurity.IsAuthenticated)
                WebSecurity.Logout();
        }

        [RequireHttps]
        [System.Web.Http.HttpPost]
        public AuthenticationResponse Login([FromBody] AuthModel authModel)
        {
            var clientIp = HttpContext.Current.Request.UserHostAddress;

            var host = Request.Headers.Host;
            if (!Regex.IsMatch(host, @"^(localhost(:\d+)?)|(jitjot.net)$"))
                return null;

            if (String.IsNullOrWhiteSpace(authModel.Username) || String.IsNullOrWhiteSpace(authModel.Password))
            {
                return new AuthenticationResponse
                {
                    Success =  false, 
                    Message = "Username / password cannot be blank."
                };
            }

            var loginValid = WebSecurity.Login(authModel.Username, authModel.Password, true);
            if (!loginValid)
            {

                return new AuthenticationResponse
                    {
                        Success = false, 
                        Message = "No account with that username / password combination found."
                    };
            }


            return new AuthenticationResponse{Success = true, Message = String.Empty};
        }

        public AuthenticationResponse SetNewPassword([FromBody] PasswordResetModel model)
        {
            if(!ValidationModel.IsNewPasswordValid(model.Password)) return new AuthenticationResponse(){Message = "Password must be at least 8 characters long", Success = false};

            var userid = WebSecurity.GetUserIdFromPasswordResetToken(model.ResetToken);
            if(userid == -1) return new AuthenticationResponse();

            var success = WebSecurity.ResetPassword(model.ResetToken, model.Password);

            if (success)
            {
                var username = GetUsernameById(userid);
                Login(new AuthModel(){Username = username, Password = model.Password});
            }

            return new AuthenticationResponse(){Message = "/", Success = true};
        }

        private static string GetUsernameById(int userid)
        {
            var dotcodedb = new dotcodedbEntities();
            var user = dotcodedb.Users.Single(u => u.UserId == userid);
            return user.Username;
        }

        [System.Web.Http.HttpPost]
        public AuthenticationResponse RequestPasswordReset([FromBody] AuthModel model)
        {
            var username = model.Username;
            // Send email with password recovery link
            var userExists = WebSecurity.UserExists(username) && WebSecurity.IsConfirmed(username);
            if (!userExists)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    Message = "* Count not find user with that username"
                };
            }

            var userid = WebSecurity.GetUserId(username);
            var resetToken = WebSecurity.GeneratePasswordResetToken(username);
            var userMailer = new UserMailer();

            userMailer.SendPasswordReset(GetEmailByUserId(userid), resetToken).SendAsync();

            return new AuthenticationResponse()
            {
                Message = "Instructions to reset your password have been sent to the email you provided.",
                Success = true
            };
        }

        private static string GetEmailByUserId(int userid)
        {
            var dotcodedb = new dotcodedbEntities();
            var user = dotcodedb.Users.Single(u => u.UserId == userid);
            return user.Email;
        }

        [System.Web.Http.HttpPost]
        public AuthenticationResponse Register([FromBody] AuthModel authModel)
        {
            if (String.IsNullOrWhiteSpace(authModel.Username) || String.IsNullOrWhiteSpace(authModel.Password))
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    Message = "* username / password cannot be blank."
                };
            }

            if (!Regex.IsMatch(authModel.Username, @"^[\w_]+$") || authModel.Username.Length < 3)
            {
                return new AuthenticationResponse()
                    {
                        Success = false,
                        Message = "* username must be at least 3 characters long only can only include letters, numbers, and underscores."
                    };
            }

            if (!ValidationModel.IsNewPasswordValid(authModel.Password))
            {
                return new AuthenticationResponse()
                    {
                        Message = "* password must be at least 8 characters long",
                        Success = false
                    };
            }

            if (String.IsNullOrWhiteSpace(authModel.Email) || !Regex.IsMatch(authModel.Email, @"\b[A-Z0-9._%-]+@[A-Z0-9.-]+\.[A-Z]{2,4}\b", RegexOptions.IgnoreCase))
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    Message = "* email required.  Example: someone@somedomain.com"
                };
            }

            if (WebSecurity.UserExists(authModel.Username))
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    Message = "A user with that username already exists."
                };
            }

            var token = WebSecurity.CreateUserAndAccount(authModel.Username, authModel.Password, new { authModel.Email }, true);
            var userMailer = new UserMailer();
            userMailer.Welcome(authModel.Email, authModel.Username, token).SendAsync();

            return new AuthenticationResponse()
                {
                    Message = "Instructions to activate your account have been sent to the email you provided.", 
                    Success = true
                };
        }

        [System.Web.Http.HttpGet]
        public HttpResponseMessage ActivateAccount(string username, string token)
        {
            username = username ?? String.Empty;
            var dotcodedb = new dotcodedbEntities();
            var user = dotcodedb.Users.SingleOrDefault(u => u.Username.ToLower() == username.ToLower());
            if (user != null && !WebSecurity.IsConfirmed(username) && WebSecurity.ConfirmAccount(token))
            {
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("REFRESH", "5;URL=/");
                response.Content = new StringContent(String.Format("Your account has been activated, {0}.  You will be redirected to the main page where you can log in.", username));
                return response;
            }

            return Request.CreateResponse(HttpStatusCode.NotFound);
        }
    }
}
