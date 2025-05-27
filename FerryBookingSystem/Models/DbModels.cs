using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace FerryBookingSystem.Models
{
    public class FerryBookingContext : DbContext
    {
        public FerryBookingContext() : base("FerryBookingConnection")
        {
        }

        public DbSet<BookingOrder> BookingOrders { get; set; }
        public DbSet<TicketOrder> TicketOrders { get; set; }
        public DbSet<RouteInfo> Routes { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure cascading delete for BookingOrder -> TicketOrder relationship
            modelBuilder.Entity<BookingOrder>()
                .HasMany(b => b.Tickets)
                .WithRequired(t => t.BookingOrder)
                .HasForeignKey(t => t.BookingOrderId)
                .WillCascadeOnDelete(true);

            base.OnModelCreating(modelBuilder);
        }
    }

    public class BookingOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string BookingCode { get; set; }

        [StringLength(100)]
        public string OrderNo { get; set; }

        [Required]
        [StringLength(100)]
        public string Booker { get; set; }

        [Required]
        [StringLength(20)]
        public string ContactNo { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(100)]
        public string Buyer { get; set; }

        [StringLength(20)]
        public string Taxcode { get; set; }

        [StringLength(200)]
        public string CompNm { get; set; }

        [StringLength(500)]
        public string CompAddress { get; set; }

        public int TotalNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        public int BoatId { get; set; }
        public int VoyageId { get; set; }
        public int ScheduleId { get; set; }
        public int RouteId { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DepartDate { get; set; }

        public bool IsRoundTrip { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed

        public virtual ICollection<TicketOrder> Tickets { get; set; }
    }

    public class TicketOrder
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("BookingOrder")]
        public int BookingOrderId { get; set; }

        public int TripType { get; set; } // 0: Outbound, 1: Return

        [Required]
        [StringLength(20)]
        public string IdNo { get; set; }

        [Required]
        [StringLength(100)]
        public string FullNm { get; set; }

        [StringLength(100)]
        public string POB { get; set; }

        [StringLength(20)]
        public string YOB { get; set; }

        public int TicketTypeId { get; set; }

        [StringLength(20)]
        public string TicketClass { get; set; }

        public int NationId { get; set; }

        [StringLength(20)]
        public string PhoneNo { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        public int PositionId { get; set; }

        [StringLength(20)]
        public string SeatNm { get; set; }

        public int TicketPriceId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceWithVAT { get; set; }

        public int No { get; set; }

        public bool IsVIP { get; set; }

        public int Gender { get; set; }

        [StringLength(500)]
        public string QRCode { get; set; }

        public virtual BookingOrder BookingOrder { get; set; }
    }

    public class RouteInfo
    {
        [Key]
        public int RouteId { get; set; }

        [Required]
        [StringLength(100)]
        public string Label { get; set; }

        [StringLength(20)]
        public string Abbrev { get; set; }

        public bool IsHaveAnotherFee { get; set; }

        [StringLength(500)]
        public string NameFees { get; set; }

        [StringLength(500)]
        public string AmountFees { get; set; }
    }
}