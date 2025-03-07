using System;
using System.Threading.Tasks;
using EventEase.Models;
using Blazored.LocalStorage;

namespace EventEase.Services
{
    public class UserSessionService
    {
        private UserSession _currentSession;
        private readonly ILocalStorageService _localStorage;

        public UserSessionService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<UserSession> GetCurrentSessionAsync()
        {
            if (_currentSession == null)
            {
                _currentSession = await _localStorage.GetItemAsync<UserSession>("userSession");
            }
            return _currentSession;
        }

        public async Task SetCurrentSessionAsync(UserSession session)
        {
            _currentSession = session;
            await _localStorage.SetItemAsync("userSession", session);
        }

        public async Task ClearSessionAsync()
        {
            _currentSession = null;
            await _localStorage.RemoveItemAsync("userSession");
        }

        public bool IsAuthenticated => _currentSession != null;
    }

    public class UserSession
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public DateTime LastActivity { get; set; }
        public List<int> RegisteredEventIds { get; set; } = new();
    }
} 