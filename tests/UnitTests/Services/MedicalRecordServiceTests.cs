using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.MedicalRecord;
using Hospital.Application.Exceptions;
using Hospital.Application.Mappings;
using Hospital.Application.Services;
using Hospital.Application.Validations;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Hospital.Shared.Queries;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnitTests.Helpers;
using Xunit;
using FluentAssertions;

namespace UnitTests.Services
{
    public class MedicalRecordServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMedicalRecordRepository> _mockMedicalRecordRepository;
        private readonly Mock<IPatientRepository> _mockPatientRepository;
        private readonly Mock<IDoctorRepository> _mockDoctorRepository;
        private readonly Mock<IAppointmentRepository> _mockAppointmentRepository;
        private readonly IMapper _mapper;
        private readonly MedicalRecordService _sut;

        public MedicalRecordServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMedicalRecordRepository = new Mock<IMedicalRecordRepository>();
            _mockPatientRepository = new Mock<IPatientRepository>();
            _mockDoctorRepository = new Mock<IDoctorRepository>();
            _mockAppointmentRepository = new Mock<IAppointmentRepository>();

            _mockUnitOfWork.Setup(u => u.MedicalRecords).Returns(_mockMedicalRecordRepository.Object);
            _mockUnitOfWork.Setup(u => u.Patients).Returns(_mockPatientRepository.Object);
            _mockUnitOfWork.Setup(u => u.Doctors).Returns(_mockDoctorRepository.Object);
            _mockUnitOfWork.Setup(u => u.Appointments).Returns(_mockAppointmentRepository.Object);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MedicalRecordProfile>();
                cfg.AddProfile<PatientProfile>();
                cfg.AddProfile<DoctorProfile>();
                cfg.AddProfile<DepartmentProfile>();
                cfg.AddProfile<AppointmentProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            IValidator<CreateMedicalRecordDto> createValidator = new CreateMedicalRecordDtoValidator();
            IValidator<UpdateMedicalRecordDto> updateValidator = new UpdateMedicalRecordDtoValidator();

            _sut = new MedicalRecordService(
                _mockUnitOfWork.Object,
                _mapper,
                createValidator,
                updateValidator);
        }

        [Fact]
        public async Task GetPagedAsync_WhenRecordsExist_ReturnsPagedResponse()
        {
            // ARRANGE
            var record = TestDataBuilder.CreateMedicalRecord();
            var list = new List<MedicalRecord> { record };
            var queryParams = new MedicalRecordQueryParams { PageNumber = 1, PageSize = 10 };

            _mockMedicalRecordRepository
                .Setup(r => r.GetPagedAsync(queryParams))
                .ReturnsAsync((list, 1));

            // ACT
            var result = await _sut.GetPagedAsync(queryParams);

            // ASSERT
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items[0].Diagnosis.Should().Be(record.Diagnosis);
            result.Items[0].PatientName.Should().Be($"{record.Patient.FirstName} {record.Patient.LastName}");
            result.Items[0].DoctorName.Should().Be($"Dr. {record.Doctor.FirstName} {record.Doctor.LastName}");
        }

        [Fact]
        public async Task GetMedicalRecordByIdAsync_WhenExists_ReturnsDto()
        {
            // ARRANGE
            var record = TestDataBuilder.CreateMedicalRecord();
            _mockMedicalRecordRepository
                .Setup(r => r.GetByIdWithDetailsAsync(record.Id))
                .ReturnsAsync(record);

            // ACT
            var result = await _sut.GetMedicalRecordByIdAsync(record.Id);

            // ASSERT
            result.Should().NotBeNull();
            result.Id.Should().Be(record.Id);
            result.Diagnosis.Should().Be(record.Diagnosis);
            result.PatientName.Should().Be($"{record.Patient.FirstName} {record.Patient.LastName}");
        }

        [Fact]
        public async Task GetMedicalRecordByIdAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            _mockMedicalRecordRepository
                .Setup(r => r.GetByIdWithDetailsAsync(id))
                .ReturnsAsync((MedicalRecord?)null);

            // ACT
            var act = async () => await _sut.GetMedicalRecordByIdAsync(id);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateMedicalRecordAsync_WhenValid_ReturnsCreatedDto()
        {
            // ARRANGE
            var patient = TestDataBuilder.CreatePatient();
            var doctor = TestDataBuilder.CreateDoctor();
            var dto = TestDataBuilder.CreateMedicalRecordDto(patient.Id, doctor.Id);

            _mockPatientRepository.Setup(r => r.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
            _mockDoctorRepository.Setup(r => r.GetByIdAsync(doctor.Id)).ReturnsAsync(doctor);

            var createdEntity = TestDataBuilder.CreateMedicalRecord(patientId: patient.Id, doctorId: doctor.Id);
            _mockMedicalRecordRepository
                .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(createdEntity);

            // ACT
            var result = await _sut.CreateMedicalRecordAsync(dto);

            // ASSERT
            result.Should().NotBeNull();
            result.PatientId.Should().Be(patient.Id);
            result.DoctorId.Should().Be(doctor.Id);
            _mockMedicalRecordRepository.Verify(r => r.AddAsync(It.IsAny<MedicalRecord>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task CreateMedicalRecordAsync_WhenPatientNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var dto = TestDataBuilder.CreateMedicalRecordDto();
            _mockPatientRepository.Setup(r => r.GetByIdAsync(dto.PatientId)).ReturnsAsync((Patient?)null);

            // ACT
            var act = async () => await _sut.CreateMedicalRecordAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateMedicalRecordAsync_WhenDoctorNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var patient = TestDataBuilder.CreatePatient();
            var dto = TestDataBuilder.CreateMedicalRecordDto(patient.Id);

            _mockPatientRepository.Setup(r => r.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
            _mockDoctorRepository.Setup(r => r.GetByIdAsync(dto.DoctorId)).ReturnsAsync((Doctor?)null);

            // ACT
            var act = async () => await _sut.CreateMedicalRecordAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateMedicalRecordAsync_WhenAppointmentNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var patient = TestDataBuilder.CreatePatient();
            var doctor = TestDataBuilder.CreateDoctor();
            var appointmentId = Guid.NewGuid();
            var dto = TestDataBuilder.CreateMedicalRecordDto(patient.Id, doctor.Id, appointmentId);

            _mockPatientRepository.Setup(r => r.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
            _mockDoctorRepository.Setup(r => r.GetByIdAsync(doctor.Id)).ReturnsAsync(doctor);
            _mockAppointmentRepository.Setup(r => r.GetByIdAsync(appointmentId)).ReturnsAsync((Appointment?)null);

            // ACT
            var act = async () => await _sut.CreateMedicalRecordAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateMedicalRecordAsync_WhenValidationFails_ThrowsValidationException()
        {
            // ARRANGE
            var dto = TestDataBuilder.CreateMedicalRecordDto();
            dto.Diagnosis = ""; // required

            // ACT
            var act = async () => await _sut.CreateMedicalRecordAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        [Fact]
        public async Task UpdateMedicalRecordAsync_WhenValid_UpdatesRecord()
        {
            // ARRANGE
            var record = TestDataBuilder.CreateMedicalRecord();
            var updateDto = TestDataBuilder.UpdateMedicalRecordDto(record.Id);

            _mockMedicalRecordRepository.Setup(r => r.GetByIdAsync(record.Id)).ReturnsAsync(record);

            // ACT
            await _sut.UpdateMedicalRecordAsync(updateDto);

            // ASSERT
            record.Diagnosis.Should().Be(updateDto.Diagnosis);
            _mockMedicalRecordRepository.Verify(r => r.UpdateAsync(record), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateMedicalRecordAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var updateDto = TestDataBuilder.UpdateMedicalRecordDto();
            _mockMedicalRecordRepository.Setup(r => r.GetByIdAsync(updateDto.Id)).ReturnsAsync((MedicalRecord?)null);

            // ACT
            var act = async () => await _sut.UpdateMedicalRecordAsync(updateDto);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateMedicalRecordAsync_WhenValidationFails_ThrowsValidationException()
        {
            // ARRANGE
            var updateDto = TestDataBuilder.UpdateMedicalRecordDto();
            updateDto.Diagnosis = ""; // invalid

            // ACT
            var act = async () => await _sut.UpdateMedicalRecordAsync(updateDto);

            // ASSERT
            await act.Should().ThrowAsync<Hospital.Application.Exceptions.ValidationException>();
        }

        [Fact]
        public async Task DeleteMedicalRecordAsync_WhenExists_DeletesRecord()
        {
            // ARRANGE
            var record = TestDataBuilder.CreateMedicalRecord();
            _mockMedicalRecordRepository.Setup(r => r.GetByIdAsync(record.Id)).ReturnsAsync(record);

            // ACT
            await _sut.DeleteMedicalRecordAsync(record.Id);

            // ASSERT
            _mockMedicalRecordRepository.Verify(r => r.DeleteAsync(record), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteMedicalRecordAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            _mockMedicalRecordRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((MedicalRecord?)null);

            // ACT
            var act = async () => await _sut.DeleteMedicalRecordAsync(id);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
