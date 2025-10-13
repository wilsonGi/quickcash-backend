using QuickCashJobAPI.Enums;
using System;

namespace QuickCashJobAPI.Models.DTO
{
    public class JobCreateDTO
    {
        public int CategoryId { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime DatePosted { get; set; }
        public byte[]? AudioDescription { get; set; }
        public string Payout { get; set; }
        public bool Negotiable { get; set; }
    }
}
