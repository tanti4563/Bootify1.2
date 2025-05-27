using FerryBookingSystem.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FerryBookingSystem.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiConfig.BaseUrl)
            };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Authorization", ApiConfig.AuthorizationHeader);
            _httpClient.DefaultRequestHeaders.Add("HVC.APIKey", ApiConfig.ApiKey);
        }

        // Step 1: Get Routes
        public async Task<List<RouteInfo>> GetRoutesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("Route/GetRoutes");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<RouteInfo>>(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting routes: {ex.Message}", ex);
            }
        }

        // Step 2: Search Voyages for outbound trip
        public async Task<List<VoyageInfo>> SearchVoyagesAsync(int routeId, DateTime departDate, int noOfPassenger)
        {
            try
            {
                var query = $"RouteId={routeId}&DepartDate={departDate:yyyy-MM-dd}&NoOfPassenger={noOfPassenger}";
                var response = await _httpClient.GetAsync($"OnlineSearchVoyageResult/SearchVoyage?{query}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<VoyageInfo>>(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching voyages: {ex.Message}", ex);
            }
        }

        // Step 2: Search Voyages for return trip
        public async Task<List<VoyageInfo>> SearchReturnVoyagesAsync(int routeIdTripGo, DateTime departDateBack, int noOfPassenger)
        {
            try
            {
                var query = $"RouteIdTripGo={routeIdTripGo}&DepartDateBack={departDateBack:yyyy-MM-dd}&NoOfPassenger={noOfPassenger}";
                var response = await _httpClient.GetAsync($"OnlineSearchVoyageResult/SearchVoyageTripBack?{query}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<VoyageInfo>>(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching return voyages: {ex.Message}", ex);
            }
        }

        // Step 3: Get Empty Seats
        public async Task<List<SeatInfo>> GetSeatsEmptyAsync(int voyageId, DateTime departDate)
        {
            try
            {
                var query = $"voyageId={voyageId}&departDate={departDate:yyyy-MM-dd}";
                var response = await _httpClient.GetAsync($"Voyage/GetSeatsEmpty?{query}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var seats = JsonConvert.DeserializeObject<List<SeatInfo>>(content);

                // Enhance the seats with deck information (for UI organization)
                foreach (var seat in seats)
                {
                    // Assign deck number based on seat name pattern (example: 21A would be deck 2)
                    if (!string.IsNullOrEmpty(seat.SeatNm) && seat.SeatNm.Length > 1 && char.IsDigit(seat.SeatNm[0]))
                    {
                        seat.DeckNumber = int.Parse(seat.SeatNm[0].ToString());
                    }
                    else
                    {
                        seat.DeckNumber = 1; // Default deck
                    }
                }

                return seats.OrderBy(s => s.DeckNumber).ThenBy(s => s.SeatNm).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting empty seats: {ex.Message}", ex);
            }
        }

        // Get voyage information by ID
        public async Task<VoyageInfo> GetVoyageInfoAsync(int voyageId, int routeId, DateTime departDate, int noOfPassenger)
        {
            try
            {
                var voyages = await SearchVoyagesAsync(routeId, departDate, noOfPassenger);
                return voyages.FirstOrDefault(v => v.VoyageId == voyageId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting voyage info: {ex.Message}", ex);
            }
        }

        // Step 3: Get Ticket Prices
        public async Task<List<TicketPriceInfo>> GetTicketPricesAsync(int routeId, int boatTypeId, DateTime departDate)
        {
            try
            {
                var query = $"routeId={routeId}&boatTypeId={boatTypeId}&DepartDate={departDate:yyyy-MM-dd}";
                var response = await _httpClient.GetAsync($"TicketPrice/GetTicketPriceByRouteId?{query}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<TicketPriceInfo>>(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting ticket prices: {ex.Message}", ex);
            }
        }

        // Step 4: Get Nations
        public async Task<List<NationInfo>> GetNationsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("Nation/GetNations");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<NationInfo>>(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting nations: {ex.Message}", ex);
            }
        }

        // Step 4: Create Order
        public async Task<ApiResponse<CreateOrderData>> CreateOrderAsync(CreateOrderRequest orderRequest)
        {
            try
            {
                var json = JsonConvert.SerializeObject(orderRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("OrderOnline/CreateOrderOnline", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse<CreateOrderData>>(responseContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating order: {ex.Message}", ex);
            }
        }

        // Step 5: Get Booking Ticket (after payment)
        public async Task<ApiResponse<List<OrderResult>>> GetBookingTicketAsync(string bookingCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"OrderOnline/GetBookingTicket?bookingCode={bookingCode}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse<List<OrderResult>>>(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting booking ticket: {ex.Message}", ex);
            }
        }
    }
}