using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BusinessLogicLayer.Utilities.Hub
{
    public class CallHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private static readonly ConcurrentDictionary<int, string> _userConnections = new();
        private static readonly ConcurrentDictionary<string, string> _bookingRooms = new(); // BookingCode -> RoomName
        private static readonly ConcurrentDictionary<string, List<string>> _roomParticipants = new(); // RoomName -> List of ConnectionIds

        private readonly IHubContext<CallHub> _hubContext;

        public CallHub(IHubContext<CallHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null || httpContext.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                throw new Exception("User is not authenticated!");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new Exception("User ID not found in claims!");
            }

            var userId = int.Parse(userIdClaim.Value);
            _userConnections[userId] = Context.ConnectionId;

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null || httpContext.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                throw new Exception("User is not authenticated!");
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new Exception("User ID not found in claims!");
            }

            var userId = int.Parse(userIdClaim.Value);
            _userConnections.Remove(userId, out _);

            // Remove user from any rooms they might be in
            foreach (var room in _roomParticipants)
            {
                if (room.Value.Contains(Context.ConnectionId))
                {
                    room.Value.Remove(Context.ConnectionId);
                    if (room.Value.Count == 0)
                    {
                        _roomParticipants.TryRemove(room.Key, out _);
                        _bookingRooms.TryRemove(_bookingRooms.FirstOrDefault(x => x.Value == room.Key).Key, out _);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Create or join a booking call room
        public async Task JoinBookingCall(string bookingCode)
        {
            if (string.IsNullOrEmpty(bookingCode))
            {
                throw new ArgumentException("Booking code cannot be empty");
            }

            // Check if room already exists for this booking
            if (!_bookingRooms.TryGetValue(bookingCode, out var roomName))
            {
                roomName = $"booking_call_{bookingCode}";
                _bookingRooms.TryAdd(bookingCode, roomName);
                _roomParticipants.TryAdd(roomName, new List<string>());
            }

            // Add user to the room
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            if (_roomParticipants.TryGetValue(roomName, out var participants))
            {
                participants.Add(Context.ConnectionId);
            }

            await Clients.Group(roomName).SendAsync("UserJoined", Context.ConnectionId);
        }

        // Leave a booking call room
        public async Task LeaveBookingCall(string bookingCode)
        {
            if (_bookingRooms.TryGetValue(bookingCode, out var roomName))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

                if (_roomParticipants.TryGetValue(roomName, out var participants))
                {
                    participants.Remove(Context.ConnectionId);

                    // Clean up empty rooms
                    if (participants.Count == 0)
                    {
                        _roomParticipants.TryRemove(roomName, out _);
                        _bookingRooms.TryRemove(bookingCode, out _);
                    }
                }

                await Clients.Group(roomName).SendAsync("UserLeft", Context.ConnectionId);
            }
        }

        // WebRTC signaling methods for booking calls
        public async Task SendOfferToBooking(string bookingCode, string offer)
        {
            if (_bookingRooms.TryGetValue(bookingCode, out var roomName))
            {
                await Clients.OthersInGroup(roomName).SendAsync("ReceiveOffer", Context.ConnectionId, offer);
            }
        }

        public async Task SendAnswerToBooking(string bookingCode, string answer)
        {
            if (_bookingRooms.TryGetValue(bookingCode, out var roomName))
            {
                await Clients.OthersInGroup(roomName).SendAsync("ReceiveAnswer", Context.ConnectionId, answer);
            }
        }

        public async Task SendIceCandidateToBooking(string bookingCode, string candidate)
        {
            if (_bookingRooms.TryGetValue(bookingCode, out var roomName))
            {
                await Clients.OthersInGroup(roomName).SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidate);
            }
        }
    }
}
