# Seat Locking Implementation

## Overview
This implementation adds seat locking functionality to prevent double-booking of seats in the ferry booking system.

## Key Features

### 1. Seat Status Management
- **Available (0)**: Seat is available for booking
- **Reserved (1)**: Seat is temporarily reserved during booking process
- **Purchased (2)**: Seat is permanently locked after successful payment
- **Blocked (3)**: Seat is administratively blocked

### 2. Booking Process Flow
1. **Seat Selection**: Users can only select available seats
2. **Reservation**: Selected seats are marked as "Reserved" when booking is created
3. **Payment**: Upon successful payment, seats are marked as "Purchased"
4. **Expiration**: Reserved seats are automatically released after 30 minutes if payment is not completed

### 3. Database Changes
Added to `BookingOrders` table:
- `ReturnVoyageId` (int, nullable): Stores return voyage ID for round trips
- `ReturnDate` (datetime2, nullable): Stores return date for round trips

Added to `TicketOrders` table:
- `StatusId` (int): Maps to SeatStatus enum (0=Available, 1=Reserved, 2=Purchased, 3=Blocked)

### 4. Real-time Seat Availability
- AJAX calls check seat availability before selection
- Seats that become unavailable are automatically marked as occupied
- Users are notified if a seat is no longer available

## Implementation Details

### Modified Files
1. **Models/DbModels.cs**: Added ReturnVoyageId, ReturnDate to BookingOrder; StatusId to TicketOrder
2. **Controllers/BookingController.cs**: 
   - Added GetAvailableSeatsAsync() method to filter booked seats
   - Updated booking creation to set seat status to Reserved
   - Updated payment confirmation to set seat status to Purchased
   - Added CheckSeatAvailability() AJAX endpoint
   - Added ReleaseExpiredReservations() method
3. **Views/Booking/SelectSeats.cshtml**: Added real-time seat availability checking
4. **Services/ApiService.cs**: Added Entity Framework using statement

### Database Migration
Run the SQL script in `Migrations/AddReturnVoyageFields.sql` to add the new columns to your database.

## Usage

### For Developers
1. Run the database migration script
2. The seat locking is automatically handled during the booking process
3. Consider setting up a scheduled task to call `ReleaseExpiredReservations()` periodically

### For Users
- Seats are now locked immediately when another user completes payment
- Real-time feedback when seats become unavailable
- 30-minute reservation window to complete payment

## Configuration
- Reservation expiration time: 30 minutes (configurable in ReleaseExpiredReservations method)
- Seat availability is checked both server-side and client-side

## Testing
1. Create multiple bookings for the same voyage
2. Verify seats become unavailable after payment
3. Test reservation expiration by abandoning payment process
4. Verify real-time seat availability updates

## Notes
- The system now properly handles both outbound and return trip seat reservations
- Expired reservations should be cleaned up regularly to free up seats
- Consider implementing a background service for automatic cleanup in production
