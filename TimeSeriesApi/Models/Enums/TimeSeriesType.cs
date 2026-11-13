using System;

namespace TimeSeriesApi.Models.Enums
{
    /// <summary>
    /// The kind of quantity a time series represents.
    /// </summary>
    public enum TimeSeriesType
    {
        Production,
        Consumption,
        CrossBorderFlow
    }
}