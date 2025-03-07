using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventEase.Models;
using Blazored.LocalStorage;
using System.Linq;

namespace EventEase.Services
{
    public class AttendanceService
    {
        private Dictionary<int, List<AttendanceRecord>> _attendanceRecords;
        private readonly UserSessionService _userSessionService;
        private readonly EventService _eventService;
        private readonly ILocalStorageService _localStorage;
        private const string STORAGE_KEY = "attendance_records";
        private bool _isInitialized = false;

        public AttendanceService(
            UserSessionService userSessionService, 
            EventService eventService,
            ILocalStorageService localStorage)
        {
            _userSessionService = userSessionService;
            _eventService = eventService;
            _localStorage = localStorage;
            _attendanceRecords = new Dictionary<int, List<AttendanceRecord>>();
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                var storedRecords = await _localStorage.GetItemAsync<Dictionary<int, List<AttendanceRecord>>>(STORAGE_KEY);
                if (storedRecords != null)
                {
                    _attendanceRecords = storedRecords;
                }
                else
                {
                    _attendanceRecords = new Dictionary<int, List<AttendanceRecord>>();
                    // Initialize records from user sessions
                    var session = await _userSessionService.GetCurrentSessionAsync();
                    if (session != null && session.RegisteredEventIds.Any())
                    {
                        foreach (var eventId in session.RegisteredEventIds)
                        {
                            if (!_attendanceRecords.ContainsKey(eventId))
                            {
                                _attendanceRecords[eventId] = new List<AttendanceRecord>();
                            }
                            
                            // Only add if not already registered
                            if (!_attendanceRecords[eventId].Any(r => r.UserId == session.UserId && r.Status == AttendanceStatus.Registered))
                            {
                                _attendanceRecords[eventId].Add(new AttendanceRecord
                                {
                                    EventId = eventId,
                                    UserId = session.UserId,
                                    RegistrationDate = DateTime.UtcNow,
                                    Status = AttendanceStatus.Registered
                                });
                            }
                        }
                        await SaveRecordsAsync();
                    }
                }
            }
            catch
            {
                _attendanceRecords = new Dictionary<int, List<AttendanceRecord>>();
            }

            _isInitialized = true;
        }

        private async Task SaveRecordsAsync()
        {
            await _localStorage.SetItemAsync(STORAGE_KEY, _attendanceRecords);
        }

        public async Task<bool> RegisterAttendanceAsync(int eventId, string userId)
        {
            await InitializeAsync();

            var session = await _userSessionService.GetCurrentSessionAsync();
            if (session == null)
            {
                throw new InvalidOperationException("User must be logged in to register attendance");
            }

            var evt = await _eventService.GetEventByIdAsync(eventId);
            if (evt == null)
            {
                throw new EventNotFoundException(eventId);
            }

            if (evt.AvailableSpots <= 0)
            {
                throw new InvalidOperationException("Event is full");
            }

            // Check if user is already registered
            if (_attendanceRecords.ContainsKey(eventId))
            {
                var existingRecord = _attendanceRecords[eventId]
                    .FirstOrDefault(r => r.UserId == userId && r.Status == AttendanceStatus.Registered);
                if (existingRecord != null)
                {
                    throw new InvalidOperationException("You are already registered for this event");
                }
            }

            if (!_attendanceRecords.ContainsKey(eventId))
            {
                _attendanceRecords[eventId] = new List<AttendanceRecord>();
            }

            var record = new AttendanceRecord
            {
                EventId = eventId,
                UserId = userId,
                RegistrationDate = DateTime.UtcNow,
                Status = AttendanceStatus.Registered
            };

            _attendanceRecords[eventId].Add(record);
            evt.AvailableSpots--;

            // Save both attendance records and event changes
            await SaveRecordsAsync();
            await _eventService.UpdateEventAsync(evt.Id, evt);

            // Update user's registered events
            if (!session.RegisteredEventIds.Contains(eventId))
            {
                session.RegisteredEventIds.Add(eventId);
                await _userSessionService.SetCurrentSessionAsync(session);
            }

            return true;
        }

        public async Task<bool> UnregisterAttendanceAsync(int eventId, string userId)
        {
            await InitializeAsync();

            var session = await _userSessionService.GetCurrentSessionAsync();
            if (session == null)
            {
                throw new InvalidOperationException("User must be logged in to unregister");
            }

            var evt = await _eventService.GetEventByIdAsync(eventId);
            if (evt == null)
            {
                throw new EventNotFoundException(eventId);
            }

            // If the event is in the user's registered events but not in attendance records,
            // create the attendance record first
            if (session.RegisteredEventIds.Contains(eventId))
            {
                if (!_attendanceRecords.ContainsKey(eventId))
                {
                    _attendanceRecords[eventId] = new List<AttendanceRecord>();
                }

                var existingRecord = _attendanceRecords[eventId].FirstOrDefault(r => r.UserId == userId);
                if (existingRecord == null)
                {
                    _attendanceRecords[eventId].Add(new AttendanceRecord
                    {
                        EventId = eventId,
                        UserId = userId,
                        RegistrationDate = DateTime.UtcNow,
                        Status = AttendanceStatus.Registered
                    });
                }
                else if (existingRecord.Status != AttendanceStatus.Registered)
                {
                    existingRecord.Status = AttendanceStatus.Registered;
                }
            }

            // Now try to find the registration to cancel
            if (!_attendanceRecords.ContainsKey(eventId))
            {
                throw new InvalidOperationException("No registration found for this event");
            }

            var record = _attendanceRecords[eventId].FirstOrDefault(r => r.UserId == userId && r.Status == AttendanceStatus.Registered);
            if (record == null)
            {
                throw new InvalidOperationException("No active registration found for this event");
            }

            // Update the record status
            record.Status = AttendanceStatus.Cancelled;
            
            // Increase available spots and save event changes
            evt.AvailableSpots++;
            await _eventService.UpdateEventAsync(evt.Id, evt);

            // Save attendance records
            await SaveRecordsAsync();

            // Remove from user's registered events
            session.RegisteredEventIds.Remove(eventId);
            await _userSessionService.SetCurrentSessionAsync(session);

            return true;
        }

        public async Task<List<AttendanceRecord>> GetUserAttendanceAsync(string userId)
        {
            await InitializeAsync();

            var records = new List<AttendanceRecord>();
            foreach (var eventRecords in _attendanceRecords.Values)
            {
                records.AddRange(eventRecords.Where(r => r.UserId == userId));
            }
            return records;
        }

        public async Task<List<AttendanceRecord>> GetEventAttendanceAsync(int eventId)
        {
            await InitializeAsync();

            if (_attendanceRecords.TryGetValue(eventId, out var records))
            {
                return records;
            }
            return new List<AttendanceRecord>();
        }
    }

    public class AttendanceRecord
    {
        public int EventId { get; set; }
        public string UserId { get; set; }
        public DateTime RegistrationDate { get; set; }
        public AttendanceStatus Status { get; set; }
    }

    public enum AttendanceStatus
    {
        Registered,
        CheckedIn,
        Cancelled
    }
} 