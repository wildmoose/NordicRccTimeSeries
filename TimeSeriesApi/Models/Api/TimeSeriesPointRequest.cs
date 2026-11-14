using System;
using System.ComponentModel.DataAnnotations;

namespace TimeSeriesApi.Models.Api
{
    public class TimeSeriesPointRequest
    {
        // Nullable + [Required]: a missing value is a 400, not a silent 0.
        [Required]
        public int? Position { get; set; }

        [Required]
        public decimal? Quantity { get; set; }
    }
}