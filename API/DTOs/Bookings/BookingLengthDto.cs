﻿namespace API.DTOs.Bookings
{
    public class BookingLengthDto
    {
        public Guid RoomGuid { get; set; }
        public string RoomName { get; set; }
        public string BookingLength { get; set; }
    }
}
