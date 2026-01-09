using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PropertyApp.Config;
using PropertyApp.Data;
using PropertyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // add this

namespace PropertyApp.Pages.Charts
{
    public class ChartsModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;
        private readonly DevelopmentVariables _devVars;
        private readonly ILogger<IndexModel> _logger;


        public ChartsModel(PropertyContext context, IOptions<DevelopmentVariables> devVars, ILogger<IndexModel> logger)
        {
            _context = context;
            _devVars = devVars.Value;
            _logger = logger;
        }

        // Expose to the Razor page

        public IList<Measure> Measures { get; set; } = default!;
        public int CurrentUserId { get; set; }
        public int IdMeasureDeviceOfInterest { get; set; }
        public string DeviceType { get; set; } = string.Empty;

        // Define variables for earliest and latest measure dates
        public DateTime earliestDate;
        public DateTime latestDate;

        // Define a list to hold daily consumption values for charting purposes where {date, value} pairs are stored
        public record DailyPoint(DateTime Date, decimal Value);
        public List<DailyPoint> DailyConsumption { get; set; } = new List<DailyPoint>();


        public void OnGet(int? id)
        {
            // prefer session-stored IdCurrentUser when available
            CurrentUserId = HttpContext.Session.GetInt32("IdCurrentUser") ?? _devVars.IdCurrentUser;
            IdMeasureDeviceOfInterest = id ?? _devVars.IdMeasureDeviceOfInterest;

            // Get DeviceType for the selected MeasureDevice
            DeviceType = _context.MeasureDevices
                .FirstOrDefault(md => md.IdMeasureDevice == IdMeasureDeviceOfInterest)
                ?.DeviceType ?? string.Empty;
        }

        // Accept optional id from AJAX so chart data is returned for requested device
        public async Task<JsonResult> OnGetChartDataAsync(string period = "daily", int? id = null)
        {
            CurrentUserId = HttpContext.Session.GetInt32("IdCurrentUser") ?? _devVars.IdCurrentUser;

            // Use id provided by client if present, otherwise fall back to configured dev var
            IdMeasureDeviceOfInterest = id ?? _devVars.IdMeasureDeviceOfInterest;

            // log the raw config value early
            _logger.LogInformation("DevelopmentVariables.IdCurrentUser = {IdCurrentUserFromConfig}", CurrentUserId);
            _logger.LogInformation("IdMeasureDeviceOfInterest = {IdMeasureDeviceOfInterest}", IdMeasureDeviceOfInterest);

            // We can only show data if we have a IdMeasureDeviceOfInterest set (>0) and that device belongs to a apartment
            // where the current user have been a owner or tenant at the time of the measure.
            if (IdMeasureDeviceOfInterest > 0 && CurrentUserId > 0)
            {
                _logger.LogInformation("Fetching measures for only one IdMeasureDevice and User was owner or tenant at the time of measure");

                Measures = await _context.Measures
                    .Where(m => m.IdMeasureDevice == IdMeasureDeviceOfInterest)
                    .Include(m => m.IdMeasureDeviceNavigation)
                    .Include(m => m.IdUserNavigation)
                    .OrderBy(m => m.Date)
                    .ToListAsync();

                // Filter measures to only include those where the user was owner or tenant at the time of the measure
                Measures = Measures.Where(m =>
                {
                    var apartment = _context.Apartments
                        .FirstOrDefault(a => a.IdApartment == m.IdMeasureDeviceNavigation.IdApartment);
                    if (apartment == null)
                        return false;
                    var ownerships = _context.ApartmentUsers
                        .Where(o => o.IdApartment == apartment.IdApartment && o.IdUser == CurrentUserId && o.UserRole == "owner")
                        .ToList();
                    var tenancies = _context.ApartmentUsers
                        .Where(t => t.IdApartment == apartment.IdApartment && t.IdUser == CurrentUserId && t.UserRole == "tenant")
                        .ToList();
                    bool wasOwnerOrTenant = ownerships.Any(o => o.FromDate <= m.Date && (o.EndDate == null || o.EndDate >= m.Date)) ||
                                           tenancies.Any(t => t.FromDate <= m.Date && (t.EndDate == null || t.EndDate >= m.Date));
                    return wasOwnerOrTenant;
                }).ToList();

                // Get earliest and latest measure dates
                if (Measures.Count > 0)
                {
                    earliestDate = Measures.Min(m => m.Date);
                    latestDate = Measures.Max(m => m.Date);
                    _logger.LogInformation("Fetched {Count} measures between {EarliestDate} and {LatestDate}", Measures.Count, earliestDate, latestDate);
                }
                else
                {
                    _logger.LogInformation("No measures found for the specified device and user.");
                    return new JsonResult(DailyConsumption);
                }

                // Loop through measures 
                if (Measures != null && Measures.Count > 0)
                {
                    for (int i = 0; i < Measures.Count; i++)
                    {
                        var m = Measures[i];
                        _logger.LogInformation("Measure {Index}: IdMeasure={IdMeasure}, Date={Date}, Value={Value}, UserId={UserId}",
                            i, m.MeasureId, m.Date, m.Amount, m.IdUser);
                        var m_next = (i < Measures.Count - 1) ? Measures[i + 1] : null;
                        // Calculate daily consumption if there is a next measure
                        if (m_next != null)
                        {
                            var daysDiff = (m_next.Date - m.Date).TotalDays;
                            if (daysDiff > 0)
                            {
                                var consumption = (m_next.Amount - m.Amount) / (decimal)daysDiff;
                                _logger.LogInformation("  Next Measure Date={NextDate}, Amount={NextAmount}, DaysDiff={DaysDiff}, DailyConsumption={DailyConsumption}",
                                    m_next.Date, m_next.Amount, daysDiff, consumption);
                                // Store the daily consumption values with the dates from m.Date to m_next.Date
                                for (DateTime date = m.Date; date < m_next.Date; date = date.AddDays(1))
                                {
                                    DailyConsumption.Add(new DailyPoint(date, consumption));
                                }
                            }
                            else
                            {
                                _logger.LogWarning("  Next measure date is not after current measure date. Skipping consumption calculation.");
                                return new JsonResult(DailyConsumption);
                            }
                        }
                    }

                    if (period == "daily")
                    {
                        return new JsonResult(DailyConsumption);
                    }

                    if (period == "monthly")
                    {
                        // after DailyConsumption is populated calculate monthly averages
                        var monthly = DailyConsumption
                            .GroupBy(p => new { p.Date.Year, p.Date.Month })
                            .Select(g => new {
                                date = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("yyyy-MM-dd"),
                                value = g.Sum(p => p.Value)
                            })
                            .OrderBy(x => x.date)
                            .ToList();
                        return new JsonResult(monthly);
                    }

                    if (period == "yearly")
                    {
                        var yearly = DailyConsumption
                            .GroupBy(p => p.Date.Year)
                            .Select(g => new {
                                date = new DateTime(g.Key, 1, 1).ToString("yyyy-MM-dd"),
                                value = g.Sum(p => p.Value)
                            })
                            .OrderBy(x => x.date)
                            .ToList();
                        return new JsonResult(yearly);
                    }

                    // If period is not recognized, return daily by default
                    return new JsonResult(DailyConsumption);
                }
                else
                {
                    return new JsonResult(DailyConsumption);
                }
            }
            else
            {
                _logger.LogWarning("IdMeasureDeviceOfInterest or CurrentUserId is not set properly. Cannot fetch measures.");
                return new JsonResult(DailyConsumption);
            }
        }
    }
}

// End of the code snippet.