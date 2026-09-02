using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hospital.Application.DTOs.Auth;
using Hospital.Shared.Models;

namespace IntegrationTests.Helpers
{
    // ─────────────────────────────────────────────────────────────────────────
    // AUTHHELPER
    //
    // Provides an HttpClient with a real JWT token pre-attached.
    //
    // DESIGN RULES:
    //   - Admin / SuperAdmin: these roles are seeded directly into the DB by
    //     CustomWebApplicationFactory (RegisterDtoValidator blocks self-signup
    //     for privileged roles — correct security behaviour). We log in with
    //     the pre-seeded credentials.
    //
    //   - All other roles (Doctor, Patient, Receptionist, etc.): we register a
    //     fresh unique user for each call, then log in. This guarantees test
    //     isolation — no shared users between tests.
    // ─────────────────────────────────────────────────────────────────────────
    public static class AuthHelper
    {
        private const string AuthBaseUrl = "/api/v1/auth";

        // Roles that cannot self-register — use the seeded account instead.
        private static readonly HashSet<string> SeededRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin", "SuperAdmin"
        };

        /// <summary>
        /// Returns an HttpClient with Authorization: Bearer set to a real JWT.
        /// </summary>
        public static async Task<HttpClient> GetAuthenticatedClientAsync(
            CustomWebApplicationFactory factory,
            string role,
            string emailPrefix = "testuser")
        {
            var client = factory.CreateUnauthenticatedClient();
            string email;
            string password;

            if (SeededRoles.Contains(role))
            {
                // Use the pre-seeded privileged account — no registration step needed.
                email    = role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)
                           ? CustomWebApplicationFactory.SuperAdminEmail
                           : CustomWebApplicationFactory.AdminEmail;
                password = CustomWebApplicationFactory.SeededPassword;
            }
            else
            {
                // Register a fresh unique user for this role.
                email    = $"{emailPrefix}_{Guid.NewGuid():N}@hospital.test";
                password = "Test@Password1!";

                var registerPayload = new RegisterDto
                {
                    FirstName       = "Test",
                    LastName        = "User",
                    Email           = email,
                    Password        = password,
                    ConfirmPassword = password,
                    Role            = role
                };

                var regResponse = await client.PostAsJsonAsync($"{AuthBaseUrl}/register", registerPayload);
                if (!regResponse.IsSuccessStatusCode)
                {
                    var body = await regResponse.Content.ReadAsStringAsync();
                    throw new InvalidOperationException(
                        $"AuthHelper: Registration failed for role '{role}'. " +
                        $"Status: {regResponse.StatusCode}. Body: {body}");
                }
            }

            // Log in to get the JWT
            var loginResponse = await client.PostAsJsonAsync($"{AuthBaseUrl}/login",
                new LoginDto { Email = email, Password = password });

            if (!loginResponse.IsSuccessStatusCode)
            {
                var body = await loginResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"AuthHelper: Login failed for role '{role}'. " +
                    $"Status: {loginResponse.StatusCode}. Body: {body}");
            }

            var loginResult = await loginResponse.Content
                .ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

            var token = loginResult?.Data?.Token
                ?? throw new InvalidOperationException(
                    $"AuthHelper: Token was null in login response for role '{role}'.");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        /// <summary>
        /// Registers a new user and returns the full AuthResponseDto (token + refresh token).
        /// Used by refresh-token tests that need access to both tokens.
        /// </summary>
        public static async Task<AuthResponseDto> RegisterAndLoginAsync(
            CustomWebApplicationFactory factory,
            string role = "Patient",
            string emailPrefix = "testuser")
        {
            var client   = factory.CreateUnauthenticatedClient();
            var email    = $"{emailPrefix}_{Guid.NewGuid():N}@hospital.test";
            const string password = "Test@Password1!";

            await client.PostAsJsonAsync($"{AuthBaseUrl}/register", new RegisterDto
            {
                FirstName       = "Test",
                LastName        = "User",
                Email           = email,
                Password        = password,
                ConfirmPassword = password,
                Role            = role
            });

            var loginResponse = await client.PostAsJsonAsync($"{AuthBaseUrl}/login",
                new LoginDto { Email = email, Password = password });

            var result = await loginResponse.Content
                .ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

            return result?.Data
                ?? throw new InvalidOperationException("AuthHelper: Login response had no data.");
        }
    }
}
