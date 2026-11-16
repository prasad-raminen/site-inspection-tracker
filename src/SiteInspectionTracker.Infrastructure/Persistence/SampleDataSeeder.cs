using Microsoft.EntityFrameworkCore;
using SiteInspectionTracker.Domain.Entities;
using SiteInspectionTracker.Domain.Enums;

namespace SiteInspectionTracker.Infrastructure.Persistence;

/// <summary>
/// Populates an empty database with a small, realistic data set so the UI and API have
/// something to show. Dates are relative to "now" so the dashboard always has upcoming work.
/// </summary>
public static class SampleDataSeeder
{
    public static async Task<bool> SeedAsync(AppDbContext db, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (await db.Sites.AnyAsync(cancellationToken))
        {
            return false;
        }

        var today = utcNow.Date;

        var tower = new Site("Harbourfront Tower", "120 Quay Street, Auckland", "Meridian Developments", -36.8433, 174.7665);
        var bridge = new Site("Northgate Rail Bridge", "Northgate Rail Corridor, km 14.2", "Transit Infrastructure Authority", 51.5072, -0.1276);
        var plant = new Site("Riverside Water Treatment Plant", "4 Mill Road, Riverside", "City Utilities", 47.6062, -122.3321);
        var warehouse = new Site("Logistics Hub B", "Unit 7, Eastport Industrial Estate", clientName: null);

        db.Sites.AddRange(tower, bridge, plant, warehouse);
        await db.SaveChangesAsync(cancellationToken);

        var structural = new Inspection(tower.Id, "Level 12 structural steel check", "Aroha Ngata", today.AddDays(-14).AddHours(9), "Laser scan captured for as-built comparison.");
        var facade = new Inspection(tower.Id, "Facade anchor pull-out test", "Liam Chen", today.AddDays(-2).AddHours(10));
        var scaffold = new Inspection(tower.Id, "Weekly scaffold safety walk", "Aroha Ngata", today.AddDays(3).AddHours(8));
        var bearings = new Inspection(bridge.Id, "Bearing and expansion joint survey", "Priya Raman", today.AddDays(-30).AddHours(7), "Drone survey of the deck underside.");
        var deck = new Inspection(bridge.Id, "Deck surface crack mapping", "Priya Raman", today.AddDays(7).AddHours(7));
        var tanks = new Inspection(plant.Id, "Clarifier tank corrosion inspection", "Marcus Webb", today.AddDays(-7).AddHours(13));
        var electrical = new Inspection(plant.Id, "Switchgear thermal imaging", "Sofia Alvarez", today.AddDays(1).AddHours(11));
        var racking = new Inspection(warehouse.Id, "Pallet racking inspection", "Liam Chen", today.AddDays(10).AddHours(9));

        db.Inspections.AddRange(structural, facade, scaffold, bearings, deck, tanks, electrical, racking);
        await db.SaveChangesAsync(cancellationToken);

        structural.ChangeStatus(InspectionStatus.InProgress);
        structural.AddFinding("Two beam-to-column bolts on grid C4 not fully tensioned.", FindingSeverity.High).MarkResolved();
        structural.AddFinding("Fireproofing overspray on embedded plates.", FindingSeverity.Low).MarkResolved();
        structural.ChangeStatus(InspectionStatus.Completed);

        facade.ChangeStatus(InspectionStatus.InProgress);
        facade.AddFinding("Anchor A-17 failed pull-out test at 60% of design load.", FindingSeverity.Critical);
        facade.AddFinding("Sealant gaps at panel joints on north elevation.", FindingSeverity.Medium);

        bearings.ChangeStatus(InspectionStatus.InProgress);
        bearings.AddFinding("Seized sliding bearing at pier 3; displacement exceeds tolerance.", FindingSeverity.Critical);
        bearings.AddFinding("Debris build-up in expansion joint EJ-2.", FindingSeverity.Medium).MarkResolved();
        bearings.AddFinding("Minor spalling on pier 2 cap beam.", FindingSeverity.Low);
        bearings.ChangeStatus(InspectionStatus.Failed);

        tanks.ChangeStatus(InspectionStatus.InProgress);
        tanks.AddFinding("Coating breakdown with pitting on clarifier 2 weir.", FindingSeverity.High);
        tanks.AddFinding("Handrail corrosion on walkway access stair.", FindingSeverity.Medium);
        tanks.ChangeStatus(InspectionStatus.Completed);

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
