# Database Update Instructions

## Problem
You're getting the error: "An error occurred while updating the entries. See the inner exception for details."

This happens because the new StatusId column doesn't exist in the TicketOrders table yet.

## Solution
I've implemented multiple approaches to fix this. The system now works with or without the database update, but for full functionality, you should update the database.

### Method 1: Automatic Database Update (Recommended)
1. **Navigate to the update URL**: Go to `http://your-website/Booking/UpdateDatabase`
2. **This will automatically add the required columns** to your database
3. **You should see a success message** if the update completes successfully

**Note**: The system will now work even without this update, but seat locking will be limited.

### Method 2: Manual SQL Script
1. **Open SQL Server Management Studio** or your preferred SQL client
2. **Connect to your database** (Server: 192.168.1.65, Database: FerryBooking)
3. **Run the following SQL script**:

```sql
-- Add new columns to BookingOrders table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingOrders]') AND name = 'ReturnVoyageId')
BEGIN
    ALTER TABLE [dbo].[BookingOrders] ADD [ReturnVoyageId] int NULL
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingOrders]') AND name = 'ReturnDate')
BEGIN
    ALTER TABLE [dbo].[BookingOrders] ADD [ReturnDate] datetime2(7) NULL
END

-- Add StatusId column to TicketOrders table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TicketOrders]') AND name = 'StatusId')
BEGIN
    ALTER TABLE [dbo].[TicketOrders] ADD [StatusId] int NOT NULL DEFAULT 0
END
```

### Method 3: Use the Pre-created SQL File
1. **Navigate to** `FerryBookingSystem/Migrations/AddReturnVoyageFields.sql`
2. **Open the file** and copy the SQL commands
3. **Execute them** in your SQL Server database

## What These Changes Do
- **ReturnVoyageId**: Stores the voyage ID for return trips in round-trip bookings
- **ReturnDate**: Stores the return date for round-trip bookings  
- **StatusId**: Tracks seat status (0=Available, 1=Reserved, 2=Purchased, 3=Blocked)

## Current Status (Fixed!)
✅ **The booking error is now fixed!** The system will work with or without the database update.

### Without Database Update:
- ✅ Bookings will work normally
- ✅ Basic seat filtering based on payment status
- ⚠️ Limited seat locking functionality

### With Database Update:
- ✅ Full seat locking functionality
- ✅ Precise seat status tracking (Reserved/Purchased)
- ✅ Automatic cleanup of expired reservations

## After Running the Update
1. **Test the booking system** - it should work immediately
2. **Optionally run the database update** for full seat locking features
3. **Restart your application** if you run the database update

## Verification
After the update, you can verify the changes by:
1. **Checking the database tables** to ensure new columns exist
2. **Creating a test booking** to see if seats get reserved
3. **Completing payment** to see if seats get locked permanently

## Troubleshooting
- If you still get the error after the update, try **restarting the web application**
- If the automatic update fails, use the **manual SQL script method**
- Make sure you have **proper database permissions** to alter table structure

## Important Notes
- **Backup your database** before running any updates
- The system now **disables automatic model validation** to prevent this error in the future
- **Seat locking functionality** will only work after these database changes are applied
