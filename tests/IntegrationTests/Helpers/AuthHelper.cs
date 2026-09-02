using System.Net.Http.Json;
using System.Net.Http.Headers;
using Hospital.Application.DTOs.Auth;
using Hospital.Shared.Models;

namespace IntegrationTests.Helpers
{
    // ─────────────────────────────────────────────────────────────────────────
    // AUTHHELPER
    //
    // WHAT IT DOES:
    //   Provides static helper methods to:
    //     1. Register a test user via POST /api/v1/auth/register
    //     2. Log in via POST /api/v1/auth/login
    //     3. Return an HttpClient that has the JWT pre-attached
    //
    // WHY A SEPARATE HELPER CLASS?
    //   Every controller test that needs auth has to:
    //     a) Register a user
    //     b) Log in
    //     c) Extract the token from the response
    //     d) Set Authorization: Bearer <token> on the client
    //
    //   Without this helper, that's 20+ lines repeated in every test method.
    //   With this helper, it's one line:
    //     var client = await AuthHelper.GetAuthenticatedClientAsync(factory, "Admin");
    //
    // WHY REGISTER + LOGIN INSTEAD OF FAKING THE TOKEN?
    //   We test the real flow. Faking a token would skip testing:
    //     - The registration endpoint itself
    //     - The login endpoint itself
    //     - The JWT middleware actually accepting valid tokens
    //   By going through the real flow, every successful authenticated request
    //   also validates that Auth works end-to-end.
    //
    // THREAD SAFETY NOTE:
    //   Each call creates a FRESH registration with a unique email (Guid suffix).
    //   This prevents "email already taken" errors when multiple tests run and
    //   avoids any shared state between tests.
    // ─────────────────────────────────────────────────────────────────────────
    public static class AuthHelper
    {
        // Base URL for all auth endpoints — matches [Route("api/v1/[controller]")]
        private const string AuthBaseUrl = "/api/v1/auth";

        /// <summary>
        /// Registers a test user with the given role, logs them in, and returns
        /// an HttpClient with the JWT Bearer token already set in the headers.
        ///
        /// Usage:
        ///   var client = await AuthHelper.GetAuthenticatedClientAsync(factory, "Admin");
        ///   var response = await client.GetAsync("/api/v1/department");
        /// </summary>
        /// <param name="factory">The shared CustomWebApplicationFactory instance.</param>
        /// <param name="role">The role to assign: "Admin", "Doctor", "Patient", etc.</param>
        /// <param name="emailPrefix">Optional prefix for the test email address.</param>
        public static async Task<HttpClient> GetAuthenticatedClientAsync(
            CustomWebApplicationFactory factory,
            string role,
            string emailPrefix = "testuser")
        {
            // Create a base client with no auth (for the login/register calls themselves)
            var client = factory.CreateUnauthenticatedClient();

            // Generate a unique email so parallel tests never collide on "email already taken"
            var uniqueEmail = $"{emailPrefix}_{Guid.NewGuid():N}@hospital.test";
            const string password = "Test@Password1!";

            // Step 1: Register the user
            // POST /api/v1/auth/register with the RegisterDto payload
            var registerPayload = new RegisterDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = uniqueEmail,
                Password = password,
                ConfirmPassword = password,
                Role = role
            };

            var registerResponse = await client.PostAsJsonAsync($"{AuthBaseUrl}/register", registerPayload);

            // If registration failed, throw immediately with a meaningful message.
            // This makes test failures point at the real cause (auth setup)
            // rather than a confusing "401 Unauthorized" on the actual test endpoint.
            if (!registerResponse.IsSuccessStatusCode)
            {
                var body = await registerResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"AuthHelper: Registration failed for role '{role}'. " +
                    $"Status: {registerResponse.StatusCode}. Body: {body}");
            }

            // Step 2: Log in to get the JWT token
            var loginPayload = new LoginDto
            {
                Email = uniqueEmail,
                Password = password
            };

            var loginResponse = await client.PostAsJsonAsync($"{AuthBaseUrl}/login", loginPayload);

            if (!loginResponse.IsSuccessStatusCode)
            {
                var body = await loginResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"AuthHelper: Login failed for role '{role}'. " +
                    $"Status: {loginResponse.StatusCode}. Body: {body}");
            }

            // Step 3: Extract the JWT token from the response
            // The response shape is ApiResponse<AuthResponseDto> — our standard wrapper.
            // System.Net.Http.Json's ReadFromJsonAsync deserializes it automatically.
            var loginResult = await loginResponse.Content
                .ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

            var token = loginResult?.Data?.Token
                ?? throw new InvalidOperationException(
                    $"AuthHelper: Token was null in login response for role '{role}'.");

            // Step 4: Attach the JWT Bearer token to the client's default headers.
            // DefaultRequestHeaders.Authorization applies to EVERY subsequent request
            // made with this client — no need to set it per-request.
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        /// <summary>
        /// Registers a user and returns just the token string.
        /// Useful when you need to test token-specific behaviour (e.g., RefreshToken).
        /// </summary>
        public static async Task<AuthResponseDto> RegisterAndLoginAsync(
            CustomWebApplicationFactory factory,
            string role = "Patient",
            string emailPrefix = "testuser")
        {
            var client = factory.CreateUnauthenticatedClient();

            var uniqueEmail = $"{emailPrefix}_{Guid.NewGuid():N}@hospital.test";
            const string password = "Test@Password1!";

            await client.PostAsJsonAsync($"{AuthBaseUrl}/register", new RegisterDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = uniqueEmail,
                Password = password,
                ConfirmPassword = password,
                Role = role
            });

            var loginResponse = await client.PostAsJsonAsync($"{AuthBaseUrl}/login", new LoginDto
            {
                Email = uniqueEmail,
                Password = password
            });

            var result = await loginResponse.Content
                .ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

            return result?.Data
                ?? throw new InvalidOperationException("AuthHelper: Login response had no data.");
        }
    }
}
