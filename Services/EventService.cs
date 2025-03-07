using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventEase.Models;
using Blazored.LocalStorage;

namespace EventEase.Services
{
    public class EventNotFoundException : Exception
    {
        public EventNotFoundException(int id) : base($"Event with ID {id} was not found.") { }
    }

    public class EventService
    {
        protected List<Event> _events;
        private int _nextId = 4;
        protected bool _isInitialized = false;
        protected readonly ILocalStorageService _localStorage;

        public EventService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected virtual async Task InitializeEventsAsync()
        {
            if (_isInitialized) return;

            // Try to load events from local storage
            _events = await _localStorage.GetItemAsync<List<Event>>("events");
            
            // If no events in storage, initialize with default events
            if (_events == null)
            {
                _events = new List<Event>
                {
                    new Event
                    {
                        Id = 1,
                        Name = "Tech Conference 2024",
                        Date = new DateTime(2024, 6, 15),
                        Location = "Convention Center",
                        Description = "Annual technology conference featuring the latest innovations",
                        Price = 299.99m,
                        AvailableSpots = 200,
                        Category = "Conference"
                    },
                    new Event
                    {
                        Id = 2,
                        Name = "Summer Music Festival",
                        Date = new DateTime(2024, 7, 20),
                        Location = "Central Park",
                        Description = "Three days of live music and entertainment",
                        Price = 149.99m,
                        AvailableSpots = 500,
                        Category = "Social"
                    },
                    new Event
                    {
                        Id = 3,
                        Name = "Business Networking Dinner",
                        Date = new DateTime(2024, 8, 5),
                        Location = "Grand Hotel",
                        Description = "Networking event for business professionals",
                        Price = 79.99m,
                        AvailableSpots = 100,
                        Category = "Networking"
                    }
                };
                await SaveEventsAsync();
            }
            _isInitialized = true;
        }

        protected virtual async Task SaveEventsAsync()
        {
            await _localStorage.SetItemAsync("events", _events);
        }

        public async Task<List<Event>> GetEventsAsync()
        {
            await InitializeEventsAsync();
            return _events;
        }

        public async Task<Event> GetEventByIdAsync(int id)
        {
            await InitializeEventsAsync();
            var evt = _events.FirstOrDefault(e => e.Id == id);
            if (evt == null)
            {
                throw new EventNotFoundException(id);
            }
            return evt;
        }

        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            if (newEvent == null)
            {
                throw new ArgumentNullException(nameof(newEvent));
            }

            await InitializeEventsAsync();
            newEvent.Id = _nextId++;
            _events.Add(newEvent);
            await SaveEventsAsync();
            return newEvent;
        }

        public async Task<Event> UpdateEventAsync(int id, Event updatedEvent)
        {
            if (updatedEvent == null)
            {
                throw new ArgumentNullException(nameof(updatedEvent));
            }

            await InitializeEventsAsync();
            var existingEvent = _events.FirstOrDefault(e => e.Id == id);
            if (existingEvent == null)
            {
                throw new EventNotFoundException(id);
            }

            existingEvent.Name = updatedEvent.Name;
            existingEvent.Date = updatedEvent.Date;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Price = updatedEvent.Price;
            existingEvent.AvailableSpots = updatedEvent.AvailableSpots;
            existingEvent.Category = updatedEvent.Category;

            await SaveEventsAsync();
            return existingEvent;
        }

        public async Task DeleteEventAsync(int id)
        {
            await InitializeEventsAsync();
            var evt = _events.FirstOrDefault(e => e.Id == id);
            if (evt == null)
            {
                throw new EventNotFoundException(id);
            }

            _events.Remove(evt);
            await SaveEventsAsync();
        }
    }
} 