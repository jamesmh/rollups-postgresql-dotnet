using System.Data.Common;
using System.Diagnostics;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Rollups.Models;

namespace Rollups.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> PageViewsFor2024([FromServices] DbConnection connection)
    {
        var _2024 = new DateTime(2024, 1, 1);
        var startTime = Stopwatch.GetTimestamp();

        var pageViews = await connection.QueryAsync<long>(@"
            select
                count(*) as views
            from 
                user_events
            where 
                tenant_id = 6
                and event_type = 3 -- 3 is a enum for ""page viewed""
                and (created_at >= @start and created_at < @end);
        ", new {
            Start = _2024,
            End = _2024.AddYears(1)
        });

       var elapsedTime = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;

        return Json(new {
            PageViews = pageViews,
            Elapsed = elapsedTime + " ms"
        });
    }

    public async Task<IActionResult> PageViewsFor2024Rollup([FromServices] DbConnection connection)
    {
        var _2024 = new DateTime(2024, 1, 1);
        var startTime = Stopwatch.GetTimestamp();

        var pageViews = await connection.QueryAsync<long?>(@"
            select
                sum(page_views)
            from 
                rollup_page_views_per_tenant_per_day
            where 
                tenant_id = 6
                and (at_day >= @start and at_day < @end);
        ", new {
            Start = _2024,
            End = _2024.AddYears(1)
        });

       var elapsedTime = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;

        return Json(new {
            PageViews = pageViews,
            Elapsed = elapsedTime + " ms"
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

internal class StopWatch
{
    public StopWatch()
    {
    }
}