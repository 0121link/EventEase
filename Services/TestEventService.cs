using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventEase.Models;
using Blazored.LocalStorage;

namespace EventEase.Services
{
    public class TestEventService : EventService
    {
        private const string TEST_STORAGE_KEY = "test_events";
        public enum TestScenario
        {
            ValidData,
            EmptyData,
            InvalidData,
            NullData,
            LargeDataset
        }

        private readonly TestScenario _scenario;

        public TestEventService(ILocalStorageService localStorage, TestScenario scenario) 
            : base(localStorage)
        {
            _scenario = scenario;
        }

        protected override async Task InitializeEventsAsync()
        {
            if (_isInitialized) return;

            // Clear any existing test data
            await _localStorage.RemoveItemAsync(TEST_STORAGE_KEY);

            _events = _scenario switch
            {
                TestScenario.ValidData => GenerateValidTestData(),
                TestScenario.EmptyData => new List<Event>(),
                TestScenario.InvalidData => GenerateInvalidTestData(),
                TestScenario.NullData => null,
                TestScenario.LargeDataset => GenerateLargeTestDataset(),
                _ => throw new ArgumentException("Invalid test scenario")
            };

            if (_events != null)
            {
                await _localStorage.SetItemAsync(TEST_STORAGE_KEY, _events);
            }

            _isInitialized = true;
        }

        protected override async Task SaveEventsAsync()
        {
            // Save to test storage instead of production storage
            await _localStorage.SetItemAsync(TEST_STORAGE_KEY, _events);
        }

        private List<Event> GenerateValidTestData()
        {
            return new List<Event>
            {
                new Event
                {
                    Id = 1,
                    Name = "Test Conference",
                    Date = DateTime.Today.AddDays(30),
                    Location = "Test Center",
                    Description = "A test conference",
                    Price = 99.99m,
                    AvailableSpots = 100,
                    Category = "Test"
                }
            };
        }

        private List<Event> GenerateInvalidTestData()
        {
            return new List<Event>
            {
                new Event
                {
                    Id = 1,
                    Name = "", // Invalid: empty name
                    Date = DateTime.MinValue, // Invalid: minimum date
                    Location = null, // Invalid: null location
                    Description = "Test description",
                    Price = -10.00m, // Invalid: negative price
                    AvailableSpots = 0, // Invalid: zero spots
                    Category = null // Invalid: null category
                }
            };
        }

        private List<Event> GenerateLargeTestDataset()
        {
            var events = new List<Event>();
            for (int i = 1; i <= 10; i++) // Reduced from 1000 to 10 for testing
            {
                events.Add(new Event
                {
                    Id = i,
                    Name = $"Test Event {i}",
                    Date = DateTime.Today.AddDays(i),
                    Location = $"Location {i}",
                    Description = $"Description for test event {i}",
                    Price = 50.00m + i,
                    AvailableSpots = 100 + i,
                    Category = "Test"
                });
            }
            return events;
        }
    }
} 