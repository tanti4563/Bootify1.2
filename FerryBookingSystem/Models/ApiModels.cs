using System;
using System.Collections.Generic;

namespace FerryBookingSystem.Models
{
    // API Response Models
    public class VoyageInfo
    {
        public int BoatTypeId { get; set; }
        public string BoatTypeNm { get; set; }
        public int VoyageId { get; set; }
        public int ScheduleId { get; set; }
        public int RouteId { get; set; }
        public int BoatId { get; set; }
        public string BoatNm { get; set; }
        public string Time { get; set; }
        public int NoOfRemain { get; set; }
        public string DepartTime { get; set; }
        public string Harbor { get; set; }
    }

    public class SeatInfo
    {
        public int SeatId { get; set; }
        public int PositionId { get; set; }
        public string SeatNm { get; set; }
        public string TicketClass { get; set; }
        public bool IsVIP { get; set; }
        public bool IsSelected { get; set; } // Added for UI
        public int DeckNumber { get; set; } // Added for better UI grouping
    }

    public class TicketPriceInfo
    {
        public int TicketPriceId { get; set; }
        public int RouteId { get; set; }
        public int TicketTypeId { get; set; }
        public string TicketClass { get; set; }
        public string TicketTypeLabel { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal PriceWithVAT { get; set; }
        public string TmpltNo { get; set; }
        public string Series { get; set; }
    }

    public class NationInfo
    {
        public int NationId { get; set; }
        public string Label { get; set; }
        public string Abbrev { get; set; }
        public string NationNm { get; set; } // Alternative property name
    }

    public class TicketTypeInfo
    {
        public int TicketTypeId { get; set; }
        public string TicketTypeName { get; set; }
        public string Description { get; set; }
        public decimal? PriceMultiplier { get; set; } // For future pricing calculations
    }

    // API Request Models
    public class CreateOrderRequest
    {
        public List<OrderItem> order { get; set; }
        public List<List<TicketItem>> lstTicketOrders { get; set; }
        public bool IsRoundTrip { get; set; }
    }

    public class OrderItem
    {
        public string Booker { get; set; }
        public string ContactNo { get; set; }
        public string Email { get; set; }
        public string Buyer { get; set; }
        public string Taxcode { get; set; }
        public string CompNm { get; set; }
        public string CompAddress { get; set; }
        public int TotalNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public int BoatId { get; set; }
        public int VoyageId { get; set; }
        public int ScheduleId { get; set; }
        public decimal PaidAmount { get; set; }
        public int RouteId { get; set; }
        public string DepartDate { get; set; }
    }

    public class TicketItem
    {
        public string IdNo { get; set; }
        public string FullNm { get; set; }
        public string POB { get; set; }
        public string YOB { get; set; }
        public int TicketTypeId { get; set; }
        public string TicketClass { get; set; }
        public int NationId { get; set; }
        public string PhoneNo { get; set; }
        public string Email { get; set; }
        public int PositionId { get; set; }
        public int TicketPriceId { get; set; }
        public decimal PriceWithVAT { get; set; }
        public int No { get; set; }
        public bool IsVIP { get; set; }
        public int Gender { get; set; }
    }

    // API Response Models
    public class ApiResponse<T>
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public string Code { get; set; }
    }

    public class CreateOrderData
    {
        public string BookingCode { get; set; }
        public decimal TotalAmt { get; set; }
    }

    public class OrderResult
    {
        public string BookingCode { get; set; }
        public string OrderNo { get; set; }
        public string RouteNm { get; set; }
        public string BoatNm { get; set; }
        public int NoOfTickets { get; set; }
        public decimal TotalAmt { get; set; }
        public decimal FeeHarborAmt { get; set; }
        public decimal Total { get; set; }
        public string DepartDate { get; set; }
        public string DepartTime { get; set; }
        public string CustomerNm { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string Customer { get; set; }
        public string Buyer { get; set; }
        public string CompanyNm { get; set; }
        public string CompanyAddr { get; set; }
        public string Taxcode { get; set; }
        public List<TicketInfoResult> TicketOrders { get; set; }
    }

    public class TicketInfoResult
    {
        public string PassengerTypeNm { get; set; }
        public string SeatNm { get; set; }
        public string IdNo { get; set; }
        public string FullNm { get; set; }
        public string YOB { get; set; }
        public string POB { get; set; }
        public string PhoneNo { get; set; }
        public string Email { get; set; }
        public string Nation { get; set; }
        public decimal UnitPrice { get; set; }
        public string QRCode { get; set; }
    }

    // View Models
    public class BookingViewModel
    {
        // Contact Information
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }

        // Invoice Information
        public string InvoiceName { get; set; }
        public string TaxCode { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }

        // Trip Information
        public int VoyageId { get; set; }
        public DateTime DepartDate { get; set; }
        public int? ReturnVoyageId { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsRoundTrip { get; set; }

        public int RouteId { get; set; }
        public int BoatId { get; set; }
        public int ScheduleId { get; set; }

        // Passenger Information
        public List<PassengerViewModel> Passengers { get; set; }
        public List<PassengerViewModel> ReturnPassengers { get; set; }
    }

    public class PassengerViewModel
    {
        public string FullName { get; set; }
        public string IdNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PlaceOfBirth { get; set; }
        public int NationalityId { get; set; }
        public int Gender { get; set; }
        public decimal Price { get; set; }
        public int SeatId { get; set; }
        public int PositionId { get; set; }
        public string SeatName { get; set; }
        public int TicketTypeId { get; set; }
        public int TicketPriceId { get; set; }
        public string TicketClass { get; set; }
    }

    public class SeatSelectionViewModel
    {
        public int VoyageId { get; set; }
        public DateTime DepartDate { get; set; }
        public List<SeatInfo> Seats { get; set; }
        public List<TicketPriceInfo> TicketPrices { get; set; }
        public int PassengerCount { get; set; }
        public bool IsRoundTrip { get; set; }
        public int? ReturnVoyageId { get; set; }
        public DateTime? ReturnDate { get; set; }
        public VoyageInfo VoyageInfo { get; set; }
    }

}