using System.Collections.Generic;

namespace Shared
{
    public class Apartment
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public int Price { get; set; }
        public int SqM { get; set; }
        public float Rooms { get; set; }

        public string Street { get; set; }
        public string Region { get; set; }
        public float Distance { get; set; }

        public List<string> Images { get; set; }

        // Custom

        public bool IsNew { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsHidden { get; set; }
        public bool IsRemoved { get; set; }
        public string Comment { get; set; }

        public Apartment()
        {
            Images = new List<string>();
            Region = string.Empty;
            Street = string.Empty;
        }
    }
}
