using System.Collections.Generic;

namespace Hospital.Shared.Models
{
    /// <summary>
    /// A standardized response wrapper for ALL API endpoints.
    /// 
    /// Why use this?
    /// Without a wrapper, every endpoint returns a different shape:
    ///   GET /patients → [ {...}, {...} ]
    ///   GET /patients/123 → { "id": "..." }
    ///   POST /patients → 201 Created with body or empty
    /// 
    /// That forces the frontend to handle every endpoint differently.
    /// 
    /// With ApiResponse<T>, every endpoint always returns:
    /// {
    ///   "success": true/false,
    ///   "message": "Human-readable message",
    ///   "data": <the actual payload>,
    ///   "errors": ["error1", "error2"]  ← only on failure
    /// }
    /// 
    /// Now the frontend just checks "if (response.success)" everywhere.
    /// This is the standard in production APIs.
    /// </summary>
    /// <typeparam name="T">The type of the data payload (e.g., PatientDto, IEnumerable&lt;DoctorDto&gt;)</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// True if the request was handled successfully, false otherwise.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// A human-readable message describing the result.
        /// E.g. "Patient created successfully", "Validation failed"
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The actual response data. Will be null if the request failed.
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// A list of error messages. Only populated when Success = false.
        /// Useful for validation errors where you want to show multiple field errors.
        /// </summary>
        public List<string>? Errors { get; set; }

        // ─────────────────────────────────────────────────────────────
        // Static factory methods — these are helper methods that make it
        // easy to create a response without having to set properties manually.
        // 
        // Instead of:
        //   var r = new ApiResponse<PatientDto>();
        //   r.Success = true; r.Data = patient; r.Message = "Found";
        //   return Ok(r);
        // 
        // You write:
        //   return Ok(ApiResponse<PatientDto>.SuccessResult(patient, "Found"));
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a successful response with data and a message.
        /// </summary>
        public static ApiResponse<T> SuccessResult(T data, string message = "Operation successful")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Creates a failed response with a single error message.
        /// </summary>
        public static ApiResponse<T> FailResult(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = new List<string> { message }
            };
        }

        /// <summary>
        /// Creates a failed response with multiple error messages.
        /// Useful for FluentValidation where multiple fields can fail at once.
        /// </summary>
        public static ApiResponse<T> FailResult(string message, List<string> errors)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Non-generic version for responses that have no data payload.
    /// E.g. DELETE or UPDATE operations that return 204 No Content but in our wrapper format.
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse SuccessResult(string message = "Operation successful")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        public new static ApiResponse FailResult(string message)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = new List<string> { message }
            };
        }
    }
}
