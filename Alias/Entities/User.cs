using System.ComponentModel.DataAnnotations;

namespace  Alias.Entities {
    public class User {
        [Key]
        public int Id { get; set;}

        public string Name { get; set; }
        public string Words { get; set; }
    }
}
