using System.Collections.Generic;

namespace Hospital.Application.DTOs.Laboratory
{
    public class RecordLabResultsDto
    {
        public List<RecordLabItemResultDto> Results { get; set; } = new();
    }
}
