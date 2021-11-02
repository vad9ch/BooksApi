using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BooksCrud.ViewModels
{
    public class CreateBook
    {

        public int? Id { get; set; }
        [Required(ErrorMessage = "Title field shouldn't be empty ")]
        public string Title { get; set; }
        [Required(ErrorMessage = "Author field shouldn't be empty")]
        public string Author { get; set; }
        [Required(ErrorMessage = "Year is required")]
        public string Year { get; set; }
        public IFormFile BookFile { get; set; }


    }
}
