using System;

namespace PBL3.Models
{
    public class CheckoutViewModel
    {
        // ==========================================
        // PHẦN 1: DỮ LIỆU ĐỂ HIỂN THỊ HÓA ĐƠN (GET)
        // ==========================================
        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public string ImageUrl { get; set; }
        public decimal PricePerNight { get; set; }

        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }

        public int NumberOfNights => (CheckOut - CheckIn).Days > 0 ? (CheckOut - CheckIn).Days : 1;

        public decimal RoomTotal => PricePerNight * NumberOfNights;
        public decimal ServiceFee => RoomTotal * 0.05m; // 5%
        public decimal VatFee => (RoomTotal + ServiceFee) * 0.08m; // 8%
        public decimal GrandTotal => RoomTotal + ServiceFee + VatFee;

        // ==========================================
        // PHẦN 2: DỮ LIỆU ĐỂ NHẬN FORM KHÁCH ĐIỀN (POST)
        // ==========================================
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Note { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; } // Hứng tổng tiền khi submit
    }
}