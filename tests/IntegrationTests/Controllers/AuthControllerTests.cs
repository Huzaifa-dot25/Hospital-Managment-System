using Hospital.Shared.Constants;

namespace IntegrationTests.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // AUTHCONTROLLERTESTS
    //
    // Tests the full authentication pipeline end-to-end:
    //   HTTP request → Controller → AuthService → Identity → SQLite DB → Response
    //
    // IClassFixture<CustomWebApplicationFactory>:
    //   Tells xUnit to create ONE factory for ALL tests in this class.
    //   The factory (and its SQLite DB) is shared across all tests.
    //   xUnit injects it via the constructor.
    //
    // Collection ordering:
    //   We don't control the order tests run, so each test must be fully
    //   independent. We achieve this by generating unique emails via Guid.
    // ─────────────────────────────────────────────────────────────────────────
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;           // unauthenticated — for public auth endpoints
        private const string BaseUrl = "/api/v1/auth";

        public AuthControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;

            // Auth endpoints (register, login) are [AllowAnonymous] — no token needed.
            // We always use an unauthenticated client here.
            _client = factory.CreateUnauthenticatedClient();
        }

        // ════════════════════════════════════════════════════════════════════
        // REGISTER
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Register_WithValidData_Returns200WithToken()
        {
            // ARRANGE — build a valid RegisterDto with a unique email
            var dto = BuildRegisterDto();

            // ACT — POST to the real register endpoint
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/register", dto);

            // ASSERT — 200 OK (our API wraps everything in ApiResponse<T> and returns 200)
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Deserialize the response into our standard wrapper
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

            // Verify the wrapper says success
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();

            // Verify the response contains a real JWT token (not empty/null)
            body.Data.Should().NotBeNull();
            body.Data!.Token.Should().NotBeNullOrWhiteSpace();

            // Verify the token is a valid 3-part JWT (header.payload.signature)
            // A JWT always has exactly 2 dots separating 3 base64-encoded parts.
            body.Data.Token.Split('.').Should().HaveCount(3,
                because: "a JWT must have the format: header.payload.signature");

            // Verify the response email matches what we sent
            body.Data.Email.Should().Be(dto.Email);
        }

        [Fact]
        public async Task Register_WithMismatchedPasswords_Returns400()
        {
            // ARRANGE — ConfirmPassword doesn't match Password
            var dto = BuildRegisterDto();
            dto.ConfirmPassword = "DifferentPassword99!";

            // ACT
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/register", dto);

            // ASSERT — the validator or AuthService must reject this
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithWeakPassword_Returns400()
        {
            // ARRANGE — "password" has no uppercase, no digit, no special char
            // Identity's password policy (configured in InfrastructureServiceRegistration):
            //   RequireDigit, RequireLowercase, RequireUppercase, RequireNonAlphanumeric
            var dto = BuildRegisterDto();
            dto.Password = "weakpassword";
            dto.ConfirmPassword = "weakpassword";

            // ACT
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/register", dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithEmptyEmail_Returns400()
        {
            // ARRANGE
            var dto = BuildRegisterDto();
            dto.Email = "";

            // ACT
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/register", dto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_Returns400()
        {
            // ARRANGE — register once to populate the email
            var dto = BuildRegisterDto();
            await _client.PostAsJsonAsync($"{BaseUrl}/register", dto);

            // ACT — try to register AGAIN with the exact same email
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/register", dto);

            // ASSERT — Identity throws "Email already taken" → 400 Bad Request
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ════════════════════════════════════════════════════════════════════
        // LOGIN
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Login_WithValidCredentials_Returns200WithJwtAndRefreshToken()
        {
            // ARRANGE — register first so the user exists
            var dto = BuildRegisterDto();
            await _client.PostAsJsonAsync($"{BaseUrl}/register", dto);

            var loginDto = new LoginDto { Email = dto.Email, Password = dto.Password };

            // ACT
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/login", loginDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
            body!.Success.Should().BeTrue();
            body.Data!.Token.Should().NotBeNullOrWhiteSpace();

            // The refresh token must also be present — critical for the mobile app flow
            body.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();

            // UserId must be a non-empty Guid
            body.Data.UserId.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns400()
        {
            // ARRANGE — register user, then try to login with wrong password
            var dto = BuildRegisterDto();
            await _client.PostAsJsonAsync($"{BaseUrl}/register", dto);

            var loginDto = new LoginDto
            {
                Email = dto.Email,
                Password = "WrongPassword@99"   // different from registration password
            };

            // ACT
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/login", loginDto);

            // ASSERT — AuthService throws BadRequestException for invalid credentials
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_WithNonExistentEmail_Returns400()
        {
            // ARRANGE — email that was never registered
            var loginDto = new LoginDto
            {
                Email = $"ghost_{Guid.NewGuid():N}@hospital.test",
                Password = "Test@Password1!"
            };

            // ACT
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/login", loginDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_WithEmptyEmail_Returns400()
        {
            // ARRANGE
            var loginDto = new LoginDto { Email = "", Password = "Test@Password1!" };

            // ACT
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/login", loginDto);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ════════════════════════════════════════════════════════════════════
        // REFRESH TOKEN
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task RefreshToken_WithValidTokens_Returns200WithNewTokenPair()
        {
            // ARRANGE — register + login to get a real token pair
            var authData = await AuthHelper.RegisterAndLoginAsync(_factory);

            var refreshRequest = new
            {
                token = authData.Token,
                refreshToken = authData.RefreshToken
            };

            // ACT — call the refresh endpoint with the real tokens
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/refresh-token", refreshRequest);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
            body!.Success.Should().BeTrue();

            // The new token must be a different value — token rotation means the old
            // refresh token is revoked and a brand-new one is issued
            body.Data!.Token.Should().NotBeNullOrWhiteSpace();
            body.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
            body.Data.RefreshToken.Should().NotBe(authData.RefreshToken,
                because: "refresh token rotation must issue a new refresh token every time");
        }

        [Fact]
        public async Task RefreshToken_WithInvalidRefreshToken_Returns400()
        {
            // ARRANGE — get a real JWT but use a FAKE refresh token
            var authData = await AuthHelper.RegisterAndLoginAsync(_factory);

            var refreshRequest = new
            {
                token = authData.Token,
                refreshToken = "this-is-not-a-real-refresh-token"
            };

            // ACT
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/refresh-token", refreshRequest);

            // ASSERT — server must reject unknown refresh tokens
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ════════════════════════════════════════════════════════════════════
        // UNAUTHORIZED ACCESS
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task ProtectedEndpoint_WithNoToken_Returns401()
        {
            // ARRANGE — use an unauthenticated client (no Bearer header)
            // Hit any protected endpoint — Department is protected with [Authorize]
            // ACT
            var response = await _client.GetAsync("/api/v1/department");

            // ASSERT — JWT middleware must reject requests with no token
            // 401 Unauthorized = "I don't know who you are"
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ProtectedEndpoint_WithInvalidToken_Returns401()
        {
            // ARRANGE — attach a garbage token
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not.a.real.token");

            // ACT
            var response = await _client.GetAsync("/api/v1/department");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Clean up the header for other tests
            _client.DefaultRequestHeaders.Authorization = null;
        }

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds a valid RegisterDto with a guaranteed-unique email.
        /// All tests call this so they never collide on email uniqueness.
        /// </summary>
        private static RegisterDto BuildRegisterDto(string role = Roles.Patient) =>
            new()
            {
                FirstName = "Test",
                LastName = "User",
                Email = $"test_{Guid.NewGuid():N}@hospital.test",
                Password = "Test@Password1!",
                ConfirmPassword = "Test@Password1!",
                Role = role
            };
    }
}
