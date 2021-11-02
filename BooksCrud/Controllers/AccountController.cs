using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BooksCrud.Models;
using BooksCrud.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BooksCrud.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]

    public class AccountController : Controller
    {
        readonly ApplicationContext _db;
        public AccountController(ApplicationContext db)
        {
            _db = db;
        }
      
        [HttpPost]
        public IActionResult Token(UserAuthorization info)
        {
            var identity = GetIdentity(info.Login, info.Password);
            if (identity == null)
            {
                return BadRequest(new { errorText = "Invalid username or password." });
            }
            var Id = GetId(info.Login, info.Password);

            var now = DateTime.UtcNow;
            // создаем JWT-токен
            var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    notBefore: now,
                    claims: identity.Claims,
                    expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new
            {
                access_token = encodedJwt,
                username = identity.Name,
                id = Id

            };

            return Json(response);
        }

        [HttpPost]
        public IActionResult Register(UserAuthorization info)
        {
            if(_db.Users.Where(x => x.Login == info.Login).Any())
            {
                return BadRequest(new { error = "There is a user with this nickname already, chose another name" });
            }

            User p = new User { Login = info.Login, PasswordHash = HashingPassword(info.Password), Role = info.Role};

            _db.Users.Add(p);
            _db.SaveChanges();

            var token = Token(info);
            return token;

        }

        public int? GetId(string username, string password)
        {
            User user = _db.Users.FirstOrDefault(x => x.Login == username);
            if(user != null)
            {
                return user.Id;
            }
            else
            {
                return null;
            }
        }

        public ClaimsIdentity GetIdentity(string username, string password)
        {
            try
            {
                User user = _db.Users.FirstOrDefault(x => x.Login == username);

                if (user != null)
                {
                    if (VerifyHashingPassword(user.PasswordHash, password))
                    {
                        var claims = new List<Claim>
                    {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, user.Login),
                    new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role)
                        };

                        ClaimsIdentity claimsIdentity =
                        new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType,
                        ClaimsIdentity.DefaultRoleClaimType);
                        return claimsIdentity;
                    }
                    return null;

                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string HashingPassword(string password)
        {

            byte[] salt;
            byte[] buffer2;
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, 0x10, 0x3e8))
            {
                salt = bytes.Salt;
                buffer2 = bytes.GetBytes(0x20);
            }
            byte[] dst = new byte[0x31];
            Buffer.BlockCopy(salt, 0, dst, 1, 0x10);
            Buffer.BlockCopy(buffer2, 0, dst, 0x11, 0x20);
            return Convert.ToBase64String(dst);

            /*new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);

            byte[] hashBytes = new byte[32];
            Buffer.BlockCopy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            string savedPasswordHash = Convert.ToBase64String(hashBytes);
            return savedPasswordHash;
            */
        }
            
        

        public static bool VerifyHashingPassword(string hashedPassword, string password)
        {
            byte[] buffer4;
            if (hashedPassword == null)
            {
                return false;
            }
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            byte[] src = Convert.FromBase64String(hashedPassword);
            if ((src.Length != 0x31) || (src[0] != 0))
            {
                return false;
            }
            byte[] dst = new byte[0x10];
            Buffer.BlockCopy(src, 1, dst, 0, 0x10);
            byte[] buffer3 = new byte[0x20];
            Buffer.BlockCopy(src, 0x11, buffer3, 0, 0x20);
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, dst, 0x3e8))
            {
                buffer4 = bytes.GetBytes(0x20);
            }
            return StructuralComparisons.StructuralEqualityComparer.Equals(buffer3, buffer4);

            /*
            if (hashedPassword == null)
            {
                return false;
            }
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            byte[] hashBytes = Convert.FromBase64String(hashedPassword);
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);

            for (int i = 0; i < 20; i++)
            {
                if(hashBytes[i + 16] != hash[i])
                {
                    return false;
                    throw new UnauthorizedAccessException();
                }
            }
            return true;
            */
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult GetUsers()
        {
            return Json(_db.Users.ToList());
        }

        [HttpDelete]
        [Authorize(Roles = "admin")]
        public IActionResult DeleteUser(int Id)
        {
            if (_db.Users.Where(x => x.Id == Id).Count() > 0)
            {
                _db.Users.Remove(_db.Users.Find(Id));
                _db.SaveChanges();
                return Ok();
            }
            else
            {
                return BadRequest(new { error = "There is no user with this Id" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public IActionResult CreateUser(UserAuthorization user)
        {
            if (_db.Users.Where(x => x.Login == user.Login).Count() > 0)
            {
                return BadRequest(new { error = "There is already user with this nickname" });
            }

            try
            {
                User p = new User { Login = user.Login, PasswordHash = AccountController.HashingPassword(user.Password), Role = user.Role };
                _db.Users.Add(p);
                _db.SaveChanges();

                string login = user.Login;
                string password = user.Password;

                var token = Token(user);
                return token;
            }
            catch 
            {
                return BadRequest(new { error = "something went wrong" });
            }
         }

        
    }
}
