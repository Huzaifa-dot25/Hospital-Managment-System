using AutoMapper;
using FluentValidation;
using Hospital.Application.DTOs.Laboratory;
using Hospital.Application.Exceptions;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Repositories;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AppValidationException = Hospital.Application.Exceptions.ValidationException;

namespace Hospital.Application.Services
{
    public class LabTestService : ILabTestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateLabTestDto> _createValidator;
        private readonly IValidator<UpdateLabTestDto> _updateValidator;

        public LabTestService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateLabTestDto> createValidator,
            IValidator<UpdateLabTestDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<PagedResponse<LabTestDto>> GetPagedAsync(LabTestQueryParams queryParams)
        {
            var (items, totalCount) = await _unitOfWork.LabTests.GetPagedAsync(queryParams);
            var dtos = _mapper.Map<List<LabTestDto>>(items);

            return PagedResponse<LabTestDto>.Create(
                dtos,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize);
        }

        public async Task<LabTestDto> GetByIdAsync(Guid id)
        {
            var test = await _unitOfWork.LabTests.GetByIdAsync(id);
            if (test == null)
                throw new NotFoundException(nameof(LabTest), id);

            return _mapper.Map<LabTestDto>(test);
        }

        public async Task<LabTestDto> CreateAsync(CreateLabTestDto createDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var codeExists = await _unitOfWork.LabTests.ExistsByCodeAsync(createDto.Code);
            if (codeExists)
                throw new BadRequestException($"A lab test with code '{createDto.Code}' already exists.");

            var entity = _mapper.Map<LabTest>(createDto);
            await _unitOfWork.LabTests.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<LabTestDto>(entity);
        }

        public async Task UpdateAsync(UpdateLabTestDto updateDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
                throw new AppValidationException(validationResult.Errors);

            var entity = await _unitOfWork.LabTests.GetByIdAsync(updateDto.Id);
            if (entity == null)
                throw new NotFoundException(nameof(LabTest), updateDto.Id);

            var codeExists = await _unitOfWork.LabTests.ExistsByCodeAsync(updateDto.Code, updateDto.Id);
            if (codeExists)
                throw new BadRequestException($"A lab test with code '{updateDto.Code}' already exists.");

            _mapper.Map(updateDto, entity);
            await _unitOfWork.LabTests.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _unitOfWork.LabTests.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException(nameof(LabTest), id);

            await _unitOfWork.LabTests.DeleteAsync(entity);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
