using System;

namespace Hospital.Application.DTOs.Laboratory
{
    public class CollectSampleDto
    {
        public DateTime? SampleCollectionDate { get; set; }
        public string? SampleCollectedBy { get; set; }
    }
}
