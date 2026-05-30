namespace PBL3.Models
{
    public class CheckoutViewModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string RoomId { get; set; } = "";

        public string RoomName { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal PricePerNight { get; set; }
        public string RoomSelection { get; set; } = "";
        public List<CheckoutRoomLineViewModel> RoomLines { get; set; } = new();
        public bool CapacityWarningConfirmed { get; set; }

        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int Guests { get; set; } = 2;
        public int NumberOfRooms { get; set; } = 1;

        public int NumberOfNights => (CheckOut - CheckIn).Days > 0 ? (CheckOut - CheckIn).Days : 1;

        public decimal RoomTotal => RoomLines.Count > 0
            ? RoomLines.Sum(x => x.PricePerNight * x.Rooms * NumberOfNights)
            : PricePerNight * NumberOfNights * Math.Max(NumberOfRooms, 1);
        public decimal ServiceFee => 0;
        public decimal VatFee => 0;
        public decimal GrandTotal => RoomTotal;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string CustomerName { get; set; } = "";

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập CCCD.")]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^\d{9,20}$", ErrorMessage = "CCCD/CMND chỉ gồm 9-20 chữ số.")]
        [System.ComponentModel.DataAnnotations.StringLength(20)]
        public string Cccd { get; set; } = "";

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^\d{10,15}$", ErrorMessage = "Số điện thoại chỉ gồm 10-15 chữ số.")]
        [System.ComponentModel.DataAnnotations.StringLength(15)]
        public string PhoneNumber { get; set; } = "";

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập email.")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Email { get; set; } = "";
        [System.ComponentModel.DataAnnotations.StringLength(255)]
        public string Address { get; set; } = "";

        [System.ComponentModel.DataAnnotations.StringLength(255)]
        public string? Note { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string PaymentMethod { get; set; } = PaymentMethods.VnPay;
        public decimal TotalAmount { get; set; }
        public bool VnPayAvailable { get; set; }
        public string? PaymentUnavailableMessage { get; set; }
    }

    public static class PaymentMethods
    {
        public const string VnPay = "VNPAY";
        public const string PayAtHotel = "PAY_AT_HOTEL";
    }

    public class CheckoutRoomLineViewModel
    {
        public string RoomTypeId { get; set; } = "";
        public string RoomName { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public int Rooms { get; set; }
        public int Guests { get; set; }
        public int MaxGuestsPerRoom { get; set; }
        public decimal PricePerNight { get; set; }
        public decimal LineTotal(int nights) => PricePerNight * Rooms * Math.Max(nights, 1);
    }
}
