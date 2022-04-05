using BlueprintAPI.Middlewares;
using BlueprintAPI.Models;
using BlueprintAPI.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BlueprintAPI.Controllers {
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller {
        private readonly AUserRepository userRepository;

        private readonly SigningCredentials signingCredentials;
        private readonly string jwtIssuer;

        public AuthController(AUserRepository userRepository, IConfiguration configuration) {
            this.userRepository = userRepository;

            //Generate signing credentials
            byte[] key = Encoding.ASCII.GetBytes(configuration.GetSection("JWTSettings")["Secret"]);

            signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature);
            jwtIssuer = configuration.GetSection("JWTSettings")["ClaimsIssuer"];
        }




        public class RegisterRequestBody {
            [Required]
            [MaxLength(256, ErrorMessage = "Username is too long, maximum length of 256 characters.")]
            public string Username { get; set; }

            [Required]
            [EmailAddress]
            [MaxLength(256, ErrorMessage = "Email address is too long, maximum length of 256 characters.")]
            public string EmailAddress { get; set; }

            [Required]
            [MinLength(12, ErrorMessage = "Password is too short, minimum length of 12 characters.")]
            [MaxLength(256, ErrorMessage = "Password is too long, maximum length of 256 characters.")]
            public string Password { get; set; }
        }

        [HttpPost("register", Name = "AuthRegister")]
        public async ValueTask<IActionResult> Register([FromBody] RegisterRequestBody registerRequestBody) {
            //Make sure this won't create duplicates.
            if (await userRepository.GetAsync(x => x.Username == registerRequestBody.Username) != null) {
                return BadRequest(new GenericResponseMessage("Username must be unique!"));
            }

            if (await userRepository.GetAsync(x => x.EmailAddress == registerRequestBody.EmailAddress) != null) {
                return BadRequest(new GenericResponseMessage("Email address must be unique!"));
            }

            //Construct user
            User user = new User() {
                //Copy over user name and email address
                Username = registerRequestBody.Username,
                EmailAddress = registerRequestBody.EmailAddress,

                //Assign known parameters
                AccountType = AccountType.Unverified
            };

            //Hash password, set up email verification and register user
            user.HashPassword(registerRequestBody.Password);
            userRepository.RegisterAsync(user);

            await ResendEmail(user.Id);

            //Send response
            return Ok(new GenericResponseModel("Account created, please verify your email address!", new { createdat = user.Created }));
        }

        public sealed class LoginRequestBody {
            [Required]
            public string Credential { get; set; }

            [Required]
            public string Password { get; set; }        
        }

 


        [HttpPost("login", Name = "AuthLogin")]
        public async ValueTask<IActionResult> Login([FromBody] LoginRequestBody loginRequestBody) {
            //Check correct credentials were provided.
            LoginResponse loginResponse = await userRepository.LoginAsync(loginRequestBody.Credential, loginRequestBody.Password);
            if (!loginResponse.Successful) {
                return BadRequest(new GenericResponseMessage("Incorrect username/email and password combination!"));
            }

            User user = loginResponse.User;
            if (user.IsUnverified()) {
                return Unauthorized(new GenericResponseMessage("Account requires email verification!"));
            }

            if (user.IsBanned()) {
                return Unauthorized(new GenericResponseMessage("Account is banned!"));
            }

            //Create JWT descriptor
            SecurityTokenDescriptor securityTokenDescriptor = new SecurityTokenDescriptor() {
                //Have it expire in 4 hours
                Expires = DateTime.UtcNow.AddHours(4),
                SigningCredentials = signingCredentials,
                Issuer = jwtIssuer,

                //Carry the account's Id
                Subject = new ClaimsIdentity(new Claim[] {
                    new Claim("id", Convert.ToString(user.Id))
                })
            };

            //Generate JWT key
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            return Ok(new GenericResponseModel("Logged in!", new { token = tokenHandler.WriteToken(tokenHandler.CreateToken(securityTokenDescriptor)) }));
        }




        [HttpPatch("{id}", Name = "AuthPatch")]
        [Authorize]
        public IActionResult Patch(Guid id, [FromBody] string value) {
            return NotFound(); //NOT YET IMPLEMENTED
        }




        [HttpDelete("{id}", Name = "AuthDelete")]
        [Authorize]
        public async ValueTask<IActionResult> Delete(Guid id) {
            User user = HttpContext.GetUser();
            User userDelete = await userRepository.GetIDAsync(id);

            if (userDelete == null) {
                return NotFound(new GenericResponseMessage($"No account with id {id} exists!"));
            }

            if (user.AccountType == AccountType.Administrator || user.Id == userDelete.Id) {
                userRepository.Delete(userDelete);
                await userRepository.SaveAsync();

                return Ok(new GenericResponseModel("$Account with id {id} deleted!", new { createdat = DateTime.UtcNow }));
            }

            else {
                return Unauthorized(new GenericResponseMessage("You may only delete your own account!"));
            }
        }

        [HttpPost("verifyemail", Name = "AuthVerifyEmail")]
        public async ValueTask<IActionResult> VerifyEmail(Guid? token) {
            if (token == null) {
                return BadRequest(new GenericResponseMessage("No email verification token given!"));
            }

            User user = await userRepository.GetAsync(x => x.EmailVerificationToken == (Guid) token);
            if (user == null) {
                return BadRequest(new GenericResponseMessage("No such account exists!"));
            }

            if (user.EmailVerificationTokenExpiration != null && user.EmailVerificationTokenExpiration.Value <= DateTime.UtcNow) {
                return BadRequest(new GenericResponseMessage("Email verification token expired, resend email if account still requires verification!"));
            }

            if (!user.IsUnverified()) {
                return BadRequest(new GenericResponseMessage("Account is already verified!"));
            }

            user.AccountType = AccountType.User;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenDate = null;
            user.EmailVerificationTokenExpiration = null;

            userRepository.Update(user);
            await userRepository.SaveAsync();

            return Ok(new GenericResponseMessage("Account verified. Please login!"));
        }

        [HttpPost("resendemail", Name = "AuthResendEmail")]
        public async ValueTask<IActionResult> ResendEmail(Guid? id) {
            if (id == null) {
                return BadRequest(new GenericResponseMessage("No account Id given!"));
            }

            User user = await userRepository.GetIDAsync((Guid) id);
            if (user == null) {
                return BadRequest(new GenericResponseMessage("No such account exists!"));
            }

            if (!user.IsUnverified()) {
                return BadRequest(new GenericResponseMessage("Account is already verified!"));
            }

            if (user.EmailVerificationTokenDate != null && user.EmailVerificationTokenDate.Value.AddMinutes(1) > DateTime.UtcNow) {
                short seconds = (short) (user.EmailVerificationTokenDate.Value.AddMinutes(1) - DateTime.UtcNow).Seconds;
                return BadRequest(new GenericResponseModel("You may only resend a verification email once per minute!", new { secondsleft = seconds }));
            }

            user.EmailVerificationToken = Guid.NewGuid();
            user.EmailVerificationTokenDate = DateTime.UtcNow;
            user.EmailVerificationTokenExpiration = DateTime.UtcNow.AddDays(1);

            userRepository.Update(user);
            await userRepository.SaveAsync();

            using SmtpClient smtpClient = new SmtpClient() {
                EnableSsl = true,
                Host = "smtp.office365.com",
                Port = 587,
                UseDefaultCredentials = false,
                //Credentials = new NetworkCredential("")
            };

            MailMessage mailMessage = new MailMessage() {
                //From = new MailAddress(),
            };

            mailMessage.To.Add(user.EmailAddress);
            mailMessage.Subject = "Email Verification";
            mailMessage.Body = "http://localhost:50483/api/auth/verifyemail?token=" + user.EmailVerificationToken;

            try {
                smtpClient.Send(mailMessage);
            }

            catch (SmtpFailedRecipientException) { }

            return Ok(new GenericResponseModel("Verification email sent!", new { emailaddress = user.EmailAddress }));
        }
    }
}
