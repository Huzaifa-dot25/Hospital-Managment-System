using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Patient;
using Hospital.Application.Exceptions;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Threading.Tasks;

using AppValidationException = Hospital.Application.Exceptions.ValidationException;

namespace Hospital.Application.Services
{
    /// <summary>
    /// Implements all Patient use cases.
    ///
    /// This class sits in the Application layer — the "brain" of the system.
    /// It orchestrates:
    ///   - Validation (FluentValidation)
    ///   - Data access (via IUnitOfWork → repositories)
    ///   - Transformation (AutoMapper: Entity ↔ DTO)
    ///   - Business rules (e.g. "patient must exist before update")
    ///
    /// It does NOT know about:
    ///   - HTTP (that's the API layer)
    ///   - SQL (that's the Persistence layer)
    ///   - Database connection strings (that's Infrastructure)
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreatePatientDto> _createValidator;
        private readonly IValidator<UpdatePatientDto> _updateValidator;

        public PatientService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreatePatientDto> createValidator,
            IValidator<UpdatePatientDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        /// <inheritdoc />
        public async Task<PagedResponse<PatientDto>> GetPagedAsync(PatientQueryParams queryParams)
        {
            // Step 1: Ask the repository for the filtered, sorted, paginated data.
            // The repository returns a tuple: (list of entities, total count).
            // Total count = how many records match the filter BEFORE pagination.
            var (patients, totalCount) = await _unitOfWork.Patients.GetPagedAsync(queryParams);

            // Step 2: Map the entity list to a DTO list.
            // AutoMapper handles the property name conversion automatically.
            var patientDtos = _mapper.Map<System.Collections.Generic.List<PatientDto>>(patients);

            // Step 3: Wrap in PagedResponse with metadata.
            // The frontend needs TotalCount/TotalPages to render the page selector.
            return PagedResponse<PatientDto>.Create(
                patientDtos,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize);
        }

        /// <inheritdoc />
        public async Task<PatientDto> GetPatientByIdAsync(Guid id)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);
            if (patient == null)
                throw new NotFoundException(nameof(Patient), id);

            return _mapper.Map<PatientDto>(patient);
        }

        /// <inheritdoc />
        public async Task<PatientDto> CreatePatientAsync(CreatePatientDto createPatientDto)
        {
            // Validate input before touching the database.
            // If validation fails, FluentValidation returns a list of errors.
            // We throw our custom AppValidationException which the middleware
            // catches and formats as HTTP 400 with field-level errors.
            var validationResult = await _createValidator.ValidateAsync(createPatientDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            // Map DTO → Entity. AutoMapper copies matching property names.
            var patient = _mapper.Map<Patient>(createPatientDto);

            // Add to the repository's change tracker (not saved to DB yet)
            await _unitOfWork.Patients.AddAsync(patient);

            // THIS is when the SQL INSERT runs.
            // SaveChangesAsync also sets CreatedDate/CreatedBy via DbContext.SaveChangesAsync override.
            await _unitOfWork.SaveChangesAsync();

            // Map Entity → DTO and return. The DTO now has the generated Id.
            return _mapper.Map<PatientDto>(patient);
        }

        /// <inheritdoc />
        public async Task UpdatePatientAsync(UpdatePatientDto updatePatientDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updatePatientDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var patient = await _unitOfWork.Patients.GetByIdAsync(updatePatientDto.Id);
            if (patient == null)
                throw new NotFoundException(nameof(Patient), updatePatientDto.Id);

            // Map DTO → existing entity (updates only the mapped properties,
            // leaving Id, CreatedDate, CreatedBy untouched)
            _mapper.Map(updatePatientDto, patient);

            await _unitOfWork.Patients.UpdateAsync(patient);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task DeletePatientAsync(Guid id)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);
            if (patient == null)
                throw new NotFoundException(nameof(Patient), id);

            // The DbContext.SaveChangesAsync override intercepts EntityState.Deleted
            // and converts it to a soft delete:
            //   patient.IsDeleted = true
            //   patient.DeletedDate = DateTime.UtcNow
            //   patient.DeletedBy = currentUser
            // The global query filter then hides this record from all future queries.
            await _unitOfWork.Patients.DeleteAsync(patient);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
