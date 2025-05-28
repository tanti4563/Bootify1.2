using System;
using System.Collections.Generic;

namespace FerryBookingSystem.Models
{
    public class BookingTicketInfo
    {
        public string BookingCode { get; set; }
        public string OrderNo { get; set; }
        public decimal Total { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string CompanyName { get; set; }
        public string Notes { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime BookingDate { get; set; }
        public string Route { get; set; }
        public DateTime DepartureDate { get; set; }
        public List<TicketInfo> Tickets { get; set; } = new List<TicketInfo>();

        // Add any other properties that might be needed for display
    }

    public class TicketInfo
    {
        public string PassengerName { get; set; }
        public string IdNumber { get; set; }
        public string SeatName { get; set; }
        public string TicketClass { get; set; }
        public decimal Price { get; set; }
        public string QRCode { get; set; }
    }
}