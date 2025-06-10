# DYNAMIC TICKET PRICING BY TICKET TYPE

## ✅ **FEATURE IMPLEMENTED**

I've successfully implemented dynamic ticket pricing that varies based on the selected ticket type. Prices now automatically update when users change their ticket type selection.

## 🎯 **How It Works**

### **Pricing Strategy:**
1. **API-Based Pricing** - First tries to get exact price from API for specific ticket type
2. **Multiplier-Based Pricing** - If no specific price found, applies multipliers to adult price
3. **Fallback Pricing** - Uses first available price for the seat class as last resort

### **Price Multipliers:**
- **Adult (100%)** - Full price
- **Child 6-11 years (75%)** - 25% discount
- **Infant Under 6 (10%)** - 90% discount
- **Senior Over 60 (85%)** - 15% discount
- **Student (90%)** - 10% discount

## 🔧 **Technical Implementation**

### **1. Dynamic Price Calculation**
```javascript
function calculatePriceForTicketType(ticketClass, ticketTypeId, tripType) {
    var basePrices = tripType === 'outbound' ? outboundTicketPrices : returnTicketPrices;
    
    // Try to find exact price from API
    var basePrice = basePrices.find(p => p.TicketClass === ticketClass && p.TicketTypeId === ticketTypeId);
    if (basePrice) {
        return basePrice.PriceWithVAT;
    }
    
    // Apply multiplier to adult price
    var adultPrice = basePrices.find(p => p.TicketClass === ticketClass && p.TicketTypeId === 1);
    if (adultPrice) {
        var multiplier = getTicketTypeMultiplier(ticketTypeId);
        return Math.round(adultPrice.PriceWithVAT * multiplier);
    }
    
    // Fallback to any available price
    var fallbackPrice = basePrices.find(p => p.TicketClass === ticketClass);
    return fallbackPrice ? fallbackPrice.PriceWithVAT : 0;
}
```

### **2. Real-Time Price Updates**
```javascript
// When user changes ticket type
$(document).on('change', '.ticket-type-select', function() {
    var seatIndex = $(this).data('seat-index');
    var tripType = $(this).data('trip-type');
    var selectedTicketTypeId = parseInt($(this).val());
    
    // Calculate new price
    var newPrice = calculatePriceForTicketType(seat.ticketClass, selectedTicketTypeId, tripType);
    
    // Update seat object
    seat.priceWithVAT = newPrice;
    seat.ticketTypeId = selectedTicketTypeId;
    
    // Update form fields
    $(`input[name="Passengers[${seatIndex}].Price"]`).val(newPrice);
    $(`input[name="Passengers[${seatIndex}].TicketTypeId"]`).val(selectedTicketTypeId);
    
    // Refresh summary
    updateSummary();
});
```

### **3. Enhanced Summary Display**
```javascript
// Shows ticket type and updated price
outboundHtml += '<tr>' +
    '<td>' + seat.seatName + '</td>' +
    '<td>' + seat.ticketClass + (seat.isVip ? ' (VIP)' : '') + 
         '<br><small class="text-muted">' + ticketTypeName + '</small></td>' +
    '<td>' + seat.priceWithVAT.toFixed(2) + ' VND</td>' +
    '</tr>';
```

## 🎨 **User Experience**

### **Seat Selection Process:**
1. **Select seat** → Form appears with Adult ticket type selected by default
2. **Change ticket type** → Price updates immediately in summary
3. **Visual feedback** → Summary shows both seat class and ticket type
4. **Real-time totals** → Grand total recalculates automatically

### **Price Display:**
- **Individual prices** shown for each selected seat
- **Ticket type labels** displayed under seat class
- **Currency formatting** with VND suffix
- **Real-time updates** as selections change

### **Default Behavior:**
- **Adult ticket type** selected by default
- **Adult pricing** applied initially
- **Immediate price calculation** when seat selected
- **Smooth transitions** when changing ticket types

## 💰 **Pricing Examples**

### **Economy Class Seat - 500,000 VND Adult Price:**
- **Adult**: 500,000 VND (100%)
- **Child**: 375,000 VND (75%)
- **Infant**: 50,000 VND (10%)
- **Senior**: 425,000 VND (85%)
- **Student**: 450,000 VND (90%)

### **VIP Class Seat - 800,000 VND Adult Price:**
- **Adult**: 800,000 VND (100%)
- **Child**: 600,000 VND (75%)
- **Infant**: 80,000 VND (10%)
- **Senior**: 680,000 VND (85%)
- **Student**: 720,000 VND (90%)

## ✅ **Features Implemented**

### **1. Dynamic Pricing**
- ✅ **Real-time price calculation** based on ticket type
- ✅ **API-first approach** - uses exact prices when available
- ✅ **Multiplier fallback** - applies discounts to adult prices
- ✅ **Graceful degradation** - always provides a price

### **2. Enhanced User Interface**
- ✅ **Ticket type dropdown** for each passenger
- ✅ **Default Adult selection** for immediate pricing
- ✅ **Real-time summary updates** showing prices and types
- ✅ **Currency formatting** with VND display

### **3. Form Integration**
- ✅ **Hidden field updates** for proper form submission
- ✅ **Validation integration** with ticket type requirements
- ✅ **Data binding** for both outbound and return journeys
- ✅ **Consistent behavior** across all seat types

### **4. Price Transparency**
- ✅ **Individual seat pricing** clearly displayed
- ✅ **Ticket type identification** in summary
- ✅ **Total calculations** with real-time updates
- ✅ **Professional formatting** with proper currency display

## 🚀 **Benefits**

### **For Users:**
- ✅ **Transparent pricing** - see exact cost for each ticket type
- ✅ **Immediate feedback** - prices update as selections change
- ✅ **Flexible options** - choose appropriate ticket type for each passenger
- ✅ **Clear summary** - understand total cost breakdown

### **For Business:**
- ✅ **Revenue optimization** - different pricing for different passenger types
- ✅ **Accurate billing** - correct prices sent to payment system
- ✅ **Flexible pricing** - can adjust multipliers or use API-specific prices
- ✅ **Professional presentation** - clear, organized pricing display

## 🎯 **Current Status**

**🎉 DYNAMIC TICKET PRICING FULLY OPERATIONAL**

- ✅ **Prices vary by ticket type** - Adult, Child, Infant, Senior, Student
- ✅ **Real-time price updates** when ticket type changes
- ✅ **Enhanced summary display** showing ticket types and prices
- ✅ **Proper form integration** with hidden field updates
- ✅ **Currency formatting** with VND display
- ✅ **Default Adult selection** for immediate pricing
- ✅ **API integration ready** for exact pricing when available
- ✅ **Fallback multipliers** ensure pricing always works

## 🚀 **How to Test**

### **1. Select Seats**
- Choose seats on the seat map
- **Notice Adult ticket type selected by default**
- **See adult pricing in summary**

### **2. Change Ticket Types**
- Change ticket type dropdown for any passenger
- **Watch price update immediately in summary**
- **See ticket type label under seat class**

### **3. Multiple Passengers**
- Select multiple seats with different ticket types
- **Each passenger can have different pricing**
- **Total updates automatically**

### **4. Round Trip**
- Test with round trip bookings
- **Both outbound and return pricing work independently**
- **Separate totals and grand total calculation**

**🎉 Ticket prices now dynamically adjust based on passenger type with real-time updates and transparent pricing display!**
