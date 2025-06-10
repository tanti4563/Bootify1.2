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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext?.Dispose();
            }
            base.Dispose(disposing);
        }

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
        public async Task<ActionResult> SelectSeats(int voyageId, DateTime departDate, int? routeId = null, int passengerCount = 1,
            int returnVoyageId = 0, DateTime? returnDate = null, bool isRoundTrip = false)
        {
            try
            {
                // If routeId is not provided, we need to get it from voyage information
                // For now, we'll use a default value or try to get it from the voyage
                int actualRouteId = routeId ?? 1; // Default fallback

                // Get voyage information
                var voyageInfo = await _apiService.GetVoyageInfoAsync(voyageId, actualRouteId, departDate, passengerCount);
                if (voyageInfo == null)
                {
                    throw new Exception("Voyage information not found");
                }

                // Update routeId from voyage info if it was null
                if (!routeId.HasValue && voyageInfo.RouteId > 0)
                {
                    actualRouteId = voyageInfo.RouteId;
                }

                // Get all seats from API (including occupied ones)
                var allSeats = await _apiService.GetSeatsEmptyAsync(voyageId, departDate);
                var occupiedSeats = await GetOccupiedSeatsAsync(voyageId, departDate);
                var prices = await _apiService.GetTicketPricesAsync(actualRouteId, voyageInfo.BoatTypeId, departDate);

                var viewModel = new SeatSelectionViewModel
                {
                    VoyageId = voyageId,
                    DepartDate = departDate,
                    Seats = allSeats,
                    TicketPrices = prices,
                    PassengerCount = passengerCount,
                    IsRoundTrip = isRoundTrip,
                    VoyageInfo = voyageInfo
                };

                // Pass occupied seats to the view
                ViewBag.OccupiedSeats = occupiedSeats;

                // Get nations for nationality dropdown
                var nations = await _apiService.GetNationsAsync();
                ViewBag.Nations = nations;

                // Get ticket types for ticket type selection
                var ticketTypes = await _apiService.GetTicketTypesAsync();
                ViewBag.TicketTypes = ticketTypes;

                if (isRoundTrip && returnVoyageId > 0 && returnDate.HasValue)
                {
                    viewModel.ReturnVoyageId = returnVoyageId;
                    viewModel.ReturnDate = returnDate.Value;

                    var returnVoyageInfo = await _apiService.GetVoyageInfoAsync(returnVoyageId, actualRouteId, returnDate.Value, passengerCount);
                    ViewBag.ReturnVoyageInfo = returnVoyageInfo;

                    var allReturnSeats = await _apiService.GetSeatsEmptyAsync(returnVoyageId, returnDate.Value);
                    var occupiedReturnSeats = await GetOccupiedSeatsAsync(returnVoyageId, returnDate.Value);
                    var returnPrices = await _apiService.GetTicketPricesAsync(actualRouteId, returnVoyageInfo.BoatTypeId, returnDate.Value);
                    ViewBag.ReturnSeats = allReturnSeats;
                    ViewBag.OccupiedReturnSeats = occupiedReturnSeats;
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



        // Step 4: Create Booking (now called directly from SelectSeats)
        [HttpPost]
        public async Task<ActionResult> CreateBooking(BookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Please fill all required fields";
                TempData["ErrorMessage"] = "Please fill all required fields and try again.";
                return RedirectToAction("SelectSeats", new {
                    voyageId = model.VoyageId,
                    departDate = model.DepartDate.ToString("yyyy-MM-dd"),
                    routeId = model.RouteId,
                    passengerCount = model.Passengers?.Count ?? 1,
                    returnVoyageId = model.ReturnVoyageId,
                    returnDate = model.ReturnDate?.ToString("yyyy-MM-dd"),
                    isRoundTrip = model.IsRoundTrip
                });
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
                        PaymentStatus = "Pending",
                        CreatedDate = DateTime.UtcNow, // Use UTC time
                        Tickets = new List<TicketOrder>()
                    };

                    // Add tickets to database
                    for (int i = 0; i < model.Passengers.Count; i++)
                    {
                        var passenger = model.Passengers[i];
                        var ticket = new TicketOrder
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
                        };

                        // Don't set StatusId during creation - will be set after successful save

                        bookingOrder.Tickets.Add(ticket);
                    }

                    if (model.IsRoundTrip && model.ReturnPassengers != null)
                    {
                        for (int i = 0; i < model.ReturnPassengers.Count; i++)
                        {
                            var passenger = model.ReturnPassengers[i];
                            var returnTicket = new TicketOrder
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
                            };

                            // Don't set StatusId during creation - will be set after successful save

                            bookingOrder.Tickets.Add(returnTicket);
                        }
                    }

                    // Add booking to context and save
                    _dbContext.BookingOrders.Add(bookingOrder);
                    _dbContext.SaveChanges();

                    // Redirect to payment page
                    return RedirectToAction("Payment", new { bookingCode = result.Data.BookingCode, amount = result.Data.TotalAmt });
                }
                else
                {
                    TempData["ErrorMessage"] = $"Error creating booking: {result.Message}";
                    return RedirectToAction("SelectSeats", new {
                        voyageId = model.VoyageId,
                        departDate = model.DepartDate.ToString("yyyy-MM-dd"),
                        routeId = model.RouteId,
                        passengerCount = model.Passengers?.Count ?? 1,
                        returnVoyageId = model.ReturnVoyageId,
                        returnDate = model.ReturnDate?.ToString("yyyy-MM-dd"),
                        isRoundTrip = model.IsRoundTrip
                    });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating booking: " + ex.Message;
                return RedirectToAction("SelectSeats", new {
                    voyageId = model.VoyageId,
                    departDate = model.DepartDate.ToString("yyyy-MM-dd"),
                    routeId = model.RouteId,
                    passengerCount = model.Passengers?.Count ?? 1,
                    returnVoyageId = model.ReturnVoyageId,
                    returnDate = model.ReturnDate?.ToString("yyyy-MM-dd"),
                    isRoundTrip = model.IsRoundTrip
                });
            }
        }

        // Step 5: Payment
        public ActionResult Payment(string bookingCode, decimal amount)
        {
            ViewBag.BookingCode = bookingCode;
            ViewBag.Amount = amount;

            // For production, uncomment this to create the real VNPAY URL
            // string returnUrl = Url.Action("PaymentConfirmation", "Booking", null, Request.Url.Scheme);
            // ViewBag.PaymentUrl = VnPayHelper.CreatePaymentUrl(returnUrl, bookingCode, amount, bookingCode);

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
                // Log detailed payment data for debugging
                System.Diagnostics.Debug.WriteLine($"=== VNPay Payment Confirmation ===");
                System.Diagnostics.Debug.WriteLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"User: {User.Identity.Name}");
                System.Diagnostics.Debug.WriteLine($"Amount: {vnp_Amount} (VND with 00 suffix)");
                System.Diagnostics.Debug.WriteLine($"Bank Code: {vnp_BankCode}");
                System.Diagnostics.Debug.WriteLine($"Bank Transaction: {vnp_BankTranNo}");
                System.Diagnostics.Debug.WriteLine($"Card Type: {vnp_CardType}");
                System.Diagnostics.Debug.WriteLine($"Order Info: {vnp_OrderInfo}");
                System.Diagnostics.Debug.WriteLine($"Pay Date: {vnp_PayDate}");
                System.Diagnostics.Debug.WriteLine($"Response Code: {vnp_ResponseCode}");
                System.Diagnostics.Debug.WriteLine($"Transaction No: {vnp_TransactionNo}");
                System.Diagnostics.Debug.WriteLine($"TxnRef: {vnp_TxnRef}");
                System.Diagnostics.Debug.WriteLine($"Secure Hash: {vnp_SecureHash}");

                // Validate required parameters
                if (string.IsNullOrEmpty(vnp_Amount) || string.IsNullOrEmpty(vnp_OrderInfo) ||
                    string.IsNullOrEmpty(vnp_ResponseCode) || string.IsNullOrEmpty(vnp_TransactionNo))
                {
                    ViewBag.ErrorMessage = "Missing required payment parameters";
                    return View("PaymentFailed");
                }

                // Create dictionary of all received parameters for signature validation
                var vnpParams = new Dictionary<string, string>();
                foreach (string key in Request.QueryString.Keys)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_") && key != "vnp_SecureHash")
                    {
                        vnpParams.Add(key, Request.QueryString[key]);
                    }
                }

                // TODO: Implement signature validation for production
                // For now, we'll validate the basic structure and response code
                bool isValidSignature = true;

                // PRODUCTION CODE - Uncomment and implement VnPayHelper:
                // const string secretKey = "0KASCDVFSAQY6BNYJHUWKBKJXM6";
                // bool isValidSignature = VnPayHelper.ValidateSignature(vnp_SecureHash, vnpParams, secretKey);

                if (!isValidSignature)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Invalid payment signature");
                    ViewBag.ErrorMessage = "Invalid payment signature";
                    return View("PaymentFailed");
                }

                // Check payment response code
                if (vnp_ResponseCode == "00") // Success
                {
                    string bookingCode = vnp_OrderInfo; // The booking code is in vnp_OrderInfo
                    System.Diagnostics.Debug.WriteLine($"✅ Payment successful for booking: {bookingCode}");

                    // Parse payment date (format: yyyyMMddHHmm)
                    DateTime paymentDate = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(vnp_PayDate) && vnp_PayDate.Length == 12)
                    {
                        try
                        {
                            paymentDate = DateTime.ParseExact(vnp_PayDate, "yyyyMMddHHmm", null);
                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Could not parse payment date: {vnp_PayDate}");
                        }
                    }

                    // Calculate the actual amount (VNPay adds 00 to the end)
                    decimal paidAmount = 0;
                    if (decimal.TryParse(vnp_Amount, out decimal vnpAmount))
                    {
                        paidAmount = vnpAmount / 100; // Remove the 00 suffix
                    }

                    System.Diagnostics.Debug.WriteLine($"💰 Parsed amount: {paidAmount:N2} VND");

                    // Update local database
                    var booking = _dbContext.BookingOrders
                        .Include("Tickets")
                        .FirstOrDefault(b => b.BookingCode == bookingCode);

                    if (booking != null)
                    {
                        // Update booking with payment information
                        booking.PaidAmount = paidAmount;
                        booking.PaymentStatus = "Paid";

                        // Create detailed payment notes
                        var paymentNotes = $"VNPay Payment Confirmed\n" +
                                         $"Amount: {paidAmount:N2} VND\n" +
                                         $"Bank: {vnp_BankCode}\n" +
                                         $"Card Type: {vnp_CardType}\n" +
                                         $"Transaction ID: {vnp_TransactionNo}\n" +
                                         $"Bank Reference: {vnp_BankTranNo}\n" +
                                         $"Payment Date: {paymentDate:yyyy-MM-dd HH:mm:ss}\n" +
                                         $"Order Reference: {vnp_TxnRef}";

                        booking.Notes = paymentNotes;

                        // Save changes
                        _dbContext.SaveChanges();

                        System.Diagnostics.Debug.WriteLine($"✅ Database updated successfully for booking {bookingCode}");
                        System.Diagnostics.Debug.WriteLine($"💾 Payment amount: {paidAmount:N2} VND");
                        System.Diagnostics.Debug.WriteLine($"📝 Payment notes: {paymentNotes}");

                        // Redirect to success page
                        return RedirectToAction("BookingComplete", new { bookingCode = bookingCode });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Booking not found in database: {bookingCode}");
                        ViewBag.ErrorMessage = $"Booking {bookingCode} not found in database";
                        return View("PaymentFailed");
                    }
                }
                else
                {
                    // Payment failed - log the error code
                    string errorMessage = GetVnPayErrorMessage(vnp_ResponseCode);
                    System.Diagnostics.Debug.WriteLine($"❌ Payment failed with code: {vnp_ResponseCode} - {errorMessage}");
                    ViewBag.ErrorMessage = $"Payment failed: {errorMessage} (Code: {vnp_ResponseCode})";
                    return View("PaymentFailed");
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception in PaymentConfirmation: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
                ViewBag.ErrorMessage = "Error processing payment: " + ex.Message;
                return View("PaymentFailed");
            }
        }

        // Helper method to get VNPay error messages
        private string GetVnPayErrorMessage(string responseCode)
        {
            switch (responseCode)
            {
                case "00": return "Transaction successful";
                case "07": return "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).";
                case "09": return "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.";
                case "10": return "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần";
                case "11": return "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.";
                case "12": return "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.";
                case "13": return "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP). Xin quý khách vui lòng thực hiện lại giao dịch.";
                case "24": return "Giao dịch không thành công do: Khách hàng hủy giao dịch";
                case "51": return "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.";
                case "65": return "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.";
                case "75": return "Ngân hàng thanh toán đang bảo trì.";
                case "79": return "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định. Xin quý khách vui lòng thực hiện lại giao dịch";
                case "99": return "Các lỗi khác (lỗi còn lại, không có trong danh sách mã lỗi đã liệt kê)";
                default: return $"Unknown error code: {responseCode}";
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
                    var booking = _dbContext.BookingOrders
                        .Include("Tickets")
                        .FirstOrDefault(b => b.BookingCode == bookingCode);
                    if (booking != null)
                    {
                        booking.PaidAmount = amount;
                        booking.PaymentStatus = "Paid";

                        // Store payment details
                        booking.Notes = $"Test payment confirmed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} by user {User.Identity.Name}";

                        // Seat status tracking removed for compatibility

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
                var booking = _dbContext.BookingOrders
                    .Include("Tickets")
                    .FirstOrDefault(b => b.BookingCode == bookingCode);

                if (booking == null)
                {
                    ViewBag.ErrorMessage = "Booking not found with code: " + bookingCode;
                    return View("PaymentFailed");
                }

                // Update the booking status
                booking.PaymentStatus = "Paid";
                booking.PaidAmount = booking.TotalAmount;
                booking.Notes = $"Test payment processed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} by user {User.Identity.Name}";

                // Seat status tracking removed for compatibility

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

        // Helper method to get available seats (filtering out booked seats)
        private async Task<List<SeatInfo>> GetAvailableSeatsAsync(int voyageId, DateTime departDate)
        {
            try
            {
                // Get all seats from API
                var allSeats = await _apiService.GetSeatsEmptyAsync(voyageId, departDate);

                // Get booked seats from local database for this voyage and date
                var bookedPositionIds = new List<int>();
                var bookedSeatNames = new List<string>();

                try
                {
                    // Use PaymentStatus to filter booked seats (simple and reliable)
                    var bookedSeats = _dbContext.TicketOrders
                        .Where(t => t.BookingOrder.VoyageId == voyageId &&
                                   t.BookingOrder.DepartDate.Year == departDate.Year &&
                                   t.BookingOrder.DepartDate.Month == departDate.Month &&
                                   t.BookingOrder.DepartDate.Day == departDate.Day &&
                                   (t.BookingOrder.PaymentStatus == "Paid" || t.BookingOrder.PaymentStatus == "Pending"))
                        .Select(t => new { t.PositionId, t.SeatNm })
                        .ToList();

                    bookedPositionIds = bookedSeats.Select(b => b.PositionId).ToList();
                    bookedSeatNames = bookedSeats.Select(b => b.SeatNm).Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error querying booked seats: {ex.Message}");
                    // Fall back to empty lists if there's an error
                    bookedPositionIds = new List<int>();
                    bookedSeatNames = new List<string>();
                }

                // Filter out booked seats
                var availableSeats = allSeats.Where(seat =>
                    !bookedPositionIds.Contains(seat.PositionId) &&
                    !bookedSeatNames.Contains(seat.SeatNm))
                    .ToList();

                return availableSeats;
            }
            catch (Exception ex)
            {
                // If there's an error with filtering, fall back to API seats only
                System.Diagnostics.Debug.WriteLine($"Error filtering booked seats: {ex.Message}");
                return await _apiService.GetSeatsEmptyAsync(voyageId, departDate);
            }
        }

        // Helper method to clean up expired bookings (simplified version)
        public void CleanupExpiredBookings()
        {
            try
            {
                var expiredTime = DateTime.UtcNow.AddMinutes(-30); // Clean up bookings older than 30 minutes

                var expiredBookings = _dbContext.BookingOrders
                    .Where(b => b.CreatedDate < expiredTime && b.PaymentStatus == "Pending")
                    .ToList();

                foreach (var booking in expiredBookings)
                {
                    booking.PaymentStatus = "Expired";
                }

                if (expiredBookings.Any())
                {
                    _dbContext.SaveChanges();
                    System.Diagnostics.Debug.WriteLine($"Marked {expiredBookings.Count} expired bookings");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up expired bookings: {ex.Message}");
            }
        }

        // Method to check seat availability before allowing selection
        [HttpPost]
        public async Task<JsonResult> CheckSeatAvailability(int seatId, int voyageId, DateTime departDate)
        {
            try
            {
                // Check if seat is occupied
                var occupiedSeats = await GetOccupiedSeatsAsync(voyageId, departDate);
                var isOccupied = occupiedSeats.Any(o => o.PositionId == seatId);
                var isAvailable = !isOccupied;

                return Json(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                return Json(new { available = false, error = ex.Message });
            }
        }



        // Test endpoint to check system status
        [HttpGet]
        [AllowAnonymous]
        public ActionResult SystemStatus()
        {
            try
            {
                var status = new
                {
                    DatabaseConnected = _dbContext.Database.Exists(),
                    BookingOrdersCount = _dbContext.BookingOrders.Count(),
                    TicketOrdersCount = _dbContext.TicketOrders.Count(),
                    Timestamp = DateTime.UtcNow
                };

                return Json(status, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, timestamp = DateTime.UtcNow }, JsonRequestBehavior.AllowGet);
            }
        }

        // Test endpoint to create a simple booking
        [HttpGet]
        [AllowAnonymous]
        public ActionResult TestBookingCreation()
        {
            try
            {
                var testBooking = new BookingOrder
                {
                    BookingCode = "TEST" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    OrderNo = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Booker = "Test User",
                    ContactNo = "1234567890",
                    Email = "test@test.com",
                    TotalNumber = 1,
                    TotalAmount = 100,
                    PaidAmount = 0,
                    BoatId = 1,
                    VoyageId = 1,
                    ScheduleId = 1,
                    RouteId = 1,
                    DepartDate = DateTime.Now.AddDays(1),
                    IsRoundTrip = false,
                    PaymentStatus = "Pending",
                    CreatedDate = DateTime.UtcNow,
                    UserId = User.Identity.IsAuthenticated ? User.Identity.GetUserId() : "anonymous"
                };

                _dbContext.BookingOrders.Add(testBooking);
                _dbContext.SaveChanges();

                return Json(new {
                    success = true,
                    bookingCode = testBooking.BookingCode,
                    message = "✅ Test booking created successfully! The error is FIXED!"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new {
                    success = false,
                    error = ex.Message,
                    message = "❌ Error still exists. Check database connection and schema."
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // Helper method to get occupied seats for display purposes
        private async Task<List<OccupiedSeatInfo>> GetOccupiedSeatsAsync(int voyageId, DateTime departDate)
        {
            try
            {
                // Get booked seats from local database for this voyage and date
                var bookedSeats = _dbContext.TicketOrders
                    .Where(t => t.BookingOrder.VoyageId == voyageId &&
                               t.BookingOrder.DepartDate.Year == departDate.Year &&
                               t.BookingOrder.DepartDate.Month == departDate.Month &&
                               t.BookingOrder.DepartDate.Day == departDate.Day &&
                               (t.BookingOrder.PaymentStatus == "Paid" || t.BookingOrder.PaymentStatus == "Pending"))
                    .Select(t => new OccupiedSeatInfo { PositionId = t.PositionId, SeatNm = t.SeatNm })
                    .ToList();

                return bookedSeats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting occupied seats: {ex.Message}");
                return new List<OccupiedSeatInfo>();
            }
        }

        // Helper class for occupied seat information
        public class OccupiedSeatInfo
        {
            public int PositionId { get; set; }
            public string SeatNm { get; set; }
        }

        // Test endpoint to simulate VNPay payment confirmation with your provided data
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> TestVnPayPayment(string bookingCode)
        {
            if (string.IsNullOrEmpty(bookingCode))
            {
                return Json(new { success = false, message = "Booking code is required" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                // Simulate the VNPay payment data you provided
                var vnpayData = new
                {
                    vnp_Amount = "78000000", // 780,000 VND (with 00 suffix)
                    vnp_BankCode = "VNPAY",
                    vnp_BankTranNo = "20241129970",
                    vnp_CardType = "VNPAY",
                    vnp_OrderInfo = bookingCode, // Use the provided booking code
                    vnp_PayDate = DateTime.Now.ToString("yyyyMMddHHmm"),
                    vnp_ResponseCode = "00", // Success
                    vnp_TransactionNo = "970532916",
                    vnp_TxnRef = bookingCode,
                    vnp_SecureHash = "3f641e61d310b327327920473be2377c7e1e8a8c4aef765bd38612e2d0cffa133cf1492a4492de3ed9396e4b6f72939d46e005fd235f444562d52ab152d0248c"
                };

                // Call the PaymentConfirmation method with the simulated data
                var result = await PaymentConfirmation(
                    vnpayData.vnp_Amount,
                    vnpayData.vnp_BankCode,
                    vnpayData.vnp_BankTranNo,
                    vnpayData.vnp_CardType,
                    vnpayData.vnp_OrderInfo,
                    vnpayData.vnp_PayDate,
                    vnpayData.vnp_ResponseCode,
                    vnpayData.vnp_TransactionNo,
                    vnpayData.vnp_TxnRef,
                    vnpayData.vnp_SecureHash
                );

                return Json(new {
                    success = true,
                    message = "VNPay payment simulation completed",
                    paymentData = vnpayData,
                    result = "Check booking status - should be marked as Paid"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new {
                    success = false,
                    message = "Error simulating VNPay payment: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // Test endpoint to check booking payment status
        [HttpGet]
        [AllowAnonymous]
        public ActionResult CheckBookingPaymentStatus(string bookingCode)
        {
            if (string.IsNullOrEmpty(bookingCode))
            {
                return Json(new { success = false, message = "Booking code is required" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var booking = _dbContext.BookingOrders
                    .Include("Tickets")
                    .FirstOrDefault(b => b.BookingCode == bookingCode);

                if (booking == null)
                {
                    return Json(new {
                        success = false,
                        message = "Booking not found"
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new {
                    success = true,
                    bookingCode = booking.BookingCode,
                    paymentStatus = booking.PaymentStatus,
                    totalAmount = booking.TotalAmount,
                    paidAmount = booking.PaidAmount,
                    createdDate = booking.CreatedDate,
                    notes = booking.Notes,
                    ticketCount = booking.Tickets?.Count ?? 0
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new {
                    success = false,
                    message = "Error checking booking status: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // Test endpoint to check ticket types data
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> TestTicketTypes()
        {
            try
            {
                var ticketTypes = await _apiService.GetTicketTypesAsync();
                return Json(new {
                    success = true,
                    ticketTypes = ticketTypes,
                    count = ticketTypes?.Count ?? 0,
                    message = "Ticket types loaded successfully"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new {
                    success = false,
                    error = ex.Message,
                    message = "Error loading ticket types"
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // Test endpoint to check nations data
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> TestNations()
        {
            try
            {
                var nations = await _apiService.GetNationsAsync();
                return Json(new {
                    success = true,
                    nations = nations,
                    count = nations?.Count ?? 0,
                    message = "Nations loaded successfully"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new {
                    success = false,
                    error = ex.Message,
                    message = "Error loading nations"
                }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}