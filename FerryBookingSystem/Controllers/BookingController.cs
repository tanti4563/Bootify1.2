using FerryBookingSystem.Models;
using FerryBookingSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace FerryBookingSystem.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApiService _apiService = new ApiService();
        private readonly FerryBookingContext _dbContext = new FerryBookingContext();

        // Step 1: Select Route and Travel Details
        [AllowAnonymous]
        public async Task<ActionResult> SelectRoute()
        {
            try
            {
                var routes = await _apiService.GetRoutesAsync();
                ViewBag.Nations = await _apiService.GetNationsAsync();
                return View(routes);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading routes: " + ex.Message;
                return View(new List<RouteInfo>());
            }
        }

        // Step 2: Search Voyages
        [HttpPost]
        public async Task<ActionResult> SearchVoyages(int routeId, DateTime departDate, bool isRoundTrip = false, DateTime? returnDate = null, int passengerCount = 1)
        {
            try
            {
                var voyages = await _apiService.SearchVoyagesAsync(routeId, departDate, passengerCount);

                ViewBag.RouteId = routeId;
                ViewBag.DepartDate = departDate;
                ViewBag.IsRoundTrip = isRoundTrip;
                ViewBag.PassengerCount = passengerCount;

                if (isRoundTrip && returnDate.HasValue)
                {
                    ViewBag.ReturnDate = returnDate.Value;
                    var returnVoyages = await _apiService.SearchReturnVoyagesAsync(routeId, returnDate.Value, passengerCount);
                    ViewBag.ReturnVoyages = returnVoyages;
                }

                return View(voyages);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error searching for voyages: " + ex.Message;
                return View(new List<VoyageInfo>());
            }
        }

        // Step 3: Select Seats
        public async Task<ActionResult> SelectSeats(int voyageId, DateTime departDate, int routeId, int passengerCount = 1,
            int returnVoyageId = 0, DateTime? returnDate = null, bool isRoundTrip = false)
        {
            try
            {
                // Get voyage information
                var voyageInfo = await _apiService.GetVoyageInfoAsync(voyageId, routeId, departDate, passengerCount);
                if (voyageInfo == null)
                {
                    throw new Exception("Voyage information not found");
                }

                var seats = await _apiService.GetSeatsEmptyAsync(voyageId, departDate);
                var prices = await _apiService.GetTicketPricesAsync(routeId, voyageInfo.BoatTypeId, departDate);

                var viewModel = new SeatSelectionViewModel
                {
                    VoyageId = voyageId,
                    DepartDate = departDate,
                    Seats = seats,
                    TicketPrices = prices,
                    PassengerCount = passengerCount,
                    IsRoundTrip = isRoundTrip,
                    VoyageInfo = voyageInfo
                };

                if (isRoundTrip && returnVoyageId > 0 && returnDate.HasValue)
                {
                    viewModel.ReturnVoyageId = returnVoyageId;
                    viewModel.ReturnDate = returnDate.Value;

                    var returnVoyageInfo = await _apiService.GetVoyageInfoAsync(returnVoyageId, routeId, returnDate.Value, passengerCount);
                    ViewBag.ReturnVoyageInfo = returnVoyageInfo;

                    var returnSeats = await _apiService.GetSeatsEmptyAsync(returnVoyageId, returnDate.Value);
                    var returnPrices = await _apiService.GetTicketPricesAsync(routeId, returnVoyageInfo.BoatTypeId, returnDate.Value);
                    ViewBag.ReturnSeats = returnSeats;
                    ViewBag.ReturnTicketPrices = returnPrices;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading seats: " + ex.Message;
                return View(new SeatSelectionViewModel());
            }
        }

        // AJAX call to get seat details
        [HttpGet]
        public async Task<ActionResult> GetSeatDetails(int seatId, int voyageId, DateTime departDate, int routeId)
        {
            try
            {
                var seats = await _apiService.GetSeatsEmptyAsync(voyageId, departDate);
                var seat = seats.FirstOrDefault(s => s.SeatId == seatId);

                if (seat == null)
                {
                    return Json(new { success = false, message = "Seat not found" }, JsonRequestBehavior.AllowGet);
                }

                var voyageInfo = await _apiService.GetVoyageInfoAsync(voyageId, routeId, departDate, 1);
                var prices = await _apiService.GetTicketPricesAsync(routeId, voyageInfo.BoatTypeId, departDate);
                var price = prices.FirstOrDefault(p => p.TicketClass == seat.TicketClass);

                return Json(new
                {
                    success = true,
                    seat = seat,
                    price = price
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Step 4: Enter Passenger Details
        [HttpPost]
        public async Task<ActionResult> PassengerDetails(int voyageId, DateTime departDate, string selectedSeats,
            int routeId, int boatId, int scheduleId,
            int returnVoyageId = 0, DateTime? returnDate = null, string returnSelectedSeats = "", bool isRoundTrip = false)
        {
            try
            {
                var nations = await _apiService.GetNationsAsync();
                ViewBag.Nations = nations;
                ViewBag.VoyageId = voyageId;
                ViewBag.DepartDate = departDate;
                ViewBag.ReturnVoyageId = returnVoyageId;
                ViewBag.ReturnDate = returnDate;
                ViewBag.IsRoundTrip = isRoundTrip;
                ViewBag.RouteId = routeId;
                ViewBag.BoatId = boatId;
                ViewBag.ScheduleId = scheduleId;

                // Parse selected seats for outbound journey
                var seatDetails = new List<PassengerViewModel>();
                if (!string.IsNullOrEmpty(selectedSeats))
                {
                    var seatIds = selectedSeats.Split(',').Select(int.Parse).ToList();
                    ViewBag.SelectedSeats = selectedSeats;
                    ViewBag.SeatCount = seatIds.Count;

                    // Get seat information
                    var seats = await _apiService.GetSeatsEmptyAsync(voyageId, departDate);
                    var voyageInfo = await _apiService.GetVoyageInfoAsync(voyageId, routeId, departDate, seatIds.Count);
                    var prices = await _apiService.GetTicketPricesAsync(routeId, voyageInfo.BoatTypeId, departDate);

                    foreach (var seatId in seatIds)
                    {
                        var seat = seats.FirstOrDefault(s => s.SeatId == seatId);
                        if (seat != null)
                        {
                            var price = prices.FirstOrDefault(p => p.TicketClass == seat.TicketClass);

                            seatDetails.Add(new PassengerViewModel
                            {
                                SeatId = seat.SeatId,
                                PositionId = seat.PositionId,
                                SeatName = seat.SeatNm,
                                TicketClass = seat.TicketClass,
                                Price = price?.PriceWithVAT ?? 0,
                                TicketTypeId = price?.TicketTypeId ?? 1,
                                TicketPriceId = price?.TicketPriceId ?? 1,
                                DateOfBirth = DateTime.Now.AddYears(-30) // Default value
                            });
                        }
                    }
                }

                // Parse selected seats for return journey
                var returnSeatDetails = new List<PassengerViewModel>();
                if (isRoundTrip && !string.IsNullOrEmpty(returnSelectedSeats) && returnVoyageId > 0 && returnDate.HasValue)
                {
                    var returnSeatIds = returnSelectedSeats.Split(',').Select(int.Parse).ToList();
                    ViewBag.ReturnSelectedSeats = returnSelectedSeats;
                    ViewBag.ReturnSeatCount = returnSeatIds.Count;

                    // Get seat information for return journey
                    var returnSeats = await _apiService.GetSeatsEmptyAsync(returnVoyageId, returnDate.Value);
                    var returnVoyageInfo = await _apiService.GetVoyageInfoAsync(returnVoyageId, routeId, returnDate.Value, returnSeatIds.Count);
                    var returnPrices = await _apiService.GetTicketPricesAsync(routeId, returnVoyageInfo.BoatTypeId, returnDate.Value);

                    foreach (var seatId in returnSeatIds)
                    {
                        var seat = returnSeats.FirstOrDefault(s => s.SeatId == seatId);
                        if (seat != null)
                        {
                            var price = returnPrices.FirstOrDefault(p => p.TicketClass == seat.TicketClass);

                            returnSeatDetails.Add(new PassengerViewModel
                            {
                                SeatId = seat.SeatId,
                                PositionId = seat.PositionId,
                                SeatName = seat.SeatNm,
                                TicketClass = seat.TicketClass,
                                Price = price?.PriceWithVAT ?? 0,
                                TicketTypeId = price?.TicketTypeId ?? 1,
                                TicketPriceId = price?.TicketPriceId ?? 1,
                                DateOfBirth = DateTime.Now.AddYears(-30) // Default value
                            });
                        }
                    }
                }

                var bookingViewModel = new BookingViewModel
                {
                    VoyageId = voyageId,
                    DepartDate = departDate,
                    ReturnVoyageId = returnVoyageId,
                    ReturnDate = returnDate,
                    IsRoundTrip = isRoundTrip,
                    RouteId = routeId,
                    BoatId = boatId,
                    ScheduleId = scheduleId,
                    Passengers = seatDetails,
                    ReturnPassengers = returnSeatDetails
                };

                // Pre-fill contact information if user is logged in
                if (User.Identity.IsAuthenticated)
                {
                    var userContext = new ApplicationDbContext();
                    var user = userContext.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
                    if (user != null)
                    {
                        bookingViewModel.ContactName = user.FullName;
                        bookingViewModel.ContactEmail = user.Email;
                    }
                }

                return View(bookingViewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error preparing passenger details: " + ex.Message;
                return View(new BookingViewModel());
            }
        }

        // Step 4: Create Booking
        [HttpPost]
        public async Task<ActionResult> CreateBooking(BookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Please fill all required fields";
                var nations = await _apiService.GetNationsAsync();
                ViewBag.Nations = nations;
                return View("PassengerDetails", model);
            }

            try
            {
                // Create the API request structure
                var orderRequest = new CreateOrderRequest
                {
                    order = new List<OrderItem>(),
                    lstTicketOrders = new List<List<TicketItem>>(),
                    IsRoundTrip = model.IsRoundTrip
                };

                // Outbound order
                var outboundOrder = new OrderItem
                {
                    Booker = model.ContactName,
                    ContactNo = model.ContactPhone,
                    Email = model.ContactEmail,
                    Buyer = model.InvoiceName,
                    Taxcode = model.TaxCode,
                    CompNm = model.CompanyName,
                    CompAddress = model.CompanyAddress,
                    TotalNumber = model.Passengers.Count,
                    TotalAmount = model.Passengers.Sum(p => p.Price),
                    PaidAmount = model.Passengers.Sum(p => p.Price),
                    BoatId = model.BoatId,
                    VoyageId = model.VoyageId,
                    ScheduleId = model.ScheduleId,
                    RouteId = model.RouteId,
                    DepartDate = model.DepartDate.ToString("yyyy-MM-dd")
                };
                orderRequest.order.Add(outboundOrder);

                // Outbound tickets
                var outboundTickets = new List<TicketItem>();
                for (int i = 0; i < model.Passengers.Count; i++)
                {
                    var passenger = model.Passengers[i];
                    var ticket = new TicketItem
                    {
                        IdNo = passenger.IdNumber,
                        FullNm = passenger.FullName,
                        POB = passenger.PlaceOfBirth,
                        YOB = passenger.DateOfBirth.ToString("dd/MM/yyyy"),
                        TicketTypeId = passenger.TicketTypeId,
                        TicketClass = passenger.TicketClass,
                        NationId = passenger.NationalityId,
                        PhoneNo = model.ContactPhone,
                        Email = model.ContactEmail,
                        PositionId = passenger.PositionId,
                        TicketPriceId = passenger.TicketPriceId,
                        PriceWithVAT = passenger.Price,
                        No = i + 1,
                        IsVIP = false,
                        Gender = passenger.Gender
                    };
                    outboundTickets.Add(ticket);
                }
                orderRequest.lstTicketOrders.Add(outboundTickets);

                // Add return trip if round trip
                if (model.IsRoundTrip && model.ReturnVoyageId.HasValue && model.ReturnDate.HasValue && model.ReturnPassengers != null && model.ReturnPassengers.Any())
                {
                    var returnOrder = new OrderItem
                    {
                        Booker = model.ContactName,
                        ContactNo = model.ContactPhone,
                        Email = model.ContactEmail,
                        Buyer = model.InvoiceName,
                        Taxcode = model.TaxCode,
                        CompNm = model.CompanyName,
                        CompAddress = model.CompanyAddress,
                        TotalNumber = model.ReturnPassengers.Count,
                        TotalAmount = model.ReturnPassengers.Sum(p => p.Price),
                        PaidAmount = model.ReturnPassengers.Sum(p => p.Price),
                        BoatId = model.BoatId,
                        VoyageId = model.ReturnVoyageId.Value,
                        ScheduleId = model.ScheduleId,
                        RouteId = model.RouteId,
                        DepartDate = model.ReturnDate.Value.ToString("yyyy-MM-dd")
                    };
                    orderRequest.order.Add(returnOrder);

                    var returnTickets = new List<TicketItem>();
                    for (int i = 0; i < model.ReturnPassengers.Count; i++)
                    {
                        var passenger = model.ReturnPassengers[i];
                        var ticket = new TicketItem
                        {
                            IdNo = passenger.IdNumber,
                            FullNm = passenger.FullName,
                            POB = passenger.PlaceOfBirth,
                            YOB = passenger.DateOfBirth.ToString("dd/MM/yyyy"),
                            TicketTypeId = passenger.TicketTypeId,
                            TicketClass = passenger.TicketClass,
                            NationId = passenger.NationalityId,
                            PhoneNo = model.ContactPhone,
                            Email = model.ContactEmail,
                            PositionId = passenger.PositionId,
                            TicketPriceId = passenger.TicketPriceId,
                            PriceWithVAT = passenger.Price,
                            No = i + 1,
                            IsVIP = false,
                            Gender = passenger.Gender
                        };
                        returnTickets.Add(ticket);
                    }
                    orderRequest.lstTicketOrders.Add(returnTickets);
                }

                // Call API to create order
                var result = await _apiService.CreateOrderAsync(orderRequest);

                if (result.Status && result.Code == "00")
                {
                    // Store in database
                    var bookingOrder = new BookingOrder
                    {
                        BookingCode = result.Data.BookingCode,
                        OrderNo = DateTime.Now.ToString("yyyyMMddHHmmss"),
                        Booker = model.ContactName,
                        ContactNo = model.ContactPhone,
                        Email = model.ContactEmail,
                        Buyer = model.InvoiceName,
                        Taxcode = model.TaxCode,
                        CompNm = model.CompanyName,
                        CompAddress = model.CompanyAddress,
                        TotalNumber = model.Passengers.Count + (model.ReturnPassengers?.Count ?? 0),
                        TotalAmount = result.Data.TotalAmt,
                        PaidAmount = 0, // Will be updated after payment
                        BoatId = model.BoatId,
                        VoyageId = model.VoyageId,
                        ScheduleId = model.ScheduleId,
                        RouteId = model.RouteId,
                        DepartDate = model.DepartDate,
                        IsRoundTrip = model.IsRoundTrip,
                        UserId = User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null,
                        CreatedDate = DateTime.UtcNow, // Use UTC time
                        Tickets = new List<TicketOrder>()
                    };

                    // Add tickets to database
                    for (int i = 0; i < model.Passengers.Count; i++)
                    {
                        var passenger = model.Passengers[i];
                        bookingOrder.Tickets.Add(new TicketOrder
                        {
                            BookingOrderId = bookingOrder.Id,
                            TripType = 0, // Outbound
                            IdNo = passenger.IdNumber,
                            FullNm = passenger.FullName,
                            POB = passenger.PlaceOfBirth,
                            YOB = passenger.DateOfBirth.ToString("dd/MM/yyyy"),
                            TicketTypeId = passenger.TicketTypeId,
                            TicketClass = passenger.TicketClass,
                            NationId = passenger.NationalityId,
                            PhoneNo = model.ContactPhone,
                            Email = model.ContactEmail,
                            PositionId = passenger.PositionId,
                            SeatNm = passenger.SeatName,
                            TicketPriceId = passenger.TicketPriceId,
                            PriceWithVAT = passenger.Price,
                            No = i + 1,
                            IsVIP = false,
                            Gender = passenger.Gender
                        });
                    }

                    if (model.IsRoundTrip && model.ReturnPassengers != null)
                    {
                        for (int i = 0; i < model.ReturnPassengers.Count; i++)
                        {
                            var passenger = model.ReturnPassengers[i];
                            bookingOrder.Tickets.Add(new TicketOrder
                            {
                                BookingOrderId = bookingOrder.Id,
                                TripType = 1, // Return
                                IdNo = passenger.IdNumber,
                                FullNm = passenger.FullName,
                                POB = passenger.PlaceOfBirth,
                                YOB = passenger.DateOfBirth.ToString("dd/MM/yyyy"),
                                TicketTypeId = passenger.TicketTypeId,
                                TicketClass = passenger.TicketClass,
                                NationId = passenger.NationalityId,
                                PhoneNo = model.ContactPhone,
                                Email = model.ContactEmail,
                                PositionId = passenger.PositionId,
                                SeatNm = passenger.SeatName,
                                TicketPriceId = passenger.TicketPriceId,
                                PriceWithVAT = passenger.Price,
                                No = i + 1,
                                IsVIP = false,
                                Gender = passenger.Gender
                            });
                        }
                    }

                    _dbContext.BookingOrders.Add(bookingOrder);
                    _dbContext.SaveChanges();

                    // Redirect to payment page
                    return RedirectToAction("Payment", new { bookingCode = result.Data.BookingCode, amount = result.Data.TotalAmt });
                }
                else
                {
                    ViewBag.ErrorMessage = $"Error creating booking: {result.Message}";
                    var nations = await _apiService.GetNationsAsync();
                    ViewBag.Nations = nations;
                    return View("PassengerDetails", model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error creating booking: " + ex.Message;
                var nations = await _apiService.GetNationsAsync();
                ViewBag.Nations = nations;
                return View("PassengerDetails", model);
            }
        }

        // Step 5: Payment
        public ActionResult Payment(string bookingCode, decimal amount)
        {
            ViewBag.BookingCode = bookingCode;
            ViewBag.Amount = amount;
            // Display current system time
            ViewBag.CurrentTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            return View();
        }

        // Step 5: Payment Confirmation (called by payment gateway)
        [HttpGet]
        public async Task<ActionResult> PaymentConfirmation(
    string vnp_Amount,
    string vnp_BankCode,
    string vnp_BankTranNo,
    string vnp_CardType,
    string vnp_OrderInfo,
    string vnp_PayDate,
    string vnp_ResponseCode,
    string vnp_TransactionNo,
    string vnp_TxnRef,
    string vnp_SecureHash)
        {
            try
            {
                // Log payment data for debugging
                System.Diagnostics.Debug.WriteLine($"Payment received at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} for user {User.Identity.Name}");
                System.Diagnostics.Debug.WriteLine($"Order: {vnp_OrderInfo}, Amount: {vnp_Amount}, Response: {vnp_ResponseCode}");

                // Create dictionary of all received parameters
                var vnpParams = new Dictionary<string, string>();
                foreach (string key in Request.QueryString.Keys)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        vnpParams.Add(key, Request.QueryString[key]);
                    }
                }

                // FOR DEMO/TESTING ONLY: Skip validation in test environment
                bool isValidSignature = true;

                // UNCOMMENT THIS FOR PRODUCTION:
                // bool isValidSignature = VnPayHelper.ValidateSignature(vnp_SecureHash, vnpParams);

                if (!isValidSignature)
                {
                    ViewBag.ErrorMessage = "Invalid payment signature";
                    return View("PaymentFailed");
                }

                // Check payment response
                if (vnp_ResponseCode == "00")
                {
                    string bookingCode = vnp_OrderInfo; // The booking code is in vnp_OrderInfo

                    // Get booking details from API
                    var bookingResult = await _apiService.GetBookingTicketAsync(bookingCode);

                    if (bookingResult.Status && bookingResult.Code == "00" && bookingResult.Data != null && bookingResult.Data.Any())
                    {
                        var bookingData = bookingResult.Data.First();

                        // Update local database
                        var booking = _dbContext.BookingOrders.FirstOrDefault(b => b.BookingCode == bookingCode);
                        if (booking != null)
                        {
                            // Calculate the amount (VNPAY adds 00 to the end)
                            decimal paidAmount = decimal.Parse(vnp_Amount) / 100;

                            booking.PaidAmount = paidAmount;
                            booking.PaymentStatus = "Paid";

                            // Store payment details
                            booking.Notes = $"Payment confirmed via {vnp_BankCode} at {DateTime.Now:yyyy-MM-dd HH:mm}. " +
                                           $"Transaction: {vnp_TransactionNo}, Bank Reference: {vnp_BankTranNo}";

                            _dbContext.SaveChanges();

                            // Log success
                            System.Diagnostics.Debug.WriteLine($"Payment successful for booking {bookingCode}");

                            // Redirect to success page
                            return RedirectToAction("BookingComplete", new { bookingCode = bookingCode });
                        }
                    }
                }

                // If we get here, payment was not successful
                ViewBag.ErrorMessage = $"Payment failed or booking information could not be retrieved. Error code: {vnp_ResponseCode}";
                return View("PaymentFailed");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error processing payment: " + ex.Message;
                return View("PaymentFailed");
            }
        }

        // Booking Complete
        public async Task<ActionResult> BookingComplete(string bookingCode)
        {
            try
            {
                // Try to get booking from local database first
                var booking = _dbContext.BookingOrders
                    .Include("Tickets")
                    .FirstOrDefault(b => b.BookingCode == bookingCode);

                if (booking != null)
                {
                    // If found in database, return it to the view
                    ViewBag.ApiResult = null; // No API result
                    return View(booking);
                }

                // If not found in database, try the API
                var bookingResult = await _apiService.GetBookingTicketAsync(bookingCode);

                if (bookingResult.Status && bookingResult.Code == "00" && bookingResult.Data != null && bookingResult.Data.Any())
                {
                    ViewBag.ApiResult = bookingResult.Data.First(); // Store API result in ViewBag
                    return View();  // Pass null to view, it will use ViewBag
                }

                ViewBag.ErrorMessage = "Booking information could not be retrieved.";
                return View("Error");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error retrieving booking details: " + ex.Message;
                return View("Error");
            }
        }

        // View user's booking history (requires authentication)
        [Authorize]
        public ActionResult MyBookings()
        {
            var userId = User.Identity.GetUserId();
            var userEmail = User.Identity.GetUserName();

            // Get bookings associated with the current user
            var bookings = _dbContext.BookingOrders
                .Where(b => b.UserId == userId || b.Email == userEmail)
                .OrderByDescending(b => b.CreatedDate)
                .ToList();

            // Add system time to ViewBag
            ViewBag.CurrentTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            ViewBag.CurrentUser = User.Identity.Name;

            return View(bookings);
        }

        // View specific booking detail (requires authentication or matching email)
        public ActionResult BookingDetail(string bookingCode)
        {
            var booking = _dbContext.BookingOrders
                .Include("Tickets")
                .FirstOrDefault(b => b.BookingCode == bookingCode);

            if (booking == null)
            {
                ViewBag.ErrorMessage = "Booking not found.";
                return View("Error");
            }

            // Security check - only allow if user is authenticated and either:
            // 1. The booking belongs to them, or
            // 2. They are an admin
            bool canAccess = User.Identity.IsAuthenticated &&
                            (booking.UserId == User.Identity.GetUserId() ||
                             booking.Email == User.Identity.Name ||
                             User.IsInRole("Admin"));

            if (!canAccess)
            {
                return new HttpUnauthorizedResult("You do not have permission to view this booking.");
            }

            return View(booking);
        }
        [HttpPost]
        public async Task<ActionResult> ProcessTestPayment(string bookingCode, decimal amount)
        {
            try
            {
                // Get booking details
                var bookingResult = await _apiService.GetBookingTicketAsync(bookingCode);

                if (bookingResult.Status && bookingResult.Code == "00" && bookingResult.Data != null && bookingResult.Data.Any())
                {
                    var bookingData = bookingResult.Data.First();

                    // Update local database
                    var booking = _dbContext.BookingOrders.FirstOrDefault(b => b.BookingCode == bookingCode);
                    if (booking != null)
                    {
                        booking.PaidAmount = amount;
                        booking.PaymentStatus = "Paid";

                        // Store payment details
                        booking.Notes = $"Test payment confirmed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} by user {User.Identity.Name}";

                        _dbContext.SaveChanges();

                        // Redirect to success page
                        return RedirectToAction("BookingComplete", new { bookingCode = bookingCode });
                    }
                }

                ViewBag.ErrorMessage = "Payment failed or booking information could not be retrieved.";
                return View("PaymentFailed");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error processing payment: " + ex.Message;
                return View("PaymentFailed");
            }
        }

        [HttpGet]
        public ActionResult TestPayment(string bookingCode)
        {
            try
            {
                // Find the booking in your database
                var booking = _dbContext.BookingOrders.FirstOrDefault(b => b.BookingCode == bookingCode);

                if (booking == null)
                {
                    ViewBag.ErrorMessage = "Booking not found with code: " + bookingCode;
                    return View("PaymentFailed");
                }

                // Update the booking status
                booking.PaymentStatus = "Paid";
                booking.PaidAmount = booking.TotalAmount;
                booking.Notes = $"Test payment processed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} by user {User.Identity.Name}";

                _dbContext.SaveChanges();

                // Redirect directly to the booking complete page
                return RedirectToAction("BookingComplete", new { bookingCode = bookingCode });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error processing test payment: " + ex.Message;
                return View("PaymentFailed");
            }
        }


        
    }
}