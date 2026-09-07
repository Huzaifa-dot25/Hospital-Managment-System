using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Pharmacy;
using Hospital.Application.Exceptions;
using Hospital.Application.Mappings;
using Hospital.Application.Services;
using Hospital.Application.Validations;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
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
    public class PrescriptionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IPrescriptionRepository> _mockPrescriptionRepository;
        private readonly Mock<IPatientRepository> _mockPatientRepository;
        private readonly Mock<IDoctorRepository> _mockDoctorRepository;
        private readonly Mock<IMedicalRecordRepository> _mockMedicalRecordRepository;
        private readonly Mock<IMedicationRepository> _mockMedicationRepository;
        private readonly IMapper _mapper;
        private readonly PrescriptionService _sut;

        public PrescriptionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockPrescriptionRepository = new Mock<IPrescriptionRepository>();
            _mockPatientRepository = new Mock<IPatientRepository>();
            _mockDoctorRepository = new Mock<IDoctorRepository>();
            _mockMedicalRecordRepository = new Mock<IMedicalRecordRepository>();
            _mockMedicationRepository = new Mock<IMedicationRepository>();

            _mockUnitOfWork.Setup(u => u.Prescriptions).Returns(_mockPrescriptionRepository.Object);
            _mockUnitOfWork.Setup(u => u.Patients).Returns(_mockPatientRepository.Object);
            _mockUnitOfWork.Setup(u => u.Doctors).Returns(_mockDoctorRepository.Object);
            _mockUnitOfWork.Setup(u => u.MedicalRecords).Returns(_mockMedicalRecordRepository.Object);
            _mockUnitOfWork.Setup(u => u.Medications).Returns(_mockMedicationRepository.Object);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<PharmacyProfile>();
                cfg.AddProfile<PatientProfile>();
                cfg.AddProfile<DoctorProfile>();
                cfg.AddProfile<DepartmentProfile>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            IValidator<CreatePrescriptionDto> createValidator = new CreatePrescriptionDtoValidator();

            _sut = new PrescriptionService(
                _mockUnitOfWork.Object,
                _mapper,
                createValidator);
        }

        [Fact]
        public async Task GetPagedAsync_WhenPrescriptionsExist_ReturnsPagedResponse()
        {
            // ARRANGE
            var prescription = TestDataBuilder.CreatePrescription();
            var list = new List<Prescription> { prescription };
            var queryParams = new PrescriptionQueryParams { PageNumber = 1, PageSize = 10 };

            _mockPrescriptionRepository
                .Setup(r => r.GetPagedAsync(queryParams))
                .ReturnsAsync((list, 1));

            // ACT
            var result = await _sut.GetPagedAsync(queryParams);

            // ASSERT
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items[0].PatientName.Should().Be($"{prescription.Patient.FirstName} {prescription.Patient.LastName}");
            result.Items[0].DoctorName.Should().Be($"Dr. {prescription.Doctor.FirstName} {prescription.Doctor.LastName}");
        }

        [Fact]
        public async Task GetPrescriptionByIdAsync_WhenExists_ReturnsDto()
        {
            // ARRANGE
            var prescription = TestDataBuilder.CreatePrescription();
            _mockPrescriptionRepository
                .Setup(r => r.GetByIdWithDetailsAsync(prescription.Id))
                .ReturnsAsync(prescription);

            // ACT
            var result = await _sut.GetPrescriptionByIdAsync(prescription.Id);

            // ASSERT
            result.Should().NotBeNull();
            result.Id.Should().Be(prescription.Id);
            result.PatientName.Should().Be($"{prescription.Patient.FirstName} {prescription.Patient.LastName}");
            result.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetPrescriptionByIdAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            _mockPrescriptionRepository
                .Setup(r => r.GetByIdWithDetailsAsync(id))
                .ReturnsAsync((Prescription?)null);

            // ACT
            var act = async () => await _sut.GetPrescriptionByIdAsync(id);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreatePrescriptionAsync_WhenValid_CreatesAndReturnsDto()
        {
            // ARRANGE
            var patient = TestDataBuilder.CreatePatient();
            var doctor = TestDataBuilder.CreateDoctor();
            var med = TestDataBuilder.CreateMedication();
            var dto = TestDataBuilder.CreatePrescriptionDto(patient.Id, doctor.Id, med.Id);

            _mockPatientRepository.Setup(r => r.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
            _mockDoctorRepository.Setup(r => r.GetByIdAsync(doctor.Id)).ReturnsAsync(doctor);
            _mockMedicationRepository.Setup(r => r.GetByIdAsync(med.Id)).ReturnsAsync(med);

            var createdEntity = TestDataBuilder.CreatePrescription(patientId: patient.Id, doctorId: doctor.Id, medication: med);
            _mockPrescriptionRepository
                .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(createdEntity);

            // ACT
            var result = await _sut.CreatePrescriptionAsync(dto);

            // ASSERT
            result.Should().NotBeNull();
            result.PatientId.Should().Be(patient.Id);
            result.DoctorId.Should().Be(doctor.Id);
            _mockPrescriptionRepository.Verify(r => r.AddAsync(It.IsAny<Prescription>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task CreatePrescriptionAsync_WhenPatientNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var dto = TestDataBuilder.CreatePrescriptionDto();
            _mockPatientRepository.Setup(r => r.GetByIdAsync(dto.PatientId)).ReturnsAsync((Patient?)null);

            // ACT
            var act = async () => await _sut.CreatePrescriptionAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreatePrescriptionAsync_WhenMedicationNotFound_ThrowsNotFoundException()
        {
            // ARRANGE
            var patient = TestDataBuilder.CreatePatient();
            var doctor = TestDataBuilder.CreateDoctor();
            var dto = TestDataBuilder.CreatePrescriptionDto(patient.Id, doctor.Id);

            _mockPatientRepository.Setup(r => r.GetByIdAsync(patient.Id)).ReturnsAsync(patient);
            _mockDoctorRepository.Setup(r => r.GetByIdAsync(doctor.Id)).ReturnsAsync(doctor);
            _mockMedicationRepository.Setup(r => r.GetByIdAsync(dto.Items[0].MedicationId)).ReturnsAsync((Medication?)null);

            // ACT
            var act = async () => await _sut.CreatePrescriptionAsync(dto);

            // ASSERT
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DispensePrescriptionAsync_WhenPendingAndInStock_DeductsStockAndDispenses()
        {
            // ARRANGE
            var med = TestDataBuilder.CreateMedication(stockQuantity: 50);
            var prescription = TestDataBuilder.CreatePrescription(medication: med, quantity: 20);

            _mockPrescriptionRepository
                .Setup(r => r.GetByIdWithItemsAsync(prescription.Id))
                .ReturnsAsync(prescription);
            _mockPrescriptionRepository
                .Setup(r => r.GetByIdWithDetailsAsync(prescription.Id))
                .ReturnsAsync(prescription);
            _mockMedicationRepository
                .Setup(r => r.GetByIdAsync(med.Id))
                .ReturnsAsync(med);

            var dispenseDto = new DispensePrescriptionDto
            {
                PharmacistId = Guid.NewGuid(),
                DispensingNotes = "Dispensed 20 capsules"
            };

            // ACT
            var result = await _sut.DispensePrescriptionAsync(prescription.Id, dispenseDto);

            // ASSERT
            result.Should().NotBeNull();
            result.Status.Should().Be(PrescriptionStatus.Dispensed);
            med.StockQuantity.Should().Be(30); // 50 - 20
            _mockMedicationRepository.Verify(r => r.UpdateAsync(med), Times.Once);
            _mockPrescriptionRepository.Verify(r => r.UpdateAsync(prescription), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DispensePrescriptionAsync_WhenInsufficientStock_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var med = TestDataBuilder.CreateMedication(stockQuantity: 10);
            var prescription = TestDataBuilder.CreatePrescription(medication: med, quantity: 25); // needs 25, only 10

            _mockPrescriptionRepository
                .Setup(r => r.GetByIdWithItemsAsync(prescription.Id))
                .ReturnsAsync(prescription);
            _mockMedicationRepository
                .Setup(r => r.GetByIdAsync(med.Id))
                .ReturnsAsync(med);

            var dispenseDto = new DispensePrescriptionDto { PharmacistId = Guid.NewGuid() };

            // ACT
            var act = async () => await _sut.DispensePrescriptionAsync(prescription.Id, dispenseDto);

            // ASSERT
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Insufficient stock*");
        }

        [Fact]
        public async Task DispensePrescriptionAsync_WhenAlreadyDispensed_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var prescription = TestDataBuilder.CreatePrescription();
            prescription.Status = PrescriptionStatus.Dispensed;

            _mockPrescriptionRepository
                .Setup(r => r.GetByIdWithItemsAsync(prescription.Id))
                .ReturnsAsync(prescription);

            var dispenseDto = new DispensePrescriptionDto { PharmacistId = Guid.NewGuid() };

            // ACT
            var act = async () => await _sut.DispensePrescriptionAsync(prescription.Id, dispenseDto);

            // ASSERT
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*cannot be dispensed*");
        }

        [Fact]
        public async Task CancelPrescriptionAsync_WhenPending_CancelsSuccessfully()
        {
            // ARRANGE
            var prescription = TestDataBuilder.CreatePrescription();
            _mockPrescriptionRepository.Setup(r => r.GetByIdAsync(prescription.Id)).ReturnsAsync(prescription);

            // ACT
            await _sut.CancelPrescriptionAsync(prescription.Id);

            // ASSERT
            prescription.Status.Should().Be(PrescriptionStatus.Cancelled);
            _mockPrescriptionRepository.Verify(r => r.UpdateAsync(prescription), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task CancelPrescriptionAsync_WhenAlreadyDispensed_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var prescription = TestDataBuilder.CreatePrescription();
            prescription.Status = PrescriptionStatus.Dispensed;
            _mockPrescriptionRepository.Setup(r => r.GetByIdAsync(prescription.Id)).ReturnsAsync(prescription);

            // ACT
            var act = async () => await _sut.CancelPrescriptionAsync(prescription.Id);

            // ASSERT
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cannot cancel an already dispensed prescription*");
        }
    }
}
