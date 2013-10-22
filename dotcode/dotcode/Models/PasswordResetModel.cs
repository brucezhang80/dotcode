using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace dotcode.Models
{
    public class PasswordResetModel
    {
        public string ResetToken { get; set; }
        public string Password { get; set; }
    }
}