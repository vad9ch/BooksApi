using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BooksCrud.ViewModels
{
    public class UserAuthorization
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}
